using System.Text.RegularExpressions;

namespace MonsterTrainAccessibility.Contracts
{
    /// <summary>
    /// Normalizes raw game text for speech. Game labels are TextMeshPro, so they carry rich-text
    /// markup (color, sprite, size, style tags) and hard line breaks. Strip the markup, turn line breaks
    /// into sentence breaks so multi-line text reads with a pause, fold typographic punctuation, and
    /// collapse the remaining whitespace. Pure and unit-tested.
    /// </summary>
    public static class TextFilter
    {
        private static readonly Regex RichTags = new Regex("<[^>]+>", RegexOptions.Compiled);
        // A line break (with surrounding whitespace) after text that does not already end a sentence.
        private static readonly Regex BreakAfterText = new Regex("(?<=[^\\s.!?,:;])\\s*[\\r\\n]+\\s*", RegexOptions.Compiled);
        // Any remaining line break (after sentence punctuation, where the pause already exists).
        private static readonly Regex LineBreak = new Regex("\\s*[\\r\\n]+\\s*", RegexOptions.Compiled);
        private static readonly Regex Whitespace = new Regex("\\s+", RegexOptions.Compiled);
        // A sentence period and a separating comma that collide when game text is concatenated with our
        // own delimiter; the second, deliberate mark wins. Mixed pairs only, so an ellipsis is untouched.
        private static readonly Regex PeriodThenComma = new Regex("\\.\\s*,", RegexOptions.Compiled);
        private static readonly Regex CommaThenPeriod = new Regex(",\\s*\\.", RegexOptions.Compiled);
        // Unicode bidi control characters: they shape visual direction, which speech has none of.
        private static readonly Regex BidiControls =
            new Regex("[\\u061C\\u200E\\u200F\\u202A-\\u202E\\u2066-\\u2069]", RegexOptions.Compiled);

        public static string Clean(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return string.Empty;

            string s = RichTags.Replace(raw, string.Empty);
            s = s.Replace(' ', ' ');   // non-breaking space
            s = s.Replace('​', ' ');   // zero-width space TMP sometimes injects
            s = BidiControls.Replace(s, string.Empty);
            s = FoldPunctuation(s);
            s = s.Trim();
            s = BreakAfterText.Replace(s, ". ");
            s = LineBreak.Replace(s, " ");
            s = PeriodThenComma.Replace(s, ", ");
            s = CommaThenPeriod.Replace(s, ". ");
            s = Whitespace.Replace(s, " ").Trim();
            return s;
        }

        // Fold the Unicode typographic punctuation common in game text (smart dashes, curly quotes,
        // ellipsis) to plain ASCII so it reads cleanly; an em dash is otherwise announced as "dash".
        private static string FoldPunctuation(string s)
        {
            s = s.Replace('–', '-')   // en dash
                 .Replace('—', '-')   // em dash
                 .Replace('―', '-')   // horizontal bar
                 .Replace('‒', '-')   // figure dash
                 .Replace('−', '-')   // minus sign
                 .Replace('‘', '\'')  // left single quote
                 .Replace('’', '\'')  // right single quote / apostrophe
                 .Replace('“', '"')   // left double quote
                 .Replace('”', '"');  // right double quote
            return s.Replace("…", "...");  // ellipsis
        }
    }
}

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace MonsterTrainAccessibility.Core.Text
{
    /// <summary>
    /// Cleans game-sourced strings for matching and speech. Game UI text is TMP rich text (labels
    /// can carry color/size/sprite tags), so type-ahead strips tags before comparing; speech strips
    /// downstream in the pipeline.
    /// </summary>
    public static class TextUtil
    {
        // Sub/superscripts are decorative; their content is noise in speech, so drop tag AND text.
        private static readonly Regex SubSup =
            new Regex("<(sub|sup)>.*?</(sub|sup)>", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);
        private static readonly Regex RichTextTag = new Regex("<[^>]+>", RegexOptions.Compiled);
        private static readonly Regex Whitespace = new Regex(@"\s+", RegexOptions.Compiled);

        public static string StripRichText(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            s = SubSup.Replace(s, "");
            // Strip remaining tags to nothing: tags are usually inline (a drop-cap "<size=200%>N</size>ew
            // Game"), so a space here would wrongly split words.
            s = RichTextTag.Replace(s, "");
            s = Whitespace.Replace(s, " ");
            return s.Trim();
        }

        /// <summary>Fold accents away for matching ("Séance" matches "seance"); ligatures expand.</summary>
        public static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            var decomposed = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(decomposed.Length);
            for (int i = 0; i < decomposed.Length; i++)
            {
                char c = decomposed[i];
                switch (c)
                {
                    case 'œ': case 'Œ': sb.Append("oe"); break;
                    case 'æ': case 'Æ': sb.Append("ae"); break;
                    default:
                        if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                            sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }
    }
}

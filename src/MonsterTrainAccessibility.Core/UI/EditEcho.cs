using System;

namespace MonsterTrainAccessibility.Core.UI
{
    /// <summary>A text field as an edit leaves it: the text, the caret and the selection's other end
    /// (equal to the caret when nothing is selected).</summary>
    public struct EditState
    {
        public string Text;
        public int Caret;
        public int Anchor;

        public EditState(string text, int caret, int anchor)
        {
            Text = text ?? "";
            Caret = Clamp(caret, Text.Length);
            Anchor = Clamp(anchor, Text.Length);
        }

        public int SelectionStart => Math.Min(Caret, Anchor);
        public int SelectionEnd => Math.Max(Caret, Anchor);
        public bool HasSelection => Caret != Anchor;

        private static int Clamp(int i, int length) => i < 0 ? 0 : i > length ? length : i;
    }

    /// <summary>
    /// What a screen reader says for one step of editing a text field, from the field's state before
    /// and after it: the typed or pasted text, else the text a deletion took; with the text unchanged,
    /// the part a selection gained ("yu selected") or lost ("u unselected"), else the character the
    /// caret came to (the word, for a Ctrl move; "blank" past the end). Null when nothing changed.
    /// </summary>
    public static class EditEcho
    {
        public static string Describe(EditState before, EditState after, bool wordMove)
        {
            string a = before.Text ?? "", b = after.Text ?? "";
            if (!string.Equals(a, b, StringComparison.Ordinal))
            {
                int prefix = 0;
                while (prefix < a.Length && prefix < b.Length && a[prefix] == b[prefix]) prefix++;
                int suffix = 0;
                while (suffix < a.Length - prefix && suffix < b.Length - prefix
                    && a[a.Length - 1 - suffix] == b[b.Length - 1 - suffix]) suffix++;
                string inserted = b.Substring(prefix, b.Length - suffix - prefix);
                string removed = a.Substring(prefix, a.Length - suffix - prefix);
                return Name(inserted.Length > 0 ? inserted : removed);
            }

            if (after.HasSelection)
            {
                if (before.HasSelection && before.SelectionStart == after.SelectionStart && before.SelectionEnd == after.SelectionEnd) return null;
                if (before.HasSelection)
                {
                    // One end moved: the part gained or lost, as the selection grows or shrinks.
                    if (after.SelectionStart <= before.SelectionStart && after.SelectionEnd >= before.SelectionEnd)
                        return Strings.Strings.EditSelected(Name(Outside(b, after, before)));
                    if (after.SelectionStart >= before.SelectionStart && after.SelectionEnd <= before.SelectionEnd)
                        return Strings.Strings.EditUnselected(Name(Outside(b, before, after)));
                }
                return Strings.Strings.EditSelected(Name(b.Substring(after.SelectionStart, after.SelectionEnd - after.SelectionStart)));
            }

            if (!before.HasSelection && before.Caret == after.Caret) return null;
            if (after.Caret >= b.Length) return Strings.Strings.ValueBlank;
            if (!wordMove) return Name(b[after.Caret].ToString());
            int end = after.Caret;
            while (end < b.Length && !char.IsWhiteSpace(b[end])) end++;
            return Name(end > after.Caret ? b.Substring(after.Caret, end - after.Caret) : b[after.Caret].ToString());
        }

        // The part of the outer range the inner one leaves out (they share an end, or both).
        private static string Outside(string text, EditState outer, EditState inner)
        {
            string left = text.Substring(outer.SelectionStart, inner.SelectionStart - outer.SelectionStart);
            string right = text.Substring(inner.SelectionEnd, outer.SelectionEnd - inner.SelectionEnd);
            return left + right;
        }

        /// <summary>A piece of text as it is echoed: a lone space by its word, anything else as it is.</summary>
        public static string Name(string text)
        {
            if (string.IsNullOrEmpty(text)) return Strings.Strings.ValueBlank;
            return text == " " ? Strings.Strings.ValueSpace : text;
        }
    }
}

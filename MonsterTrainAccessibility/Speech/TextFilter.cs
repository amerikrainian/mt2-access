using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using MonsterTrainAccessibility.Localization;

namespace MonsterTrainAccessibility.Speech
{
    internal static class TextFilter
    {
        private const string TableName = "textfilter";

        private static readonly Regex WhitespaceRegex = new Regex("\\s+", RegexOptions.Compiled);
        private static readonly Regex RepeatedPunctuationRegex = new Regex("([!?.,])\\1+", RegexOptions.Compiled);

        private static KeyValuePair<string, string>[] _rules = Array.Empty<KeyValuePair<string, string>>();

        public static void Reload()
        {
            IReadOnlyDictionary<string, string> table = LocalizationManager.Instance?.GetTable(TableName);
            if (table == null || table.Count == 0)
            {
                _rules = Array.Empty<KeyValuePair<string, string>>();
                return;
            }

            KeyValuePair<string, string>[] rules = new KeyValuePair<string, string>[table.Count];
            int index = 0;
            foreach (KeyValuePair<string, string> pair in table)
            {
                rules[index++] = pair;
            }

            _rules = rules;
        }

        public static string Apply(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            text = WhitespaceRegex.Replace(text, " ").Trim();
            text = RepeatedPunctuationRegex.Replace(text, "$1");

            KeyValuePair<string, string>[] rules = _rules;
            for (int i = 0; i < rules.Length; i++)
            {
                text = text.Replace(rules[i].Key, rules[i].Value);
            }

            return text;
        }
    }
}

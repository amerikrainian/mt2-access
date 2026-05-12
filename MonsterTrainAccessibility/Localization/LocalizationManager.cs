using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Speech;

namespace MonsterTrainAccessibility.Localization
{
    internal sealed class LocalizationManager
    {
        private const string DefaultLanguageCode = "en";
        private const string MessagesTable = "messages";

        private readonly Dictionary<string, Dictionary<string, string>> _tables = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _missingKeys = new HashSet<string>(StringComparer.Ordinal);
        private string _currentLanguageCode = DefaultLanguageCode;

        public static LocalizationManager Instance { get; private set; }

        public static LocalizationManager Initialize()
        {
            if (Instance != null)
            {
                return Instance;
            }

            Instance = new LocalizationManager();
            Instance.ReloadInternal(DefaultLanguageCode);
            return Instance;
        }

        public static void Shutdown()
        {
            Instance = null;
        }

        public static string Get(string key)
        {
            return Instance?.GetInternal(key) ?? FormatMissingKey(key);
        }

        public static string Get(string table, string key)
        {
            return Instance?.GetInternal(table, key) ?? FormatMissingKey(string.IsNullOrEmpty(table) ? key : table + "." + key);
        }

        public static void ReloadCurrentLanguage(string languageCode = null)
        {
            Instance?.ReloadInternal(languageCode);
        }

        public IReadOnlyDictionary<string, string> GetTable(string tableName)
        {
            if (string.IsNullOrEmpty(tableName))
            {
                return null;
            }

            return _tables.TryGetValue(tableName, out Dictionary<string, string> table) ? table : null;
        }

        private void ReloadInternal(string languageCode)
        {
            _currentLanguageCode = NormalizeLanguageCode(languageCode);
            _tables.Clear();
            _missingKeys.Clear();

            foreach (string path in ResolveCandidateFiles(_currentLanguageCode))
            {
                if (!File.Exists(path))
                {
                    continue;
                }

                try
                {
                    MergeBundle(File.ReadAllText(path));
                }
                catch (Exception ex)
                {
                    Log.Warn("Failed to load localization bundle " + path + ": " + ex);
                }
            }

            global::MonsterTrainAccessibility.Speech.TextFilter.Reload();
        }

        private string GetInternal(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }

            if (_tables.TryGetValue(MessagesTable, out Dictionary<string, string> messages) &&
                messages.TryGetValue(key, out string value))
            {
                return value;
            }

            foreach (KeyValuePair<string, Dictionary<string, string>> tablePair in _tables)
            {
                if (tablePair.Value.TryGetValue(key, out value))
                {
                    return value;
                }
            }

            if (_missingKeys.Add(key))
            {
                Log.Warn("Missing localization key: " + key);
            }

            return FormatMissingKey(key);
        }

        private string GetInternal(string table, string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }

            if (!string.IsNullOrEmpty(table) &&
                _tables.TryGetValue(table, out Dictionary<string, string> tableEntries))
            {
                if (tableEntries.TryGetValue(key, out string tableValue))
                {
                    return tableValue;
                }
            }

            string flatKey = string.IsNullOrEmpty(table) ? key : table + "." + key;
            return GetInternal(flatKey);
        }

        private static string FormatMissingKey(string key)
        {
            return "MISSING(" + (key ?? string.Empty) + ")";
        }

        private void MergeBundle(string json)
        {
            using (JsonDocument document = JsonDocument.Parse(json))
            {
                foreach (JsonProperty tableProperty in document.RootElement.EnumerateObject())
                {
                    if (!_tables.TryGetValue(tableProperty.Name, out Dictionary<string, string> table))
                    {
                        table = new Dictionary<string, string>(StringComparer.Ordinal);
                        _tables.Add(tableProperty.Name, table);
                    }

                    foreach (JsonProperty entryProperty in tableProperty.Value.EnumerateObject())
                    {
                        table[entryProperty.Name] = entryProperty.Value.GetString() ?? string.Empty;
                    }
                }
            }
        }

        private static IEnumerable<string> ResolveCandidateFiles(string languageCode)
        {
            string stringsDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty, "Localization", "Strings");
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string candidate in new[]
            {
                Path.Combine(stringsDir, DefaultLanguageCode + ".json"),
                Path.Combine(stringsDir, GetNeutralLanguageCode(languageCode) + ".json"),
                Path.Combine(stringsDir, languageCode + ".json")
            })
            {
                if (seen.Add(candidate))
                {
                    yield return candidate;
                }
            }
        }

        private static string NormalizeLanguageCode(string languageCode)
        {
            return string.IsNullOrWhiteSpace(languageCode) ? DefaultLanguageCode : languageCode.Trim();
        }

        private static string GetNeutralLanguageCode(string languageCode)
        {
            string normalized = NormalizeLanguageCode(languageCode);
            int separator = normalized.IndexOf('-');
            return separator > 0 ? normalized.Substring(0, separator) : normalized;
        }
    }
}

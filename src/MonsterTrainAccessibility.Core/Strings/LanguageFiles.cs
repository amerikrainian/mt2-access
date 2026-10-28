using System;
using System.Collections.Generic;

namespace MonsterTrainAccessibility.Core.Strings
{
    /// <summary>
    /// Which lang/ file answers a game language. The game names a language by its Unity locale code
    /// ("de", "pt-BR", "zh-Hans"), and the files are named for the same codes, so the code is the first
    /// candidate; then its lowercase (a file system that tells cases apart), then the bare language
    /// ("pt" for "pt-BR", "zh" for "zh-Hans": a regional code the mod has no file of its own for).
    /// </summary>
    public static class LanguageFiles
    {
        /// <summary>The file names (without directory) to try for a locale code, best first, no repeats.</summary>
        public static List<string> Candidates(string code)
        {
            var names = new List<string>();
            if (string.IsNullOrWhiteSpace(code)) return names;
            code = code.Trim();
            Add(names, code);
            Add(names, code.ToLowerInvariant());
            int cut = code.IndexOfAny(new[] { '-', '_' });
            if (cut > 0) Add(names, code.Substring(0, cut).ToLowerInvariant());
            return names;
        }

        private static void Add(List<string> names, string stem)
        {
            string name = stem + ".txt";
            foreach (var n in names) if (string.Equals(n, name, StringComparison.Ordinal)) return;
            names.Add(name);
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MonsterTrainAccessibility.Core.Strings;
using Xunit;

namespace MonsterTrainAccessibility.Tests
{
    /// <summary>
    /// The shipped lang/*.txt files against the strings table: one per language the game offers, named
    /// for the game's own locale code (which is how LanguageSync finds it), complete, well formed, every
    /// slot of the English kept. A broken entry would otherwise surface only as an English or a garbled
    /// line in a language none of us plays in.
    /// </summary>
    public class LangFilesTests : IDisposable
    {
        public void Dispose() => Strings.LoadTranslation(null);

        /// <summary>The game's languages as I2 Localization names them
        /// (<c>LocalizationManager.GetAllLanguagesCode</c>, read live: en-US, fr-FR, de-DE, ru, pt-BR,
        /// zh-CN, es, zh-HK, ko, ja), with English as "en": the generated template, which en-US falls
        /// back to.</summary>
        private static readonly string[] Codes = { "de-DE", "en", "es", "fr-FR", "ja", "ko", "pt-BR", "ru", "zh-CN", "zh-HK" };

        public static IEnumerable<object[]> LanguageCodes() => Codes.Select(c => new object[] { c });

        [Fact]
        public void There_is_a_file_for_every_language_of_the_game_and_no_other()
        {
            var files = Directory.GetFiles(LangDir(), "*.txt").Select(Path.GetFileNameWithoutExtension)
                .OrderBy(c => c, StringComparer.Ordinal);
            Assert.Equal(Codes.OrderBy(c => c, StringComparer.Ordinal), files);
        }

        /// <summary>lang/en.txt is <see cref="Strings.DumpTemplate"/>, so a translator's starting point
        /// never lags the table. After adding or rewording a string, regenerate it: run this test with
        /// MTA_WRITE_LANG_TEMPLATE=1.</summary>
        [Fact]
        public void The_English_file_is_the_generated_template()
        {
            string path = Path.Combine(LangDir(), "en.txt");
            if (Environment.GetEnvironmentVariable("MTA_WRITE_LANG_TEMPLATE") == "1")
                File.WriteAllText(path, Strings.DumpTemplate());
            Assert.Equal(Strings.DumpTemplate(), File.ReadAllText(path).Replace("\r\n", "\n"));
        }

        [Theory]
        [MemberData(nameof(LanguageCodes))]
        public void A_lang_file_is_complete_and_well_formed(string code)
        {
            string path = Path.Combine(LangDir(), code + ".txt");
            string content = File.ReadAllText(path);
            Assert.DoesNotContain((char)0xFFFD, content); // a replacement character: the file is not UTF-8

            var lines = File.ReadAllLines(path);
            var report = Strings.LoadTranslation(lines);
            Assert.Empty(report.Problems());

            // Every data line is a key of its own (a repeated key would silently be last-wins), and
            // every key of the table is there.
            var keys = lines.Select(l => l.Trim()).Where(l => l.Length > 0 && l[0] != '#')
                .Select(l => l.Substring(0, l.IndexOf('=')).Trim()).ToList();
            Assert.Equal(keys.Count, keys.Distinct(StringComparer.Ordinal).Count());
            Assert.Equal(Strings.Keys.OrderBy(k => k, StringComparer.Ordinal), keys.OrderBy(k => k, StringComparer.Ordinal));

            // The same slots as the English: a dropped one is dropped information.
            foreach (var key in Strings.Keys)
            {
                var english = Strings.Slots(Strings.Default(key)).OrderBy(n => n);
                var translated = Strings.Slots(Strings.Get(key)).OrderBy(n => n);
                Assert.True(english.SequenceEqual(translated), code + ".txt, " + key + ": the slots differ from the English");
            }
        }

        private static string LangDir()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "MonsterTrainAccessibility.slnx"))) dir = dir.Parent;
            Assert.True(dir != null, "the repository root was not found above " + AppContext.BaseDirectory);
            return Path.Combine(dir.FullName, "lang");
        }
    }
}

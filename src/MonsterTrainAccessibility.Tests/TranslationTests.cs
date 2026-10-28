using System;
using MonsterTrainAccessibility.Core.Strings;
using Xunit;

namespace MonsterTrainAccessibility.Tests
{
    /// <summary>
    /// The strings table under a translation: what a lang file overrides, what it cannot, and that a
    /// language switch swaps the whole file. Each test ends back on the English defaults.
    /// </summary>
    public class TranslationTests : IDisposable
    {
        public void Dispose() => Strings.LoadTranslation(null);

        [Fact]
        public void A_translated_key_overrides_and_a_missing_one_stays_English()
        {
            var report = Strings.LoadTranslation(new[] { "# German", "", "role.button=Schalter" });
            Assert.Equal(1, report.Applied);
            Assert.Empty(report.Problems());
            Assert.Equal("Schalter", Strings.Get("role.button"));
            Assert.Equal("toggle", Strings.Get("role.toggle"));
        }

        [Fact]
        public void A_template_keeps_its_slots_in_the_translations_order()
        {
            Strings.LoadTranslation(new[] { "nav.position = {1} gesamt, Nummer {0}" });
            Assert.Equal("9 gesamt, Nummer 3", Strings.Position(3, 9));
        }

        [Fact]
        public void A_value_may_hold_an_equals_sign_and_a_written_line_break()
        {
            Strings.LoadTranslation(new[] { "help.row={0} = {1}\\nweiter" });
            Assert.Equal("Zurück = Escape\nweiter", Strings.F("help.row", "Zurück", "Escape"));
        }

        [Fact]
        public void A_byte_order_mark_does_not_hide_the_first_key()
        {
            Strings.LoadTranslation(new[] { (char)0xFEFF + "role.button=Schalter" });
            Assert.Equal("Schalter", Strings.Get("role.button"));
        }

        [Fact]
        public void Null_returns_to_English()
        {
            Strings.LoadTranslation(new[] { "role.button=Schalter" });
            Strings.LoadTranslation(null);
            Assert.Equal("button", Strings.Get("role.button"));
        }

        [Fact]
        public void A_switch_never_blends_two_languages()
        {
            Strings.LoadTranslation(new[] { "role.button=Schalter", "role.toggle=Umschalter" });
            Strings.LoadTranslation(new[] { "role.button=bouton" });
            Assert.Equal("bouton", Strings.Get("role.button"));
            Assert.Equal("toggle", Strings.Get("role.toggle"));
        }

        [Fact]
        public void What_a_file_gets_wrong_is_reported_and_not_applied()
        {
            var report = Strings.LoadTranslation(new[]
            {
                "no equals sign here",
                "role.buton=Schalter",          // a typo: no such key
                "role.toggle=",                 // empty: it would silence the role
                "nav.position={0} von {1} {2}", // a slot the English has not
                "search.no_match=kein {0",       // unbalanced
                "role.slider=Regler",
            });
            Assert.Equal(1, report.Applied);
            Assert.Equal(new[] { 1 }, report.Malformed);
            Assert.Equal(new[] { "role.buton" }, report.UnknownKeys);
            Assert.Equal(new[] { "role.toggle" }, report.EmptyKeys);
            Assert.Equal(new[] { "nav.position", "search.no_match" }, report.BadSlots);
            Assert.Equal("toggle", Strings.Get("role.toggle"));
            Assert.Equal("3 of 9", Strings.Position(3, 9));
            Assert.Equal("Regler", Strings.Get("role.slider"));
            Assert.Equal(4, System.Linq.Enumerable.Count(report.Problems()));
        }

        [Fact]
        public void A_translation_may_drop_a_slot_but_not_add_one()
        {
            Assert.True(Strings.SlotsFit("Rang", "rank {0}"));
            Assert.True(Strings.SlotsFit("{1}: {0}", "{0} {1}"));
            Assert.False(Strings.SlotsFit("{1}", "rank {0}"));
        }

        [Theory]
        [InlineData("de", new[] { "de.txt" })]
        [InlineData("pt-BR", new[] { "pt-BR.txt", "pt-br.txt", "pt.txt" })]
        [InlineData("zh-Hans", new[] { "zh-Hans.txt", "zh-hans.txt", "zh.txt" })]
        [InlineData("es_419", new[] { "es_419.txt", "es.txt" })]
        [InlineData(" fr ", new[] { "fr.txt" })]
        public void The_files_tried_for_a_locale_code(string code, string[] expected)
        {
            Assert.Equal(expected, LanguageFiles.Candidates(code));
        }

        [Fact]
        public void No_code_no_file()
        {
            Assert.Empty(LanguageFiles.Candidates(null));
            Assert.Empty(LanguageFiles.Candidates("  "));
        }
    }
}

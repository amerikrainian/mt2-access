using System;
using System.Collections.Generic;
using System.Linq;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Core.Buffers;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.Text;
using MonsterTrainAccessibility.Core.UI;
using Xunit;

namespace MonsterTrainAccessibility.Tests
{
    public class GlossaryTests
    {
        // An entry given links read them from its source text; one given none has no source text, and its
        // rendered body's exact names stand in.
        private static GlossaryEntry Entry(string name, string body, params string[] references)
            => new GlossaryEntry(name, "test:" + name, () => body, () => name + ": " + TextUtil.StripRichText(body),
                references.Length > 0 ? references.Select(r => "test:" + r) : null);

        private static GlossaryEntry Linked(string name, string body, params string[] references)
            => new GlossaryEntry(name, "test:" + name, () => body, () => name + ": " + TextUtil.StripRichText(body), references.Select(r => "test:" + r));

        // The definitions behind the examples, in English: Valor's source text names Armor, the ability
        // status Cooldown.
        private static Glossary English() => new Glossary(new[]
        {
            Entry("Valor", "<nobr><b>+1<sprite name=\"Attack\"></b></nobr> per stack. Raise <b>Armor</b> to stack count if this is the front unit after combat.", "Armor"),
            Entry("Armor", "Damage is dealt to <b>Armor</b> before health."),
            Entry("Ability", "This unit's <b>Ability</b> can be activated when there are no stacks of <b>Cooldown</b>.", "Cooldown"),
            Entry("Cooldown", "This unit's <b>Ability</b> can't be activated while there are any stacks of <b>Cooldown</b>.", "Ability"),
            Entry("Shift", "Triggers whenever this unit moves."),
            Entry("Multistrike", "Attacks an additional time per turn."),
        }, inflect: true);

        private static string[] Names(IEnumerable<GlossaryEntry> entries) => entries.Select(e => e.Name).ToArray();

        [Theory]
        [InlineData("<nobr><b>Valor</b> <b>1</b></nobr>", "Valor")]
        [InlineData("Valor 1", "Valor")]
        [InlineData("+2 Rage", "Rage")]
        [InlineData("Spikes {0}", "Spikes")]
        [InlineData("x2", "")]
        [InlineData("3x", "")]
        [InlineData("1x Valor", "Valor")]
        [InlineData("+", "")]
        [InlineData("x", "")]
        [InlineData("盾", "盾")]
        [InlineData("連撃1", "連撃")]
        [InlineData("アーマー20", "アーマー")]
        [InlineData("30X", "")]
        [InlineData("Алчность х10", "Алчность")]
        [InlineData("Scourge T1", "Scourge T1")]
        [InlineData("+1<sprite name=\"Attack\">", "")]
        [InlineData("«Пожирание»", "Пожирание")]
        [InlineData("<color=#88C701><sprite name=\"StatusValor\" tint=1></color> Valor", "Valor")]
        public void NormalizeStripsMarkupCountsAndQuotes(string raw, string expected)
            => Assert.Equal(expected, Glossary.Normalize(raw));

        [Fact]
        public void MentionsAreTheBoldSpansWithTheirCountsDropped()
            => Assert.Equal(new[] { "Shift", "Valor" },
                Glossary.Mentions("<b>Shift</b>: Gain <nobr><b>Valor</b> <b>1</b></nobr> permanently, <b>+1</b>.").ToArray());

        [Fact]
        public void ADefinitionAShownTooltipRefersToIsAdded()
        {
            // Unchained at the Light Forge: the game shows Shift, Multistrike and Valor, never Armor.
            var added = English().Expand(
                new[] { "Fel", "<b>Shift</b>: Gain <nobr><b>Valor</b> <b>1</b></nobr> permanently." },
                new[] { "Shift: Triggers whenever this unit moves.", "Multistrike: Attacks an additional time per turn.", "Valor: +1 per stack. Raise Armor to stack count if this is the front unit after combat." });
            Assert.Equal(new[] { "Armor" }, Names(added));
        }

        [Fact]
        public void MentionsFollowInTheOrderMetAndChainUntilNothingIsNew()
        {
            var added = English().Expand(new[] { "Savior", "<b>Ability</b>: Savior." }, new string[0]);
            Assert.Equal(new[] { "Ability", "Cooldown" }, Names(added));
        }

        [Fact]
        public void TheNodesOwnNameIsNeverAddedThoughWhatItRefersToIs()
        {
            var added = English().Expand(new[] { "Cooldown 3" }, new string[0]);
            Assert.Equal(new[] { "Ability" }, Names(added));
        }

        [Fact]
        public void MentionsInsideAHeadPartNeverCountAsShown()
        {
            // "<b>Shift</b>: ..." is card text, not Shift's tooltip: its definition is still wanted.
            var added = English().Expand(new[] { "<b>Shift</b>: Gain armor." }, new string[0]);
            Assert.Equal(new[] { "Shift" }, Names(added));
        }

        [Fact]
        public void NothingMentionedAddsNothing()
            => Assert.Empty(English().Expand(new[] { "Settings" }, new[] { "Volume: 80" }));

        [Fact]
        public void ATranslationReachesWhatItsSourceTextRefersToWhateverItsWording()
        {
            // Russian Valor writes Armor out by hand, inflected; the link comes from the source text.
            var russian = new Glossary(new[]
            {
                Entry("Доблесть", "+1 за уровень. После битвы, если боец находится впереди, он получит <b>броню</b> равную количеству слоев.", "Броня"),
                Entry("Броня", "Урон наносится сначала по броне."),
            });
            Assert.Equal(new[] { "Броня" }, Names(russian.Expand(new[] { "Фел" }, new[] { "Доблесть: +1 за уровень." })));
        }

        [Fact]
        public void ATranslationsSpellingIsNeverGuessedAt()
        {
            // German Dormant: "Verzauberungen" is Enchant (Begeisterung), not Incant (Verzauberung), which
            // its spelling resembles. Only the link names it.
            var german = new Glossary(new[]
            {
                Entry("Schlafend", "Die <b>Fähigkeiten</b> und <b>Verzauberungen</b> dieser Einheit sind deaktiviert.", "Begeisterung"),
                Entry("Verzauberung", "Wird ausgelöst, wenn du auf dieser Ebene einen Zauber spielst."),
                Entry("Begeisterung", "Andere Einheiten auf derselben Ebene erhalten einen Bonus."),
            });
            Assert.False(german.Inflected);
            Assert.Null(german.Resolve("Verzauberungen"));
            Assert.Equal(new[] { "Begeisterung" }, Names(german.Expand(new[] { "Legiertes Konstrukt" }, new[] { "Schlafend: deaktiviert." })));
        }

        [Fact]
        public void ADefinitionWithSourceTextIgnoresItsTranslationsBold()
        {
            // Traditional Chinese Dormant bolds 強化 for Enchant, though 強化 is Enhance's exact name.
            var chinese = new Glossary(new[]
            {
                Linked("休眠", "此單位的<b>能力</b>和<b>強化</b>無法使用。", "附魔"),
                Entry("強化", "Enhance."),
                Entry("附魔", "Enchant."),
            });
            Assert.Equal(new[] { "附魔" }, Names(chinese.Expand(new[] { "合金造物" }, new[] { "休眠: 無法使用。" })));
            Assert.Empty(new Glossary(new[] { Linked("休眠", "<b>強化</b>"), Entry("強化", "Enhance.") })
                .Expand(new[] { "合金造物" }, new[] { "休眠: 無法使用。" }));
        }

        [Fact]
        public void ATranslationsExactNamesStillCount()
        {
            // A token renders as the keyword's exact name, in any language: those need no link.
            var french = new Glossary(new[] { Entry("Armure", "Bloque les dégâts."), Entry("Bravoure", "Augmente l'<b>Armure</b>.") });
            Assert.Equal(new[] { "Bravoure", "Armure" }, Names(french.Expand(new[] { "Gagne <b>Bravoure</b> 1." }, new string[0])));
        }

        [Theory]
        [InlineData("Abilities", "Ability")]
        [InlineData("Enchants", "Enchant")]
        [InlineData("Smelted", "Smelt")]
        [InlineData("Advances", "Advance")]
        [InlineData("Forge Point", "Forge Points")]
        public void EnglishInflectedFormsMatchTheirEntry(string mention, string name)
            => Assert.True(Glossary.Inflects(mention, name));

        [Theory]
        [InlineData("Forge Points", "Forge")]
        [InlineData("Armor", "Armor")] // equal is an exact match, not an inflection
        [InlineData("Arm", "Armor")]
        [InlineData("Consumes", "Consumption")]
        [InlineData("触发效果", "触发")]
        public void OtherWordsDoNot(string mention, string name)
            => Assert.False(Glossary.Inflects(mention, name));

        [Fact]
        public void OnlyAnEnglishGlossaryMatchesInflections()
        {
            var entries = new[] { Entry("Ability", "a") };
            Assert.Equal("Ability", new Glossary(entries, inflect: true).Resolve("Abilities").Name);
            Assert.Null(new Glossary(entries).Resolve("Abilities"));
        }

        [Fact]
        public void AnInflectionMatchingTwoEntriesResolvesToTheCloser()
        {
            var glossary = new Glossary(new[] { Entry("Consumed", "a"), Entry("Consume", "b") }, inflect: true);
            Assert.Equal("Consume", glossary.Resolve("Consumes").Name);
            Assert.Equal("Consumed", glossary.Resolve("consumed").Name);
        }

        [Fact]
        public void AnInflectionEquallyCloseToTwoEntriesResolvesToNeither()
        {
            var glossary = new Glossary(new[] { Entry("Shields", "a"), Entry("Shielda", "b") }, inflect: true);
            Assert.Null(glossary.Resolve("Shieldx"));
        }

        [Fact]
        public void ANameClaimedTwiceKeepsTheFirstAndItsIdMeansTheKeptOne()
        {
            var glossary = new Glossary(new[] { Entry("Frozen", "status"), new GlossaryEntry("frozen", "test:trait", () => "trait", () => "frozen: trait") });
            Assert.Single(glossary.Entries);
            Assert.Equal("test:Frozen", glossary.Find("FROZEN").Id);
            Assert.Equal("test:trait", glossary.Duplicates.Single().Value.Id);
            Assert.Equal("test:Frozen", glossary.ById("test:trait").Id);
        }

        [Fact]
        public void WhatAnotherLineOfThePanelDefinesIsNotAddedAgain()
        {
            // An enemy's details, a node per line: "Spikes: ..." on one line, a mention of Spikes on another.
            var glossary = new Glossary(new[] { Entry("Spikes", "Deals damage to attackers."), Entry("Armor", "Blocks damage.") });
            var panel = new[] { "Spikes: Deals damage to attackers.", "Strike: Gain <b>Spikes</b> 2 and <b>Armor</b> 3." };
            Assert.Equal(new[] { "Armor" }, Names(glossary.Expand(new[] { panel[1] }, new string[0], panel)));
            Assert.Equal(new[] { "Spikes", "Armor" }, Names(glossary.Expand(new[] { panel[1] }, new string[0])));
        }

        [Fact]
        public void AReferenceToNoEntryIsIgnored()
            => Assert.Empty(new Glossary(new[] { Entry("Valor", "body", "Gone") }).Expand(new[] { "<b>Valor</b>" }, new[] { "Valor: shown" }));

        [Fact]
        public void ABodyThatThrowsIsLoggedAndSkipped()
        {
            var warnings = new List<string>();
            var saved = CoreLog.Warning;
            CoreLog.Warning = warnings.Add;
            try
            {
                var glossary = new Glossary(new[]
                {
                    new GlossaryEntry("Broken", "test:Broken", () => throw new InvalidOperationException("no data"), () => "Broken: ?"),
                    Entry("Armor", "Blocks damage."),
                });
                var added = glossary.Expand(new[] { "Gain <b>Broken</b> and <b>Armor</b>." }, new string[0]);
                Assert.Equal(new[] { "Broken", "Armor" }, Names(added));
                Assert.Contains(warnings, w => w.Contains("test:Broken") && w.Contains("no data"));
            }
            finally { CoreLog.Warning = saved; }
        }

        [Fact]
        public void TheBufferAppendsDefinitionsAfterTheGamesDetails()
        {
            var saved = NodeLines.Definitions;
            var glossary = English();
            NodeLines.Definitions = () => glossary;
            try
            {
                var b = new GraphBuilder();
                b.PushContext("Forge", "list");
                b.AddItem(ControlId.Structural("n0"), new NodeVtable
                {
                    ControlType = ControlTypes.Item,
                    Announcements = new[] { new NodeAnnouncement(() => "Unchained", kind: AnnouncementKinds.Label) },
                    Details = () => new[] { "Valor: +1 per stack. Raise Armor to stack count.", "Armor: shown already" },
                });
                var node = b.Build().Nodes[ControlId.Structural("n0")];
                // Armor is a detail's title, so it counts as shown; nothing is added.
                Assert.Equal(new[] { "Unchained", "Valor: +1 per stack. Raise Armor to stack count.", "Armor: shown already" },
                    NodeLines.Lines(node).ToArray());

                var b2 = new GraphBuilder();
                b2.PushContext("Forge", "list");
                b2.AddItem(ControlId.Structural("n0"), new NodeVtable
                {
                    ControlType = ControlTypes.Item,
                    Announcements = new[] { new NodeAnnouncement(() => "Unchained", kind: AnnouncementKinds.Label) },
                    Details = () => new[] { "Valor: +1 per stack. Raise Armor to stack count." },
                });
                var node2 = b2.Build().Nodes[ControlId.Structural("n0")];
                Assert.Equal(new[] { "Unchained", "Valor: +1 per stack. Raise Armor to stack count.", "Armor: Damage is dealt to Armor before health." },
                    NodeLines.Lines(node2).ToArray());
            }
            finally { NodeLines.Definitions = saved; }
        }
    }
}

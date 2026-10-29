using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using I2.Loc;
using MonsterTrainAccessibility.Core.Text;
using MonsterTrainAccessibility.Module.Nodes;
using MonsterTrainAccessibility.Module.UI;

namespace MonsterTrainAccessibility.Module.Dev
{
    /// <summary>
    /// Development only, run through the REPL (<c>MonsterTrainAccessibility.Module.Dev.GlossaryAudit.Run()</c>):
    /// how well <see cref="GameGlossary"/> covers the game's content, in the current language.
    /// <list type="number">
    /// <item>Completeness: every tooltip the game's own pipeline draws for every card (a borrowed card
    /// container, <c>SetSelectedCard</c>) and every artifact of every kind (<c>GetRelicTooltips</c>)
    /// must be a glossary entry, or the name of a card, unit, artifact or upgrade. Anything else is a
    /// definition the glossary has no source for.</item>
    /// <item>Linking: every bold mention in that content's text resolves to an entry, else is a name of
    /// content, else is listed. Mentions matched as inflections (English only) are listed too, to be
    /// checked by eye.</item>
    /// <item>Added to buffers: what the buffer would add for each card and artifact, as NodeLines asks.</item>
    /// <item>Definitions: how many link to others from their source text, and the bold this language's
    /// bodies carry that names no entry exactly: wording of its own, linked only where the source text
    /// links it (Spanish Burst's "Impactar", where the English says nothing of Strike).</item>
    /// </list>
    /// <see cref="RunAllLanguages"/> repeats the linking check in every game language through the game's
    /// own switch, and switches back.
    /// </summary>
    public static class GlossaryAudit
    {
        private sealed class Tally
        {
            public readonly Dictionary<string, int> Counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            public readonly Dictionary<string, string> Where = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            public void Add(string name, string where)
            {
                Counts.TryGetValue(name, out int n);
                Counts[name] = n + 1;
                if (!Where.ContainsKey(name)) Where[name] = where;
            }

            public string Top(int count) => string.Join("\n", Counts.OrderByDescending(p => p.Value).ThenBy(p => p.Key).Take(count)
                .Select(p => "  " + p.Key + " (" + p.Value + ", first in " + Where[p.Key] + ")"));
        }

        private sealed class Content
        {
            public readonly List<KeyValuePair<string, string>> Texts = new List<KeyValuePair<string, string>>();
            public readonly HashSet<string> Names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            public readonly Tally UnknownTitles = new Tally();
            public readonly Tally Added = new Tally();
            public int Titles, KnownTitles, NameTitles, Cards, Relics, Failures, Gaining;
            public readonly List<string> FailureNotes = new List<string>();

            public void Fail(string what, Exception e)
            {
                Failures++;
                if (FailureNotes.Count < 10) FailureNotes.Add(what + ": " + e.GetType().Name + " " + e.Message);
            }
        }

        public static string Run(int top = 40)
        {
            var glossary = GameGlossary.Current;
            var content = Collect(glossary);
            var report = new StringBuilder();
            report.AppendLine("Glossary audit, " + LocalizationManager.CurrentLanguageCode);
            report.AppendLine(glossary.Entries.Count + " entries: " + string.Join(", ", glossary.Entries
                .GroupBy(e => e.Id.Substring(0, e.Id.IndexOf(':'))).Select(g => g.Key + " " + g.Count())));
            var clashes = glossary.Duplicates.Where(d => Body(d.Key) != Body(d.Value)).ToList();
            report.AppendLine("names taken twice with different bodies (" + clashes.Count + "):");
            foreach (var d in clashes) report.AppendLine("  " + d.Key.Name + ": kept " + d.Key.Id + ", set aside " + d.Value.Id);

            report.AppendLine();
            report.AppendLine("1. Completeness: " + content.Cards + " cards, " + content.Relics + " artifacts, " + content.Titles + " tooltip titles; "
                + content.KnownTitles + " entries, " + content.NameTitles + " content names, " + content.UnknownTitles.Counts.Count + " distinct unknown");
            report.AppendLine(content.UnknownTitles.Top(top));
            if (content.Failures > 0)
            {
                report.AppendLine("  " + content.Failures + " items failed to draw:");
                foreach (var note in content.FailureNotes) report.AppendLine("    " + note);
            }

            report.AppendLine();
            Linking(glossary, content, report, top);
            return report.ToString();
        }

        /// <summary>The linking check in every game language, then back to the one in use.</summary>
        public static string RunAllLanguages(int top = 25)
        {
            var languages = UnityEngine.Object.FindObjectOfType<LanguageManager>();
            if (languages == null) return "no LanguageManager";
            string original = LocalizationManager.CurrentLanguageCode;
            var report = new StringBuilder();
            try
            {
                foreach (var code in LocalizationManager.GetAllLanguagesCode(true))
                {
                    languages.HandleLanguageChanged(code);
                    var glossary = GameGlossary.Current;
                    report.AppendLine("== " + LocalizationManager.CurrentLanguageCode + ": " + glossary.Entries.Count + " entries");
                    Linking(glossary, Collect(glossary), report, top);
                    report.AppendLine();
                }
            }
            finally { languages.HandleLanguageChanged(original); }
            return report.ToString();
        }

        private static void Linking(Glossary glossary, Content content, StringBuilder report, int top)
        {
            int exact = 0, names = 0;
            var inflected = new Tally();
            var unresolved = new Tally();
            foreach (var text in content.Texts)
            {
                foreach (var mention in Glossary.Mentions(text.Value))
                {
                    if (glossary.Find(mention) != null) exact++;
                    else if (glossary.Resolve(mention) is GlossaryEntry entry) inflected.Add(mention + " => " + entry.Name, text.Key);
                    else if (content.Names.Contains(mention)) names++;
                    else unresolved.Add(mention, text.Key);
                }
            }
            report.AppendLine("2. Linking: " + content.Texts.Count + " texts; mentions " + exact + " exact, "
                + inflected.Counts.Values.Sum() + " inflected, " + names + " content names, " + unresolved.Counts.Values.Sum() + " unresolved");
            report.AppendLine(" inflected (" + inflected.Counts.Count + " distinct):");
            report.AppendLine(inflected.Top(top));
            report.AppendLine(" unresolved (" + unresolved.Counts.Count + " distinct):");
            report.AppendLine(unresolved.Top(top));

            int linked = 0, links = 0;
            var own = new Tally();
            foreach (var entry in glossary.Entries)
            {
                if (entry.References != null && entry.References.Count > 0) { linked++; links += entry.References.Count; }
                foreach (var mention in Glossary.Mentions(Body(entry)))
                    if (glossary.Resolve(mention) == null && !content.Names.Contains(mention)) own.Add(mention, "definition " + entry.Name);
            }
            report.AppendLine("3. Added to buffers: " + content.Gaining + " of " + (content.Cards + content.Relics) + " items gain definitions, "
                + content.Added.Counts.Values.Sum() + " in all; most often:");
            report.AppendLine(content.Added.Top(Math.Min(top, 15)));
            report.AppendLine("4. Definitions: " + linked + " of " + glossary.Entries.Count + " link to others from their source text ("
                + links + " links); bold of this language's own naming no entry: " + own.Counts.Values.Sum());
            report.AppendLine(own.Top(top));
        }

        private static Content Collect(Glossary glossary)
        {
            var content = new Content();
            var managers = AllGameManagers.Instance;
            var data = managers.GetAllGameData();
            var save = managers.GetSaveManager();
            var statuses = managers.GetStatusEffectManager();
            var relics = managers.GetRelicManager();

            foreach (var card in data.GetAllCardData()) AddName(content, card.GetName());
            foreach (var unit in data.GetAllCharacterData()) AddName(content, unit.GetName());
            foreach (var upgrade in data.GetAllCardUpgradeData()) AddName(content, upgrade.GetUpgradeTitleKey().Localize());
            var relicData = new List<RelicData>();
            relicData.AddRange(data.GetAllCollectableRelicData());
            relicData.AddRange(data.GetAllEnhancerData());
            relicData.AddRange(data.GetAllSinsDatas());
            relicData.AddRange(data.GetAllMutatorData());
            relicData.AddRange(data.GetAllPyreArtifactDatas());
            relicData.AddRange(data.GetAllCovenantDatas());
            foreach (var relic in relicData) AddName(content, relic.GetName());

            Cards(glossary, content, data, save);

            var tooltips = new List<TooltipContent>();
            foreach (var relic in relicData)
            {
                try
                {
                    var state = new RelicState(relic);
                    TooltipGenerator.GetRelicTooltips(tooltips, state, statuses, relics, TooltipGenerator.OptionalFlags.MainItem, save);
                    content.Relics++;
                    string where = "artifact " + relic.GetName();
                    for (int i = 0; i < tooltips.Count; i++)
                    {
                        if (i > 0) Title(glossary, content, tooltips[i].title, where);
                        content.Texts.Add(new KeyValuePair<string, string>(where, tooltips[i].body));
                    }
                    Expand(glossary, content, where, new[] { relic.GetName() },
                        tooltips.Select(t => TooltipReader.Format(t.title, t.body)).Where(l => l != null).ToList());
                }
                catch (Exception e) { content.Fail("artifact " + relic.name, e); }
            }

            return content;
        }

        // Every card through a borrowed container, the way hover fills it; the container's own card is
        // put back afterwards.
        private static void Cards(Glossary glossary, Content content, AllGameData data, SaveManager save)
        {
            var owner = UnityEngine.Object.FindObjectsOfType<CardUI>(true).FirstOrDefault(c => c.cardTooltipsUI != null);
            if (owner == null)
            {
                content.FailureNotes.Add("no card container in the scene: cards not audited (open a screen with a card)");
                return;
            }
            var container = owner.cardTooltipsUI;
            try
            {
                foreach (var card in data.GetAllCardData())
                {
                    string where = "card " + card.GetName();
                    try
                    {
                        var state = new CardState(card, save);
                        container.SetSelectedCard(state, save);
                        content.Cards++;
                        content.Texts.Add(new KeyValuePair<string, string>(where, state.GetCardText(null)));
                        foreach (var tooltip in container.tooltipUIs)
                        {
                            if (tooltip == null || tooltip == container.secondaryTooltipPrompt) continue;
                            string title = tooltip.titleText != null && tooltip.titleText.gameObject.activeSelf ? tooltip.titleText.Content() : null;
                            string body = tooltip.bodyText != null && tooltip.bodyText.gameObject.activeSelf ? tooltip.bodyText.Content() : null;
                            Title(glossary, content, title, where);
                            content.Texts.Add(new KeyValuePair<string, string>(where, body));
                        }
                        Expand(glossary, content, where, new[] { state.GetTitle(), state.GetCardText(null) }, TooltipReader.Container(container));
                    }
                    catch (Exception e) { content.Fail(where, e); }
                }
            }
            finally
            {
                var own = owner.GetCardState();
                if (own != null) owner.UpdateTooltip(own);
                else container.DisableTooltips();
            }
        }

        // What the buffer would add for this item, as NodeLines asks it.
        private static void Expand(Glossary glossary, Content content, string where, IReadOnlyList<string> head, IReadOnlyList<string> details)
        {
            var added = glossary.Expand(head, details);
            if (added.Count > 0) content.Gaining++;
            foreach (var entry in added) content.Added.Add(entry.Name, where);
        }

        private static void Title(Glossary glossary, Content content, string title, string where)
        {
            string name = Glossary.Normalize(Interop.GameSprites.WithoutEchoedIcons(CardTextHelper.RemoveBold(title ?? "")));
            if (name.Length == 0) return;
            content.Titles++;
            if (glossary.Find(name) != null) content.KnownTitles++;
            else if (content.Names.Contains(name)) content.NameTitles++;
            else content.UnknownTitles.Add(name, where);
        }

        private static void AddName(Content content, string name)
        {
            string n = Glossary.Normalize(name);
            if (n.Length > 0) content.Names.Add(n);
        }

        private static string Body(GlossaryEntry entry)
        {
            try { return entry.Body(); }
            catch (Exception e) { return "(threw " + e.Message + ")"; }
        }
    }
}

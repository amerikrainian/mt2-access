using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using I2.Loc;
using MonsterTrainAccessibility.Contracts;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Core.Buffers;
using MonsterTrainAccessibility.Core.Text;
using MonsterTrainAccessibility.Core.UI;
using MonsterTrainAccessibility.Module.Nodes;
using MonsterTrainAccessibility.Module.UI;

namespace MonsterTrainAccessibility.Module.Dev
{
    /// <summary>
    /// Development only, run through the REPL or <c>tools/python/audit.py</c>: how the buffers read the
    /// game's content, every card (through a borrowed card container, <c>SetSelectedCard</c>, so a screen
    /// with a card must be open: the Logbook will do) and every artifact of every kind
    /// (<c>GetRelicTooltips</c>), in the current language.
    /// <list type="bullet">
    /// <item><see cref="Run"/>, the glossary (<see cref="GameGlossary"/>): (1) completeness, every
    /// tooltip title the game draws an entry or the name of a card, unit, artifact or upgrade, anything
    /// else a definition with no source; (2) linking, every bold mention resolved, else a content name,
    /// else listed, with the inflections (English only) listed to be checked by eye; (3) what each item's
    /// buffer gains; (4) the definitions' links, and the bold of this language's own that names no entry
    /// (wording only a translation has: Spanish Burst's "Impactar").
    /// <see cref="RunAllLanguages"/> repeats (2) to (4) in every language.</item>
    /// <item><see cref="Titles"/>: the tooltip lines as speech reads them: none may open by saying its
    /// name twice ("Spikes Spikes"); the titles still reading an icon are listed to be checked by eye.</item>
    /// <item><see cref="Compare"/>: what each item gains against the earlier rule (bold matched by
    /// spelling in every language, no links read from source text), per language.</item>
    /// <item><see cref="Screen"/>: the lines the glossary adds to each node of the screen open now.</item>
    /// </list>
    /// The language checks switch through the game's own <c>LanguageManager</c> and switch back.
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

            public int Total => Counts.Values.Sum();

            public string Top(int count) => string.Join("\n", Counts.OrderByDescending(p => p.Value).ThenBy(p => p.Key).Take(count)
                .Select(p => "  " + p.Key + " (" + p.Value + ", first in " + Where[p.Key] + ")"));
        }

        // A card or an artifact as its buffer reads it: its head (name, card text) and details (one line
        // per tooltip), and the tooltips as the game drew them (title and body), its own name's first for
        // an artifact.
        private sealed class Item
        {
            public string Where;
            public bool Card;
            public string Text;
            public IReadOnlyList<string> Head;
            public IReadOnlyList<string> Details;
            public List<KeyValuePair<string, string>> Drawn = new List<KeyValuePair<string, string>>();
        }

        private sealed class Content
        {
            public readonly List<KeyValuePair<string, string>> Texts = new List<KeyValuePair<string, string>>();
            public readonly HashSet<string> Names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            public readonly Tally UnknownTitles = new Tally();
            public readonly Tally Added = new Tally();
            public int Titles, KnownTitles, NameTitles, Cards, Relics, Gaining;
            public readonly List<string> Failures = new List<string>();
        }

        private static readonly Regex Doubled = new Regex(@"^(\p{L}[\p{L}'\-]*(?: \p{L}[\p{L}'\-]*)?) \1\b", RegexOptions.IgnoreCase);

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
            Failures(content.Failures, report);

            report.AppendLine();
            Linking(glossary, content, report, top);
            return report.ToString();
        }

        /// <summary>The linking checks in every game language, then back to the one in use.</summary>
        public static string RunAllLanguages(int top = 25)
            => InEachLanguage((code, report) =>
            {
                var glossary = GameGlossary.Current;
                report.AppendLine("== " + code + ": " + glossary.Entries.Count + " entries");
                Linking(glossary, Collect(glossary), report, top);
            });

        /// <summary>The tooltip lines as speech reads them, the glossary's own among them.</summary>
        public static string Titles(int top = 40)
        {
            var doubled = new Tally();
            var icons = new Tally();
            var failures = new List<string>();
            int lines = 0;
            void Check(string where, string line)
            {
                if (string.IsNullOrEmpty(line)) return;
                lines++;
                string said = Spoken(line);
                var m = Doubled.Match(said);
                if (m.Success) doubled.Add(said.Length > 90 ? said.Substring(0, 90) : said, where);
                int colon = line.IndexOf(": ", StringComparison.Ordinal);
                if (colon > 0 && line.Substring(0, colon).IndexOf("<sprite", StringComparison.OrdinalIgnoreCase) >= 0)
                    icons.Add(Spoken(line.Substring(0, colon)), where);
            }
            Items(item => { foreach (var line in item.Details) Check(item.Where, line); }, failures);
            foreach (var entry in GameGlossary.Current.Entries)
            {
                try { Check("definition " + entry.Name, entry.Line()); }
                catch (Exception e) { failures.Add("definition " + entry.Name + ": " + e.Message); }
            }
            var report = new StringBuilder();
            report.AppendLine("Titles, " + LocalizationManager.CurrentLanguageCode + ": " + lines + " lines read");
            report.AppendLine("opening with a name said twice (" + doubled.Total + "):");
            report.AppendLine(doubled.Top(top));
            report.AppendLine("titles still reading an icon (" + icons.Counts.Count + " distinct; the ability icon and a cost are meant):");
            report.AppendLine(icons.Top(top));
            Failures(failures, report);
            return report.ToString();
        }

        /// <summary>What each item gains now against the earlier rule, in the current language or every one.</summary>
        public static string Compare(bool allLanguages = false, int top = 10)
        {
            Action<string, StringBuilder> one = (code, report) =>
            {
                var now = GameGlossary.Current;
                var before = new Glossary(now.Entries.Select(e => new GlossaryEntry(e.Name, e.Id, e.Body, e.Line)), inflect: true);
                var lost = new Tally();
                var gained = new Tally();
                int was = 0, @is = 0;
                var failures = new List<string>();
                Items(item =>
                {
                    var a = before.Expand(item.Head, item.Details).Select(e => e.Name).ToList();
                    var b = now.Expand(item.Head, item.Details).Select(e => e.Name).ToList();
                    was += a.Count;
                    @is += b.Count;
                    foreach (var n in a.Except(b)) lost.Add(n, item.Where);
                    foreach (var n in b.Except(a)) gained.Add(n, item.Where);
                }, failures);
                report.AppendLine("== " + code + ": the earlier rule adds " + was + ", now " + @is);
                report.AppendLine(" only the earlier rule (" + lost.Total + "):");
                report.AppendLine(lost.Top(top));
                report.AppendLine(" only now (" + gained.Total + "):");
                report.AppendLine(gained.Top(top));
                Failures(failures, report);
            };
            if (allLanguages) return InEachLanguage(one);
            var single = new StringBuilder();
            one(LocalizationManager.CurrentLanguageCode, single);
            return single.ToString();
        }

        /// <summary>The lines the glossary adds to each node of the screen open now.</summary>
        public static string Screen()
        {
            var render = (Navigation.Active as GraphNavigator)?.CurrentRender;
            if (render == null) return "no screen is rendered";
            var saved = NodeLines.Definitions;
            var report = new StringBuilder();
            int nodes = 0, gaining = 0;
            try
            {
                foreach (var node in render.Nodes.Values)
                {
                    nodes++;
                    var with = NodeLines.Lines(node).ToList();
                    NodeLines.Definitions = null;
                    var without = NodeLines.Lines(node).ToList();
                    NodeLines.Definitions = saved;
                    if (with.Count <= without.Count) continue;
                    gaining++;
                    report.AppendLine(node.Id + " (" + (without.Count > 0 ? Spoken(without[0]) : "") + ")");
                    foreach (var line in with.Skip(without.Count)) report.AppendLine("  + " + Spoken(line));
                }
            }
            finally { NodeLines.Definitions = saved; }
            return nodes + " nodes, " + gaining + " gain definitions\n" + report;
        }

        private static string Spoken(string text) => TextFilter.Clean(Speech.IconWords(text));

        private static string InEachLanguage(Action<string, StringBuilder> check)
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
                    check(LocalizationManager.CurrentLanguageCode, report);
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
                + inflected.Total + " inflected, " + names + " content names, " + unresolved.Total + " unresolved");
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
                + content.Added.Total + " in all; most often:");
            report.AppendLine(content.Added.Top(Math.Min(top, 15)));
            report.AppendLine("4. Definitions: " + linked + " of " + glossary.Entries.Count + " link to others from their source text ("
                + links + " links); bold of this language's own naming no entry: " + own.Total);
            report.AppendLine(own.Top(top));
        }

        private static Content Collect(Glossary glossary)
        {
            var content = new Content();
            var data = AllGameManagers.Instance.GetAllGameData();
            foreach (var card in data.GetAllCardData()) AddName(content, card.GetName());
            foreach (var unit in data.GetAllCharacterData()) AddName(content, unit.GetName());
            foreach (var upgrade in data.GetAllCardUpgradeData()) AddName(content, Interop.GameText.TryTerm(upgrade.GetUpgradeTitleKey()));
            foreach (var relic in Relics(data)) AddName(content, relic.GetName());

            Items(item =>
            {
                if (item.Card) content.Cards++;
                else content.Relics++;
                if (item.Text != null) content.Texts.Add(new KeyValuePair<string, string>(item.Where, item.Text));
                for (int i = 0; i < item.Drawn.Count; i++)
                {
                    // An artifact's first tooltip is its own name and description.
                    if (item.Card || i > 0) Title(glossary, content, item.Drawn[i].Key, item.Where);
                    content.Texts.Add(new KeyValuePair<string, string>(item.Where, item.Drawn[i].Value));
                }
                var added = glossary.Expand(item.Head, item.Details);
                if (added.Count > 0) content.Gaining++;
                foreach (var entry in added) content.Added.Add(entry.Name, item.Where);
            }, content.Failures);
            return content;
        }

        private static List<RelicData> Relics(AllGameData data)
        {
            var relics = new List<RelicData>();
            relics.AddRange(data.GetAllCollectableRelicData());
            relics.AddRange(data.GetAllEnhancerData());
            relics.AddRange(data.GetAllSinsDatas());
            relics.AddRange(data.GetAllMutatorData());
            relics.AddRange(data.GetAllPyreArtifactDatas());
            relics.AddRange(data.GetAllCovenantDatas());
            return relics;
        }

        // Every card through a borrowed container, the way hover fills it (the container's own card put
        // back afterwards), then every artifact.
        private static void Items(Action<Item> each, List<string> failures)
        {
            var managers = AllGameManagers.Instance;
            var data = managers.GetAllGameData();
            var save = managers.GetSaveManager();
            var owner = UnityEngine.Object.FindObjectsOfType<CardUI>(true).FirstOrDefault(c => c.cardTooltipsUI != null);
            if (owner == null) failures.Add("no card container in the scene: cards not read (open a screen with a card: the Logbook will do)");
            else
            {
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
                            var item = new Item { Where = where, Card = true, Text = state.GetCardText(null) };
                            foreach (var tooltip in container.tooltipUIs)
                            {
                                if (tooltip == null || tooltip == container.secondaryTooltipPrompt) continue;
                                string title = tooltip.titleText != null && tooltip.titleText.gameObject.activeSelf ? tooltip.titleText.Content() : null;
                                string body = tooltip.bodyText != null && tooltip.bodyText.gameObject.activeSelf ? tooltip.bodyText.Content() : null;
                                item.Drawn.Add(new KeyValuePair<string, string>(title, body));
                            }
                            item.Head = new[] { state.GetTitle(), item.Text };
                            item.Details = TooltipReader.Container(container);
                            each(item);
                        }
                        catch (Exception e) { failures.Add(where + ": " + e.GetType().Name + " " + e.Message); }
                    }
                }
                finally
                {
                    var own = owner.GetCardState();
                    if (own != null) owner.UpdateTooltip(own);
                    else container.DisableTooltips();
                }
            }

            var tooltips = new List<TooltipContent>();
            foreach (var relic in Relics(data))
            {
                string where = "artifact " + relic.GetName();
                try
                {
                    var state = new RelicState(relic);
                    TooltipGenerator.GetRelicTooltips(tooltips, state, managers.GetStatusEffectManager(), managers.GetRelicManager(),
                        TooltipGenerator.OptionalFlags.MainItem, save);
                    var item = new Item { Where = where, Head = new[] { relic.GetName() }, Details = TooltipReader.Lines(tooltips) };
                    foreach (var tooltip in tooltips) item.Drawn.Add(new KeyValuePair<string, string>(tooltip.title, tooltip.body));
                    each(item);
                }
                catch (Exception e) { failures.Add(where + ": " + e.GetType().Name + " " + e.Message); }
            }
        }

        private static void Failures(List<string> failures, StringBuilder report)
        {
            if (failures.Count == 0) return;
            report.AppendLine("  " + failures.Count + " not read:");
            foreach (var note in failures.Take(10)) report.AppendLine("    " + note);
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

using System.Collections.Generic;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.Strings;
using MonsterTrainAccessibility.Core.UI;
using MonsterTrainAccessibility.Module.Interop;
using MonsterTrainAccessibility.Module.Nodes;
using MonsterTrainAccessibility.Module.UI;
using ShinyShoe;
using TMPro;
using UnityEngine;
using Screen = MonsterTrainAccessibility.Core.Screens.Screen;

namespace MonsterTrainAccessibility.Module.Screens
{
    /// <summary>
    /// The Logbook (<see cref="global::CompendiumScreen"/>, The Library), in four Tab-stops:
    /// <list type="bullet">
    /// <item>the sections as tabs, by the game's own section titles, landing on one opening it;</item>
    /// <item>the open section, as the game draws its current page: on Cards the filters (each option a
    /// button with "selected", the dropdowns combo boxes with pickers) then the page's cards
    /// (<see cref="CardNodes"/>: the drawn face, the tooltips in the buffer; landing on one hovers it
    /// as the mouse does) and locked cards; on Artifacts each collection with its artifacts (the
    /// game's tooltips, "???" and the lock reason for an undiscovered one); the other sections read
    /// from their drawn widgets and tooltips in the game's order;</item>
    /// <item>the page: "Page 1 of 57", then the game's Previous Page and Next Page while each turns,
    /// PageUp and PageDown turning from anywhere;</item>
    /// <item>Back.</item>
    /// </list>
    /// Escape is the game's: back to the Covenant Outpost.
    /// </summary>
    public sealed class CompendiumScreen : Screen
    {
        public const string PagePrev = "page.prev";
        public const string PageNext = "page.next";

        public CompendiumScreen()
        {
            Wrap = true;
        }

        public override string Key => "game.logbook";
        public override bool KeepStateOnPop => true;
        public override object InitialFocusStop => "sections";

        private static global::CompendiumScreen Game
            => GameScreens.Active<global::CompendiumScreen>(global::ScreenName.Compendium);

        public override bool IsActive() => Game != null;

        public override void Build(GraphBuilder b)
        {
            var screen = Game;
            if (screen == null) return;
            b.PushContext(GameText.Term("Compendium_Title"), Strings.RoleList, positions: false);

            b.BeginStop("sections");
            b.StartRow();
            foreach (var section in screen.sectionOrder)
            {
                if (!screen.tabsBySection.TryGetValue(section, out CompendiumTab tab) || tab == null || !tab.gameObject.activeInHierarchy) continue;
                var id = ControlId.Structural("logbook:tab:" + section);
                b.AddItem(id, Tab(screen, section, tab));
                if (section == screen.currentSection) b.SetStart(id); // the open section's tab, as the game shows it
            }
            b.EndRow();

            b.BeginStop("page");
            if (screen.pagesBySection.TryGetValue(screen.currentSection, out CompendiumSection page) && page != null && page.gameObject.activeInHierarchy)
            {
                if (page is CompendiumSectionCards cards) Cards(b, cards);
                else if (page is CompendiumSectionBlessings artifacts) Artifacts(b, artifacts);
                else Walk(b, page.transform, "logbook:" + screen.currentSection);
            }

            b.BeginStop("paging");
            if (page is PaginatedCompendiumSection paged && paged.pageCountLabel != null && paged.pageCountLabel.gameObject.activeInHierarchy)
                b.AddItem(ControlId.Structural("logbook:pagecount"), GameNodes.Text(() => paged.pageCountLabel.text));
            PageButton(b, screen, PageTurnZone.TurnDir.Left, "Leaderboard_Navigation_PreviousPage");
            PageButton(b, screen, PageTurnZone.TurnDir.Right, "Leaderboard_Navigation_NextPage");

            b.BeginStop("actions");
            var back = screen.backButton;
            if (back != null && back.gameObject.activeInHierarchy)
                b.AddItem(ControlId.Structural("logbook:back"), GameWidgets.Button(back, () => PauseMenuScreen.Caption(back)));
            b.PopContext();
        }

        public override IEnumerable<ElementAction> GetActions()
        {
            yield return new ElementAction(ActionIds.Back, Strings.Get("bind.ui.back"),
                _ => GameInput.Send(global::InputManager.Controls.Escape));
            var screen = Game;
            if (screen == null) yield break;
            var prev = Zone(screen, PageTurnZone.TurnDir.Left);
            if (prev != null)
                yield return new ElementAction(PagePrev, GameText.Term("Leaderboard_Navigation_PreviousPage"), _ => Turn(screen, prev));
            var next = Zone(screen, PageTurnZone.TurnDir.Right);
            if (next != null)
                yield return new ElementAction(PageNext, GameText.Term("Leaderboard_Navigation_NextPage"), _ => Turn(screen, next));
        }

        // A page turn through the game's own page control, then the game's new page line ("Page 2 of
        // 57") once it has redrawn it.
        private static void Turn(global::CompendiumScreen screen, PageTurnZone zone)
        {
            GameInput.Submit(zone);
            Later.Frames(2, () =>
            {
                if (screen.pagesBySection.TryGetValue(screen.currentSection, out CompendiumSection page)
                    && page is PaginatedCompendiumSection paged && paged.pageCountLabel != null
                    && !string.IsNullOrWhiteSpace(paged.pageCountLabel.text))
                    Core.Speech.Say(paged.pageCountLabel.text, interrupt: true);
            }, "logbook page");
        }

        // ---- sections ----

        private static NodeVtable Tab(global::CompendiumScreen screen, global::CompendiumScreen.Section section, CompendiumTab tab)
        {
            System.Func<string> name = () => GameText.Term(TitleKey(section));
            System.Action open = () =>
            {
                if (screen.currentSection != section) GameInput.Submit(tab.Button);
            };
            return new NodeVtable
            {
                ControlType = ControlTypes.Tab,
                Announcements = new List<NodeAnnouncement> { GameNodes.LabelPart(name) },
                SearchText = name,
                OnActivate = open,
                OnFocus = open,
            };
        }

        // The game's own title for each section (the tabs are icons).
        private static string TitleKey(global::CompendiumScreen.Section section)
        {
            switch (section)
            {
                case global::CompendiumScreen.Section.Checklist: return "Compendium_Section_Classes";
                case global::CompendiumScreen.Section.Cards: return "Compendium_Section_Cards";
                case global::CompendiumScreen.Section.Enemies: return "Compendium_Section_Enemies";
                case global::CompendiumScreen.Section.ChampUpgrades: return "Compendium_Title_ChampUpgrades";
                case global::CompendiumScreen.Section.Blessings: return "Compendium_Section_Blessings";
                case global::CompendiumScreen.Section.CardFrames: return "CompendiumCardFrames_Title";
                case global::CompendiumScreen.Section.Stats: return "Compendium_Section_ProgressRecord";
                default: return null;
            }
        }

        // ---- Cards ----

        private static void Cards(GraphBuilder b, CompendiumSectionCards cards)
        {
            var toolbar = cards.filterToolbar;
            if (toolbar != null)
            {
                Options(b, "clan", toolbar.classFilterUI);
                Dropdown(b, "type", toolbar.typeFilterUI);
                Options(b, "rarity", toolbar.rarityFilterUI);
                Options(b, "cost", toolbar.costFilterUI);
                Dropdown(b, "mastery", toolbar.masteryFilterUI);
                b.BeginStop("cards"); // the cards a Tab-stop of their own after the filters
            }
            b.PushContext(GameText.Term("Compendium_Section_Cards"), Strings.RoleList);
            foreach (var selectable in cards.cardSelectableUIs)
            {
                var widget = selectable?.component;
                if (widget == null || !widget.gameObject.activeInHierarchy) continue;
                var hovered = selectable;
                System.Action hover = () => GameInput.Hover(hovered);
                var card = selectable.GetUISubComponent<CardUI>() ?? widget.GetComponentInParent<CardUI>();
                if (card != null && card.GetCardState() != null)
                {
                    b.AddItem(ControlId.Structural("logbook:card:" + card.GetCardState().GetCardDataID()), CardNodes.Card(card, CardTooltipContainer.LoreTooltipSetting.Always, hover));
                    continue;
                }
                var locked = widget.GetComponent<LockedCardUI>() ?? widget.GetComponentInParent<LockedCardUI>();
                if (locked != null)
                    b.AddItem(ControlId.Structural("logbook:locked:" + locked.GetInstanceID()), CardNodes.Locked(locked, hover));
            }
            b.PopContext();
        }

        // An options filter (clan, rarity, cost): its label as the context, each option a button with
        // "selected", named by its tooltip where it is an icon. Enter toggles it through the game.
        private static void Options(GraphBuilder b, string key, OptionsFilterUI filter)
        {
            if (filter == null || !filter.gameObject.activeInHierarchy) return;
            b.PushContext(FilterLabel(filter), Strings.RoleList);
            for (int i = 0; i < filter.optionButtons.Count; i++)
            {
                var option = filter.optionButtons[i];
                var button = option != null ? option.GetComponent<GameUISelectableButton>() : null;
                if (button == null || !option.gameObject.activeInHierarchy) continue;
                int index = i;
                var o = option;
                b.AddItem(ControlId.Structural("logbook:filter:" + key + ":" + i), GameWidgets.Button(button,
                    () => OptionName(o), () => OptionDescription(o),
                    GameNodes.SelectedPart(() => filter.SelectedOption == index)));
            }
            b.PopContext();
        }

        private static void Dropdown(GraphBuilder b, string key, DropdownFilterUI filter)
        {
            if (filter == null || filter.dropdown == null || !filter.gameObject.activeInHierarchy) return;
            b.AddItem(ControlId.Structural("logbook:filter:" + key), GameWidgets.Dropdown(filter.dropdown, () => FilterLabel(filter)));
        }

        // A filter's caption ("Clan"): its first shown text that is not inside an option, the dropdown or
        // a key hint (it sits in the filter's header).
        private static string FilterLabel(Component filter)
        {
            foreach (var text in filter.GetComponentsInChildren<TMP_Text>(false))
            {
                if (string.IsNullOrWhiteSpace(text.text)) continue;
                if (text.GetComponentInParent<FilterOptionButton>() != null || text.GetComponentInParent<GameUISelectableDropdown>() != null
                    || text.GetComponentInParent<GameUIControlMapping>() != null) continue;
                return text.text;
            }
            return null;
        }

        // An option by its tooltip title (the clan icons, "Common" for "C"), else its drawn caption.
        private static string OptionName(FilterOptionButton option)
        {
            var tips = option.Tooltips;
            if (tips != null && tips.Count > 0 && !string.IsNullOrWhiteSpace(tips[0].title)) return CardTextHelper.RemoveBold(tips[0].title);
            var caption = option.label != null && option.label.gameObject.activeInHierarchy ? option.label.text : null;
            return !string.IsNullOrWhiteSpace(caption) ? caption : PauseMenuScreen.Caption(option);
        }

        private static string OptionDescription(FilterOptionButton option)
        {
            var tips = option.Tooltips;
            return tips != null && tips.Count > 0 && !string.IsNullOrWhiteSpace(tips[0].body) ? tips[0].body : null;
        }

        // ---- Artifacts ----

        private static void Artifacts(GraphBuilder b, CompendiumSectionBlessings artifacts)
        {
            if (artifacts.countLabel != null && artifacts.countLabel.gameObject.activeInHierarchy)
                b.AddItem(ControlId.Structural("logbook:artifacts:count"), GameNodes.Text(() => artifacts.countLabel.text));
            int c = 0;
            foreach (var collection in artifacts.collectionUIs)
            {
                if (collection == null || !collection.gameObject.activeInHierarchy) continue;
                int ci = c++;
                b.PushContext(Title(collection.tooltipProvider), Strings.RoleList);
                int r = 0;
                foreach (var relic in collection.relicUIs)
                {
                    if (relic == null || !relic.gameObject.activeInHierarchy) continue;
                    var artifact = relic;
                    b.AddItem(ControlId.Structural("logbook:artifact:" + ci + ":" + r++), new NodeVtable
                    {
                        ControlType = ControlTypes.Item,
                        Announcements = new List<NodeAnnouncement>
                        {
                            GameNodes.LabelPart(() => FirstTitle(artifact.Tooltips)),
                            GameNodes.TooltipPart(() => FirstBody(artifact.Tooltips)),
                        },
                        SearchText = () => FirstTitle(artifact.Tooltips),
                        OnFocus = () => GameInput.Hover(artifact.SelectableUI),
                        Details = () => GameNodes.Lines(TooltipReader.Lines(artifact)),
                    });
                }
                b.PopContext();
            }
        }

        private static string Title(ITooltipProvider provider)
            => provider != null ? FirstTitle(provider.Tooltips) : null;

        private static string FirstTitle(List<TooltipContent> tooltips)
            => tooltips != null && tooltips.Count > 0 ? CardTextHelper.RemoveBold(tooltips[0].title) : null;

        private static string FirstBody(List<TooltipContent> tooltips)
            => tooltips != null && tooltips.Count > 0 ? CardTextHelper.ScrubUpgradeHighlights(tooltips[0].body) : null;

        // ---- the other sections: what is drawn, in the game's order ----

        // A first pass for the sections without a reader of their own yet: every shown text and every
        // widget, in hierarchy order. A widget with a tooltip reads its title (else its caption) and
        // keeps the tooltips in the buffer; a button presses through the game; a dropdown or a toggle
        // is the shared widget; a card reads as a card.
        private static void Walk(GraphBuilder b, Transform t, string path)
        {
            if (!t.gameObject.activeInHierarchy) return;
            if (t.GetComponent<GameUIControlMapping>() != null) return; // a key hint
            string id = path + "/" + t.GetSiblingIndex();

            var card = t.GetComponent<CardUI>();
            if (card != null && card.GetCardState() != null)
            {
                b.AddItem(ControlId.Structural(id), CardNodes.Card(card, CardTooltipContainer.LoreTooltipSetting.Always, null));
                return;
            }
            var dropdown = t.GetComponent<GameUISelectableDropdown>();
            if (dropdown != null)
            {
                b.AddItem(ControlId.Structural(id), GameWidgets.Dropdown(dropdown, () => FilterLabel(t.parent != null ? t.parent : t)));
                return;
            }
            var toggle = t.GetComponent<GameUISelectableToggle>();
            if (toggle != null)
            {
                b.AddItem(ControlId.Structural(id), GameWidgets.Toggle(toggle, () => FilterLabel(t.parent != null ? t.parent : t)));
                return;
            }
            var provider = t.GetComponent<ITooltipProvider>();
            var button = t.GetComponent<GameUISelectableButton>();
            if (provider != null || button != null)
            {
                var widget = t;
                // A widget with a tooltip reads by it (its title, else its body; the body after a title):
                // what it means is there, while its drawn caption is often a bare number or an icon's.
                System.Func<string> label = () => TooltipLabel(widget.GetComponent<ITooltipProvider>()) ?? Caption(widget);
                System.Func<string> description = () => TooltipDescription(widget.GetComponent<ITooltipProvider>());
                if (label() != null || provider != null)
                {
                    var vt = button != null
                        ? GameWidgets.Button(button, label, description)
                        : new NodeVtable
                        {
                            ControlType = ControlTypes.Item,
                            Announcements = new List<NodeAnnouncement> { GameNodes.LabelPart(label), GameNodes.TooltipPart(description) },
                            SearchText = label,
                        };
                    var selectable = t.GetComponent<IGameUIComponent>();
                    if (selectable != null) vt.OnFocus = () => GameInput.Hover(selectable);
                    vt.Details = () => GameNodes.Lines(TooltipReader.Lines(widget));
                    b.AddItem(ControlId.Structural(id), vt);
                    return;
                }
            }
            var text = t.GetComponent<TMP_Text>();
            if (Spoken(text))
                b.AddItem(ControlId.Structural(id), GameNodes.Text(() => text.text));
            for (int i = 0; i < t.childCount; i++)
            {
                var child = t.GetChild(i);
                // A caption followed by its number ("Cards drawn", "359"; "Level", "10") is one line.
                var label = PlainText(child);
                var value = i + 1 < t.childCount ? PlainText(t.GetChild(i + 1)) : null;
                if (label != null && value != null && IsNumber(value.text))
                {
                    var l = label;
                    var v = value;
                    b.AddItem(ControlId.Structural(id + "/" + i), GameNodes.Text(() => l.text + ": " + v.text));
                    i++;
                    continue;
                }
                Walk(b, child, id);
            }
        }

        // A shown text with nothing beneath it: a bare caption or value.
        private static TMP_Text PlainText(Transform t)
        {
            if (!t.gameObject.activeInHierarchy || t.childCount > 0) return null;
            var text = t.GetComponent<TMP_Text>();
            return Spoken(text) ? text : null;
        }

        // A text that says something once its markup and invisible characters are gone.
        private static bool Spoken(TMP_Text text)
            => text != null && text.enabled && text.gameObject.activeInHierarchy
               && Contracts.TextFilter.Clean(text.text).Length > 0;

        // A count or amount as the game prints one: digits with separators, a fraction, a percentage.
        private static bool IsNumber(string text)
        {
            string clean = Contracts.TextFilter.Clean(text);
            if (clean.Length == 0) return false;
            bool digit = false;
            foreach (char c in clean)
            {
                if (char.IsDigit(c)) { digit = true; continue; }
                if (c == ',' || c == '.' || c == '/' || c == '%' || c == '+' || c == '-' || c == ' ' || c == ':' || c == '(' || c == ')') continue;
                return false;
            }
            return digit;
        }

        private static string TooltipLabel(ITooltipProvider provider)
        {
            var tips = provider?.Tooltips;
            if (tips == null || tips.Count == 0) return null;
            string title = CardTextHelper.RemoveBold(tips[0].title);
            return !string.IsNullOrWhiteSpace(title) ? title : CardTextHelper.ScrubUpgradeHighlights(tips[0].body);
        }

        private static string TooltipDescription(ITooltipProvider provider)
        {
            var tips = provider?.Tooltips;
            if (tips == null || tips.Count == 0 || string.IsNullOrWhiteSpace(tips[0].title)) return null;
            return CardTextHelper.ScrubUpgradeHighlights(tips[0].body);
        }

        // A widget's own shown texts, joined: its caption.
        private static string Caption(Transform widget)
        {
            var parts = new List<string>();
            foreach (var text in widget.GetComponentsInChildren<TMP_Text>(false))
            {
                if (string.IsNullOrWhiteSpace(text.text) || text.GetComponentInParent<GameUIControlMapping>() != null) continue;
                parts.Add(text.text.Trim());
            }
            return parts.Count > 0 ? string.Join(", ", parts) : null;
        }

        // ---- paging ----

        private static PageTurnZone Zone(global::CompendiumScreen screen, PageTurnZone.TurnDir dir)
        {
            foreach (var zone in screen.pageTurnZones)
                if (zone != null && zone.direction == dir && zone.enabled && zone.gameObject.activeInHierarchy) return zone;
            return null;
        }

        private static void PageButton(GraphBuilder b, global::CompendiumScreen screen, PageTurnZone.TurnDir dir, string term)
        {
            var zone = Zone(screen, dir);
            if (zone == null) return;
            b.AddItem(ControlId.Structural("logbook:page:" + dir), GameNodes.Button(() => GameText.Term(term), () => Turn(screen, zone)));
        }
    }
}

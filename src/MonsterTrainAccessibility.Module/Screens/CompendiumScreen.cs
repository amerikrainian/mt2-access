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
    /// The Logbook (<see cref="global::CompendiumScreen"/>, The Library). Tab-stops: the sections as
    /// tabs (by the game's own section titles; landing on one opens it), the open section's stops, the
    /// page ("Page 1 of 57", then the game's Previous Page and Next Page while each turns; PageUp and
    /// PageDown turn from anywhere), Back. Each section has its reader, in its own file:
    /// <list type="bullet">
    /// <item>Cards: the filters (<see cref="FilterNodes"/>), the search, then the page's cards
    /// (<see cref="CardNodes"/>; landing hovers one, as the mouse does);</item>
    /// <item>Enemies: the base game and Soul Savior lists as tabs with the list's filter, the search,
    /// the enemies (chosen by landing), then the chosen one's variants and details
    /// (<see cref="EnemyNodes"/>);</item>
    /// <item>Light Forge Upgrades: a row of clans and a row of champions (chosen by landing,
    /// <see cref="ClanNodes"/>), then the upgrade paths (<see cref="UpgradeTreeNodes"/>);</item>
    /// <item>Artifacts: each collection with its artifacts, by the game's tooltips;</item>
    /// <item>Progress Record: the record pages as tabs, then the open page: stat rows, or the friends'
    /// leaderboard with its sort choices;</item>
    /// <item>Clans and Mastery Card Frames: their panels as drawn, in the game's order, a clan's
    /// progress (<see cref="ClanNodes"/>) and a frame choice (<see cref="CardFrameNodes"/>) through
    /// their own nodes.</item>
    /// </list>
    /// A search field is edited with the game's own editor (<see cref="SearchField"/>). Escape is the
    /// game's: back to the Covenant Outpost.
    /// </summary>
    public sealed partial class CompendiumScreen : Screen
    {
        public const string PagePrev = "page.prev";
        public const string PageNext = "page.next";

        public CompendiumScreen()
        {
            Wrap = true;
        }

        // The card and enemy searches: each landing starts the game's own field editing.
        private readonly SearchField _cardSearch = new SearchField("logbook:cards:search");
        private readonly SearchField _enemySearch = new SearchField("logbook:enemies:search");

        public override string Key => "game.logbook";
        public override object InitialFocusStop => "sections";
        public override bool AllowsTypeahead => !_cardSearch.Focused && !_enemySearch.Focused;

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
                switch (page)
                {
                    case CompendiumSectionCards cards: Cards(b, cards); break;
                    case CompendiumSectionEnemies enemies: Enemies(b, enemies); break;
                    case CompendiumSectionChampUpgrades forge: LightForge(b, forge); break;
                    case CompendiumSectionBlessings artifacts: Artifacts(b, artifacts); break;
                    case CompendiumSectionStats stats: Stats(b, stats); break;
                    default: Walk(b, page.transform, "logbook:" + screen.currentSection, (page as PaginatedCompendiumSection)?.pageCountLabel?.transform); break;
                }
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

        public override void OnUpdate()
        {
            var screen = Game;
            CompendiumSection page = null;
            if (screen != null) screen.pagesBySection.TryGetValue(screen.currentSection, out page);
            _cardSearch.Update(page is CompendiumSectionCards cards && cards.filterToolbar != null ? FilterNodes.Field(cards.filterToolbar.searchFilterUI) : null);
            _enemySearch.Update(page is CompendiumSectionEnemies enemies ? FilterNodes.Field(enemies.searchFilterUI) : null);
        }

        public override void OnUnfocus()
        {
            _cardSearch.Release();
            _enemySearch.Release();
        }

        public override void OnPop() => OnUnfocus();

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

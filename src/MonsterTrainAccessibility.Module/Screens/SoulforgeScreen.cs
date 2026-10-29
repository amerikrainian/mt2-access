using System.Collections.Generic;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.Strings;
using MonsterTrainAccessibility.Core.UI;
using MonsterTrainAccessibility.Module.Interop;
using MonsterTrainAccessibility.Module.Nodes;
using MonsterTrainAccessibility.Module.UI;
using Screen = MonsterTrainAccessibility.Core.Screens.Screen;

namespace MonsterTrainAccessibility.Module.Screens
{
    /// <summary>
    /// The Soulforge, the Rift Station's soul collection (<see cref="global::SoulforgeScreen"/>), one
    /// Tab-stop, top to bottom: the soul types as a row of tabs (Unit, Spell, Global; landing on one opens
    /// it, and a vertical move into the row lands on the open one), the open type's souls as a row
    /// (<see cref="SoulNodes"/>; landing on one hovers it), Back. Escape is the game's: back to the Rift
    /// Station.
    /// </summary>
    public sealed class SoulforgeScreen : Screen
    {
        public override string Key => "game.soulforge";

        private static global::SoulforgeScreen Game
        {
            get
            {
                var screen = GameScreens.Active<global::SoulforgeScreen>(global::ScreenName.Soulforge);
                return screen != null && screen.IsInteractable() ? screen : null;
            }
        }

        public override bool IsActive() => Game != null;

        public override void Build(GraphBuilder b)
        {
            var screen = Game;
            if (screen == null) return;
            b.PushContext(ChallengeProgressScreen.HeaderOf(screen, "Text header"), Strings.RoleList, positions: false);

            var tabs = new List<SettingsTab>();
            if (screen.tabs != null)
                foreach (var tab in screen.tabs)
                    if (tab != null && tab.button != null && tab.button.gameObject.activeInHierarchy) tabs.Add(tab);
            if (tabs.Count > 0)
            {
                b.StartRow(entry: () => System.Math.Max(0, tabs.FindIndex(t => t.SectionRoot != null && t.SectionRoot.activeInHierarchy)));
                foreach (var tab in tabs) b.AddItem(ControlId.Structural("soulforge:tab:" + tab.gameObject.name), SettingsScreen.Tab(tab));
                b.EndRow();
            }

            var open = tabs.Find(t => t.SectionRoot != null && t.SectionRoot.activeInHierarchy);
            var souls = new List<SoulSelectionItemUI>();
            foreach (var item in Souls(screen))
                if (item != null && item.gameObject.activeInHierarchy && item.SoulData != null) souls.Add(item);
            ControlId first = null;
            if (souls.Count > 0)
            {
                b.PushContext(open != null ? PauseMenuScreen.Caption(open.button) : null, Strings.RoleList);
                b.StartRow();
                foreach (var soul in souls)
                {
                    var id = ControlId.Structural("soulforge:soul:" + soul.SoulData.GetID());
                    first = first ?? id;
                    b.AddItem(id, SoulNodes.Soul(soul, screen));
                }
                b.EndRow();
                b.PopContext();
            }

            var back = screen.backButton;
            if (back != null && back.gameObject.activeInHierarchy)
                b.AddItem(ControlId.Structural("soulforge:back"), GameWidgets.Button(back, () => PauseMenuScreen.Caption(back)));
            b.PopContext();
            // The game's own landing: the first soul.
            if (first != null) b.SetStart(first);
        }

        public override IEnumerable<ElementAction> GetActions()
        {
            yield return new ElementAction(ActionIds.Back, Strings.Get("bind.ui.back"),
                _ => GameInput.Send(global::InputManager.Controls.Escape));
        }

        // The open tab's souls, as the game lists them.
        private static List<SoulSelectionItemUI> Souls(global::SoulforgeScreen screen)
        {
            switch (screen.tabIndex)
            {
                case 0: return screen.unitSoulItems;
                case 1: return screen.spellSoulItems;
                case 2: return screen.artifactSoulItems;
                default: return new List<SoulSelectionItemUI>();
            }
        }
    }
}

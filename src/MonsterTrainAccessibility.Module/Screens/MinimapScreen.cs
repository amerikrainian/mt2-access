using System.Collections.Generic;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.Strings;
using MonsterTrainAccessibility.Core.UI;
using MonsterTrainAccessibility.Module.Interop;
using MonsterTrainAccessibility.Module.Nodes;
using MonsterTrainAccessibility.Module.UI;
using TMPro;
using Screen = MonsterTrainAccessibility.Core.Screens.Screen;

namespace MonsterTrainAccessibility.Module.Screens
{
    /// <summary>
    /// The whole run's map (<see cref="global::MinimapScreen"/>, the HUD's Show Map): opening it says
    /// the game's title ("Map of the Gates of Heaven"); then one list of the rings in order, each under
    /// its number and name ("1, Limbo"; "current" on the ring the train is in): its left branch's nodes
    /// as a row under the game's "Left", its right branch's under "Right", the node both share, a pact
    /// node, then the battle that ends the ring, each by its name and what it offers
    /// (<see cref="MapNodes.Marker"/>, <see cref="MapNodes.MinimapBattle"/>). Focus starts on the
    /// current ring. Escape is the game's: it closes the map.
    /// </summary>
    public sealed class MinimapScreen : Screen
    {
        public override string Key => "game.minimap";
        public override int Layer => 20;
        public override bool Exclusive => true;
        public override bool AllowsTypeahead => false;

        private static global::MinimapScreen Game => GameScreens.Active<global::MinimapScreen>(global::ScreenName.Minimap);

        public override bool IsActive() => Game != null;

        public override void OnPush()
        {
            var screen = Game;
            if (screen != null) ScreenIntro.Say(ChallengeProgressScreen.HeaderOf(screen, "Title label") ?? GameText.Term("ScreenMinimap_Title"));
        }

        public override void Build(GraphBuilder b)
        {
            var screen = Game;
            if (screen == null || screen.sections == null) return;
            var save = screen.saveManager;
            int current = save != null ? save.GetCurrentDistance() : -1;
            ControlId start = null;
            b.PushContext(null);
            for (int ring = 0; ring < screen.sections.Count; ring++)
            {
                var section = screen.sections[ring];
                if (section == null || !section.gameObject.activeInHierarchy) continue;
                int at = ring;
                string header = Header(section, at == current);
                b.PushContext(header);
                var first = Branch(b, "left:" + ring, GameText.Term("ScreenKeyMapping_Control_Left"), section.leftNodeMarkers);
                var right = Branch(b, "right:" + ring, GameText.Term("ScreenKeyMapping_Control_Right"), section.rightNodeMarkers);
                first = first ?? right;
                foreach (var single in new[] { section.centerNode, section.pactNode })
                    if (Shown(single))
                    {
                        var id = ControlId.Structural("minimap:" + single.name + ":" + ring);
                        b.AddItem(id, MapNodes.Marker(single));
                        first = first ?? id;
                    }
                if (section.battleNode != null && section.battleNode.gameObject.activeInHierarchy && TooltipReader.Lines(section.battleNode.tooltipProvider).Count > 0)
                {
                    var id = ControlId.Structural("minimap:battle:" + ring);
                    b.AddItem(id, MapNodes.MinimapBattle(section.battleNode));
                    first = first ?? id;
                }
                b.PopContext();
                // The last ring is the run's goal alone: no nodes, its heading the line.
                if (first == null) b.AddItem(first = ControlId.Structural("minimap:ring:" + ring), GameNodes.Text(() => header));
                if (at == current) start = first;
            }
            b.PopContext();
            if (start != null) b.SetStart(start);
        }

        public override IEnumerable<ElementAction> GetActions()
        {
            yield return new ElementAction(ActionIds.Back, Strings.Get("bind.ui.back"),
                _ => GameInput.Send(global::InputManager.Controls.Escape));
        }

        // A ring's heading as the game draws it: its number, its name; "current" where the train is.
        private static string Header(MinimapSection section, bool current)
        {
            var parts = new List<string>();
            foreach (var label in new TMP_Text[] { section.sectionNumberLabel, section.sectionNameLabel })
                if (label != null && label.gameObject.activeInHierarchy && !string.IsNullOrWhiteSpace(label.Content())) parts.Add(label.Content());
            if (current) parts.Add(Strings.StateCurrent);
            return parts.Count > 0 ? string.Join(", ", parts) : null;
        }

        // A branch's nodes as a row under its side's name; the first node's id, or null for none.
        private static ControlId Branch(GraphBuilder b, string key, string side, List<MinimapNodeMarker> markers)
        {
            var shown = new List<MinimapNodeMarker>();
            if (markers != null)
                foreach (var marker in markers)
                    if (Shown(marker)) shown.Add(marker);
            if (shown.Count == 0) return null;
            ControlId first = null;
            b.PushContext(side);
            b.StartRow();
            for (int i = 0; i < shown.Count; i++)
            {
                var id = ControlId.Structural("minimap:" + key + ":" + i);
                b.AddItem(id, MapNodes.Marker(shown[i]));
                first = first ?? id;
            }
            b.EndRow();
            b.PopContext();
            return first;
        }

        private static bool Shown(MinimapNodeMarker marker)
            => marker != null && marker.gameObject.activeInHierarchy && marker.mapNodeData != null;
    }
}

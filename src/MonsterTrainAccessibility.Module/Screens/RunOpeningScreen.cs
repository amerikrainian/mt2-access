using System.Collections.Generic;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.Input;
using MonsterTrainAccessibility.Core.UI;
using MonsterTrainAccessibility.Module.Interop;
using MonsterTrainAccessibility.Module.Nodes;
using MonsterTrainAccessibility.Module.UI;
using TMPro;
using Screen = MonsterTrainAccessibility.Core.Screens.Screen;

namespace MonsterTrainAccessibility.Module.Screens
{
    /// <summary>
    /// The run's opening briefing (<see cref="global::RunOpeningScreen"/>, over the map as a run
    /// begins), one list in the panel's order:
    /// <list type="bullet">
    /// <item>each boss as the game labels it ("Ring 3: The Fleshweaver", "Final Boss: Seraph
    /// Aeternus"), then its power, the second of its tooltips; all of them (its description, its power,
    /// the keywords it uses) in the buffer;</item>
    /// <item>the pyre heart: its header, its name, then what it does;</item>
    /// <item>the cards the run starts with beyond the deck (the Covenant's random cards) while the game
    /// shows them, as one row (left and right), each through <see cref="CardNodes"/>;</item>
    /// <item>OK.</item>
    /// </list>
    /// Focus lands on the first boss, so the briefing reads top to bottom. Enter on OK, or Escape, closes
    /// it through the game.
    /// </summary>
    public sealed class RunOpeningScreen : Screen
    {
        private static readonly InputCategory[] Categories = { InputCategory.Game, InputCategory.UI };

        public override string Key => "game.run_opening";
        public override int Layer => 5;
        public override bool Exclusive => true;
        // The run keys (Tab's HUD) live here as on the map.
        public override IReadOnlyList<InputCategory> InputCategories => Categories;

        // While it closes (its cards flying into the deck, its content fading) the game has let it go.
        private static global::RunOpeningScreen Game
        {
            get
            {
                var screen = GameScreens.Active<global::RunOpeningScreen>(global::ScreenName.RunOpening);
                return screen != null && screen.open ? screen : null;
            }
        }

        public override bool IsActive() => Game != null;

        public override void Build(GraphBuilder b)
        {
            var screen = Game;
            if (screen == null) return;
            b.PushContext(null);
            ControlId first = null;
            int i = 0;
            if (screen.bossDetailsUIs != null)
            {
                foreach (var boss in screen.bossDetailsUIs)
                {
                    if (boss == null || !boss.gameObject.activeInHierarchy) continue;
                    var id = ControlId.Structural("opening:boss:" + i++);
                    b.AddItem(id, Boss(boss));
                    if (first == null) first = id;
                }
            }
            PyreHeart(b, screen.pyreHeartInfoUI);
            if (screen.cardSectionRoot != null && screen.cardSectionRoot.activeInHierarchy && screen.cardUIs != null)
            {
                // One row, as the game lays them out: left and right walk the cards.
                b.StartRow();
                foreach (var card in screen.cardUIs)
                {
                    if (card == null || !card.gameObject.activeInHierarchy || card.GetCardState() == null) continue;
                    b.AddItem(ControlId.Structural("opening:card:" + card.GetCardState().GetCardDataID()),
                        CardNodes.Card(card, CardTooltipContainer.LoreTooltipSetting.Preferences, null));
                }
                b.EndRow();
            }
            var confirm = screen.confirmButton;
            if (confirm != null && confirm.gameObject.activeInHierarchy)
            {
                var id = ControlId.Structural("opening:ok");
                b.AddItem(id, GameWidgets.Button(confirm, () => PauseMenuScreen.Caption(confirm)));
                if (first == null) first = id;
            }
            if (first != null) b.SetStart(first);
            b.PopContext();
        }

        public override IEnumerable<ElementAction> GetActions()
        {
            var screen = Game;
            if (screen == null) yield break;
            yield return new ElementAction(ActionIds.Back, PauseMenuScreen.Caption(screen.confirmButton),
                _ => GameInput.Send(global::InputManager.Controls.Escape));
        }

        // A boss: its labels as drawn ("Ring 3:" and the name), then its power; its tooltips in the
        // buffer, the first of which is its name and description.
        private static NodeVtable Boss(BossDetailsUI boss)
        {
            System.Func<string> label = () =>
            {
                var texts = new List<string>();
                foreach (var text in boss.GetComponentsInChildren<TMP_Text>(false))
                    if (!string.IsNullOrWhiteSpace(text.Content())) texts.Add(text.Content().Trim());
                return texts.Count > 0 ? string.Join(" ", texts) : null;
            };
            return new NodeVtable
            {
                ControlType = ControlTypes.Text,
                Announcements = new List<NodeAnnouncement>
                {
                    GameNodes.LabelPart(label),
                    GameNodes.TooltipPart(() =>
                    {
                        var lines = TooltipReader.Lines(boss);
                        return lines.Count > 1 ? lines[1] : null;
                    }),
                },
                SearchText = label,
                Details = () => GameNodes.Lines(TooltipReader.Lines(boss)),
            };
        }

        // The pyre heart: the section's header, the heart's name, then the description the panel shows.
        private static void PyreHeart(GraphBuilder b, PyreHeartInfoUI pyre)
        {
            if (pyre == null || !pyre.gameObject.activeInHierarchy) return;
            System.Func<string> header = () =>
            {
                foreach (var text in pyre.GetComponentsInChildren<TMP_Text>(false))
                    if (text != pyre.titleLabel && text != pyre.externalDescriptionLabel && !string.IsNullOrWhiteSpace(text.Content())) return text.Content();
                return null;
            };
            b.AddItem(ControlId.Structural("opening:pyre"), new NodeVtable
            {
                ControlType = ControlTypes.Text,
                Announcements = new List<NodeAnnouncement>
                {
                    GameNodes.LabelPart(header),
                    new NodeAnnouncement(() => Shown(pyre.titleLabel), kind: AnnouncementKinds.Value),
                    GameNodes.TooltipPart(() => Shown(pyre.externalDescriptionLabel)),
                },
                SearchText = header,
            });
        }

        private static string Shown(TMP_Text text)
            => text != null && text.gameObject.activeInHierarchy && !string.IsNullOrWhiteSpace(text.Content()) ? text.Content() : null;
    }
}

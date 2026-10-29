using System.Collections.Generic;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.Input;
using MonsterTrainAccessibility.Core.Strings;
using MonsterTrainAccessibility.Core.UI;
using MonsterTrainAccessibility.Module.Interop;
using MonsterTrainAccessibility.Module.Nodes;
using MonsterTrainAccessibility.Module.UI;
using Screen = MonsterTrainAccessibility.Core.Screens.Screen;

namespace MonsterTrainAccessibility.Module.Screens
{
    /// <summary>
    /// The Light Forge (<see cref="global::ChampionUpgradeScreen"/>): the upgrades it offers side by
    /// side, as the game lays them out, each by its path's name and the champion's card as it would be
    /// with it (<see cref="CardNodes.Offer"/>); the card's tooltips in the buffer. Landing shows the
    /// choice as hovered, Enter takes it through the game. When the forge has nothing to offer, its
    /// button to leave. Escape is the game's: the Menu. The run keys (Tab's HUD) are live here.
    /// </summary>
    public sealed class ChampionUpgradeScreen : Screen
    {
        private static readonly InputCategory[] Categories = { InputCategory.Game, InputCategory.UI };

        public override string Key => "game.champion_upgrade";
        public override int Layer => 5;
        public override IReadOnlyList<InputCategory> InputCategories => Categories;

        private static global::ChampionUpgradeScreen Game
        {
            get
            {
                var screen = GameScreens.Active<global::ChampionUpgradeScreen>(global::ScreenName.ChampionUpgrade);
                return screen != null && screen.IsInteractable() ? screen : null;
            }
        }

        public override bool IsActive() => Game != null;

        public override void Build(GraphBuilder b)
        {
            var screen = Game;
            if (screen == null) return;
            b.PushContext(null);
            if (screen.hasOptions && screen.upgradeChoiceItems != null)
            {
                b.StartRow();
                int i = 0;
                foreach (var option in screen.upgradeChoiceItems)
                {
                    // Taking a choice, the game clears the others and turns the taken one's card off
                    // while it plays the upgrade: an option out of play is not declared, so nothing is
                    // left to read again until the forge closes or offers its next round.
                    if (option == null || !option.gameObject.activeInHierarchy || option.cardUI == null || option.upgradeData == null || !option.interactable) continue;
                    var choice = option;
                    b.AddItem(ControlId.Structural("forge:" + i++), CardNodes.Offer(
                        () => choice._upgradeNameLabel != null ? choice._upgradeNameLabel.text : null,
                        choice.cardUI, CardTooltipContainer.LoreTooltipSetting.Never,
                        () => GameInput.Hover(choice.SelectableUI),
                        () => GameInput.Submit(choice.SelectableUI)));
                }
                b.EndRow();
            }
            var leave = screen.noUpgradesButton;
            if (leave != null && leave.gameObject.activeInHierarchy && PauseMenuScreen.Caption(leave) != null)
                b.AddItem(ControlId.Structural("forge:leave"), GameWidgets.Button(leave, () => PauseMenuScreen.Caption(leave)));
            b.PopContext();
        }

        public override IEnumerable<ElementAction> GetActions()
        {
            yield return new ElementAction(ActionIds.Back, Strings.Get("bind.ui.back"),
                _ => GameInput.Send(global::InputManager.Controls.Escape));
        }
    }
}

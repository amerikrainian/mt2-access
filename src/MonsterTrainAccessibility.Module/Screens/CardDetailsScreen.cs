using System.Collections.Generic;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.UI;
using MonsterTrainAccessibility.Module.Interop;
using MonsterTrainAccessibility.Module.Nodes;
using MonsterTrainAccessibility.Module.UI;
using Screen = MonsterTrainAccessibility.Core.Screens.Screen;

namespace MonsterTrainAccessibility.Module.Screens
{
    /// <summary>
    /// A card shown large (<see cref="global::CardDetailsScreen"/>, the game's card preview key or a
    /// click on "Card Details"): the card through <see cref="CardNodes"/>, its tooltips all in the buffer
    /// (the screen shows them all); Next while the screen holds several cards, Enter on the card doing the
    /// same, as the game's Submit does; the artist ("Artist: name") where the card credits one. Escape is
    /// the game's: it closes the screen.
    /// </summary>
    public sealed class CardDetailsScreen : Screen
    {
        public override string Key => "game.card_details";
        public override int Layer => 20;
        public override bool Exclusive => true;

        private static global::CardDetailsScreen Game => GameScreens.Active<global::CardDetailsScreen>(global::ScreenName.CardDetails);

        public override bool IsActive() => Game != null;

        public override void Build(GraphBuilder b)
        {
            var screen = Game;
            var card = screen != null ? screen.cardUI : null;
            if (card == null || !card.gameObject.activeInHierarchy || card.GetCardState() == null) return;
            b.PushContext(null);
            var vt = CardNodes.Card(card, CardTooltipContainer.LoreTooltipSetting.Always, null);
            var next = screen.nextButton;
            bool several = next != null && next.gameObject.activeInHierarchy;
            if (several) vt.OnActivate = screen.ShowNextCard;
            // By the card shown, so the next one is read as it comes.
            var id = ControlId.Structural("details:card:" + screen.selectedCardStateIndex);
            b.AddItem(id, vt);
            if (several) b.AddItem(ControlId.Structural("details:next"), GameWidgets.Button(next, () => PauseMenuScreen.Caption(next)));
            if (screen.artistAttribution != null && screen.artistAttribution.activeInHierarchy && screen.artistName != null
                && !string.IsNullOrWhiteSpace(screen.artistName.Content()))
                b.AddItem(ControlId.Structural("details:artist"), GameNodes.Text(() =>
                    string.Format(GameText.Term("TextFormat_Colon"), GameText.Term("CardDetailsScreen_Artist"), screen.artistName.Content())));
            b.PopContext();
            b.SetStart(id);
        }

        public override IEnumerable<ElementAction> GetActions()
        {
            yield return new ElementAction(ActionIds.Back, Core.Strings.Strings.Get("bind.ui.back"),
                _ => GameInput.Send(global::InputManager.Controls.Escape));
        }
    }
}

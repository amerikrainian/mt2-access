using System.Collections.Generic;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.Input;
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
    /// A pile of cards (<see cref="global::DeckScreen"/>): the deck, a battle's draw, discard and
    /// consumed piles, and the deck in the modes that pick from it (purge, upgrade, duplicate...).
    /// Under the pile's own banner ("Deck"), or, in a mode that picks, after the game's instructions
    /// spoken as it opens ("Purge Card. Remove a card from your deck."), the cards
    /// as the grid the game draws (its own column count; Up and Down keep the column, and go nowhere
    /// from a column the next row does not reach), each through <see cref="CardNodes"/>, "selected"
    /// where the mode picks; then sort, confirm and cancel while the game shows them, or its message
    /// that there are no cards. Landing on a card focuses it as the pointer does; Enter, where the mode
    /// picks, selects it through the game. Escape is the game's: it closes the pile.
    /// </summary>
    public sealed class DeckScreen : Screen
    {
        private static readonly InputCategory[] Categories = { InputCategory.Game, InputCategory.UI };

        public override string Key => "game.deck";
        public override int Layer => 10;
        public override bool Exclusive => true;
        public override IReadOnlyList<InputCategory> InputCategories => Categories;

        private static global::DeckScreen Game
        {
            get
            {
                var screen = GameScreens.Active<global::DeckScreen>(global::ScreenName.Deck);
                return screen != null && screen.IsInteractable() ? screen : null;
            }
        }

        public override bool IsActive() => Game != null;

        private bool _prompted;

        public override void OnPush() => _prompted = false;

        public override void Build(GraphBuilder b)
        {
            var screen = Game;
            if (screen == null || !Dealt(screen)) return;
            // A mode that picks from the deck says what it wants as it opens ("Purge Card. Remove a card
            // from your deck."), not as items: the list starts on the cards.
            bool instructed = screen.instructionsRoot != null && screen.instructionsRoot.activeInHierarchy
                && Shown(screen.instructionsTitleLabel) != null;
            if (instructed && !_prompted)
            {
                _prompted = true;
                ScreenIntro.Say(Shown(screen.instructionsTitleLabel), Shown(screen.instructionsDescriptionLabel));
            }
            if (instructed) b.PushContext(null);
            else b.PushContext(Banner(screen), Strings.RoleList);
            Cards(b, screen);
            if (screen.sortDropdownRoot != null && screen.sortDropdownRoot.gameObject.activeInHierarchy && screen.sortDropdown != null)
                b.AddItem(ControlId.Structural("deck:sort"), GameWidgets.Dropdown(screen.sortDropdown, () => PauseMenuScreen.Caption(screen.sortDropdownRoot)));
            if (screen.selectionConfirmRoot != null && screen.selectionConfirmRoot.gameObject.activeInHierarchy)
                Button(b, "confirm", screen.confirmButton, () => Shown(screen.selectionConfirmButtonLabel));
            Button(b, "cancel", screen.cancelButton, () => PauseMenuScreen.Caption(screen.cancelButton));
            Text(b, "empty", screen.noCardsMessage);
            b.PopContext();
        }

        public override IEnumerable<ElementAction> GetActions()
        {
            yield return new ElementAction(ActionIds.Back, Strings.Get("bind.ui.back"),
                _ => GameInput.Send(global::InputManager.Controls.Escape));
        }

        // The cards, row by row as the grid lays them out.
        private static void Cards(GraphBuilder b, global::DeckScreen screen)
        {
            var cards = screen.cardInfos;
            if (cards == null || cards.Count == 0) return;
            int columns = Mathf.Max(1, screen.columnCount);
            bool picks = screen.ModeAllowsSelect();
            bool open = false;
            int column = 0;
            for (int i = 0; i < cards.Count; i++)
            {
                var info = cards[i];
                var card = info != null ? info.cardUI : null;
                if (card == null || info.purged || !card.gameObject.activeInHierarchy || card.GetCardState() == null) continue;
                if (!open)
                {
                    b.StartRow("deck:grid", strictColumns: true);
                    open = true;
                }
                int index = i;
                var vt = CardNodes.Card(card, CardTooltipContainer.LoreTooltipSetting.Preferences, () => GameInput.Hover(card.SelectableUI));
                if (picks)
                {
                    // Where the game selects the card in focus (a purge), "selected" would only echo the
                    // landing: said only where picking is apart from focusing.
                    if (!screen.ShouldSelectOnFocus())
                        vt.Announcements = new List<NodeAnnouncement>(vt.Announcements) { GameNodes.SelectedPart(() => screen._selectedIndices.Contains(index)) };
                    vt.OnActivate = () => GameInput.Submit(card.SelectableUI);
                }
                b.AddItem(ControlId.Structural("deck:card:" + i), vt);
                if (++column == columns)
                {
                    b.EndRow();
                    open = false;
                    column = 0;
                }
            }
            if (open) b.EndRow();
        }

        // The game deals the cards in a column at a time, each blank until it is set up: nothing on the
        // screen is declared until all are in (the game's own test before it shows the sort), so the
        // first landing reads a whole card, not the buttons under a grid still forming.
        private static bool Dealt(global::DeckScreen screen)
        {
            var cards = screen.cardInfos;
            if (cards == null) return true;
            foreach (var info in cards)
                if (info != null && !info.purged && !info.isDoneAnimatingIn) return false;
            return true;
        }

        // The pile's name, off the banner the mode shows ("Deck", "Draw Pile").
        private static string Banner(global::DeckScreen screen)
        {
            foreach (var banner in new[] { screen.deckBanner, screen.drawPileBanner, screen.discardPileBanner, screen.exhaustedPileBanner, screen.eatenPileBanner })
                if (banner != null && banner.activeInHierarchy) return PauseMenuScreen.Caption(banner.transform);
            return null;
        }

        private static void Text(GraphBuilder b, string key, TMP_Text text)
        {
            if (Shown(text) == null) return;
            b.AddItem(ControlId.Structural("deck:" + key), GameNodes.Text(() => Shown(text)));
        }

        private static void Button(GraphBuilder b, string key, GameUISelectableButton button, System.Func<string> label)
        {
            if (button == null || !button.gameObject.activeInHierarchy || label() == null) return;
            b.AddItem(ControlId.Structural("deck:" + key), GameWidgets.Button(button, label));
        }

        private static string Shown(TMP_Text text)
            => text != null && text.gameObject.activeInHierarchy && !string.IsNullOrWhiteSpace(text.Content()) ? text.Content() : null;
    }
}

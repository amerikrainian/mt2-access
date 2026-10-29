using System.Collections.Generic;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.UI;
using MonsterTrainAccessibility.Module.Nodes;
using MonsterTrainAccessibility.Module.UI;
using ShinyShoe;
using UnityEngine;

namespace MonsterTrainAccessibility.Module.Screens
{
    // The game asking for a card from the hand (Judge's Scales' discards, a card to copy): its
    // instructions are said once as it opens; then the hand, the card chosen marked selected (Enter on
    // a card chooses it, again drops it, as the game's own choosing does), then the game's buttons
    // (Confirm once a card is chosen, Cancel or the game's own word for being done, Hide). Choosing a
    // card lands on Confirm when the game shows it. Hidden, the battle is back with the game's Show in
    // the top panel.
    public sealed partial class BattleScreen
    {
        private bool _selectionPrompted;

        private static HandSelectionUI Selection => HandUi != null ? HandUi.handSelectionUI : null;

        /// <summary>The game is asking for a card from the hand and showing its prompt.</summary>
        private static bool Selecting
        {
            get
            {
                var selection = Selection;
                return selection != null && selection.Active && !selection.Hidden;
            }
        }

        private void BuildHandSelection(GraphBuilder b, HandSelectionUI selection)
        {
            if (!_selectionPrompted)
            {
                _selectionPrompted = true;
                ScreenIntro.Say(Shown(selection.instructionsDescriptionLabel));
            }
            Hand(b);
            var items = new List<KeyValuePair<ControlId, NodeVtable>>();
            SelectionButton(items, "confirm", selection, selection.confirmButton, selection.confirmButtonLabel);
            SelectionButton(items, "cancel", selection, selection.cancelButton, selection.cancelButtonLabel);
            SelectionButton(items, "hide", selection, selection.hideButton, selection.hideButtonLabel);
            Row(b, "selection", null, items);
        }

        private static void SelectionButton(List<KeyValuePair<ControlId, NodeVtable>> items, string key, HandSelectionUI selection, GameUISelectableButton button, TMPro.TMP_Text label)
        {
            if (button == null || !button.gameObject.activeInHierarchy) return;
            var vt = GameWidgets.Button(button, () => Shown(label));
            vt.Details = () => GameNodes.Lines(Shown(selection.instructionsDescriptionLabel));
            items.Add(Item("selection:" + key, vt));
        }

        // Whether a card in hand is the one chosen.
        private static bool ChosenInSelection(CardUI card)
        {
            var selection = Selection;
            return selection != null && selection.Active && selection.CurrentlySelectedCardUI == card;
        }

        // A card chosen: Confirm, once the game has shown it (after the card's move out of the hand).
        private static void LandOnConfirm(int tries = 60)
        {
            Later.Frames(1, () =>
            {
                var selection = Selection;
                if (selection == null || !selection.Active || selection.CurrentlySelectedCardUI == null) return;
                if (selection.confirmButton != null && selection.confirmButton.gameObject.activeInHierarchy)
                    Navigation.MoveTo(ControlId.Structural("selection:confirm"));
                else if (tries > 0) LandOnConfirm(tries - 1);
            }, "battle: land on confirm");
        }

        private static string Shown(TMPro.TMP_Text text)
        {
            string content = text != null && text.gameObject.activeInHierarchy ? text.Content() : null;
            return string.IsNullOrWhiteSpace(content) ? null : content.Trim();
        }

        private static bool Shown(Component widget) => widget != null && widget.gameObject.activeInHierarchy;
    }
}

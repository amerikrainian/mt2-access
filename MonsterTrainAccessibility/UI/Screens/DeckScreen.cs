using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Speech;
using MonsterTrainAccessibility.UI.Elements;
using ShinyShoe;
using TMPro;
using UnityEngine.UI;

namespace MonsterTrainAccessibility.UI.Screens
{
    internal sealed class DeckScreen : GridNavigationGameScreen
    {
        private static readonly FieldInfo CardGridField = AccessTools.Field(typeof(global::DeckScreen), "cardGrid")!;
        private static readonly FieldInfo NoCardsMessageField = AccessTools.Field(typeof(global::DeckScreen), "noCardsMessage")!;
        private static readonly FieldInfo InstructionsTitleLabelField = AccessTools.Field(typeof(global::DeckScreen), "instructionsTitleLabel")!;
        private static readonly FieldInfo InstructionsDescriptionLabelField = AccessTools.Field(typeof(global::DeckScreen), "instructionsDescriptionLabel")!;
        private static readonly FieldInfo CardInfosField = AccessTools.Field(typeof(global::DeckScreen), "cardInfos")!;
        private static readonly FieldInfo ModeField = AccessTools.Field(typeof(global::DeckScreen), "mode")!;
        private static readonly FieldInfo RelicDataField = AccessTools.Field(typeof(global::DeckScreen), "relicData")!;
        private static readonly FieldInfo CardInfoCardUIField = AccessTools.Field(typeof(global::DeckScreen.CardInfo), "cardUI")!;
        private static readonly FieldInfo CardInfoCardStateField = AccessTools.Field(typeof(global::DeckScreen.CardInfo), "cardState")!;
        private static readonly FieldInfo CardInfoIsDoneAnimatingInField = AccessTools.Field(typeof(global::DeckScreen.CardInfo), "isDoneAnimatingIn")!;

        private readonly global::DeckScreen _screen;
        private bool _soulEquipAnnounced;

        public DeckScreen(global::DeckScreen screen)
        {
            _screen = screen;
            ClaimGridMovementActions();
        }

        public override void OnPush()
        {
            _soulEquipAnnounced = false;
            base.OnPush();
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            AnnounceSoulEquipScreen();
        }

        protected override void PopulateGrid()
        {
            int columns = GetColumnCount();
            int count = 0;
            List<global::CardUI> cards = new List<global::CardUI>(GetCards());

            TMP_Text instructionsTitle = Get<TMP_Text>(_screen, InstructionsTitleLabelField);
            TMP_Text instructionsDescription = Get<TMP_Text>(_screen, InstructionsDescriptionLabelField);
            if (cards.Count == 0 &&
                (!string.IsNullOrWhiteSpace(AccessibleScreenText.Text(instructionsTitle)?.Resolve()) ||
                !string.IsNullOrWhiteSpace(AccessibleScreenText.Text(instructionsDescription)?.Resolve()))
            )
            {
                ProxyDeckInstructions instructions = new ProxyDeckInstructions(instructionsTitle, instructionsDescription, _screen.gameObject);
                Grid.Add(instructions, 0, count / columns);
                RegisterElement(instructions, instructionsTitle != null ? instructionsTitle.gameObject : null);
                count++;
            }

            for (int i = 0; i < cards.Count; i++)
            {
                global::CardUI cardUI = cards[i];
                IGameUIComponent selectable = cardUI.SelectableUI;
                if (selectable == null)
                {
                    continue;
                }

                ProxyCombatCard element = new ProxyCombatCard(
                    cardUI,
                    selectable,
                    bufferBottomParts: InscryptionPathDescriber.VisibleDeckBufferBottomParts);
                Grid.Add(element, count % columns, count / columns);
                RegisterElement(element, cardUI.gameObject, selectable.component != null ? selectable.component.gameObject : null);
                count++;
            }

            TMP_Text noCards = Get<TMP_Text>(_screen, NoCardsMessageField);
            if (count == 0 && noCards != null)
            {
                ProxyDeckEmptyMessage empty = new ProxyDeckEmptyMessage(noCards);
                Grid.Add(empty, 0, 0);
                RegisterElement(empty, noCards.gameObject);
                count++;
            }

            if (IsSoulEquipMode(out _))
            {
                int buttonRow = count / columns;
                int buttonColumn = count % columns;
                AddButton(_screen.GetCloseButton(), ref buttonColumn, ref buttonRow, columns);
            }
        }

        public override bool ShouldRestoreNavigationFocus()
        {
            return HasStableNativeDefaultSelectable();
        }

        protected override bool ShouldFocusFirstOnPush()
        {
            return HasStableNativeDefaultSelectable();
        }

        protected override string BuildSignature()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (global::DeckScreen.CardInfo cardInfo in GetCardInfos())
            {
                global::CardUI cardUI = Get<global::CardUI>(cardInfo, CardInfoCardUIField);
                if (cardUI == null || !cardUI.gameObject.activeInHierarchy)
                {
                    continue;
                }

                sb.Append(cardUI.GetInstanceID()).Append(':').Append(cardUI.GetCardState()?.GetID()).Append('|');
                sb.Append(IsDoneAnimatingIn(cardInfo)).Append('|');
            }
            global::ShinyShoe.GameUISelectableButton confirm = _screen.GetConfirmButton();
            global::ShinyShoe.GameUISelectableButton close = _screen.GetCloseButton();
            sb.Append(";confirm:").Append(confirm != null && confirm.gameObject.activeInHierarchy);
            sb.Append(";close:").Append(close != null && close.gameObject.activeInHierarchy);
            return sb.ToString();
        }

        private IEnumerable<global::CardUI> GetCards()
        {
            foreach (global::DeckScreen.CardInfo cardInfo in GetCardInfos())
            {
                global::CardUI cardUI = Get<global::CardUI>(cardInfo, CardInfoCardUIField);
                if (cardUI != null && cardUI.gameObject.activeInHierarchy)
                {
                    yield return cardUI;
                }
            }
        }

        private void AnnounceSoulEquipScreen()
        {
            if (_soulEquipAnnounced || !IsSoulEquipMode(out SoulData soul))
            {
                return;
            }

            _soulEquipAnnounced = true;
            SpeechManager.Output(Message.Localized("ui", "DECK.EQUIPPING_SOUL", new { soul = soul.GetName() }));
        }

        private bool IsSoulEquipMode(out SoulData soul)
        {
            soul = Get<RelicData>(_screen, RelicDataField) as SoulData;
            object mode = ModeField.GetValue(_screen);
            return soul != null &&
                mode is global::DeckScreen.Mode deckMode &&
                deckMode == global::DeckScreen.Mode.ApplyUpgrade;
        }

        private int GetColumnCount()
        {
            GridLayoutGroup grid = Get<GridLayoutGroup>(_screen, CardGridField);
            if (grid != null && grid.constraint == GridLayoutGroup.Constraint.FixedColumnCount && grid.constraintCount > 0)
            {
                return grid.constraintCount;
            }
            return 4;
        }

        private void AddButton(GameUISelectableButton button, ref int x, ref int y, int columns)
        {
            if (button == null)
            {
                return;
            }

            if (x >= columns)
            {
                x = 0;
                y++;
            }

            LabeledButton element = new LabeledButton(button, () => DeckButtonLabel(button));
            Grid.Add(element, x, y);
            RegisterElement(element, button.gameObject);
            x++;
        }

        private Message DeckButtonLabel(GameUISelectableButton button)
        {
            string label = AccessibleScreenText.ReadButtonLabel(button);
            if (!string.IsNullOrWhiteSpace(Message.Clean(label)))
            {
                return Message.FromText(label);
            }

            if (button == _screen.GetCloseButton() && IsSoulEquipMode(out _))
            {
                return Message.Localized("ui", "DIALOGUE.SKIP");
            }

            return null;
        }

        private bool HasStableNativeDefaultSelectable()
        {
            List<global::DeckScreen.CardInfo> cardInfos = Get<List<global::DeckScreen.CardInfo>>(_screen, CardInfosField);
            if (cardInfos != null && cardInfos.Count > 0)
            {
                global::DeckScreen.CardInfo firstCardInfo = cardInfos[0];
                global::CardState cardState = Get<global::CardState>(firstCardInfo, CardInfoCardStateField);
                if (cardState != null && !cardState.IsCurrentlyDisabled())
                {
                    global::CardUI cardUI = Get<global::CardUI>(firstCardInfo, CardInfoCardUIField);
                    return IsDoneAnimatingIn(firstCardInfo) &&
                        cardUI != null &&
                        cardUI.gameObject.activeInHierarchy &&
                        cardUI.SelectableUI != null &&
                        cardUI.SelectableUI.CanBeSelected();
                }
            }

            GameUISelectableButton close = _screen.GetCloseButton();
            return close != null && close.gameObject.activeInHierarchy && close.CanBeSelected();
        }

        private IEnumerable<global::DeckScreen.CardInfo> GetCardInfos()
        {
            List<global::DeckScreen.CardInfo> cardInfos = Get<List<global::DeckScreen.CardInfo>>(_screen, CardInfosField);
            if (cardInfos == null)
            {
                yield break;
            }

            for (int i = 0; i < cardInfos.Count; i++)
            {
                global::DeckScreen.CardInfo cardInfo = cardInfos[i];
                if (cardInfo != null)
                {
                    yield return cardInfo;
                }
            }
        }

        private static bool IsDoneAnimatingIn(global::DeckScreen.CardInfo cardInfo)
        {
            object value = CardInfoIsDoneAnimatingInField.GetValue(cardInfo);
            return value is bool done && done;
        }

        private static T Get<T>(object owner, FieldInfo field) where T : class
        {
            return owner != null ? field.GetValue(owner) as T : null;
        }
    }
}

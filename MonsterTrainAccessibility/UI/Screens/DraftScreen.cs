using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Presentation;
using MonsterTrainAccessibility.Presentation.Verbosity;
using MonsterTrainAccessibility.UI.Elements;
using ShinyShoe;
using TMPro;

namespace MonsterTrainAccessibility.UI.Screens
{
    internal sealed class DraftScreen : GridNavigationGameScreen
    {
        private static readonly FieldInfo HeaderLabelField = AccessTools.Field(typeof(global::DraftScreenBase), "headerLabel")!;
        private static readonly FieldInfo SkipButtonField = AccessTools.Field(typeof(global::DraftScreenBase), "skipButton")!;
        private static readonly FieldInfo SkipButtonLabelField = AccessTools.Field(typeof(global::DraftScreenBase), "skipButtonLabel")!;
        private static readonly FieldInfo SkipButtonKeyField = AccessTools.Field(typeof(global::DraftScreenBase), "skipButtonKey")!;
        private static readonly FieldInfo GoldForSkipFormatKeyField = AccessTools.Field(typeof(global::DraftScreenBase), "goldForSkipFormatKey")!;
        private static readonly FieldInfo PyreHealthCostForSkipFormatKeyField = AccessTools.Field(typeof(global::DraftScreenBase), "pyreHealthCostForSkipFormatKey")!;
        private static readonly FieldInfo SaveManagerField = AccessTools.Field(typeof(global::DraftScreenBase), "saveManager")!;
        private static readonly FieldInfo DraftItemsField = AccessTools.Field(typeof(global::DraftScreenBase), "draftItems")!;
        private static readonly FieldInfo BonusCardSummaryItemsField = AccessTools.Field(typeof(global::CardDraftScreen), "_bonusCardSummaryItems")!;
        private static readonly FieldInfo SoulRerollButtonField = AccessTools.Field(typeof(global::SoulDraftScreen), "rerollButton")!;
        private static readonly FieldInfo SoulRerollButtonLabelField = AccessTools.Field(typeof(global::SoulDraftScreen), "rerollButtonLabel")!;
        private static readonly FieldInfo ElixirHideButtonField = AccessTools.Field(typeof(global::ElixirDraftScreen), "hideButton")!;
        private static readonly FieldInfo ElixirShowButtonField = AccessTools.Field(typeof(global::ElixirDraftScreen), "showButton")!;
        private static readonly MethodInfo DoesSkipCostPyreHealthFromRelicEffectMethod = AccessTools.Method(typeof(global::DraftScreenBase), "DoesSkipCostPyreHealthFromRelicEffect")!;
        private static readonly MethodInfo GetPyreHealthCostForSkippingFromRelicEffectMethod = AccessTools.Method(typeof(global::DraftScreenBase), "GetPyreHealthCostForSkippingFromRelicEffect")!;
        private static readonly MethodInfo GetGoldForSkippingMethod = AccessTools.Method(typeof(global::DraftScreenBase), "GetGoldForSkipping")!;

        private readonly global::DraftScreenBase _screen;

        public DraftScreen(global::DraftScreenBase screen)
        {
            _screen = screen;
            Grid.AnnouncePosition = false;
        }

        protected override void PopulateGrid()
        {
            int columns = 3;
            int count = 0;

            TMP_Text header = Get<TMP_Text>(_screen, HeaderLabelField);
            string headerText = AccessibleScreenText.Text(header)?.Resolve();
            Grid.ContainerLabel = Message.ShouldAdd(headerText) ? headerText : null;
            Grid.AnnounceName = Grid.ContainerLabel != null;

            List<IDraftableUI> items = Get<List<IDraftableUI>>(_screen, DraftItemsField);
            if (items != null)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    AddDraftItem(items[i], count % columns, count / columns);
                    count++;
                }
            }

            int buttonRow = count == 0 ? 0 : (count + columns - 1) / columns;
            int buttonColumn = 0;
            AddButton(Get<GameUISelectableButton>(_screen, SkipButtonField), buttonColumn++, buttonRow);
            if (_screen is global::SoulDraftScreen)
            {
                AddButton(Get<GameUISelectableButton>(_screen, SoulRerollButtonField), buttonColumn++, buttonRow);
            }
            if (_screen is global::ElixirDraftScreen)
            {
                AddButton(Get<GameUISelectableButton>(_screen, ElixirHideButtonField), buttonColumn++, buttonRow);
                AddButton(Get<GameUISelectableButton>(_screen, ElixirShowButtonField), buttonColumn, buttonRow);
            }
        }

        protected override string BuildSignature()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            List<IDraftableUI> items = Get<List<IDraftableUI>>(_screen, DraftItemsField);
            if (items != null)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    IGameUIComponent component = items[i]?.SelectableUI;
                    sb.Append(component?.component != null ? component.component.GetInstanceID() : 0).Append(':');
                    global::CardUI cardUI = items[i] as global::CardUI;
                    CardState card = cardUI?.GetCardState();
                    sb.Append(card != null ? card.GetID() : AccessibleScreenText.ReadDraftableLabel(items[i])?.Resolve());
                    CardState bonus = BonusCardFor(card);
                    if (bonus != null)
                    {
                        sb.Append('+').Append(bonus.GetID());
                    }
                    sb.Append('|');
                }
            }

            return sb.ToString();
        }

        private void AddDraftItem(IDraftableUI item, int x, int y)
        {
            if (item == null)
            {
                return;
            }

            IGameUIComponent selectable = item.SelectableUI;
            if (selectable == null || selectable.component == null)
            {
                return;
            }

            global::CardUI cardUI = item as global::CardUI;
            UIElement element = cardUI != null
                ? (UIElement)new ProxyCombatCard(cardUI, selectable, bufferBottomParts: DraftBufferBottomParts, focusSummary: DraftFocusSummary)
                : item is RelicInfoUI relicInfo
                ? (UIElement)new ProxyRelicInfo(relicInfo)
                : item is SoulInfoUI soulInfo
                ? (UIElement)new ProxySoulInfoItem(soulInfo, selectable)
                : item is EndlessMutatorPairUI endlessMutatorPair
                ? (UIElement)new ProxyEndlessMutatorPair(endlessMutatorPair, selectable)
                : new ProxyDraftableFallback(item, selectable);

            Grid.Add(element, x, y);
            RegisterElement(element, selectable.component.gameObject, item.rootTransform != null ? item.rootTransform.gameObject : null);
        }

        private Message DraftFocusSummary(CardState card)
        {
            Message summary = ProxyCombatCard.FocusSummary(card);
            Message bonusName = Message.FromText(BonusCardFor(card)?.GetTitle());
            return Message.Join(", ", summary, bonusName);
        }

        private List<Message> DraftBufferBottomParts(global::CardUI cardUI, CardState card)
        {
            CardState bonus = BonusCardFor(card);
            return bonus != null
                ? new List<Message>(PresentationRenderer.BufferLines(
                    PhaseRegistry.Cards.Build(bonus),
                    VerbosityRegistry.ForSource<CardState>()))
                : null;
        }

        private CardState BonusCardFor(CardState parentCard)
        {
            global::CardDraftScreen cardDraftScreen = _screen as global::CardDraftScreen;
            if (cardDraftScreen == null || parentCard == null)
            {
                return null;
            }

            List<global::BonusCardSummaryItem> bonusItems = Get<List<global::BonusCardSummaryItem>>(cardDraftScreen, BonusCardSummaryItemsField);
            if (bonusItems == null)
            {
                return null;
            }

            for (int i = 0; i < bonusItems.Count; i++)
            {
                global::BonusCardSummaryItem bonus = bonusItems[i];
                if (bonus != null && bonus.gameObject.activeInHierarchy && ReferenceEquals(bonus.ParentCardState, parentCard))
                {
                    return bonus.CardState;
                }
            }

            return null;
        }

        private void AddButton(GameUISelectableButton button, int x, int y)
        {
            if (button == null)
            {
                return;
            }

            LabeledButton element = new LabeledButton(button, ResolveButtonLabel(button));
            Grid.Add(element, x, y);
            RegisterElement(element, button.gameObject);
        }

        private Message ResolveButtonLabel(GameUISelectableButton button)
        {
            if (button == Get<GameUISelectableButton>(_screen, SkipButtonField))
            {
                Message skip = ResolveSkipButtonLabel(button);
                if (skip != null)
                {
                    return skip;
                }
            }

            if (_screen is global::SoulDraftScreen && button == Get<GameUISelectableButton>(_screen, SoulRerollButtonField))
            {
                Message reroll = Message.FromText(AccessibilityText.ReadLocalizedText(Get<TMP_Text>(_screen, SoulRerollButtonLabelField)));
                if (reroll != null)
                {
                    return reroll;
                }
            }

            return Message.FromText(AccessibleScreenText.ReadButtonLabel(button));
        }

        private Message ResolveSkipButtonLabel(GameUISelectableButton button)
        {
            TMP_Text authoredLabel = Get<TMP_Text>(_screen, SkipButtonLabelField);
            Message authored = Message.FromText(AccessibilityText.ReadLocalizedText(authoredLabel));
            if (authored != null)
            {
                return authored;
            }

            string text = AccessibilityText.LocalizeTerm(Get<string>(_screen, SkipButtonKeyField));
            if (string.IsNullOrWhiteSpace(text))
            {
                return Message.FromText(AccessibleScreenText.ReadButtonLabel(button));
            }

            bool costsPyreHealth = (bool)DoesSkipCostPyreHealthFromRelicEffectMethod.Invoke(_screen, null);
            if (costsPyreHealth)
            {
                int pyreCost = (int)GetPyreHealthCostForSkippingFromRelicEffectMethod.Invoke(_screen, null);
                string formatKey = Get<string>(_screen, PyreHealthCostForSkipFormatKeyField);
                if (!string.IsNullOrWhiteSpace(formatKey) && pyreCost > 0)
                {
                    text += global::LocalizationUtil.LocalizeWithNumber(formatKey, pyreCost);
                }
            }
            else
            {
                SaveManager saveManager = Get<SaveManager>(_screen, SaveManagerField);
                if (saveManager != null)
                {
                    int adjustedGold = saveManager.GetAdjustedGoldAmount(
                        (int)GetGoldForSkippingMethod.Invoke(_screen, new object[] { saveManager.GetBalanceData().GetGoldForSkippingRewards() }),
                        isReward: true);
                    if (adjustedGold > 0)
                    {
                        string formatKey = Get<string>(_screen, GoldForSkipFormatKeyField);
                        if (!string.IsNullOrWhiteSpace(formatKey))
                        {
                            text += global::LocalizationUtil.LocalizeWithNumber(formatKey, adjustedGold);
                        }
                    }
                }
            }

            return Message.FromText(text);
        }

        private static T Get<T>(object owner, FieldInfo field) where T : class
        {
            return owner != null ? field.GetValue(owner) as T : null;
        }
    }
}

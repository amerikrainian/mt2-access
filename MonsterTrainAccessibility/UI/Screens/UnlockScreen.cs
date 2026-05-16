using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Elements;
using ShinyShoe;
using TMPro;
using UnityEngine;

namespace MonsterTrainAccessibility.UI.Screens
{
    internal sealed class UnlockScreen : ListNavigationGameScreen
    {
        private static readonly FieldInfo HeaderLabelField = AccessTools.Field(typeof(global::UnlockScreen), "headerLabel")!;
        private static readonly FieldInfo InstructionsLabelField = AccessTools.Field(typeof(global::UnlockScreen), "instructionsLabel")!;
        private static readonly FieldInfo RewardDetailsUIField = AccessTools.Field(typeof(global::UnlockScreen), "rewardDetailsUI")!;
        private static readonly FieldInfo UnlockDetailsUIField = AccessTools.Field(typeof(global::UnlockScreen), "unlockDetailsUI")!;
        private static readonly FieldInfo CardMasteryUIField = AccessTools.Field(typeof(global::UnlockScreen), "cardMasteryUI")!;
        private static readonly FieldInfo CardMasteryCardsField = AccessTools.Field(typeof(global::CardMasteryUI), "cardUIs")!;
        private static readonly FieldInfo CardFrameUIField = AccessTools.Field(typeof(global::UnlockScreen), "cardFrameUI")!;
        private static readonly FieldInfo CollectButtonField = AccessTools.Field(typeof(global::UnlockScreen), "collectButton")!;
        private static readonly PropertyInfo CurrentItemProperty = AccessTools.Property(typeof(global::UnlockScreen), "currentItem")!;

        private readonly global::UnlockScreen _screen;

        public UnlockScreen(global::UnlockScreen screen)
        {
            _screen = screen;
        }

        public override bool ShouldRestoreNavigationFocus() => false;

        public override bool ShouldAnnounceFocus(UIElement element)
        {
            return CurrentItem() != null && base.ShouldAnnounceFocus(element);
        }

        protected override void PopulateList()
        {
            if (CurrentItem() == null)
            {
                return;
            }

            TMP_Text header = Get<TMP_Text>(_screen, HeaderLabelField);
            if (header != null)
            {
                ProxyUnlockText element = new ProxyUnlockText(header);
                AddElement(element, header.gameObject);
            }

            TMP_Text instructions = Get<TMP_Text>(_screen, InstructionsLabelField);
            if (instructions != null)
            {
                ProxyUnlockText element = new ProxyUnlockText(instructions);
                AddElement(element, instructions.gameObject);
            }

            AddCurrentUnlockContent();
            AddButton(Get<GameUISelectableButton>(_screen, CollectButtonField));
        }

        protected override string BuildSignature()
        {
            global::UnlockScreen.UnlockDisplayData item = CurrentItem();
            TMP_Text header = Get<TMP_Text>(_screen, HeaderLabelField);
            TMP_Text instructions = Get<TMP_Text>(_screen, InstructionsLabelField);
            return (item?.source.ToString() ?? string.Empty) + "|" +
                (item?.unlockedCardData?.GetID() ?? string.Empty) + "|" +
                (item?.unlockedRelicData?.GetID() ?? string.Empty) + "|" +
                (item?.unlockedFeatureData?.title ?? string.Empty) + "|" +
                item?.masteryFrameType.ToString() + "|" +
                MasteredCardsSignature(item?.masteredCardDatas) + "|" +
                AccessibilityText.ReadLocalizedText(header) + "|" +
                AccessibilityText.ReadLocalizedText(instructions);
        }

        private void AddCurrentUnlockContent()
        {
            global::UnlockScreen.UnlockDisplayData item = CurrentItem();
            if (item == null)
            {
                return;
            }

            RewardDetailsUI rewardDetails = Get<RewardDetailsUI>(_screen, RewardDetailsUIField);
            if (item.unlockedCardData != null || item.unlockedRelicData != null)
            {
                ProxyUnlockContent element = new ProxyUnlockContent(item, rewardDetails);
                AddElement(element, rewardDetails != null ? rewardDetails.gameObject : null);
            }

            if (item.unlockedFeatureData != null)
            {
                AddFeatureUnlock(item.unlockedFeatureData);
            }

            if (item.masteredCardDatas != null && item.masteredCardDatas.Count > 0)
            {
                CardMasteryUI mastery = Get<CardMasteryUI>(_screen, CardMasteryUIField);
                ProxyUnlockMasteredCards element = new ProxyUnlockMasteredCards(
                    mastery != null ? mastery.gameObject : null,
                    item.masteredCardDatas);
                AddElement(element, MasteryTargets(mastery));
            }

            if (item.masteryFrameType != MasteryFrameType.None)
            {
                CardFramePreviewUI frame = Get<CardFramePreviewUI>(_screen, CardFrameUIField);
                ProxyUnlockMasteryFrame element = new ProxyUnlockMasteryFrame(
                    frame,
                    item.masteryFrameType,
                    item.instructions);
                AddElement(element, frame != null ? frame.gameObject : null);
            }
        }

        private void AddFeatureUnlock(global::UnlockDetailsUI.Data data)
        {
            global::UnlockDetailsUI details = Get<global::UnlockDetailsUI>(_screen, UnlockDetailsUIField);
            ProxyUnlockFeature element = new ProxyUnlockFeature(data, details);
            AddElement(element, details != null ? details.gameObject : null, element.Title != null ? element.Title.gameObject : null, element.Description != null ? element.Description.gameObject : null);
        }

        private void AddButton(GameUISelectableButton button)
        {
            if (button == null)
            {
                return;
            }

            ProxyUnlockButton element = new ProxyUnlockButton(button);
            AddElement(element, button.gameObject);
        }

        private global::UnlockScreen.UnlockDisplayData CurrentItem()
        {
            return CurrentItemProperty.GetValue(_screen) as global::UnlockScreen.UnlockDisplayData;
        }

        private static GameObject[] MasteryTargets(CardMasteryUI mastery)
        {
            if (mastery == null)
            {
                return new GameObject[] { null };
            }

            List<GameObject> targets = new List<GameObject>
            {
                mastery.gameObject
            };

            List<CardUI> cardUIs = Get<List<CardUI>>(mastery, CardMasteryCardsField);
            if (cardUIs != null)
            {
                for (int i = 0; i < cardUIs.Count; i++)
                {
                    CardUI cardUI = cardUIs[i];
                    if (cardUI == null)
                    {
                        continue;
                    }

                    targets.Add(cardUI.gameObject);
                    IGameUIComponent selectable = cardUI.SelectableUI;
                    if (selectable?.component != null)
                    {
                        targets.Add(selectable.component.gameObject);
                    }
                }
            }

            return targets.ToArray();
        }

        private static string MasteredCardsSignature(IReadOnlyList<CardData> cards)
        {
            if (cards == null || cards.Count == 0)
            {
                return string.Empty;
            }

            List<string> ids = new List<string>();
            for (int i = 0; i < cards.Count; i++)
            {
                string id = cards[i]?.GetID();
                if (!string.IsNullOrEmpty(id))
                {
                    ids.Add(id);
                }
            }

            return string.Join(",", ids.ToArray());
        }

        private static T Get<T>(object owner, FieldInfo field) where T : class
        {
            return owner != null ? field.GetValue(owner) as T : null;
        }
    }
}

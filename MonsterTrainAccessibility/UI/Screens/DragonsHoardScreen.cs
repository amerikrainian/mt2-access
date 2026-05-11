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
    internal sealed class DragonsHoardScreen : ListNavigationGameScreen
    {
        private static readonly FieldInfo CurrentLootLabelField = AccessTools.Field(typeof(global::DragonsHoardScreen), "currentDragonsHoardLootLabel")!;
        private static readonly FieldInfo SaveManagerField = AccessTools.Field(typeof(global::DragonsHoardScreen), "saveManager")!;
        private static readonly FieldInfo RewardUIsField = AccessTools.Field(typeof(global::DragonsHoardScreen), "rewardUIs")!;
        private static readonly FieldInfo ConfirmButtonField = AccessTools.Field(typeof(global::DragonsHoardScreen), "confirmButton")!;
        private static readonly FieldInfo CancelButtonField = AccessTools.Field(typeof(global::DragonsHoardScreen), "cancelButton")!;
        private static readonly FieldInfo LootLevelsButtonField = AccessTools.Field(typeof(global::DragonsHoardScreen), "lootLevelsButton")!;
        private static readonly FieldInfo InfoDialogField = AccessTools.Field(typeof(global::DragonsHoardScreen), "dragonsHoardInfoDialog")!;
        private static readonly FieldInfo DialogItemsField = AccessTools.Field(typeof(global::DragonsHoardInfoDialog), "dragonsHoardRewardSelectionItems")!;
        private static readonly FieldInfo DialogCurrentLootLevelTextField = AccessTools.Field(typeof(global::DragonsHoardInfoDialog), "currentLootLevelText")!;
        private static readonly FieldInfo DialogLootLevelAmountField = AccessTools.Field(typeof(global::DragonsHoardInfoDialog), "lootLevelAmount")!;

        private readonly global::DragonsHoardScreen _screen;

        public DragonsHoardScreen(global::DragonsHoardScreen screen)
        {
            _screen = screen;
            RootList.AnnouncePosition = false;
        }

        protected override void PopulateList()
        {
            global::DragonsHoardInfoDialog dialog = Get<global::DragonsHoardInfoDialog>(_screen, InfoDialogField);
            if (dialog != null && dialog.gameObject.activeInHierarchy)
            {
                PopulateDialog(dialog);
                return;
            }

            TMP_Text lootLabel = Get<TMP_Text>(_screen, CurrentLootLabelField);
            SaveManager saveManager = Get<SaveManager>(_screen, SaveManagerField);
            if (lootLabel != null || saveManager != null)
            {
                ProxyDragonsHoardAmount element = new ProxyDragonsHoardAmount(
                    lootLabel != null ? lootLabel.gameObject : _screen.gameObject,
                    saveManager,
                    lootLabel);
                AddElement(element, lootLabel != null ? lootLabel.gameObject : null);
            }

            List<RewardItemUI> rewards = Get<List<RewardItemUI>>(_screen, RewardUIsField);
            if (rewards != null)
            {
                for (int i = 0; i < rewards.Count; i++)
                {
                    RewardItemUI reward = rewards[i];
                    if (reward == null)
                    {
                        continue;
                    }

                    AddRewardPreview(reward);
                }
            }

            AddButton(Get<GameUISelectableButton>(_screen, ConfirmButtonField), Message.Localized("ui", "DRAGONS_HOARD.REDEEM"));
            AddButton(Get<GameUISelectableButton>(_screen, LootLevelsButtonField), Message.Localized("ui", "DRAGONS_HOARD.LOOT_LEVELS"));
            AddButton(Get<GameUISelectableButton>(_screen, CancelButtonField), Message.Localized("ui", "DRAGONS_HOARD.CLOSE"));
        }

        protected override string BuildSignature()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            TMP_Text lootLabel = Get<TMP_Text>(_screen, CurrentLootLabelField);
            sb.Append(AccessibilityText.ReadLocalizedText(lootLabel)).Append('|');
            List<RewardItemUI> rewards = Get<List<RewardItemUI>>(_screen, RewardUIsField);
            if (rewards != null)
            {
                for (int i = 0; i < rewards.Count; i++)
                {
                    RewardItemUI reward = rewards[i];
                    sb.Append(reward != null && reward.gameObject.activeInHierarchy)
                        .Append(':')
                        .Append(reward?.rewardState?.RewardData?.RewardTitle)
                        .Append(':')
                        .Append(reward?.rewardState?.Claimed)
                        .Append('|');
                }
            }

            global::DragonsHoardInfoDialog dialog = Get<global::DragonsHoardInfoDialog>(_screen, InfoDialogField);
            sb.Append(";dialog:").Append(dialog != null && dialog.gameObject.activeInHierarchy);
            return sb.ToString();
        }

        private void PopulateDialog(global::DragonsHoardInfoDialog dialog)
        {
            List<DragonsHoardRewardSelectionItem> items = Get<List<DragonsHoardRewardSelectionItem>>(dialog, DialogItemsField);
            if (items != null)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    AddDialogItem(items[i]);
                }
            }

            TMP_Text currentLootLevelText = Get<TMP_Text>(dialog, DialogCurrentLootLevelTextField);
            TMP_Text selectedLootLevelAmount = Get<TMP_Text>(dialog, DialogLootLevelAmountField);
            if (currentLootLevelText != null || selectedLootLevelAmount != null)
            {
                ProxyDragonsHoardDialogLevel element = new ProxyDragonsHoardDialogLevel(
                    currentLootLevelText != null ? currentLootLevelText.gameObject : dialog.gameObject,
                    dialog.gameObject,
                    currentLootLevelText,
                    selectedLootLevelAmount);
                AddElement(
                    element,
                    currentLootLevelText != null ? currentLootLevelText.gameObject : null,
                    selectedLootLevelAmount != null ? selectedLootLevelAmount.gameObject : null);
            }

            AddButton(dialog.CloseButton, Message.Localized("ui", "DRAGONS_HOARD.CLOSE"));
        }

        private void AddDialogItem(DragonsHoardRewardSelectionItem item)
        {
            if (item == null)
            {
                return;
            }

            IGameUIComponent selectable = item.GetDefaultGameUISelectable();
            if (selectable == null || selectable.component == null)
            {
                return;
            }

            ProxyDragonsHoardDialogItem element = new ProxyDragonsHoardDialogItem(item, selectable);
            AddElement(element, item.gameObject, selectable.component.gameObject);
        }

        private void AddRewardPreview(RewardItemUI reward)
        {
            ProxyDragonsHoardRewardPreview element = new ProxyDragonsHoardRewardPreview(reward);
            if (!element.IsVisible)
            {
                return;
            }

            AddElement(element, reward.gameObject);
        }

        private void AddButton(GameUISelectableButton button, Message label)
        {
            if (button == null)
            {
                return;
            }

            LabeledButton element = new LabeledButton(button, label);
            AddElement(element, button.gameObject);
        }

        private static T Get<T>(object owner, FieldInfo field) where T : class
        {
            return owner != null ? field.GetValue(owner) as T : null;
        }
    }
}

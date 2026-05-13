using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Buffers;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Elements;
using ShinyShoe;
using UnityEngine;

namespace MonsterTrainAccessibility.UI.Screens
{
    internal sealed class ChampionUpgradeScreen : ListNavigationGameScreen
    {
        private static readonly FieldInfo UpgradeChoiceItemsField = AccessTools.Field(typeof(global::ChampionUpgradeScreen), "upgradeChoiceItems")!;
        private static readonly FieldInfo NoUpgradesButtonField = AccessTools.Field(typeof(global::ChampionUpgradeScreen), "noUpgradesButton")!;
        private static readonly FieldInfo NoUpgradesRootField = AccessTools.Field(typeof(global::ChampionUpgradeScreen), "noUpgradesRoot")!;

        private readonly global::ChampionUpgradeScreen _screen;

        public ChampionUpgradeScreen(global::ChampionUpgradeScreen screen)
        {
            _screen = screen;
        }

        protected override void PopulateList()
        {
            List<UpgradeCardChoiceItem> items = Get<List<UpgradeCardChoiceItem>>(_screen, UpgradeChoiceItemsField);
            if (items != null)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    AddUpgrade(items[i]);
                }
            }

            GameObject noUpgradesRoot = Get<GameObject>(_screen, NoUpgradesRootField);
            GameUISelectableButton noUpgradesButton = Get<GameUISelectableButton>(_screen, NoUpgradesButtonField);
            if (noUpgradesButton != null)
            {
                LabeledButton element = new LabeledButton(
                    noUpgradesButton,
                    () => Message.FromText(AccessibleScreenText.ReadButtonLabel(noUpgradesButton)),
                    () => noUpgradesRoot != null && noUpgradesRoot.activeInHierarchy && noUpgradesButton.gameObject.activeInHierarchy);
                AddElement(element, noUpgradesButton.gameObject);
            }
        }

        protected override string BuildSignature()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            List<UpgradeCardChoiceItem> items = Get<List<UpgradeCardChoiceItem>>(_screen, UpgradeChoiceItemsField);
            if (items != null)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    UpgradeCardChoiceItem item = items[i];
                    sb.Append(item != null && item.gameObject.activeInHierarchy)
                        .Append(':')
                        .Append(item?.upgradeData?.upgradeState?.GetCardUpgradeDataId())
                        .Append('|');
                }
            }

            GameObject noUpgradesRoot = Get<GameObject>(_screen, NoUpgradesRootField);
            sb.Append(";none:").Append(noUpgradesRoot != null && noUpgradesRoot.activeInHierarchy);
            return sb.ToString();
        }

        private void AddUpgrade(UpgradeCardChoiceItem item)
        {
            if (item == null || item.SelectableUI == null || item.SelectableUI.component == null)
            {
                return;
            }

            ProxyChampionUpgradeChoice element = new ProxyChampionUpgradeChoice(item);
            AddElement(element, item.gameObject, item.SelectableUI.component.gameObject, item.cardUI != null ? item.cardUI.gameObject : null);
        }

        private static Message UpgradeLabel(UpgradeCardChoiceItem item)
        {
            if (item?.upgradeData == null)
            {
                return null;
            }

            string upgradeTitle = AccessibilityText.LocalizeTerm(item.upgradeData.upgradeState?.GetUpgradeTitleKey());
            string cardTitle = item.upgradeData.postCardState?.GetTitle();
            if (string.IsNullOrWhiteSpace(upgradeTitle))
            {
                return Message.RawCleaned(cardTitle);
            }

            if (string.IsNullOrWhiteSpace(cardTitle))
            {
                return Message.RawCleaned(upgradeTitle);
            }

            return Message.RawCleaned(upgradeTitle + ", " + cardTitle);
        }

        internal static Message UpgradeFocusSummary(UpgradeCardChoiceItem item)
        {
            Message upgrade = Message.RawCleaned(item?.upgradeData?.upgradeState?.GetUpgradeTitle());
            Message cardSummary = ProxyCombatCard.FocusSummary(item?.upgradeData?.postCardState);
            if (upgrade == null)
            {
                return cardSummary;
            }

            if (cardSummary == null)
            {
                return upgrade;
            }

            return Message.Join(", ", new[] { upgrade, cardSummary });
        }

        internal static Message UpgradeTooltip(UpgradeCardChoiceItem item)
        {
            List<Message> parts = new List<Message>();
            Message cardSummary = ProxyCombatCard.AccessibilitySummary(item?.upgradeData?.postCardState);
            if (cardSummary != null)
            {
                parts.Add(cardSummary);
            }

            CardUpgradeData upgradeData = ResolveUpgradeData(item);
            Message upgradeTooltip = new ProxyCardUpgrade(upgradeData).GetTooltip();
            if (upgradeTooltip != null)
            {
                parts.Add(upgradeTooltip);
            }

            return parts.Count > 0 ? Message.JoinLines(parts) : null;
        }

        private static CardUpgradeData ResolveUpgradeData(UpgradeCardChoiceItem item)
        {
            string upgradeId = item?.upgradeData?.upgradeState?.GetCardUpgradeDataId();
            SaveManager saveManager = item?.upgradeData?.saveManager;
            return !string.IsNullOrWhiteSpace(upgradeId)
                ? saveManager?.GetAllGameData()?.FindCardUpgradeData(upgradeId)
                : null;
        }
        private static T Get<T>(object owner, FieldInfo field) where T : class
        {
            return owner != null ? field.GetValue(owner) as T : null;
        }
    }
}

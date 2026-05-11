using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Elements;
using ShinyShoe;

namespace MonsterTrainAccessibility.UI.Screens
{
    internal sealed class RunSummaryScreen : ListNavigationGameScreen
    {
        private static readonly FieldInfo SummaryDetailsField = AccessTools.Field(typeof(global::RunSummaryScreen), "summaryDetailsUI")!;
        private static readonly FieldInfo BackButtonField = AccessTools.Field(typeof(global::RunSummaryScreen), "backButton")!;
        private static readonly FieldInfo PreviousEntryButtonField = AccessTools.Field(typeof(global::RunSummaryScreen), "previousEntryButton")!;
        private static readonly FieldInfo NextEntryButtonField = AccessTools.Field(typeof(global::RunSummaryScreen), "nextEntryButton")!;
        private static readonly FieldInfo CreateChallengeButtonField = AccessTools.Field(typeof(global::RunSummaryScreen), "createChallengeButton")!;

        private static readonly FieldInfo MainClanUIField = AccessTools.Field(typeof(global::RunSummaryDetailsUI), "mainClanUI")!;
        private static readonly FieldInfo SubClanUIField = AccessTools.Field(typeof(global::RunSummaryDetailsUI), "subClanUI")!;
        private static readonly FieldInfo PyreHeartUIField = AccessTools.Field(typeof(global::RunSummaryDetailsUI), "pyreHeartUI")!;
        private static readonly FieldInfo DifficultyTierUIField = AccessTools.Field(typeof(global::RunSummaryDetailsUI), "difficultyTierUI")!;
        private static readonly FieldInfo BlessingCollectionField = AccessTools.Field(typeof(global::RunSummaryDetailsUI), "blessingCollection")!;
        private static readonly FieldInfo MutatorCollectionField = AccessTools.Field(typeof(global::RunSummaryDetailsUI), "mutatorCollection")!;
        private static readonly FieldInfo SoulsCollectionField = AccessTools.Field(typeof(global::RunSummaryDetailsUI), "soulsCollection")!;
        private static readonly FieldInfo CardUIsField = AccessTools.Field(typeof(global::RunSummaryDetailsUI), "cardUIs")!;
        private static readonly FieldInfo BattleHistoryItemsField = AccessTools.Field(typeof(global::RunSummaryDetailsUI), "battleHistoryItems")!;

        private static readonly FieldInfo RelicIconsField = AccessTools.Field(typeof(global::RelicCollectionUI), "relicIcons")!;

        private readonly global::RunSummaryScreen _screen;

        public RunSummaryScreen(global::RunSummaryScreen screen)
        {
            _screen = screen;
            RootList.AnnouncePosition = false;
        }

        public override bool ShouldRestoreNavigationFocus() => false;
        public override bool ShouldAcceptGameSelection() => false;

        protected override void PopulateList()
        {
            global::RunSummaryDetailsUI details = Get<global::RunSummaryDetailsUI>(_screen, SummaryDetailsField);
            AddSummaryLabels(details);
            AddRunSetup(details);
            AddRelicCollection(Get<global::RelicCollectionUI>(details, BlessingCollectionField), "RUN_SUMMARY.RELICS");
            AddRelicCollection(Get<global::RelicCollectionUI>(details, MutatorCollectionField), "RUN_SUMMARY.MUTATORS");
            AddRelicCollection(Get<global::RelicCollectionUI>(details, SoulsCollectionField), "RUN_SUMMARY.SOULS");
            AddCards(details);
            AddBattles(details);
            AddButton(Get<GameUISelectableButton>(_screen, PreviousEntryButtonField), "RUN_SUMMARY.PREVIOUS_ENTRY");
            AddButton(Get<GameUISelectableButton>(_screen, NextEntryButtonField), "RUN_SUMMARY.NEXT_ENTRY");
            AddButton(Get<GameUISelectableButton>(_screen, CreateChallengeButtonField), "RUN_SUMMARY.CREATE_CHALLENGE");
            AddButton(Get<GameUISelectableButton>(_screen, BackButtonField), "RUN_SUMMARY.BACK");
        }

        protected override string BuildSignature()
        {
            global::RunSummaryDetailsUI details = Get<global::RunSummaryDetailsUI>(_screen, SummaryDetailsField);
            int cards = Get<List<global::CardSummaryItem>>(details, CardUIsField)?.Count ?? 0;
            int battles = Get<List<global::BattleHistoryItem>>(details, BattleHistoryItemsField)?.Count ?? 0;
            return base.BuildSignature() +
                "|" + ProxyRunSummaryText.Signature(details) +
                "|" + cards.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                "|" + battles.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        private void AddSummaryLabels(global::RunSummaryDetailsUI details)
        {
            AddText(details, RunSummaryTextPart.Player, "RUN_SUMMARY.PLAYER");
            AddText(details, RunSummaryTextPart.Sharecode, "RUN_SUMMARY.SHARECODE");
            AddText(details, RunSummaryTextPart.Date, "RUN_SUMMARY.DATE");
            AddText(details, RunSummaryTextPart.RunType, "RUN_SUMMARY.RUN_TYPE");
            AddText(details, RunSummaryTextPart.OutcomeDefault, "RUN_SUMMARY.OUTCOME");
            AddText(details, RunSummaryTextPart.OutcomeEndless, "RUN_SUMMARY.OUTCOME");
            AddText(details, RunSummaryTextPart.RunTime, "RUN_SUMMARY.RUN_TIME");
            AddText(details, RunSummaryTextPart.Score, "RUN_SUMMARY.SCORE");
            AddText(details, RunSummaryTextPart.Covenant, "RUN_SUMMARY.COVENANT");
        }

        private void AddRunSetup(global::RunSummaryDetailsUI details)
        {
            AddClassIcon(Get<global::ClassIconUI>(details, MainClanUIField), "RUN_SUMMARY.PRIMARY_CLAN");
            AddClassIcon(Get<global::ClassIconUI>(details, SubClanUIField), "RUN_SUMMARY.ALLIED_CLAN");
            AddTooltipElement(Get<global::PyreHeartInfoUI>(details, PyreHeartUIField), "RUN_SUMMARY.PYRE_HEART");
            AddTooltipElement(Get<global::DifficultyTierUI>(details, DifficultyTierUIField), "RUN_SUMMARY.DIFFICULTY");
        }

        private void AddText(global::RunSummaryDetailsUI details, RunSummaryTextPart part, string labelKey)
        {
            ProxyRunSummaryText element = new ProxyRunSummaryText(details, part, labelKey);
            AddElement(element, element.Text() != null ? element.Text().gameObject : null);
        }

        private void AddClassIcon(global::ClassIconUI icon, string labelKey)
        {
            AddElement(new ProxyRunSummaryClassIcon(icon, labelKey), icon != null ? icon.gameObject : null);
        }

        private void AddTooltipElement(UnityEngine.Component component, string labelKey)
        {
            AddElement(new ProxyRunSummaryTooltip(component, labelKey), component != null ? component.gameObject : null);
        }

        private void AddRelicCollection(global::RelicCollectionUI collection, string labelKey)
        {
            List<global::RelicIconUI> relics = Get<List<global::RelicIconUI>>(collection, RelicIconsField);
            if (relics == null)
            {
                return;
            }

            ListContainer group = new ListContainer(Message.Localized("ui", labelKey).Resolve(), NavigationAxis.Horizontal)
            {
                AnnouncePosition = true
            };

            for (int i = 0; i < relics.Count; i++)
            {
                global::RelicIconUI relic = relics[i];
                if (relic == null)
                {
                    continue;
                }

                ProxyRelicIcon element = new ProxyRelicIcon(relic);
                group.Add(element);
                Register(element, relic.gameObject, relic.SelectableUI != null ? relic.SelectableUI.component.gameObject : null);
            }

            if (group.Children.Count > 0)
            {
                AddElement(group, collection != null ? collection.gameObject : null);
            }
        }

        private void AddCards(global::RunSummaryDetailsUI details)
        {
            List<global::CardSummaryItem> cards = Get<List<global::CardSummaryItem>>(details, CardUIsField);
            if (cards == null)
            {
                return;
            }

            ListContainer deck = new ListContainer(Message.Localized("ui", "RUN_SUMMARY.DECK").Resolve(), NavigationAxis.Horizontal)
            {
                AnnouncePosition = true
            };

            for (int i = 0; i < cards.Count; i++)
            {
                global::CardSummaryItem item = cards[i];
                if (item == null)
                {
                    continue;
                }

                RunSummaryCardElement element = new RunSummaryCardElement(item);
                deck.Add(element);
                Register(element, item.gameObject, item.SelectableUI != null ? item.SelectableUI.component.gameObject : null);
            }

            if (deck.Children.Count > 0)
            {
                AddElement(deck);
            }
        }

        private void AddBattles(global::RunSummaryDetailsUI details)
        {
            List<global::BattleHistoryItem> battles = Get<List<global::BattleHistoryItem>>(details, BattleHistoryItemsField);
            if (battles == null)
            {
                return;
            }

            ListContainer group = new ListContainer(Message.Localized("ui", "RUN_SUMMARY.BATTLES").Resolve(), NavigationAxis.Horizontal)
            {
                AnnouncePosition = true
            };

            for (int i = 0; i < battles.Count; i++)
            {
                global::BattleHistoryItem item = battles[i];
                if (item == null)
                {
                    continue;
                }

                UIElement element = new ProxyRunSummaryBattleHistory(item);
                group.Add(element);
                Register(element, item.gameObject, item.ScoreEventUI != null ? item.ScoreEventUI.gameObject : null);
            }

            if (group.Children.Count > 0)
            {
                AddElement(group);
            }
        }

        private void AddButton(GameUISelectableButton button, string fallbackLabelKey)
        {
            if (button == null)
            {
                return;
            }

            AddElement(new ProxyRunSummaryButton(button, fallbackLabelKey), button.gameObject);
        }

        private static T Get<T>(object owner, FieldInfo field) where T : class
        {
            return owner != null ? field.GetValue(owner) as T : null;
        }
    }
}

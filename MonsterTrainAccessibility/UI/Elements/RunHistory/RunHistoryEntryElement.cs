using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI;
using ShinyShoe;
using TMPro;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class RunHistoryEntryElement : UIElement, IActivatableElement, INavigationTargetElement
    {
        private static readonly MethodInfo ShowRunSummaryMethod = AccessTools.Method(typeof(global::RunHistoryUI), "ShowRunSummary")!;
        private static readonly FieldInfo AllGameDataField = AccessTools.Field(typeof(global::RunHistoryEntryUI), "allGameData")!;
        private static readonly FieldInfo DateLabelField = AccessTools.Field(typeof(global::RunHistoryEntryUI), "dateLabel")!;
        private static readonly FieldInfo RunTypeLabelField = AccessTools.Field(typeof(global::RunHistoryEntryUI), "runTypeLabel")!;
        private static readonly FieldInfo ScoreLabelField = AccessTools.Field(typeof(global::RunHistoryEntryUI), "scoreLabel")!;
        private static readonly FieldInfo VictoryIconField = AccessTools.Field(typeof(global::RunHistoryEntryUI), "victoryIcon")!;
        private static readonly FieldInfo CleanRunIconField = AccessTools.Field(typeof(global::RunHistoryEntryUI), "cleanRunIcon")!;
        private static readonly FieldInfo CovenantUIField = AccessTools.Field(typeof(global::RunHistoryEntryUI), "covenantUI")!;
        private static readonly FieldInfo DistanceUIField = AccessTools.Field(typeof(global::RunHistoryEntryUI), "distanceUI")!;
        private static readonly FieldInfo DifficultyTierUIField = AccessTools.Field(typeof(global::RunHistoryEntryUI), "difficultyTierUI")!;
        private static readonly FieldInfo RunDistanceTooltipProviderField = AccessTools.Field(typeof(global::RunDistanceDisplay), "tooltipProvider")!;

        private readonly global::RunHistoryEntryUI _row;
        private readonly global::RunHistoryUI _runHistory;
        private readonly SaveManager _saveManager;

        public RunHistoryEntryElement(global::RunHistoryUI runHistory, global::RunHistoryEntryUI row, SaveManager saveManager)
        {
            _runHistory = runHistory;
            _row = row;
            _saveManager = saveManager;
        }

        public override bool IsVisible => _row != null && _row.gameObject.activeInHierarchy && _row.GetRunData() != null;
        public override string GetTypeKey() => "button";
        public override Message GetLabel() => Label(_row);
        public override Message GetStatusString() => Status(_row);
        public override Message GetTooltip() => Tooltip(_row);

        public bool Activate()
        {
            global::RunAggregateData data = _row?.GetRunData();
            if (_runHistory == null || data == null)
            {
                return false;
            }

            ShowRunSummaryMethod.Invoke(_runHistory, new object[] { data });
            return true;
        }

        public void SelectForNavigation()
        {
            global::InputManager.Inst?.SelectGameUIComponent(null, allowClearingSelection: true);
        }

        public override Message GetExtrasString()
        {
            return FavoriteStatus(_row, _saveManager);
        }

        private static Message Label(global::RunHistoryEntryUI row)
        {
            global::RunAggregateData data = row?.GetRunData();
            if (data == null)
            {
                return null;
            }

            List<Message> parts = new List<Message>();
            AddText(parts, Get<TMP_Text>(row, DateLabelField));
            AddText(parts, Get<TMP_Text>(row, RunTypeLabelField));
            AddClass(parts, row, data.GetMainClassID());
            AddClass(parts, row, data.GetSubClassID());
            parts.Add(Outcome(row, data));
            AddScore(parts, row);
            return Message.Join(", ", parts);
        }

        private static Message Status(global::RunHistoryEntryUI row)
        {
            if (row == null)
            {
                return null;
            }

            UnityEngine.UI.Image cleanRunIcon = Get<UnityEngine.UI.Image>(row, CleanRunIconField);
            return cleanRunIcon != null && cleanRunIcon.gameObject.activeInHierarchy
                ? Message.Localized("ui", "RUN_HISTORY.CLEAN_RUN")
                : null;
        }

        private static Message Tooltip(global::RunHistoryEntryUI row)
        {
            List<Message> parts = new List<Message>();
            AddTooltipProvider(parts, Get<global::ChallengeCovenantUI>(row, CovenantUIField)?.TooltipProvider);
            AddTooltipProvider(parts, Get<global::DifficultyTierUI>(row, DifficultyTierUIField)?.TooltipProvider);

            global::RunDistanceDisplay distance = Get<global::RunDistanceDisplay>(row, DistanceUIField);
            AddTooltipProvider(parts, Get<global::TooltipProviderComponent>(distance, RunDistanceTooltipProviderField));
            return parts.Count > 0 ? Message.JoinLines(parts) : null;
        }

        private static Message FavoriteStatus(global::RunHistoryEntryUI row, SaveManager saveManager)
        {
            global::RunAggregateData data = row?.GetRunData();
            if (saveManager == null || data == null)
            {
                return null;
            }

            return saveManager.RunHistoryManager.IsStarred(data.GetID())
                ? Message.Localized("ui", "RUN_HISTORY.FAVORITED")
                : null;
        }

        private static Message Outcome(global::RunHistoryEntryUI row, global::RunAggregateData data)
        {
            UnityEngine.UI.Image victoryIcon = Get<UnityEngine.UI.Image>(row, VictoryIconField);
            bool victory = victoryIcon != null ? victoryIcon.gameObject.activeInHierarchy : data.GetVictory();
            return Message.Localized("ui", victory ? "RUN_HISTORY.VICTORY" : "RUN_HISTORY.DEFEAT");
        }

        private static void AddScore(List<Message> parts, global::RunHistoryEntryUI row)
        {
            string score = Read(Get<TMP_Text>(row, ScoreLabelField));
            if (!string.IsNullOrWhiteSpace(score))
            {
                parts.Add(Message.Localized("ui", "RUN_HISTORY.SCORE", new { score }));
            }
        }

        private static void AddClass(List<Message> parts, global::RunHistoryEntryUI row, string classId)
        {
            AllGameData allGameData = Get<AllGameData>(row, AllGameDataField);
            ClassData classData = allGameData?.FindClassData(classId);
            string title = classData?.GetTitle();
            if (!string.IsNullOrWhiteSpace(title))
            {
                parts.Add(Message.RawCleaned(title));
            }
        }

        private static void AddText(List<Message> parts, TMP_Text text)
        {
            string value = Read(text);
            if (!string.IsNullOrWhiteSpace(value))
            {
                parts.Add(Message.RawCleaned(value));
            }
        }

        private static string Read(TMP_Text text)
        {
            return text != null && text.gameObject.activeInHierarchy
                ? AccessibilityText.ReadLocalizedText(text)
                : string.Empty;
        }

        private static void AddTooltipProvider(List<Message> parts, TooltipProviderComponent provider)
        {
            if (provider?.Tooltips == null)
            {
                return;
            }

            for (int i = 0; i < provider.Tooltips.Count; i++)
            {
                TooltipContent tooltip = provider.Tooltips[i];
                if (tooltip.IsEmpty())
                {
                    continue;
                }

                Message entry = TooltipEntry(tooltip);
                if (entry != null)
                {
                    parts.Add(entry);
                }
            }
        }

        private static Message TooltipEntry(TooltipContent tooltip)
        {
            Message title = Message.FromText(tooltip.title);
            Message body = Message.FromText(tooltip.body);
            if (title != null && body != null)
            {
                return Message.Join(", ", title, body);
            }

            return title ?? body;
        }

        private static T Get<T>(object owner, FieldInfo field) where T : class
        {
            return owner != null ? field.GetValue(owner) as T : null;
        }
    }
}

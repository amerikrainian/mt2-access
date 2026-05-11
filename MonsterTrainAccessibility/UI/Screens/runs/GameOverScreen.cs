using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Elements;
using ShinyShoe;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MonsterTrainAccessibility.UI.Screens
{
    internal sealed class GameOverScreen : ListNavigationGameScreen
    {
        private static readonly FieldInfo TitleLabelField = AccessTools.Field(typeof(global::GameOverScreen), "titleLabel")!;
        private static readonly FieldInfo RunInfoUIField = AccessTools.Field(typeof(global::GameOverScreen), "runInfoUI")!;
        private static readonly FieldInfo FinalScoreLabelField = AccessTools.Field(typeof(global::GameOverScreen), "finalScoreStatLabel")!;
        private static readonly FieldInfo EndlessBattlesLabelField = AccessTools.Field(typeof(global::GameOverScreen), "battleScoresEndlessLabel")!;
        private static readonly FieldInfo GoldUIField = AccessTools.Field(typeof(global::GameOverScreen), "goldUI")!;
        private static readonly FieldInfo MainClassInfoField = AccessTools.Field(typeof(global::GameOverScreen), "mainClassInfo")!;
        private static readonly FieldInfo SubClassInfoField = AccessTools.Field(typeof(global::GameOverScreen), "subClassInfo")!;
        private static readonly FieldInfo ProgressionObjectiveUIsField = AccessTools.Field(typeof(global::GameOverScreen), "progressionObjectiveUIs")!;
        private static readonly FieldInfo BattleScoreUIsField = AccessTools.Field(typeof(global::GameOverScreen), "battleScoreUIs")!;
        private static readonly FieldInfo BackButtonField = AccessTools.Field(typeof(global::GameOverScreen), "backButton")!;
        private static readonly FieldInfo RestartButtonField = AccessTools.Field(typeof(global::GameOverScreen), "restartButton")!;
        private static readonly FieldInfo EndlessButtonField = AccessTools.Field(typeof(global::GameOverScreen), "endlessButton")!;
        private static readonly FieldInfo RunSummaryButtonField = AccessTools.Field(typeof(global::GameOverScreen), "runSummaryButton")!;
        private static readonly FieldInfo BackButtonLabelField = AccessTools.Field(typeof(global::GameOverScreen), "backButtonLabel")!;

        private readonly global::GameOverScreen _screen;
        private bool _suppressNextRestoredDuplicateFocus;
        private string _suppressedRestoredFocusText;

        public GameOverScreen(global::GameOverScreen screen)
        {
            _screen = screen;
            RootList.AnnouncePosition = false;
        }

        public override void OnPush()
        {
            base.OnPush();
            SyncNavigationSelection();
        }

        public override bool ShouldRestoreNavigationFocus() => false;

        public override bool ShouldAnnounceFocus(UIElement element)
        {
            if (_suppressNextRestoredDuplicateFocus)
            {
                _suppressNextRestoredDuplicateFocus = false;
                string text = element?.GetFocusString();
                if (string.Equals(text, _suppressedRestoredFocusText, StringComparison.Ordinal))
                {
                    _suppressedRestoredFocusText = null;
                    return false;
                }

                _suppressedRestoredFocusText = null;
            }

            return base.ShouldAnnounceFocus(element);
        }

        protected override void RestoreFocusAfterRebuild(int oldIndex, UIElement oldFocused)
        {
            string oldText = oldFocused?.GetFocusString();
            base.RestoreFocusAfterRebuild(oldIndex, oldFocused);

            string newText = RootList.FocusedChild?.GetFocusString();
            if (!string.IsNullOrEmpty(oldText) && string.Equals(oldText, newText, StringComparison.Ordinal))
            {
                _suppressNextRestoredDuplicateFocus = true;
                _suppressedRestoredFocusText = newText;
            }
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            SyncNavigationSelection();
        }

        protected override void PopulateList()
        {
            AddText(Get<TMP_Text>(_screen, TitleLabelField));

            global::RunInfo runInfo = Get<global::RunInfo>(_screen, RunInfoUIField);
            AddRunInfo(runInfo);

            AddStat(Get<TMP_Text>(_screen, FinalScoreLabelField), Message.Localized("ui", "GAME_OVER.FINAL_SCORE"));
            AddStat(Get<TMP_Text>(_screen, EndlessBattlesLabelField), Message.Localized("ui", "GAME_OVER.ENDLESS_BATTLES"));
            AddGold(Get<global::GoldScoreModifierDisplay>(_screen, GoldUIField));
            AddClassInfo(Get<global::GameOverClassInfoUI>(_screen, MainClassInfoField));
            AddClassInfo(Get<global::GameOverClassInfoUI>(_screen, SubClassInfoField));
            AddProgression(Get<List<global::ProgressionObjectiveUI>>(_screen, ProgressionObjectiveUIsField));
            AddBattles(Get<List<global::BattleScoreUI>>(_screen, BattleScoreUIsField));

            AddButton(Get<GameUISelectableButton>(_screen, BackButtonField), labelText: Get<TMP_Text>(_screen, BackButtonLabelField));
            AddButton(Get<GameUISelectableButton>(_screen, RestartButtonField), fallbackKey: "GAME_OVER.RESTART");
            AddButton(Get<GameUISelectableButton>(_screen, RunSummaryButtonField), fallbackKey: "GAME_OVER.RUN_SUMMARY");
            AddButton(Get<GameUISelectableButton>(_screen, EndlessButtonField), fallbackKey: "GAME_OVER.PLAY_ENDLESS");
        }

        protected override string BuildSignature()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            ProxyGameOverText.AppendSignature(sb, Get<TMP_Text>(_screen, TitleLabelField));
            ProxyGameOverText.AppendSignature(sb, Get<TMP_Text>(_screen, FinalScoreLabelField));
            ProxyGameOverText.AppendSignature(sb, Get<TMP_Text>(_screen, EndlessBattlesLabelField));

            List<global::BattleScoreUI> battles = Get<List<global::BattleScoreUI>>(_screen, BattleScoreUIsField);
            sb.Append(battles != null ? battles.Count : 0).Append('|');
            if (battles != null)
            {
                for (int i = 0; i < battles.Count; i++)
                {
                    ProxyGameOverBattleScore.AppendSignature(sb, battles[i]);
                }
            }

            List<global::ProgressionObjectiveUI> progressionObjectives = Get<List<global::ProgressionObjectiveUI>>(_screen, ProgressionObjectiveUIsField);
            sb.Append(progressionObjectives != null ? progressionObjectives.Count : 0).Append('|');
            if (progressionObjectives != null)
            {
                for (int i = 0; i < progressionObjectives.Count; i++)
                {
                    ProxyGameOverProgressionObjective.AppendSignature(sb, progressionObjectives[i]);
                }
            }

            ProxyGameOverButton.AppendSignature(sb, Get<GameUISelectableButton>(_screen, BackButtonField));
            ProxyGameOverButton.AppendSignature(sb, Get<GameUISelectableButton>(_screen, RestartButtonField));
            ProxyGameOverButton.AppendSignature(sb, Get<GameUISelectableButton>(_screen, RunSummaryButtonField));
            ProxyGameOverButton.AppendSignature(sb, Get<GameUISelectableButton>(_screen, EndlessButtonField));
            return sb.ToString();
        }

        private void AddRunInfo(global::RunInfo runInfo)
        {
            if (runInfo == null)
            {
                return;
            }

            AddRunInfoText(runInfo, GameOverRunInfoTextPart.RunType);
            AddRunInfoText(runInfo, GameOverRunInfoTextPart.Points);
            AddRunInfoText(runInfo, GameOverRunInfoTextPart.Playtime);
            AddRunInfoTooltip(runInfo, GameOverRunInfoTooltipPart.Covenant);
            AddRunInfoTooltip(runInfo, GameOverRunInfoTooltipPart.Difficulty);
            AddRunInfoTooltip(runInfo, GameOverRunInfoTooltipPart.Distance);
            AddRunInfoTooltip(runInfo, GameOverRunInfoTooltipPart.EndlessDistance);
            AddRunInfoTooltip(runInfo, GameOverRunInfoTooltipPart.WinStreak);
            AddRunInfoTooltip(runInfo, GameOverRunInfoTooltipPart.TrueFinalBossWinStreak);
            ProxyGameOverRunInfoClasses classes = new ProxyGameOverRunInfoClasses(runInfo);
            AddElement(classes, classes.Target, classes.Selectable?.component != null ? classes.Selectable.component.gameObject : null);
        }

        private void AddGold(global::GoldScoreModifierDisplay gold)
        {
            if (gold == null)
            {
                return;
            }

            AddElement(new ProxyGameOverGold(gold), gold.gameObject);
        }

        private void AddClassInfo(global::GameOverClassInfoUI classInfo)
        {
            if (classInfo == null)
            {
                return;
            }

            AddElement(new ProxyGameOverClassInfo(classInfo), classInfo.gameObject);
        }

        private void AddProgression(List<global::ProgressionObjectiveUI> objectives)
        {
            if (objectives == null)
            {
                return;
            }

            for (int i = 0; i < objectives.Count; i++)
            {
                global::ProgressionObjectiveUI objective = objectives[i];
                if (objective == null)
                {
                    continue;
                }

                AddElement(new ProxyGameOverProgressionObjective(objective), objective.gameObject);
            }
        }

        private void AddBattles(List<global::BattleScoreUI> battles)
        {
            if (battles == null)
            {
                return;
            }

            for (int i = 0; i < battles.Count; i++)
            {
                global::BattleScoreUI battle = battles[i];
                if (battle == null)
                {
                    continue;
                }

                AddElement(new ProxyGameOverBattleScore(battle), battle.gameObject);
            }
        }

        private void AddStat(TMP_Text value, Message prefix)
        {
            if (value == null)
            {
                return;
            }

            AddElement(new ProxyGameOverText(value, prefix), value.gameObject);
        }

        private void AddText(TMP_Text text)
        {
            if (text == null)
            {
                return;
            }

            AddElement(new ProxyGameOverText(text), text.gameObject);
        }

        private void AddRunInfoText(global::RunInfo runInfo, GameOverRunInfoTextPart part)
        {
            ProxyGameOverRunInfoText element = new ProxyGameOverRunInfoText(runInfo, part);
            AddElement(element, element.Target);
        }

        private void AddRunInfoTooltip(global::RunInfo runInfo, GameOverRunInfoTooltipPart part)
        {
            ProxyGameOverRunInfoTooltip element = new ProxyGameOverRunInfoTooltip(runInfo, part);
            AddElement(element, element.Target);
        }

        private void AddButton(GameUISelectableButton button, string fallbackKey = null, TMP_Text labelText = null)
        {
            if (button == null)
            {
                return;
            }

            ProxyGameOverButton element = new ProxyGameOverButton(button, fallbackKey, labelText);
            AddElement(element, button.gameObject);
        }

        private void SyncNavigationSelection()
        {
            UIElement focused = RootList.FocusedChild;
            if (focused is ProxyGameOverButton)
            {
                return;
            }

            GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (selected != null)
            {
                ClearGameSelection();
                UIElement refreshedFocus = RootList.FocusedChild;
                if (refreshedFocus != null)
                {
                    UIManager.SetFocusedElement(refreshedFocus);
                }
            }
        }

        private static void ClearGameSelection()
        {
            global::InputManager.Inst?.SelectGameUIComponent(null);
        }

        private static T Get<T>(object owner, FieldInfo field) where T : class
        {
            return owner != null ? field.GetValue(owner) as T : null;
        }
    }
}

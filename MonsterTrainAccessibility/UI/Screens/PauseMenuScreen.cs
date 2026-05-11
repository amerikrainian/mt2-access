using System;
using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI;
using MonsterTrainAccessibility.UI.Elements;
using ShinyShoe;
using UnityEngine;

namespace MonsterTrainAccessibility.UI.Screens
{
    internal sealed class PauseMenuScreen : GameScreen
    {
        private static readonly string[] ButtonFields =
        {
            "settingsButton",
            "compendiumButton",
            "runHistoryButton",
            "creditsButton",
            "discordButton",
            "quitButton",
            "mainMenuButton",
            "dimensionalPortalButton",
            "riftStationButton",
            "abandonRunButton",
            "quickRestartButton",
            "restartBattleButton",
            "lockedRestartBattleButton"
        };

        private static readonly FieldInfo RunInfoField = AccessTools.Field(typeof(global::PauseDialog), "runInfo")!;
        private static readonly FieldInfo ClassesUIField = AccessTools.Field(typeof(global::RunInfo), "classesUI")!;
        private static readonly FieldInfo CovenantUIField = AccessTools.Field(typeof(global::RunInfo), "covenantUI")!;
        private static readonly FieldInfo DifficultyTierUIField = AccessTools.Field(typeof(global::RunInfo), "difficultyTierUI")!;
        private static readonly FieldInfo DistanceUIField = AccessTools.Field(typeof(global::RunInfo), "distanceUI")!;
        private static readonly FieldInfo EndlessDistanceUIField = AccessTools.Field(typeof(global::RunInfo), "endlessDistanceUI")!;

        private readonly global::PauseDialog _pauseDialog;
        private readonly Func<bool> _shouldStayActive;

        public PauseMenuScreen(global::PauseDialog pauseDialog, Func<bool> shouldStayActive = null)
        {
            _pauseDialog = pauseDialog;
            _shouldStayActive = shouldStayActive;
        }

        public override void OnPush()
        {
            base.OnPush();
            (RootElement as ListContainer)?.FocusFirst();
        }

        public override void OnFocus()
        {
            base.OnFocus();
            ListContainer root = RootElement as ListContainer;
            if (root == null)
            {
                return;
            }

            if (root.FocusIndex >= 0)
            {
                root.SetFocusIndex(root.FocusIndex);
                return;
            }

            root.FocusFirst();
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (_shouldStayActive != null && !_shouldStayActive())
            {
                Parent?.RemoveChild(this);
            }
        }

        protected override void BuildRegistry()
        {
            ListContainer root = new ListContainer
            {
                AnnounceName = false,
                AnnouncePosition = false,
                NavigationAxis = NavigationAxis.Vertical
            };
            RootElement = root;

            AddRunInfo(root);
            for (int i = 0; i < ButtonFields.Length; i++)
            {
                string fieldName = ButtonFields[i];
                FieldInfo field = AccessTools.Field(typeof(global::PauseDialog), fieldName)!;
                GameUISelectableButton button = Get<GameUISelectableButton>(_pauseDialog, field);
                if (button == null)
                {
                    continue;
                }

                LabeledButton element = new LabeledButton(button, () => ResolvePauseButtonLabel(fieldName, button));
                AddToContainer(root, button.gameObject, element);
            }
        }

        private void AddRunInfo(ListContainer root)
        {
            global::RunInfo runInfo = Get<global::RunInfo>(_pauseDialog, RunInfoField);
            if (runInfo == null || !runInfo.gameObject.activeInHierarchy)
            {
                return;
            }

            ListContainer row = new ListContainer
            {
                AnnounceName = false,
                AnnouncePosition = false,
                NavigationAxis = NavigationAxis.Horizontal
            };

            AddClasses(row, Get<global::ChosenClassesUI>(runInfo, ClassesUIField));
            AddTooltip(row, Get<global::ChallengeCovenantUI>(runInfo, CovenantUIField), "HUD.COVENANT");
            AddTooltip(row, Get<global::DifficultyTierUI>(runInfo, DifficultyTierUIField), "HUD.DIFFICULTY");
            AddTooltip(row, Get<global::RunDistanceDisplay>(runInfo, DistanceUIField), "HUD.DISTANCE");
            AddTooltip(row, Get<global::EndlessRunDistanceDisplay>(runInfo, EndlessDistanceUIField), "HUD.DISTANCE");

            if (row.Children.Count > 0)
            {
                root.Add(row);
                Register(runInfo.gameObject, row);
            }
        }

        private void AddClasses(ListContainer row, global::ChosenClassesUI classes)
        {
            if (classes == null || !classes.gameObject.activeInHierarchy)
            {
                return;
            }

            ProxyClassesUI element = new ProxyClassesUI(classes);
            row.Add(element);
            Register(classes.gameObject, element);
            Register(classes.SelectableUI?.component != null ? classes.SelectableUI.component.gameObject : null, element);
        }

        private void AddTooltip(ListContainer row, Component component, string fallbackLabelKey)
        {
            if (component == null || !component.gameObject.activeInHierarchy)
            {
                return;
            }

            ProxyTooltipInfo element = new ProxyTooltipInfo(component, fallbackLabelKey);
            row.Add(element);
            Register(component.gameObject, element);
        }

        internal static Message ResolvePauseButtonLabel(string fieldName, GameUISelectableButton button)
        {
            string label = NormalizePauseButtonLabel(button);
            if (IsUsableLabel(label))
            {
                return Message.RawCleaned(label);
            }

            return FallbackButtonLabel(fieldName);
        }

        private static string NormalizePauseButtonLabel(GameUISelectableButton button)
        {
            string label = Message.Clean(GameUIButtonSupport.ResolveLabel(button));
            if (string.IsNullOrWhiteSpace(label))
            {
                label = Message.Clean(AuthoredLabelReader.Read(button));
            }

            string role = LocalizationManager.Get("role.button");
            if (string.IsNullOrWhiteSpace(label) || string.IsNullOrWhiteSpace(role))
            {
                return label;
            }

            string suffix = " " + role;
            return label.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)
                ? label.Substring(0, label.Length - suffix.Length).Trim()
                : label;
        }

        private static bool IsUsableLabel(string label)
        {
            if (string.IsNullOrWhiteSpace(label))
            {
                return false;
            }

            string role = LocalizationManager.Get("role.button");
            return !string.Equals(label, "Button", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(label, role, StringComparison.OrdinalIgnoreCase);
        }

        private static Message FallbackButtonLabel(string fieldName)
        {
            switch (fieldName)
            {
                case "settingsButton":
                    return Message.Localized("ui", "HUD.SETTINGS");
                case "compendiumButton":
                    return Message.Localized("ui", "PAUSE.COMPENDIUM");
                case "runHistoryButton":
                    return Message.Localized("ui", "PAUSE.RUN_HISTORY");
                case "creditsButton":
                    return Message.Localized("ui", "PAUSE.CREDITS");
                case "discordButton":
                    return Message.Localized("ui", "PAUSE.DISCORD");
                case "quitButton":
                    return Message.Localized("ui", "PAUSE.QUIT");
                case "mainMenuButton":
                    return Message.Localized("ui", "PAUSE.MAIN_MENU");
                case "dimensionalPortalButton":
                    return Message.Localized("ui", "PAUSE.DIMENSIONAL_PORTAL");
                case "riftStationButton":
                    return Message.Localized("ui", "PAUSE.RIFT_STATION");
                case "abandonRunButton":
                    return Message.Localized("ui", "PAUSE.ABANDON_RUN");
                case "quickRestartButton":
                    return Message.Localized("ui", "PAUSE.QUICK_RESTART");
                case "restartBattleButton":
                case "lockedRestartBattleButton":
                    return Message.Localized("ui", "PAUSE.RESTART_BATTLE");
                default:
                    return null;
            }
        }

        private static T Get<T>(object owner, FieldInfo field) where T : class
        {
            return owner != null ? field.GetValue(owner) as T : null;
        }
    }
}

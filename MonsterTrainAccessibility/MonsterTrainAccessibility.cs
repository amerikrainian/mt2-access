using BepInEx;
using HarmonyLib;
using MonsterTrainAccessibility.Buffers;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Events;
using MonsterTrainAccessibility.Input;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Patches;
using MonsterTrainAccessibility.Speech;
using MonsterTrainAccessibility.Updates;
using MonsterTrainAccessibility.UI.Elements;
using MonsterTrainAccessibility.UI.Screens;
using ModInputManager = MonsterTrainAccessibility.Input.InputManager;
using ModScreenManager = MonsterTrainAccessibility.UI.Screens.ScreenManager;
using ScreenRegistration = MonsterTrainAccessibility.UI.Screens.ScreenRegistration;

namespace MonsterTrainAccessibility
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInProcess("MonsterTrain2.exe")]
    public class MonsterTrainAccessibility : BaseUnityPlugin
    {
        public const string GUID = "com.accessibility.monstertrain";
        public const string NAME = "Monster Train Accessibility";
        public const string VERSION = "0.1.8";

        private Harmony _harmony;
        private AccessibleParamsManager _accessibleParamsManager;
        private bool _quitting;

        private void Awake()
        {
            try
            {
                Log.Source = Logger;
                Log.Info($"{NAME} {VERSION} loading");

                InstallerState.WriteInstalledVersion(VERSION);
                Settings.Initialize(Config);
                UpdateSettings.Register(Config);
                LocalizationManager.Initialize();
                EventRegistry.Initialize(Config);
                ElementSettingsRegistry.Initialize(Config);
                RegisterAccessibilityParamsManager();
                BufferManager.Instance.RegisterDefaults();
                SpeechManager.Initialize();
                UIManager.Initialize();
                ModScreenManager.Initialize();
                ScreenRegistration.RegisterAll();
                ModInputManager.Initialize();
                InputBindingSettings.Register(Config);
                if (UpdateSettings.CheckUpdatesOnLaunch?.Value == true)
                {
                    UpdateChecker.Run();
                }

                _harmony = new Harmony(GUID);
                RegisterPatches();

                Log.Info($"{NAME} ready");
            }
            catch (System.Exception ex)
            {
                Log.Error($"{NAME} failed during Awake: {ex}");
                throw;
            }
        }

        private void RegisterPatches()
        {
            HarmonyHelper.PatchIfFound(
                _harmony,
                typeof(global::StatusEffectsDisplayData),
                "GetTMPSpriteTag",
                prefix: new HarmonyMethod(typeof(SemanticLocalizationHooks), nameof(SemanticLocalizationHooks.StatusEffectsDisplayData_GetTMPSpriteTag_Prefix)),
                argTypes: new[] { typeof(global::StatusEffectsDisplayData.IconDisplayData) });

            HarmonyHelper.PatchIfFound(
                _harmony,
                typeof(global::TooltipUI),
                "FormatTitleWithIcon",
                prefix: new HarmonyMethod(typeof(SemanticLocalizationHooks), nameof(SemanticLocalizationHooks.TooltipUI_FormatTitleWithIcon_Prefix)),
                argTypes: new[] { typeof(string), typeof(string) });

            HarmonyHelper.PatchIfFound(
                _harmony,
                typeof(global::UIScreen),
                "SetScreenActive",
                postfix: new HarmonyMethod(typeof(ScreenHooks), nameof(ScreenHooks.UIScreen_SetScreenActive_Postfix)),
                argTypes: new[] { typeof(bool) });

            HarmonyHelper.PatchIfFound(
                _harmony,
                typeof(global::ScreenTransition),
                "SetActive",
                postfix: new HarmonyMethod(typeof(ScreenHooks), nameof(ScreenHooks.ScreenTransition_SetActive_Postfix)),
                argTypes: new[] { typeof(bool), typeof(UnityEngine.GameObject), typeof(System.Action), typeof(System.Action) });

            HarmonyHelper.PatchIfFound(
                _harmony,
                typeof(global::ShinyShoe.GameUISelectableCommonExtensions),
                "OnSelectCommon",
                postfix: new HarmonyMethod(typeof(SelectionHooks), nameof(SelectionHooks.GameUISelectableCommonExtensions_OnSelectCommon_Postfix)),
                argTypes: new[] { typeof(global::ShinyShoe.IGameUISelectableCommon), typeof(UnityEngine.EventSystems.BaseEventData) });

            HarmonyHelper.PatchIfFound(
                _harmony,
                typeof(global::ShinyShoe.GameInputModuleBridge),
                "GetAxisRaw",
                postfix: new HarmonyMethod(typeof(NavigationInputHooks), nameof(NavigationInputHooks.GameInputModuleBridge_GetAxisRaw_Postfix)),
                argTypes: new[] { typeof(string) });

            HarmonyHelper.PatchIfFound(
                _harmony,
                typeof(UnityEngine.EventSystems.StandaloneInputModule),
                "SendMoveEventToSelectedObject",
                prefix: new HarmonyMethod(typeof(NavigationInputHooks), nameof(NavigationInputHooks.StandaloneInputModule_SendMoveEventToSelectedObject_Prefix)));

            HarmonyHelper.PatchIfFound(
                _harmony,
                typeof(UnityEngine.EventSystems.StandaloneInputModule),
                "SendSubmitEventToSelectedObject",
                prefix: new HarmonyMethod(typeof(NavigationInputHooks), nameof(NavigationInputHooks.StandaloneInputModule_SendSubmitEventToSelectedObject_Prefix)));

            HarmonyHelper.PatchIfFound(
                _harmony,
                typeof(global::ShinyShoe.CoreInputDriverKeyboard),
                "OnLateUpdate",
                prefix: new HarmonyMethod(typeof(InputHooks), nameof(InputHooks.CoreInputDriverKeyboard_OnLateUpdate_Prefix)));

            HarmonyHelper.PatchIfFound(
                _harmony,
                typeof(global::ShinyShoe.CoreInputDriverGamepad),
                "OnLateUpdate",
                postfix: new HarmonyMethod(typeof(InputHooks), nameof(InputHooks.CoreInputDriverGamepad_OnLateUpdate_Postfix)));

            HarmonyHelper.PatchIfFound(
                _harmony,
                typeof(global::ScreenManager),
                "OnGameUISignaled",
                prefix: new HarmonyMethod(typeof(ScreenInputGateHooks), nameof(ScreenInputGateHooks.ScreenManager_OnGameUISignaled_Prefix)),
                argTypes: new[] { typeof(global::ShinyShoe.CoreInputControlMapping), typeof(global::ShinyShoe.IGameUIComponent) });

            HarmonyHelper.PatchIfFound(
                _harmony,
                typeof(global::InputManager),
                "SelectGameUIComponent",
                prefix: new HarmonyMethod(typeof(CompendiumSearchInputHooks), nameof(CompendiumSearchInputHooks.InputManager_SelectGameUIComponent_Prefix)),
                argTypes: new[] { typeof(global::ShinyShoe.IGameUIComponent), typeof(bool), typeof(bool) });

            HarmonyHelper.PatchIfFound(
                _harmony,
                typeof(global::InputFieldContainer),
                "ApplyScreenInput",
                prefix: new HarmonyMethod(typeof(CompendiumSearchInputHooks), nameof(CompendiumSearchInputHooks.InputFieldContainer_ApplyScreenInput_Prefix)),
                postfix: new HarmonyMethod(typeof(CompendiumSearchInputHooks), nameof(CompendiumSearchInputHooks.InputFieldContainer_ApplyScreenInput_Postfix)),
                argTypes: new[] { typeof(global::ShinyShoe.CoreInputControlMapping), typeof(global::ShinyShoe.IGameUIComponent), typeof(global::InputManager.Controls) });

            HarmonyHelper.PatchIfFound(
                _harmony,
                typeof(global::KeyMappingScreen),
                "DetectBindingKeyPress",
                prefix: new HarmonyMethod(typeof(KeyMappingInputHooks), nameof(KeyMappingInputHooks.KeyMappingScreen_DetectBindingKeyPress_Prefix)),
                postfix: new HarmonyMethod(typeof(KeyMappingInputHooks), nameof(KeyMappingInputHooks.KeyMappingScreen_DetectBindingKeyPress_Postfix)));

            HarmonyHelper.PatchIfFound(
                _harmony,
                typeof(global::Hud),
                "ApplyScreenInput",
                prefix: new HarmonyMethod(typeof(ForgeToggleHooks), nameof(ForgeToggleHooks.Hud_ApplyScreenInput_Prefix)),
                postfix: new HarmonyMethod(typeof(ForgeToggleHooks), nameof(ForgeToggleHooks.Hud_ApplyScreenInput_Postfix)),
                argTypes: new[] { typeof(global::ShinyShoe.CoreInputControlMapping), typeof(global::ShinyShoe.IGameUIComponent), typeof(global::InputManager.Controls) });

            HarmonyHelper.PatchIfFound(
                _harmony,
                typeof(global::RunHistoryUI),
                "ApplyScreenInput",
                prefix: new HarmonyMethod(typeof(RunHistoryInputHooks), nameof(RunHistoryInputHooks.RunHistoryUI_ApplyScreenInput_Prefix)),
                argTypes: new[] { typeof(global::ShinyShoe.CoreInputControlMapping), typeof(global::ShinyShoe.IGameUIComponent), typeof(global::InputManager.Controls) });

            HarmonyHelper.PatchIfFound(
                _harmony,
                typeof(global::RunHistoryScreen),
                "ApplyScreenInput",
                prefix: new HarmonyMethod(typeof(RunHistoryInputHooks), nameof(RunHistoryInputHooks.RunHistoryScreen_ApplyScreenInput_Prefix)),
                argTypes: new[] { typeof(global::ShinyShoe.CoreInputControlMapping), typeof(global::ShinyShoe.IGameUIComponent), typeof(global::InputManager.Controls) });

            HarmonyHelper.PatchIfFound(
                _harmony,
                typeof(global::ChallengeProgressScreen),
                "ApplyScreenInput",
                prefix: new HarmonyMethod(typeof(ChallengeProgressInputHooks), nameof(ChallengeProgressInputHooks.ChallengeProgressScreen_ApplyScreenInput_Prefix)),
                argTypes: new[] { typeof(global::ShinyShoe.CoreInputControlMapping), typeof(global::ShinyShoe.IGameUIComponent), typeof(global::InputManager.Controls) });

            HarmonyHelper.PatchIfFound(
                _harmony,
                typeof(global::MutatorSelectionUI),
                "ApplyScreenInput",
                prefix: new HarmonyMethod(typeof(ChallengeMutatorInputHooks), nameof(ChallengeMutatorInputHooks.MutatorSelectionUI_ApplyScreenInput_Prefix)),
                argTypes: new[] { typeof(global::ShinyShoe.CoreInputControlMapping), typeof(global::ShinyShoe.IGameUIComponent), typeof(global::InputManager.Controls) });

            HarmonyHelper.PatchIfFound(
                _harmony,
                typeof(global::ChallengeDetailsScreen),
                "SetLoading",
                postfix: new HarmonyMethod(typeof(ChallengeDetailsLoadHooks), nameof(ChallengeDetailsLoadHooks.ChallengeDetailsScreen_SetLoading_Postfix)),
                argTypes: new[] { typeof(bool) });

            HarmonyHelper.PatchIfFound(
                _harmony,
                typeof(global::ChallengeDetailsScreen),
                "HandleChallengeData",
                postfix: new HarmonyMethod(typeof(ChallengeDetailsLoadHooks), nameof(ChallengeDetailsLoadHooks.ChallengeDetailsScreen_HandleChallengeData_Postfix)),
                argTypes: new[] { typeof(global::ChallengeData) });

            HarmonyHelper.PatchIfFound(
                _harmony,
                typeof(global::AbilityCounterUI),
                "MoveCameraToNearestAbilityUnit",
                postfix: new HarmonyMethod(typeof(BattleAbilityHooks), nameof(BattleAbilityHooks.AbilityCounterUI_MoveCameraToNearestAbilityUnit_Postfix)));

            HarmonyHelper.PatchIfFound(
                _harmony,
                typeof(global::HandUI),
                "InputSubmit",
                prefix: new HarmonyMethod(typeof(BattleAbilityHooks), nameof(BattleAbilityHooks.HandUI_InputSubmit_Prefix)));

            HarmonyHelper.PatchIfFound(
                _harmony,
                typeof(global::LanguageManager),
                "HandleLanguageChanged",
                postfix: new HarmonyMethod(typeof(LocalizationHooks), nameof(LocalizationHooks.LanguageManager_HandleLanguageChanged_Postfix)),
                argTypes: new[] { typeof(string) });

            HarmonyHelper.PatchIfFound(
                _harmony,
                typeof(global::CompendiumScreen),
                "ShowChecklistChanges",
                postfix: new HarmonyMethod(typeof(CompendiumChecklistHooks), nameof(CompendiumChecklistHooks.CompendiumScreen_ShowChecklistChanges_Postfix)),
                argTypes: new[] { typeof(System.Collections.Generic.IReadOnlyList<global::ChecklistChangeData>) });

            HarmonyHelper.PatchIfFound(
                _harmony,
                typeof(global::CompendiumEnemyDetailsUI),
                "Set",
                postfix: new HarmonyMethod(typeof(CompendiumEnemyDetailsHooks), nameof(CompendiumEnemyDetailsHooks.CompendiumEnemyDetailsUI_Set_Postfix)),
                argTypes: new[]
                {
                    typeof(global::EnemyLogbookSortData),
                    typeof(global::EnemyLogbookSortData.LogbookEnemyEntry),
                    typeof(bool),
                    typeof(global::CompendiumScreen),
                    typeof(global::AllGameManagers),
                    typeof(global::CharacterData),
                    typeof(int),
                    typeof(int),
                    typeof(int),
                    typeof(int),
                    typeof(global::CovenantData),
                    typeof(int)
                });

            HarmonyHelper.PatchIfFound(
                _harmony,
                typeof(global::UnlockScreen),
                "GetFeatureUnlockItems",
                postfix: new HarmonyMethod(typeof(CompendiumChecklistHooks), nameof(CompendiumChecklistHooks.UnlockScreen_GetFeatureUnlockItems_Postfix)),
                argTypes: new[] { typeof(global::AllGameData), typeof(System.Collections.Generic.List<global::MetagameSaveData.UnlockedFeatureData>) });

        }

        private void OnApplicationQuit()
        {
            _quitting = true;
            Log.Info("Application quitting, cleaning up");

            ModScreenManager.Shutdown();
            BufferManager.Instance.Shutdown();
            UIManager.Shutdown();
            ModInputManager.Shutdown();
            SpeechManager.Shutdown();
            UnregisterAccessibilityParamsManager();
            LocalizationManager.Shutdown();
            _harmony?.UnpatchSelf();
        }

        private void OnDestroy()
        {
            if (_quitting)
            {
                return;
            }

            Log.Info("Plugin OnDestroy (scene transition, not cleaning up)");
        }

        private void RegisterAccessibilityParamsManager()
        {
            _accessibleParamsManager = new AccessibleParamsManager();
            if (!global::I2.Loc.LocalizationManager.ParamManagers.Contains(_accessibleParamsManager))
            {
                global::I2.Loc.LocalizationManager.ParamManagers.Insert(0, _accessibleParamsManager);
            }
        }

        private void UnregisterAccessibilityParamsManager()
        {
            if (_accessibleParamsManager == null)
            {
                return;
            }

            global::I2.Loc.LocalizationManager.ParamManagers.Remove(_accessibleParamsManager);
            _accessibleParamsManager = null;
        }
    }
}

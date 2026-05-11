using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Input;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Elements;
using ShinyShoe;
using UnityEngine;

namespace MonsterTrainAccessibility.UI.Screens
{
    internal sealed class RegionSelectionScreen : GameScreen
    {
        private static readonly FieldInfo SaveManagerField = AccessTools.Field(typeof(global::RegionSelectionScreen), "saveManager")!;
        private static readonly FieldInfo RegionButtonsField = AccessTools.Field(typeof(global::RegionSelectionScreen), "regionButtons")!;
        private static readonly FieldInfo FinalBossButtonField = AccessTools.Field(typeof(global::RegionSelectionScreen), "finalBossButton")!;
        private static readonly FieldInfo RegionRewardsDisplaysField = AccessTools.Field(typeof(global::RegionSelectionScreen), "regionRewardsDisplays")!;
        private static readonly FieldInfo MutatorRewardDisplayField = AccessTools.Field(typeof(global::RegionSelectionScreen), "mutatorRewardDisplay")!;
        private static readonly FieldInfo ContinueButtonField = AccessTools.Field(typeof(global::RegionSelectionScreen), "continueButton")!;
        private static readonly FieldInfo SelectedRegionButtonField = AccessTools.Field(typeof(global::RegionSelectionScreen), "selectedRegionButton")!;
        private static readonly FieldInfo FinalBossUnlockTitleKeyField = AccessTools.Field(typeof(global::RegionSelectionScreen), "finalBossUnlockTitleKey")!;
        private static readonly FieldInfo FinalBossUnlockBodyKeyField = AccessTools.Field(typeof(global::RegionSelectionScreen), "finalBossUnlockBodyKey")!;
        private static readonly MethodInfo SelectRegionButtonMethod = AccessTools.Method(typeof(global::RegionSelectionScreen), "SelectRegionButton")!;
        private static readonly MethodInfo SelectFinalBossButtonMethod = AccessTools.Method(typeof(global::RegionSelectionScreen), "SelectFinalBossButton")!;

        private readonly global::RegionSelectionScreen _screen;

        public RegionSelectionScreen(global::RegionSelectionScreen screen)
        {
            _screen = screen;
        }

        public override void OnPush()
        {
            base.OnPush();
            (RootElement as ListContainer)?.FocusFirst();
        }

        public override bool ShouldAcceptGameSelection() => false;

        public override bool BlocksGameInput(InputAction action)
        {
            if (action?.Key == "ui_accept" || action?.Key == "ui_select")
            {
                return true;
            }

            return base.BlocksGameInput(action);
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

            SaveManager saveManager = Get<SaveManager>(_screen, SaveManagerField);
            List<global::RegionSelectionButton> regionButtons = Get<List<global::RegionSelectionButton>>(_screen, RegionButtonsField);
            List<ScenarioData> regionScenarios = RegionScenarios(saveManager);

            if (regionButtons != null)
            {
                for (int i = 0; i < regionButtons.Count; i++)
                {
                    global::RegionSelectionButton button = regionButtons[i];
                    ScenarioData scenario = ScenarioForRegion(button, regionScenarios);
                    AddRegionButton(root, button, scenario, saveManager, finalBoss: false);
                }
            }

            global::RegionSelectionFinalBossButton finalBossButton =
                Get<global::RegionSelectionFinalBossButton>(_screen, FinalBossButtonField);
            AddRegionButton(root, finalBossButton, FinalBossScenario(saveManager), saveManager, finalBoss: true);

            List<global::RegionSelectionRewardDisplay> displays =
                Get<List<global::RegionSelectionRewardDisplay>>(_screen, RegionRewardsDisplaysField);
            ListContainer rewards = new ListContainer(Message.Localized("ui", "REGION_SELECTION.REWARDS").Resolve(), NavigationAxis.Horizontal)
            {
                AnnouncePosition = true
            };
            if (displays != null)
            {
                for (int i = 0; i < displays.Count; i++)
                {
                    int slot = i;
                    ProxyRegionRewardDisplay element = new ProxyRegionRewardDisplay(displays[i], () => RewardForSelectedRegion(slot));
                    rewards.Add(element);
                    Register(displays[i]?.RewardSelectable?.gameObject, element);
                }
            }

            global::RegionSelectionRewardDisplay mutatorDisplay =
                Get<global::RegionSelectionRewardDisplay>(_screen, MutatorRewardDisplayField);
            ProxyRegionMutatorReward mutatorElement = new ProxyRegionMutatorReward(mutatorDisplay, MutatorForSelectedRegion);
            rewards.Add(mutatorElement);
            Register(mutatorDisplay?.RewardSelectable?.gameObject, mutatorElement);
            root.Add(rewards);

            GameUISelectableButton continueButton = Get<GameUISelectableButton>(_screen, ContinueButtonField);
            GameObjectElement continueElement = new GameObjectElement(
                continueButton?.gameObject,
                typeKey: "button",
                label: () => Message.Localized("ui", "REGION_SELECTION.CONTINUE"),
                status: () => GameButtonElement.StateMessage(continueButton));
            root.Add(continueElement);
            Register(continueButton?.gameObject, continueElement);
        }

        private void AddRegionButton(
            ListContainer root,
            global::RegionSelectionButton button,
            ScenarioData scenario,
            SaveManager saveManager,
            bool finalBoss)
        {
            if (root == null || button == null)
            {
                return;
            }

            ProxyRegionSelectionButton element = new ProxyRegionSelectionButton(
                button,
                scenario,
                saveManager,
                finalBoss,
                SelectedRegionButton,
                SelectRegionButton,
                FinalBossLockedTooltip);
            root.Add(element);
            Register(button.Button?.gameObject, element);
            Register(button.gameObject, element);
        }

        private global::RegionSelectionButton SelectedRegionButton()
        {
            return Get<global::RegionSelectionButton>(_screen, SelectedRegionButtonField);
        }

        private void SelectRegionButton(global::RegionSelectionButton button, bool finalBoss)
        {
            if (button == null || _screen == null)
            {
                return;
            }

            try
            {
                if (finalBoss)
                {
                    SelectFinalBossButtonMethod.Invoke(_screen, Array.Empty<object>());
                }
                else
                {
                    SelectRegionButtonMethod.Invoke(_screen, new object[] { button });
                }
            }
            catch (Exception ex)
            {
                Log.Info("[AccessibilityMod] RegionSelection failed to select region "
                    + button.RegionIndex
                    + ": "
                    + ex);
            }
        }

        private Message FinalBossLockedTooltip()
        {
            List<Message> parts = new List<Message>
            {
                Message.FromText(LocalizeKey(GetString(_screen, FinalBossUnlockTitleKeyField))),
                Message.FromText(LocalizeKey(GetString(_screen, FinalBossUnlockBodyKeyField)))
            };
            parts.RemoveAll(part => part == null);
            return parts.Count > 0 ? Message.JoinLines(parts) : null;
        }

        private MapNodeData RewardForSelectedRegion(int slot)
        {
            int region = SelectedRegionIndex();
            if (region < 0)
            {
                return null;
            }

            SaveManager saveManager = Get<SaveManager>(_screen, SaveManagerField);
            List<MapNodeData> rewards = new List<MapNodeData>();
            RegionRunSetupHelper.GetCentralRewardsForRegion(region, saveManager, rewards);
            return slot >= 0 && slot < rewards.Count ? rewards[slot] : null;
        }

        private MutatorData MutatorForSelectedRegion()
        {
            int region = SelectedRegionIndex();
            if (region < 0)
            {
                return null;
            }

            SaveManager saveManager = Get<SaveManager>(_screen, SaveManagerField);
            RegionRunSetupHelper.TryGetMutatorForRegion(region, saveManager, out MutatorData mutator);
            return mutator;
        }

        private int SelectedRegionIndex()
        {
            return SelectedRegionButton()?.RegionIndex ?? -1;
        }

        private static ScenarioData ScenarioForRegion(
            global::RegionSelectionButton button,
            IReadOnlyList<ScenarioData> scenarios)
        {
            if (button == null || scenarios == null)
            {
                return null;
            }

            int index = button.RegionIndex;
            return index >= 0 && index < scenarios.Count ? scenarios[index] : null;
        }

        private static List<ScenarioData> RegionScenarios(SaveManager saveManager)
        {
            List<ScenarioData> scenarios = new List<ScenarioData>();
            if (saveManager == null)
            {
                return scenarios;
            }

            int runLength = saveManager.GetRunLength();
            for (int i = 0; i < runLength; i++)
            {
                ScenarioData scenario = saveManager.GetScenarioData(i);
                if (scenario != null && scenario.GetDifficulty() == ScenarioDifficulty.Boss)
                {
                    scenarios.Add(scenario);
                }
            }

            return scenarios;
        }

        private static ScenarioData FinalBossScenario(SaveManager saveManager)
        {
            if (saveManager == null)
            {
                return null;
            }

            int distance = saveManager.GetRunLength() - 2;
            return distance >= 0 ? saveManager.GetScenarioData(distance) : null;
        }

        private static string LocalizeKey(string key)
        {
            return !string.IsNullOrWhiteSpace(key) ? key.Localize() : string.Empty;
        }

        private static string GetString(object owner, FieldInfo field)
        {
            return field.GetValue(owner) as string ?? string.Empty;
        }

        private static T Get<T>(object owner, FieldInfo field) where T : class
        {
            return owner != null ? field.GetValue(owner) as T : null;
        }
    }
}

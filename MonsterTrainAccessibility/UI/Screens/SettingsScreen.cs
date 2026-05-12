using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.UI.Elements;
using ShinyShoe;
using UnityEngine;
using UnityEngine.UI;

namespace MonsterTrainAccessibility.UI.Screens
{
    internal sealed class SettingsScreen : SettingsControlsScreen
    {
        private static readonly FieldInfo PauseDialogField = AccessTools.Field(typeof(global::SettingsScreen), "pauseDialog")!;
        private static readonly FieldInfo SettingsDialogField = AccessTools.Field(typeof(global::SettingsScreen), "settingsDialog")!;
        private static readonly FieldInfo GlobalVolumeControlField = AccessTools.Field(typeof(global::SettingsDialog), "globalVolumeControl")!;
        private static readonly FieldInfo VolumeSliderField = AccessTools.Field(typeof(global::VolumeControl), "slider")!;
        private static readonly FieldInfo SfxVolumeControlField = AccessTools.Field(typeof(global::SettingsDialog), "sfxVolumeControl")!;
        private static readonly FieldInfo MusicVolumeControlField = AccessTools.Field(typeof(global::SettingsDialog), "musicVolumeControl")!;
        private static readonly FieldInfo BackgroundMuteToggleField = AccessTools.Field(typeof(global::SettingsDialog), "backgroundMuteToggle")!;
        private static readonly FieldInfo TabsField = AccessTools.Field(typeof(global::SettingsDialog), "tabs")!;
        private static readonly FieldInfo GraphicsQualityToggleField = AccessTools.Field(typeof(global::SettingsDialog), "graphicsQualityToggle")!;
        private static readonly FieldInfo ResolutionDropdownField = AccessTools.Field(typeof(global::SettingsDialog), "resolutionDropdown")!;
        private static readonly FieldInfo FullScreenDropdownField = AccessTools.Field(typeof(global::SettingsDialog), "fullScreenDropdown")!;
        private static readonly FieldInfo VsyncToggleField = AccessTools.Field(typeof(global::SettingsDialog), "vsyncToggle")!;
        private static readonly FieldInfo FramerateDropdownField = AccessTools.Field(typeof(global::SettingsDialog), "framerateDropdown")!;
        private static readonly FieldInfo GameSpeedDropdownField = AccessTools.Field(typeof(global::SettingsDialog), "gameSpeedDropdown")!;
        private static readonly FieldInfo DialogueSpeedDropdownField = AccessTools.Field(typeof(global::SettingsDialog), "dialogueSpeedDropdown")!;
        private static readonly FieldInfo RoomScrollToggleField = AccessTools.Field(typeof(global::SettingsDialog), "roomScrollToggle")!;
        private static readonly FieldInfo TitanTrialDefaultToggleField = AccessTools.Field(typeof(global::SettingsDialog), "titanTrialDefaultToggle")!;
        private static readonly FieldInfo ScrollSensitivityControlField = AccessTools.Field(typeof(global::SettingsDialog), "scrollSensitivityControl")!;
        private static readonly FieldInfo LoreTooltipsToggleField = AccessTools.Field(typeof(global::SettingsDialog), "loreTooltipsToggle")!;
        private static readonly FieldInfo KeyMappingButtonField = AccessTools.Field(typeof(global::SettingsDialog), "keyMappingButton")!;
        private static readonly FieldInfo BackgroundScrollToggleField = AccessTools.Field(typeof(global::SettingsDialog), "backgroundScrollToggle")!;
        private static readonly FieldInfo CameraShakeToggleField = AccessTools.Field(typeof(global::SettingsDialog), "cameraShakeToggle")!;
        private static readonly FieldInfo ColorblindModeDropdownField = AccessTools.Field(typeof(global::SettingsDialog), "colorblindModeDropdown")!;
        private static readonly FieldInfo UIScaleDropdownField = AccessTools.Field(typeof(global::SettingsDialog), "uiScaleDropdown")!;
        private static readonly FieldInfo SafeZoneButtonField = AccessTools.Field(typeof(global::SettingsDialog), "safeZoneButton")!;
        private static readonly FieldInfo LanguageDropdownField = AccessTools.Field(typeof(global::SettingsDialog), "languageDropdown")!;
        private static readonly FieldInfo StreamerModeToggleField = AccessTools.Field(typeof(global::SettingsDialog), "streamerModeToggle")!;
        private static readonly FieldInfo GooglyEyesToggleField = AccessTools.Field(typeof(global::SettingsDialog), "googlyEyesToggle")!;
        private static readonly FieldInfo RefreshRateUncapToggleField = AccessTools.Field(typeof(global::SettingsDialog), "refreshRateUncapToggle")!;
        private static readonly FieldInfo ResetSaveButtonField = AccessTools.Field(typeof(global::SettingsDialog), "resetSaveButton")!;

        private readonly global::SettingsScreen _screen;
        private bool _lastSettingsDialogActive;

        public SettingsScreen(global::SettingsScreen screen)
        {
            _screen = screen;
        }

        public override void OnPush()
        {
            base.OnPush();
            SyncChildScreen();
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            SyncChildScreen();
            if (IsSettingsDialogActive())
            {
                global::SettingsDialog dialog = Get<global::SettingsDialog>(_screen, SettingsDialogField);
                if (dialog != null)
                {
                    WireTabNavigation(dialog);
                }
            }
        }

        private void SyncChildScreen()
        {
            bool settingsDialogActive = IsSettingsDialogActive();
            bool shouldHavePauseChild = !settingsDialogActive;
            if (settingsDialogActive == _lastSettingsDialogActive && (ActiveChild != null) == shouldHavePauseChild)
            {
                return;
            }

            if (ActiveChild != null)
            {
                RemoveChild(ActiveChild);
            }

            _lastSettingsDialogActive = settingsDialogActive;
            if (settingsDialogActive)
            {
                return;
            }

            global::PauseDialog pauseDialog = Get<global::PauseDialog>(_screen, PauseDialogField);
            if (pauseDialog != null)
            {
                PushChild(new PauseMenuScreen(pauseDialog, () => !IsSettingsDialogActive()));
            }
        }

        private bool IsSettingsDialogActive()
        {
            global::SettingsDialog dialog = Get<global::SettingsDialog>(_screen, SettingsDialogField);
            return dialog != null && dialog.Active;
        }

        protected override void BuildRegistry()
        {
            ListContainer root = new ListContainer { AnnounceName = false, AnnouncePosition = false };
            RootElement = root;

            global::SettingsDialog dialog = Get<global::SettingsDialog>(_screen, SettingsDialogField);
            if (dialog == null)
            {
                return;
            }

            AddSettingsTabs(root, dialog);
            AddAudioGroup(root, dialog);
            AddGraphicsGroup(root, dialog);
            AddGameplayGroup(root, dialog);
            AddAccessibilityGroup(root, dialog);
            AddOtherGroup(root, dialog);
            WireTabNavigation(dialog);
        }

        private void AddSettingsTabs(ListContainer root, global::SettingsDialog dialog)
        {
            List<global::SettingsTab> tabs = Get<List<global::SettingsTab>>(dialog, TabsField);
            if (tabs == null || tabs.Count == 0)
            {
                return;
            }

            ListContainer tabList = new ListContainer
            {
                AnnounceName = false,
                AnnouncePosition = true,
                NavigationAxis = NavigationAxis.Horizontal
            };

            for (int i = 0; i < tabs.Count; i++)
            {
                ProxySettingsTab element = new ProxySettingsTab(tabs[i]);
                if (element.Button == null)
                {
                    continue;
                }

                tabList.Add(element);
                Register(element.Button.gameObject, element);
            }

            if (tabList.Children.Count > 0)
            {
                root.Add(tabList);
            }
        }

        private void WireTabNavigation(global::SettingsDialog dialog)
        {
            List<global::SettingsTab> tabs = Get<List<global::SettingsTab>>(dialog, TabsField);
            if (tabs == null || tabs.Count == 0)
            {
                return;
            }

            List<IExplicitNavElement> firstSettings = new List<IExplicitNavElement>
            {
                FirstVolumeNavigationElement(Get<global::VolumeControl>(dialog, GlobalVolumeControlField)),
                Get<GameUISelectableToggle>(dialog, GraphicsQualityToggleField),
                Get<GameUISelectableDropdown>(dialog, GameSpeedDropdownField),
                Get<GameUISelectableToggle>(dialog, BackgroundScrollToggleField),
                Get<GameUISelectableDropdown>(dialog, LanguageDropdownField)
            };

            for (int i = 0; i < tabs.Count; i++)
            {
                ProxySettingsTab tabElement = new ProxySettingsTab(tabs[i]);
                GameUISelectableButton tabButton = tabElement.Button;
                IExplicitNavElement setting = i < firstSettings.Count ? firstSettings[i] : null;
                if (tabButton == null || setting == null)
                {
                    continue;
                }

                bool selected = tabs[i].SectionRoot != null && tabs[i].SectionRoot.activeInHierarchy;
                tabButton.interactable = tabButton.gameObject.activeInHierarchy;
                tabButton.SetState(selected ? GameUISelectableButton.State.Activated : GameUISelectableButton.State.Enabled);
                WireTabButton(tabButton, setting, PreviousVisibleTab(tabs, i), NextVisibleTab(tabs, i));
                WireFirstSetting(setting, tabButton);
            }
        }

        private static void WireTabButton(
            GameUISelectableButton tabButton,
            IExplicitNavElement firstSetting,
            GameUISelectableButton previousTab,
            GameUISelectableButton nextTab)
        {
            Navigation nav = tabButton.navigation;
            nav.mode = Navigation.Mode.Explicit;
            nav.selectOnLeft = previousTab;
            nav.selectOnRight = nextTab;
            nav.selectOnDown = firstSetting.GetNavigationTarget(NavigationGroup.Direction.Down);
            tabButton.SetNavigation(nav);
        }

        private static void WireFirstSetting(IExplicitNavElement setting, GameUISelectableButton tabButton)
        {
            Selectable target = setting.GetNavigationTarget(NavigationGroup.Direction.Up);
            if (target == null)
            {
                return;
            }

            Navigation nav = target.navigation;
            Selectable down = nav.selectOnDown ?? target.FindSelectableOnDown();
            nav.mode = Navigation.Mode.Explicit;
            nav.selectOnUp = tabButton;
            nav.selectOnDown = down;
            setting.SetNavigation(nav);
        }

        private static GameUISelectableButton PreviousVisibleTab(List<global::SettingsTab> tabs, int index)
        {
            for (int i = index - 1; i >= 0; i--)
            {
                GameUISelectableButton button = new ProxySettingsTab(tabs[i]).Button;
                if (button != null && button.gameObject.activeInHierarchy)
                {
                    return button;
                }
            }

            return null;
        }

        private static GameUISelectableButton NextVisibleTab(List<global::SettingsTab> tabs, int index)
        {
            for (int i = index + 1; i < tabs.Count; i++)
            {
                GameUISelectableButton button = new ProxySettingsTab(tabs[i]).Button;
                if (button != null && button.gameObject.activeInHierarchy)
                {
                    return button;
                }
            }

            return null;
        }

        private static IExplicitNavElement FirstVolumeNavigationElement(global::VolumeControl control)
        {
            return Get<SelectableSliderHelper>(control, VolumeSliderField);
        }

        private void AddAudioGroup(ListContainer root, global::SettingsDialog dialog)
        {
            ListContainer group = AddSettingsGroup(root);
            RegisterSettingsVolumeControl(Get<global::VolumeControl>(dialog, GlobalVolumeControlField), group);
            RegisterSettingsVolumeControl(Get<global::VolumeControl>(dialog, SfxVolumeControlField), group);
            RegisterSettingsVolumeControl(Get<global::VolumeControl>(dialog, MusicVolumeControlField), group);
            RegisterSettingsToggle(Get<GameUISelectableToggle>(dialog, BackgroundMuteToggleField), group);
        }

        private void AddGraphicsGroup(ListContainer root, global::SettingsDialog dialog)
        {
            ListContainer group = AddSettingsGroup(root);
            RegisterSettingsToggle(Get<GameUISelectableToggle>(dialog, GraphicsQualityToggleField), group);
            RegisterSettingsDropdown(Get<GameUISelectableDropdown>(dialog, ResolutionDropdownField), group);
            RegisterSettingsDropdown(Get<GameUISelectableDropdown>(dialog, FullScreenDropdownField), group);
            RegisterSettingsToggle(Get<GameUISelectableToggle>(dialog, VsyncToggleField), group);
            RegisterSettingsDropdown(Get<GameUISelectableDropdown>(dialog, FramerateDropdownField), group);
        }

        private void AddGameplayGroup(ListContainer root, global::SettingsDialog dialog)
        {
            ListContainer group = AddSettingsGroup(root);
            RegisterSettingsDropdown(Get<GameUISelectableDropdown>(dialog, GameSpeedDropdownField), group);
            RegisterSettingsDropdown(Get<GameUISelectableDropdown>(dialog, DialogueSpeedDropdownField), group);
            RegisterSettingsToggle(Get<GameUISelectableToggle>(dialog, RoomScrollToggleField), group);
            RegisterSettingsToggle(Get<GameUISelectableToggle>(dialog, TitanTrialDefaultToggleField), group);
            RegisterSettingsScrollSensitivity(Get<global::ScrollSensitivityControl>(dialog, ScrollSensitivityControlField), group);
            RegisterSettingsToggle(Get<GameUISelectableToggle>(dialog, LoreTooltipsToggleField), group);
            RegisterSettingsButton(Get<GameUISelectableButton>(dialog, KeyMappingButtonField), group);
        }

        private void AddAccessibilityGroup(ListContainer root, global::SettingsDialog dialog)
        {
            ListContainer group = AddSettingsGroup(root);
            RegisterSettingsToggle(Get<GameUISelectableToggle>(dialog, BackgroundScrollToggleField), group);
            RegisterSettingsToggle(Get<GameUISelectableToggle>(dialog, CameraShakeToggleField), group);
            RegisterSettingsDropdown(Get<GameUISelectableDropdown>(dialog, ColorblindModeDropdownField), group);
            RegisterSettingsDropdown(Get<GameUISelectableDropdown>(dialog, UIScaleDropdownField), group);
            RegisterSettingsButton(Get<GameUISelectableButton>(dialog, SafeZoneButtonField), group);
        }

        private void AddOtherGroup(ListContainer root, global::SettingsDialog dialog)
        {
            ListContainer group = AddSettingsGroup(root);
            RegisterSettingsDropdown(Get<GameUISelectableDropdown>(dialog, LanguageDropdownField), group);
            RegisterSettingsToggle(Get<GameUISelectableToggle>(dialog, StreamerModeToggleField), group);
            RegisterSettingsToggle(Get<GameUISelectableToggle>(dialog, GooglyEyesToggleField), group);
            RegisterSettingsToggle(Get<GameUISelectableToggle>(dialog, RefreshRateUncapToggleField), group);
            RegisterSettingsButton(Get<GameUISelectableButton>(dialog, ResetSaveButtonField), group);
        }
    }
}

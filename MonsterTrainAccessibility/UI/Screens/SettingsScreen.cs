using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Speech;
using MonsterTrainAccessibility.UI;
using MonsterTrainAccessibility.UI.Elements;
using ShinyShoe;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MonsterTrainAccessibility.UI.Screens
{
    internal sealed class SettingsScreen : GameScreen
    {
        private static readonly FieldInfo PauseDialogField = AccessTools.Field(typeof(global::SettingsScreen), "pauseDialog")!;
        private static readonly FieldInfo SettingsDialogField = AccessTools.Field(typeof(global::SettingsScreen), "settingsDialog")!;
        private static readonly FieldInfo VolumeSliderField = AccessTools.Field(typeof(global::VolumeControl), "slider")!;
        private static readonly FieldInfo VolumeMuteButtonField = AccessTools.Field(typeof(global::VolumeControl), "muteButton")!;
        private static readonly FieldInfo ScrollSensitivitySliderField = AccessTools.Field(typeof(global::ScrollSensitivityControl), "slider")!;
        private static readonly FieldInfo DropdownEntriesField = AccessTools.Field(typeof(GameUISelectableDropdown), "entries")!;
        private static readonly FieldInfo GlobalVolumeControlField = AccessTools.Field(typeof(global::SettingsDialog), "globalVolumeControl")!;
        private static readonly FieldInfo SfxVolumeControlField = AccessTools.Field(typeof(global::SettingsDialog), "sfxVolumeControl")!;
        private static readonly FieldInfo MusicVolumeControlField = AccessTools.Field(typeof(global::SettingsDialog), "musicVolumeControl")!;
        private static readonly FieldInfo BackgroundMuteToggleField = AccessTools.Field(typeof(global::SettingsDialog), "backgroundMuteToggle")!;
        private static readonly FieldInfo GraphicsTabField = AccessTools.Field(typeof(global::SettingsDialog), "graphicsTab")!;
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

            AddAudioGroup(root, dialog);
            AddGraphicsGroup(root, dialog);
            AddGameplayGroup(root, dialog);
            AddAccessibilityGroup(root, dialog);
            AddOtherGroup(root, dialog);
        }

        private void AddAudioGroup(ListContainer root, global::SettingsDialog dialog)
        {
            ListContainer group = AddGroup(root);
            RegisterVolumeControl(Get<global::VolumeControl>(dialog, GlobalVolumeControlField), group);
            RegisterVolumeControl(Get<global::VolumeControl>(dialog, SfxVolumeControlField), group);
            RegisterVolumeControl(Get<global::VolumeControl>(dialog, MusicVolumeControlField), group);
            RegisterToggle(Get<GameUISelectableToggle>(dialog, BackgroundMuteToggleField), group);
        }

        private void AddGraphicsGroup(ListContainer root, global::SettingsDialog dialog)
        {
            ListContainer group = AddGroup(root);
            RegisterTab(Get<global::SettingsTab>(dialog, GraphicsTabField), group);
            RegisterToggle(Get<GameUISelectableToggle>(dialog, GraphicsQualityToggleField), group);
            RegisterDropdown(Get<GameUISelectableDropdown>(dialog, ResolutionDropdownField), group);
            RegisterDropdown(Get<GameUISelectableDropdown>(dialog, FullScreenDropdownField), group);
            RegisterToggle(Get<GameUISelectableToggle>(dialog, VsyncToggleField), group);
            RegisterDropdown(Get<GameUISelectableDropdown>(dialog, FramerateDropdownField), group);
        }

        private void AddGameplayGroup(ListContainer root, global::SettingsDialog dialog)
        {
            ListContainer group = AddGroup(root);
            RegisterDropdown(Get<GameUISelectableDropdown>(dialog, GameSpeedDropdownField), group);
            RegisterDropdown(Get<GameUISelectableDropdown>(dialog, DialogueSpeedDropdownField), group);
            RegisterToggle(Get<GameUISelectableToggle>(dialog, RoomScrollToggleField), group);
            RegisterToggle(Get<GameUISelectableToggle>(dialog, TitanTrialDefaultToggleField), group);
            RegisterScrollSensitivity(Get<global::ScrollSensitivityControl>(dialog, ScrollSensitivityControlField), group);
            RegisterToggle(Get<GameUISelectableToggle>(dialog, LoreTooltipsToggleField), group);
            RegisterButton(Get<GameUISelectableButton>(dialog, KeyMappingButtonField), group);
        }

        private void AddAccessibilityGroup(ListContainer root, global::SettingsDialog dialog)
        {
            ListContainer group = AddGroup(root);
            RegisterToggle(Get<GameUISelectableToggle>(dialog, BackgroundScrollToggleField), group);
            RegisterToggle(Get<GameUISelectableToggle>(dialog, CameraShakeToggleField), group);
            RegisterDropdown(Get<GameUISelectableDropdown>(dialog, ColorblindModeDropdownField), group);
            RegisterDropdown(Get<GameUISelectableDropdown>(dialog, UIScaleDropdownField), group);
            RegisterButton(Get<GameUISelectableButton>(dialog, SafeZoneButtonField), group);
        }

        private void AddOtherGroup(ListContainer root, global::SettingsDialog dialog)
        {
            ListContainer group = AddGroup(root);
            RegisterDropdown(Get<GameUISelectableDropdown>(dialog, LanguageDropdownField), group);
            RegisterToggle(Get<GameUISelectableToggle>(dialog, StreamerModeToggleField), group);
            RegisterToggle(Get<GameUISelectableToggle>(dialog, GooglyEyesToggleField), group);
            RegisterToggle(Get<GameUISelectableToggle>(dialog, RefreshRateUncapToggleField), group);
            RegisterButton(Get<GameUISelectableButton>(dialog, ResetSaveButtonField), group);
        }

        private static ListContainer AddGroup(ListContainer root)
        {
            ListContainer group = new ListContainer
            {
                AnnounceName = false,
                AnnouncePosition = false
            };
            root.Add(group);
            return group;
        }

        private void RegisterTab(global::SettingsTab tab, ListContainer container)
        {
            ProxySettingsTab element = new ProxySettingsTab(tab);
            if (element.Button == null)
            {
                return;
            }

            Add(container, element.Button.gameObject, element);
        }

        private void RegisterVolumeControl(global::VolumeControl control, ListContainer container)
        {
            SelectableSliderHelper slider = Get<SelectableSliderHelper>(control, VolumeSliderField);
            GameUISelectableButton muteButton = Get<GameUISelectableButton>(control, VolumeMuteButtonField);
            string label = ProxySettingsButton.FindLabelInSettingsEntry(control);

            if (slider != null)
            {
                RegisterSlider(slider, container, label);
            }

            if (muteButton != null)
            {
                ProxySettingsVolumeMute muteElement = new ProxySettingsVolumeMute(control, muteButton);
                Add(container, muteButton.gameObject, muteElement);
            }

            Action<bool, float> listener = (muted, volume) =>
            {
                if (IsFocused(muteButton))
                {
                    SpeechManager.Output(ProxySettingsVolumeMute.StateMessage(muted));
                }
            };
            control.volumeSetSignal.AddListener(listener);
            TrackUnsubscribe(() => control.volumeSetSignal.RemoveListener(listener));
        }

        private void RegisterScrollSensitivity(global::ScrollSensitivityControl control, ListContainer container)
        {
            SelectableSliderHelper slider = Get<SelectableSliderHelper>(control, ScrollSensitivitySliderField);
            if (slider == null)
            {
                return;
            }

            RegisterSlider(slider, container, ProxySettingsButton.FindLabelInSettingsEntry(control));
        }

        private void RegisterSlider(SelectableSliderHelper slider, ListContainer container, string label = null)
        {
            SliderElement element = new SliderElement(slider)
            {
                HasOverrideLabel = true,
                OverrideLabel = label ?? ProxySettingsButton.FindLabelInSettingsEntry(slider)
            };
            Add(container, slider.gameObject, element);
        }

        private void RegisterDropdown(GameUISelectableDropdown dropdown, ListContainer container)
        {
            if (dropdown == null)
            {
                return;
            }

            ProxyDropdown element = new ProxyDropdown(dropdown.gameObject)
            {
                HasOverrideLabel = true,
                OverrideLabel = ProxySettingsButton.FindLabelInSettingsEntry(dropdown)
            };
            Add(container, dropdown.gameObject, element);
            RegisterDropdownEntries(dropdown, container);
            ConnectDropdown(dropdown);
        }

        private void RegisterToggle(GameUISelectableToggle toggle, ListContainer container)
        {
            if (toggle == null)
            {
                return;
            }

            ToggleElement element = new ToggleElement(toggle)
            {
                HasOverrideLabel = true,
                OverrideLabel = ProxySettingsButton.FindLabelInSettingsEntry(toggle)
            };
            Add(container, toggle.gameObject, element);
        }

        private void RegisterButton(GameUISelectableButton button, ListContainer container)
        {
            if (button == null)
            {
                return;
            }

            ProxySettingsButton element = new ProxySettingsButton(button);
            Add(container, button.gameObject, element);
        }

        private void ConnectDropdown(GameUISelectableDropdown dropdown)
        {
            Action<int, string> listener = (index, value) =>
            {
                if (!IsFocused(dropdown))
                {
                    return;
                }

                Message status = Message.RawCleaned(ProxySettingsDropdownEntry.ResolveDropdownText(value)) ?? Message.RawCleaned(ProxyDropdown.ResolveStatus(dropdown.gameObject));
                SpeechManager.Output(status);
            };
            dropdown.optionChosenSignal.AddListener(listener);
            TrackUnsubscribe(() => dropdown.optionChosenSignal.RemoveListener(listener));
        }

        private void RegisterDropdownEntries(GameUISelectableDropdown dropdown, ListContainer container)
        {
            List<global::SettableLabel> entries = Get<List<global::SettableLabel>>(dropdown, DropdownEntriesField);
            if (entries == null)
            {
                return;
            }

            ListContainer entriesContainer = new ListContainer
            {
                AnnounceName = false,
                AnnouncePosition = true
            };

            for (int i = 0; i < entries.Count; i++)
            {
                global::SettableLabel entry = entries[i];
                if (entry == null)
                {
                    continue;
                }

                GameUISelectableButton button = entry.GetComponent<GameUISelectableButton>();
                if (button == null)
                {
                    continue;
                }

                ProxySettingsDropdownEntry element = new ProxySettingsDropdownEntry(entry, button);
                Add(entriesContainer, entry.gameObject, element);
            }

            if (entriesContainer.Children.Count > 0)
            {
                container.Add(entriesContainer);
            }
        }

        private void Add(ListContainer container, GameObject target, UIElement element)
        {
            if (container == null || target == null || element == null)
            {
                return;
            }

            container.Add(element);
            Register(target, element);
        }

        private static bool IsFocused(Component component)
        {
            if (component == null)
            {
                return false;
            }

            GameObject selected = EventSystem.current?.currentSelectedGameObject;
            if (selected != null &&
                (selected == component.gameObject ||
                selected.transform.IsChildOf(component.transform) ||
                component.transform.IsChildOf(selected.transform)))
            {
                return true;
            }

            IGameUIComponent uiComponent = component as IGameUIComponent;
            IGameUIComponent selectedComponent = global::InputManager.Inst?.GetSelectedGameUIComponent();
            return uiComponent != null && selectedComponent?.IsGameUIComponent(uiComponent) == true;
        }

        private static T Get<T>(object owner, FieldInfo field) where T : class
        {
            return owner != null ? field.GetValue(owner) as T : null;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Speech;
using MonsterTrainAccessibility.UI.Elements;
using ShinyShoe;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MonsterTrainAccessibility.UI.Screens
{
    internal abstract class SettingsControlsScreen : GameScreen
    {
        private static readonly FieldInfo VolumeSliderField = AccessTools.Field(typeof(global::VolumeControl), "slider")!;
        private static readonly FieldInfo VolumeMuteButtonField = AccessTools.Field(typeof(global::VolumeControl), "muteButton")!;
        private static readonly FieldInfo ScrollSensitivitySliderField = AccessTools.Field(typeof(global::ScrollSensitivityControl), "slider")!;
        private static readonly FieldInfo DropdownEntriesField = AccessTools.Field(typeof(GameUISelectableDropdown), "entries")!;

        protected static ListContainer AddSettingsGroup(ListContainer root)
        {
            ListContainer group = new ListContainer
            {
                AnnounceName = false,
                AnnouncePosition = false
            };
            root.Add(group);
            return group;
        }

        protected void RegisterSettingsTab(global::SettingsTab tab, ListContainer container)
        {
            ProxySettingsTab element = new ProxySettingsTab(tab);
            if (element.Button == null)
            {
                return;
            }

            AddSettingsElement(container, element.Button.gameObject, element);
        }

        protected void RegisterSettingsVolumeControl(global::VolumeControl control, ListContainer container)
        {
            if (control == null)
            {
                return;
            }

            SelectableSliderHelper slider = Get<SelectableSliderHelper>(control, VolumeSliderField);
            GameUISelectableButton muteButton = Get<GameUISelectableButton>(control, VolumeMuteButtonField);

            if (slider != null)
            {
                RegisterSettingsSlider(slider, container, () => ProxySettingsButton.FindLabelInSettingsEntry(control));
            }

            if (muteButton != null)
            {
                ProxySettingsVolumeMute muteElement = new ProxySettingsVolumeMute(control, muteButton);
                AddSettingsElement(container, muteButton.gameObject, muteElement);
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

        protected void RegisterSettingsScrollSensitivity(global::ScrollSensitivityControl control, ListContainer container)
        {
            SelectableSliderHelper slider = Get<SelectableSliderHelper>(control, ScrollSensitivitySliderField);
            if (slider == null)
            {
                return;
            }

            RegisterSettingsSlider(slider, container, () => ProxySettingsButton.FindLabelInSettingsEntry(control));
        }

        protected void RegisterSettingsSlider(SelectableSliderHelper slider, ListContainer container, Func<string> label = null)
        {
            if (slider == null)
            {
                return;
            }

            SliderElement element = new SliderElement(
                slider,
                () => Message.RawCleaned(label != null ? label() : ProxySettingsButton.FindLabelInSettingsEntry(slider)));
            AddSettingsElement(container, slider.gameObject, element);
        }

        protected void RegisterSettingsDropdown(GameUISelectableDropdown dropdown, ListContainer container)
        {
            if (dropdown == null)
            {
                return;
            }

            ProxyDropdown element = new ProxyDropdown(
                dropdown.gameObject,
                () => Message.RawCleaned(ProxySettingsButton.FindLabelInSettingsEntry(dropdown)));
            AddSettingsElement(container, dropdown.gameObject, element);
            RegisterSettingsDropdownEntries(dropdown, container);
            ConnectSettingsDropdown(dropdown);
        }

        protected void RegisterSettingsToggle(GameUISelectableToggle toggle, ListContainer container)
        {
            if (toggle == null)
            {
                return;
            }

            ToggleElement element = new ToggleElement(
                toggle,
                () => Message.RawCleaned(ProxySettingsButton.FindLabelInSettingsEntry(toggle)));
            AddSettingsElement(container, toggle.gameObject, element);
        }

        protected void RegisterSettingsButton(GameUISelectableButton button, ListContainer container)
        {
            if (button == null)
            {
                return;
            }

            ProxySettingsButton element = new ProxySettingsButton(button);
            AddSettingsElement(container, button.gameObject, element);
        }

        protected void AddSettingsElement(ListContainer container, GameObject target, UIElement element)
        {
            if (container == null || target == null || element == null)
            {
                return;
            }

            container.Add(element);
            Register(target, element);
        }

        protected static T Get<T>(object owner, FieldInfo field) where T : class
        {
            return owner != null ? field.GetValue(owner) as T : null;
        }

        private void ConnectSettingsDropdown(GameUISelectableDropdown dropdown)
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

        private void RegisterSettingsDropdownEntries(GameUISelectableDropdown dropdown, ListContainer container)
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
                AddSettingsElement(entriesContainer, entry.gameObject, element);
            }

            if (entriesContainer.Children.Count > 0)
            {
                container.Add(entriesContainer);
            }
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
    }
}

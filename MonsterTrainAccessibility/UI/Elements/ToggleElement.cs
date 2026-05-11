using System;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Speech;
using UnityEngine;
using UnityEngine.UI;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal class ToggleElement : GameObjectElement
    {
        private readonly global::ShinyShoe.GameUISelectableToggle _toggle;
        private readonly global::ShinyShoe.GameUISelectableCheckbox _checkbox;
        private readonly Toggle _unityToggle;
        private bool _listening;
        private bool? _lastAnnouncedState;

        public ToggleElement(
            global::ShinyShoe.GameUISelectableToggle toggle,
            Func<Message> label = null,
            Func<Message> tooltip = null,
            Func<bool> visibility = null)
            : base(
                target: toggle != null ? toggle.gameObject : null,
                typeKey: "toggle",
                label: label ?? (() => DefaultLabel(toggle != null ? toggle.gameObject : null)),
                status: () => StateMessage(toggle != null && toggle.isOn),
                tooltip: tooltip,
                visibility: visibility)
        {
            _toggle = toggle;
        }

        public ToggleElement(
            global::ShinyShoe.GameUISelectableCheckbox checkbox,
            Func<Message> label = null,
            Func<Message> tooltip = null,
            Func<bool> visibility = null)
            : base(
                target: checkbox != null ? checkbox.gameObject : null,
                typeKey: "toggle",
                label: label ?? (() => DefaultLabel(checkbox != null ? checkbox.gameObject : null)),
                status: () => StateMessage(checkbox != null && checkbox.isChecked),
                tooltip: tooltip,
                visibility: visibility)
        {
            _checkbox = checkbox;
        }

        public ToggleElement(
            Toggle toggle,
            Func<Message> label = null,
            Func<Message> tooltip = null,
            Func<bool> visibility = null)
            : base(
                target: toggle != null ? toggle.gameObject : null,
                typeKey: "toggle",
                label: label ?? (() => DefaultLabel(toggle != null ? toggle.gameObject : null)),
                status: () => StateMessage(toggle != null && toggle.isOn),
                tooltip: tooltip,
                visibility: visibility)
        {
            _unityToggle = toggle;
        }

        public override bool Activate()
        {
            SelectTarget();

            if (_toggle != null)
            {
                _toggle.Toggle();
            }
            else if (_checkbox != null)
            {
                _checkbox.Toggle();
                UIManager.RefreshBuffersFor(this);
            }
            else if (_unityToggle != null)
            {
                _unityToggle.isOn = !_unityToggle.isOn;
            }
            else
            {
                return false;
            }

            return true;
        }

        protected override void OnFocus()
        {
            if (_listening)
            {
                return;
            }

            _lastAnnouncedState = null;

            if (_toggle != null)
            {
                _toggle.OnToggle.AddListener(HandleToggleChanged);
                _listening = true;
            }
            else if (_unityToggle != null)
            {
                _unityToggle.onValueChanged.AddListener(HandleToggleChanged);
                _listening = true;
            }
        }

        protected override void OnUnfocus()
        {
            if (!_listening)
            {
                return;
            }

            if (_toggle != null)
            {
                _toggle.OnToggle.RemoveListener(HandleToggleChanged);
            }
            else if (_unityToggle != null)
            {
                _unityToggle.onValueChanged.RemoveListener(HandleToggleChanged);
            }

            _listening = false;
            _lastAnnouncedState = null;
        }

        private void HandleToggleChanged(bool value)
        {
            if (_lastAnnouncedState.HasValue && _lastAnnouncedState.Value == value)
            {
                return;
            }

            _lastAnnouncedState = value;
            SpeechManager.Output(StateMessage(value));
            UIManager.RefreshBuffersFor(this);
        }

        private static Message StateMessage(bool isOn)
        {
            return new Message(isOn ? "state.on" : "state.off");
        }
    }
}

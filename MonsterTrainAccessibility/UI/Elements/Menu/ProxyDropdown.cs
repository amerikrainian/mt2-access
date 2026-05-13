using MonsterTrainAccessibility.Localization;
using System;
using System.Reflection;
using HarmonyLib;
using ShinyShoe;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyDropdown : GameObjectElement
    {
        private static readonly FieldInfo GameDropdownValueLabelField = AccessTools.Field(typeof(GameUISelectableDropdown), "valueLabel")!;
        private readonly TMP_Dropdown _tmpDropdown;
        private readonly Dropdown _unityDropdown;
        private readonly GameUISelectableDropdown _gameDropdown;

        public ProxyDropdown(GameObject target, Func<Message> label = null)
            : base(
                target: target,
                typeKey: "dropdown",
                label: label,
                status: null)
        {
            _tmpDropdown = target != null ? target.GetComponent<TMP_Dropdown>() : null;
            _unityDropdown = target != null ? target.GetComponent<Dropdown>() : null;
            _gameDropdown = target != null ? target.GetComponent<GameUISelectableDropdown>() : null;
        }

        public override Message GetLabel()
        {
            Message label = base.GetLabel();
            if (HasOverrideLabel || label != null)
            {
                return label;
            }

            return null;
        }

        public override Message GetStatusString()
        {
            return Message.RawCleaned(ResolveStatus());
        }

        public static string ResolveStatus(GameObject target)
        {
            return new ProxyDropdown(target).ResolveStatus();
        }

        private string ResolveStatus()
        {
            if (_tmpDropdown != null &&
                _tmpDropdown.options != null &&
                _tmpDropdown.value >= 0 &&
                _tmpDropdown.value < _tmpDropdown.options.Count)
            {
                return Message.Clean(_tmpDropdown.options[_tmpDropdown.value].text);
            }

            if (_unityDropdown != null &&
                _unityDropdown.options != null &&
                _unityDropdown.value >= 0 &&
                _unityDropdown.value < _unityDropdown.options.Count)
            {
                return Message.Clean(_unityDropdown.options[_unityDropdown.value].text);
            }

            TMP_Text valueLabel = _gameDropdown != null ? GameDropdownValueLabelField.GetValue(_gameDropdown) as TMP_Text : null;
            string text = AccessibilityText.ReadLocalizedText(valueLabel);
            if (!string.IsNullOrWhiteSpace(text))
            {
                return text;
            }

            return string.Empty;
        }
    }
}

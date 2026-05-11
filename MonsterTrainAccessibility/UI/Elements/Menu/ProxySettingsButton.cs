using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI;
using MonsterTrainAccessibility.UI.Screens;
using ShinyShoe;
using TMPro;
using UnityEngine;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal class ProxySettingsButton : GameObjectElement
    {
        private readonly GameUISelectableButton _button;

        public ProxySettingsButton(GameUISelectableButton button)
            : base(
                button != null ? button.gameObject : null,
                typeKey: "button",
                label: null)
        {
            _button = button;
        }

        public override bool IsVisible => _button != null && _button.gameObject.activeInHierarchy;
        public override Message GetLabel()
        {
            return Message.RawCleaned(FirstText(
                FindLabelInSettingsEntry(_button),
                AuthoredLabelReader.Read(_button),
                GameUIButtonSupport.ResolveLabel(_button)));
        }

        public override Message GetStatusString() => GameButtonElement.StateMessage(_button);

        internal static string FindLabelInSettingsEntry(Component component)
        {
            global::SettingsEntry entry = component != null ? component.GetComponentInParent<global::SettingsEntry>(includeInactive: true) : null;
            if (entry == null)
            {
                return null;
            }

            Transform label = entry.transform.Find("Label");
            if (label == null)
            {
                return null;
            }

            TMP_Text text = label.GetComponent<TMP_Text>();
            return AccessibilityText.ReadLocalizedText(text);
        }

        internal static string FirstText(params string[] values)
        {
            if (values == null)
            {
                return null;
            }

            for (int i = 0; i < values.Length; i++)
            {
                string value = Message.Clean(values[i]);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return null;
        }
    }
}

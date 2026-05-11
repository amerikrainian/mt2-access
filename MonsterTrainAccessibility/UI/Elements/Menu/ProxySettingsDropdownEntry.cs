using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI;
using ShinyShoe;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxySettingsDropdownEntry : GameObjectElement
    {
        private readonly global::SettableLabel _entry;
        private readonly GameUISelectableButton _button;

        public ProxySettingsDropdownEntry(global::SettableLabel entry, GameUISelectableButton button)
            : base(
                button != null ? button.gameObject : null,
                typeKey: "button",
                label: null)
        {
            _entry = entry;
            _button = button;
        }

        public override bool IsVisible => _button != null && _button.gameObject.activeInHierarchy;
        public override Message GetLabel() => Message.RawCleaned(FindDropdownEntryText(_entry));
        public override Message GetStatusString() => GameButtonElement.StateMessage(_button);

        public static string ResolveDropdownText(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value.HasTranslation()
                ? AccessibilityText.LocalizeTerm(value)
                : Message.Clean(value);
        }

        private static string FindDropdownEntryText(global::SettableLabel entry)
        {
            if (entry == null)
            {
                return null;
            }

            string label = AuthoredLabelReader.Read(entry);
            if (!string.IsNullOrWhiteSpace(label))
            {
                return label;
            }

            return ResolveDropdownText(entry.content.text);
        }
    }
}

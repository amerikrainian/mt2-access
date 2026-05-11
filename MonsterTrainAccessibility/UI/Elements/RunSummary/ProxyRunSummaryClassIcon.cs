using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Screens;
using TMPro;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyRunSummaryClassIcon : UIElement
    {
        private static readonly FieldInfo ClassIconLabelField = AccessTools.Field(typeof(global::ClassIconUI), "label")!;

        private readonly global::ClassIconUI _icon;
        private readonly string _labelKey;

        public ProxyRunSummaryClassIcon(global::ClassIconUI icon, string labelKey)
        {
            _icon = icon;
            _labelKey = labelKey;
        }

        public override bool IsVisible
        {
            get
            {
                TMP_Text label = LabelText();
                return _icon != null &&
                    _icon.gameObject.activeInHierarchy &&
                    (HasText(label) || AccessibleScreenText.Tooltip(_icon) != null);
            }
        }

        public override Message GetLabel()
        {
            Message value = AccessibleScreenText.Text(LabelText()) ?? AccessibleScreenText.Tooltip(_icon);
            return value != null
                ? Message.Join(", ", Message.Localized("ui", _labelKey), value)
                : null;
        }

        public override Message GetTooltip() => AccessibleScreenText.Tooltip(_icon);

        private TMP_Text LabelText()
        {
            return Get<TMP_Text>(_icon, ClassIconLabelField);
        }

        private static bool HasText(TMP_Text text)
        {
            return text != null &&
                text.gameObject.activeInHierarchy &&
                !string.IsNullOrWhiteSpace(AccessibilityText.ReadLocalizedText(text));
        }

        private static T Get<T>(object owner, FieldInfo field) where T : class
        {
            return owner != null ? field.GetValue(owner) as T : null;
        }
    }
}

using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI;
using TMPro;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyLunaCoven : GameObjectElement
    {
        private static readonly FieldInfo LabelField = AccessTools.Field(typeof(global::LunaCovenUI), "label")!;
        private static readonly FieldInfo TooltipProviderField = AccessTools.Field(typeof(global::LunaCovenUI), "tooltipProvider")!;

        private readonly global::LunaCovenUI _luna;

        public ProxyLunaCoven(global::LunaCovenUI luna)
            : base(
                target: luna != null ? luna.gameObject : null,
                typeKey: null,
                label: null)
        {
            _luna = luna;
        }

        public override bool IsVisible => _luna != null && _luna.gameObject.activeInHierarchy;
        public override Message GetLabel() => Message.Localized("ui", "HUD.MOON_PHASE");
        public override Message GetStatusString() => Status(_luna);
        public override Message GetTooltip()
        {
            TooltipProviderComponent provider = Get<TooltipProviderComponent>(_luna, TooltipProviderField);
            return provider != null ? TooltipText.ForComponent(provider) : null;
        }

        public static Message Status(global::LunaCovenUI luna)
        {
            TMP_Text label = Get<TMP_Text>(luna, LabelField);
            string status = AccessibilityText.ReadLocalizedText(label);
            return !string.IsNullOrWhiteSpace(status) ? Message.RawCleaned(status) : null;
        }

        public static string SignatureText(global::LunaCovenUI luna)
        {
            return AccessibilityText.ReadLocalizedText(Get<TMP_Text>(luna, LabelField));
        }

        private static T Get<T>(object owner, FieldInfo field) where T : class
        {
            return owner != null ? field.GetValue(owner) as T : null;
        }
    }
}

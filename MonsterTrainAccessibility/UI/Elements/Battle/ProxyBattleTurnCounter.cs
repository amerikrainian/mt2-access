using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI;
using TMPro;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyBattleTurnCounter : GameObjectElement
    {
        private static readonly FieldInfo TextLabelField = AccessTools.Field(typeof(global::BattleTurnCounter), "textLabel")!;
        private static readonly FieldInfo CountLabelField = AccessTools.Field(typeof(global::BattleTurnCounter), "countLabel")!;
        private static readonly FieldInfo TooltipProviderField = AccessTools.Field(typeof(global::BattleTurnCounter), "tooltipProvider")!;

        private readonly global::BattleTurnCounter _counter;

        public ProxyBattleTurnCounter(global::BattleTurnCounter counter)
            : base(
                target: counter != null ? counter.gameObject : null,
                typeKey: null,
                label: null)
        {
            _counter = counter;
        }

        public override bool IsVisible => _counter != null && _counter.gameObject.activeInHierarchy;
        public override Message GetLabel() => Message.Localized("ui", "HUD.TURN_COUNTER");
        public override Message GetStatusString() => Status(_counter);
        public override Message GetTooltip() => Tooltip(_counter);

        public static Message Status(global::BattleTurnCounter counter)
        {
            string combined = SignatureText(counter);
            return !string.IsNullOrWhiteSpace(combined) ? Message.RawCleaned(combined) : null;
        }

        public static Message Tooltip(global::BattleTurnCounter counter)
        {
            TooltipProviderComponent provider = Get<TooltipProviderComponent>(counter, TooltipProviderField);
            return provider != null && provider.enabled ? TooltipText.ForComponent(provider) : null;
        }

        public static string SignatureText(global::BattleTurnCounter counter)
        {
            TMP_Text textLabel = Get<TMP_Text>(counter, TextLabelField);
            TMP_Text countLabel = Get<TMP_Text>(counter, CountLabelField);
            string text = AccessibilityText.ReadLocalizedText(textLabel);
            string count = AccessibilityText.ReadLocalizedText(countLabel);
            return Message.JoinText(text, count);
        }

        public static bool IsTooltipEnabled(global::BattleTurnCounter counter)
        {
            TooltipProviderComponent provider = Get<TooltipProviderComponent>(counter, TooltipProviderField);
            return provider != null && provider.enabled;
        }

        private static T Get<T>(object owner, FieldInfo field) where T : class
        {
            return owner != null ? field.GetValue(owner) as T : null;
        }
    }
}

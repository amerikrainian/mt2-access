using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyGameSpeed : GameObjectElement
    {
        private readonly global::GameSpeedUI _speed;

        public ProxyGameSpeed(global::GameSpeedUI speed)
            : base(
                target: speed != null ? speed.gameObject : null,
                typeKey: "button",
                label: null)
        {
            _speed = speed;
        }

        public override bool IsVisible => _speed != null && _speed.gameObject.activeInHierarchy;
        public override Message GetLabel() => TooltipTitle(_speed) ?? Message.Localized("ui", "HUD.GAME_SPEED");
        public override Message GetTooltip() => _speed != null ? TooltipText.ForComponent(_speed) : null;

        private static Message TooltipTitle(UnityEngine.Component component)
        {
            TooltipProviderComponent provider = component != null ? component.GetComponent<TooltipProviderComponent>() : null;
            string title = TooltipText.FirstTitle(provider);
            return !string.IsNullOrWhiteSpace(title) ? Message.RawCleaned(title) : null;
        }
    }
}

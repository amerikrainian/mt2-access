using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Screens;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyAbilityCounter : GameObjectElement
    {
        private readonly global::AbilityCounterUI _abilityCounter;
        private readonly BattleScreen _screen;

        public ProxyAbilityCounter(global::AbilityCounterUI abilityCounter, BattleScreen screen)
            : base(
                target: abilityCounter?.Button != null ? abilityCounter.Button.gameObject : null,
                typeKey: "button",
                label: null)
        {
            _abilityCounter = abilityCounter;
            _screen = screen;
        }

        public override bool IsVisible => _abilityCounter != null && _abilityCounter.gameObject.activeInHierarchy;
        public override Message GetLabel() => Message.Localized("combat", "BUTTON.ABILITIES");
        public override Message GetStatusString() => AbilityCounterStatus();

        public override bool Activate()
        {
            return _screen != null && _screen.RouteToNearestAbility();
        }

        private Message AbilityCounterStatus()
        {
            int count = _screen?.CountAvailableAbilities() ?? 0;
            return count > 0 ? Message.Localized("combat", "BUTTON.ABILITIES_AVAILABLE", new { count }) : null;
        }
    }
}

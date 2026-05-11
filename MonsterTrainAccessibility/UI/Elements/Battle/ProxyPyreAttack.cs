using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.UI;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyPyreAttack : GameObjectElement
    {
        private readonly global::PyreAttackUI _attack;

        public ProxyPyreAttack(global::PyreAttackUI attack)
            : base(
                target: attack != null ? attack.gameObject : null,
                typeKey: null,
                label: null)
        {
            _attack = attack;
        }

        public override bool IsVisible => _attack != null && _attack.gameObject.activeInHierarchy;
        public override Message GetLabel() => Message.Localized("ui", "HUD.PYRE_ATTACK");
        public override Message GetStatusString() => Message.Localized("combat", "CREATURE.ATTACK", new { attack = GameManagers.GetSaveManager()?.GetDisplayedPyreAttack() ?? 0 });
        public override Message GetTooltip() => _attack != null ? TooltipText.ForComponent(_attack) : null;
    }
}

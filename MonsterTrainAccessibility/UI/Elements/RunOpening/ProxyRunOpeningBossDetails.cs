using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Screens;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyRunOpeningBossDetails : GameObjectElement
    {
        private readonly global::BossDetailsUI _boss;

        public ProxyRunOpeningBossDetails(global::BossDetailsUI boss)
            : base(
                boss != null ? boss.gameObject : null,
                label: null)
        {
            _boss = boss;
        }

        public override bool IsVisible => _boss != null && _boss.gameObject.activeInHierarchy;

        public override Message GetLabel()
        {
            return Screens.RunOpeningScreen.BossLabel(_boss);
        }

        public override Message GetTooltip()
        {
            return Screens.RunOpeningScreen.BossTooltip(_boss);
        }
    }
}

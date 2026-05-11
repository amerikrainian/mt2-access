using System.Globalization;
using MonsterTrainAccessibility.Localization;
using ShinyShoe;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyEnergyUI : GameObjectElement
    {
        private readonly global::EnergyUI _energyUI;

        public ProxyEnergyUI(global::EnergyUI energyUI, IGameUIComponent selectable)
            : base(
                component: selectable,
                typeKey: null,
                label: null)
        {
            _energyUI = energyUI;
        }

        public override bool IsVisible => _energyUI != null && _energyUI.gameObject.activeInHierarchy;
        public override Message GetLabel() => Message.Localized("combat", "RESOURCE.EMBER");
        public override Message GetStatusString()
        {
            int energy = AllGameManagers.Instance.OrNull()?.GetPlayerManager()?.GetEnergy() ?? 0;
            return Message.RawCleaned(energy.ToString(CultureInfo.InvariantCulture));
        }
    }
}

using MonsterTrainAccessibility.UI.Screens;
using MonsterTrainAccessibility.UI.Elements;
using ShinyShoe;
using UnityEngine;

namespace MonsterTrainAccessibility.UI.Elements.Compendium
{
    internal abstract class ProxyCompendiumGameButton : ButtonProxy
    {
        protected ProxyCompendiumGameButton(IGameUIComponent component)
            : base(component)
        {
        }

        protected ProxyCompendiumGameButton(GameObject target)
            : base(target)
        {
        }

        protected override bool SelectTarget()
        {
            IGameUIComponent component = Component;
            if (component != null)
            {
                Screens.CompendiumScreen.SelectGameComponent(component);
                return true;
            }

            Screens.CompendiumScreen.ClearGameSelection();
            return Target != null;
        }
    }
}

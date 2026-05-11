using MonsterTrainAccessibility.Localization;
using UnityEngine;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyEmptyHand : GameObjectElement
    {
        public ProxyEmptyHand(GameObject target)
            : base(
                target: target,
                label: null)
        {
        }

        public override bool IsVisible => Target == null || Target.activeInHierarchy;
        public override Message GetLabel() => Message.Localized("combat", "HAND.EMPTY");
    }
}

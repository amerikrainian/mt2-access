using MonsterTrainAccessibility.Localization;
using TMPro;
using UnityEngine;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyCardDetailsArtist : GameObjectElement
    {
        private readonly TMP_Text _artistName;

        public ProxyCardDetailsArtist(GameObject target, TMP_Text artistName)
            : base(
                target,
                typeKey: null,
                label: null)
        {
            _artistName = artistName;
        }

        public override bool IsVisible => Target != null && Target.activeInHierarchy;

        public override Message GetLabel()
        {
            return Message.RawCleaned(AccessibilityText.ReadLocalizedText(_artistName));
        }
    }
}

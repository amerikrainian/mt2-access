using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Screens;
using TMPro;
using UnityEngine;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyDragonsHoardAmount : GameObjectElement
    {
        private readonly SaveManager _saveManager;
        private readonly TMP_Text _fallbackText;

        public ProxyDragonsHoardAmount(GameObject target, SaveManager saveManager, TMP_Text fallbackText)
            : base(target, label: null)
        {
            _saveManager = saveManager;
            _fallbackText = fallbackText;
        }

        public override bool IsVisible => _fallbackText == null || _fallbackText.gameObject.activeInHierarchy;
        public override Message GetLabel() => Message.Localized("ui", "HUD.DRAGONS_HOARD");
        public override Message GetStatusString()
        {
            if (_saveManager != null)
            {
                return Message.Localized("ui", "HUD.DRAGONS_HOARD_VALUE", new
                {
                    amount = _saveManager.GetDragonsHoardAmount(),
                    cap = _saveManager.GetDragonsHoardCap()
                });
            }

            return Message.FromText(AccessibilityText.ReadLocalizedText(_fallbackText));
        }
    }
}

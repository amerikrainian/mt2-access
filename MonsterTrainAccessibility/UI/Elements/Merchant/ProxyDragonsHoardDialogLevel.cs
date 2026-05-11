using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Screens;
using TMPro;
using UnityEngine;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyDragonsHoardDialogLevel : GameObjectElement
    {
        private readonly GameObject _dialogRoot;
        private readonly TMP_Text _currentLootLevelText;
        private readonly TMP_Text _selectedLootLevelAmount;

        public ProxyDragonsHoardDialogLevel(GameObject target, GameObject dialogRoot, TMP_Text currentLootLevelText, TMP_Text selectedLootLevelAmount)
            : base(target, label: null)
        {
            _dialogRoot = dialogRoot;
            _currentLootLevelText = currentLootLevelText;
            _selectedLootLevelAmount = selectedLootLevelAmount;
        }

        public override bool IsVisible => _dialogRoot != null && _dialogRoot.activeInHierarchy;
        public override Message GetLabel() => Message.FromText(AccessibilityText.ReadLocalizedText(_currentLootLevelText));
        public override Message GetStatusString() => Message.FromText(AccessibilityText.ReadLocalizedText(_selectedLootLevelAmount));
    }
}

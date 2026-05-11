using MonsterTrainAccessibility.Localization;
using TMPro;
using UnityEngine.EventSystems;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyMerchantDialogue : ProxyElement, INavigationTargetElement
    {
        private readonly global::MerchantCharacterUI _character;
        private readonly TMP_Text _label;

        public ProxyMerchantDialogue(global::MerchantCharacterUI character, TMP_Text label)
            : base(character != null ? character.gameObject : null)
        {
            _character = character;
            _label = label;
        }

        public override bool IsVisible =>
            _character != null &&
            _character.gameObject.activeInHierarchy &&
            Message.ShouldAdd(Message.Clean(AccessibilityText.ReadLocalizedText(_label)));

        public override Message GetLabel()
        {
            return Message.FromText(AccessibilityText.ReadLocalizedText(_label));
        }

        public void SelectForNavigation()
        {
            if (Target == null || !Target.activeInHierarchy)
            {
                return;
            }

            EventSystem.current?.SetSelectedGameObject(Target);
        }
    }
}

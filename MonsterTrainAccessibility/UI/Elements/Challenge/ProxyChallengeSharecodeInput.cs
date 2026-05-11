using System;
using MonsterTrainAccessibility.Localization;
using ShinyShoe;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyChallengeSharecodeInput : UIElement, IActivatableElement, INavigationTargetElement
    {
        private readonly global::InputFieldContainer _input;
        private readonly Func<bool> _activate;

        public ProxyChallengeSharecodeInput(global::InputFieldContainer input, Func<bool> activate)
        {
            _input = input;
            _activate = activate;
        }

        public override bool IsVisible => _input != null && _input.gameObject.activeInHierarchy;
        public override string GetTypeKey() => "button";
        public override Message GetLabel() => Message.Localized("ui", "CHALLENGE.SHARECODE_FIELD");
        public override Message GetStatusString() => Message.FromText(_input?.text);

        public void SelectForNavigation()
        {
            if (_input?.button != null && InputManager.Inst != null)
            {
                InputManager.Inst.SelectGameUIComponent(_input.button, allowClearingSelection: false);
            }
        }

        public bool Activate() => _activate != null && _activate();
    }
}

using System;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Screens;
using ShinyShoe;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyChallengeProgressButton : UIElement, IActivatableElement, INavigationTargetElement
    {
        private readonly GameUISelectableButton _button;
        private readonly Message _fallbackLabel;
        private readonly Func<bool> _activate;

        public ProxyChallengeProgressButton(GameUISelectableButton button, Message fallbackLabel, Func<bool> activate)
        {
            _button = button;
            _fallbackLabel = fallbackLabel;
            _activate = activate;
        }

        public override bool IsVisible => _button != null && _button.gameObject.activeInHierarchy;

        public override string GetTypeKey() => "button";

        public override Message GetLabel()
        {
            return Screens.ChallengeProgressScreen.ButtonLabel(_button, _fallbackLabel);
        }

        public override Message GetStatusString()
        {
            return GameButtonElement.StateMessage(_button);
        }

        public bool Activate()
        {
            return Screens.ChallengeProgressScreen.ActivateButton(_button, _activate);
        }

        public void SelectForNavigation()
        {
            Screens.ChallengeProgressScreen.ClearNativeSelection();
        }
    }
}

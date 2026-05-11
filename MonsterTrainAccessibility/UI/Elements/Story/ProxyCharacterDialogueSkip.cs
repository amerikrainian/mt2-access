using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Screens;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyCharacterDialogueSkip : UIElement, IActivatableElement, INavigationTargetElement
    {
        private readonly CharacterDialogueScreen _screen;

        public ProxyCharacterDialogueSkip(CharacterDialogueScreen screen)
        {
            _screen = screen;
        }

        public override string GetTypeKey() => "button";

        public override Message GetLabel()
        {
            return Message.Localized("ui", "DIALOGUE.SKIP");
        }

        public bool Activate()
        {
            return _screen != null && _screen.DispatchDialogueInput(global::InputManager.Controls.SkipDialogue);
        }

        public void SelectForNavigation()
        {
            CharacterDialogueScreen.ClearGameSelection();
        }
    }
}

using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Screens;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyCharacterDialogueContinue : UIElement, IActivatableElement, INavigationTargetElement
    {
        private readonly CharacterDialogueScreen _screen;

        public ProxyCharacterDialogueContinue(CharacterDialogueScreen screen)
        {
            _screen = screen;
        }

        public override bool IsVisible => _screen != null && _screen.CanAdvanceDialogue();

        public override string GetTypeKey() => "button";

        public override Message GetLabel()
        {
            return Message.Localized("ui", "DIALOGUE.CONTINUE");
        }

        public bool Activate()
        {
            return _screen != null && _screen.DispatchDialogueInput(global::InputManager.Controls.AdvanceDialogue);
        }

        public void SelectForNavigation()
        {
            CharacterDialogueScreen.ClearGameSelection();
        }
    }
}

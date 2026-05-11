using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Screens;
using ShinyShoe;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyRunHistoryBackButton : GameObjectElement
    {
        private readonly GameUISelectableButton _button;
        private readonly global::RunHistoryScreen _screen;

        public ProxyRunHistoryBackButton(GameUISelectableButton button, global::RunHistoryScreen screen)
            : base(
                button != null ? button.gameObject : null,
                typeKey: "button",
                label: null)
        {
            _button = button;
            _screen = screen;
        }

        public override bool IsVisible => _button != null && _button.gameObject.activeInHierarchy;

        public override Message GetLabel()
        {
            return Screens.RunHistoryScreen.BackButtonLabel(_button);
        }

        public override bool Activate()
        {
            return Screens.RunHistoryScreen.ReturnToMainMenu(_screen);
        }

        public override void SelectForNavigation()
        {
            global::InputManager.Inst?.SelectGameUIComponent(null, allowClearingSelection: true);
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
        }
    }
}

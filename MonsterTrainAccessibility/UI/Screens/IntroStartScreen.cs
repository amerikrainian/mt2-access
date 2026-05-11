using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Speech;
using MonsterTrainAccessibility.UI.Elements;
using UnityEngine;

namespace MonsterTrainAccessibility.UI.Screens
{
    internal sealed class IntroStartScreen : GameScreen
    {
        private readonly global::IntroStartScreen _screen;
        private string _lastAnnouncedPrompt;

        public IntroStartScreen(global::IntroStartScreen screen)
            : base(screen != null ? screen.transform : null)
        {
            _screen = screen;
        }

        public override void OnPush()
        {
            base.OnPush();
            AnnouncePrompt(force: true);

            global::ShinyShoe.CoreSignals.InputModeChanged.AddListener(HandleInputModeChanged);
            TrackUnsubscribe(() => global::ShinyShoe.CoreSignals.InputModeChanged.RemoveListener(HandleInputModeChanged));
        }

        public override bool ShouldAnnounceFocus(UIElement element) => false;

        protected override void BuildRegistry()
        {
            ListContainer controls = new ListContainer();
            RootElement = controls;

            AddAccountButton(controls);
            AddStaticAccountButton(controls);
        }

        private void AddAccountButton(ListContainer container)
        {
            global::ShinyShoe.GameUISelectableButton button = ReflectionUtil.GetFieldValue<global::ShinyShoe.GameUISelectableButton>(_screen, "changeUserButton", "IntroStartScreen");
            if (button == null)
            {
                return;
            }

            AddToContainer(
                container,
                button.gameObject,
                new ProxyIntroChangeUserButton(button));
        }

        private void AddStaticAccountButton(ListContainer container)
        {
            global::ShinyShoe.GameUISelectableButton button = ReflectionUtil.GetFieldValue<global::ShinyShoe.GameUISelectableButton>(_screen, "nonSwitchableUserButton", "IntroStartScreen");
            if (button == null)
            {
                return;
            }

            AddToContainer(
                container,
                button.gameObject,
                new ProxyIntroCurrentUserButton(button));
        }

        private void HandleInputModeChanged(global::ShinyShoe.InputDeviceType current, global::ShinyShoe.InputDeviceType previous)
        {
            AnnouncePrompt();
        }

        private void AnnouncePrompt(bool force = false)
        {
            Message prompt = ResolvePromptMessage();
            string text = prompt?.Resolve();
            if (!force && string.Equals(text, _lastAnnouncedPrompt, System.StringComparison.Ordinal))
            {
                return;
            }

            _lastAnnouncedPrompt = text;
            SpeechManager.Output(prompt);
        }

        private Message ResolvePromptMessage()
        {
            global::ShinyShoe.InputDeviceType deviceType = global::InputManager.Inst != null
                ? global::InputManager.Inst.currentInputDeviceMode
                : global::ShinyShoe.AppManager.PlatformServices.GetInitialInputDeviceType();
            bool needsLogin = global::ShinyShoe.AppManager.PlatformServices.SupportsFeature(global::ShinyShoe.PlatformFeature.UserLogin) &&
                !global::ShinyShoe.AppManager.PlatformServices.HasPlayerID() &&
                global::ShinyShoe.AppManager.PlatformServices.IsPlayerIDNeededForPlay();

            string fieldName = deviceType == global::ShinyShoe.InputDeviceType.Gamepad
                ? needsLogin ? "_labelGamepadLoggedOut" : "_labelGamepad"
                : needsLogin ? "_labelKeyboardMouseLoggedOut" : "_labelKeyboardMouse";

            RectTransform root = ReflectionUtil.GetFieldValue<RectTransform>(_screen, fieldName, "IntroStartScreen");
            return AuthoredLabelReader.ReadMessage(root);
        }
    }
}

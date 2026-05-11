using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Input;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.ModSettings;
using MonsterTrainAccessibility.Speech;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class BoolSettingElement : UIElement, IActivatableElement, INavigationActionHandler
    {
        private readonly BoolSetting _setting;

        public BoolSettingElement(BoolSetting setting)
        {
            _setting = setting;
        }

        public override Message GetLabel() => _setting?.Label;
        public override string GetTypeKey() => "toggle";
        public override Message GetStatusString() => StateMessage(_setting != null && _setting.Value);

        public bool Activate()
        {
            if (_setting == null)
            {
                return false;
            }

            _setting.Value = !_setting.Value;
            SpeechManager.Output(StateMessage(_setting.Value));
            UIManager.RefreshBuffersFor(this);
            return true;
        }

        public bool HandleAction(InputAction action)
        {
            switch (action?.Key)
            {
                case "ui_accept":
                case "ui_select":
                    return Activate();
                case "ui_left":
                case "ui_right":
                    return true;
                default:
                    return false;
            }
        }

        private static Message StateMessage(bool isOn)
        {
            return new Message(isOn ? "state.on" : "state.off");
        }
    }
}

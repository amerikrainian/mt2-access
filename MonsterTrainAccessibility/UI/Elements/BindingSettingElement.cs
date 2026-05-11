using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.ModSettings;
using MonsterTrainAccessibility.UI.Screens;
using ModScreenManager = MonsterTrainAccessibility.UI.Screens.ScreenManager;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class BindingSettingElement : UIElement, IActivatableElement
    {
        private readonly BindingSetting _setting;

        public BindingSettingElement(BindingSetting setting)
        {
            _setting = setting;
        }

        public override Message GetLabel()
        {
            return Message.Localized("ui", "KEYBINDINGS.ACTION_SUMMARY", new
            {
                action = _setting?.Action?.Label ?? string.Empty,
                bindings = GetBindingsSummary()
            });
        }

        public override string GetTypeKey() => "button";

        public bool Activate()
        {
            if (_setting == null)
            {
                return false;
            }

            ModScreenManager.PushScreen(new BindingListScreen(_setting));
            return true;
        }

        private string GetBindingsSummary()
        {
            if (_setting == null || _setting.Action.Bindings.Count == 0)
            {
                return Message.Localized("ui", "KEYBINDINGS.NONE").Resolve();
            }

            return BindingText.Summarize(_setting.Action.Bindings);
        }
    }
}

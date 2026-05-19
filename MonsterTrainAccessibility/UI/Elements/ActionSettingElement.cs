using System;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.ModSettings;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ActionSettingElement : UIElement, IActivatableElement
    {
        private readonly ActionSetting _setting;
        private readonly Action<ActionSetting> _activated;

        public ActionSettingElement(ActionSetting setting, Action<ActionSetting> activated = null)
        {
            _setting = setting;
            _activated = activated;
        }

        public ActionSetting Setting => _setting;

        public override Message GetLabel() => _setting?.Label;
        public override string GetTypeKey() => "button";

        public bool Activate()
        {
            if (_setting == null || !_setting.Activate())
            {
                return false;
            }

            _activated?.Invoke(_setting);
            return true;
        }
    }
}

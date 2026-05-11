using System;
using System.Collections.Generic;
using MonsterTrainAccessibility.Input;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.ModSettings;
using MonsterTrainAccessibility.Speech;
using MonsterTrainAccessibility.UI.Elements;

namespace MonsterTrainAccessibility.UI.Screens
{
    internal sealed class ModSettingsScreen : Screen
    {
        private readonly CategorySetting _category;
        private readonly ListContainer _root;

        public ModSettingsScreen(CategorySetting category)
        {
            _category = category;
            _root = new ListContainer
            {
                ContainerLabel = category?.Label?.Resolve(),
                AnnounceName = true,
                AnnouncePosition = true,
                NavigationAxis = NavigationAxis.Vertical
            };
            RootElement = _root;

            ClaimAction("ui_up");
            ClaimAction("ui_down");
            ClaimAction("ui_left");
            ClaimAction("ui_right");
            ClaimAction("ui_accept");
            ClaimAction("ui_select");
            ClaimAction("ui_cancel");
            ClaimAction("mod_settings");

            BuildControls();
        }

        public override string ScreenName => _category?.Label?.Resolve();

        public override void OnPush()
        {
            _root.FocusFirst();
        }

        public override void OnFocus()
        {
            if (_root.FocusIndex >= 0)
            {
                _root.SetFocusIndex(_root.FocusIndex);
                return;
            }

            _root.FocusFirst();
        }

        public override bool OnActionJustPressed(InputAction action)
        {
            if (action == null)
            {
                return false;
            }

            switch (action.Key)
            {
                case "ui_cancel":
                case "mod_settings":
                    ScreenManager.RemoveScreen(this);
                    SpeechManager.Output(Message.Localized("ui", "MOD_SETTINGS.CLOSED"));
                    return true;
                default:
                    return _root.HandleAction(action);
            }
        }

        public override bool BlocksGameInput(InputAction action)
        {
            return action != null;
        }

        public override bool ShouldAcceptGameSelection() => false;

        private void BuildControls()
        {
            if (_category == null)
            {
                return;
            }

            List<Setting> settings = new List<Setting>(_category.Children);
            settings.Sort(CompareSettings);

            for (int i = 0; i < settings.Count; i++)
            {
                Setting setting = settings[i];
                if (setting is CategorySetting category)
                {
                    _root.Add(new ProxyModSettingsCategory(category));
                }
                else if (setting is BoolSetting boolSetting)
                {
                    _root.Add(new BoolSettingElement(boolSetting));
                }
                else if (setting is BindingSetting bindingSetting)
                {
                    _root.Add(new BindingSettingElement(bindingSetting));
                }
            }
        }

        private static int CompareSettings(Setting left, Setting right)
        {
            string leftLabel = left?.Label?.Resolve() ?? string.Empty;
            string rightLabel = right?.Label?.Resolve() ?? string.Empty;
            return string.Compare(leftLabel, rightLabel, StringComparison.CurrentCultureIgnoreCase);
        }
    }
}

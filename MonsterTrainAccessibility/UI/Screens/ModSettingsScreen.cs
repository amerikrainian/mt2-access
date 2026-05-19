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
                    UIElement element = _category.ReorderableChildren
                        ? (UIElement)BuildReorderableRow(category)
                        : new ProxyModSettingsCategory(category);
                    _root.Add(element);
                }
                else if (setting is BoolSetting boolSetting)
                {
                    _root.Add(new BoolSettingElement(boolSetting));
                }
                else if (setting is BindingSetting bindingSetting)
                {
                    _root.Add(new BindingSettingElement(bindingSetting));
                }
                else if (setting is ActionSetting actionSetting)
                {
                    _root.Add(new ActionSettingElement(actionSetting, HandleActionSettingActivated));
                }
            }
        }

        private RowContainer BuildReorderableRow(CategorySetting category)
        {
            RowContainer row = new RowContainer(category.Label?.Resolve())
            {
                Tag = category
            };

            if (category.CanConfigure)
            {
                row.Add(new ActionElement(
                    label: () => Message.Localized("ui", "VERBOSITY_SETTINGS.CONFIGURE"),
                    typeKey: "button",
                    activate: () =>
                    {
                        ScreenManager.PushScreen(new ModSettingsScreen(category));
                        return true;
                    }));
            }

            row.Add(new ActionElement(
                label: () => Message.Localized("ui", "VERBOSITY_SETTINGS.MOVE_UP"),
                typeKey: "button",
                activate: () => MoveRow(row, -1)));
            row.Add(new ActionElement(
                label: () => Message.Localized("ui", "VERBOSITY_SETTINGS.MOVE_DOWN"),
                typeKey: "button",
                activate: () => MoveRow(row, 1)));

            return row;
        }

        private bool MoveRow(RowContainer row, int direction)
        {
            int index = _root.IndexOf(row);
            if (index < 0)
            {
                return true;
            }

            int neighbor = -1;
            for (int i = index + direction; i >= 0 && i < _root.Children.Count; i += direction)
            {
                if (_root.Children[i] is RowContainer)
                {
                    neighbor = i;
                    break;
                }
            }

            if (neighbor < 0)
            {
                SpeechManager.Output(Message.Localized(
                    "ui",
                    "VERBOSITY_SETTINGS.SLOT_AT_BOUNDARY",
                    new
                    {
                        boundary = Message.Localized(
                            "ui",
                            direction < 0
                                ? "VERBOSITY_SETTINGS.BOUNDARY_TOP"
                                : "VERBOSITY_SETTINGS.BOUNDARY_BOTTOM").Resolve()
                    }));
                return true;
            }

            RowContainer neighborRow = _root.Children[neighbor] as RowContainer;
            _category.OnChildSwap?.Invoke(row.Tag, neighborRow?.Tag);
            _root.Swap(index, neighbor);
            SpeechManager.Output(MoveFeedback(row));
            return true;
        }

        private Message MoveFeedback(RowContainer row)
        {
            int position = 0;
            for (int i = 0; i < _root.Children.Count; i++)
            {
                if (!(_root.Children[i] is RowContainer))
                {
                    continue;
                }

                position++;
                if (ReferenceEquals(_root.Children[i], row))
                {
                    break;
                }
            }

            Setting setting = row?.Tag;
            return Message.Localized(
                "ui",
                "VERBOSITY_SETTINGS.SLOT_MOVED",
                new
                {
                    slot = setting?.Label?.Resolve() ?? string.Empty,
                    position
                });
        }

        private void RebuildControls(string focusKey)
        {
            _root.Clear();
            BuildControls();
            if (!string.IsNullOrEmpty(focusKey))
            {
                for (int i = 0; i < _root.Children.Count; i++)
                {
                    ActionSettingElement action = _root.Children[i] as ActionSettingElement;
                    if (action?.Setting?.Key == focusKey)
                    {
                        _root.SetFocusIndex(i);
                        return;
                    }
                }
            }

            _root.FocusFirst();
        }

        private void HandleActionSettingActivated(ActionSetting setting)
        {
            if (setting == null)
            {
                return;
            }

            if (setting.SuccessMessage != null)
            {
                SpeechManager.Output(setting.SuccessMessage);
            }

            if (setting.RebuildScreenOnActivate)
            {
                RebuildControls(setting.Key);
            }
        }

        private static int CompareSettings(Setting left, Setting right)
        {
            int leftPriority = left?.SortPriority ?? int.MaxValue;
            int rightPriority = right?.SortPriority ?? int.MaxValue;
            if (leftPriority != rightPriority)
            {
                return leftPriority.CompareTo(rightPriority);
            }

            string leftLabel = left?.Label?.Resolve() ?? string.Empty;
            string rightLabel = right?.Label?.Resolve() ?? string.Empty;
            return string.Compare(leftLabel, rightLabel, StringComparison.CurrentCultureIgnoreCase);
        }
    }
}

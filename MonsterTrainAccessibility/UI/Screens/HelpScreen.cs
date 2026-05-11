using System.Collections.Generic;
using MonsterTrainAccessibility.Buffers;
using MonsterTrainAccessibility.Help;
using MonsterTrainAccessibility.Input;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Speech;
using MonsterTrainAccessibility.UI.Elements;

namespace MonsterTrainAccessibility.UI.Screens
{
    internal sealed class HelpScreen : Screen
    {
        private readonly ListContainer _root;
        private bool _closed;

        public HelpScreen(IReadOnlyList<HelpActionEntry> actions)
        {
            _root = new ListContainer
            {
                ContainerLabel = Message.Localized("ui", "HELP.TITLE").Resolve(),
                AnnounceName = true,
                AnnouncePosition = true,
                NavigationAxis = NavigationAxis.Vertical
            };
            RootElement = _root;

            ClaimAllActions();
            BuildActions(actions);
        }

        public override string ScreenName => Message.Localized("ui", "HELP.TITLE").Resolve();

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
                return true;
            }

            if (_closed)
            {
                return true;
            }

            switch (action.Key)
            {
                case "ui_cancel":
                case "help":
                case "ui_accept":
                case "ui_select":
                    Close(announce: true);
                    return true;
                case "ui_up":
                case "ui_down":
                case "ui_left":
                case "ui_right":
                    _root.HandleAction(action);
                    return true;
                case "buffer_prev_item":
                    BufferManager.Instance.PreviousItem();
                    return true;
                case "buffer_next_item":
                    BufferManager.Instance.NextItem();
                    return true;
                case "buffer_prev":
                    BufferManager.Instance.PreviousBuffer();
                    return true;
                case "buffer_next":
                    BufferManager.Instance.NextBuffer();
                    return true;
                default:
                    return true;
            }
        }

        public override bool BlocksGameInput(InputAction action)
        {
            return action != null;
        }

        public override bool ShouldAcceptGameSelection() => false;

        private void BuildActions(IReadOnlyList<HelpActionEntry> actions)
        {
            if (actions == null || actions.Count == 0)
            {
                _root.Add(new CustomElement(() => Message.Localized("ui", "HELP.NO_ACTIONS")));
                return;
            }

            for (int i = 0; i < actions.Count; i++)
            {
                HelpActionEntry entry = actions[i];
                _root.Add(new ProxyHelpAction(entry));
            }
        }

        private void Close(bool announce)
        {
            if (_closed)
            {
                return;
            }

            _closed = true;
            ScreenManager.RemoveFromTree(this);
            if (announce)
            {
                SpeechManager.Output(Message.Localized("ui", "HELP.CLOSED"));
            }
        }
    }
}

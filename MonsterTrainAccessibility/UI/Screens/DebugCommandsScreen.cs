using System;
using System.Collections.Generic;
using MonsterTrainAccessibility.Buffers;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Input;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Speech;
using MonsterTrainAccessibility.UI.Elements;

namespace MonsterTrainAccessibility.UI.Screens
{
    internal sealed class DebugCommandsScreen : Screen
    {
        private enum Mode
        {
            Root,
            Events,
            Artifacts,
            TestScreens
        }

        private readonly Mode _mode;
        private readonly ListContainer _root;
        private bool _closed;

        public DebugCommandsScreen()
            : this(Mode.Root)
        {
        }

        private DebugCommandsScreen(Mode mode)
        {
            _mode = mode;
            _root = new ListContainer
            {
                ContainerLabel = ScreenName,
                AnnounceName = true,
                AnnouncePosition = true,
                NavigationAxis = NavigationAxis.Vertical
            };
            RootElement = _root;

            ClaimAllActions();
            BuildControls();
        }

        public override string ScreenName => ModeTitle(_mode).Resolve();

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
            if (action == null || _closed)
            {
                return true;
            }

            switch (action.Key)
            {
                case "ui_cancel":
                case "debug_commands":
                    Close(announce: true);
                    return true;
                case "ui_up":
                case "ui_down":
                case "ui_left":
                case "ui_right":
                case "ui_accept":
                case "ui_select":
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

        private void BuildControls()
        {
            switch (_mode)
            {
                case Mode.Root:
                    AddRootControls();
                    break;
                case Mode.Events:
                    AddEventControls();
                    break;
                case Mode.Artifacts:
                    AddArtifactControls();
                    break;
                case Mode.TestScreens:
                    AddTestScreenControls();
                    break;
            }
        }

        private void AddRootControls()
        {
            _root.Add(CreateAction(
                () => Message.Localized("ui", "DEBUG.EVENTS"),
                () => ReplaceWith(Mode.Events)));

            _root.Add(CreateAction(
                () => Message.Localized("ui", "DEBUG.ARTIFACTS"),
                () => ReplaceWith(Mode.Artifacts)));

            _root.Add(CreateAction(
                () => Message.Localized("ui", "DEBUG.TEST_SCREENS"),
                () => ReplaceWith(Mode.TestScreens)));

            _root.Add(CreateAction(
                () => Message.Localized("ui", "DEBUG.CLOSE"),
                () =>
                {
                    Close(announce: true);
                    return true;
                }));
        }

        private void AddEventControls()
        {
            AddBackControl();
            AllGameData allGameData = GetAllGameData();
            IReadOnlyList<StoryEventData> events = allGameData?.GetAllStoryEventData();
            List<StoryEventData> sortedEvents = new List<StoryEventData>();
            if (events != null)
            {
                for (int i = 0; i < events.Count; i++)
                {
                    if (events[i] != null)
                    {
                        sortedEvents.Add(events[i]);
                    }
                }
            }

            sortedEvents.Sort((left, right) => string.Compare(GetStoryEventLabel(left), GetStoryEventLabel(right), StringComparison.CurrentCultureIgnoreCase));
            if (sortedEvents.Count == 0)
            {
                _root.Add(new CustomElement(() => Message.Localized("ui", "DEBUG.NO_ITEMS")));
                return;
            }

            for (int i = 0; i < sortedEvents.Count; i++)
            {
                StoryEventData storyEvent = sortedEvents[i];
                _root.Add(CreateAction(
                    () => Message.RawCleaned(GetStoryEventLabel(storyEvent)),
                    () => OpenStoryEvent(storyEvent)));
            }
        }

        private void AddArtifactControls()
        {
            AddBackControl();
            AllGameData allGameData = GetAllGameData();
            IReadOnlyList<CollectableRelicData> relics = allGameData?.GetAllCollectableRelicData();
            List<CollectableRelicData> sortedRelics = new List<CollectableRelicData>();
            if (relics != null)
            {
                for (int i = 0; i < relics.Count; i++)
                {
                    if (relics[i] != null)
                    {
                        sortedRelics.Add(relics[i]);
                    }
                }
            }

            sortedRelics.Sort((left, right) => string.Compare(GetRelicLabel(left), GetRelicLabel(right), StringComparison.CurrentCultureIgnoreCase));
            if (sortedRelics.Count == 0)
            {
                _root.Add(new CustomElement(() => Message.Localized("ui", "DEBUG.NO_ITEMS")));
                return;
            }

            for (int i = 0; i < sortedRelics.Count; i++)
            {
                CollectableRelicData relic = sortedRelics[i];
                _root.Add(CreateAction(
                    () => Message.RawCleaned(GetRelicLabel(relic)),
                    () => AddRelic(relic)));
            }
        }

        private void AddTestScreenControls()
        {
            AddBackControl();
            _root.Add(CreateAction(
                () => Message.Localized("ui", "DEBUG.CREDITS"),
                OpenCredits));
        }

        private void AddBackControl()
        {
            _root.Add(CreateAction(
                () => Message.Localized("ui", "DEBUG.BACK"),
                () => ReplaceWith(Mode.Root)));
        }

        private static ActionElement CreateAction(Func<Message> label, Func<bool> activate)
        {
            return new ActionElement(
                label: label,
                typeKey: "button",
                activate: activate);
        }

        private bool ReplaceWith(Mode mode)
        {
            ScreenManager.ReplaceScreen(this, new DebugCommandsScreen(mode));
            return true;
        }

        private bool OpenStoryEvent(StoryEventData storyEvent)
        {
            if (storyEvent == null)
            {
                return false;
            }

            SaveManager saveManager = GameManagers.GetSaveManager();
            global::ScreenManager gameScreenManager = GameManagers.GetScreenManager();
            string label = GetStoryEventLabel(storyEvent);
            if (saveManager == null || gameScreenManager == null || !saveManager.IsInActiveGameplay())
            {
                SpeechManager.Output(Message.Localized("ui", "DEBUG.NO_ACTIVE_GAMEPLAY"));
                return true;
            }

            try
            {
                Log.Info("[AccessibilityDebug] Opening story event: " + label + " (" + storyEvent.GetID() + ")");
                Close(announce: false);
                gameScreenManager.ShowStoryEventScreen(storyEvent, saveManager.IsRegionRun);
                SpeechManager.Output(Message.Localized("ui", "DEBUG.EVENT_OPENED", new { name = label }));
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("[AccessibilityDebug] Failed to open story event " + label + ": " + ex);
                SpeechManager.Output(Message.Localized("ui", "DEBUG.EVENT_OPEN_FAILED", new { name = label }));
                return true;
            }
        }

        private bool AddRelic(CollectableRelicData relic)
        {
            if (relic == null)
            {
                return false;
            }

            SaveManager saveManager = GameManagers.GetSaveManager();
            string label = GetRelicLabel(relic);
            if (saveManager == null || !saveManager.HasRun() || !saveManager.IsAlive())
            {
                SpeechManager.Output(Message.Localized("ui", "DEBUG.NO_ACTIVE_RUN"));
                return true;
            }

            try
            {
                Log.Info("[AccessibilityDebug] Adding artifact: " + label + " (" + relic.GetID() + ")");
                saveManager.AddRelic(relic);
                SpeechManager.Output(Message.Localized("ui", "DEBUG.ARTIFACT_ADDED", new { name = label }));
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("[AccessibilityDebug] Failed to add artifact " + label + ": " + ex);
                SpeechManager.Output(Message.Localized("ui", "DEBUG.ARTIFACT_ADD_FAILED", new { name = label }));
                return true;
            }
        }

        private bool OpenCredits()
        {
            global::ScreenManager gameScreenManager = GameManagers.GetScreenManager();
            if (gameScreenManager == null)
            {
                SpeechManager.Output(Message.Localized("ui", "DEBUG.SCREEN_OPEN_FAILED", new { name = Message.Localized("ui", "DEBUG.CREDITS").Resolve() }));
                return true;
            }

            try
            {
                Log.Info("[AccessibilityDebug] Opening test screen: Credits");
                Close(announce: false);
                gameScreenManager.ShowScreen(global::ScreenName.Credits);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("[AccessibilityDebug] Failed to open credits screen: " + ex);
                SpeechManager.Output(Message.Localized("ui", "DEBUG.SCREEN_OPEN_FAILED", new { name = Message.Localized("ui", "DEBUG.CREDITS").Resolve() }));
                return true;
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
                SpeechManager.Output(Message.Localized("ui", "DEBUG.CLOSED"));
            }
        }

        private static AllGameData GetAllGameData()
        {
            SaveManager saveManager = GameManagers.GetSaveManager();
            return saveManager?.GetAllGameData();
        }

        private static Message ModeTitle(Mode mode)
        {
            switch (mode)
            {
                case Mode.Events:
                    return Message.Localized("ui", "DEBUG.EVENTS");
                case Mode.Artifacts:
                    return Message.Localized("ui", "DEBUG.ARTIFACTS");
                case Mode.TestScreens:
                    return Message.Localized("ui", "DEBUG.TEST_SCREENS");
                default:
                    return Message.Localized("ui", "DEBUG.TITLE");
            }
        }

        private static string GetStoryEventLabel(StoryEventData storyEvent)
        {
            if (storyEvent == null)
            {
                return string.Empty;
            }

            string label = Message.Clean(storyEvent.Cheat_GetNameEnglish());
            if (Message.ShouldAdd(label))
            {
                return label;
            }

            label = Message.Clean(storyEvent.GetAssetKey());
            if (Message.ShouldAdd(label))
            {
                return label;
            }

            return Message.Clean(storyEvent.GetID());
        }

        private static string GetRelicLabel(CollectableRelicData relic)
        {
            if (relic == null)
            {
                return string.Empty;
            }

            string label = Message.Clean(relic.GetName());
            if (Message.ShouldAdd(label))
            {
                return label;
            }

            label = Message.Clean(relic.Cheat_GetNameEnglish());
            if (Message.ShouldAdd(label))
            {
                return label;
            }

            label = Message.Clean(relic.GetAssetKey());
            if (Message.ShouldAdd(label))
            {
                return label;
            }

            return Message.Clean(relic.GetID());
        }
    }
}

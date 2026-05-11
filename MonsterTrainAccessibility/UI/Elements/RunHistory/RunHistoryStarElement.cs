using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Speech;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class RunHistoryStarElement : UIElement, IActivatableElement, INavigationTargetElement
    {
        private readonly global::RunHistoryEntryUI _row;
        private readonly SaveManager _saveManager;

        public RunHistoryStarElement(global::RunHistoryEntryUI row, SaveManager saveManager)
        {
            _row = row;
            _saveManager = saveManager;
        }

        public override bool IsVisible => _row != null && _row.gameObject.activeInHierarchy && _row.GetRunData() != null;
        public override string GetTypeKey() => "toggle";
        public override Message GetLabel() => Message.Localized("ui", "RUN_HISTORY.FAVORITE");
        public override Message GetStatusString() => StateMessage(IsStarred(_row, _saveManager));

        public bool Activate()
        {
            global::RunAggregateData data = _row?.GetRunData();
            if (_saveManager == null || data == null)
            {
                return false;
            }

            bool starred = !_saveManager.RunHistoryManager.IsStarred(data.GetID());
            _saveManager.RunHistoryManager.SetStarred(data.GetID(), starred);
            _row.SetStarred(starred);
            SpeechManager.Output(StateMessage(starred));
            UIManager.RefreshBuffersFor(this);
            return true;
        }

        public void SelectForNavigation()
        {
            global::InputManager.Inst?.SelectGameUIComponent(null, allowClearingSelection: true);
        }

        private static bool IsStarred(global::RunHistoryEntryUI row, SaveManager saveManager)
        {
            global::RunAggregateData data = row?.GetRunData();
            return saveManager != null && data != null && saveManager.RunHistoryManager.IsStarred(data.GetID());
        }

        private static Message StateMessage(bool isOn)
        {
            return new Message(isOn ? "state.on" : "state.off");
        }
    }
}

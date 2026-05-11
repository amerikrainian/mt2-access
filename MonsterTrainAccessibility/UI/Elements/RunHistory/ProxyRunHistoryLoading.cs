using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Screens;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyRunHistoryLoading : ProxyElement, INavigationTargetElement
    {
        private readonly global::RunHistoryUI _runHistory;

        public ProxyRunHistoryLoading(global::RunHistoryUI runHistory)
        {
            _runHistory = runHistory;
        }

        public override bool IsVisible => IsFetching();

        public override Message GetLabel()
        {
            return Message.Localized("ui", "RUN_HISTORY.LOADING");
        }

        public void SelectForNavigation()
        {
            global::InputManager.Inst?.SelectGameUIComponent(null, allowClearingSelection: true);
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
        }

        private bool IsFetching()
        {
            return _runHistory != null && Screens.RunHistoryScreen.IsFetchingGameRuns(_runHistory);
        }
    }
}

using MonsterTrainAccessibility.UI.Elements;

namespace MonsterTrainAccessibility.UI.Screens
{
    internal sealed class RunHistoryPaginatorSource : IPageNavigationSource
    {
        private readonly global::PaginationControls _pagination;

        public RunHistoryPaginatorSource(global::PaginationControls pagination)
        {
            _pagination = pagination;
        }

        public int CurrentPage => _pagination != null ? _pagination.CurrentPage : 1;
        public bool HasPrevious => _pagination != null && _pagination.CurrentPage > 1;
        public bool HasNext => _pagination != null && _pagination.CurrentPage < _pagination.LastPage;
        public bool IsVisible => _pagination == null || _pagination.gameObject.activeInHierarchy;

        public void Previous()
        {
            if (HasPrevious)
            {
                _pagination.pageRequestSignal.Dispatch(_pagination.CurrentPage - 1);
            }
        }

        public void Next()
        {
            if (HasNext)
            {
                _pagination.pageRequestSignal.Dispatch(_pagination.CurrentPage + 1);
            }
        }

        public void SelectForNavigation()
        {
            global::InputManager.Inst?.SelectGameUIComponent(null, allowClearingSelection: true);
        }
    }
}

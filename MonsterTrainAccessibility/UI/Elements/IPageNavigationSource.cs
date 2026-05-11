namespace MonsterTrainAccessibility.UI.Elements
{
    internal interface IPageNavigationSource
    {
        int CurrentPage { get; }
        bool HasPrevious { get; }
        bool HasNext { get; }
        bool IsVisible { get; }
        void Previous();
        void Next();
        void SelectForNavigation();
    }
}

using System;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Input;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Speech;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class PaginatorElement : CustomElement, INavigationActionHandler, IActivatableElement, INavigationTargetElement
    {
        private readonly IPageNavigationSource _source;

        public PaginatorElement(IPageNavigationSource source)
            : base(
                label: () => PageLabel(source),
                tooltip: () => PageTooltip(source),
                visibility: () => source == null || source.IsVisible)
        {
            _source = source;
        }

        public bool Activate()
        {
            return true;
        }

        public void SelectForNavigation()
        {
            _source?.SelectForNavigation();
        }

        public bool HandleAction(InputAction action)
        {
            switch (action?.Key)
            {
                case "ui_left":
                    return TurnPrevious();
                case "ui_right":
                    return TurnNext();
                default:
                    return false;
            }
        }

        private bool TurnPrevious()
        {
            if (_source == null)
            {
                return false;
            }

            if (!_source.HasPrevious)
            {
                return true;
            }

            _source.Previous();
            ReportPage();
            return true;
        }

        private bool TurnNext()
        {
            if (_source == null)
            {
                return false;
            }

            if (!_source.HasNext)
            {
                return true;
            }

            _source.Next();
            ReportPage();
            return true;
        }

        private void ReportPage()
        {
            SpeechManager.Output(PageLabel(_source));
            UIManager.RefreshBuffersFor(this);
        }

        private static Message PageLabel(IPageNavigationSource source)
        {
            int page = source != null ? Math.Max(1, source.CurrentPage) : 1;
            return Message.Localized("ui", "PAGINATOR.PAGE", new { page });
        }

        private static Message PageTooltip(IPageNavigationSource source)
        {
            bool previous = source != null && source.HasPrevious;
            bool next = source != null && source.HasNext;
            if (previous && next)
            {
                return Message.Localized("ui", "PAGINATOR.PAGE.BOTH");
            }
            if (previous)
            {
                return Message.Localized("ui", "PAGINATOR.PREVIOUS_PAGE");
            }
            if (next)
            {
                return Message.Localized("ui", "PAGINATOR.NEXT_PAGE");
            }

            return null;
        }
    }
}

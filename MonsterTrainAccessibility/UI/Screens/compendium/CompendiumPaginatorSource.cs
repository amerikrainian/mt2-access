using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.UI.Elements;
using ShinyShoe;

namespace MonsterTrainAccessibility.UI.Screens
{
    internal sealed class CompendiumPaginatorSource : IPageNavigationSource
    {
        private static readonly FieldInfo CurrentPageIndexField = AccessTools.Field(typeof(global::PaginatedCompendiumSection), "currentPageIndex")!;

        private readonly global::CompendiumSection _section;
        private readonly PageTurnZone _previousZone;
        private readonly PageTurnZone _nextZone;

        public CompendiumPaginatorSource(
            global::CompendiumSection section,
            List<PageTurnZone> zones)
        {
            _section = section;
            _previousZone = FindPageTurnZone(zones, PageTurnZone.TurnDir.Left);
            _nextZone = FindPageTurnZone(zones, PageTurnZone.TurnDir.Right);
        }

        public int CurrentPage => CurrentPageOf(_section);
        public bool HasPrevious => _section != null && _section.CanTurnPage(PageTurnZone.TurnDir.Left);
        public bool HasNext => _section != null && _section.CanTurnPage(PageTurnZone.TurnDir.Right);
        public bool IsVisible => _section == null || _section.gameObject.activeInHierarchy;

        public void Previous()
        {
            Trigger(_previousZone);
        }

        public void Next()
        {
            Trigger(_nextZone);
        }

        public void SelectForNavigation()
        {
            CompendiumScreen.ClearGameSelection();
        }

        internal static int CurrentPageOf(global::CompendiumSection section)
        {
            if (section is global::PaginatedCompendiumSection)
            {
                return global::MonsterTrainAccessibility.Core.ReflectionUtil.Get<int>(section, CurrentPageIndexField) + 1;
            }

            return 1;
        }

        private void Trigger(PageTurnZone zone)
        {
            if (zone == null || !zone.enabled || !zone.gameObject.activeInHierarchy)
            {
                return;
            }

            CompendiumScreen.ClearGameSelection();
            CoreInputControlMapping mapping = new CoreInputControlMapping(global::InputManager.Controls.Submit, InputType.None, fake: true);
            zone.HandleInputMappingID(mapping, global::InputManager.Controls.Submit);
            CompendiumScreen.ClearGameSelection();
        }

        private static PageTurnZone FindPageTurnZone(List<PageTurnZone> zones, PageTurnZone.TurnDir direction)
        {
            if (zones == null)
            {
                return null;
            }

            for (int i = 0; i < zones.Count; i++)
            {
                PageTurnZone zone = zones[i];
                if (zone != null && zone.direction == direction)
                {
                    return zone;
                }
            }

            return null;
        }
    }
}

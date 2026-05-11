using MonsterTrainAccessibility.UI.Screens;
using System;
using MonsterTrainAccessibility.Localization;

namespace MonsterTrainAccessibility.UI.Elements.Compendium
{
    internal sealed class ProxyCompendiumClanOptionButton : ProxyCompendiumGameButton
    {
        private readonly global::ClanOptionButton _clan;
        private readonly Func<global::ClassData> _selectedClass;

        public ProxyCompendiumClanOptionButton(global::ClanOptionButton clan, Func<global::ClassData> selectedClass)
            : base(clan?.Button)
        {
            _clan = clan;
            _selectedClass = selectedClass;
        }

        public override bool IsVisible => _clan != null && _clan.gameObject.activeInHierarchy;

        public override Message GetLabel()
        {
            return Message.RawCleaned(_clan?.ClassData?.GetTitle());
        }

        public override Message GetStatusString()
        {
            return ReferenceEquals(_clan?.ClassData, _selectedClass != null ? _selectedClass() : null)
                ? Message.Localized("messages", "state.selected")
                : null;
        }

        public override Message GetTooltip()
        {
            return AccessibleScreenText.Tooltip(_clan);
        }
    }
}

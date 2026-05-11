using MonsterTrainAccessibility.UI.Screens;
using System;
using MonsterTrainAccessibility.Localization;

namespace MonsterTrainAccessibility.UI.Elements.Compendium
{
    internal sealed class ProxyCompendiumChampionOptionButton : ProxyCompendiumGameButton
    {
        private readonly global::ChampionOptionButton _champion;
        private readonly Func<global::ClassData> _selectedClass;
        private readonly Func<int> _selectedChampionIndex;

        public ProxyCompendiumChampionOptionButton(
            global::ChampionOptionButton champion,
            Func<global::ClassData> selectedClass,
            Func<int> selectedChampionIndex)
            : base(champion?.Button)
        {
            _champion = champion;
            _selectedClass = selectedClass;
            _selectedChampionIndex = selectedChampionIndex;
        }

        public override bool IsVisible => _champion != null && _champion.gameObject.activeInHierarchy;

        public override Message GetLabel()
        {
            global::CardData card = _champion?.ClassData?.GetChampionCard(_champion.ChampionIndex);
            return card != null ? Message.RawCleaned(card.GetName()) : null;
        }

        public override Message GetStatusString()
        {
            if (_champion != null &&
                _selectedChampionIndex != null &&
                _champion.ChampionIndex == _selectedChampionIndex() &&
                ReferenceEquals(_champion.ClassData, _selectedClass != null ? _selectedClass() : null))
            {
                return Message.Localized("messages", "state.selected");
            }

            return ButtonState(_champion?.Button);
        }
    }
}

using MonsterTrainAccessibility.Localization;
using ShinyShoe;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyDifficultyTierSummary : GameObjectElement
    {
        private readonly GameUISelectableButton _button;
        private readonly global::DifficultyTierInfoUI _info;
        private readonly AllGameData _allGameData;

        public ProxyDifficultyTierSummary(
            GameUISelectableButton button,
            global::DifficultyTierInfoUI info,
            AllGameData allGameData)
            : base(
                button != null ? button.gameObject : null,
                typeKey: "button",
                label: null)
        {
            _button = button;
            _info = info;
            _allGameData = allGameData;
        }

        public override bool IsVisible => _button != null && _button.gameObject.activeInHierarchy;

        public override Message GetLabel()
        {
            DifficultyTierDisplayData display = _allGameData?.GetDifficultyTierDisplayData();
            if (display == null)
            {
                return AuthoredLabelReader.ReadMessage(_button);
            }

            return Message.Localized(
                "ui",
                "DIFFICULTY.LABEL",
                new
                {
                    label = display.GetDifficultyKeyLocalized(),
                    name = display.GetDifficultyTierName(_info?.CurrentDifficultyTier ?? 1)
                });
        }

        public override Message GetStatusString() => GameButtonElement.StateMessage(_button);

        public override Message GetTooltip()
        {
            DifficultyTierDisplayData display = _allGameData?.GetDifficultyTierDisplayData();
            return Message.FromText(display?.GetDifficultyTierDescription(_info?.CurrentDifficultyTier ?? 1));
        }
    }
}

using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Screens;
using TMPro;
using UnityEngine;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyBattleIntroScenarioTitle : ProxyElement, INavigationTargetElement
    {
        private readonly TMP_Text _titleLabel;
        private readonly ScenarioData _scenario;
        private readonly GameObject _target;

        public ProxyBattleIntroScenarioTitle(GameObject target, TMP_Text titleLabel, ScenarioData scenario)
            : base(target)
        {
            _target = target;
            _titleLabel = titleLabel;
            _scenario = scenario;
        }

        public override bool IsVisible => _target != null && _target.activeInHierarchy;

        public override Message GetLabel()
        {
            return Message.FromText(Screens.BattleIntroScreen.FirstText(AccessibilityText.ReadLocalizedText(_titleLabel), _scenario?.GetBattleName()));
        }

        public void SelectForNavigation()
        {
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(_target);
        }
    }
}

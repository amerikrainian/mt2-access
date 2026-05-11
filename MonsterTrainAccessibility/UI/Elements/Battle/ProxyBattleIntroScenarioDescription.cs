using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Screens;
using TMPro;
using UnityEngine;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyBattleIntroScenarioDescription : ProxyElement, INavigationTargetElement
    {
        private readonly TMP_Text _descriptionLabel;
        private readonly ScenarioData _scenario;
        private readonly GameObject _screenObject;

        public ProxyBattleIntroScenarioDescription(TMP_Text descriptionLabel, ScenarioData scenario, GameObject screenObject)
            : base(descriptionLabel != null ? descriptionLabel.gameObject : null)
        {
            _descriptionLabel = descriptionLabel;
            _scenario = scenario;
            _screenObject = screenObject;
        }

        public override bool IsVisible => _screenObject != null && _screenObject.activeInHierarchy && !string.IsNullOrWhiteSpace(Description());

        public override Message GetLabel()
        {
            return Message.FromText(Description());
        }

        public void SelectForNavigation()
        {
            if (Target != null)
            {
                UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(Target);
            }
        }

        private string Description()
        {
            return Screens.BattleIntroScreen.ScenarioDescription(_descriptionLabel, _scenario);
        }
    }
}

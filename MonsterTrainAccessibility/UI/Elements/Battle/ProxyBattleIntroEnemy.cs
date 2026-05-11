using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Screens;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyBattleIntroEnemy : ProxyElement, INavigationTargetElement
    {
        private readonly global::BattleIntroEnemy _enemy;

        public ProxyBattleIntroEnemy(global::BattleIntroEnemy enemy)
            : base(enemy != null ? enemy.gameObject : null)
        {
            _enemy = enemy;
        }

        public override bool IsVisible => _enemy != null && _enemy.gameObject.activeInHierarchy;

        public override Message GetLabel()
        {
            return Screens.BattleIntroScreen.EnemyLabel(_enemy);
        }

        public override Message GetTooltip()
        {
            return Screens.BattleIntroScreen.EnemyTooltip(_enemy);
        }

        public void SelectForNavigation()
        {
            if (Target != null)
            {
                UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(Target);
            }
        }
    }
}

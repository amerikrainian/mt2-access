using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Screens;
using TMPro;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyGameOverGold : UIElement, INavigationTargetElement
    {
        private static readonly FieldInfo GoldLabelField = AccessTools.Field(typeof(global::GoldScoreModifierDisplay), "goldLabel")!;

        private readonly global::GoldScoreModifierDisplay _gold;

        public ProxyGameOverGold(global::GoldScoreModifierDisplay gold)
        {
            _gold = gold;
        }

        public override bool IsVisible => _gold != null && _gold.gameObject.activeInHierarchy;
        public override Message GetLabel()
        {
            TMP_Text goldLabel = Get<TMP_Text>(_gold, GoldLabelField);
            return Message.Join(", ", Message.Localized("ui", "GAME_OVER.GOLD"), AccessibleScreenText.Text(goldLabel));
        }

        public override Message GetTooltip() => AccessibleScreenText.Tooltip(_gold);

        public void SelectForNavigation()
        {
            global::InputManager.Inst?.SelectGameUIComponent(null);
        }

        private static T Get<T>(object owner, FieldInfo field) where T : class
        {
            return owner != null ? field.GetValue(owner) as T : null;
        }
    }
}

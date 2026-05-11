using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI;
using System.Reflection;
using HarmonyLib;
using TMPro;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyTrainStatsUI : GameObjectElement
    {
        private static readonly FieldInfo EnergyPerTurnLabelField = AccessTools.Field(typeof(global::TrainStatsUI), "energyPerTurnLabel")!;
        private static readonly FieldInfo DeploymentPhaseEnergyPerTurnLabelField = AccessTools.Field(typeof(global::TrainStatsUI), "deploymentPhaseEnergyPerTurnLabel")!;
        private static readonly FieldInfo CardsPerTurnLabelField = AccessTools.Field(typeof(global::TrainStatsUI), "cardsPerTurnLabel")!;
        private static readonly FieldInfo CapacityPerFloorLabelField = AccessTools.Field(typeof(global::TrainStatsUI), "capacityPerFloorLabel")!;

        private readonly global::TrainStatsUI _stats;

        public ProxyTrainStatsUI(global::TrainStatsUI stats)
            : base(
                target: stats != null ? stats.gameObject : null,
                typeKey: null,
                label: null)
        {
            _stats = stats;
        }

        public override bool IsVisible => _stats != null && _stats.gameObject.activeInHierarchy;
        public override Message GetLabel() => Message.Localized("ui", "HUD.TRAIN_STATS");
        public override Message GetStatusString()
        {
            return Message.Join(", ",
                Message.Localized("ui", "HUD.TRAIN_STATS_EMBER", new { value = TextValue(EnergyPerTurnLabelField) }),
                Message.Localized("ui", "HUD.TRAIN_STATS_DEPLOYMENT_EMBER", new { value = TextValue(DeploymentPhaseEnergyPerTurnLabelField) }),
                Message.Localized("ui", "HUD.TRAIN_STATS_CARDS", new { value = TextValue(CardsPerTurnLabelField) }),
                Message.Localized("ui", "HUD.TRAIN_STATS_CAPACITY", new { value = TextValue(CapacityPerFloorLabelField) }));
        }

        public override Message GetTooltip() => _stats != null ? TooltipText.ForComponent(_stats) : null;

        private string TextValue(FieldInfo field)
        {
            TMP_Text text = _stats != null ? field.GetValue(_stats) as TMP_Text : null;
            return AccessibilityText.ReadLocalizedText(text);
        }
    }
}

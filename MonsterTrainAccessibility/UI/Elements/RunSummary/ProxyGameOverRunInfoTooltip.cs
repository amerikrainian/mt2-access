using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Screens;
using UnityEngine;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal enum GameOverRunInfoTooltipPart
    {
        Covenant,
        Difficulty,
        Distance,
        EndlessDistance,
        WinStreak,
        TrueFinalBossWinStreak
    }

    internal sealed class ProxyGameOverRunInfoTooltip : UIElement, INavigationTargetElement
    {
        private static readonly FieldInfo CovenantUIField = AccessTools.Field(typeof(global::RunInfo), "covenantUI")!;
        private static readonly FieldInfo DifficultyTierUIField = AccessTools.Field(typeof(global::RunInfo), "difficultyTierUI")!;
        private static readonly FieldInfo DistanceUIField = AccessTools.Field(typeof(global::RunInfo), "distanceUI")!;
        private static readonly FieldInfo EndlessDistanceUIField = AccessTools.Field(typeof(global::RunInfo), "endlessDistanceUI")!;
        private static readonly FieldInfo WinStreakUIField = AccessTools.Field(typeof(global::RunInfo), "winStreakUI")!;
        private static readonly FieldInfo TrueFinalBossWinStreakUIField = AccessTools.Field(typeof(global::RunInfo), "trueFinalBossWinStreakUI")!;

        private readonly global::RunInfo _runInfo;
        private readonly GameOverRunInfoTooltipPart _part;

        public ProxyGameOverRunInfoTooltip(global::RunInfo runInfo, GameOverRunInfoTooltipPart part)
        {
            _runInfo = runInfo;
            _part = part;
        }

        public override bool IsVisible
        {
            get
            {
                Component component = Component();
                return component != null &&
                    component.gameObject.activeInHierarchy &&
                    ProxyGameOverText.HasMessage(GetLabel());
            }
        }

        public override Message GetLabel() => AccessibleScreenText.Tooltip(Component());
        public GameObject Target => Component() != null ? Component().gameObject : null;

        public void SelectForNavigation()
        {
            global::InputManager.Inst?.SelectGameUIComponent(null);
        }

        private Component Component()
        {
            return Get<Component>(_runInfo, FieldFor(_part));
        }

        private static FieldInfo FieldFor(GameOverRunInfoTooltipPart part)
        {
            switch (part)
            {
                case GameOverRunInfoTooltipPart.Covenant:
                    return CovenantUIField;
                case GameOverRunInfoTooltipPart.Difficulty:
                    return DifficultyTierUIField;
                case GameOverRunInfoTooltipPart.Distance:
                    return DistanceUIField;
                case GameOverRunInfoTooltipPart.EndlessDistance:
                    return EndlessDistanceUIField;
                case GameOverRunInfoTooltipPart.WinStreak:
                    return WinStreakUIField;
                case GameOverRunInfoTooltipPart.TrueFinalBossWinStreak:
                    return TrueFinalBossWinStreakUIField;
                default:
                    return CovenantUIField;
            }
        }

        private static T Get<T>(object owner, FieldInfo field) where T : class
        {
            return owner != null ? field.GetValue(owner) as T : null;
        }
    }
}

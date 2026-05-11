using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Screens;
using TMPro;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal enum GameOverRunInfoTextPart
    {
        RunType,
        Points,
        Playtime
    }

    internal sealed class ProxyGameOverRunInfoText : UIElement, INavigationTargetElement
    {
        private static readonly FieldInfo RunTypeLabelField = AccessTools.Field(typeof(global::RunInfo), "runTypeLabel")!;
        private static readonly FieldInfo PointsLabelField = AccessTools.Field(typeof(global::RunInfo), "pointsLabel")!;
        private static readonly FieldInfo PlaytimeLabelField = AccessTools.Field(typeof(global::RunInfo), "playtimeLabel")!;

        private readonly global::RunInfo _runInfo;
        private readonly GameOverRunInfoTextPart _part;

        public ProxyGameOverRunInfoText(global::RunInfo runInfo, GameOverRunInfoTextPart part)
        {
            _runInfo = runInfo;
            _part = part;
        }

        public override bool IsVisible
        {
            get
            {
                TMP_Text text = Text();
                return text != null && text.gameObject.activeInHierarchy && ProxyGameOverText.HasMessage(GetLabel());
            }
        }

        public override Message GetLabel() => AccessibleScreenText.Text(Text());
        public UnityEngine.GameObject Target => Text() != null ? Text().gameObject : null;

        public void SelectForNavigation()
        {
            global::InputManager.Inst?.SelectGameUIComponent(null);
        }

        private TMP_Text Text()
        {
            return Get<TMP_Text>(_runInfo, FieldFor(_part));
        }

        private static FieldInfo FieldFor(GameOverRunInfoTextPart part)
        {
            switch (part)
            {
                case GameOverRunInfoTextPart.RunType:
                    return RunTypeLabelField;
                case GameOverRunInfoTextPart.Points:
                    return PointsLabelField;
                case GameOverRunInfoTextPart.Playtime:
                    return PlaytimeLabelField;
                default:
                    return RunTypeLabelField;
            }
        }

        private static T Get<T>(object owner, FieldInfo field) where T : class
        {
            return owner != null ? field.GetValue(owner) as T : null;
        }
    }
}

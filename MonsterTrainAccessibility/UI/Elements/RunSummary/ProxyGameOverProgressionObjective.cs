using System.Collections.Generic;
using System.Text;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Presentation;
using MonsterTrainAccessibility.Presentation.Progression;
using MonsterTrainAccessibility.UI.Screens;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyGameOverProgressionObjective : UIElement, INavigationTargetElement
    {
        private readonly global::ProgressionObjectiveUI _objective;

        public ProxyGameOverProgressionObjective(global::ProgressionObjectiveUI objective)
        {
            _objective = objective;
        }

        public override bool IsVisible => IsObjectiveVisible(_objective) && ProxyGameOverText.HasMessage(GetLabel());
        public override Message GetLabel()
        {
            return PresentationRenderer.FocusSummary(BuildPresentation());
        }

        public override Message GetTooltip()
        {
            global::MonsterTrainAccessibility.Presentation.Presentation presentation = BuildPresentation();
            List<Message> parts = new List<Message>();
            AddPart(parts, presentation?.Subtitle);
            AddPart(parts, presentation?.Description);
            return parts.Count > 0 ? Message.JoinLines(parts) : null;
        }

        public void SelectForNavigation()
        {
            global::InputManager.Inst?.SelectGameUIComponent(null);
        }

        private global::MonsterTrainAccessibility.Presentation.Presentation BuildPresentation()
        {
            return BuildPresentation(_objective);
        }

        public static void AppendSignature(StringBuilder sb, global::ProgressionObjectiveUI objective)
        {
            if (sb == null)
            {
                return;
            }

            if (objective == null)
            {
                sb.Append("null|");
                return;
            }

            bool visible = IsObjectiveVisible(objective);
            sb.Append(visible ? '1' : '0').Append(':');
            if (visible)
            {
                global::MonsterTrainAccessibility.Presentation.Presentation presentation = BuildPresentation(objective);
                sb.Append(presentation?.Title?.Resolve()).Append('|');
                sb.Append(presentation?.Subtitle?.Resolve()).Append('|');
            }

            sb.Append('|');
        }

        private static bool IsObjectiveVisible(global::ProgressionObjectiveUI objective)
        {
            return objective != null &&
                objective.gameObject.activeInHierarchy &&
                objective.transform.localScale.x > 0.01f;
        }

        private static global::MonsterTrainAccessibility.Presentation.Presentation BuildPresentation(global::ProgressionObjectiveUI objective)
        {
            return PhaseRegistry.ProgressionObjectives.Build(new ProgressionObjectivePresentationSource(objective));
        }

        private static void AddPart(List<Message> parts, Message value)
        {
            if (ProxyGameOverText.HasMessage(value))
            {
                parts.Add(value);
            }
        }
    }
}

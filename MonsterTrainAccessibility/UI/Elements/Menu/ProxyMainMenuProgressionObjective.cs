using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Presentation;
using MonsterTrainAccessibility.Presentation.Progression;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyMainMenuProgressionObjective : UIElement
    {
        private readonly global::ProgressionObjectiveUI _objective;

        public ProxyMainMenuProgressionObjective(global::ProgressionObjectiveUI objective)
        {
            _objective = objective;
        }

        public override Message GetLabel()
        {
            global::MonsterTrainAccessibility.Presentation.Presentation presentation = BuildPresentation();
            return Message.Join(", ", presentation?.Title, presentation?.Subtitle);
        }

        public override Message GetStatusString()
        {
            return BuildPresentation().Description;
        }

        private global::MonsterTrainAccessibility.Presentation.Presentation BuildPresentation()
        {
            return PhaseRegistry.ProgressionObjectives.Build(new ProgressionObjectivePresentationSource(_objective));
        }
    }
}

using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Presentation;
using MonsterTrainAccessibility.Presentation.Progression;
using MonsterTrainAccessibility.Presentation.Verbosity;

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
            return PresentationRenderer.FocusSummary(
                presentation,
                VerbosityRegistry.ForSource<ProgressionObjectivePresentationSource>());
        }

        public override Message GetStatusString()
        {
            return null;
        }

        private global::MonsterTrainAccessibility.Presentation.Presentation BuildPresentation()
        {
            return PhaseRegistry.ProgressionObjectives.Build(new ProgressionObjectivePresentationSource(_objective));
        }
    }
}

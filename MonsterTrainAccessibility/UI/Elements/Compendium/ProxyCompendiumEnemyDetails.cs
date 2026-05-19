using MonsterTrainAccessibility.Buffers;
using MonsterTrainAccessibility.Presentation;
using MonsterTrainAccessibility.Presentation.Compendium;
using MonsterTrainAccessibility.Presentation.Verbosity;
using MonsterTrainAccessibility.UI.Screens;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Elements;

namespace MonsterTrainAccessibility.UI.Elements.Compendium
{
    internal sealed class ProxyCompendiumEnemyDetails : ProxyElement, INavigationTargetElement
    {
        private readonly global::CompendiumEnemyDetailsUI _details;

        public ProxyCompendiumEnemyDetails(global::CompendiumEnemyDetailsUI details)
            : base(details != null ? details.gameObject : null)
        {
            _details = details;
        }

        public override bool IsVisible => _details != null && _details.gameObject.activeInHierarchy;

        public override Message GetLabel()
        {
            return PresentationRenderer.Label(Presentation());
        }

        public override Message GetTooltip()
        {
            return PresentationRenderer.FocusTooltip(
                Presentation(),
                VerbosityRegistry.ForSource<CompendiumEnemyPresentationSource>());
        }

        public override Message GetExtrasString()
        {
            return null;
        }

        internal override string HandleBuffers(BufferManager buffers)
        {
            base.HandleBuffers(buffers);
            PresentationBuffer<CompendiumEnemyPresentationSource> buffer =
                buffers?.GetBuffer("compendium_enemy") as PresentationBuffer<CompendiumEnemyPresentationSource>;
            CompendiumEnemyPresentationSource source = Source();
            if (buffer == null || source == null)
            {
                return "ui";
            }

            buffer.Bind(source);
            buffers.EnableBuffer("compendium_enemy", true);
            return "compendium_enemy";
        }

        public void SelectForNavigation()
        {
            Screens.CompendiumScreen.ClearGameSelection();
        }

        internal CompendiumEnemyPresentationSource Source()
        {
            return CompendiumEnemyPresentationSource.FromDetails(_details);
        }

        private global::MonsterTrainAccessibility.Presentation.Presentation Presentation()
        {
            CompendiumEnemyPresentationSource source = Source();
            return source != null ? PhaseRegistry.CompendiumEnemies.Build(source) : null;
        }
    }
}

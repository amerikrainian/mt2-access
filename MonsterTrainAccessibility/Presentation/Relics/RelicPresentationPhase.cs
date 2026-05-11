using System.Collections.Generic;
using MonsterTrainAccessibility.Localization;

namespace MonsterTrainAccessibility.Presentation.Relics
{
    internal sealed class RelicPresentationPhase : IPhase<RelicPresentationSource>
    {
        public bool Matches(PresentationContext<RelicPresentationSource> ctx)
        {
            return ctx?.Source != null;
        }

        public void Apply(PresentationContext<RelicPresentationSource> ctx, PresentationBuilder builder)
        {
            RelicPresentationSource source = ctx.Source;
            builder.SetTitle(source.Label());
            builder.SetDescription(source.Description());
            for (int i = 0; i < source.ContextLines.Count; i++)
            {
                builder.AddSection(SectionKind.Context, null, source.ContextLines[i]);
            }

            IReadOnlyList<Message> extraTooltips = source.ExtraTooltipParts();
            for (int i = 0; i < extraTooltips.Count; i++)
            {
                builder.AddSection(SectionKind.Tooltip, null, extraTooltips[i]);
            }
        }
    }
}

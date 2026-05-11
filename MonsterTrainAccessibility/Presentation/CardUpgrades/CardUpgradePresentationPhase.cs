using System.Collections.Generic;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Elements;

namespace MonsterTrainAccessibility.Presentation.CardUpgrades
{
    internal sealed class CardUpgradePresentationPhase : IPhase<CardUpgradeData>
    {
        public bool Matches(PresentationContext<CardUpgradeData> ctx)
        {
            return ctx?.Source != null;
        }

        public void Apply(PresentationContext<CardUpgradeData> ctx, PresentationBuilder builder)
        {
            builder.SetTitle(ProxyCardUpgrade.Label(ctx.Source));
            List<Message> parts = ProxyCardUpgrade.TooltipParts(ctx.Source);
            for (int i = 1; i < parts.Count; i++)
            {
                builder.AddSection(SectionKind.Annotation, null, parts[i]);
            }
        }
    }
}

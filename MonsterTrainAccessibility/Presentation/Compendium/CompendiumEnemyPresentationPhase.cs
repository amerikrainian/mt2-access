using System.Collections.Generic;
using MonsterTrainAccessibility.Presentation.Creatures;
using MonsterTrainAccessibility.UI.Elements;

namespace MonsterTrainAccessibility.Presentation.Compendium
{
    internal sealed class CompendiumEnemyPresentationPhase : IPhase<CompendiumEnemyPresentationSource>
    {
        public bool Matches(PresentationContext<CompendiumEnemyPresentationSource> ctx)
        {
            return ctx?.Source != null;
        }

        public void Apply(PresentationContext<CompendiumEnemyPresentationSource> ctx, PresentationBuilder builder)
        {
            CompendiumEnemyPresentationSource source = ctx.Source;
            builder.SetTitle(source.Name);
            for (int i = 0; i < source.Stats.Count; i++)
            {
                builder.AddStat(source.Stats[i]);
            }
            builder.SetDescription(source.Lore);
            builder.AddSection(SectionKind.Context, null, source.Artist, "artist");

            if (source.Character != null && !source.Character.IsDestroyed)
            {
                List<TooltipContent> tooltips = ProxyCombatCreature.CollectSemanticTooltips(source.Character, isFromCompendium: true);
                CreaturePresentationPhase.AddTooltipSections(builder, tooltips);
                return;
            }

            for (int i = 0; i < source.FallbackEffects.Count; i++)
            {
                CompendiumEnemyPresentationSource.EffectBlock effect = source.FallbackEffects[i];
                builder.AddSection(SectionKind.Tooltip, effect.Title, effect.Body, effect.Key);
            }
        }
    }
}

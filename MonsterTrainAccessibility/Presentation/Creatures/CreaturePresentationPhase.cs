using System.Collections.Generic;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Elements;
using MonsterTrainAccessibility.Util;

namespace MonsterTrainAccessibility.Presentation.Creatures
{
    internal sealed class CreaturePresentationPhase : IPhase<CharacterState>
    {
        public bool Matches(PresentationContext<CharacterState> ctx)
        {
            return ctx?.Source != null && !ctx.Source.IsDestroyed;
        }

        public void Apply(PresentationContext<CharacterState> ctx, PresentationBuilder builder)
        {
            CharacterState character = ctx.Source;
            List<TooltipContent> tooltips = ProxyCombatCreature.CollectSemanticTooltips(character);
            builder.SetTitle(ProxyCombatCreature.Label(character));
            builder.SetSubtitle(ProxyCombatCreature.Status(character, tooltips));
            builder.SetDescription(ProxyCombatCreature.MainTooltipBody(character));
            AddTooltipSections(builder, tooltips);
            AddBossIntent(builder, character);
        }

        internal static void AddTooltipSections(PresentationBuilder builder, IList<TooltipContent> tooltips)
        {
            if (tooltips == null)
            {
                return;
            }

            for (int i = 0; i < tooltips.Count; i++)
            {
                TooltipContent tooltip = tooltips[i];
                if (tooltip.IsEmpty())
                {
                    continue;
                }

                string key = MessageList.TooltipKey(tooltip);
                builder.AddSection(
                    SectionKind.Tooltip,
                    MessageList.TooltipTitle(tooltip),
                    null,
                    key + ":title");
                builder.AddSection(
                    SectionKind.Tooltip,
                    null,
                    Message.FromText(tooltip.body),
                    key + ":body");
            }
        }

        private static void AddBossIntent(PresentationBuilder builder, CharacterState character)
        {
            if (character == null || !character.IsOuterTrainBoss())
            {
                return;
            }

            IReadOnlyList<Message> intentParts = ProxyCombatCreature.BossIntent(character).Parts;
            for (int i = 0; i < intentParts.Count; i++)
            {
                builder.AddSection(SectionKind.Annotation, null, intentParts[i]);
            }
        }
    }
}

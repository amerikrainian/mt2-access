using System.Collections.Generic;
using System;
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
            builder.SetSubtitle(ProxyCombatCreature.MainTooltipBody(character));
            IReadOnlyList<Message> stats = ProxyCombatCreature.StatParts(character);
            for (int i = 0; i < stats.Count; i++)
            {
                builder.AddStat(stats[i]);
            }

            AddAbility(builder, character);
            AddConciseTooltipSections(builder, character, tooltips);
            AddTooltipSections(builder, tooltips);
            AddBossIntent(builder, character);
        }

        private static void AddAbility(PresentationBuilder builder, CharacterState character)
        {
            Message ability = ProxyCombatCreature.UnitAbilitySummary(character);
            if (ability != null)
            {
                builder.AddSection(SectionKind.Ability, null, ability, "unit_ability");
            }
        }

        private static void AddConciseTooltipSections(
            PresentationBuilder builder,
            CharacterState character,
            IList<TooltipContent> tooltips)
        {
            if (tooltips == null)
            {
                return;
            }

            HashSet<string> statusIds = ProxyCombatCreature.VisibleStatusIds(character);
            HashSet<string> seenTitles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < tooltips.Count; i++)
            {
                TooltipContent tooltip = tooltips[i];
                if (tooltip.IsEmpty() ||
                    IsSuppressedConciseTooltip(tooltip))
                {
                    continue;
                }

                Message title = MessageList.TooltipTitle(tooltip);
                string resolvedTitle = title?.Resolve();
                if (!Message.ShouldAdd(resolvedTitle) || !seenTitles.Add(resolvedTitle))
                {
                    continue;
                }

                string tooltipId = tooltip.tooltipId ?? resolvedTitle;
                if (!string.IsNullOrWhiteSpace(tooltip.tooltipId) && statusIds.Contains(tooltip.tooltipId))
                {
                    builder.AddSection(SectionKind.Status, null, title, tooltipId);
                }
                else if (tooltip.designType == TooltipDesigner.TooltipDesignType.Trigger)
                {
                    builder.AddSection(SectionKind.Trigger, null, title, tooltipId);
                }
                else
                {
                    builder.AddSection(SectionKind.Annotation, null, title, tooltipId);
                }
            }
        }

        private static bool IsSuppressedConciseTooltip(TooltipContent tooltip)
        {
            string tooltipId = tooltip.tooltipId;
            return string.Equals(tooltipId, "armor", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(tooltipId, "unit_ability", StringComparison.OrdinalIgnoreCase);
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
                    Message.FromText(tooltip.body),
                    key);
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
                builder.AddSection(SectionKind.Intent, null, intentParts[i]);
            }
        }
    }
}

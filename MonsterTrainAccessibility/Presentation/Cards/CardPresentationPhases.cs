using System;
using System.Collections.Generic;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Elements;
using MonsterTrainAccessibility.Util;

namespace MonsterTrainAccessibility.Presentation.Cards
{
    internal sealed class CardIdentityPhase : IPhase<CardState>
    {
        public bool Matches(PresentationContext<CardState> ctx) => ctx?.Source != null;

        public void Apply(PresentationContext<CardState> ctx, PresentationBuilder builder)
        {
            CardState card = ctx.Source;
            builder.SetTitle(Message.FromText(card.GetTitle()));
            builder.SetSubtitle(Metadata(card));
        }

        private static Message Metadata(CardState card)
        {
            List<Message> parts = new List<Message>();
            MessageList.Add(parts, MetadataTypeName(card));
            MessageList.Add(parts, Rarity(card));
            return parts.Count > 0 ? Message.Join(", ", parts) : null;
        }

        private static Message MetadataTypeName(CardState card)
        {
            if (card == null)
            {
                return null;
            }

            List<Message> parts = new List<Message>();
            MessageList.Add(parts, CardTypeCardText(card));
            if (IsBannerUnit(card))
            {
                MessageList.Add(parts, Message.Localized("ui", "CARD_METADATA.BANNER_UNIT"));
            }

            if (parts.Count == 0)
            {
                MessageList.Add(parts, CardTypeName(card));
            }

            return parts.Count > 0 ? Message.Join(", ", parts) : null;
        }

        private static Message CardTypeCardText(CardState card)
        {
            if (card == null || card.GetCardType() == CardType.Invalid)
            {
                return null;
            }

            string text = AccessibilityLocalizationScope.Run(() =>
            {
                card.GetCardTypeCardText(out string cardText);
                return cardText;
            });
            return Message.FromText(text);
        }

        private static Message CardTypeName(CardState card)
        {
            if (card == null || card.GetCardType() == CardType.Invalid)
            {
                return null;
            }

            return Message.RawCleaned(AccessibilityLocalizationScope.Run(() => card.GetCardType().GetLocalizedName()));
        }

        private static Message Rarity(CardState card)
        {
            if (card == null)
            {
                return null;
            }

            CollectableRarity rarity = card.GetRarity();
            return rarity != CollectableRarity.Unset
                ? Message.Localized("ui", "CARD_RARITY." + rarity.ToString().ToUpperInvariant())
                : null;
        }

        private static bool IsBannerUnit(CardState card)
        {
            if (card == null || !card.IsSpawnerCard())
            {
                return false;
            }

            CharacterData character = card.GetSpawnCharacterData();
            List<SubtypeData> subtypes = character?.GetSubtypes();
            if (subtypes == null)
            {
                return false;
            }

            for (int i = 0; i < subtypes.Count; i++)
            {
                if (subtypes[i]?.Key == "SubtypesData_BannerUnit")
                {
                    return true;
                }
            }

            return false;
        }
    }

    internal sealed class CardCostPhase : IPhase<CardState>
    {
        public bool Matches(PresentationContext<CardState> ctx) => ctx?.Source != null;

        public void Apply(PresentationContext<CardState> ctx, PresentationBuilder builder)
        {
            builder.SetCost(Cost(ctx.Source));
        }

        private static Message Cost(CardState card)
        {
            if (card == null)
            {
                return null;
            }

            if (card.IsNonPlayableEnergyCostType())
            {
                return Message.Localized("combat", "CARD.UNPLAYABLE");
            }

            if (card.IsConsumeRemainingEnergyCostType())
            {
                return Message.Localized("combat", "CARD.X_COST");
            }

            AllGameManagers managers = AllGameManagers.Instance.OrNull();
            CardStatistics cardStatistics = managers?.GetCardStatistics();
            if (cardStatistics == null)
            {
                return null;
            }

            int cost = card.GetCost(cardStatistics, managers?.GetMonsterManager(), managers?.GetRelicManager());
            return Message.Localized("combat", "CARD.COST", new { cost });
        }
    }

    internal sealed class CardStatsPhase : IPhase<CardState>
    {
        public bool Matches(PresentationContext<CardState> ctx) => ctx?.Source != null;

        public void Apply(PresentationContext<CardState> ctx, PresentationBuilder builder)
        {
            CardState card = ctx.Source;
            bool isUnit = card.IsSpawnerCard();
            bool isEquipment = card.GetCardType() == CardType.Equipment;
            if (!isUnit && !isEquipment)
            {
                return;
            }

            int attack = card.GetTotalAttackDamage();
            int health = (int)card.GetHealth();
            int size = card.GetSize();
            if (isUnit && size > 0)
            {
                builder.AddStat(Message.Localized("ui", "CARD_STATS.SIZE", new { size }));
            }
            if (isUnit || health > 0)
            {
                builder.AddStat(Message.Localized("ui", "CARD_STATS.HEALTH", new { health }));
            }
            if (isUnit || attack > 0)
            {
                builder.AddStat(Message.Localized("combat", "CREATURE.ATTACK", new { attack }));
            }
        }
    }

    internal sealed class CardGameplayDescriptionPhase : IPhase<CardState>
    {
        public bool Matches(PresentationContext<CardState> ctx) => ctx?.Source != null;

        public void Apply(PresentationContext<CardState> ctx, PresentationBuilder builder)
        {
            builder.SetDescription(GeneratedGameplayText(ctx.Source));
        }

        private static Message GeneratedGameplayText(CardState card)
        {
            AllGameManagers managers = AllGameManagers.Instance.OrNull();
            CardStatistics cardStatistics = managers?.GetCardStatistics();
            if (cardStatistics == null)
            {
                return null;
            }

            string text = AccessibilityLocalizationScope.Run(() => Message.Clean(card.GetCardText(
                cardStatistics,
                managers?.GetSaveManager(),
                includeCurrentTraitEffectText: true)));
            return Message.ShouldAdd(text) ? Message.Raw(text) : null;
        }
    }

    internal sealed class CardTooltipPhase : IPhase<CardState>
    {
        public bool Matches(PresentationContext<CardState> ctx) => ctx?.Source != null;

        public void Apply(PresentationContext<CardState> ctx, PresentationBuilder builder)
        {
            List<TooltipContent> tooltips = ProxyCombatCard.CollectTooltips(ctx.Source, ctx.SaveManager);
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

                builder.AddSection(
                    SectionKind.Tooltip,
                    MessageList.TooltipTitle(tooltip),
                    Message.FromText(tooltip.body),
                    MessageList.TooltipKey(tooltip));
            }
        }
    }

    internal sealed class CardUpgradePhase : IPhase<CardState>
    {
        public bool Matches(PresentationContext<CardState> ctx) => ctx?.Source != null;

        public void Apply(PresentationContext<CardState> ctx, PresentationBuilder builder)
        {
            List<Message> parts = new List<Message>();
            ProxyCombatCard.AddUpgradeSummary(
                ctx.Source,
                parts,
                includeAbilityDescriptions: true,
                includeStatusTooltips: true);

            for (int i = parts.Count - 1; i >= 0; i--)
            {
                builder.AddSection(SectionKind.Upgrade, null, parts[i]);
            }
        }
    }
}

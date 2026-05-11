using MonsterTrainAccessibility.Presentation.Cards;
using MonsterTrainAccessibility.Presentation.CardUpgrades;
using MonsterTrainAccessibility.Presentation.Compendium;
using MonsterTrainAccessibility.Presentation.Creatures;
using MonsterTrainAccessibility.Presentation.Progression;
using MonsterTrainAccessibility.Presentation.Relics;
using MonsterTrainAccessibility.Presentation.Rewards;

namespace MonsterTrainAccessibility.Presentation
{
    internal static class PhaseRegistry
    {
        public static PresentationPipeline<CardState> Cards { get; } =
            new PresentationPipeline<CardState>(new IPhase<CardState>[]
            {
                new CardIdentityPhase(),
                new CardCostPhase(),
                new CardStatsPhase(),
                new CardGameplayDescriptionPhase(),
                new CardTooltipPhase(),
                new CardUpgradePhase()
            });

        public static PresentationPipeline<CharacterState> Creatures { get; } =
            new PresentationPipeline<CharacterState>(new IPhase<CharacterState>[]
            {
                new CreaturePresentationPhase()
            });

        public static PresentationPipeline<CardUpgradeData> CardUpgrades { get; } =
            new PresentationPipeline<CardUpgradeData>(new IPhase<CardUpgradeData>[]
            {
                new CardUpgradePresentationPhase()
            });

        public static PresentationPipeline<RelicPresentationSource> Relics { get; } =
            new PresentationPipeline<RelicPresentationSource>(new IPhase<RelicPresentationSource>[]
            {
                new RelicPresentationPhase()
            });

        public static PresentationPipeline<RewardPresentationSource> Rewards { get; } =
            new PresentationPipeline<RewardPresentationSource>(new IPhase<RewardPresentationSource>[]
            {
                new RewardPresentationPhase()
            });

        public static PresentationPipeline<ProgressionObjectivePresentationSource> ProgressionObjectives { get; } =
            new PresentationPipeline<ProgressionObjectivePresentationSource>(new IPhase<ProgressionObjectivePresentationSource>[]
            {
                new ProgressionObjectivePresentationPhase()
            });

        public static PresentationPipeline<CompendiumEnemyPresentationSource> CompendiumEnemies { get; } =
            new PresentationPipeline<CompendiumEnemyPresentationSource>(new IPhase<CompendiumEnemyPresentationSource>[]
            {
                new CompendiumEnemyPresentationPhase()
            });
    }
}

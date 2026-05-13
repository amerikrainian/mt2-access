namespace MonsterTrainAccessibility.UI.Screens
{
    internal static class ScreenRegistration
    {
        private static bool _registered;

        public static void RegisterAll()
        {
            if (_registered)
            {
                return;
            }

            _registered = true;

            ScreenManager.RegisterUIScreen<global::IntroStartScreen>(global::ScreenName.IntroStart, screen => new IntroStartScreen(screen));
            ScreenManager.RegisterUIScreen<global::FirstTimeSettingsScreen>(global::ScreenName.FirstTimeSettings, screen => new FirstTimeSettingsScreen(screen));
            ScreenManager.RegisterUIScreen<global::MainMenuScreen>(global::ScreenName.MainMenu, screen => new MainMenuScreen(screen));
            ScreenManager.RegisterUIScreen<global::SoulSaviorScreen>(global::ScreenName.SoulSavior, screen => new SoulSaviorScreen(screen));
            ScreenManager.RegisterUIScreen<global::SoulforgeScreen>(global::ScreenName.Soulforge, screen => new SoulforgeScreen(screen));
            ScreenManager.RegisterUIScreen<global::TrainCosmeticsScreen>(global::ScreenName.TrainCosmetics, screen => new TrainCosmeticsScreen(screen));
            ScreenManager.RegisterUIScreen<global::SettingsScreen>(global::ScreenName.Settings, screen => new SettingsScreen(screen));
            ScreenManager.RegisterUIScreen<global::KeyMappingScreen>(global::ScreenName.KeyMapping, screen => new KeyMappingScreen(screen));
            ScreenManager.RegisterUIScreen<global::RunSetupScreen>(global::ScreenName.RunSetup, screen => new RunSetupScreen(screen));
            ScreenManager.RegisterUIScreen<global::SoulSaviorRunSetupScreen>(global::ScreenName.SoulSaviorRunSetup, screen => new SoulSaviorRunSetupScreen(screen));
            ScreenManager.RegisterUIScreen<global::RunOpeningScreen>(global::ScreenName.RunOpening, screen => new RunOpeningScreen(screen));
            ScreenManager.RegisterUIScreen<global::RunOpeningSoulSaviorScreen>(global::ScreenName.SoulSaviorRunOpening, screen => new RunOpeningScreen(screen));
            ScreenManager.RegisterUIScreen<global::GameScreen>(global::ScreenName.Game, screen => new BattleScreen(screen));
            ScreenManager.RegisterUIScreen<global::BattleIntroScreen>(global::ScreenName.BattleIntro, screen => new BattleIntroScreen(screen));
            ScreenManager.RegisterUIScreen<global::DeckScreen>(global::ScreenName.Deck, screen => new DeckScreen(screen));
            ScreenManager.RegisterUIScreen<global::CardDraftScreen>(global::ScreenName.Draft, screen => new DraftScreen(screen));
            ScreenManager.RegisterUIScreen<global::RelicDraftScreen>(global::ScreenName.RelicChoice, screen => new DraftScreen(screen));
            ScreenManager.RegisterUIScreen<global::ElixirDraftScreen>(global::ScreenName.Elixir, screen => new DraftScreen(screen));
            ScreenManager.RegisterUIScreen<global::SoulDraftScreen>(global::ScreenName.SoulChoice, screen => new DraftScreen(screen));
            ScreenManager.RegisterUIScreen<global::EndlessMutatorDraftScreen>(global::ScreenName.EndlessMutatorDraft, screen => new DraftScreen(screen));
            ScreenManager.RegisterUIScreen<global::RewardScreen>(global::ScreenName.Reward, screen => new RewardScreen(screen));
            ScreenManager.RegisterUIScreen<global::GameOverScreen>(global::ScreenName.GameOver, screen => new GameOverScreen(screen));
            ScreenManager.RegisterUIScreen<global::SoulProgressionScreen>(global::ScreenName.SoulProgression, screen => new SoulProgressionScreen(screen));
            ScreenManager.RegisterUIScreen<global::RunHistoryScreen>(global::ScreenName.RunHistory, screen => new RunHistoryScreen(screen));
            ScreenManager.RegisterUIScreen<global::RunSummaryScreen>(global::ScreenName.RunSummary, screen => new RunSummaryScreen(screen));
            ScreenManager.RegisterUIScreen<global::CreditsScreen>(global::ScreenName.Credits, screen => new CreditsScreen(screen));
            ScreenManager.RegisterUIScreen<global::ChampionUpgradeScreen>(global::ScreenName.ChampionUpgrade, screen => new ChampionUpgradeScreen(screen));
            ScreenManager.RegisterUIScreen<global::MerchantScreen>(global::ScreenName.Merchant, screen => new MerchantScreen(screen));
            ScreenManager.RegisterUIScreen<global::StoryEventScreen>(global::ScreenName.StoryEvent, screen => new StoryEventScreen(screen));
            ScreenManager.RegisterUIScreen<global::ShinyShoe.CharacterDialogueScreen>(global::ScreenName.CharacterDialogue, screen => new CharacterDialogueScreen(screen));
            ScreenManager.RegisterUIScreen<global::DragonsHoardScreen>(global::ScreenName.DragonsHoard, screen => new DragonsHoardScreen(screen));
            ScreenManager.RegisterUIScreen<global::UnlockScreen>(global::ScreenName.Unlock, screen => new UnlockScreen(screen));
            ScreenManager.RegisterUIScreen<global::ChallengeOverviewScreen>(global::ScreenName.ChallengeOverview, screen => new ChallengeOverviewScreen(screen));
            ScreenManager.RegisterUIScreen<global::ChallengeDetailsScreen>(global::ScreenName.ChallengeDetails, screen => new ChallengeDetailsScreen(screen));
            ScreenManager.RegisterUIScreen<global::ChallengeProgressScreen>(global::ScreenName.ChallengeProgress, screen => new ChallengeProgressScreen(screen));
            ScreenManager.RegisterUIScreen<global::MapScreen>(global::ScreenName.Map, screen => new MapNavigationScreen(screen));
            ScreenManager.RegisterUIScreen<global::SoulSaviorMapScreen>(global::ScreenName.SoulSaviorMap, screen => new MapNavigationScreen(screen));
            ScreenManager.RegisterUIScreen<global::RegionSelectionScreen>(global::ScreenName.RegionSelection, screen => new RegionSelectionScreen(screen));
            ScreenManager.RegisterUIScreen<global::MinimapScreen>(global::ScreenName.Minimap, screen => new MinimapScreen(screen));
            ScreenManager.RegisterUIScreen<global::CardDetailsScreen>(global::ScreenName.CardDetails, screen => new CardDetailsScreen(screen));
            ScreenManager.RegisterUIScreen<global::CompendiumScreen>(global::ScreenName.Compendium, screen => new CompendiumScreen(screen));
        }
    }
}

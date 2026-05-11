using ShinyShoe;
using ModChallengeDetailsScreen = MonsterTrainAccessibility.UI.Screens.ChallengeDetailsScreen;

namespace MonsterTrainAccessibility.Patches
{
    internal static class ChallengeMutatorInputHooks
    {
        public static bool MutatorSelectionUI_ApplyScreenInput_Prefix(
            global::MutatorSelectionUI __instance,
            CoreInputControlMapping mapping,
            IGameUIComponent triggeredUI,
            global::InputManager.Controls triggeredMappingID)
        {
            return !ModChallengeDetailsScreen.ShouldBlockNativeMutatorClear(
                __instance,
                mapping,
                triggeredUI,
                triggeredMappingID);
        }
    }
}

using System.Collections.Generic;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.Strings;
using MonsterTrainAccessibility.Core.UI;
using MonsterTrainAccessibility.Module.Interop;
using MonsterTrainAccessibility.Module.Nodes;
using MonsterTrainAccessibility.Module.UI;
using Screen = MonsterTrainAccessibility.Core.Screens.Screen;

namespace MonsterTrainAccessibility.Module.Screens
{
    /// <summary>
    /// Soul Savior's region choice (<see cref="global::RegionSelectionScreen"/>, before each region):
    /// opening it speaks the game's "Choose a Region"; then one list, top to bottom: the regions as a row
    /// (<see cref="RegionNodes.Region"/>: landing on one selects it, as the game's keyboard and pad
    /// navigation do, so what follows is its), the final boss (<see cref="RegionNodes.FinalBoss"/>);
    /// the selected region's "Major Rewards" as a row and its "Curse Afflicted" mutator, each by its
    /// tooltip; Continue, which takes the selected region. Focus starts on the selected region. Escape is
    /// the game's: Settings.
    /// </summary>
    public sealed class RegionSelectionScreen : Screen
    {
        public override string Key => "game.soulsavior.regions";

        private static global::RegionSelectionScreen Game
            => GameScreens.Active<global::RegionSelectionScreen>(global::ScreenName.RegionSelection);

        public override bool IsActive() => Game != null;

        public override void OnPush()
        {
            var screen = Game;
            if (screen != null) ScreenIntro.Say(ChallengeProgressScreen.HeaderOf(screen, "Text choose a region"));
        }

        public override void Build(GraphBuilder b)
        {
            var screen = Game;
            if (screen == null) return;
            b.PushContext(null);
            ControlId start = null;
            var regions = new List<RegionSelectionButton>();
            if (screen.regionButtons != null)
                foreach (var region in screen.regionButtons)
                    if (region != null && region.gameObject.activeInHierarchy && region.Button != null) regions.Add(region);
            if (regions.Count > 0)
            {
                b.StartRow(entry: () => System.Math.Max(0, regions.IndexOf(screen.selectedRegionButton)));
                foreach (var region in regions)
                {
                    var shown = region;
                    var id = ControlId.Structural("region:" + region.RegionIndex);
                    b.AddItem(id, RegionNodes.Region(shown, () => screen.SelectRegionButton(shown)));
                    if (region == screen.selectedRegionButton) start = id;
                }
                b.EndRow();
            }
            var final = screen.finalBossButton;
            if (final != null && final.gameObject.activeInHierarchy && final.Button != null)
            {
                var id = ControlId.Structural("region:final");
                b.AddItem(id, RegionNodes.FinalBoss(final, screen.finalBossTooltipProvider, screen.SelectFinalBossButton));
                if (start == null) start = id;
            }
            Rewards(b, screen);
            var mutator = screen.mutatorRewardDisplay;
            if (mutator != null && mutator.gameObject.activeInHierarchy && TooltipReader.Lines(mutator).Count > 0)
            {
                b.PushContext(ChallengeProgressScreen.HeaderOf(screen, "Curse afflicted label"));
                b.AddItem(ControlId.Structural("region:mutator"), RegionNodes.Mutator(mutator));
                b.PopContext();
            }
            var go = screen.continueButton;
            if (go != null && go.gameObject.activeInHierarchy)
                b.AddItem(ControlId.Structural("region:continue"), GameWidgets.Button(go, () => PauseMenuScreen.Caption(go)));
            b.PopContext();
            if (start != null) b.SetStart(start);
        }

        public override IEnumerable<ElementAction> GetActions()
        {
            yield return new ElementAction(ActionIds.Back, GameText.Term("ScreenMainMenu_Settings"),
                _ => GameInput.Send(global::InputManager.Controls.Escape));
        }

        // The selected region's major rewards, as the game lays them out; each the reward its slot shows.
        private static void Rewards(GraphBuilder b, global::RegionSelectionScreen screen)
        {
            if (screen.regionRewardsInfoRoot == null || !screen.regionRewardsInfoRoot.activeInHierarchy || screen.regionRewardsDisplays == null) return;
            var shown = new List<int>();
            for (int i = 0; i < screen.regionRewardsDisplays.Count; i++)
            {
                var display = screen.regionRewardsDisplays[i];
                if (display != null && display.gameObject.activeInHierarchy && TooltipReader.Lines(display).Count > 0) shown.Add(i);
            }
            if (shown.Count == 0) return;
            b.PushContext(ChallengeProgressScreen.HeaderOf(screen, "Major rewards label"), Strings.RoleList);
            b.StartRow();
            foreach (int i in shown)
            {
                int slot = i;
                b.AddItem(ControlId.Structural("region:reward:" + i), RegionNodes.Reward(screen.regionRewardsDisplays[i], () => RewardAt(screen, slot)));
            }
            b.EndRow();
            b.PopContext();
        }

        // The reward the game shows in a slot: the selected region's central rewards, in order.
        private static MapNodeData RewardAt(global::RegionSelectionScreen screen, int slot)
        {
            var selected = screen.selectedRegionButton;
            if (selected == null || screen.saveManager == null) return null;
            var rewards = new List<MapNodeData>();
            RegionRunSetupHelper.GetCentralRewardsForRegion(selected.RegionIndex, screen.saveManager, rewards);
            return slot < rewards.Count ? rewards[slot] : null;
        }
    }
}

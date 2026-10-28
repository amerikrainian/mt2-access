using System.Collections.Generic;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.Strings;
using MonsterTrainAccessibility.Core.UI;
using MonsterTrainAccessibility.Module.Interop;
using MonsterTrainAccessibility.Module.Nodes;
using MonsterTrainAccessibility.Module.UI;
using TMPro;
using UnityEngine;
using Screen = MonsterTrainAccessibility.Core.Screens.Screen;

namespace MonsterTrainAccessibility.Module.Screens
{
    /// <summary>
    /// The Dimensional Portal (<see cref="global::ChallengeProgressScreen"/>), in four Tab-stops:
    /// <list type="bullet">
    /// <item>the challenge sets (Primary Challenges, Echoes) as tabs, landing on one switching to it
    /// through the game's own next/previous buttons;</item>
    /// <item>the set's challenges, each landing selecting it as a click on its node does (the details
    /// follow), with "selected", the game's "Locked" and "completed";</item>
    /// <item>the selected challenge's details: its name, why it is locked, its description, Covenant
    /// rank, clans, pyre heart and mutators (their hover tooltips in the buffer), then Depart or
    /// Resume;</item>
    /// <item>Rewards and Back.</item>
    /// </list>
    /// Escape is the game's: back to the Covenant Outpost. Rewards opens
    /// <see cref="ChallengeRewardsScreen"/>.
    /// </summary>
    public sealed class ChallengeProgressScreen : Screen
    {
        public ChallengeProgressScreen()
        {
            Wrap = true;
        }

        public override string Key => "game.dimensional_portal";
        public override bool KeepStateOnPop => true;
        public override object InitialFocusStop => "challenges";

        internal static global::ChallengeProgressScreen Game
            => GameScreens.Active<global::ChallengeProgressScreen>(global::ScreenName.ChallengeProgress);

        public override bool IsActive()
        {
            var screen = Game;
            return screen != null && !ChallengeRewardsScreen.IsOpen(screen);
        }

        public override void Build(GraphBuilder b)
        {
            var screen = Game;
            if (screen == null) return;
            b.PushContext(HeaderOf(screen, "Text Main Header"), Strings.RoleList, positions: false);

            b.BeginStop("sets");
            b.StartRow();
            foreach (var set in screen.challengeSetOrder)
                b.AddItem(ControlId.Structural("portal:set:" + set), SetTab(screen, set));
            b.EndRow();

            b.BeginStop("challenges");
            var selection = Selection(screen);
            if (selection != null && selection.availableChallenges != null)
            {
                b.PushContext(screen.challengeSetTitleLabel != null ? screen.challengeSetTitleLabel.text : null, Strings.RoleList);
                foreach (var node in selection.availableChallenges)
                {
                    if (node == null || node.Challenge == null || !node.gameObject.activeInHierarchy) continue;
                    var id = ControlId.Structural("portal:challenge:" + node.Challenge.GetID());
                    b.AddItem(id, Challenge(selection, node));
                    if (node == selection.CurrentChallenge) b.SetStart(id);
                }
                b.PopContext();
            }

            b.BeginStop("details");
            Details(b, screen.challengeDetailsAndLaunchUI);

            b.BeginStop("actions");
            var details = screen.challengeDetailsAndLaunchUI;
            Button(b, "rewards", details != null ? details.showRewardProgressButton : null);
            Button(b, "back", screen.backButton);
            b.PopContext();
        }

        public override IEnumerable<ElementAction> GetActions()
        {
            yield return new ElementAction(ActionIds.Back, Strings.Get("bind.ui.back"),
                _ => GameInput.Send(global::InputManager.Controls.Escape));
        }

        private static ChallengeSelectionUI Selection(global::ChallengeProgressScreen screen)
            => screen.currentChallengeSet == global::ChallengeProgressScreen.ChallengeSet.Echoes
                ? screen.challengeSelectionUIEchoes : screen.challengeSelectionUI;

        // A challenge set: named by the game's own title for it; landing on it steps the game's set
        // buttons until it is shown (the sets cycle, so at most one lap).
        private static NodeVtable SetTab(global::ChallengeProgressScreen screen, global::ChallengeProgressScreen.ChallengeSet set)
        {
            System.Func<string> name = () => GameText.Term(set == global::ChallengeProgressScreen.ChallengeSet.Echoes
                ? screen.echoesChallengeSetTitleLocKey : screen.standardChallengeSetTitleLocKey);
            System.Action show = () =>
            {
                for (int i = 0; i < screen.challengeSetOrder.Count && screen.currentChallengeSet != set; i++)
                {
                    if (screen.nextButton == null || !screen.nextButton.gameObject.activeInHierarchy) return;
                    GameInput.Submit(screen.nextButton);
                }
            };
            return new NodeVtable
            {
                ControlType = ControlTypes.Tab,
                Announcements = new List<NodeAnnouncement> { GameNodes.LabelPart(name) },
                SearchText = name,
                OnActivate = show,
                OnFocus = show,
            };
        }

        // A challenge node: its name, then selected, the game's "Locked" (a node that is always open never
        // reads locked), completed. Landing selects it, as a click does: the details follow.
        private static NodeVtable Challenge(ChallengeSelectionUI selection, ChallengeNode node)
        {
            System.Func<string> name = () => GameText.Term(node.Challenge.GetNameKey());
            System.Action select = () =>
            {
                if (selection.CurrentChallenge != node) GameInput.Submit(node.Button);
            };
            return new NodeVtable
            {
                ControlType = ControlTypes.RadioButton,
                Announcements = new List<NodeAnnouncement>
                {
                    GameNodes.LabelPart(name),
                    GameNodes.SelectedPart(() => selection.CurrentChallenge == node),
                    new NodeAnnouncement(() => !node.IsAlwaysUnlocked && node.IsLocked ? GameText.Term("Locked") : null, kind: AnnouncementKinds.Enabled),
                    new NodeAnnouncement(() => node.IsCompleted ? Strings.StateCompleted : null, kind: AnnouncementKinds.Value),
                },
                SearchText = name,
                OnActivate = select,
                OnFocus = select,
            };
        }

        // The selected challenge's details, in the panel's order, only what the game shows.
        private static void Details(GraphBuilder b, ChallengeDetailsAndLaunchUI details)
        {
            if (details == null) return;
            Text(b, "name", details.challengeNameLabel, heading: true);
            if (details.lockedContainer != null && details.lockedContainer.gameObject.activeInHierarchy)
                Text(b, "locked", details.lockedLabel);
            Text(b, "description", details.challengeDescriptionLabel);
            if (Shown(details.covenantUI)) b.AddItem(ControlId.Structural("portal:covenant"), StartingConditionsNodes.Covenant(details.covenantUI));
            if (Shown(details.mainClanUI)) b.AddItem(ControlId.Structural("portal:mainclan"), StartingConditionsNodes.Clan(details.mainClanUI));
            if (Shown(details.alliedClanUI)) b.AddItem(ControlId.Structural("portal:alliedclan"), StartingConditionsNodes.Clan(details.alliedClanUI));
            if (Shown(details.pyreHeartUI)) b.AddItem(ControlId.Structural("portal:pyre"), StartingConditionsNodes.PyreHeart(details.pyreHeartUI));
            if (Shown(details.mutatorsContainer))
            {
                b.PushContext(SectionTitle(details.mutatorsContainer), Strings.RoleList);
                int i = 0;
                foreach (var mutator in details.mutatorsContainer.GetComponentsInChildren<MutatorButtonUI>(false))
                    b.AddItem(ControlId.Structural("portal:mutator:" + i++), StartingConditionsNodes.Mutator(mutator));
                b.PopContext();
            }
            Button(b, "depart", details.startButton);
            Button(b, "resume", details.resumeButton);
        }

        private static void Text(GraphBuilder b, string key, TMP_Text text, bool heading = false)
        {
            if (text == null || !text.gameObject.activeInHierarchy || string.IsNullOrWhiteSpace(text.text)) return;
            b.AddItem(ControlId.Structural("portal:" + key), heading ? GameNodes.Heading(() => text.text) : GameNodes.Text(() => text.text));
        }

        private static void Button(GraphBuilder b, string key, ShinyShoe.GameUISelectableButton button)
        {
            if (button == null || !button.gameObject.activeInHierarchy) return;
            b.AddItem(ControlId.Structural("portal:" + key), GameWidgets.Button(button, () => PauseMenuScreen.Caption(button)));
        }

        private static bool Shown(Component widget) => widget != null && widget.gameObject.activeInHierarchy;

        // A text the game heads a screen with, found by its object name (no field holds it).
        internal static string HeaderOf(Component root, string name)
        {
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(false))
                if (text.gameObject.name == name && !string.IsNullOrWhiteSpace(text.text)) return text.text;
            return null;
        }

        // A list's title: the text beside its container ("Mutators"), not inside it.
        private static string SectionTitle(Component container)
        {
            var parent = container.transform.parent;
            if (parent == null) return null;
            foreach (Transform sibling in parent)
            {
                if (sibling == container.transform) continue;
                var text = sibling.GetComponent<TMP_Text>();
                if (text != null && sibling.gameObject.activeInHierarchy && !string.IsNullOrWhiteSpace(text.text)) return text.text;
            }
            return null;
        }
    }

    /// <summary>
    /// A Dimensional Challenge's rewards (<see cref="ChallengeRewardsUI"/>): each reward the challenge
    /// unlocks, with the game's "Locked" and its unlock condition and progress while still locked,
    /// then the card mastery reward the same way, then Close. Escape closes it.
    /// </summary>
    public sealed class ChallengeRewardsScreen : Screen
    {
        public override string Key => "game.dimensional_portal.rewards";
        public override int Layer => 5;
        public override bool Exclusive => true;

        internal static bool IsOpen(global::ChallengeProgressScreen screen)
        {
            var rewards = screen.challengeDetailsAndLaunchUI != null ? screen.challengeDetailsAndLaunchUI.challengeRewardsUI : null;
            return rewards != null && rewards.gameObject.activeInHierarchy;
        }

        public override bool IsActive()
        {
            var screen = ChallengeProgressScreen.Game;
            return screen != null && IsOpen(screen);
        }

        public override void Build(GraphBuilder b)
        {
            var screen = ChallengeProgressScreen.Game;
            if (screen == null || !IsOpen(screen)) return;
            var rewards = screen.challengeDetailsAndLaunchUI.challengeRewardsUI;
            b.PushContext(ChallengeProgressScreen.HeaderOf(rewards, "Text Reward Progress"), Strings.RoleList);
            int i = 0;
            foreach (var item in rewards.challengeRewardItems)
            {
                if (item == null || !item.gameObject.activeInHierarchy) continue;
                var reward = item;
                b.AddItem(ControlId.Structural("rewards:" + i++), Reward(() => Text(reward.textLabel),
                    () => reward.lockedRoot != null && reward.lockedRoot.activeSelf, reward.progressionObjectiveUI));
            }
            var mastery = rewards.masteryRewardItemUI;
            if (mastery != null && mastery.gameObject.activeInHierarchy)
                b.AddItem(ControlId.Structural("rewards:mastery"), Reward(() => Text(mastery.textLabel),
                    () => mastery.lockedRoot != null && mastery.lockedRoot.activeSelf, mastery.progressionObjectiveUI));
            var close = rewards.closeButton;
            if (close != null && close.gameObject.activeInHierarchy && PauseMenuScreen.Caption(close) != null)
                b.AddItem(ControlId.Structural("rewards:close"), GameWidgets.Button(close, () => PauseMenuScreen.Caption(close)));
            b.PopContext();
        }

        public override IEnumerable<ElementAction> GetActions()
        {
            yield return new ElementAction(ActionIds.Back, Strings.Get("bind.ui.back"),
                _ => GameInput.Send(global::InputManager.Controls.Escape));
        }

        private static NodeVtable Reward(System.Func<string> name, System.Func<bool> locked, ProgressionObjectiveUI unlock)
        {
            return new NodeVtable
            {
                ControlType = ControlTypes.Text,
                Announcements = new List<NodeAnnouncement>
                {
                    GameNodes.LabelPart(name),
                    new NodeAnnouncement(() => locked() ? GameText.Term("Locked") : null, kind: AnnouncementKinds.Enabled),
                    GameNodes.TooltipPart(() => locked() ? ProgressionReader.Line(unlock) : null),
                },
                SearchText = name,
            };
        }

        private static string Text(TMP_Text text)
            => text != null && !string.IsNullOrWhiteSpace(text.text) ? text.text : null;
    }
}

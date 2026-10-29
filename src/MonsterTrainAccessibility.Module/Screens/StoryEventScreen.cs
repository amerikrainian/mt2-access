using System.Collections.Generic;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.Input;
using MonsterTrainAccessibility.Core.Strings;
using MonsterTrainAccessibility.Core.UI;
using MonsterTrainAccessibility.Module.Interop;
using MonsterTrainAccessibility.Module.Nodes;
using MonsterTrainAccessibility.Module.UI;
using ShinyShoe;
using TMPro;
using Screen = MonsterTrainAccessibility.Core.Screens.Screen;

namespace MonsterTrainAccessibility.Module.Screens
{
    /// <summary>
    /// A story event (<see cref="global::StoryEventScreen"/>: a map stop's encounter). Each page of the
    /// story is said as it appears (only what is new when the story adds to the page), and stays the
    /// list's first line to read again. Then the choices the game offers, once it shows them, each a
    /// button by its words and the game's note on it ("Wrath, button, (Get Wrath ability.)"), disabled
    /// where the game's conditions fail; landing on one shows it as the pointer does, so the game fills
    /// its reward preview, which the buffer reads (the artifact or card it gives, with their tooltips).
    /// Focus lands on the first choice. At the story's end, Continue; the event's design credit where the
    /// game shows one. Escape is the game's (it hurries the text along).
    /// </summary>
    public sealed class StoryEventScreen : Screen
    {
        private static readonly InputCategory[] Categories = { InputCategory.Game, InputCategory.UI };

        public override string Key => "game.story_event";
        public override int Layer => 5;
        public override IReadOnlyList<InputCategory> InputCategories => Categories;

        private string _spoken;

        private static global::StoryEventScreen Game
        {
            get
            {
                var screen = GameScreens.Active<global::StoryEventScreen>(global::ScreenName.StoryEvent);
                return screen != null && screen.IsInteractable() ? screen : null;
            }
        }

        public override bool IsActive() => Game != null;

        public override void OnPush() => _spoken = null;

        public override void Build(GraphBuilder b)
        {
            var screen = Game;
            if (screen == null) return;
            string text = Shown(screen.contentLabel);
            SpeakNew(text);
            b.PushContext(null);
            if (text != null) b.AddItem(ControlId.Structural("story:text"), GameNodes.Text(() => Shown(screen.contentLabel)));
            ControlId start = null;
            if (screen.buttonsVisible && screen.currentChoiceItems != null)
                foreach (var item in screen.currentChoiceItems)
                {
                    if (item == null || item.button == null || !item.gameObject.activeInHierarchy) continue;
                    var id = ControlId.Structural("story:choice:" + item.choiceIndex + ":" + item.choiceText);
                    b.AddItem(id, Choice(screen, item));
                    start = start ?? id;
                }
            var go = screen.continueButton;
            if (go != null && go.gameObject.activeInHierarchy)
            {
                var id = ControlId.Structural("story:continue");
                b.AddItem(id, GameWidgets.Button(go, () => PauseMenuScreen.Caption(go)));
                start = start ?? id;
            }
            if (screen.designContributor != null && screen.designContributor.activeInHierarchy && Shown(screen.designContributorName) != null)
                b.AddItem(ControlId.Structural("story:credit"), GameNodes.Text(() => Credit(screen)));
            b.PopContext();
            if (start != null) b.SetStart(start);
        }

        public override IEnumerable<ElementAction> GetActions()
        {
            yield return new ElementAction(ActionIds.Back, Strings.Get("bind.ui.back"),
                _ => GameInput.Send(global::InputManager.Controls.Escape));
        }

        // The page as it appears: all of it the first time, only what follows when the story adds to it.
        private void SpeakNew(string text)
        {
            if (string.IsNullOrWhiteSpace(text) || text == _spoken) return;
            string said = _spoken;
            _spoken = text;
            Speech.Say(said != null && text.StartsWith(said, System.StringComparison.Ordinal) ? text.Substring(said.Length) : text);
        }

        // "Wrath, button, (Get Wrath ability.)": the choice and the game's note on it; disabled where
        // the game's conditions fail; its reward preview in the buffer.
        private static NodeVtable Choice(global::StoryEventScreen screen, StoryChoiceItem item)
        {
            System.Func<string> label = () => Shown(item.label) ?? item.choiceText;
            return new NodeVtable
            {
                ControlType = ControlTypes.Button,
                Announcements = new List<NodeAnnouncement>
                {
                    GameNodes.LabelPart(label),
                    GameNodes.DisabledPart(() => item.button.interactable),
                    new NodeAnnouncement(() => Shown(item.optionalRewardLabel), kind: AnnouncementKinds.Value),
                },
                SearchText = label,
                OnFocus = () => GameInput.Hover(item.button),
                OnActivate = () => GameInput.Submit(item.button),
                Details = () => Preview(screen),
            };
        }

        // The game's reward preview, as it shows it for the choice hovered.
        private static IEnumerable<string> Preview(global::StoryEventScreen screen)
        {
            var preview = screen.rewardPreview;
            var lines = new List<string>();
            if (preview == null || !preview.gameObject.activeInHierarchy || preview.rewardItems == null) return lines;
            foreach (var reward in preview.rewardItems)
                if (reward != null && reward.gameObject.activeInHierarchy) lines.AddRange(RewardNodes.Lines(reward));
            return lines;
        }

        // The design credit as the game words it (its heading and the name).
        private static string Credit(global::StoryEventScreen screen)
        {
            var parts = new List<string>();
            foreach (var text in screen.designContributor.GetComponentsInChildren<TMP_Text>(false))
                if (Shown(text) != null) parts.Add(Shown(text));
            return string.Join(" ", parts);
        }

        private static string Shown(TMP_Text text)
        {
            string content = text != null && text.gameObject.activeInHierarchy ? text.Content() : null;
            return string.IsNullOrWhiteSpace(content) ? null : content.Trim();
        }
    }
}

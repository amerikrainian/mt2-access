using System.Collections.Generic;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.Input;
using MonsterTrainAccessibility.Core.Strings;
using MonsterTrainAccessibility.Core.UI;
using MonsterTrainAccessibility.Module.Interop;
using MonsterTrainAccessibility.Module.UI;
using TMPro;
using Screen = MonsterTrainAccessibility.Core.Screens.Screen;

namespace MonsterTrainAccessibility.Module.Screens
{
    /// <summary>
    /// A conversation (<see cref="ShinyShoe.CharacterDialogueScreen"/>: the outpost's characters, the
    /// story's scenes), phrase by phrase. Each phrase is said whole the moment it starts, its speaker
    /// first ("Herzal: ..."), without waiting for the game to type it out; it stays the screen's one line
    /// to read again. Enter moves on at once: a phrase still typing is finished and left in the same press
    /// (the game's own advance, twice). Escape is the game's: it offers to skip the conversation.
    /// </summary>
    public sealed class CharacterDialogueScreen : Screen
    {
        private static readonly InputCategory[] Categories = { InputCategory.UI };

        public override string Key => "game.character_dialogue";
        public override int Layer => 30;
        public override bool Exclusive => true;
        public override bool AllowsTypeahead => false;
        public override IReadOnlyList<InputCategory> InputCategories => Categories;

        private string _said;

        private static ShinyShoe.CharacterDialogueScreen Game
            => GameScreens.Active<ShinyShoe.CharacterDialogueScreen>(global::ScreenName.CharacterDialogue);

        public override bool IsActive() => Game != null;

        public override void OnPush() => _said = null;

        public override void Build(GraphBuilder b)
        {
            var screen = Game;
            if (screen == null) return;
            string phrase = Phrase(screen);
            // The first phrase is read by focus landing on it; each after it replaces it in place, said
            // as it starts unless Basic events are turned off. Every phrase goes to the events log.
            if (phrase != null && phrase != _said)
            {
                bool say = Core.Events.GameEvents.Report(Core.Events.EventKinds.Basic, phrase);
                if (_said != null && say) Speech.Say(phrase, interrupt: true);
                _said = phrase;
            }
            if (phrase == null) return;
            b.PushContext(null);
            b.AddItem(ControlId.Structural("dialogue:phrase"), new NodeVtable
            {
                ControlType = ControlTypes.Text,
                Announcements = new List<NodeAnnouncement> { GameNodes.LabelPart(() => Phrase(screen)) },
                OnActivate = () => Advance(screen),
            });
            b.PopContext();
        }

        public override IEnumerable<ElementAction> GetActions()
        {
            yield return new ElementAction(ActionIds.Back, Strings.Get("bind.ui.back"),
                _ => GameInput.Send(global::InputManager.Controls.Escape));
        }

        // "Herzal: ...": the speaker the game names and the phrase, the whole of it (the typewriter only
        // reveals what is already set), once the phrase is showing.
        private static string Phrase(ShinyShoe.CharacterDialogueScreen screen)
        {
            if (screen.dialogueContainer == null || screen.dialogueContainer.alpha <= 0f) return null;
            string text = Shown(screen.dialogueText);
            if (text == null) return null;
            string speaker = screen.speakerLabelContainer != null && screen.speakerLabelContainer.activeInHierarchy ? Shown(screen.speakerLabel) : null;
            return speaker != null ? string.Format(GameText.Term("TextFormat_Colon") ?? "{0}: {1}", speaker, text) : text;
        }

        // Finish the phrase if it is still typing, then move on: the game's advance, as often as it takes.
        private static void Advance(ShinyShoe.CharacterDialogueScreen screen)
        {
            if (screen.isPhraseAnimating) GameInput.Send(global::InputManager.Controls.AdvanceDialogue);
            GameInput.Send(global::InputManager.Controls.AdvanceDialogue);
        }

        private static string Shown(TMP_Text text)
        {
            string content = text != null && text.gameObject.activeInHierarchy ? text.Content() : null;
            return string.IsNullOrWhiteSpace(content) ? null : content.Trim();
        }
    }
}

using System.Collections.Generic;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.Input;
using MonsterTrainAccessibility.Core.Strings;
using MonsterTrainAccessibility.Core.UI;
using MonsterTrainAccessibility.Module.Interop;
using MonsterTrainAccessibility.Module.Nodes;
using MonsterTrainAccessibility.Module.UI;
using ShinyShoe;
using UnityEngine;
using Screen = MonsterTrainAccessibility.Core.Screens.Screen;

namespace MonsterTrainAccessibility.Module.Screens
{
    /// <summary>
    /// A draft (<see cref="DraftScreenBase"/>: the game's pick-one screens), one screen per game screen
    /// it is registered for (the artifact draft so far). Opening it says the draft's header and the
    /// game's instructions ("Herzal's Hoard. Choose an artifact."); then the items side by side as the
    /// game lays them out, each read by its kind (an artifact by <see cref="RelicNodes"/>, a card by
    /// <see cref="CardNodes"/>, a soul by <see cref="SoulNodes.Offer"/>, an endless pair of mutators by
    /// its title and each half's description, the cards and artifacts it gives, the game's preview of it
    /// on hovering, in its buffer: what a curse does), then Skip with what skipping gives. Nothing is declared while the game
    /// deals the items in, and an item the game has taken out of play (all of them, once one is
    /// chosen) is not declared. Landing on an item shows it as hovered; Enter takes it through the
    /// game. Escape is the game's: the Menu.
    /// </summary>
    public sealed class DraftScreen : Screen
    {
        private static readonly InputCategory[] Categories = { InputCategory.Game, InputCategory.UI };

        private readonly global::ScreenName _screen;
        private readonly string _key;

        public DraftScreen(global::ScreenName screen, string key)
        {
            _screen = screen;
            _key = key;
        }

        public override string Key => _key;
        public override int Layer => 5;
        public override IReadOnlyList<InputCategory> InputCategories => Categories;

        private DraftScreenBase Game
        {
            get
            {
                var draft = GameScreens.Active<DraftScreenBase>(_screen);
                return draft != null && draft.IsInteractable() ? draft : null;
            }
        }

        // An elixir being mixed can be put aside to look at the train (its Hide button): the draft is
        // then out of the way, the battle in front with the game's Show button.
        public override bool IsActive()
        {
            var draft = Game;
            return draft != null && !(draft is ElixirDraftScreen elixir && !ElixirShown(elixir));
        }

        internal static bool ElixirShown(ElixirDraftScreen elixir)
            => elixir.screenContentTransform != null && elixir.screenContentTransform.gameObject.activeSelf;

        public override void OnPush()
        {
            var draft = Game;
            if (draft != null) ScreenIntro.Say(Shown(draft.headerLabel), ChallengeProgressScreen.HeaderOf(draft, "Instructions label"));
        }

        public override void Build(GraphBuilder b)
        {
            var draft = Game;
            if (draft == null || draft.animatingIn) return;
            b.PushContext(null);
            var items = new List<NodeVtable>();
            foreach (var item in draft.draftItems)
            {
                var vt = Item(item, draft);
                if (vt != null) items.Add(vt);
            }
            if (items.Count > 0)
            {
                b.StartRow();
                for (int i = 0; i < items.Count; i++) b.AddItem(ControlId.Structural(_key + ":item:" + i), items[i]);
                b.EndRow();
            }
            var skip = draft.skipButton;
            if (skip != null && skip.gameObject.activeInHierarchy && skip.interactable && PauseMenuScreen.Caption(skip) != null)
                b.AddItem(ControlId.Structural(_key + ":skip"), GameWidgets.Button(skip, () => PauseMenuScreen.Caption(skip)));
            if (draft is ElixirDraftScreen elixir && elixir.hideButton != null && elixir.hideButton.gameObject.activeInHierarchy)
                b.AddItem(ControlId.Structural(_key + ":hide"), GameWidgets.Button(elixir.hideButton, () => PauseMenuScreen.Caption(elixir.hideButton) ?? TooltipReader.FirstTitle(elixir.hideButton)));
            b.PopContext();
        }

        public override IEnumerable<ElementAction> GetActions()
        {
            yield return new ElementAction(ActionIds.Back, Strings.Get("bind.ui.back"),
                _ => GameInput.Send(global::InputManager.Controls.Escape));
        }

        // An item by its kind, while it is in play; null for one out of play or of a kind not read yet.
        private static NodeVtable Item(IDraftableUI item, DraftScreenBase draft)
        {
            var widget = item as Component;
            if (widget == null || !widget.gameObject.activeInHierarchy) return null;
            if (item is RelicInfoUI relic)
            {
                var button = relic.GetComponent<GameUISelectableButton>();
                if (button == null || !button.interactable) return null;
                return RelicNodes.Offer(relic, () => GameInput.Hover(button), () => GameInput.Submit(button));
            }
            if (item is SoulInfoUI soul)
            {
                var button = soul.SelectableUI;
                if (soul.GetSoulData() == null || button == null) return null;
                return SoulNodes.Offer(soul, () => GameInput.Hover(button), () => GameInput.Submit(button));
            }
            if (item is EndlessMutatorPairUI pair)
            {
                var button = pair.SelectableUI;
                if (pair.MutatorPair == null || button == null) return null;
                var vt = GameNodes.Button(() => Shown(pair.titleText), () => GameInput.Submit(button));
                vt.Announcements = new List<NodeAnnouncement>(vt.Announcements)
                {
                    GameNodes.TooltipPart(() => Shown(pair.GetEndlessMutatorUIPositive().descriptionLabel)),
                    GameNodes.TooltipPart(() => Shown(pair.GetEndlessMutatorUINegative().descriptionLabel)),
                };
                vt.OnFocus = () => GameInput.Hover(button);
                if (draft is EndlessMutatorDraftScreen endless) vt.Details = () => RewardNodes.PreviewLines(endless.rewardPreview);
                return vt;
            }
            if (item is CardUI card)
            {
                if (card.GetCardState() == null || !card.IsCardInteractable()) return null;
                var vt = CardNodes.Card(card, CardTooltipContainer.LoreTooltipSetting.Preferences, () => GameInput.Hover(card.SelectableUI));
                vt.OnActivate = () => GameInput.Submit(card.SelectableUI);
                return vt;
            }
            return null;
        }

        private static string Shown(TMPro.TMP_Text text)
        {
            string content = text != null && text.gameObject.activeInHierarchy ? text.Content() : null;
            return string.IsNullOrWhiteSpace(content) ? null : content;
        }
    }
}

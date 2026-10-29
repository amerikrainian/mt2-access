using System.Collections.Generic;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.Input;
using MonsterTrainAccessibility.Core.Strings;
using MonsterTrainAccessibility.Core.UI;
using MonsterTrainAccessibility.Module.Interop;
using MonsterTrainAccessibility.Module.Nodes;
using MonsterTrainAccessibility.Module.UI;
using ShinyShoe;
using TMPro;
using UnityEngine;
using Screen = MonsterTrainAccessibility.Core.Screens.Screen;

namespace MonsterTrainAccessibility.Module.Screens
{
    /// <summary>
    /// The community challenges (<see cref="global::ChallengeOverviewScreen"/>). Opening it says its title
    /// and the game's line under it; then the two tabs (Featured, Recently Played; landing on one shows
    /// it), the challenges the shown tab lists (<see cref="Challenge"/>: its name, clans, Pyre
    /// Heart, covenant, mutators, creator, entries, the player's rank), or the game's note that there are
    /// none; then Join Challenge, Create Challenge, Back. Enter on a challenge opens it through the game.
    /// Join Challenge opens the game's share code entry, which replaces the list: the code (edited with
    /// the game's own editor, a Tab-stop of its own), then the game's submit and cancel.
    /// </summary>
    public sealed class ChallengeOverviewScreen : Screen
    {
        private static readonly InputCategory[] Categories = { InputCategory.UI };

        public override string Key => "game.challenge_overview";
        public override int Layer => 5;
        public override IReadOnlyList<InputCategory> InputCategories => Categories;

        private readonly SearchField _code = new SearchField("challenges:code");

        private static global::ChallengeOverviewScreen Game
        {
            get
            {
                var screen = GameScreens.Active<global::ChallengeOverviewScreen>(global::ScreenName.ChallengeOverview);
                return screen != null && screen.IsInteractable() ? screen : null;
            }
        }

        public override bool IsActive() => Game != null;

        public override void OnPush()
        {
            var screen = Game;
            if (screen != null) ScreenIntro.Say(ChallengeProgressScreen.HeaderOf(screen, "Title label"), ChallengeProgressScreen.HeaderOf(screen, "Text call to action"));
        }

        public override void Build(GraphBuilder b)
        {
            var screen = Game;
            if (screen == null) return;
            var dialog = screen.enterSharecodeDialog;
            if (dialog != null && dialog.gameObject.activeSelf)
            {
                ShareCode(b, dialog);
                return;
            }
            b.PushContext(null);
            // Coming up from the list lands on the tab shown (the one the game leaves unpressable).
            b.StartRow(entry: () => Visible(screen.recentlyViewedButton) && !screen.recentlyViewedButton.interactable && Visible(screen.featuredChallengesButton) ? 1 : 0);
            Tab(b, "featured", screen.featuredChallengesButton);
            Tab(b, "recent", screen.recentlyViewedButton);
            b.EndRow();
            var section = screen.featuredChallengesSection != null && screen.featuredChallengesSection.gameObject.activeSelf
                ? screen.featuredChallengesSection : screen.recentChallengesSection;
            Section(b, section);
            Button(b, "join", screen.joinChallengeButton);
            Button(b, "create", screen.createChallengeButton);
            Button(b, "back", screen.backButton);
            b.PopContext();
        }

        public override void OnUpdate()
        {
            var dialog = Game?.enterSharecodeDialog;
            _code.Update(dialog != null && dialog.gameObject.activeSelf ? Field(dialog) : null);
        }

        public override void OnUnfocus() => _code.Release();

        public override void OnPop() => OnUnfocus();

        public override IEnumerable<ElementAction> GetActions()
        {
            yield return new ElementAction(ActionIds.Back, Strings.Get("bind.ui.back"),
                _ => GameInput.Send(global::InputManager.Controls.Escape));
        }

        // A tab: landing on it shows it, as with every tab (the game leaves the shown one unpressable).
        private static void Tab(GraphBuilder b, string key, GameUISelectableButton tab)
        {
            if (tab == null || !tab.gameObject.activeInHierarchy) return;
            System.Func<string> label = () => PauseMenuScreen.Caption(tab);
            System.Action open = () => { if (tab.interactable) GameInput.Submit(tab); };
            b.AddItem(ControlId.Structural("challenges:tab:" + key), new NodeVtable
            {
                ControlType = ControlTypes.Tab,
                Announcements = new List<NodeAnnouncement> { GameNodes.LabelPart(label) },
                SearchText = label,
                OnActivate = open,
                OnFocus = open,
            });
        }

        private static void Section(GraphBuilder b, ChallengesSection section)
        {
            if (section == null) return;
            if (section.spinner != null && section.spinner.gameObject.activeInHierarchy) return;
            int i = 0;
            if (section.challengeEntries != null)
                foreach (var entry in section.challengeEntries)
                {
                    if (entry == null || !entry.gameObject.activeInHierarchy || entry.ChallengeData == null) continue;
                    b.AddItem(ControlId.Structural("challenges:entry:" + i + ":" + Shown(entry.sharecodeLabel)), Challenge(entry));
                    i++;
                }
            if (i == 0 && section.noChallengeLabel != null && section.noChallengeLabel.gameObject.activeInHierarchy)
                b.AddItem(ControlId.Structural("challenges:none"), GameNodes.Text(() => Shown(section.noChallengeLabel)));
        }

        /// <summary>"DishesHeatingAreas, button, Stygian Guard, Pyreborne, Pyre of Dominion, Covenant Rank 2,
        /// Cheap Trick, by NomNom, 44 entries": the challenge by its name, then what it sets (the icons'
        /// own words), who made it and how many have played it; the player's rank or run under way. The
        /// icons' tooltips (what the Pyre Heart, the covenant, the mutators do) in the buffer.</summary>
        private static NodeVtable Challenge(ChallengeEntryUI entry)
        {
            System.Func<string> name = () => Shown(entry.sharecodeLabel);
            return new NodeVtable
            {
                ControlType = ControlTypes.Button,
                Announcements = new List<NodeAnnouncement>
                {
                    GameNodes.LabelPart(name),
                    new NodeAnnouncement(() => Settings(entry), kind: AnnouncementKinds.Value),
                },
                SearchText = name,
                OnActivate = () => GameInput.Submit(entry.Button),
                Details = () =>
                {
                    var lines = new List<string>();
                    foreach (var provider in Providers(entry)) lines.AddRange(TooltipReader.Lines(provider));
                    return GameNodes.Lines(lines);
                },
            };
        }

        private static string Settings(ChallengeEntryUI entry)
        {
            var parts = new List<string>();
            var clans = Visible(entry.classesUI) ? entry.classesUI.GetComponent<ITooltipProvider>() : null;
            if (clans?.Tooltips != null)
                foreach (var t in clans.Tooltips) Add(parts, t.body);
            if (Visible(entry.pyreHeartInfoUI)) Add(parts, TooltipReader.FirstTitle(entry.pyreHeartInfoUI.GetComponentInChildren<TooltipProviderComponent>(false)));
            if (Visible(entry.covenantLevelUI)) Add(parts, TooltipReader.FirstTitle(entry.covenantLevelUI));
            foreach (var mutator in entry.mutatorIcons)
                if (Visible(mutator)) Add(parts, TooltipReader.FirstTitle(mutator));
            string creator = Shown(entry.creatorLabel);
            if (creator != null) parts.Add(Strings.F("challenge.creator", creator));
            string entries = Shown(entry.entriesLabel);
            if (entries != null) parts.Add(Strings.F("challenge.entries", entries));
            Add(parts, Shown(entry.rankLabel));
            if (Visible(entry.inProgressRunFlag)) parts.Add(Strings.Get("challenge.in_progress"));
            return string.Join(", ", parts);
        }

        // The icons that explain themselves on hover, in the row's order.
        private static IEnumerable<Component> Providers(ChallengeEntryUI entry)
        {
            if (Visible(entry.classesUI)) yield return entry.classesUI;
            if (Visible(entry.pyreHeartInfoUI)) yield return entry.pyreHeartInfoUI.GetComponentInChildren<TooltipProviderComponent>(false);
            if (Visible(entry.covenantLevelUI)) yield return entry.covenantLevelUI;
            foreach (var mutator in entry.mutatorIcons)
                if (Visible(mutator)) yield return mutator;
        }

        // ---- the share code entry ----

        private void ShareCode(GraphBuilder b, EnterSharecodeDialog dialog)
        {
            var field = Field(dialog);
            if (field != null && field.gameObject.activeInHierarchy)
            {
                b.BeginStop(_code.Key);
                b.PushContext(null);
                b.AddItem(_code.Id, _code.Node(field, () => FieldLabel(dialog, field)));
                b.PopContext();
            }
            b.BeginStop("challenges:code:buttons");
            b.PushContext(null);
            var submit = dialog.submitSharecodeButton != null ? dialog.submitSharecodeButton.Button : null;
            Button(b, "code:submit", submit);
            var cancel = dialog.dialog != null ? dialog.dialog.GetComponentsInChildren<GameUISelectableButton>(false) : null;
            if (cancel != null)
                foreach (var button in cancel)
                    if (button != submit && button != (dialog.sharecodeEntryField != null ? dialog.sharecodeEntryField.button : null))
                        Button(b, "code:" + button.name, button);
            b.PopContext();
        }

        private static TMP_InputField Field(EnterSharecodeDialog dialog)
            => dialog.sharecodeEntryField != null ? dialog.sharecodeEntryField.inputField : null;

        // The field's prompt as drawn, else the title the game gives its on-screen keyboard.
        private static string FieldLabel(EnterSharecodeDialog dialog, TMP_InputField field)
        {
            var placeholder = field.placeholder as TMP_Text;
            string text = placeholder != null ? placeholder.Content() : null;
            if (!string.IsNullOrWhiteSpace(text)) return text.Trim();
            var container = dialog.sharecodeEntryField;
            return container != null ? GameText.Term(container.fieldTitleLocalizationKey) : null;
        }

        private static void Button(GraphBuilder b, string key, GameUISelectableButton button)
        {
            if (button == null || !button.gameObject.activeInHierarchy || PauseMenuScreen.Caption(button) == null) return;
            b.AddItem(ControlId.Structural("challenges:" + key), GameWidgets.Button(button, () => PauseMenuScreen.Caption(button)));
        }

        private static void Add(List<string> parts, string text)
        {
            if (!string.IsNullOrWhiteSpace(text)) parts.Add(text.Trim());
        }

        private static bool Visible(Component widget) => widget != null && widget.gameObject.activeInHierarchy;

        private static string Shown(TMP_Text text)
        {
            string content = text != null && text.gameObject.activeInHierarchy ? text.Content() : null;
            return string.IsNullOrWhiteSpace(content) ? null : content.Trim();
        }
    }
}

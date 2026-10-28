using System.Collections.Generic;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.Input;
using MonsterTrainAccessibility.Core.Screens;
using MonsterTrainAccessibility.Core.UI;
using Xunit;

namespace MonsterTrainAccessibility.Tests
{
    // A choice submenu opened as a CHILD screen of a settings-style parent, driven through ScreenManager
    // (the child push/remove and the focus hand-back happen in its tick), with speech captured through
    // the Core seam. What matters is what is spoken, and how many times.
    public class ChoiceSubmenuTests
    {
        // One combo box (Enter opens its option list) and one toggle after it.
        private sealed class ComboScreen : Screen
        {
            public readonly string[] Options = { "Windowed", "Windowed Borderless", "Fullscreen" };
            public int Value = 1;
            public bool SpeakTitle;
            public bool HideCombo; // the picked option removed the combo (focus must land elsewhere)

            public override string Key => "test.combo";
            public override bool IsActive() => true;

            public override void Build(GraphBuilder b)
            {
                b.PushContext("Settings", "list");
                if (!HideCombo)
                    b.AddItem(ControlId.Structural("displaymode"), new NodeVtable
                    {
                        ControlType = ControlTypes.ComboBox,
                        Announcements = new[]
                        {
                            new NodeAnnouncement(() => "Display Mode", kind: AnnouncementKinds.Label),
                            new NodeAnnouncement(() => Options[Value], live: true, kind: AnnouncementKinds.Value),
                        },
                        OnActivate = Open,
                    });
                b.AddItem(ControlId.Structural("vsync"), new NodeVtable
                {
                    ControlType = ControlTypes.Toggle,
                    Announcements = new[] { new NodeAnnouncement(() => "VSync", kind: AnnouncementKinds.Label) },
                });
                b.PopContext();
            }

            private void Open()
            {
                var choices = new List<ChoiceOption>();
                for (int i = 0; i < Options.Length; i++)
                {
                    int index = i;
                    choices.Add(new ChoiceOption(Options[i], () => Value = index));
                }
                ChoiceSubmenuScreen.Open("Display Mode", choices, Value, speakTitle: SpeakTitle);
            }
        }

        private static InputAction Action(string key) => new InputAction(key, key) { Category = InputCategory.UI };

        // One game frame past the idle-rebuild throttle: the screen stack re-syncs and focus settles.
        private static void Tick(FakeNavInput input)
        {
            input.FrameCount += 10;
            ScreenManager.Tick();
        }

        private static void Run(ComboScreen screen, System.Action<List<string>, FakeNavInput> body)
        {
            var spoken = new List<string>();
            var input = new FakeNavInput { FrameCount = 1 };
            var previous = Navigation.Active;
            Speech.Speak = (t, i) => spoken.Add(t);
            NavInput.Current = input;
            GraphAnnouncer.PositionText = (i, n) => i + " of " + n;
            Navigation.Active = new GraphNavigator();
            try
            {
                ScreenManager.Register(screen);
                Tick(input);
                Assert.Equal(new[] { "Settings, list, Display Mode, combo box, Windowed Borderless, 1 of 2" }, spoken.ToArray());
                spoken.Clear();
                body(spoken, input);
            }
            finally
            {
                ScreenManager.Shutdown();
                Navigation.Active = previous;
                Speech.Speak = (t, i) => { };
                GraphAnnouncer.PositionText = null;
            }
        }

        [Fact]
        public void ComboBoxListSpeaksOnlyTheLandedOptionThenTheControlWithItsNewValue()
        {
            var screen = new ComboScreen { SpeakTitle = false };
            Run(screen, (spoken, input) =>
            {
                // Enter: the list opens on the current option; the label was just heard, so no title.
                Assert.True(Navigation.DispatchJustPressed(Action(UiActions.Activate)));
                Tick(input);
                Assert.Equal(new[] { "Windowed Borderless, button, selected, 2 of 3" }, spoken.ToArray());
                spoken.Clear();

                Assert.True(Navigation.DispatchJustPressed(Action(UiActions.Down)));
                Assert.Equal(new[] { "Fullscreen, button, 3 of 3" }, spoken.ToArray());
                spoken.Clear();

                // Enter picks: the list closes and the combo box re-reads itself with the new value,
                // without the screen path it never left.
                Assert.True(Navigation.DispatchJustPressed(Action(UiActions.Activate)));
                Tick(input);
                Assert.Equal(2, screen.Value);
                Assert.Equal(new[] { "Display Mode, combo box, Fullscreen, 1 of 2" }, spoken.ToArray());
                spoken.Clear();

                // Settled: nothing more is said.
                Tick(input);
                Assert.Empty(spoken);
            });
        }

        [Fact]
        public void TitledSubmenuSpeaksItsTitleOnceAsTheListContext()
        {
            var screen = new ComboScreen { SpeakTitle = true };
            Run(screen, (spoken, input) =>
            {
                Assert.True(Navigation.DispatchJustPressed(Action(UiActions.Activate)));
                Tick(input);
                Assert.Equal(new[] { "Display Mode, list, Windowed Borderless, button, selected, 2 of 3" }, spoken.ToArray());
                spoken.Clear();

                // Escape leaves the value alone; the return still re-reads the control.
                Assert.True(Navigation.DispatchJustPressed(Action(UiActions.Back)));
                Tick(input);
                Assert.Equal(1, screen.Value);
                Assert.Equal(new[] { "Display Mode, combo box, Windowed Borderless, 1 of 2" }, spoken.ToArray());
            });
        }

        [Fact]
        public void ReturnLandingElsewhereReadsOnlyTheLevelsEntered()
        {
            var screen = new ComboScreen { SpeakTitle = false };
            Run(screen, (spoken, input) =>
            {
                Assert.True(Navigation.DispatchJustPressed(Action(UiActions.Activate)));
                Tick(input);
                spoken.Clear();

                // The pick removes the combo box: focus falls to its neighbour, spoken as a move within
                // the settings list, not as a fresh screen entry.
                screen.HideCombo = true;
                Assert.True(Navigation.DispatchJustPressed(Action(UiActions.Activate)));
                Tick(input);
                Assert.Equal(new[] { "VSync, toggle" }, spoken.ToArray());
            });
        }

        // An item's "equip to" list as rows: each hero, then its slots; the item's sale a line of its own.
        private sealed class EquipScreen : Screen
        {
            public string Picked;

            public override string Key => "test.equip";
            public override bool IsActive() => true;

            public override void Build(GraphBuilder b)
            {
                b.AddItem(ControlId.Structural("quiver"), new NodeVtable
                {
                    Announcements = new[] { new NodeAnnouncement(() => "Quiver", kind: AnnouncementKinds.Label) },
                    OnActivate = () => ChoiceSubmenuScreen.Open("Equip Quiver to", new List<ChoiceOption>
                    {
                        new ChoiceOption("Kai", () => Picked = "Kai", row: "Kai"),
                        new ChoiceOption("slot 1", () => Picked = "Kai 1", "empty", row: "Kai"),
                        new ChoiceOption("slot 2", () => Picked = "Kai 2", "Hammer", row: "Kai"),
                        new ChoiceOption("Reyna", () => Picked = "Reyna", row: "Reyna"),
                        new ChoiceOption("slot 1", () => Picked = "Reyna 1", "empty", row: "Reyna"),
                        new ChoiceOption("Sell Quiver", () => Picked = "sell"),
                    }),
                });
            }
        }

        [Fact]
        public void RowsWalkAcrossAHerosSlotsAndNameTheHeroOnEnteringItsRow()
        {
            var spoken = new List<string>();
            var input = new FakeNavInput { FrameCount = 1 };
            var previous = Navigation.Active;
            Speech.Speak = (t, i) => spoken.Add(t);
            NavInput.Current = input;
            GraphAnnouncer.PositionText = (i, n) => i + " of " + n;
            Navigation.Active = new GraphNavigator();
            var screen = new EquipScreen();
            try
            {
                ScreenManager.Register(screen);
                Tick(input);
                spoken.Clear();
                Assert.True(Navigation.DispatchJustPressed(Action(UiActions.Activate)));
                Tick(input);
                // The hero's own label is the row's caption: said once.
                Assert.Equal(new[] { "Equip Quiver to, list, Kai, button" }, spoken.ToArray());
                spoken.Clear();

                Assert.True(Navigation.DispatchJustPressed(Action(UiActions.Right)));
                Assert.True(Navigation.DispatchJustPressed(Action(UiActions.Right)));
                Assert.Equal(new[] { "slot 1, button, empty", "slot 2, button, Hammer" }, spoken.ToArray());
                spoken.Clear();

                // Down keeps the column where the next row reaches it, else lands on its hero.
                Assert.True(Navigation.DispatchJustPressed(Action(UiActions.Down)));
                Assert.True(Navigation.DispatchJustPressed(Action(UiActions.Right)));
                Assert.True(Navigation.DispatchJustPressed(Action(UiActions.Up)));
                Assert.True(Navigation.DispatchJustPressed(Action(UiActions.Down)));
                Assert.Equal(new[] { "Reyna, button", "slot 1, button, empty", "Kai, slot 1, button, empty", "Reyna, slot 1, button, empty" }, spoken.ToArray());
                spoken.Clear();

                Assert.True(Navigation.DispatchJustPressed(Action(UiActions.Down)));
                Assert.Equal(new[] { "Sell Quiver, button" }, spoken.ToArray());

                Assert.True(Navigation.DispatchJustPressed(Action(UiActions.Up)));
                Assert.True(Navigation.DispatchJustPressed(Action(UiActions.Activate)));
                Tick(input);
                Assert.Equal("Reyna", screen.Picked);
            }
            finally
            {
                ScreenManager.Shutdown();
                Navigation.Active = previous;
                Speech.Speak = (t, i) => { };
                GraphAnnouncer.PositionText = null;
            }
        }

        [Fact]
        public void AnOuterScreenChangeStillReadsTheFullPath()
        {
            var screen = new ComboScreen();
            Run(screen, (spoken, input) =>
            {
                // Re-attaching after another screen took over is a fresh entry: the whole path reads.
                Navigation.Attach(null);
                Navigation.Attach(screen);
                Tick(input);
                Assert.Equal(new[] { "Settings, list, Display Mode, combo box, Windowed Borderless, 1 of 2" }, spoken.ToArray());
            });
        }
    }
}

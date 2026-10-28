using System.Collections.Generic;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.Input;
using MonsterTrainAccessibility.Core.Screens;
using MonsterTrainAccessibility.Core.UI;
using Xunit;

namespace MonsterTrainAccessibility.Tests
{
    // The navigator over a stub screen, driven through the same action keys the input layer uses, with
    // speech captured through the Core seam.
    public class NavigatorTests
    {
        private sealed class MenuScreen : Screen
        {
            public List<string> Items = new List<string> { "Continue", "New run", "Settings", "Quit" };
            public bool QuietRows; // rows that vanish without their replacement being spoken
            public override string Key => "test.menu";
            public override string ScreenName => "Test menu";
            public override bool IsActive() => true;
            public override void Build(GraphBuilder b)
            {
                b.PushContext("Main menu", "list");
                foreach (var item in Items)
                    b.AddItem(ControlId.Structural(item), new NodeVtable
                    {
                        ControlType = ControlTypes.Button,
                        QuietVanish = QuietRows,
                        Announcements = new[] { new NodeAnnouncement(() => item, kind: AnnouncementKinds.Label) },
                    });
                b.PopContext();
            }
        }

        private static InputAction Action(string key) => new InputAction(key, key) { Category = InputCategory.UI };

        // A screen answering a key of its own (the run HUD's Shift+arrow moves) at screen level.
        private sealed class OwnKeyScreen : Screen
        {
            public int Moves;
            public override string Key => "test.ownkey";
            public override bool IsActive() => true;
            public override void Build(GraphBuilder b)
            {
                b.PushContext("Board", null);
                b.AddItem(ControlId.Structural("cell"), new NodeVtable
                {
                    Announcements = new[] { new NodeAnnouncement(() => "cell", kind: AnnouncementKinds.Label) },
                });
                b.PopContext();
            }
            public override IEnumerable<ElementAction> GetActions()
            {
                yield return new ElementAction("run.move.up", "Move hero up", _ => Moves++);
            }
        }

        // Radio buttons that are chosen by landing on them (a list of enemies, a row of clans).
        private sealed class ChooseOnLandingScreen : Screen
        {
            public readonly List<string> Chosen = new List<string>();
            public override string Key => "test.choose";
            public override bool IsActive() => true;
            public override void Build(GraphBuilder b)
            {
                b.PushContext("Enemies", "list");
                foreach (var name in new[] { "Forsaken", "Stryx", "Hallow" })
                    b.AddItem(ControlId.Structural(name), new NodeVtable
                    {
                        ControlType = ControlTypes.RadioButton,
                        Announcements = new[] { new NodeAnnouncement(() => name, kind: AnnouncementKinds.Label) },
                        OnFocus = () => Chosen.Add(name),
                    });
                b.PopContext();
            }
        }

        [Fact]
        public void ATypeAheadResultIsALandingThatRunsTheFocusHook()
        {
            Speech.Speak = (t, i) => { };
            var input = new FakeNavInput { FrameCount = 1 };
            NavInput.Current = input;
            try
            {
                var nav = new GraphNavigator();
                var screen = new ChooseOnLandingScreen();
                nav.Attach(screen);
                nav.EnsureFocus();
                nav.TickTypeahead(); // the first tick on a screen only notes it

                input.TypedText = "s";
                nav.TickTypeahead();

                Assert.Contains("Stryx", screen.Chosen);
            }
            finally
            {
                Speech.Speak = (t, i) => { };
            }
        }

        [Fact]
        public void AUiKeyTheNavigatorDoesNotOwnGoesToTheScreenByItsKey()
        {
            Speech.Speak = (t, i) => { };
            NavInput.Current = new FakeNavInput { FrameCount = 1 };
            try
            {
                var nav = new GraphNavigator();
                var screen = new OwnKeyScreen();
                nav.Attach(screen);
                nav.EnsureFocus();

                Assert.True(nav.OnInputJustPressed(Action("run.move.up")));
                Assert.Equal(1, screen.Moves);
                // A key nobody answers is not consumed: it falls through to its own handler.
                Assert.False(nav.OnInputJustPressed(Action("run.move.down")));
                Assert.Equal(1, screen.Moves);
            }
            finally
            {
                Speech.Speak = (t, i) => { };
            }
        }

        // A tab strip: landing on a tab selects it (the vtable's focus hook), Enter selects it too.
        private sealed class TabScreen : Screen
        {
            public int Selected;
            public int FocusCalls;
            public override string Key => "test.tabs";
            public override bool IsActive() => true;
            public override void Build(GraphBuilder b)
            {
                b.PushContext("Sections", null);
                for (int i = 0; i < 3; i++)
                {
                    int index = i;
                    b.AddItem(ControlId.Structural("tab" + i), new NodeVtable
                    {
                        ControlType = ControlTypes.Tab,
                        Announcements = new[] { new NodeAnnouncement(() => "Tab " + index, kind: AnnouncementKinds.Label) },
                        OnFocus = () => { FocusCalls++; Selected = index; },
                    });
                }
                b.PopContext();
            }
        }

        [Fact]
        public void LandingOnATabRunsItsFocusHookButFirstFocusDoesNot()
        {
            var input = new FakeNavInput { FrameCount = 1 };
            Speech.Speak = (t, i) => { };
            NavInput.Current = input;
            try
            {
                var nav = new GraphNavigator();
                var screen = new TabScreen();
                nav.Attach(screen);
                nav.EnsureFocus();
                Assert.Equal(0, screen.FocusCalls); // the screen's first focus is not a choice

                Assert.True(nav.OnInputJustPressed(Action(UiActions.Down)));
                Assert.Equal(1, screen.FocusCalls);
                Assert.Equal(1, screen.Selected);

                Assert.True(nav.OnInputJustPressed(Action(UiActions.End)));
                Assert.Equal(2, screen.FocusCalls);
                Assert.Equal(2, screen.Selected);
            }
            finally
            {
                Speech.Speak = (t, i) => { };
            }
        }

        [Fact]
        public void EntryReadsContextThenArrowsReadLeaves()
        {
            var spoken = new List<string>();
            var input = new FakeNavInput { FrameCount = 1 };
            Speech.Speak = (t, i) => spoken.Add(t);
            NavInput.Current = input;
            GraphAnnouncer.PositionText = (i, n) => i + " of " + n;
            try
            {
                var nav = new GraphNavigator();
                var screen = new MenuScreen();
                nav.Attach(screen);
                nav.EnsureFocus();
                Assert.Equal("Main menu, list, Continue, button, 1 of 4", spoken[spoken.Count - 1]);

                Assert.True(nav.OnInputJustPressed(Action(UiActions.Down)));
                Assert.Equal("New run, button, 2 of 4", spoken[spoken.Count - 1]);

                Assert.True(nav.OnInputJustPressed(Action(UiActions.End)));
                Assert.Equal("Quit, button, 4 of 4", spoken[spoken.Count - 1]);

                // A rebuild that removes the focused item lands on the nearest survivor, spoken once.
                screen.Items.Remove("Quit");
                int before = spoken.Count;
                input.FrameCount += 10; // past the idle-rebuild throttle
                nav.EnsureFocus();
                Assert.Equal(before + 1, spoken.Count);
                Assert.Equal("Settings, button, 3 of 3", spoken[spoken.Count - 1]);
            }
            finally
            {
                Speech.Speak = (t, i) => { };
                GraphAnnouncer.PositionText = null;
            }
        }

        [Fact]
        public void AQuietRowVanishingUnderFocusMovesSilentlyAndTheNextKeySpeaks()
        {
            var spoken = new List<string>();
            var input = new FakeNavInput { FrameCount = 1 };
            Speech.Speak = (t, i) => spoken.Add(t);
            NavInput.Current = input;
            GraphAnnouncer.PositionText = (i, n) => i + " of " + n;
            try
            {
                var nav = new GraphNavigator();
                var screen = new MenuScreen { QuietRows = true };
                nav.Attach(screen);
                nav.EnsureFocus();
                Assert.True(nav.OnInputJustPressed(Action(UiActions.Down)));
                Assert.Equal("New run, button, 2 of 4", spoken[spoken.Count - 1]);

                // The focused quiet row goes: focus slides to the nearest survivor without a word.
                screen.Items.Remove("New run");
                int before = spoken.Count;
                input.FrameCount += 10;
                nav.EnsureFocus();
                Assert.Equal(before, spoken.Count);
                input.FrameCount += 10;
                nav.EnsureFocus(); // and stays quiet on later frames
                Assert.Equal(before, spoken.Count);

                // The next key reads from where focus now sits (the plain nearest-survivor walk went to
                // the previous row, Continue).
                Assert.True(nav.OnInputJustPressed(Action(UiActions.Down)));
                Assert.Equal("Settings, button, 2 of 3", spoken[spoken.Count - 1]);
            }
            finally
            {
                Speech.Speak = (t, i) => { };
                GraphAnnouncer.PositionText = null;
            }
        }

        // One unit whose line reads a value that the "game" changes under focus.
        private sealed class UnitScreen : Screen
        {
            public int Health = 950;
            public int Mana = 40;
            public override string Key => "test.unit";
            public override bool IsActive() => true;
            public override void Build(GraphBuilder b)
            {
                b.AddItem(ControlId.Structural("unit"), new NodeVtable
                {
                    LiveReadout = true,
                    Announcements = new[]
                    {
                        new NodeAnnouncement(() => "Skorn, " + Health + " health", kind: AnnouncementKinds.Label),
                        // Spoken with the line, never a trigger on its own.
                        new NodeAnnouncement(() => "mana " + Mana, kind: AnnouncementKinds.Value) { LiveReadoutIgnore = true },
                    },
                });
            }
        }

        [Fact]
        public void ALiveReadoutNodeRereadsItselfWhenItsStateChangesOncePerWindow()
        {
            var spoken = new List<string>();
            var interrupts = new List<bool>();
            var input = new FakeNavInput { FrameCount = 1 }; // UnscaledTime = frames / 60
            Speech.Speak = (t, i) => { spoken.Add(t); interrupts.Add(i); };
            NavInput.Current = input;
            try
            {
                var nav = new GraphNavigator();
                var screen = new UnitScreen();
                nav.Attach(screen);
                nav.EnsureFocus();
                Assert.Single(spoken); // the landing
                Assert.EndsWith("Skorn, 950 health, mana 40", spoken[0]);

                input.FrameCount += 2;
                nav.EnsureFocus(); // nothing changed: silence
                Assert.Single(spoken);

                screen.Mana = 45;
                input.FrameCount += 30;
                nav.EnsureFocus(); // an ignored part changed on its own: silence
                Assert.Single(spoken);

                screen.Health = 900;
                input.FrameCount += 2;
                nav.EnsureFocus();
                Assert.Equal(2, spoken.Count); // a change: the whole line again, interrupting, mana as it is now
                Assert.Equal("Skorn, 900 health, mana 45", spoken[1]);
                Assert.True(interrupts[1]);

                screen.Health = 850;
                input.FrameCount += 2; // 33 ms later: inside the window, held back
                nav.EnsureFocus();
                Assert.Equal(2, spoken.Count);

                screen.Health = 800;
                screen.Mana = 50;
                input.FrameCount += 30; // half a second on: one re-read, the latest state only
                nav.EnsureFocus();
                Assert.Equal(3, spoken.Count);
                Assert.Equal("Skorn, 800 health, mana 50", spoken[2]);

                input.FrameCount += 30;
                nav.EnsureFocus(); // steady: silence
                Assert.Equal(3, spoken.Count);
            }
            finally
            {
                Speech.Speak = (t, i) => { };
            }
        }
    }
}

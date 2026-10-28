using System.Collections.Generic;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.Input;
using MonsterTrainAccessibility.Core.Screens;
using MonsterTrainAccessibility.Core.UI;
using Xunit;

namespace MonsterTrainAccessibility.Tests
{
    // A screen's jump key (Alt+B for the board): the navigator hands the key to the screen, which
    // lands on a Tab-stop as Tab would, from anywhere, and says the landing as a move.
    public class JumpToStopTests
    {
        private sealed class HudScreen : Screen
        {
            public bool BoardUp = true;
            public override string Key => "test.hud";
            public override bool IsActive() => true;
            public override void Build(GraphBuilder b)
            {
                b.PushContext("Run", null, positions: false);
                b.BeginStop("actions");
                b.AddItem(ControlId.Structural("fight"), Item("Fight"));
                if (BoardUp)
                {
                    b.BeginStop("board");
                    b.PushContext("board", null);
                    b.AddItem(ControlId.Structural("cell1"), Item("cell 1"));
                    b.AddItem(ControlId.Structural("cell2"), Item("cell 2"));
                    b.PopContext();
                }
                b.BeginStop("party");
                b.PushContext("party", "list");
                b.AddItem(ControlId.Structural("slot1"), Item("slot 1"));
                b.PopContext();
                b.PopContext();
            }

            private static NodeVtable Item(string label) => new NodeVtable
            {
                Announcements = new[] { new NodeAnnouncement(() => label, kind: AnnouncementKinds.Label) },
            };

            public override IEnumerable<ElementAction> GetActions()
            {
                // Offered only while the stop is up, as the run screens offer theirs.
                if (Navigation.HasStop("board"))
                    yield return new ElementAction("jump.board", "Jump to board", _ => Navigation.JumpToStop("board"));
                if (Navigation.HasStop("party"))
                    yield return new ElementAction("jump.party", "Jump to party", _ => Navigation.JumpToStop("party"));
            }
        }

        private static InputAction Action(string key) => new InputAction(key, key) { Category = InputCategory.UI };

        [Fact]
        public void AJumpKeyLandsOnTheStopAndSpeaksTheLanding()
        {
            var spoken = new List<string>();
            Speech.Speak = (t, i) => spoken.Add(t);
            NavInput.Current = new FakeNavInput { FrameCount = 1 };
            var nav = new GraphNavigator();
            Navigation.Active = nav;
            try
            {
                var screen = new HudScreen();
                nav.Attach(screen);
                nav.EnsureFocus();
                Assert.Equal("fight", nav.FocusedNodeId.StructuralKey);
                spoken.Clear();

                Assert.True(nav.OnInputJustPressed(Action("jump.party")));
                Assert.Equal("slot1", nav.FocusedNodeId.StructuralKey);
                Assert.Contains(spoken, s => s.Contains("party") && s.Contains("slot 1"));
                spoken.Clear();

                // The stop's remembered position is the landing, as with Tab.
                Assert.True(nav.OnInputJustPressed(Action("jump.board")));
                Assert.Equal("cell1", nav.FocusedNodeId.StructuralKey);
                Assert.True(nav.OnInputJustPressed(Action(UiActions.Down)));
                Assert.Equal("cell2", nav.FocusedNodeId.StructuralKey);
                Assert.True(nav.OnInputJustPressed(Action("jump.party")));
                Assert.True(nav.OnInputJustPressed(Action("jump.board")));
                Assert.Equal("cell2", nav.FocusedNodeId.StructuralKey);
                spoken.Clear();

                // Pressed on the stop itself: the focused control is read again, as the answer.
                Assert.True(nav.OnInputJustPressed(Action("jump.board")));
                Assert.Equal("cell2", nav.FocusedNodeId.StructuralKey);
                Assert.Contains(spoken, s => s.Contains("cell 2"));

                // A stop that is not up is not offered: the key is not consumed, and the help would not list it.
                screen.BoardUp = false;
                Assert.False(nav.WouldHandle(Action("jump.board")));
                Assert.True(nav.WouldHandle(Action("jump.party")));
                Assert.False(nav.OnInputJustPressed(Action("jump.board")));
            }
            finally
            {
                Speech.Speak = (t, i) => { };
                Navigation.Active = new GraphNavigator();
            }
        }
    }
}

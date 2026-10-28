using System.Collections.Generic;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.Input;
using MonsterTrainAccessibility.Core.Screens;
using MonsterTrainAccessibility.Core.UI;
using Xunit;

namespace MonsterTrainAccessibility.Tests
{
    // A screen of raw (unwired) nodes that answers the arrows itself, landing through MoveTo: the run
    // HUD's hex board, where the arrows jump between units instead of riding edges.
    public class ScreenAnsweredArrowTests
    {
        private sealed class BoardScreen : Screen
        {
            public readonly List<string> Answered = new List<string>();
            public override string Key => "test.board";
            public override bool IsActive() => true;

            public override void Build(GraphBuilder b)
            {
                b.PushContext("Board", null, positions: false);
                b.SetRegion("heroes");
                b.PushContext("heroes", null, positions: false);
                Cell(b, "h1");
                Cell(b, "h2");
                b.PopContext();
                b.SetRegion("enemies");
                b.PushContext("enemies", null, positions: false);
                Cell(b, "e1");
                b.PopContext();
                b.SetRegion(null);
                b.PopContext();
            }

            private static void Cell(GraphBuilder b, string name)
                => b.AddNode(ControlId.Structural(name), new NodeVtable
                {
                    Announcements = new[] { new NodeAnnouncement(() => name, kind: AnnouncementKinds.Label) },
                });

            public override IEnumerable<ElementAction> GetActions()
            {
                yield return new ElementAction(UiActions.Up, "up", _ => { Answered.Add("up"); Navigation.MoveTo(ControlId.Structural("e1")); });
                yield return new ElementAction(UiActions.Right, "right", _ => { Answered.Add("right"); Navigation.MoveTo(ControlId.Structural("h2")); });
                yield return new ElementAction(UiActions.End, "end", _ => { Answered.Add("end"); Navigation.MoveTo(ControlId.Structural("h2")); });
            }
        }

        private static InputAction Action(string key) => new InputAction(key, key) { Category = InputCategory.UI };

        [Fact]
        public void AnArrowWithoutAnEdgeGoesToTheScreenAndMoveToLandsAsAMove()
        {
            var spoken = new List<string>();
            Speech.Speak = (t, i) => spoken.Add((i ? "!" : "") + t);
            NavInput.Current = new FakeNavInput { FrameCount = 1 };
            var nav = new GraphNavigator();
            var previous = Navigation.Active;
            Navigation.Active = nav;
            try
            {
                var screen = new BoardScreen();
                nav.Attach(screen);
                nav.EnsureFocus();
                Assert.Equal("Board, heroes, h1", spoken[spoken.Count - 1]);

                // Right: a raw node has no edge; the screen answers by the key, landing on h2 as an
                // interrupting move.
                Assert.True(nav.OnInputJustPressed(Action(UiActions.Right)));
                Assert.Equal(new[] { "right" }, screen.Answered.ToArray());
                Assert.Equal("!h2", spoken[spoken.Count - 1]);
                Assert.Equal("h2", nav.FocusedNode.Id.StructuralKey);

                // Up: crossing into the enemies region reads the context entered, then the cell.
                Assert.True(nav.OnInputJustPressed(Action(UiActions.Up)));
                Assert.Equal("!enemies, e1", spoken[spoken.Count - 1]);

                // A key the screen does not answer bubbles: nothing spoken, focus stays.
                int count = spoken.Count;
                Assert.False(nav.OnInputJustPressed(Action(UiActions.Down)));
                Assert.False(nav.OnInputJustPressed(Action(UiActions.Left)));
                Assert.Equal(count, spoken.Count);
                Assert.Equal("e1", nav.FocusedNode.Id.StructuralKey);

                // End: nothing wired below either, so the screen answers it too.
                Assert.True(nav.OnInputJustPressed(Action(UiActions.End)));
                Assert.Equal("!heroes, h2", spoken[spoken.Count - 1]);
                Assert.Equal(new[] { "right", "up", "end" }, screen.Answered.ToArray());
            }
            finally
            {
                Speech.Speak = (t, i) => { };
                Navigation.Active = previous;
            }
        }

        [Fact]
        public void MoveToRefusesTheFocusedNodeAndUnknownOnes()
        {
            var spoken = new List<string>();
            Speech.Speak = (t, i) => spoken.Add(t);
            NavInput.Current = new FakeNavInput { FrameCount = 1 };
            var nav = new GraphNavigator();
            try
            {
                nav.Attach(new BoardScreen());
                nav.EnsureFocus();
                int count = spoken.Count;
                Assert.False(nav.MoveTo(ControlId.Structural("h1")));
                Assert.False(nav.MoveTo(ControlId.Structural("nowhere")));
                Assert.False(nav.MoveTo(null));
                Assert.Equal(count, spoken.Count);
                Assert.Equal("h1", nav.FocusedNode.Id.StructuralKey);

                Assert.True(nav.MoveTo(ControlId.Structural("h2")));
                Assert.Equal("h2", spoken[spoken.Count - 1]);
                Assert.Equal("h2", nav.FocusedNode.Id.StructuralKey);
            }
            finally
            {
                Speech.Speak = (t, i) => { };
            }
        }
    }
}

using System.Collections.Generic;
using MonsterTrainAccessibility.Core.UI;
using Xunit;

namespace MonsterTrainAccessibility.Tests
{
    public class RouteCursorTests
    {
        // battle -> (left: a1, a2 | right: b1) -> shared s -> boss
        private static readonly Dictionary<string, string[]> Route = new Dictionary<string, string[]>
        {
            ["battle"] = new[] { "a1", "b1" },
            ["a1"] = new[] { "a2" },
            ["a2"] = new[] { "s" },
            ["b1"] = new[] { "s" },
            ["s"] = new[] { "boss" },
            ["boss"] = new string[0],
        };

        private static RouteCursor<string> Make(string origin = "battle")
        {
            var cursor = new RouteCursor<string>(stop => Route[stop]);
            cursor.Reset(origin);
            return cursor;
        }

        [Fact]
        public void ForwardLandsOnTheFirstOfAForkAndSaysItArrivedAtOne()
        {
            var cursor = Make();
            var step = cursor.Forward();
            Assert.Equal(RouteMove.Moved, step.Move);
            Assert.Equal("a1", step.Stop);
            Assert.True(step.ArrivedAtFork);
            Assert.Equal(2, step.Count);
        }

        [Fact]
        public void CyclingWithinAForkIsNotAnArrivalAndStopsAtTheEnds()
        {
            var cursor = Make();
            cursor.Forward();
            var right = cursor.Cycle(1);
            Assert.Equal("b1", right.Stop);
            Assert.False(right.ArrivedAtFork);
            Assert.Equal(RouteMove.Edge, cursor.Cycle(1).Move);
            Assert.Equal("b1", cursor.Current);
            Assert.Equal("a1", cursor.Cycle(-1).Stop);
            Assert.Equal(RouteMove.Edge, cursor.Cycle(-1).Move);
        }

        [Fact]
        public void ASingleWayOnIsNoForkAndCannotBeCycled()
        {
            var cursor = Make("a1");
            var step = cursor.Forward();
            Assert.Equal("a2", step.Stop);
            Assert.False(step.ArrivedAtFork);
            Assert.Equal(RouteMove.Edge, cursor.Cycle(1).Move);
        }

        [Fact]
        public void ForwardFollowsTheBranchCycledTo()
        {
            var cursor = Make();
            cursor.Forward();
            cursor.Cycle(1); // b1
            Assert.Equal("s", cursor.Forward().Stop);
            Assert.Equal("boss", cursor.Forward().Stop);
            var end = cursor.Forward();
            Assert.Equal(RouteMove.NoForward, end.Move);
            Assert.Equal("boss", end.Stop);
        }

        [Fact]
        public void BackRetracesThePathRestoringTheForkPosition()
        {
            var cursor = Make();
            cursor.Forward();
            cursor.Cycle(1); // b1
            cursor.Forward(); // s
            var back = cursor.Back();
            Assert.Equal("b1", back.Stop);
            Assert.True(back.ArrivedAtFork); // back among the fork's siblings: a choice again
            Assert.Equal(1, back.Index);
            Assert.Equal("a1", cursor.Cycle(-1).Stop);
            Assert.Equal("battle", cursor.Back().Stop);
            Assert.Equal(RouteMove.NoBackward, cursor.Back().Move);
            Assert.Equal(0, cursor.Depth);
        }

        [Fact]
        public void ResetForgetsThePath()
        {
            var cursor = Make();
            cursor.Forward();
            cursor.Forward();
            cursor.Reset("s");
            Assert.Equal("s", cursor.Current);
            Assert.Equal(0, cursor.Depth);
            Assert.Equal(RouteMove.NoBackward, cursor.Back().Move);
            Assert.Equal(RouteMove.Edge, cursor.Cycle(1).Move);
        }
    }
}

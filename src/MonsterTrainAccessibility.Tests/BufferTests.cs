using System.Collections.Generic;
using System.Linq;
using MonsterTrainAccessibility.Core.Buffers;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.UI;
using Xunit;

namespace MonsterTrainAccessibility.Tests
{
    public class BufferTests
    {
        private static Buffer Make(string key = "ui") => new Buffer(key, () => key);

        [Fact]
        public void ReadsLinesFromSourceAndStepsWithinBounds()
        {
            var buffer = Make();
            buffer.SetSource(() => new[] { "one", "two" });
            Assert.Equal("one", buffer.CurrentLine);
            Assert.True(buffer.MoveNext());
            Assert.Equal("two", buffer.CurrentLine);
            Assert.False(buffer.MoveNext()); // edge
            Assert.True(buffer.MovePrevious());
            Assert.False(buffer.MovePrevious()); // edge
        }

        [Fact]
        public void RefreshRereadsLiveSourcePreservingPosition()
        {
            var lines = new List<string> { "a", "b", "c" };
            var buffer = Make();
            buffer.SetSource(() => lines);
            buffer.MoveNext();
            lines[1] = "b2"; // the live model changed under us
            Assert.Equal("b2", buffer.CurrentLine);
        }

        [Fact]
        public void PositionResetsWhenContentShrinksUnderTheCursor()
        {
            var lines = new List<string> { "a", "b", "c" };
            var buffer = Make();
            buffer.SetSource(() => lines);
            buffer.MoveNext();
            buffer.MoveNext();
            lines.RemoveRange(1, 2);
            Assert.Equal("a", buffer.CurrentLine);
        }

        [Fact]
        public void ResetGoesBackToTheFirstLineOfTheSameSource()
        {
            var buffer = Make();
            buffer.SetSource(() => new[] { "a", "b" });
            buffer.MoveNext();
            buffer.Reset();
            Assert.Equal("a", buffer.CurrentLine);
        }

        [Fact]
        public void NullSourceEmpties()
        {
            var buffer = Make();
            buffer.SetSource(() => new[] { "x" });
            buffer.SetSource(null);
            Assert.True(buffer.IsEmpty);
            Assert.Null(buffer.CurrentLine);
        }

        [Fact]
        public void ManagerSkipsEmptyBuffersAndWraps()
        {
            var manager = new BufferManager();
            var a = manager.Add(new Buffer("a", () => "a"));
            manager.Add(new Buffer("b", () => "b"));
            var c = manager.Add(new Buffer("c", () => "c"));
            a.SetSource(() => new[] { "line a" });
            c.SetSource(() => new[] { "line c" });

            manager.SetCurrent("a");
            Assert.True(manager.MoveBuffer(1)); // skips empty b
            Assert.Equal("c", manager.Current.Key);
            Assert.True(manager.MoveBuffer(1)); // wraps
            Assert.Equal("a", manager.Current.Key);
        }

        [Fact]
        public void ManagerCurrentIsNullWhenEverythingIsEmpty()
        {
            var manager = new BufferManager();
            manager.Add(new Buffer("a", () => "a"));
            Assert.Null(manager.Current);
            Assert.False(manager.MoveBuffer(1));
        }

        [Fact]
        public void FollowLatestJumpsToNewestLineOnSwitch()
        {
            var manager = new BufferManager();
            var events = manager.Add(new Buffer("events", () => "events") { FollowLatest = true });
            events.SetSource(() => new[] { "old", "new" });
            manager.SetCurrent("events");
            Assert.Equal("new", events.CurrentLine);
        }
    }

    public class BufferControlsTests
    {
        private readonly List<string> _spoken = new List<string>();
        private readonly BufferManager _manager = new BufferManager();
        private readonly BufferControls _controls;

        public BufferControlsTests()
        {
            _controls = new BufferControls(_manager, (text, interrupt) => _spoken.Add(text));
        }

        [Fact]
        public void SwitchingBufferSpeaksNameAndCurrentLine()
        {
            _manager.Add(new Buffer("ui", () => "control")).SetSource(() => new[] { "Continue", "tooltip line" });
            _controls.NextBuffer();
            Assert.Equal("control: Continue", Assert.Single(_spoken));
        }

        [Fact]
        public void SteppingSpeaksJustTheLineAndEdgesRepeat()
        {
            _manager.Add(new Buffer("ui", () => "control")).SetSource(() => new[] { "first", "second" });
            _controls.NextLine();
            Assert.Equal("second", _spoken[_spoken.Count - 1]);
            _controls.NextLine(); // at the edge: re-reads
            Assert.Equal("second", _spoken[_spoken.Count - 1]);
            _controls.PreviousLine();
            Assert.Equal("first", _spoken[_spoken.Count - 1]);
        }

        [Fact]
        public void NoContentSpeaksNoBufferLines()
        {
            _manager.Add(new Buffer("ui", () => "control"));
            _controls.NextLine();
            Assert.Equal("no buffer lines", Assert.Single(_spoken));
        }
    }

    // The ui buffer composed from a node: the head without the role or position, one line per
    // description part and per detail, repeats of the head folded, blanks dropped.
    public class NodeLinesTests
    {
        private static GraphNode Build(NodeVtable vt, int siblings = 1)
        {
            var b = new GraphBuilder();
            b.PushContext("Shop", "list");
            for (int i = 0; i < siblings; i++)
                b.AddItem(ControlId.Structural("n" + i), i == 0 ? vt : new NodeVtable { Announcements = new[] { NodeAnnouncement.Static("other") } });
            var render = b.Build();
            return render.Nodes[ControlId.Structural("n0")];
        }

        [Fact]
        public void HeadCarriesLabelValueAndStateButNoRoleOrPosition()
        {
            GraphAnnouncer.PositionText = (i, n) => i + " of " + n;
            try
            {
                var node = Build(new NodeVtable
                {
                    ControlType = ControlTypes.Toggle,
                    Announcements = new[]
                    {
                        new NodeAnnouncement(() => "Tutorials", kind: AnnouncementKinds.Label),
                        new NodeAnnouncement(() => "on", kind: AnnouncementKinds.Value),
                    },
                }, siblings: 3);
                Assert.Equal(new[] { "Tutorials, on" }, NodeLines.Lines(node).ToArray());
            }
            finally { GraphAnnouncer.PositionText = null; }
        }

        [Fact]
        public void ARoleTheNodeSuppliesItselfEndsTheHead()
        {
            var node = Build(new NodeVtable
            {
                ControlType = ControlTypes.Item,
                Announcements = new[]
                {
                    new NodeAnnouncement(() => "Firestarter", kind: AnnouncementKinds.Label),
                    new NodeAnnouncement(() => "Spell", kind: AnnouncementKinds.Role),
                    new NodeAnnouncement(() => "cost 1", kind: AnnouncementKinds.Value),
                },
            });
            // The type's own role word ("item") is replaced, and the node's role closes the head line.
            Assert.Equal(new[] { "Firestarter, cost 1, Spell" }, NodeLines.Lines(node).ToArray());
        }

        [Fact]
        public void AReviewPartEndsTheHeadButIsNeverSpoken()
        {
            var node = Build(new NodeVtable
            {
                ControlType = ControlTypes.Item,
                Announcements = new[]
                {
                    new NodeAnnouncement(() => "Firestarter", kind: AnnouncementKinds.Label),
                    new NodeAnnouncement(() => "Common", kind: AnnouncementKinds.Review),
                    new NodeAnnouncement(() => "Spell", kind: AnnouncementKinds.Role),
                    new NodeAnnouncement(() => "cost 1", kind: AnnouncementKinds.Value),
                },
            });
            Assert.Equal(new[] { "Firestarter, cost 1, Spell, Common" }, NodeLines.Lines(node).ToArray());
            Assert.DoesNotContain("Common", GraphAnnouncer.LeafText(node));
        }

        [Fact]
        public void DescriptionPartsAndDetailsAreSeparateLines()
        {
            var node = Build(new NodeVtable
            {
                Announcements = new[]
                {
                    new NodeAnnouncement(() => "Healing Potion", kind: AnnouncementKinds.Label),
                    new NodeAnnouncement(() => "cost 5", kind: AnnouncementKinds.Value),
                    new NodeAnnouncement(() => "Restores 20 health", kind: AnnouncementKinds.Tooltip),
                },
                Details = () => new[] { "<b>Healing Potion</b>", "Rare item", "  ", null, "Restores 20 health" },
            });
            Assert.Equal(new[] { "Healing Potion, cost 5", "Restores 20 health", "Rare item" }, NodeLines.Lines(node).ToArray());
        }

        [Fact]
        public void SideLinesComeFromTheVtableAndSkipBlanks()
        {
            var node = Build(new NodeVtable
            {
                Announcements = new[] { new NodeAnnouncement(() => "Irini", kind: AnnouncementKinds.Label) },
                SideLines = key => key == "side" ? new[] { "Irini, Duelist", "", "health 650" } : null,
            });
            Assert.Equal(new[] { "Irini, Duelist", "health 650" }, NodeLines.SideLines(node, "side").ToArray());
            Assert.Empty(NodeLines.SideLines(node, "other"));
        }
    }
}

using System.Collections.Generic;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.Screens;
using MonsterTrainAccessibility.Core.UI;
using Xunit;

namespace MonsterTrainAccessibility.Tests
{
    // A composite screen fans Build, actions, update and pop out to its sections in add order, with its
    // own actions first and every section's nodes inside the screen's context.
    public class CompositeScreenTests
    {
        private sealed class Section : ScreenSection
        {
            public readonly string Name;
            public readonly List<string> Calls;
            public Section(string name, List<string> calls) { Name = name; Calls = calls; }

            public override void Build(GraphBuilder b)
            {
                Calls.Add("build " + Name);
                b.BeginStop(Name);
                b.AddItem(ControlId.Structural(Name), new NodeVtable
                {
                    Announcements = new[] { new NodeAnnouncement(() => Name, kind: AnnouncementKinds.Label) },
                });
            }

            public override IEnumerable<ElementAction> GetActions()
            {
                yield return new ElementAction("act." + Name, Name, _ => Calls.Add("action " + Name));
            }

            public override void OnUpdate() => Calls.Add("update " + Name);
            public override void OnPop() => Calls.Add("pop " + Name);
        }

        private sealed class Hud : CompositeScreen
        {
            public readonly List<string> Calls = new List<string>();
            public Hud()
            {
                Add(new Section("first", Calls));
                Add(new Section("second", Calls));
            }
            public override string Key => "test.hud";
            public override bool IsActive() => true;
            protected override string ContextLabel => "Run";
            protected override IEnumerable<ElementAction> OwnActions()
            {
                yield return new ElementAction(ActionIds.Back, "back", _ => Calls.Add("own back"));
            }
        }

        [Fact]
        public void SectionsBuildInOrderInsideTheScreenContext()
        {
            var hud = new Hud();
            var b = new GraphBuilder(new HashSet<ControlId>());
            hud.Build(b);
            var render = b.Build();

            Assert.Equal(new[] { "build first", "build second" }, hud.Calls.ToArray());
            Assert.Equal(2, render.Order.Count);
            Assert.Equal("first", render.Order[0].Id.StructuralKey);
            Assert.Equal("second", render.Order[1].Id.StructuralKey);
            // The wrapping context reads on entry, then the node.
            Assert.Equal("Run, first", GraphAnnouncer.ComposeFull(render.Order[0]));
        }

        [Fact]
        public void OwnActionsResolveBeforeTheSections()
        {
            var hud = new Hud();
            Assert.True(hud.InvokeAction(ActionIds.Back));
            Assert.True(hud.InvokeAction("act.second"));
            Assert.False(hud.InvokeAction("act.none"));
            Assert.Equal(new[] { "own back", "action second" }, hud.Calls.ToArray());
        }

        [Fact]
        public void UpdateAndPopFanOutToEverySection()
        {
            var hud = new Hud();
            hud.OnUpdate();
            hud.OnPop();
            Assert.Equal(new[] { "update first", "update second", "pop first", "pop second" }, hud.Calls.ToArray());
        }
    }
}

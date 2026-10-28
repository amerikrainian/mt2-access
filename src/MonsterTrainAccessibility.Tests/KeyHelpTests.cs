using System.Collections.Generic;
using System.Linq;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.Input;
using MonsterTrainAccessibility.Core.Screens;
using MonsterTrainAccessibility.Core.UI;
using Xunit;

namespace MonsterTrainAccessibility.Tests
{
    // The context-sensitive key help: a key is listed only where it would do something, asked of the
    // navigator without acting, of the focused screen's offers, and of a handler's own availability.
    public class KeyHelpTests
    {
        private sealed class ShopScreen : Screen
        {
            public bool Open = true;
            public override string Key => "test.shop";
            public override bool IsActive() => true;

            public override void Build(GraphBuilder b)
            {
                b.PushContext("Shop", null, positions: false);
                b.BeginStop("offers");
                b.AddItem(ControlId.Structural("a"), Node("a", activate: true));
                b.AddItem(ControlId.Structural("b"), Node("b", activate: false));
                b.PopContext();
            }

            private static NodeVtable Node(string name, bool activate) => new NodeVtable
            {
                Announcements = new[] { new NodeAnnouncement(() => name, kind: AnnouncementKinds.Label) },
                OnActivate = activate ? () => { } : (System.Action)null,
            };

            public override IEnumerable<ElementAction> GetActions()
            {
                if (!Open) yield break;
                yield return new ElementAction("shop.reroll", "Reroll the shop", _ => { });
                yield return new ElementAction(ActionIds.Back, "Leave", _ => { });
            }
        }

        private static InputAction Ui(string key, System.Action handler = null)
        {
            var a = new InputAction(key, key) { Category = InputCategory.UI };
            if (handler != null) a.Performed += handler;
            return a;
        }

        private static string[] Keys(IEnumerable<KeyHelpEntry> entries) => entries.Select(e => e.ActionKey).ToArray();

        private static GraphNavigator Attach(Screen screen, List<string> spoken)
        {
            Speech.EndHold();
            Speech.Speak = (t, i) => spoken.Add((i ? "!" : "") + t);
            NavInput.Current = new FakeNavInput { FrameCount = 1 };
            var nav = new GraphNavigator();
            Navigation.Active = nav;
            nav.Attach(screen);
            nav.EnsureFocus();
            return nav;
        }

        [Fact]
        public void OnlyTheKeysThatWouldDoSomethingAreListedTheScreensOwnFirst()
        {
            var previous = Navigation.Active;
            try
            {
                var screen = new ShopScreen();
                Attach(screen, new List<string>());
                bool onBoard = false;
                var actions = new[]
                {
                    Ui(UiActions.Up), Ui(UiActions.Down), Ui(UiActions.Activate), Ui(UiActions.Secondary), Ui(UiActions.Back),
                    Ui(UiActions.Next), Ui("shop.reroll"), Ui("run.hex.left"),
                    Ui("buffer.next", () => { }),
                    Ui("glance.vitals", () => { }).When(() => true),
                    Ui("run.nearby", () => { }).When(() => onBoard),
                    new InputAction("mod.menu", "Mod menu") { Category = InputCategory.Global },
                };
                actions[actions.Length - 1].Performed += () => { };

                // Focus on "a", the first of two in one stop: Down has an edge, Up has none; Enter
                // activates; nothing answers Backspace; one stop, so no Tab; the screen offers reroll
                // and Escape but no hex key; the glance whose fact is on screen, not the other. The
                // most particular first: the screen's key, the glance, the navigation, the always-on
                // buffer key, the global one.
                var listed = KeyHelp.Collect(actions, _ => true);
                Assert.Equal(new[] { "shop.reroll", "glance.vitals", UiActions.Down, UiActions.Activate, UiActions.Back, "buffer.next", "mod.menu" }, Keys(listed));
                // A key the screen offers reads by the screen's label, Escape too: what it does HERE.
                Assert.Equal("Reroll the shop", listed[0].Label);
                Assert.Equal("Leave", listed[4].Label);
                Assert.Equal("Navigate down", listed[2].Label);

                // The screen stops offering: its keys leave the list, Escape among them.
                screen.Open = false;
                Assert.Equal(new[] { "glance.vitals", UiActions.Down, UiActions.Activate, "buffer.next", "mod.menu" }, Keys(KeyHelp.Collect(actions, _ => true)));

                // A key that is not live (a category not active, a shadowed chord) is not listed.
                Assert.Equal(new[] { "mod.menu" }, Keys(KeyHelp.Collect(actions, a => a.Category == InputCategory.Global)));
                // The help leaves itself out through the skip.
                Assert.DoesNotContain("mod.menu", Keys(KeyHelp.Collect(actions, _ => true, a => a.Key == "mod.menu")));
            }
            finally { Navigation.Active = previous; }
        }

        [Fact]
        public void TheDryRunFollowsTheFocusedControl()
        {
            var previous = Navigation.Active;
            try
            {
                var nav = Attach(new ShopScreen(), new List<string>());
                Assert.True(nav.WouldHandle(Ui(UiActions.Activate)));
                Assert.True(nav.OnInputJustPressed(Ui(UiActions.Down)));
                // "b": Up now has the edge, Down has none, and Enter does nothing here.
                Assert.True(nav.WouldHandle(Ui(UiActions.Up)));
                Assert.False(nav.WouldHandle(Ui(UiActions.Down)));
                Assert.False(nav.WouldHandle(Ui(UiActions.Activate)));
                // Asking moved nothing.
                Assert.Equal("b", nav.FocusedNode.Id.StructuralKey);
            }
            finally { Navigation.Active = previous; }
        }

        [Fact]
        public void AQuietLandingIsRecordedNotSpokenAndOnlyOnce()
        {
            var previous = Navigation.Active;
            try
            {
                var spoken = new List<string>();
                var screen = new ShopScreen();
                var nav = Attach(screen, spoken);
                Assert.Equal("Shop, a", spoken[spoken.Count - 1]);

                // An overlay closes over the screen. Left alone, the landing it brings is spoken (the
                // idle rebuild is throttled by frames, so the clock steps on).
                var clock = (FakeNavInput)NavInput.Current;
                int count = spoken.Count;
                nav.Attach(null);
                nav.Attach(screen);
                clock.FrameCount += 30;
                nav.EnsureFocus();
                Assert.Equal(count + 1, spoken.Count);

                // Asked to be quiet, it is recorded without a word.
                nav.Attach(null);
                nav.QuietNextLanding();
                nav.Attach(screen);
                clock.FrameCount += 30;
                nav.EnsureFocus();
                Assert.Equal(count + 1, spoken.Count);
                Assert.Equal("a", nav.FocusedNode.Id.StructuralKey);

                // Once only: the next landing speaks as ever.
                nav.Attach(null);
                nav.Attach(screen);
                clock.FrameCount += 30;
                nav.EnsureFocus();
                Assert.Equal(count + 2, spoken.Count);

                // And a request nothing consumed goes stale instead of swallowing a later landing.
                nav.QuietNextLanding();
                clock.FrameCount += 600;
                nav.Attach(null);
                nav.Attach(screen);
                clock.FrameCount += 30;
                nav.EnsureFocus();
                Assert.Equal(count + 3, spoken.Count);
            }
            finally { Navigation.Active = previous; }
        }

        [Fact]
        public void AHoldQueuesWhatWouldInterruptAndRemembersIt()
        {
            var spoken = new List<string>();
            Speech.EndHold();
            Speech.Speak = (t, i) => spoken.Add((i ? "!" : "") + t);
            var clock = new FakeNavInput { FrameCount = 1 };
            NavInput.Current = clock;

            // The first line of a hold interrupts, whatever it asked for: it cuts off the help row
            // that was being read when Enter was pressed. What follows queues behind it, in order.
            Speech.Say("Shop freeze: Ctrl+F, 2 of 26", interrupt: true);
            Speech.BeginHold();
            Speech.Say("Unfreeze Shop", interrupt: false);
            Speech.Say("reroll cost 3", interrupt: true);
            Speech.Say("Dragomir", interrupt: false);
            Assert.Equal(new[] { "!Shop freeze: Ctrl+F, 2 of 26", "!Unfreeze Shop", "reroll cost 3", "Dragomir" }, spoken.ToArray());
            Assert.Equal(new[] { "Unfreeze Shop", "reroll cost 3", "Dragomir" }, Speech.Held.ToArray());

            Speech.EndHold();
            Speech.Say("moved", interrupt: true);
            Assert.Equal("!moved", spoken[spoken.Count - 1]);

            // A hold nobody ends lapses by itself.
            Speech.BeginHold();
            clock.FrameCount += 60 * 10;
            Speech.Say("later", interrupt: true);
            Assert.Equal("!later", spoken[spoken.Count - 1]);
        }
    }
}

using System;
using System.Collections.Generic;
using MonsterTrainAccessibility.Core.Input;
using Xunit;

namespace MonsterTrainAccessibility.Tests
{
    // The mod's own key and button rebinding (Star Vaders' tests, with the pad and the game's keys):
    // only changes against the registered defaults are saved, a saved form replaces an action's
    // bindings beside its fixed ones, protected actions keep one per device, and conflicts are within
    // a category (or with Global).
    public class KeyBindingsTests : IDisposable
    {
        // A key or button by name ("Ctrl+M", "LT+A"), saved as "fake:Ctrl+M" or "fakepad:LT+A"; a fixed
        // one (the game's own key) as "fixed:Tab".
        private sealed class Key : InputBinding
        {
            private readonly string _name;
            private readonly string _device;
            private readonly bool _fixed;

            public Key(string name, string device = "keyboard", bool isFixed = false)
            {
                _name = name;
                _device = device;
                _fixed = isFixed;
            }

            public override string DisplayName => _name;
            public override bool JustPressed() => false;
            public override bool Held() => false;
            public override bool Released() => false;
            public override string Device => _device;
            public override bool Fixed => _fixed;
            public override string Type => _fixed ? "fixed" : _device == "controller" ? "fakepad" : "fake";
            public override string Serialize() => _name;
        }

        private static Key Pad(string name) => new Key(name, "controller");

        private string _saved;

        public KeyBindingsTests()
        {
            InputManager.Clear();
            KeyBindings.Clear();
            InputBinding.RegisterType("fake", data => new Key(data));
            InputBinding.RegisterType("fakepad", data => Pad(data));
            InputBinding.RegisterType("fixed", data => new Key(data, isFixed: true));
            InputManager.Register("mod.menu", "Mod menu", InputCategory.Global).AddBinding(new Key("Ctrl+Shift+M")).AddBinding(Pad("LT+Start"));
            InputManager.Register("ui.back", "Back", InputCategory.UI).AddBinding(new Key("Escape")).AddBinding(Pad("B"));
            InputManager.Register("buffer.next", "Next buffer", InputCategory.UI).AddBinding(new Key("Ctrl+Right"));
            InputManager.Register("battle.ember", "Read ember", InputCategory.Battle).AddBinding(new Key("R")).AddBinding(Pad("LT+X"));
            InputManager.Register("battle.moon", "Read moon phase", InputCategory.Battle).AddBinding(new Key("P"));
            InputManager.Register("run.hud", "Run info", InputCategory.Run).AddBinding(new Key("Tab", isFixed: true)).AddBinding(Pad("Y"));
            KeyBindings.Snapshot();
            KeyBindings.Persist = s => _saved = s;
        }

        public void Dispose()
        {
            KeyBindings.Clear();
            InputManager.Clear();
        }

        private static InputAction A(string key) => InputManager.Find(key);

        [Fact]
        public void Only_changed_actions_are_saved_and_a_return_to_the_defaults_saves_nothing()
        {
            KeyBindings.Add(A("battle.moon"), new Key("F3"));
            Assert.Equal("battle.moon=fake:P fake:F3", _saved);
            KeyBindings.Reset(A("battle.moon"));
            Assert.Equal("", _saved);
            Assert.True(KeyBindings.IsDefault(A("battle.moon")));
        }

        [Fact]
        public void A_saved_form_replaces_the_bindings_and_an_empty_list_unbinds()
        {
            KeyBindings.Load("battle.ember=fake:Ctrl+E fakepad:RT+X;battle.moon=");
            Assert.Equal("Ctrl+E, RT+X", A("battle.ember").BindingsDisplay);
            Assert.Empty(A("battle.moon").Bindings);
            Assert.Equal("battle.ember=fake:Ctrl+E fakepad:RT+X;battle.moon=", KeyBindings.Format());
        }

        [Fact]
        public void A_protected_action_keeps_a_binding_on_each_device()
        {
            KeyBindings.Load("mod.menu=fake:F10");
            // The save left it no button: it keeps its default one.
            Assert.Equal("F10, LT+Start", A("mod.menu").BindingsDisplay);
            var key = A("mod.menu").Bindings[0];
            var button = A("mod.menu").Bindings[1];
            Assert.False(KeyBindings.Remove(A("mod.menu"), key));
            Assert.False(KeyBindings.Remove(A("mod.menu"), button));
            KeyBindings.Add(A("mod.menu"), new Key("F11"));
            Assert.True(KeyBindings.Remove(A("mod.menu"), key));
            Assert.Equal("LT+Start, F11", A("mod.menu").BindingsDisplay);
            // An action that is not protected can lose its last.
            Assert.True(KeyBindings.Remove(A("battle.moon"), A("battle.moon").Bindings[0]));
        }

        [Fact]
        public void Unknown_actions_survive_a_save_and_unreadable_bindings_are_skipped()
        {
            KeyBindings.Load("gone.action=fake:X;battle.moon=nope:Z fake:V");
            Assert.Equal("V", A("battle.moon").BindingsDisplay);
            KeyBindings.Add(A("battle.ember"), new Key("Y"));
            Assert.Equal("battle.ember=fake:R fakepad:LT+X fake:Y;battle.moon=fake:V;gone.action=fake:X", _saved);
        }

        [Fact]
        public void The_games_own_keys_stay_with_their_action_are_never_saved_or_removed_and_block_a_rebind()
        {
            var tab = A("run.hud").Bindings[0];
            Assert.False(KeyBindings.CanRemove(A("run.hud"), tab));
            Assert.False(KeyBindings.Remove(A("run.hud"), tab));
            KeyBindings.Load("run.hud=fixed:Q fakepad:RT+Y");
            Assert.Equal("Tab, RT+Y", A("run.hud").BindingsDisplay);
            Assert.Equal("run.hud=fakepad:RT+Y", KeyBindings.Format());
            KeyBindings.Load("run.hud=");
            Assert.Equal("Tab", A("run.hud").BindingsDisplay);
            Assert.Equal("run.hud", KeyBindings.Conflict(A("mod.menu"), new Key("Tab"))?.Key);
        }

        [Fact]
        public void Conflicts_are_within_a_category_or_with_global_and_other_categories_only_overlap()
        {
            Assert.Equal("battle.moon", KeyBindings.Conflict(A("battle.ember"), new Key("P"))?.Key);
            Assert.Equal("mod.menu", KeyBindings.Conflict(A("battle.ember"), new Key("Ctrl+Shift+M"))?.Key);
            Assert.Equal("mod.menu", KeyBindings.Conflict(A("battle.ember"), Pad("LT+Start"))?.Key);
            Assert.Equal("battle.ember", KeyBindings.Conflict(A("mod.menu"), new Key("R"))?.Key);
            Assert.Null(KeyBindings.Conflict(A("battle.ember"), new Key("Ctrl+Right")));
            Assert.Equal(new[] { "buffer.next" }, KeyBindings.Overlaps(A("battle.ember"), new Key("Ctrl+Right")).ConvertAll(a => a.Key));
            Assert.Same(A("battle.ember").Bindings[1], KeyBindings.Existing(A("battle.ember"), Pad("LT+X")));
        }

        [Fact]
        public void Reset_all_restores_every_action_and_forgets_unknown_entries()
        {
            KeyBindings.Load("gone.action=fake:X;battle.moon=fake:V;buffer.next=");
            KeyBindings.ResetAll();
            Assert.Equal("P", A("battle.moon").BindingsDisplay);
            Assert.Equal("Ctrl+Right", A("buffer.next").BindingsDisplay);
            Assert.Equal("", _saved);
        }

        [Fact]
        public void The_saved_form_parses_entries_in_order()
        {
            var entries = KeyBindings.Parse(" a.b=fake:Ctrl+M  x:y ;;c=;bad");
            Assert.Equal(2, entries.Count);
            Assert.Equal("a.b", entries[0].Key);
            Assert.Equal(new List<(string, string)> { ("fake", "Ctrl+M"), ("x", "y") }, entries[0].Value);
            Assert.Equal("c", entries[1].Key);
            Assert.Empty(entries[1].Value);
        }
    }
}

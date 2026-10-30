using System.Collections.Generic;
using MonsterTrainAccessibility.Contracts;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Core.Buffers;
using MonsterTrainAccessibility.Core.Events;
using Xunit;

namespace MonsterTrainAccessibility.Tests
{
    public class EventTests
    {
        private sealed class Store : ISettingsStore
        {
            public readonly Dictionary<string, object> Values = new Dictionary<string, object>();
            public bool GetBool(string key, bool defaultValue) => Values.TryGetValue(key, out var v) ? (bool)v : defaultValue;
            public void SetBool(string key, bool value) => Values[key] = value;
            public int GetInt(string key, int defaultValue) => defaultValue;
            public void SetInt(string key, int value) { }
            public string GetString(string key, string defaultValue) => defaultValue;
            public void SetString(string key, string value) { }
        }

        [Fact]
        public void TheLogIsNewestFirstWithAMultiLineEventInItsOwnOrder()
        {
            var log = new EventLog();
            log.Add("Turn 1.");
            log.Add("first\nsecond");
            Assert.Equal(new[] { "first", "second", "Turn 1." }, log.Lines);
        }

        [Fact]
        public void AReviewingCursorKeepsItsLineAsEventsArrive()
        {
            var log = new EventLog();
            var buffer = new Buffer("events", () => "events");
            buffer.SetSource(() => log.Lines);
            log.Added += buffer.LinesInserted;
            log.Add("a");
            log.Add("b");
            log.Add("c");
            buffer.MoveNext();
            Assert.Equal("b", buffer.CurrentLine);
            log.Add("d");
            Assert.Equal("b", buffer.CurrentLine);
            // On the top line, the cursor stays on top: the newest.
            buffer.MovePrevious();
            buffer.MovePrevious();
            Assert.Equal("d", buffer.CurrentLine);
            log.Add("e");
            Assert.Equal("e", buffer.CurrentLine);
        }

        [Fact]
        public void FlushSpeaksAndLogsByTheKindsSettingsAndSides()
        {
            var spoken = new List<string>();
            var store = new Store();
            Speech.Speak = (t, i) => spoken.Add(t);
            GameEvents.Store = () => store;
            GameEvents.Suppressed = () => false;
            GameEvents.Events.Clear();
            try
            {
                store.SetBool(EventKinds.Damage.EnemyKey, false);
                store.SetBool(EventKinds.Ember.AnnounceKey, false);
                GameEvents.Raise(EventKinds.Damage, "Fel took 3 damage.", EventSide.Friendly);
                GameEvents.Raise(EventKinds.Damage, "Imp took 3 damage.", EventSide.Enemy);
                GameEvents.Raise(EventKinds.Ember, "Ember 2.");
                GameEvents.Raise(EventKinds.ChatterIdle, "Fel \"Hm.\"", EventSide.Friendly);
                GameEvents.Flush();
                Assert.Equal(new[] { "Fel took 3 damage." }, spoken);
                Assert.Equal(new[] { "Ember 2.", "Fel took 3 damage." }, GameEvents.Events.Lines);
                Assert.Equal(new[] { "Fel \"Hm.\"" }, GameEvents.Quotes.Lines);

                // Replaying: logged, not spoken.
                GameEvents.Suppressed = () => true;
                GameEvents.Raise(EventKinds.Turn, "Turn 2.");
                GameEvents.Flush();
                Assert.Single(spoken);
                Assert.Equal("Turn 2.", GameEvents.Events.Lines[0]);
            }
            finally
            {
                Speech.Speak = (t, i) => { };
                GameEvents.Store = () => null;
                GameEvents.Suppressed = () => false;
                GameEvents.Events.Clear();
                GameEvents.Quotes.Clear();
            }
        }

        [Fact]
        public void AReportedEventIsLoggedAtOnceAndSaysWhetherToSpeakIt()
        {
            var store = new Store();
            GameEvents.Store = () => store;
            GameEvents.Suppressed = () => true;
            GameEvents.Events.Clear();
            try
            {
                // Never held back for a replay: what a screen shows now is not a replayed event.
                Assert.True(GameEvents.Report(EventKinds.Basic, "Choose a reward."));
                Assert.Equal(new[] { "Choose a reward." }, GameEvents.Events.Lines);
                store.SetBool(EventKinds.Basic.AnnounceKey, false);
                Assert.False(GameEvents.Report(EventKinds.Basic, "Gain 50 Coin."));
                Assert.Equal("Gain 50 Coin.", GameEvents.Events.Lines[0]);
                store.SetBool(EventKinds.Basic.BufferKey, false);
                store.SetBool(EventKinds.Basic.AnnounceKey, true);
                Assert.True(GameEvents.Report(EventKinds.Basic, "Not logged."));
                Assert.Equal(2, GameEvents.Events.Lines.Count);
            }
            finally
            {
                GameEvents.Store = () => null;
                GameEvents.Suppressed = () => false;
                GameEvents.Events.Clear();
            }
        }

        [Fact]
        public void AnItemSetApartIsSilencedOrUnloggedOnItsOwnUnderItsKind()
        {
            var spoken = new List<string>();
            var store = new StringStore();
            Speech.Speak = (t, i) => spoken.Add(t);
            GameEvents.Store = () => store;
            GameEvents.Suppressed = () => false;
            GameEvents.Events.Clear();
            var kind = EventKinds.RelicTriggered;
            try
            {
                kind.SetItemAnnounces(store, "Shardsoul", false);
                kind.SetItemBuffers(store, "Imp-cicle", false);
                Assert.Equal("Shardsoul", store.GetString(kind.SilencedKey, ""));
                GameEvents.Raise(kind, "Shardsoul triggered.", EventSide.Friendly, "Shardsoul");
                GameEvents.Raise(kind, "Imp-cicle triggered.", EventSide.Friendly, "Imp-cicle");
                GameEvents.Raise(kind, "Gel Transfusor triggered.", EventSide.Friendly, "Gel");
                GameEvents.Flush();
                Assert.Equal(new[] { "Imp-cicle triggered.", "Gel Transfusor triggered." }, spoken);
                Assert.Equal(new[] { "Gel Transfusor triggered.", "Shardsoul triggered." }, GameEvents.Events.Lines);

                // The kind's own setting still rules: an item never set apart follows it.
                store.SetBool(kind.AnnounceKey, false);
                GameEvents.Raise(kind, "Gel Transfusor triggered.", EventSide.Friendly, "Gel");
                GameEvents.Flush();
                Assert.Equal(2, spoken.Count);

                kind.SetItemAnnounces(store, "Shardsoul", true);
                Assert.Equal("", store.GetString(kind.SilencedKey, ""));
            }
            finally
            {
                Speech.Speak = (t, i) => { };
                GameEvents.Store = () => null;
                GameEvents.Events.Clear();
            }
        }

        private sealed class StringStore : ISettingsStore
        {
            private readonly Dictionary<string, object> _values = new Dictionary<string, object>();
            public bool GetBool(string key, bool defaultValue) => _values.TryGetValue(key, out var v) ? (bool)v : defaultValue;
            public void SetBool(string key, bool value) => _values[key] = value;
            public int GetInt(string key, int defaultValue) => defaultValue;
            public void SetInt(string key, int value) { }
            public string GetString(string key, string defaultValue) => _values.TryGetValue(key, out var v) ? (string)v : defaultValue;
            public void SetString(string key, string value) => _values[key] = value;
        }

        [Fact]
        public void EveryKindHasALabelAndAGroupTheSettingsList()
        {
            foreach (var kind in EventKinds.All)
            {
                Assert.False(string.IsNullOrWhiteSpace(kind.Label()), kind.Key);
                Assert.False(string.IsNullOrWhiteSpace(Core.Strings.Strings.EventGroup(kind.Group.ToString())), kind.Key);
            }
        }
    }
}

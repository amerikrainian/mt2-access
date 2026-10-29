using System;
using System.Collections.Generic;
using MonsterTrainAccessibility.Contracts;

namespace MonsterTrainAccessibility.Core.Events
{
    /// <summary>Who an event concerns, for the per-kind source filter: the player's side (a friendly
    /// unit, the pyre, a card, an artifact), the enemy's, or nobody in particular (always passes).</summary>
    public enum EventSide
    {
        None,
        Friendly,
        Enemy,
    }

    /// <summary>The log an event goes to: the event history, or the monster quotes (chatter).</summary>
    public enum EventLogKind
    {
        Events,
        Quotes,
    }

    /// <summary>
    /// One kind of game event the mod reports ("Unit damage", "Combat turn"), with its settings: whether
    /// it is spoken, whether it goes to its log, and, for kinds about units, which sides it is reported
    /// for. The settings live in the host's store under "events.&lt;key&gt;.*", read live on every event.
    /// </summary>
    public sealed class EventKind
    {
        public string Key { get; }
        /// <summary>The kind's name in the settings, localized at read time.</summary>
        public Func<string> Label { get; }
        public bool AnnounceDefault { get; }
        public bool BufferDefault { get; }
        /// <summary>Whether the kind is reported per side (the units' kinds).</summary>
        public bool HasSourceFilter { get; }
        public EventLogKind Log { get; }

        public EventKind(string key, Func<string> label, bool announce = true, bool buffer = true,
            bool sourceFilter = false, EventLogKind log = EventLogKind.Events)
        {
            Key = key;
            Label = label;
            AnnounceDefault = announce;
            BufferDefault = buffer;
            HasSourceFilter = sourceFilter;
            Log = log;
        }

        public string AnnounceKey => "events." + Key + ".announce";
        public string BufferKey => "events." + Key + ".buffer";
        public string FriendlyKey => "events." + Key + ".sources.friendly";
        public string EnemyKey => "events." + Key + ".sources.enemy";

        public bool Announces(ISettingsStore store) => store == null ? AnnounceDefault : store.GetBool(AnnounceKey, AnnounceDefault);
        public bool Buffers(ISettingsStore store) => store == null ? BufferDefault : store.GetBool(BufferKey, BufferDefault);

        /// <summary>Whether an event on this side passes the kind's source filter.</summary>
        public bool Passes(ISettingsStore store, EventSide side)
        {
            if (!HasSourceFilter || store == null) return true;
            switch (side)
            {
                case EventSide.Friendly: return store.GetBool(FriendlyKey, true);
                case EventSide.Enemy: return store.GetBool(EnemyKey, true);
                default: return true;
            }
        }
    }

    /// <summary>Every event kind the mod reports, in the order the settings list them.</summary>
    public static class EventKinds
    {
        private static readonly List<EventKind> _all = new List<EventKind>();
        public static IReadOnlyList<EventKind> All => _all;

        private static EventKind K(string key, bool announce = true, bool sourceFilter = false, EventLogKind log = EventLogKind.Events)
        {
            var kind = new EventKind(key, () => Strings.Strings.Get("event_kind." + key), announce, true, sourceFilter, log);
            _all.Add(kind);
            return kind;
        }

        public static readonly EventKind Phase = K("combat.phase");
        public static readonly EventKind Turn = K("combat.turn");
        public static readonly EventKind MoonPhase = K("combat.moon_phase");
        public static readonly EventKind DeckShuffled = K("card.deck_shuffled");
        public static readonly EventKind Ember = K("resource.energy");
        public static readonly EventKind Gold = K("resource.gold");
        public static readonly EventKind ForgePoints = K("resource.forge_points");
        public static readonly EventKind PyreHp = K("pyre.hp");
        public static readonly EventKind PyreArmor = K("pyre.armor");
        public static readonly EventKind RelicTriggered = K("relic.triggered");
        public static readonly EventKind Spawned = K("character.spawned", sourceFilter: true);
        public static readonly EventKind Moved = K("character.moved", sourceFilter: true);
        public static readonly EventKind Died = K("character.died", sourceFilter: true);
        public static readonly EventKind Damage = K("character.damage", sourceFilter: true);
        public static readonly EventKind Hp = K("character.hp", sourceFilter: true);
        public static readonly EventKind Status = K("character.status", sourceFilter: true);
        public static readonly EventKind Ability = K("character.ability", sourceFilter: true);
        public static readonly EventKind ChatterSummoned = K("character.chatter.summoned", false, true, EventLogKind.Quotes);
        public static readonly EventKind ChatterAttacking = K("character.chatter.attacking", false, true, EventLogKind.Quotes);
        public static readonly EventKind ChatterSlay = K("character.chatter.slay", false, true, EventLogKind.Quotes);
        public static readonly EventKind ChatterIdle = K("character.chatter.idle", false, true, EventLogKind.Quotes);
        public static readonly EventKind ChatterTrigger = K("character.chatter.trigger", false, true, EventLogKind.Quotes);
    }
}

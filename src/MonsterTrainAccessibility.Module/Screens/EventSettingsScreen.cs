using System;
using System.Collections.Generic;
using MonsterTrainAccessibility.Contracts;
using MonsterTrainAccessibility.Core.Events;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.Strings;
using MonsterTrainAccessibility.Core.UI;
using MonsterTrainAccessibility.Module.UI;
using Screen = MonsterTrainAccessibility.Core.Screens.Screen;

namespace MonsterTrainAccessibility.Module.Screens
{
    /// <summary>
    /// The mod menu's Event announcements: the event kinds by group (Combat, Units, Resources, Chatter,
    /// Other), each group opening its kinds (<see cref="EventGroupScreen"/>), each kind its settings
    /// (<see cref="EventKindScreen"/>). The settings are the kinds' own, in the host's store, read live
    /// by every event.
    /// </summary>
    public sealed class EventSettingsScreen : Screen
    {
        public override string Key => "mod.events";
        public override bool IsActive() => false; // only ever a child of the mod menu

        public override void Build(GraphBuilder b)
        {
            b.PushContext(Strings.EventSettings, Strings.RoleList);
            foreach (EventGroup group in Enum.GetValues(typeof(EventGroup)))
            {
                var g = group;
                bool any = false;
                foreach (var kind in EventKinds.All)
                    if (kind.Group == g) any = true;
                if (!any) continue;
                b.AddItem(ControlId.Structural("events:" + g), GameNodes.Button(() => Strings.EventGroup(g.ToString()),
                    () => PushChild(new EventGroupScreen(g))));
            }
            b.PopContext();
        }

        public override IEnumerable<ElementAction> GetActions()
        {
            yield return new ElementAction(ActionIds.Back, Strings.Get("bind.ui.back"), _ => ParentScreen?.RemoveChild(this));
        }
    }

    /// <summary>One group's event kinds, in the order the mod declares them; Enter opens a kind's
    /// settings.</summary>
    public sealed class EventGroupScreen : Screen
    {
        private readonly EventGroup _group;

        public EventGroupScreen(EventGroup group) => _group = group;

        public override string Key => "mod.eventgroup";
        public override bool IsActive() => false; // only ever a child of Event announcements

        public override void Build(GraphBuilder b)
        {
            b.PushContext(Strings.EventGroup(_group.ToString()), Strings.RoleList);
            foreach (var kind in EventKinds.All)
            {
                if (kind.Group != _group) continue;
                var k = kind;
                b.AddItem(ControlId.Structural("eventkind:" + k.Key), GameNodes.Button(k.Label, () => PushChild(new EventKindScreen(k))));
            }
            b.PopContext();
        }

        public override IEnumerable<ElementAction> GetActions()
        {
            yield return new ElementAction(ActionIds.Back, Strings.Get("bind.ui.back"), _ => ParentScreen?.RemoveChild(this));
        }
    }

    /// <summary>One event kind's settings, each a toggle: Announce, Add to buffer, and for the kinds
    /// about units, Friendly units and Enemy units (the sides it is reported for).</summary>
    public sealed class EventKindScreen : Screen
    {
        private readonly EventKind _kind;

        public EventKindScreen(EventKind kind) => _kind = kind;

        public override string Key => "mod.eventkind";
        public override bool IsActive() => false; // only ever a child of an event group

        private static ISettingsStore Store => GameEvents.Store();

        public override void Build(GraphBuilder b)
        {
            var k = _kind;
            b.PushContext(k.Label(), Strings.RoleList);
            Setting(b, "announce", () => Strings.EventAnnounce, k.AnnounceKey, k.AnnounceDefault);
            Setting(b, "buffer", () => Strings.EventBuffer, k.BufferKey, k.BufferDefault);
            if (k.HasSourceFilter)
            {
                Setting(b, "friendly", () => Strings.EventFriendly, k.FriendlyKey, true);
                Setting(b, "enemy", () => Strings.EventEnemy, k.EnemyKey, true);
            }
            b.PopContext();
        }

        private static void Setting(GraphBuilder b, string id, Func<string> label, string key, bool fallback)
        {
            b.AddItem(ControlId.Structural("eventsetting:" + id), ModSettingsScreen.Toggle(label,
                () => Store != null ? Store.GetBool(key, fallback) : fallback,
                v => Store?.SetBool(key, v)));
        }

        public override IEnumerable<ElementAction> GetActions()
        {
            yield return new ElementAction(ActionIds.Back, Strings.Get("bind.ui.back"), _ => ParentScreen?.RemoveChild(this));
        }
    }
}

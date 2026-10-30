using System;
using System.Collections.Generic;
using MonsterTrainAccessibility.Contracts;
using MonsterTrainAccessibility.Core.Events;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.Strings;
using MonsterTrainAccessibility.Core.UI;
using MonsterTrainAccessibility.Module.Interop;
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

    /// <summary>
    /// Every relic the game knows, for Artifact triggered: each set apart from the kind's own settings
    /// (<see cref="RelicEventScreen"/>). In groups, each a Tab stop, sorted by name: the run's own first,
    /// then the game's (and the mods') artifacts, mutators, trials (what a trial does to the enemies)
    /// and souls, read from the game's registries when the page is built, so a mod's relics are there
    /// without a word of ours. Only what the player has met is listed, as the Logbook lists it: an
    /// artifact, mutator or trial once discovered (an artifact also when the one it stands in for is),
    /// a soul once unlocked; the run's own always. A relic reads by its name, then any setting turned
    /// off ("Shardsoul, Announce: off"). Relics without a name (pyre hearts, covenants) are never
    /// announced, so never listed.
    /// </summary>
    public sealed class RelicEventsScreen : Screen
    {
        private readonly EventKind _kind;

        public RelicEventsScreen(EventKind kind) => _kind = kind;

        public override string Key => "mod.relicevents";
        public override bool IsActive() => false; // only ever a child of Artifact triggered

        public override void Build(GraphBuilder b)
        {
            var managers = AllGameManagers.Instance;
            var data = managers != null ? managers.GetAllGameData() : null;
            if (data == null) return;
            var save = managers.GetSaveManager();
            if (save != null && save.HasRun())
            {
                var run = new List<KeyValuePair<string, string>>();
                foreach (var relic in save.GetAllRelics())
                    if (relic != null) run.Add(new KeyValuePair<string, string>(relic.GetAssetName(), relic.GetName()));
                Group(b, "run", () => Strings.RelicsThisRun, run);
            }
            var meta = save != null ? save.GetMetagameSave() : null;
            if (meta == null) return;
            Group(b, "artifacts", () => GameText.Term("Compendium_Section_Blessings"), Entries(data.GetAllCollectableRelicData(), r => ArtifactDiscovered(save, r)));
            Group(b, "mutators", () => GameText.Term("ScreenRunSetup_Mutators"), Entries(data.GetAllMutatorData(), meta.HasDiscoveredRelic));
            Group(b, "trials", () => Strings.RelicsTrials, Entries(data.GetAllSinsDatas(), meta.HasDiscoveredRelic));
            Group(b, "souls", () => GameText.Term("Hud_Souls"), Entries(data.GetAllSoulDatas(), r => meta.HasUnlockedSoul(r.GetID())));
        }

        // As the Logbook has it: discovered, or the artifact it stands in for is.
        private static bool ArtifactDiscovered(SaveManager save, CollectableRelicData relic)
        {
            var meta = save.GetMetagameSave();
            if (meta.HasDiscoveredRelic(relic)) return true;
            var balance = save.GetBalanceData();
            if (balance == null) return false;
            foreach (var substitution in balance.GetCollectableRelicDataSubstitutions())
                if (substitution.RelicToSubstituteWith == relic && meta.HasDiscoveredRelic(substitution.RelicToSubstitute)) return true;
            return false;
        }

        private static List<KeyValuePair<string, string>> Entries<T>(IReadOnlyList<T> relics, Func<T, bool> met) where T : RelicData
        {
            var entries = new List<KeyValuePair<string, string>>();
            if (relics == null) return entries;
            foreach (var relic in relics)
            {
                if (relic == null || !met(relic)) continue;
                string name;
                try { name = relic.GetName(); }
                catch (Exception e)
                {
                    Core.CoreLog.Warning("RelicEventsScreen: " + relic.name + "'s name: " + e.Message);
                    continue;
                }
                entries.Add(new KeyValuePair<string, string>(relic.name, name));
            }
            return entries;
        }

        // One group: its relics with a name, each once, by name.
        private void Group(GraphBuilder b, string key, Func<string> label, List<KeyValuePair<string, string>> relics)
        {
            var seen = new HashSet<string>();
            var shown = new List<KeyValuePair<string, string>>();
            foreach (var relic in relics)
                if (!string.IsNullOrEmpty(relic.Key) && !string.IsNullOrWhiteSpace(relic.Value)
                    && !relic.Value.StartsWith("KEY>>", StringComparison.Ordinal) && seen.Add(relic.Key))
                    shown.Add(relic);
            if (shown.Count == 0) return;
            shown.Sort((x, y) => string.Compare(x.Value, y.Value, StringComparison.CurrentCultureIgnoreCase));
            b.BeginStop("relics:" + key);
            b.PushContext(label(), Strings.RoleList);
            var kind = _kind;
            foreach (var relic in shown)
            {
                string asset = relic.Key, name = relic.Value.Trim();
                b.AddItem(ControlId.Structural("relic:" + key + ":" + asset), GameNodes.Button(() => Row(kind, asset, name),
                    () => PushChild(new RelicEventScreen(kind, asset, name))));
            }
            b.PopContext();
        }

        // "Shardsoul", or "Shardsoul, Announce: off, Add to buffer: off": only what is set apart.
        private static string Row(EventKind kind, string asset, string name)
        {
            var store = GameEvents.Store();
            var parts = new List<string> { name };
            if (!kind.ItemAnnounces(store, asset)) parts.Add(Strings.HelpRow(Strings.EventAnnounce, Strings.StateOff));
            if (!kind.ItemBuffers(store, asset)) parts.Add(Strings.HelpRow(Strings.EventBuffer, Strings.StateOff));
            return string.Join(", ", parts);
        }

        public override IEnumerable<ElementAction> GetActions()
        {
            yield return new ElementAction(ActionIds.Back, Strings.Get("bind.ui.back"), _ => ParentScreen?.RemoveChild(this));
        }
    }

    /// <summary>One relic's settings for Artifact triggered: Announce and Add to buffer, each on unless
    /// turned off here, and each still under the kind's own.</summary>
    public sealed class RelicEventScreen : Screen
    {
        private readonly EventKind _kind;
        private readonly string _asset;
        private readonly string _name;

        public RelicEventScreen(EventKind kind, string asset, string name)
        {
            _kind = kind;
            _asset = asset;
            _name = name;
        }

        public override string Key => "mod.relicevent";
        public override bool IsActive() => false; // only ever a child of the relic list

        public override void Build(GraphBuilder b)
        {
            var kind = _kind;
            string asset = _asset;
            b.PushContext(_name, Strings.RoleList);
            b.AddItem(ControlId.Structural("relicsetting:announce"), ModSettingsScreen.Toggle(() => Strings.EventAnnounce,
                () => kind.ItemAnnounces(GameEvents.Store(), asset), v => kind.SetItemAnnounces(GameEvents.Store(), asset, v)));
            b.AddItem(ControlId.Structural("relicsetting:buffer"), ModSettingsScreen.Toggle(() => Strings.EventBuffer,
                () => kind.ItemBuffers(GameEvents.Store(), asset), v => kind.SetItemBuffers(GameEvents.Store(), asset, v)));
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
            // Artifact triggered: each artifact set apart, the game's "Artifacts" opening them all.
            if (k == EventKinds.RelicTriggered)
                b.AddItem(ControlId.Structural("eventsetting:items"), GameNodes.Button(() => GameText.Term("Compendium_Section_Blessings"),
                    () => PushChild(new RelicEventsScreen(k))));
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

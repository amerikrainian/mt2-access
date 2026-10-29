using System;
using System.Collections.Generic;
using System.Text;

namespace MonsterTrainAccessibility.Core.Strings
{
    /// <summary>
    /// Central table for text the MOD itself authors and speaks (never game content, which is read live
    /// and already localized). Every authored word lives in <see cref="Defaults"/> as a key and its
    /// English value; typed accessors read through the loaded translation, so a lang/&lt;language&gt;.txt
    /// file overrides any value at speak time. Word order lives in "{0}"-style templates, never in code.
    /// Values are spoken by a screen reader: terse, lowercase unless shown otherwise, no decorative
    /// punctuation. Keys follow the wotr-access locale vocabulary (role.*, state.*, nav.*, screen.*).
    /// </summary>
    public static class Strings
    {
        private static KeyValuePair<string, string> D(string key, string value)
            => new KeyValuePair<string, string>(key, value);

        /// <summary>Every authored string, in template order: the key a translation file addresses and
        /// its English default.</summary>
        internal static readonly KeyValuePair<string, string>[] Defaults =
        {
            // Spoken once when the mod finishes loading; {0} = the mod version.
            D("app.loaded", "Monster Train Accessibility {0} loaded"),
            // Spoken at launch instead when the feature module fails to load (the mod is then dead).
            D("app.module_failed", "Monster Train Accessibility features failed to load"),
            // Follows the loaded line when the newest release outranks the running build; {0} = that
            // newer version. Up to date (or ahead, a dev build) stays silent.
            D("app.update_available", "update {0} available"),
            // Spoken when the focus-mode toggle engages / releases our keyboard navigation.
            D("app.focus_on", "navigation on"),
            D("app.focus_off", "navigation off"),

            // Control role words, spoken after the label ("Continue, button").
            D("role.button", "button"),
            D("role.toggle", "toggle"),
            D("role.slider", "slider"),
            D("role.radio button", "radio button"),
            D("role.combo box", "combo box"),
            D("role.tab", "tab"),
            D("role.key binding", "key binding"),
            D("role.item", "item"),
            D("role.heading", "heading"),
            D("role.list", "list"),
            D("role.table", "table"),
            D("role.link", "link"),
            D("role.edit", "edit"),

            // Control states.
            D("state.disabled", "disabled"),
            D("state.selected", "selected"),
            D("state.on", "on"),
            D("state.off", "off"),
            D("state.expanded", "expanded"),
            D("state.collapsed", "collapsed"),
            // A challenge the player has beaten (the game marks it with an icon only).
            D("state.completed", "completed"),
            D("value.blank", "blank"),
            D("value.space", "space"),
            D("edit.begin", "editing"),
            D("edit.selected", "{0} selected"),
            D("edit.unselected", "{0} unselected"),
            // The mod's own number entry: the decimal point as typed, a character erased with Backspace.
            D("edit.point", "point"),
            D("edit.deleted", "{0} deleted"),

            // Navigation feedback. {0} = index, {1} = count.
            D("nav.position", "{0} of {1}"),
            D("nav.no_tooltip", "no description"),
            D("nav.no_details", "no details"),
            // Buffer review (Ctrl plus arrows): the buffer names, the "name: line" readout on switching,
            // and the words when every buffer is empty.
            D("buffer.ui", "control"),
            D("buffer.events", "events"),
            D("buffer.quotes", "monster quotes"),
            D("buffer.none", "no buffer lines"),
            // {0} = the buffer's name, {1} = its current line.
            D("buffer.line", "{0}: {1}"),
            D("drag.no_target", "nothing to drag"),
            D("delete.no_target", "nothing to remove"),
            D("nav.minimum", "minimum"),
            D("nav.maximum", "maximum"),

            // Type-ahead search. {0} = the typed text.
            D("search.no_match", "no match for {0}"),
            D("search.cleared", "search cleared"),

            // Input action labels (for a key-help reader). Short imperative phrases.
            D("bind.ui.up", "Navigate up"),
            D("bind.ui.down", "Navigate down"),
            D("bind.ui.left", "Navigate left"),
            D("bind.ui.right", "Navigate right"),
            D("bind.ui.next", "Next control group"),
            D("bind.ui.prev", "Previous control group"),
            D("bind.ui.activate", "Activate"),
            D("bind.ui.secondary", "Secondary action"),
            D("bind.ui.back", "Back"),
            D("bind.ui.home", "Jump to first"),
            D("bind.ui.end", "Jump to last"),
            D("bind.ui.tooltip", "Read description"),
            D("bind.ui.regionPrev", "Previous section"),
            D("bind.ui.regionNext", "Next section"),
            D("bind.ui.readFocus", "Read current control"),
            // Buffer review (Ctrl plus arrows): switch between the review lists, step through one.
            D("bind.buffer.next", "Next buffer"),
            D("bind.buffer.prev", "Previous buffer"),
            D("bind.buffer.line.next", "Next buffer line"),
            D("bind.buffer.line.prev", "Previous buffer line"),
            D("bind.mod.focus", "Toggle navigation"),

            // The role word for a modal dialog context.
            D("role.dialog", "dialog"),
            // The game's Menu: its Discord link shows only the logo.
            D("pause.discord", "Discord"),
            // The words for the icons game text draws inline (<sprite name="Ember">), where the game has
            // no name of its own for the icon (status effect, trigger and card effect icons read by the
            // game's own names).
            D("icon.attack", "attack"),
            D("icon.armor", "armor"),
            D("icon.ember", "ember"),
            D("icon.health", "health"),
            D("icon.pyre", "pyre health"),
            D("icon.capacity", "capacity"),
            D("icon.dragons_hoard", "Dragon's Hoard"),
            D("icon.forge", "forge points"),
            D("icon.x_cost", "X cost"),
            D("icon.deployment_ember", "deployment ember"),
            D("icon.ability", "ability"),
            D("icon.charged_echo", "charged echo"),
            D("icon.charged_echo_slot", "charged echo slot"),
            // A card's numbers, which its face shows beside icons; {0} = the number as drawn.
            D("card.cost", "cost {0}"),
            D("card.attack", "{0} attack"),
            D("card.health", "{0} health"),
            D("card.size", "size {0}"),
            // What an upgrade adds to a card's text, which the game draws in its upgrade colour; {0} = that
            // text ("new: Ability: Savior.").
            D("card.upgraded", "new: {0}"),
            // The soul attached to a card in a Soul Savior run ({0} = its name).
            D("card.soul_attached", "{0} attached"),
            // The Logbook. An enemy's numbers beside their icons ({0} = the number), its variant buttons
            // (drawn as bare numbers), its artist credit ({0} = the name).
            D("enemy.variant", "Variant {0}"),
            D("enemy.attack", "{0} attack"),
            D("enemy.heal", "{0} healing"),
            D("enemy.health", "{0} health"),
            D("enemy.armor", "{0} armor"),
            D("enemy.artist", "Artist: {0}"),
            // A search field's clear button, an icon.
            D("search.clear", "Clear search"),
            // A champion upgrade path's level nodes, drawn as numerals; {0} = the level.
            D("upgrade.level", "Upgrade level {0}"),
            // A mastery card frame, which the game shows as a picture; {0} = its kind below.
            D("frame.name", "{0} frame"),
            D("frame.default", "Default"),
            D("frame.metallic", "Metallic"),
            D("frame.heaven_throne", "Heaven's Throne"),
            D("frame.prismatic", "Prismatic"),
            // A champion whose portrait the game rings with the mastery frame.
            D("state.mastered", "mastered"),
            // A pyre heart's two stats, which the game shows as icons with numbers; {0} = the number.
            D("pyre.attack", "Pyre attack {0}"),
            D("pyre.health", "Pyre health {0}"),

            // The run's map. Said before a stop on arriving where the route forks ("choice, Light Forge"):
            // the branches after a battle, a ring's nodes, a fork the route preview reaches.
            D("map.choice", "choice"),
            // The route preview (Ctrl+arrows on the map) at its ends.
            D("map.no_forward", "No forward map node."),
            D("map.no_backward", "No previous map node."),
            // A map node already visited, in the ring the train is in.
            D("state.visited", "visited"),
            // Soul Savior's region choice: a region already cleared, and a reward a cleared region has
            // improved (the game draws both only as images).
            D("state.defeated", "defeated"),
            D("state.upgraded", "upgraded"),
            // The Dragon's Hoard's loot levels, which the game shows as numbers; {0} = the level. "current"
            // marks the level the hoard is at, which the game shows only by a highlight.
            D("hoard.level", "Level {0}"),
            // An amount of a currency, as a reward's short form: {0} = the amount, {1} = the game's word
            // for the currency ("75 Coins").
            D("reward.amount", "{0} {1}"),
            D("state.current", "current"),
            // The run's HUD, which Tab opens as a list anywhere in a run, and its groups the game
            // draws without a heading.
            D("hud.title", "Run info"),
            D("hud.train_stats", "Train stats"),
            D("hud.artifacts", "Artifacts"),
            D("hud.enemy_artifacts", "Enemy artifacts"),
            D("bind.map.forward", "Preview next stop"),
            D("bind.map.back", "Preview previous stop"),
            D("bind.map.prev", "Previous route choice"),
            D("bind.map.next", "Next route choice"),
            D("bind.run.hud", "Run info"),

            D("screen.mod_menu", "Monster Train Accessibility"),
            D("mod.settings", "Settings"),
            // The mod menu's list of every key the mod binds, whatever the screen.
            D("mod.key_help", "All keys"),
            D("mod.close", "Close"),
            D("mod.speak_positions", "Speak list positions"),
            D("mod.focus_on_launch", "Keyboard navigation on at launch"),
            // {0} = the key category, one of the three names below.
            D("mod.key_category", "{0} keys"),
            // The key categories of the "All keys" list: live everywhere, on the focused screen, in a run,
            // on the run's map.
            D("mod.category.global", "Global"),
            D("mod.category.ui", "UI"),
            D("mod.category.game", "Game"),
            D("mod.category.map", "Map"),
            D("bind.mod.menu", "Mod menu"),
            // The context-sensitive key help (F1): the keys that do something on this screen, on this
            // control. Enter on a row runs it.
            D("bind.mod.help", "Keys here"),
            D("screen.help", "Keys here"),
            D("help.none", "no keys here"),
            // {0} = what the key does, {1} = the keys bound to it.
            D("help.row", "{0}: {1}"),
            // A jump key's row, by what it lands on here; {0} = that stop's name ("offers", "party").
            D("help.jump_to", "Jump to {0}"),

            // Battle events, spoken as they happen (queued) and kept in the events buffer, newest first.
            D("event.deployment", "Deployment."),
            // {0} = the turn.
            D("event.turn", "Turn {0}."),
            // {0} = the turn, {1} = the battle's turns.
            D("event.turn_of", "Turn {0}/{1}."),
            D("event.player_action", "Player action."),
            D("event.room_combat", "Combat."),
            D("event.enemy_turn", "Enemy turn."),
            D("event.combat_ended", "Combat ended."),
            D("event.enemies_cleared", "Enemies cleared."),
            D("event.deck_shuffled", "Deck shuffled."),
            // {0} = the ember the player has now.
            D("event.ember", "Ember {0}."),
            // {0} = the forge points the player has now.
            D("event.forge_points", "Forge {0}."),
            // {0} = the game's word for the currency ("Coin"), {1} = the amount gained / lost.
            D("event.gold_gained", "{0} +{1}."),
            D("event.gold_lost", "{0} -{1}."),
            // {0} = the health the pyre lost / gained.
            D("event.pyre_damaged", "Pyre -{0} health."),
            D("event.pyre_healed", "Pyre +{0} health."),
            // {0} = the pyre's armor now.
            D("event.pyre_armor", "Pyre armor {0}."),
            // {0} = the unit, {1} = its floor's number.
            D("event.spawned", "{0} spawned, floor {1}."),
            D("event.moved", "{0} moved to floor {1}."),
            // {0} = the unit.
            D("event.died", "{0} died."),
            // {0} = the unit, {1} = the amount.
            D("event.damaged", "{0} took {1} damage."),
            D("event.damaged_one", "{0} took damage."),
            D("event.healed", "{0} healed {1}."),
            D("event.healed_one", "{0} healed."),
            // {0} = the unit, {1} = the status, {2} = the change in stacks.
            D("event.status_increased", "{0} {1} +{2}."),
            D("event.status_decreased", "{0} {1} -{2}."),
            // {0} = the unit.
            D("event.ability_ready", "{0} ability ready."),
            D("event.ability_unavailable", "{0} ability unavailable."),
            // {0} = the artifact.
            D("event.relic_triggered", "{0} triggered."),
            // A unit's quote: {0} = the unit, {1} = why it spoke, {2} = the quote.
            D("event.chatter_reason", "{0} {1}, \"{2}\""),
            // {0} = the unit, {1} = the quote.
            D("event.chatter", "{0} \"{1}\""),
            D("event.chatter.summoned", "summoned"),
            D("event.chatter.attacking", "attacking"),
            D("event.chatter.slay", "slay"),
            D("event.chatter.idle", "idle"),

            // A run's end: a battle of the run by its place ({0} = 1 for the first), a clan's level ({0}).
            D("gameover.battle", "Battle {0}"),
            D("gameover.level", "level {0}"),

            // A community challenge: {0} = who made it; {0} = how many have played it; a run of it under way.
            D("challenge.creator", "by {0}"),
            D("challenge.entries", "{0} entries"),
            D("challenge.in_progress", "run in progress"),

            // A run's marks, drawn as icons (a leaderboard entry, the run history): won; no restarts or undos.
            D("run.victory", "Victory"),
            D("run.clean", "clean run"),
            // A run the player has starred in the run history; the star just taken off one.
            D("run.favorite", "favorited"),
            D("run.unfavorite", "unfavorited"),

            // A shop's good or service the player's coin does not cover.
            D("shop.unaffordable", "can't afford"),

            // The battle. A unit: {0} = its size / attack.
            D("battle.size", "size {0}"),
            D("battle.attack", "{0} attack"),
            // {0} = health now, {1} = its maximum.
            D("battle.hp", "{0} of {1} health"),
            // {0} = armor, {1} = health now, {2} = its maximum.
            D("battle.armor_hp", "{0} armor, {1} of {2} health"),
            // A unit's ability and where it stands: {0} = the ability, {1} = its state.
            D("battle.ability", "{0}, {1}"),
            D("battle.ability.ready", "ready"),
            // {0} = turns of cooldown left.
            D("battle.ability.cooldown", "{0} turns"),
            D("battle.ability.deployment", "deployment phase"),
            D("battle.ability.hand_full", "hand full"),
            D("battle.ability.disabled", "disabled"),
            D("battle.ability.unavailable", "unavailable"),
            // A boss's next action: {0} = the floor it targets / moves to.
            D("battle.boss_targets", "targets floor {0}"),
            D("battle.boss_moves", "moves to floor {0}"),
            // A floor: {0} = its number (1 at the bottom).
            D("battle.floor", "Floor {0}"),
            // Its room for the player's units: {0} = space free, {1} = the floor's capacity.
            D("battle.capacity", "{0}/{1}"),
            // {0} = the capacity line, {1} = echoes, {2} = the most it holds.
            D("battle.capacity_echo", "{0}, {1}/{2}"),
            // Shift+B, one floor of the list: {0} = the floor, {1} = its capacity line.
            D("battle.floor_capacity", "{0} {1}"),
            D("battle.floor_empty", "No creatures"),
            D("battle.hand", "Hand"),
            D("battle.hand_empty", "No cards"),
            D("battle.top_panel", "Top panel"),
            // Targeting: a card that takes the whole floor; a place for a unit ({0} = 1 at the front),
            // with the unit already standing there ({0} = the place, {1} = that unit).
            D("battle.selected_floor", "Selected floor"),
            D("battle.spawn_point", "Spawn point {0}"),
            D("battle.spawn_point_occupied", "{0}, {1}"),
            // What the coming combat does to a unit (I, Ctrl+I, Ctrl+Shift+I).
            D("battle.outcome.eaten", "eaten"),
            D("battle.outcome.burns_out", "burns out"),
            D("battle.outcome.dies", "dies"),
            D("battle.outcome.despawns", "despawns"),
            // {0} = health / armor after the combat.
            D("battle.outcome.health", "{0} health"),
            D("battle.outcome.armor", "{0} armor"),
            D("battle.outcome.no_change", "no change"),
            D("battle.outcome.no_changes", "no changes"),
            // Ctrl+I: {0} = the unit, {1} = its outcome. Ctrl+Shift+I: {0} = the floor, {1} = its outcomes.
            D("battle.outcome.unit", "{0}, {1}"),
            D("battle.outcome.floor", "{0}: {1}"),
            // The read keys: {0} = ember / forge points; the pyre's health and its maximum.
            D("battle.read.ember", "{0} ember"),
            D("battle.read.forge_points", "{0} forge points"),
            D("battle.read.pyre", "{0}/{1} hp"),
            // {0} = the amount, {1} = the game's word for the currency.
            D("battle.read.gold", "{0} {1}"),
            // The keys' names in the key help.
            D("bind.battle.ember", "Read ember"),
            D("bind.battle.forge_points", "Read forge points"),
            D("bind.run.gold", "Read coin"),
            D("bind.run.pyre", "Read pyre health"),
            D("bind.battle.moon", "Read moon phase"),
            D("bind.battle.outcome", "Read unit outcome"),
            D("bind.battle.floor_outcomes", "Read floor outcomes"),
            D("bind.battle.all_outcomes", "Read all floor outcomes"),
            D("bind.battle.capacity", "Read floor capacity and charged echoes"),
            D("bind.battle.all_capacity", "Read all floor capacity and charged echoes"),
            D("bind.battle.hand", "Jump to hand"),

            // The event settings (mod menu): the list, its toggles, and each kind's name.
            D("event_settings", "Event announcements"),
            D("event_settings.announce", "Announce"),
            D("event_settings.buffer", "Add to buffer"),
            D("event_settings.friendly", "Friendly units"),
            D("event_settings.enemy", "Enemy units"),
            D("event_kind.combat.phase", "Combat phases"),
            D("event_kind.combat.turn", "Combat turn"),
            D("event_kind.combat.moon_phase", "Moon phase changes"),
            D("event_kind.card.deck_shuffled", "Deck shuffled"),
            D("event_kind.resource.energy", "Ember changes"),
            D("event_kind.resource.gold", "Coin changes"),
            D("event_kind.resource.forge_points", "Forge point changes"),
            D("event_kind.pyre.hp", "Pyre health changes"),
            D("event_kind.pyre.armor", "Pyre armor"),
            D("event_kind.relic.triggered", "Artifact triggered"),
            D("event_kind.character.spawned", "Unit spawned"),
            D("event_kind.character.moved", "Unit movement"),
            D("event_kind.character.died", "Unit death"),
            D("event_kind.character.damage", "Unit damage"),
            D("event_kind.character.hp", "Unit health changes"),
            D("event_kind.character.status", "Unit status changes"),
            D("event_kind.character.ability", "Unit ability availability"),
            D("event_kind.character.chatter.summoned", "Unit summoned chatter"),
            D("event_kind.character.chatter.attacking", "Unit attacking chatter"),
            D("event_kind.character.chatter.slay", "Unit slay chatter"),
            D("event_kind.character.chatter.idle", "Unit idle chatter"),
            D("event_kind.character.chatter.trigger", "Unit trigger chatter"),
            D("event_kind.merchant.chatter", "Merchant chatter"),
        };

        private static readonly Dictionary<string, string> _defaults = BuildDefaults();
        private static Dictionary<string, string> _overrides = new Dictionary<string, string>();

        private static Dictionary<string, string> BuildDefaults()
        {
            var d = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var kv in Defaults) d[kv.Key] = kv.Value;
            return d;
        }

        /// <summary>The value for a key: the loaded translation, else the English default, else the
        /// key itself (a missing key is then audible rather than silent).</summary>
        public static string Get(string key)
        {
            if (_overrides.TryGetValue(key, out var o)) return o;
            if (_defaults.TryGetValue(key, out var v)) return v;
            CoreLog.Warning("Strings: missing key " + key);
            return key;
        }

        /// <summary>Whether a key exists in the defaults (for optional lookups like role words).</summary>
        public static bool Has(string key) => _defaults.ContainsKey(key) || _overrides.ContainsKey(key);

        /// <summary>The value for a key with "{0}"-style slots filled.</summary>
        public static string F(string key, params object[] args)
        {
            string template = Get(key);
            try { return string.Format(template, args); }
            catch (FormatException e)
            {
                CoreLog.Warning("Strings: " + key + " does not fit its arguments: " + e.Message);
                return template;
            }
        }

        /// <summary>Install a translation, replacing any previous one whole (a language switch never
        /// blends two files): "key=value" lines split at the first '=' ("#" comments and blanks
        /// ignored, a written-out "\n" a line break); missing keys fall back to English. Pass null to
        /// return to the defaults. What the file got wrong is set aside, never applied, and comes
        /// back in the report for the caller to log: a line without a key, a key this table does not
        /// define (a typo, a file for a newer mod), an empty value (it would silence information), a
        /// value with a slot its English has not ("{2}" where the code fills two: the line would be
        /// spoken as the raw template).</summary>
        public static TranslationReport LoadTranslation(IEnumerable<string> lines)
        {
            var report = new TranslationReport();
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            if (lines != null)
            {
                int n = 0;
                foreach (var raw in lines)
                {
                    n++;
                    if (raw == null) continue;
                    string line = raw.Trim().TrimStart('\uFEFF');
                    if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal)) continue;
                    int eq = line.IndexOf('=');
                    if (eq <= 0) { report.Malformed.Add(n); continue; }
                    string key = line.Substring(0, eq).Trim();
                    string value = line.Substring(eq + 1).Trim().Replace("\\n", "\n");
                    if (!_defaults.TryGetValue(key, out var english)) { report.UnknownKeys.Add(key); continue; }
                    if (value.Length == 0) { report.EmptyKeys.Add(key); continue; }
                    if (!SlotsFit(value, english)) { report.BadSlots.Add(key); continue; }
                    map[key] = value;
                }
            }
            _overrides = map;
            report.Applied = map.Count;
            return report;
        }

        // Whether a translated template formats with the arguments its English takes: every "{n}" of
        // it one the English has, and the braces well formed.
        public static bool SlotsFit(string value, string english)
        {
            int max = -1;
            foreach (int slot in Slots(english)) if (slot > max) max = slot;
            try { string.Format(value, new object[Math.Max(max + 1, 0)]); }
            catch (FormatException) { return false; }
            return true;
        }

        /// <summary>The "{n}" slots a template uses, for the translation checks.</summary>
        public static HashSet<int> Slots(string template)
        {
            var slots = new HashSet<int>();
            if (template == null) return slots;
            for (int i = 0; i < template.Length - 2; i++)
            {
                if (template[i] != '{' || !char.IsDigit(template[i + 1])) continue;
                int j = i + 1, slot = 0;
                while (j < template.Length && char.IsDigit(template[j])) slot = slot * 10 + (template[j++] - '0');
                if (j < template.Length && (template[j] == '}' || template[j] == ':' || template[j] == ',')) slots.Add(slot);
            }
            return slots;
        }

        /// <summary>Every key of the table, in template order (the translation tests walk it).</summary>
        public static IEnumerable<string> Keys
        {
            get { foreach (var kv in Defaults) yield return kv.Key; }
        }

        /// <summary>A key's English default, or null.</summary>
        public static string Default(string key) => _defaults.TryGetValue(key, out var v) ? v : null;

        private const string TemplateHeader =
            "# Monster Train Accessibility: the words the mod itself speaks (the game's own text is the game's).\n" +
            "# This file is the template, generated from the mod's strings table: copy it to\n" +
            "# <language>.txt, named for the game's language code (de-DE, es, fr-FR, ja, ko, pt-BR, ru,\n" +
            "# zh-CN, zh-HK), and translate the values. key=value, one per line; keep every {0} slot (their\n" +
            "# order is free); a missing key is spoken in English. The mod follows the game's language.\n";

        /// <summary>The translator template (lang/en.txt): every key with its English default, one per
        /// line, under a header that tells a translator what to do with it.</summary>
        public static string DumpTemplate()
        {
            var sb = new StringBuilder();
            sb.Append(TemplateHeader);
            foreach (var kv in Defaults)
                sb.Append(kv.Key).Append('=').Append(kv.Value.Replace("\n", "\\n")).Append('\n');
            return sb.ToString();
        }

        // ---- typed accessors ----

        public static string ModLoaded(string version) => F("app.loaded", version);
        public static string ModuleFailed => Get("app.module_failed");
        public static string UpdateAvailable(string version) => F("app.update_available", version);
        public static string FocusOn => Get("app.focus_on");
        public static string FocusOff => Get("app.focus_off");

        /// <summary>The role word for a control type key ("button" -> "button"); null when none.</summary>
        public static string Role(string word) => Has("role." + word) ? Get("role." + word) : null;
        public static string RoleTable => Get("role.table");
        public static string RoleList => Get("role.list");
        public static string RoleHeading => Get("role.heading");

        public static string StateDisabled => Get("state.disabled");
        public static string StateSelected => Get("state.selected");
        public static string StateOn => Get("state.on");
        public static string StateOff => Get("state.off");
        public static string ExpandedState(bool expanded) => Get(expanded ? "state.expanded" : "state.collapsed");
        public static string ValueBlank => Get("value.blank");
        public static string ValueSpace => Get("value.space");
        public static string EditBegin => Get("edit.begin");
        public static string EditSelected(string text) => F("edit.selected", text);
        public static string EditUnselected(string text) => F("edit.unselected", text);
        public static string EditPoint => Get("edit.point");
        public static string EditDeleted(string text) => F("edit.deleted", text);

        public static string Position(int index, int count) => F("nav.position", index, count);
        public static string NoTooltip => Get("nav.no_tooltip");
        public static string NoDetails => Get("nav.no_details");
        public static string BufferUi => Get("buffer.ui");
        public static string BufferEvents => Get("buffer.events");
        public static string BufferQuotes => Get("buffer.quotes");
        public static string BufferNone => Get("buffer.none");
        public static string BufferLine(string name, string line) => F("buffer.line", name, line);
        public static string NoDragTarget => Get("drag.no_target");
        public static string NoDeleteTarget => Get("delete.no_target");
        public static string Minimum => Get("nav.minimum");
        public static string Maximum => Get("nav.maximum");

        public static string SearchNoMatch(string text) => F("search.no_match", text);
        public static string SearchCleared => Get("search.cleared");

        /// <summary>The spoken label of an input action ("bind.ui.down" -> "Navigate down"); the
        /// registration label when no entry exists.</summary>
        public static string BindLabel(string actionKey, string fallback)
            => Has("bind." + actionKey) ? Get("bind." + actionKey) : fallback;

        public static string RoleDialog => Get("role.dialog");
        public static string PauseDiscord => Get("pause.discord");
        public static string StateCompleted => Get("state.completed");
        public static string EnemyVariant(string number) => F("enemy.variant", number);
        public static string EnemyAttack(string value) => F("enemy.attack", value);
        public static string EnemyHeal(string value) => F("enemy.heal", value);
        public static string EnemyHealth(string value) => F("enemy.health", value);
        public static string EnemyArmor(string value) => F("enemy.armor", value);
        public static string EnemyArtist(string name) => F("enemy.artist", name);
        public static string SearchClear => Get("search.clear");
        public static string UpgradeLevel(int level) => F("upgrade.level", level);
        public static string FrameName(string kind) => F("frame.name", kind);
        public static string Frame(string key) => Get("frame." + key);
        public static string StateMastered => Get("state.mastered");

        public static string Icon(string key) => Has("icon." + key) ? Get("icon." + key) : null;
        public static string CardCost(string value) => F("card.cost", value);
        public static string CardAttack(string value) => F("card.attack", value);
        public static string CardHealth(string value) => F("card.health", value);
        public static string CardSize(string value) => F("card.size", value);
        public static string CardUpgraded(string text) => F("card.upgraded", text);
        public static string CardSoulAttached(string soul) => F("card.soul_attached", soul);

        public static string PyreAttack(string value) => F("pyre.attack", value);
        public static string PyreHealth(string value) => F("pyre.health", value);

        public static string MapChoice => Get("map.choice");
        public static string MapNoForward => Get("map.no_forward");
        public static string MapNoBackward => Get("map.no_backward");
        public static string StateVisited => Get("state.visited");
        public static string StateDefeated => Get("state.defeated");
        public static string StateUpgraded => Get("state.upgraded");
        public static string HoardLevel(int level) => F("hoard.level", level);
        public static string RewardAmount(string amount, string currency) => F("reward.amount", amount, currency);
        public static string StateCurrent => Get("state.current");
        public static string HudTitle => Get("hud.title");
        public static string HudTrainStats => Get("hud.train_stats");
        public static string HudArtifacts => Get("hud.artifacts");
        public static string HudEnemyArtifacts => Get("hud.enemy_artifacts");

        public static string ScreenModMenu => Get("screen.mod_menu");
        public static string ModSettings => Get("mod.settings");
        public static string ModKeyHelp => Get("mod.key_help");
        public static string ModClose => Get("mod.close");
        public static string ModSpeakPositions => Get("mod.speak_positions");
        public static string ModFocusOnLaunch => Get("mod.focus_on_launch");
        public static string ModKeyCategory(string category)
        {
            string name = "mod.category." + (category ?? "").ToLowerInvariant();
            return F("mod.key_category", Has(name) ? Get(name) : category);
        }

        public static string ScreenHelp => Get("screen.help");
        public static string HelpNone => Get("help.none");
        public static string HelpRow(string label, string keys) => F("help.row", label, keys);
        public static string JumpTo(string stop) => F("help.jump_to", stop);

        /// <summary>A battle event's line: "event.&lt;key&gt;" filled in ("Turn {0}.").</summary>
        public static string Event(string key, params object[] args) => F("event." + key, args);
        /// <summary>A battle line: "battle.&lt;key&gt;" filled in ("Floor {0}").</summary>
        public static string Battle(string key, params object[] args) => F("battle." + key, args);
        public static string EventSettings => Get("event_settings");
        public static string EventAnnounce => Get("event_settings.announce");
        public static string EventBuffer => Get("event_settings.buffer");
        public static string EventFriendly => Get("event_settings.friendly");
        public static string EventEnemy => Get("event_settings.enemy");
    }
}

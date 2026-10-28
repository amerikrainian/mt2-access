using System;
using System.Collections.Generic;

namespace MonsterTrainAccessibility.Core.Graph
{
    /// <summary>The four navigable directions between graph nodes (explicit edges). Tab-stop cycling and
    /// region jumps are OPERATIONS over node metadata (<see cref="GraphNode.StopKey"/> /
    /// <see cref="GraphNode.RegionKey"/>), not edges: they carry per-stop remembered positions, which a
    /// static edge cannot express.</summary>
    public enum GraphDir
    {
        Up,
        Right,
        Down,
        Left,
    }

    /// <summary>The well-known announcement-part kinds. A part's kind is its identity for control-type
    /// ordering, node-over-type overriding, and the user's per-kind announcement settings.</summary>
    public static class AnnouncementKinds
    {
        public const string Label = "label";
        public const string Role = "role";
        public const string Value = "value";
        public const string Selected = "selected";
        public const string Enabled = "enabled";
        public const string Tooltip = "tooltip";
        public const string Position = "position";
    }

    /// <summary>
    /// One part of a control's spoken focus readout ("Hold position" / "toggle" / "on"), resolved live at
    /// speak time. A LIVE part is additionally watched while its node is focused: when its resolved text
    /// changes (an async toggle settling, a value the game flips), the navigator speaks just that part
    /// immediately, state feedback without re-reading the whole control.
    /// </summary>
    public sealed class NodeAnnouncement
    {
        /// <summary>The part's text, resolved live. Null/empty at speak time = the part stays silent.</summary>
        public Func<string> Text;

        /// <summary>Watch this part while the node is focused and speak it when its value changes.</summary>
        public bool Live;

        /// <summary>On a <see cref="NodeVtable.LiveReadout"/> node: this part's changes do NOT trigger the
        /// re-read (it is still spoken, at its current value, whenever another part triggers one). For
        /// values that never rest, like a unit's mana regenerating all through a fight.</summary>
        public bool LiveReadoutIgnore;

        /// <summary>The part's kind (<see cref="AnnouncementKinds"/>), or null for a custom one-off part.
        /// Kinds drive the control type's speak order, let a node's part override the type's common part
        /// of the same kind, and key the user's per-kind announcement settings.</summary>
        public string Kind;

        public NodeAnnouncement(Func<string> text, bool live = false, string kind = null)
        {
            Text = text;
            Live = live;
            Kind = kind;
        }

        public static NodeAnnouncement Static(string text) => new NodeAnnouncement(() => text);
    }

    /// <summary>
    /// A CONTROL TYPE ("button", "toggle", "slider") as a registry value rather than a C# class. A type
    /// owns the speak ORDER of its announcement kinds and the parts COMMON to every control of the type
    /// (the localized role word); nodes contribute their specific parts, overriding a common part of the
    /// same kind. The user's per-type announcement settings key off <see cref="Key"/>.
    /// </summary>
    public sealed class ControlType
    {
        /// <summary>Stable settings/registry key ("button", "toggle", "slider").</summary>
        public string Key;

        /// <summary>The announcement kinds in speak order; parts with unknown/absent kinds append after,
        /// in declaration order.</summary>
        public string[] Order;

        /// <summary>The parts every control of this type shares (the role word), resolved per compose.
        /// Null = none.</summary>
        public Func<IReadOnlyList<NodeAnnouncement>> Common;
    }

    /// <summary>
    /// The behaviors of a control, as data. <see cref="Announcements"/> is required (its parts compose the
    /// spoken focus readout; the first part is the control's label for search/dedupe purposes); the rest
    /// are optional: a null slot means the control does not have that behavior and the navigator speaks
    /// its "nothing there" feedback instead.
    /// </summary>
    public sealed class NodeVtable
    {
        /// <summary>Required, at least one part. The control's spoken focus readout. Parts marked
        /// <see cref="NodeAnnouncement.Live"/> re-speak on change while focused. When
        /// <see cref="ControlType"/> is set, the type's common parts merge in and the type's kind order
        /// applies; otherwise parts speak in declaration order.</summary>
        public IReadOnlyList<NodeAnnouncement> Announcements;

        /// <summary>The control's type (registry value): supplies the role word, the speak order, and the
        /// per-type announcement settings identity. Null = an untyped one-off.</summary>
        public ControlType ControlType;

        /// <summary>Optional. The node's LANDING group, the general row-vanish rule: when the focused
        /// node disappears on a rebuild, the reconcile fallback lands on the nearest surviving node of the
        /// SAME group (next first, then previous), never on a header, lead line or another panel. Sheets
        /// stamp their data rows with their key automatically; nodes without a group use the plain
        /// nearest-survivor walk.</summary>
        public string LandGroup;

        /// <summary>Optional. When this node vanishes on a rebuild while focused, the reconcile landing on
        /// its replacement is NOT spoken (focus moves silently; the next key reads from there). For rows
        /// that come and go on their own all the time, like the units of a fight: reading out a neighbour
        /// at every death is noise, not information.</summary>
        public bool QuietVanish;

        /// <summary>Optional. Where focus goes when this node vanishes on a rebuild while focused,
        /// instead of the nearest survivor (which is the control BEFORE it): asked at that rebuild,
        /// honoured when the node it names is in the new render. For a placeholder standing where
        /// content is about to be, a list's "Loading...": the player who waited on it lands on what
        /// it loaded, the first entry, not back on the tab that started the load.</summary>
        public Func<ControlId> VanishTo;

        /// <summary>Optional. While this node is focused, its whole readout is spoken again whenever
        /// any part's text changes (the parts read live game state at speak time), interrupting so the
        /// latest value wins; changes within a short window coalesce into one re-read. For a unit in a
        /// fight: its health, shield and mana follow the fight while the player rests on it. Independent
        /// of <see cref="NodeAnnouncement.Live"/>, which speaks single parts and queues.</summary>
        public bool LiveReadout;

        /// <summary>Optional. Primary activation, the left-click equivalent (Enter).</summary>
        public Action OnActivate;

        /// <summary>Optional. Runs when navigation lands on the node (arrows, Home/End, Tab), just before
        /// the landing is spoken; not on a screen's first focus. Tabs and radio buttons select themselves
        /// here, so browsing them is choosing them and the landing reads "selected".</summary>
        public Action OnFocus;

        /// <summary>Optional. Secondary activation, the right-click equivalent (Backspace).</summary>
        public Action OnSecondary;

        /// <summary>Optional. The control's detail lines for the ui buffer, read live: one line per
        /// tooltip (an item's, an ability's, a stat's), each a separate line the player steps through
        /// with the buffer keys. Nothing here is ever spoken on its own; the focus announcement stays
        /// short and the detail waits in the buffer.</summary>
        public Func<IEnumerable<string>> Details;

        /// <summary>Optional. Lines for a side buffer (<c>BufferKeys.Hero</c>, <c>BufferKeys.Item</c>) about
        /// what the control concerns: the hero whose slot, card or unit this is, the items it carries.
        /// Null or empty for a key = that buffer has nothing here and the review keys skip it.</summary>
        public Func<string, IEnumerable<string>> SideLines;

        /// <summary>Optional. The game thing this control concerns, for the glance keys (the digits
        /// read one fact group of the unit under focus, in place): a unit's view, card, slot or id,
        /// whatever the module's resolver understands. Read at the keypress, never cached. Null = the
        /// glance keys are silent here.</summary>
        public Func<object> Subject;

        /// <summary>Optional. Drag/drop participation (Backslash): pick up here, or place the held thing
        /// here. The action owns the whole pick-up/place state machine; the core only dispatches.</summary>
        public Action OnDrag;

        /// <summary>Optional. Remove / clear the focused thing (Delete). The action owns the behavior;
        /// the core only dispatches.</summary>
        public Action OnDelete;

        /// <summary>Optional. Horizontal value adjust (a slider): sign is -1 (decrease) / +1 (increase),
        /// large requests a coarse step. When set, left/right do NOT navigate.</summary>
        public Action<int, bool> OnAdjust;

        /// <summary>Optional. The control's state line, spoken IMMEDIATELY (interrupting) after an
        /// activation/adjust that changes state: the synchronous feedback path for rapid key repeats.
        /// Asynchronous/game-driven changes ride the Live announcement watch instead.</summary>
        public Func<string> StateText;

        /// <summary>Optional. Plays this control's hover sound when focus lands on it, replacing the
        /// navigator's default. A plain action so the core stays game-agnostic.</summary>
        public Action HoverSound;

        /// <summary>Optional. The text type-ahead matches against; null = the first announcement part
        /// (the label).</summary>
        public Func<string> SearchText;

        /// <summary>If true, type-ahead never matches this control.</summary>
        public bool ExcludeFromSearch;

        /// <summary>Optional (Expandable groups): override HOW expansion state changes. When null the
        /// engine mutates the persistent expansion set (<see cref="GraphState.Expanded"/>).</summary>
        public Action OnExpand;
        public Action OnCollapse;

        /// <summary>Set when this group's own announcements already include its expanded/collapsed state,
        /// so the announcer does not append it again.</summary>
        public bool SpeaksOwnExpansion;

        /// <summary>Set when this node's announcements already include its list position, so the
        /// announcer does not append the auto-stamped one.</summary>
        public bool SpeaksOwnPosition;

        /// <summary>The node's LOGICAL COLUMN in a tabular row (0 = the row's primary), or -1 when not
        /// tabular. Stamped by GraphSheet; the engine uses it to PRESERVE the column when focus jumps
        /// non-directionally (the reconcile fallback after a row vanishes, and type-ahead landings) by
        /// sliding along the destination row's Left/Right edges to the same column.</summary>
        public int Column = -1;
    }

    /// <summary>A directed edge to another node, with an optional spoken transition line (a "lane
    /// change", e.g. crossing into a new column band). Plain data; contextual announcements are composed
    /// from node metadata by the announcer, not per-edge closures.</summary>
    public sealed class Transition
    {
        public ControlId Destination;
        public string Label; // spoken only while crossing this edge; null = silent edge

        public Transition(ControlId destination, string label = null)
        {
            Destination = destination;
            Label = label;
        }
    }

    /// <summary>A control: identity, behaviors, directional transitions, and structural metadata (its
    /// parent chain, tab-stop and region membership, expandability).</summary>
    public sealed class GraphNode
    {
        public ControlId Id;
        public NodeVtable Vtable;
        public readonly Dictionary<GraphDir, Transition> Transitions = new Dictionary<GraphDir, Transition>();

        /// <summary>The node's structural parent within THIS render, or null at screen level. The parent
        /// chain IS the presentation hierarchy: the announcer prefix-diffs old/new chains by identity, so
        /// entering a group reads its levels outermost-first and descending from a group onto its own
        /// child re-announces nothing. A parent may be non-focusable pure structure (a labeled panel,
        /// <see cref="Focusable"/> false, never in Nodes/Order) or a real control (a tree group header).</summary>
        public GraphNode Parent;

        /// <summary>False for a pure-structure parent node (a labeled panel): it exists only on
        /// <see cref="Parent"/> chains for announcements, never navigable, never in Nodes/Order.</summary>
        public bool Focusable = true;

        /// <summary>This node is a group that can expand/collapse (a tree section header). The engine's
        /// tree operations (expand/collapse/descend/ascend) key off this.</summary>
        public bool Expandable;

        /// <summary>An <see cref="Expandable"/> group's state AT THIS RENDER (stamped by the builder from
        /// the persistent expansion set, or the explicit value the declarer passed).</summary>
        public bool Expanded;

        /// <summary>The Tab-stop this node belongs to. Nodes sharing a StopKey form one stop; Tab cycles
        /// stops in first-appearance order, landing on the stop's remembered position.</summary>
        public object StopKey;

        /// <summary>The region (within a stop) this node belongs to, or null. Ctrl+Up/Down jumps between
        /// regions in first-appearance order.</summary>
        public object RegionKey;

        /// <summary>Auto-stamped sibling position (1-based) and count, from the builder: menu-mode nodes
        /// grouped by (parent, stop), "3 of 10" among the siblings arrows actually reach. 0 = none
        /// (single sibling, raw/grid nodes, or a multi-item row member positioned within its row).</summary>
        public int PositionIndex;
        public int PositionCount;

        /// <summary>On a parent (context/group) node: its direct children get NO auto position, for
        /// log-like streams where "37 of 200" is noise.</summary>
        public bool SuppressChildPositions;
    }

    /// <summary>
    /// One built snapshot of a graph: the nodes (keyed by structural identity), their order of
    /// declaration, and where focus starts when there is no prior position. Rebuilt per operation and
    /// thrown away; live state belongs in the node callbacks, not here.
    /// </summary>
    public sealed class GraphRender
    {
        public ControlId StartKey;
        public readonly Dictionary<ControlId, GraphNode> Nodes = new Dictionary<ControlId, GraphNode>();

        /// <summary>Declaration order: drives stop/region cycling and type-ahead scan order.</summary>
        public readonly List<GraphNode> Order = new List<GraphNode>();

        public GraphNode NodeAt(ControlId key)
        {
            if (key == null) return null;
            GraphNode n;
            return Nodes.TryGetValue(key, out n) ? n : null;
        }
    }

    /// <summary>
    /// The persistent cursor for a graph, the only thing that survives between renders. Holds where
    /// focus is, the last computed traversal order (for closest-survivor recovery), per-stop remembered
    /// positions (so Tab returns to where you were in a stop), and a one-shot move request.
    /// </summary>
    public sealed class GraphState
    {
        /// <summary>The focused control's id (carries its Reference for tier-1 recovery). Null until first render.</summary>
        public ControlId CurKey;

        /// <summary>The down-right total order from the previous render. Null on first render.</summary>
        public List<ControlId> KeyOrder;

        /// <summary>If set, focus jumps here on the next render when present (consumed either way).</summary>
        public ControlId NextSuggestedMove;

        /// <summary>The last render, kept with the state so a graph built anew over it (the screen
        /// re-attached after a dialog over it) still asks the focused node, gone meanwhile, for its
        /// <see cref="NodeVtable.VanishTo"/>.</summary>
        internal GraphRender LastRender;

        /// <summary>The logical column focus last sat on in a tabular row (see NodeVtable.Column), or
        /// -1. The reconcile fallback slides to this column when the focused row vanished.</summary>
        public int LastColumn = -1;

        /// <summary>The focused node's <see cref="NodeVtable.LandGroup"/> (null when it had none); the
        /// row-vanish fallback prefers survivors of this group.</summary>
        public string LastLandGroup;

        /// <summary>Set by a rebuild whose reconcile moved focus off a vanished <see cref="NodeVtable.QuietVanish"/>
        /// node; the navigator's next frame takes the landing as already spoken and clears it.</summary>
        public bool QuietLanding;

        /// <summary>Remembered position per Tab-stop: where Tab lands when cycling back into a stop.</summary>
        public readonly Dictionary<object, ControlId> StopMemory = new Dictionary<object, ControlId>();

        /// <summary>The expanded groups (by id). The builder consults this for groups declared without an
        /// explicit state; the engine's expand/collapse operations mutate it. Screens hold NO expansion
        /// state of their own.</summary>
        public readonly HashSet<ControlId> Expanded = new HashSet<ControlId>();
    }
}

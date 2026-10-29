using System;
using System.Collections.Generic;

namespace MonsterTrainAccessibility.Core.Graph
{
    /// <summary>
    /// Builds a <see cref="GraphRender"/>. Two construction styles, freely mixable in one build:
    ///
    /// <para><b>Menu mode</b>: rows of controls, wired automatically: left/right within a row, up/down
    /// between consecutive rows (two rows sharing a non-null row key get column navigation: up/down
    /// preserves the position instead of snapping to the first item). Items added outside an explicit
    /// row become single-item rows (a plain vertical menu).</para>
    ///
    /// <para><b>Raw mode</b>: <see cref="AddNode"/> + <see cref="Connect"/> for arbitrary topologies.</para>
    ///
    /// <para><b>Columns</b>: <see cref="StartColumn"/> / <see cref="EndColumn"/> lay containers side by
    /// side inside one stop (heroes, items and relics for sale): up/down within a column, left/right
    /// across consecutive columns with the index kept where the neighbour reaches that far, each column
    /// typically its own context.</para>
    ///
    /// Orthogonal to both: <see cref="BeginStop"/> groups nodes into Tab-stops (arrows never cross a stop;
    /// Tab cycles them), <see cref="SetRegion"/> tags nodes with a region for Ctrl+arrow jumps, and the
    /// PARENT STACK builds the presentation hierarchy: <see cref="PushContext"/> pushes a non-focusable
    /// structural level ("Difficulty settings, list", announced when focus enters from outside), while
    /// <see cref="BeginGroup"/> pushes a focusable, EXPANDABLE group header (a tree section) whose children
    /// only emit while it is expanded. Expansion state lives in the persistent set the builder is
    /// constructed with (<see cref="GraphState.Expanded"/>), so screens hold no tree state of their own.
    /// Nesting recurses; a collapsed ancestor suppresses everything beneath it.
    /// </summary>
    public sealed class GraphBuilder
    {
        private readonly HashSet<ControlId> _expansion; // persistent expanded-group set (null = all explicit)

        public GraphBuilder(HashSet<ControlId> expansion = null) { _expansion = expansion; }

        private sealed class Row
        {
            public readonly List<GraphNode> Items = new List<GraphNode>();
            public object Key;
            public object StopKey;
            public Func<int> Entry;
            public bool StrictColumns;
        }

        private sealed class RawEdge
        {
            public ControlId From;
            public GraphDir Dir;
            public ControlId To;
            public string Label;
        }

        // Menu mode.
        private readonly List<Row> _rows = new List<Row>();
        private Row _currentRow;

        // Columns: raw nodes in vertical chains of their own, consecutive columns of a stop chained across.
        private sealed class Column
        {
            public readonly List<GraphNode> Items = new List<GraphNode>();
            public object StopKey;
        }

        private readonly List<Column> _columns = new List<Column>();
        private Column _currentColumn;

        // Raw mode.
        private readonly List<GraphNode> _rawNodes = new List<GraphNode>();
        private readonly List<RawEdge> _rawEdges = new List<RawEdge>();

        // Every node in DECLARATION order regardless of mode: the render's node order (and so the
        // Tab-stop cycle) must interleave menu rows and raw nodes as the screen declared them.
        private readonly List<GraphNode> _declared = new List<GraphNode>();

        // The menu row each menu-mode node belongs to (null for raw nodes), for stitching the vertical
        // gap where a stop mixes menu rows with raw content (a sheet below filter controls).
        private readonly Dictionary<GraphNode, Row> _rowOf = new Dictionary<GraphNode, Row>();

        // Shared.
        private readonly HashSet<ControlId> _ids = new HashSet<ControlId>();
        private ControlId _start;

        // Stop / region / parent state applied to nodes as they are added.
        private object _stopKey = AutoStopKey(0);
        private int _stopAuto = 1;
        private object _regionKey;

        // The parent stack: structural levels (PushContext) and group headers (BeginGroup). A frame whose
        // group is collapsed suppresses every declaration beneath it (the stack stays balanced regardless).
        private sealed class ParentFrame
        {
            public GraphNode Node;      // the parent node (non-focusable context, or the group header)
            public bool Suppressed;     // this frame's subtree is swallowed (collapsed, or under a collapsed ancestor)
        }

        private readonly List<ParentFrame> _parents = new List<ParentFrame>();

        private GraphNode CurrentParent => _parents.Count > 0 ? _parents[_parents.Count - 1].Node : null;
        private bool Suppressed => _parents.Count > 0 && _parents[_parents.Count - 1].Suppressed;

        private static object AutoStopKey(int index) => "stop#" + index;

        // ---- stops / regions ----

        /// <summary>Start a new Tab-stop; nodes added from here belong to it. <paramref name="key"/> must be
        /// stable across rebuilds (it keys the stop's remembered position); null auto-assigns by index,
        /// which is stable when the screen builds its stops in a fixed order.</summary>
        public GraphBuilder BeginStop(object key = null)
        {
            if (_currentRow != null) throw new InvalidOperationException("Cannot begin a stop inside an open row");
            if (_currentColumn != null) throw new InvalidOperationException("Cannot begin a stop inside an open column");
            _stopKey = key ?? AutoStopKey(_stopAuto);
            _stopAuto++;
            _regionKey = null; // regions are per-stop
            return this;
        }

        /// <summary>Tag nodes added from here with a region (Ctrl+arrow jump target) within the current
        /// stop; null clears. Region keys must be stable across rebuilds.</summary>
        public GraphBuilder SetRegion(object key)
        {
            _regionKey = key;
            return this;
        }

        // ---- the parent stack: contexts + groups ----

        /// <summary>Push one NON-FOCUSABLE level of presentation hierarchy ("Difficulty settings",
        /// "list") onto nodes added from here: pure structure, never navigable, announced when focus
        /// enters from outside. Close with <see cref="PopContext"/>.</summary>
        public GraphBuilder PushContext(string label, string role = null, bool positions = true)
        {
            var parent = CurrentParent;
            var anns = new List<NodeAnnouncement> { NodeAnnouncement.Static(label) };
            if (!string.IsNullOrEmpty(role)) anns.Add(NodeAnnouncement.Static(role));
            var node = new GraphNode
            {
                // Stable synthetic identity so cross-render chain diffs match up, SCOPED BY THE STOP: a
                // purely label-pathed id collided for same-named contexts on different stops.
                Id = ControlId.Structural("ctx:" + _stopKey + ":" + (parent?.Id.StructuralKey ?? "") + "/" + label),
                Vtable = new NodeVtable { Announcements = anns },
                Parent = parent,
                Focusable = false,
                SuppressChildPositions = !positions,
            };
            _parents.Add(new ParentFrame { Node = node, Suppressed = Suppressed });
            return this;
        }

        public GraphBuilder PopContext()
        {
            if (_parents.Count == 0) throw new InvalidOperationException("No context/group to pop");
            _parents.RemoveAt(_parents.Count - 1);
            return this;
        }

        /// <summary>
        /// Push a FOCUSABLE, expandable group header (a tree section): the header emits as a navigable
        /// node here, and the children declared before <see cref="EndGroup"/> emit only while the group is
        /// expanded (a collapsed ancestor suppresses the whole subtree). Expansion state:
        /// <paramref name="expanded"/> when given, else the persistent expansion set the builder was
        /// constructed with, else <paramref name="defaultExpanded"/>. The engine's tree operations
        /// (Right/Left) expand/collapse via the vtable's OnExpand/OnCollapse overrides when set, else by
        /// mutating the persistent set.
        /// </summary>
        public GraphBuilder BeginGroup(ControlId id, NodeVtable vtable, bool? expanded = null,
            bool defaultExpanded = false)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            if (_currentRow != null) throw new InvalidOperationException("Cannot begin a group inside an open row");
            if (_currentColumn != null) throw new InvalidOperationException("Cannot begin a group inside an open column");
            bool isExpanded = expanded ?? (_expansion != null ? _expansion.Contains(id) : defaultExpanded);

            GraphNode header = null;
            if (!Suppressed)
            {
                header = MakeNode(id, vtable);
                header.Expandable = true;
                header.Expanded = isExpanded;
                var row = new Row { StopKey = _stopKey };
                row.Items.Add(header);
                _rows.Add(row);
                _rowOf[header] = row;
            }
            _parents.Add(new ParentFrame
            {
                // Suppressed subtree: keep chaining from the outer parent so the stack stays coherent.
                Node = header ?? CurrentParent,
                Suppressed = Suppressed || !isExpanded,
            });
            return this;
        }

        public GraphBuilder EndGroup() => PopContext();

        /// <summary>Whether a group id is expanded in the persistent set, for screens that must avoid
        /// even BUILDING a collapsed group's children (a lazy hierarchy).</summary>
        public bool IsExpanded(ControlId id) => _expansion != null && id != null && _expansion.Contains(id);

        /// <summary>Focus starts here when the graph has no prior position (defaults to the first node).</summary>
        public GraphBuilder SetStart(ControlId id)
        {
            _start = id;
            return this;
        }

        // ---- menu mode ----

        /// <summary>Open a horizontal row. Rows sharing a non-null <paramref name="rowKey"/> with the row
        /// above/below get column-preserving vertical navigation. <paramref name="entry"/> is the index
        /// of the item a vertical move INTO the row lands on when no column says otherwise (the first
        /// by default): a row of tabs that select on landing names its selected one, so coming up from
        /// the content under them does not switch tabs. Asked when the render is wired.
        /// <paramref name="strictColumns"/> makes keyed rows a true grid: a vertical move into a column
        /// the neighbouring row does not reach (a short last row) goes nowhere, instead of falling to
        /// that row's entry.</summary>
        public GraphBuilder StartRow(object rowKey = null, Func<int> entry = null, bool strictColumns = false)
        {
            if (_currentRow != null) throw new InvalidOperationException("Cannot start a row while another is open");
            if (_currentColumn != null) throw new InvalidOperationException("Cannot start a row inside an open column");
            _currentRow = new Row { Key = rowKey, StopKey = _stopKey, Entry = entry, StrictColumns = strictColumns };
            return this;
        }

        // The item a vertical move into the row lands on: the one its Entry names, else the first.
        private static GraphNode EntryOf(Row row)
        {
            int index = 0;
            if (row.Entry != null)
            {
                try { index = row.Entry(); }
                catch (Exception e) { CoreLog.Warning("GraphBuilder: a row's entry failed: " + e.Message); }
            }
            return row.Items[index >= 0 && index < row.Items.Count ? index : 0];
        }

        public GraphBuilder EndRow()
        {
            if (_currentRow == null) throw new InvalidOperationException("No row to end");
            if (_currentRow.Items.Count == 0 && !Suppressed)
                throw new InvalidOperationException("Row cannot be empty");
            if (_currentRow.Items.Count > 0) _rows.Add(_currentRow);
            _currentRow = null;
            return this;
        }

        // ---- columns ----

        /// <summary>Open a vertical column: the controls added until <see cref="EndColumn"/> chain up/down
        /// among themselves, and consecutive columns of one stop chain left/right, the index kept where
        /// the neighbour reaches that far (its last control else). So containers sit side by side, each
        /// pushed as its own context: Right crosses from the last hero for sale to the items. Column nodes
        /// are raw: no menu row chains into them, Home and End walk the column, an explicit
        /// <see cref="Connect"/> edge wins, and positions stamp within the column (unless its context
        /// said no). An empty column declares nothing, so a container with nothing in it is simply
        /// not there and its neighbours meet.</summary>
        public GraphBuilder StartColumn()
        {
            if (_currentRow != null) throw new InvalidOperationException("Cannot start a column inside an open row");
            if (_currentColumn != null) throw new InvalidOperationException("Cannot start a column while another is open");
            _currentColumn = new Column { StopKey = _stopKey };
            return this;
        }

        public GraphBuilder EndColumn()
        {
            if (_currentColumn == null) throw new InvalidOperationException("No column to end");
            if (_currentColumn.Items.Count > 0) _columns.Add(_currentColumn);
            _currentColumn = null;
            return this;
        }

        /// <summary>Add a control: into the open column or row, or as its own single-item row. A no-op
        /// inside a collapsed group's subtree.</summary>
        public GraphBuilder AddItem(ControlId id, NodeVtable vtable)
        {
            if (Suppressed) return this;
            var node = MakeNode(id, vtable);
            if (_currentColumn != null)
            {
                _rawNodes.Add(node);
                _currentColumn.Items.Add(node);
            }
            else if (_currentRow != null)
            {
                _currentRow.Items.Add(node);
                _rowOf[node] = _currentRow;
            }
            else
            {
                var row = new Row { StopKey = _stopKey };
                row.Items.Add(node);
                _rows.Add(row);
                _rowOf[node] = row;
            }
            return this;
        }

        /// <summary>Add a read-only line (label only; no actions).</summary>
        public GraphBuilder AddLabel(ControlId id, Func<string> label)
            => AddItem(id, new NodeVtable { Announcements = new[] { new NodeAnnouncement(label) } });

        // ---- raw mode ----

        /// <summary>Add a node with no automatic wiring (raw mode; wire with <see cref="Connect"/>).
        /// A no-op inside a collapsed group's subtree.</summary>
        public GraphBuilder AddNode(ControlId id, NodeVtable vtable)
        {
            if (Suppressed) return this;
            _rawNodes.Add(MakeNode(id, vtable));
            return this;
        }

        /// <summary>Directed edge from -> to, with an optional spoken transition line ("lane change").
        /// Edges to/from undeclared nodes are dropped at build.</summary>
        public GraphBuilder Connect(ControlId from, GraphDir dir, ControlId to, string label = null)
        {
            if (from == null || to == null)
                throw new ArgumentNullException(from == null ? nameof(from) : nameof(to));
            _rawEdges.Add(new RawEdge { From = from, Dir = dir, To = to, Label = label });
            return this;
        }

        private GraphNode MakeNode(ControlId id, NodeVtable vtable)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            if (vtable == null || vtable.Announcements == null || vtable.Announcements.Count == 0)
                throw new ArgumentException("A control must have at least one announcement", nameof(vtable));
            if (!_ids.Add(id)) throw new InvalidOperationException("Duplicate control id: " + id);
            var node = new GraphNode
            {
                Id = id,
                Vtable = vtable,
                Parent = CurrentParent,
                StopKey = _stopKey,
                RegionKey = _regionKey,
            };
            _declared.Add(node);
            return node;
        }

        // ---- build ----

        /// <summary>Finalize into a render, or null when nothing was declared (treat as "closed").
        /// Menu rows and raw nodes/edges may coexist in one build: rows wire themselves; raw edges may
        /// reference any node.</summary>
        public GraphRender Build()
        {
            if (_currentRow != null) throw new InvalidOperationException("Unclosed row - call EndRow()");
            if (_currentColumn != null) throw new InvalidOperationException("Unclosed column - call EndColumn()");
            if (_rawNodes.Count == 0 && _rows.Count == 0) return null;

            var render = new GraphRender();
            foreach (var node in _declared) AddNodeTo(render, node);

            WireMenuEdges(render);
            foreach (var e in _rawEdges)
                if (render.Nodes.ContainsKey(e.From) && render.Nodes.ContainsKey(e.To))
                    render.Nodes[e.From].Transitions[e.Dir] = new Transition(e.To, e.Label);
            WireColumns();
            StitchModeBoundaries();

            render.StartKey = _start != null && render.Nodes.ContainsKey(_start)
                ? _start
                : render.Order[0].Id;
            StampPositions();
            return render;
        }

        // Where a stop mixes MENU rows with RAW content (search/sort/filter controls above a sheet), the
        // two wiring systems do not see each other, leaving a vertical gap arrows cannot cross. Stitch
        // it: at each menu->raw boundary (declaration order, same stop), the menu row's cells gain Down
        // edges into the first raw node still missing an Up edge, and that node gains the Up back; at
        // raw->menu boundaries the reverse. Only MISSING edges are filled.
        private void StitchModeBoundaries()
        {
            var byStop = new Dictionary<object, List<GraphNode>>();
            var stops = new List<object>();
            foreach (var n in _declared)
            {
                if (!byStop.TryGetValue(n.StopKey, out var list))
                {
                    list = new List<GraphNode>();
                    byStop.Add(n.StopKey, list);
                    stops.Add(n.StopKey);
                }
                list.Add(n);
            }

            foreach (var stop in stops)
            {
                var nodes = byStop[stop];
                for (int i = 1; i < nodes.Count; i++)
                {
                    var prev = nodes[i - 1];
                    var cur = nodes[i];
                    bool prevMenu = _rowOf.ContainsKey(prev);
                    bool curMenu = _rowOf.ContainsKey(cur);
                    if (prevMenu == curMenu) continue; // same mode: its own wiring covers it

                    if (prevMenu) // menu row above raw content: row cells down to first raw node without an Up
                    {
                        if (cur.Transitions.ContainsKey(GraphDir.Up)) continue;
                        var row = _rowOf[prev];
                        foreach (var cell in row.Items)
                            if (!cell.Transitions.ContainsKey(GraphDir.Down))
                                cell.Transitions[GraphDir.Down] = new Transition(cur.Id);
                        cur.Transitions[GraphDir.Up] = new Transition(EntryOf(row).Id);
                    }
                    else // raw content above a menu row: last raw node without a Down links to the row
                    {
                        var row = _rowOf[cur];
                        // The raw side's bottom = the latest raw node (walking back) missing a Down.
                        GraphNode bottom = null;
                        for (int j = i - 1; j >= 0 && !_rowOf.ContainsKey(nodes[j]); j--)
                            if (!nodes[j].Transitions.ContainsKey(GraphDir.Down)) { bottom = nodes[j]; break; }
                        if (bottom == null) continue;
                        bottom.Transitions[GraphDir.Down] = new Transition(EntryOf(row).Id);
                        foreach (var cell in row.Items)
                            if (!cell.Transitions.ContainsKey(GraphDir.Up))
                                cell.Transitions[GraphDir.Up] = new Transition(bottom.Id);
                    }
                }
            }
        }

        // Auto-stamp "n of m" positions: a multi-item row's members are positioned within their ROW (a
        // bar); single-item-row nodes among the siblings sharing their (parent, stop), the vertical
        // list/tree level arrows actually traverse; a column's members within their column. Raw/grid
        // nodes get none. Announced only when m > 1.
        private void StampPositions()
        {
            var groups = new Dictionary<object, List<GraphNode>>();
            var keys = new List<object>();
            foreach (var row in _rows)
            {
                var first = row.Items[0];
                if (first.Parent != null && first.Parent.SuppressChildPositions) continue; // the context said no
                if (row.Items.Count > 1)
                {
                    Stamp(row.Items);
                    continue;
                }
                var node = first;
                var key = new KeyValuePair<GraphNode, object>(node.Parent, node.StopKey);
                if (!groups.TryGetValue(key, out var list))
                {
                    list = new List<GraphNode>();
                    groups.Add(key, list);
                    keys.Add(key);
                }
                list.Add(node);
            }
            foreach (var key in keys) Stamp(groups[key]);
            foreach (var column in _columns)
            {
                var parent = column.Items[0].Parent;
                if (parent == null || !parent.SuppressChildPositions) Stamp(column.Items);
            }
        }

        private static void Stamp(List<GraphNode> siblings)
        {
            if (siblings.Count < 2) return;
            for (int i = 0; i < siblings.Count; i++)
            {
                siblings[i].PositionIndex = i + 1;
                siblings[i].PositionCount = siblings.Count;
            }
        }

        // Columns: up/down within each, left/right between consecutive columns of the same stop, the
        // index kept where the neighbour reaches that far (its last control else). Only MISSING edges
        // are filled, so an explicit Connect edge wins.
        private void WireColumns()
        {
            for (int c = 0; c < _columns.Count; c++)
            {
                var column = _columns[c];
                var left = c > 0 && Equals(_columns[c - 1].StopKey, column.StopKey) ? _columns[c - 1] : null;
                var right = c < _columns.Count - 1 && Equals(_columns[c + 1].StopKey, column.StopKey) ? _columns[c + 1] : null;
                for (int i = 0; i < column.Items.Count; i++)
                {
                    var node = column.Items[i];
                    if (i > 0) Fill(node, GraphDir.Up, column.Items[i - 1]);
                    if (i < column.Items.Count - 1) Fill(node, GraphDir.Down, column.Items[i + 1]);
                    if (left != null) Fill(node, GraphDir.Left, left.Items[Math.Min(i, left.Items.Count - 1)]);
                    if (right != null) Fill(node, GraphDir.Right, right.Items[Math.Min(i, right.Items.Count - 1)]);
                }
            }
        }

        private static void Fill(GraphNode node, GraphDir dir, GraphNode to)
        {
            if (!node.Transitions.ContainsKey(dir)) node.Transitions[dir] = new Transition(to.Id);
        }

        private static void AddNodeTo(GraphRender render, GraphNode node)
        {
            render.Nodes.Add(node.Id, node);
            render.Order.Add(node);
        }

        // Left/right within a row; up/down between consecutive rows OF THE SAME STOP (arrows never cross a
        // Tab-stop). Shared non-null row keys preserve the column; otherwise vertical lands on first item.
        private void WireMenuEdges(GraphRender render)
        {
            // Segment rows in DECLARATION order: within a stop, consecutive menu rows chain vertically
            // only when no raw node was declared between them. Interleaved raw content BREAKS the chain;
            // StitchModeBoundaries wires the seams. Without the break, menu edges would skip straight
            // over the raw block, leaving it an unreachable island.
            var byStop = new List<List<Row>>();
            var openSegment = new Dictionary<object, List<Row>>(); // stop -> its currently-open segment
            foreach (var node in _declared)
            {
                Row row;
                if (_rowOf.TryGetValue(node, out row))
                {
                    List<Row> seg;
                    if (!openSegment.TryGetValue(node.StopKey, out seg))
                    {
                        seg = new List<Row>();
                        openSegment.Add(node.StopKey, seg);
                        byStop.Add(seg);
                    }
                    if (seg.Count == 0 || seg[seg.Count - 1] != row) seg.Add(row);
                }
                else
                {
                    openSegment.Remove(node.StopKey); // raw node: close this stop's segment
                }
            }

            foreach (var rows in byStop)
            {
                for (int r = 0; r < rows.Count; r++)
                {
                    var row = rows[r];
                    for (int pos = 0; pos < row.Items.Count; pos++)
                    {
                        var node = row.Items[pos];
                        var up = r > 0 ? VerticalTarget(row, rows[r - 1], pos) : null;
                        if (up != null) node.Transitions[GraphDir.Up] = new Transition(up);
                        var down = r < rows.Count - 1 ? VerticalTarget(row, rows[r + 1], pos) : null;
                        if (down != null) node.Transitions[GraphDir.Down] = new Transition(down);
                        if (pos > 0)
                            node.Transitions[GraphDir.Left] = new Transition(row.Items[pos - 1].Id);
                        if (pos < row.Items.Count - 1)
                            node.Transitions[GraphDir.Right] = new Transition(row.Items[pos + 1].Id);
                    }
                }
            }
        }

        // Where vertical navigation from position pos lands in the adjacent row: the same position when
        // the rows share a non-null key (column nav) and it exists there, else the row's entry; nowhere
        // (null) for a column a strict grid's row does not reach.
        private static ControlId VerticalTarget(Row from, Row to, int pos)
        {
            bool sameGrid = from.Key != null && to.Key != null && Equals(from.Key, to.Key);
            if (sameGrid && pos < to.Items.Count) return to.Items[pos].Id;
            if (sameGrid && (from.StrictColumns || to.StrictColumns)) return null;
            return EntryOf(to).Id;
        }
    }
}

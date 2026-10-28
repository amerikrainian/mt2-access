using System;
using System.Collections.Generic;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.Input;
using MonsterTrainAccessibility.Core.Text;

namespace MonsterTrainAccessibility.Core.UI
{
    /// <summary>
    /// The graph-based navigator: runs every screen on the key-graph core (<see cref="KeyGraph"/>) with
    /// pull-based diffing: the graph is rebuilt per operation and per frame, focus is reconciled by
    /// identity, and a focus change is announced exactly once no matter what caused it (input, a screen
    /// moving focus, a content rebuild, or the game yanking a view). Screens declare their graph fresh
    /// from live game state on every render (immediate mode).
    /// </summary>
    public sealed class GraphNavigator : Navigator
    {
        // One GraphState per LIVE screen (focus cursor, per-stop memory, tree expansion): a screen
        // covered by another keeps its state and restores exactly where you were when focus returns; a
        // POPPED screen's state is dropped (ScreenClosed), so reopening starts fresh.
        private readonly Dictionary<Screens.Screen, GraphState> _states =
            new Dictionary<Screens.Screen, GraphState>();
        private GraphState _state = new GraphState();
        private KeyGraph _graph;

        // The differ's memory: the node identity (and its render node, for context diffing) last spoken.
        private ControlId _lastSpokenKey;
        private GraphNode _lastSpokenNode;

        // The differ memory of a screen the navigator left for a CHILD screen (a dropdown's option list,
        // a context menu), restored when the child hands focus back: the return then diffs against what
        // was last spoken there, so the restored control reads just itself (with any new value) and a
        // landing elsewhere reads only the levels entered, not the whole path again as a fresh entry
        // would. An OUTER screen change (a window closing over a screen) keeps the fresh-entry readout.
        private readonly Dictionary<Screens.Screen, KeyValuePair<ControlId, GraphNode>> _spokenMemory =
            new Dictionary<Screens.Screen, KeyValuePair<ControlId, GraphNode>>();
        private Screens.Screen _returnTo; // the parent a closing child hands focus back to
        private bool _reannounce;         // speak the restored landing even when it is the remembered node

        // A focus request whose target is not in the render yet (lazy content): applied by EnsureFocus.
        private ControlId _pendingFocus;
        private bool _pendingAnnounce;

        public GraphNavigator()
        {
            _search.OnNoMatch = text => Speak(Strings.Strings.SearchNoMatch(text), interrupt: true);
        }

        /// <summary>Focus = a focused NODE.</summary>
        public override bool HasFocus => _graph?.CurrentNode != null;

        public override void Attach(Screens.Screen screen)
        {
            var prev = Screen;
            bool same = ReferenceEquals(screen, prev);
            Screen = screen;
            ClearSearch(announce: false);
            if (!same)
            {
                // Leaving for a child: keep this screen's differ memory for the return.
                if (prev != null && screen != null && ReferenceEquals(screen.ParentScreen, prev))
                    _spokenMemory[prev] = new KeyValuePair<ControlId, GraphNode>(_lastSpokenKey, _lastSpokenNode);

                // Swap to this screen's own state (creating it on first attach). The differ memory
                // resets so the (possibly restored) landing announces itself on return, except on a
                // return from a child, which diffs against the screen's own last readout.
                if (screen != null)
                {
                    if (!_states.TryGetValue(screen, out _state))
                    {
                        _state = new GraphState();
                        _states[screen] = _state;
                    }
                }
                else
                {
                    _state = new GraphState();
                }
                bool childReturn = screen != null && ReferenceEquals(screen, _returnTo)
                    && _spokenMemory.TryGetValue(screen, out var memory);
                if (childReturn)
                {
                    _spokenMemory.Remove(screen);
                    _lastSpokenKey = memory.Key;
                    _lastSpokenNode = memory.Value;
                }
                else
                {
                    _lastSpokenKey = null;
                    _lastSpokenNode = null;
                }
                _reannounce = childReturn;
                // A newly bound screen renders on its first frame: the idle throttle is for a screen
                // at rest, and until a render there is no focused node for a key to act on.
                _lastIdleRender = int.MinValue / 2;
                _returnTo = null;
                _pendingFocus = null;
                _pendingStop = null;
                _liveKey = null;
            }
            _graph = screen != null ? new KeyGraph(() => BuildRender(screen), _state) : null;
        }

        public override void ScreenClosed(Screens.Screen screen)
        {
            if (screen == null) return;
            _states.Remove(screen);
            _spokenMemory.Remove(screen);
            // A closing CHILD hands focus back to its parent (still set here: Screen.RemoveChild notifies
            // before unlinking); the parent's next attach is a return. A closing outer screen is nobody's
            // return target.
            _returnTo = screen.ParentScreen;
        }

        public override void FocusNode(ControlId id, bool announce = true)
        {
            if (id == null) return;
            _pendingFocus = id;
            _pendingAnnounce = announce;
        }

        // Idle-rebuild throttle (see EnsureFocus): full immediate-mode rebuilds only every Nth frame.
        private int _lastIdleRender = int.MinValue / 2; // far past; halved so the subtraction never overflows
        private const int IdleRenderEvery = 6; // ~100ms at 60fps

        // A pending land-on-stop request (applied by EnsureFocus once the stop has nodes): resolves to
        // the stop's FIRST node at apply time, so it works when the caller cannot know the node keys.
        private object _pendingStop;

        public override void FocusStop(object stopKey)
        {
            _pendingStop = stopKey;
        }

        public override object FocusedStopKey => _graph?.CurrentNode?.StopKey;

        public override bool MoveTo(ControlId id)
        {
            if (id == null || _graph == null || !_graph.Rerender()) return false;
            var from = _graph.CurrentNode;
            if (from != null && from.Id.Equals(id)) return false;
            if (!_graph.Focus(id)) return false;
            var node = _graph.CurrentNode;
            if (node == null) return false;
            PlayHover(node);
            FireFocus(node);
            Speak(ComposeMove(from, node, entry: false), interrupt: true);
            _lastSpokenKey = node.Id;
            _lastSpokenNode = node;
            return true;
        }

        public override bool HasStop(object stopKey)
        {
            if (stopKey == null || _graph == null || !_graph.Rerender()) return false;
            foreach (var n in _graph.Current.Order)
                if (n.Focusable && Equals(n.StopKey, stopKey)) return true;
            return false;
        }

        public override bool JumpToStop(object stopKey)
        {
            if (stopKey == null || _graph == null || !_graph.Rerender()) return false;
            var land = KeyGraph.StopLanding(_graph.Current, _graph.State, stopKey);
            if (land == null) return false;
            var from = _graph.CurrentNode;
            if (!_graph.Focus(land.Id)) return false;
            var node = _graph.CurrentNode;
            if (node == null) return false;
            // Spoken as a move from wherever focus was, the node itself included: the key answers
            // "where is the board" with the control it lands on, even when that is the focused one.
            PlayHover(node);
            FireFocus(node);
            Speak(ComposeMove(ReferenceEquals(from, node) ? null : from, node, entry: false), interrupt: true);
            _lastSpokenKey = node.Id;
            _lastSpokenNode = node;
            return true;
        }

        /// <summary>The live render + focused node id (dev inspection).</summary>
        public GraphRender CurrentRender => _graph?.Current;
        public ControlId FocusedNodeId => _graph?.CurrentNode?.Id;
        /// <summary>The focused node itself, for readers that compose from it (the buffers).</summary>
        public GraphNode FocusedNode => _graph?.CurrentNode;

        // Screens declare fresh from live game state on every render (immediate mode).
        private GraphRender BuildRender(Screens.Screen screen)
        {
            var b = new GraphBuilder(_state.Expanded); // groups consult the persistent expansion set
            screen.Build(b);
            return b.Build();
        }

        public override void Blur()
        {
            _state.CurKey = null;
            _lastSpokenKey = null;
            _lastSpokenNode = null;
            _reannounce = false;
            _pendingFocus = null;
            _liveKey = null;
        }

        /// <summary>The per-frame pull: rebuild + reconcile, establish initial focus when content appears,
        /// apply pending focus requests, and announce any focus-identity change exactly once.</summary>
        public override void EnsureFocus()
        {
            if (Screen == null || _graph == null) return;

            // Throttle the IDLE rebuild: every input path rerenders on its own before acting, so this
            // per-frame pull only needs to catch content appearing, external focus drift, and live-state
            // flips, none of which need 60Hz. On throttled frames still watch the focused node's live
            // parts against the cached render (closures read live game state, so value flips are caught
            // at full rate; only structure lags ~100ms).
            bool mustRender = _state.CurKey == null || _pendingFocus != null || _pendingStop != null;
            if (!mustRender && NavInput.Current.FrameCount - _lastIdleRender < IdleRenderEvery)
            {
                var cached = _graph.CurrentNode;
                if (cached != null) WatchLive(cached);
                return;
            }
            _lastIdleRender = NavInput.Current.FrameCount;

            if (_state.CurKey == null && _pendingFocus == null)
            {
                // Unfocused screens stay unfocused until Tab seats a cursor.
                if (Screen.StartUnfocused) return;
                if (!_graph.Rerender()) return; // no content yet: Reconcile will seat the start node once there is
                // Declared initial landing: seat the stop's landing node (remembered, selected member,
                // first), BEFORE the differ announces below.
                var stop = Screen.InitialFocusStop;
                if (stop != null)
                {
                    var land = KeyGraph.StopLanding(_graph.Current, _graph.State, stop);
                    if (land != null) _graph.Focus(land.Id);
                }
            }
            else
            {
                if (!_graph.Rerender()) return; // nothing focusable this frame: retry
                if (_pendingFocus != null)
                {
                    // One retry frame for a target focused mid-build; a target that still is not in the
                    // render was removed, so drop the request rather than re-seating every frame.
                    if (_graph.Current.Nodes.ContainsKey(_pendingFocus))
                    {
                        _graph.Focus(_pendingFocus);
                        if (!_pendingAnnounce) { _lastSpokenKey = _pendingFocus; _lastSpokenNode = _graph.CurrentNode; _reannounce = false; }
                    }
                    _pendingFocus = null;
                }
                if (_pendingStop != null)
                {
                    var land = KeyGraph.StopLanding(_graph.Current, _graph.State, _pendingStop);
                    if (land != null) _graph.Focus(land.Id);
                    _pendingStop = null; // announce rides the normal differ below
                }
            }

            var node = _graph.CurrentNode;
            if (node == null) return;

            // A rebuild moved focus off a vanished QuietVanish node: take that landing as spoken. A key
            // press that rerendered spoke and recorded its own landing already, so only the silent
            // per-frame re-seat is affected.
            if (_graph.State.QuietLanding)
            {
                _graph.State.QuietLanding = false;
                if (_lastSpokenKey != null && !_lastSpokenKey.Equals(node.Id))
                {
                    _lastSpokenKey = node.Id;
                    _lastSpokenNode = node;
                    _reannounce = false;
                }
            }

            if (_reannounce || _lastSpokenKey == null || !_lastSpokenKey.Equals(node.Id))
            {
                // Queued (not interrupting): landings follow the screen name / preceding feedback. A
                // return from a child re-reads the remembered node itself (its value may have changed).
                // A landing asked to be quiet is only recorded: its asker reads the focus itself.
                if (!TakeQuietLanding() && Navigation.FocusActive()) Speak(ComposeMove(_lastSpokenNode, node, entry: _lastSpokenNode == null));
                _lastSpokenKey = node.Id;
                _lastSpokenNode = node;
                _reannounce = false;
            }

            WatchLive(node);
        }

        // ---- live announcements: watch the FOCUSED node's Live parts and speak a part when its value
        // changes (an async toggle settling, the game flipping a state). Baselines silently whenever
        // focus lands on a new identity (the focus announcement already spoke the initial state).
        private ControlId _liveKey;
        private readonly List<string> _liveValues = new List<string>();

        // A LiveReadout node: its whole readout, re-spoken (interrupting) when it changes, at most once
        // per window; a burst of changes (every hit of a fight) ends in one re-read of the latest state.
        private const float LiveReadoutWindow = 0.35f;
        private string _liveReadout;
        private bool _liveReadoutDirty;
        private float _liveReadoutDue;

        private void WatchLive(GraphNode node)
        {
            if (node.Vtable != null && node.Vtable.LiveReadout)
            {
                WatchLiveReadout(node);
                return;
            }
            var anns = GraphAnnouncer.EffectiveAnnouncements(node); // type-merged + settings-filtered
            if (anns.Count == 0) return;
            bool baseline = _liveKey == null || !_liveKey.Equals(node.Id) || _liveValues.Count != anns.Count;
            if (baseline) { _liveKey = node.Id; _liveValues.Clear(); }

            for (int i = 0; i < anns.Count; i++)
            {
                if (anns[i] == null || !anns[i].Live)
                {
                    if (baseline) _liveValues.Add(null);
                    continue;
                }
                string v = null;
                try { v = anns[i].Text?.Invoke(); } catch { }
                if (baseline) { _liveValues.Add(v); continue; }
                if (!string.Equals(_liveValues[i], v))
                {
                    _liveValues[i] = v;
                    if (!string.IsNullOrEmpty(v) && Navigation.FocusActive()) Speak(v, interrupt: false);
                }
            }
        }

        private void WatchLiveReadout(GraphNode node)
        {
            // What triggers a re-read: the parts not marked LiveReadoutIgnore, joined; what is spoken:
            // the whole readout at the moment of speaking.
            string key;
            try { key = LiveReadoutKey(node); }
            catch (Exception e) { CoreLog.Warning("live readout: " + e.Message); return; }
            float now = NavInput.Current.UnscaledTime;
            bool baseline = _liveKey == null || !_liveKey.Equals(node.Id);
            if (baseline)
            {
                // A new landing: the focus announcement spoke this state already.
                _liveKey = node.Id;
                _liveValues.Clear();
                _liveReadout = key;
                _liveReadoutDirty = false;
                _liveReadoutDue = now;
                return;
            }
            if (!string.Equals(_liveReadout, key))
            {
                _liveReadout = key;
                _liveReadoutDirty = true;
            }
            if (_liveReadoutDirty && now >= _liveReadoutDue)
            {
                _liveReadoutDirty = false;
                _liveReadoutDue = now + LiveReadoutWindow;
                string text;
                try { text = GraphAnnouncer.LeafText(node); }
                catch (Exception e) { CoreLog.Warning("live readout: " + e.Message); return; }
                if (!string.IsNullOrEmpty(text) && Navigation.FocusActive()) Speak(text, interrupt: true);
            }
        }

        private static string LiveReadoutKey(GraphNode node)
        {
            var anns = GraphAnnouncer.EffectiveAnnouncements(node);
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < anns.Count; i++)
            {
                if (anns[i] == null || anns[i].LiveReadoutIgnore) continue;
                sb.Append(anns[i].Text?.Invoke()).Append('');
            }
            return sb.ToString();
        }

        public override void AnnounceCurrent()
        {
            if (_graph == null) return;
            // An unfocused screen with nothing focused has nothing to announce, and Rerender would
            // auto-seat a phantom focus at the start node.
            if (_state.CurKey == null && Screen != null && Screen.StartUnfocused) return;
            if (!_graph.Rerender()) return;
            var node = _graph.CurrentNode;
            if (node == null) return;
            _quietLandingUntil = int.MinValue / 2; // read in full here: no landing is owed quiet any more
            Speak(ComposeMove(null, node, entry: true));
            _lastSpokenKey = node.Id;
            _lastSpokenNode = node;
        }

        // ---- input ----

        public override bool OnInputJustPressed(InputAction action)
        {
            if (_search.IsSearchActive)
            {
                if (_searchFocusId != null && !_searchFocusId.Equals(_graph?.CurrentNode?.Id))
                    ClearSearch(announce: false); // focus moved under us: results are stale
                else if (action.Key == UiActions.Home && _search.ResultCount > 0) { _search.JumpToFirstResult(); return true; }
                else if (action.Key == UiActions.End && _search.ResultCount > 0) { _search.JumpToLastResult(); return true; }
                else if (FiredFromSearchKey(action)) return true; // reserved key: TickTypeahead owns it
                else ClearSearch(announce: false);
            }

            switch (action.Key)
            {
                // An arrow, Home or End the graph has no edge for is the focused screen's to answer by
                // the action's key (the run HUD's board jumps between units); unanswered, it bubbles.
                case UiActions.Up: return Arrow(NavDirection.Up) || ScreenAnswers(action.Key);
                case UiActions.Down: return Arrow(NavDirection.Down) || ScreenAnswers(action.Key);
                case UiActions.Left: return Arrow(NavDirection.Left) || ScreenAnswers(action.Key);
                case UiActions.Right: return Arrow(NavDirection.Right) || ScreenAnswers(action.Key);
                case UiActions.Next: return Tab(1);
                case UiActions.Prev: return Tab(-1);
                case UiActions.Home: return JumpEdge(first: true) || ScreenAnswers(action.Key);
                case UiActions.End: return JumpEdge(first: false) || ScreenAnswers(action.Key);
                // Region jumps consume only when the focused node is IN a region; elsewhere Ctrl+arrows bubble.
                case UiActions.RegionPrev: return _graph?.CurrentNode?.RegionKey != null && RegionJump(-1);
                case UiActions.RegionNext: return _graph?.CurrentNode?.RegionKey != null && RegionJump(1);
                case UiActions.Activate:
                    {
                        if (_graph?.CurrentNode == null) return false;
                        VtableActivate();
                        return true;
                    }
                case UiActions.Secondary:
                    {
                        var node = _graph?.CurrentNode;
                        if (node == null) return false;
                        if (node.Vtable.OnSecondary != null) _graph.Secondary();
                        return true;
                    }
                case UiActions.Back:
                    return Screen != null && Screen.InvokeAction(ActionIds.Back);
                case UiActions.Drag:
                    {
                        var node = _graph?.CurrentNode;
                        if (node == null) return false;
                        if (node.Vtable.OnDrag != null) { _graph.Drag(); return true; }
                        Speak(Strings.Strings.NoDragTarget);
                        return true;
                    }
                case UiActions.Delete:
                    {
                        var node = _graph?.CurrentNode;
                        if (node == null) return false;
                        if (node.Vtable.OnDelete != null) { _graph.Delete(); return true; }
                        Speak(Strings.Strings.NoDeleteTarget);
                        return true;
                    }
                case UiActions.ReadFocus:
                    {
                        if (_graph?.CurrentNode == null) return false;
                        AnnounceCurrent();
                        return true;
                    }
                default:
                    // A UI key the navigator has no meaning of its own for is the focused screen's to
                    // answer, by the action's key (the run HUD's Shift+arrows move a hero); unanswered,
                    // it falls through to the action's own handler.
                    return Screen != null && Screen.InvokeAction(action.Key);
            }
        }

        private bool ScreenAnswers(string actionKey) => Screen != null && Screen.InvokeAction(actionKey);

        // ---- the dry run ----

        /// <summary>The same decisions as <see cref="OnInputJustPressed"/>, made without acting: an
        /// arrow where the focused node has a way that way (an edge, a value to adjust, a group to
        /// open or leave) or the screen answers it, Enter and Backspace where the node has the
        /// behavior, Escape and any other key where the screen offers it.</summary>
        public override bool WouldHandle(InputAction action)
        {
            var node = _graph?.CurrentNode;
            switch (action.Key)
            {
                case UiActions.Up: return HasWay(node, GraphDir.Up) || ScreenOffers(action.Key);
                case UiActions.Down: return HasWay(node, GraphDir.Down) || ScreenOffers(action.Key);
                case UiActions.Left:
                case UiActions.Right:
                    return node != null && (node.Vtable.OnAdjust != null || node.Expandable || KeyGraph.InTree(node)
                        || HasWay(node, action.Key == UiActions.Left ? GraphDir.Left : GraphDir.Right)) || ScreenOffers(action.Key);
                case UiActions.Next:
                case UiActions.Prev: return StopCount() > 1;
                case UiActions.Home:
                case UiActions.End: return SiblingCount(node) > 1 || ScreenOffers(action.Key);
                case UiActions.RegionPrev:
                case UiActions.RegionNext: return node != null && node.RegionKey != null;
                case UiActions.Activate: return node != null && node.Vtable.OnActivate != null;
                case UiActions.Secondary: return node != null && node.Vtable.OnSecondary != null;
                case UiActions.Back: return ScreenOffers(ActionIds.Back);
                case UiActions.Drag: return node != null && node.Vtable.OnDrag != null;
                case UiActions.Delete: return node != null && node.Vtable.OnDelete != null;
                case UiActions.ReadFocus: return node != null;
                default: return ScreenOffers(action.Key);
            }
        }

        private static bool HasWay(GraphNode node, GraphDir dir) => node != null && node.Transitions.ContainsKey(dir);

        private bool ScreenOffers(string actionId)
        {
            if (Screen == null) return false;
            foreach (var a in Screen.GetActions())
                if (a.Id == actionId) return true;
            return false;
        }

        private int StopCount()
        {
            var render = _graph?.Current;
            if (render == null) return 0;
            var stops = new HashSet<object>();
            foreach (var n in render.Order)
                if (n.Focusable && n.StopKey != null) stops.Add(n.StopKey);
            return stops.Count;
        }

        // The focusable nodes of the focused node's Tab-stop: what Home and End travel.
        private int SiblingCount(GraphNode node)
        {
            var render = _graph?.Current;
            if (render == null || node == null) return 0;
            int count = 0;
            foreach (var n in render.Order)
                if (n.Focusable && Equals(n.StopKey, node.StopKey)) count++;
            return count;
        }

        // ---- the quiet landing ----

        // Good for a short while only: a request nothing consumed (the overlay never closed) must not
        // swallow some later landing.
        private const int QuietLandingFrames = 90;
        private int _quietLandingUntil = int.MinValue / 2;

        public override void QuietNextLanding() => _quietLandingUntil = NavInput.Current.FrameCount + QuietLandingFrames;

        private bool TakeQuietLanding()
        {
            bool quiet = NavInput.Current.FrameCount <= _quietLandingUntil;
            _quietLandingUntil = int.MinValue / 2;
            return quiet;
        }

        private static GraphDir ToDir(NavDirection dir)
        {
            switch (dir)
            {
                case NavDirection.Up: return GraphDir.Up;
                case NavDirection.Down: return GraphDir.Down;
                case NavDirection.Left: return GraphDir.Left;
                default: return GraphDir.Right;
            }
        }

        private bool Arrow(NavDirection dir)
        {
            var focusNode = _graph?.CurrentNode;
            if (focusNode == null) return false;

            // A focused slider/dropdown adjusts on Left/Right (priority over any navigation).
            if (dir == NavDirection.Left || dir == NavDirection.Right)
            {
                if (VtableAdjust(dir == NavDirection.Right ? 1 : -1)) return true;
            }

            // Edge-wired movement first (rows/grids/flattened tree rows all ride edges).
            var move = _graph.Move(ToDir(dir));
            if (move.Moved) { AnnounceMove(move); return true; }

            // At an edge. Left/Right get tree semantics: expand/collapse a group, descend into an
            // expanded one, ascend from a child.
            if (dir == NavDirection.Left || dir == NavDirection.Right)
            {
                var tr = dir == NavDirection.Right ? _graph.TreeRight() : _graph.TreeLeft();
                switch (tr.Kind)
                {
                    case KeyGraph.TreeMove.Expanded:
                    case KeyGraph.TreeMove.Collapsed:
                        SpeakFocusedState();
                        return true;
                    case KeyGraph.TreeMove.EmptyGroup:
                        Speak(Strings.Strings.NoDetails, interrupt: true);
                        return true;
                    case KeyGraph.TreeMove.Descended:
                    case KeyGraph.TreeMove.Ascended:
                        AnnounceMove(tr.Move);
                        return true;
                    case KeyGraph.TreeMove.Leaf:
                        return true; // inside a tree; nothing that way: consume
                }
            }

            // Nothing moved: consume edges inside trees; bubble from plain lists so an unfocused
            // screen's arrows fall through to the game-layer handlers.
            return KeyGraph.InTree(focusNode);
        }

        // Speak the focused group's post-toggle state (its full readout includes expanded/collapsed) and
        // rebaseline the differ+live watch so the toggle is not re-announced.
        private void SpeakFocusedState()
        {
            var node = _graph.CurrentNode;
            if (node == null) return;
            Speak(GraphAnnouncer.LeafText(node), interrupt: true);
            _lastSpokenKey = node.Id;
            _lastSpokenNode = node;
            _liveKey = null;
        }

        private bool Tab(int step)
        {
            // Snapshot BEFORE rerendering: Reconcile auto-seats a null cursor at the start node, and an
            // unfocused screen's Tab must enter at the first stop, not step from that phantom seat.
            bool wasUnfocused = _state.CurKey == null;
            if (_graph == null || !_graph.Rerender())
            {
                // No focusable content: on an unfocused-capable screen Tab has nothing to enter.
                return false;
            }

            var stops = new List<object>();
            foreach (var n in _graph.Current.Order)
                if (n.StopKey != null && !stops.Contains(n.StopKey)) stops.Add(n.StopKey);
            if (stops.Count == 0) return false;

            var curNode = wasUnfocused ? null : _graph.CurrentNode;
            int idx = curNode != null ? stops.IndexOf(curNode.StopKey) : -1;

            if (idx < 0)
            {
                // Unfocused: Tab enters at the first/last stop.
                return LandOnStop(stops[step >= 0 ? 0 : stops.Count - 1]);
            }

            int ni = idx + step;
            if (ni < 0 || ni >= stops.Count)
            {
                if (Screen != null && Screen.StartUnfocused)
                {
                    Blur(); // truly unfocused: a later re-entry stays with the game
                    if (!string.IsNullOrEmpty(Screen.ScreenName)) Speak(Screen.ScreenName, interrupt: true);
                    return true;
                }
                if (Screen != null && Screen.Wrap)
                    ni = ((ni % stops.Count) + stops.Count) % stops.Count;
                else
                    return true; // at the end; consume, no wrap
            }
            return LandOnStop(stops[ni]);
        }

        // Land on a stop (Tab cycling).
        private bool LandOnStop(object stopKey)
        {
            // Remembered position, SELECTED member, first node (the shared StopLanding).
            var land = KeyGraph.StopLanding(_graph.Current, _graph.State, stopKey);
            if (land == null || !_graph.Focus(land.Id)) return true;

            var node = _graph.CurrentNode;
            PlayHover(node);
            FireFocus(node);
            Speak(ComposeMove(_lastSpokenNode, node, entry: false), interrupt: true);
            _lastSpokenKey = node.Id;
            _lastSpokenNode = node;
            return true;
        }

        // The landed node's own hover sound when it declares one, else the default.
        private static void PlayHover(GraphNode node)
        {
            var custom = node?.Vtable?.HoverSound;
            if (custom != null) { try { custom(); } catch { } }
            else Navigation.HoverSound?.Invoke();
        }

        private bool JumpEdge(bool first)
        {
            var focusNode = _graph?.CurrentNode;
            if (focusNode == null) return false;

            // In a tree: first/last sibling at the current depth.
            if (KeyGraph.InTree(focusNode))
            {
                var sib = _graph.MoveToSiblingEdge(first);
                if (sib.Moved) AnnounceMove(sib);
                return true;
            }

            // First/last along the vertical axis of the current structure. Already there, or nothing
            // wired that way: not consumed, so the screen may answer the key itself.
            var move = _graph.MoveToEdge(first ? GraphDir.Up : GraphDir.Down);
            if (move.Moved) AnnounceMove(move);
            return move.Moved;
        }

        private bool RegionJump(int dir)
        {
            var result = _graph.MoveRegion(dir);
            if (!result.Moved) return true; // no region that way: consume
            AnnounceMove(result, regionEntry: true);
            return true;
        }

        private void AnnounceMove(MoveResult result, bool regionEntry = false)
        {
            var node = result.To;
            if (node == null) return;
            PlayHover(node);
            FireFocus(node); // before speaking, so a tab that selects itself is read as selected
            Speak(ComposeMove(result.From, node, entry: false, transitionLabel: result.TransitionLabel, regionEntry: regionEntry), interrupt: true);
            _lastSpokenKey = node.Id;
            _lastSpokenNode = node;
        }

        // The landed node's focus hook (a tab selecting itself); a throwing hook must not break navigation.
        private static void FireFocus(GraphNode node)
        {
            var hook = node?.Vtable?.OnFocus;
            if (hook == null) return;
            try { hook(); }
            catch (Exception e) { CoreLog.Warning("Navigator: focus hook failed: " + e.Message); }
        }

        // Run the focused node's vtable activation; speak its StateText as immediate feedback when it
        // declares one, and rebaseline the live watch so the same change is not spoken twice.
        private bool VtableActivate()
        {
            var node = _graph.CurrentNode;
            if (node?.Vtable.OnActivate == null) return false;
            _graph.Activate();
            node = _graph.CurrentNode;
            var st = node?.Vtable.StateText;
            if (st != null)
            {
                Speak(st(), interrupt: true);
                _liveKey = null; // rebaseline: the change was just spoken synchronously
            }
            return true;
        }

        private bool VtableAdjust(int sign)
        {
            var node = _graph.CurrentNode;
            if (node?.Vtable.OnAdjust == null) return false;
            _graph.TryAdjust(sign, large: false);
            node = _graph.CurrentNode;
            var st = node?.Vtable.StateText;
            if (st != null)
            {
                Speak(st(), interrupt: true);
                _liveKey = null; // rebaseline: the change was just spoken synchronously
            }
            return true;
        }

        // ---- composition ----

        private string ComposeMove(GraphNode from, GraphNode to, bool entry, string transitionLabel = null, bool regionEntry = false)
        {
            return GraphAnnouncer.Compose(entry ? null : from, to, transitionLabel);
        }

        // ---- type-ahead search (landing goes via the graph) ----

        private readonly TypeAheadSearch _search = new TypeAheadSearch();
        private readonly List<GraphNode> _searchNodes = new List<GraphNode>(); // the focused stop's nodes
        private ControlId _searchFocusId; // where the last result landed (staleness check)
        private Screens.Screen _lastTypeaheadScreen;
        private int _searchHeldDir;
        private float _searchRepeatIn;

        public override void TickTypeahead()
        {
            var input = NavInput.Current;
            if (Screen == null || Screen.CapturesRawInput || !Screen.AllowsTypeahead
                || !Navigation.FocusActive() || _graph?.CurrentNode == null)
            {
                if (_search.IsSearchActive || _search.HasBuffer) ClearSearch(announce: false);
                _lastTypeaheadScreen = Screen;
                return;
            }

            if (!ReferenceEquals(Screen, _lastTypeaheadScreen))
            {
                _lastTypeaheadScreen = Screen;
                if (_search.IsSearchActive || _search.HasBuffer) ClearSearch(announce: false);
                return;
            }

            if (input.CtrlHeld || input.AltHeld)
                return;

            bool shift = input.ShiftHeld;
            if (_search.IsSearchActive && !shift)
            {
                if (input.EscapeDown) { ClearSearch(announce: true); return; }
                if (_search.ResultCount > 0 && TickResultArrows(input)) return;
            }
            else
            {
                _searchHeldDir = 0;
            }

            var typed = input.TypedText;
            if (string.IsNullOrEmpty(typed)) return;

            foreach (var ch in typed)
            {
                if (char.IsLetter(ch)) TypeChar(ch);
                else if (ch == ' ' && _search.HasBuffer) TypeChar(ch);
            }
        }

        private bool TickResultArrows(INavInput input)
        {
            int dir = input.UpHeld ? -1 : input.DownHeld ? 1 : 0;
            if (dir == 0) { _searchHeldDir = 0; return false; }

            if (dir != _searchHeldDir)
            {
                _searchHeldDir = dir;
                _searchRepeatIn = OsKeyboard.InitialDelay;
                _search.NavigateResults(dir);
                return true;
            }

            _searchRepeatIn -= input.UnscaledDeltaTime;
            if (_searchRepeatIn <= 0f)
            {
                _searchRepeatIn = OsKeyboard.RepeatInterval;
                _search.NavigateResults(dir);
            }
            return true;
        }

        // An action that fired from a key type-ahead owns while a search is live (a bare letter, space,
        // up/down, escape) is swallowed here: TickTypeahead handles that key itself.
        private static bool FiredFromSearchKey(InputAction action)
        {
            foreach (var b in action.Bindings)
                if (b.ConflictsWithTypeahead && b.Held()) return true;
            return false;
        }

        private void TypeChar(char c)
        {
            // A fresh search remembers the column you are on: every result lands there (row changes,
            // column does not). Captured before the first result moves focus to a primary.
            if (!_search.HasBuffer)
                _searchColumn = _graph?.CurrentNode?.Vtable?.Column ?? -1;
            RebuildSearchScope();
            if (_searchNodes.Count > 0)
            {
                _search.AddChar(c);
                _search.Search(_searchNodes.Count, i => SearchTextOf(_searchNodes[i]), SearchFocusNodeResult);
            }
        }

        // The node's type-ahead text, STRIPPED of rich-text markup: node labels are raw game strings.
        private static string SearchTextOf(GraphNode n)
        {
            string t;
            if (n.Vtable.SearchText != null) t = n.Vtable.SearchText();
            else
            {
                var first = n.Vtable.Announcements != null && n.Vtable.Announcements.Count > 0
                    ? n.Vtable.Announcements[0] : null;
                t = first?.Text?.Invoke();
            }
            return t == null ? null : TextUtil.StripRichText(t);
        }

        private void RebuildSearchScope()
        {
            _searchNodes.Clear();
            // The searchable scope is the focused node's Tab-stop. Tabular rows contribute ONE result,
            // the primary (metadata cells all match the row's name).
            var node = _graph?.CurrentNode;
            if (node == null || _graph.Current == null) return;
            foreach (var n in _graph.Current.Order)
                if (Equals(n.StopKey, node.StopKey) && !n.Vtable.ExcludeFromSearch && n.Vtable.Column <= 0)
                    _searchNodes.Add(n);
        }

        // The tabular column focus was on when the search began: results land ON THE MATCHED ROW at
        // this column, so searching never yanks you out of the column you were scanning.
        private int _searchColumn = -1;

        private void SearchFocusNodeResult(int index)
        {
            if (index < 0 || index >= _searchNodes.Count) return;
            if (!_graph.FocusAtColumn(_searchNodes[index].Id, _searchColumn)) return;
            var node = _graph.CurrentNode;
            PlayHover(node);
            FireFocus(node); // a result is a landing like any other: a radio found by typing is chosen
            Speak(ComposeMove(_lastSpokenNode, node, entry: false), interrupt: true);
            _lastSpokenKey = node.Id;
            _lastSpokenNode = node;
            _searchFocusId = node.Id; // the staleness check clears results if focus moves off this
        }

        private void ClearSearch(bool announce)
        {
            bool had = _search.IsSearchActive || _search.HasBuffer;
            _search.Clear();
            _searchNodes.Clear();
            _searchFocusId = null;
            _searchHeldDir = 0;
            _searchColumn = -1;
            if (announce && had) Speak(Strings.Strings.SearchCleared, interrupt: true);
        }
    }

    /// <summary>The UI-category action keys the navigator understands (the input registrations use them).</summary>
    public static class UiActions
    {
        public const string Up = "ui.up";
        public const string Down = "ui.down";
        public const string Left = "ui.left";
        public const string Right = "ui.right";
        public const string Next = "ui.next";
        public const string Prev = "ui.prev";
        public const string Home = "ui.home";
        public const string End = "ui.end";
        public const string RegionPrev = "ui.regionPrev";
        public const string RegionNext = "ui.regionNext";
        public const string Activate = "ui.activate";
        public const string Secondary = "ui.secondary";
        public const string Back = "ui.back";
        public const string Drag = "ui.drag";
        public const string Delete = "ui.delete";
        public const string ReadFocus = "ui.readFocus";
    }
}

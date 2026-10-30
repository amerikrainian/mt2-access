using System;
using System.Collections.Generic;

namespace MonsterTrainAccessibility.Core.UI
{
    /// <summary>What a <see cref="RouteCursor{T}"/> step did.</summary>
    public enum RouteMove
    {
        /// <summary>The cursor moved: <see cref="RouteStep{T}.Stop"/> is where it now is.</summary>
        Moved,
        /// <summary>Nothing follows the cursor's stop: it stayed.</summary>
        NoForward,
        /// <summary>The cursor is back where it started: it stayed.</summary>
        NoBackward,
        /// <summary>No sibling that way (or none at all): it stayed.</summary>
        Edge,
    }

    /// <summary>One step of a <see cref="RouteCursor{T}"/>: where it is, what happened, and whether the
    /// stop is one of several the route forks into here and was just arrived at (the moment to say it is
    /// a choice; cycling among the siblings is not an arrival).</summary>
    public struct RouteStep<T>
    {
        public T Stop;
        public RouteMove Move;
        public bool ArrivedAtFork;
        public int Index;
        public int Count;
    }

    /// <summary>
    /// A preview cursor over a route that only goes forward and forks (a run's map): from an origin it
    /// steps on to what follows (the first of several when the route forks), cycles among a fork's
    /// siblings, and steps back the way it came. Engine-free: the route is a function from a stop to the
    /// stops that follow it, read fresh at every forward step, so the cursor holds no picture of the
    /// route beyond the path it walked.
    /// </summary>
    public sealed class RouteCursor<T> where T : class
    {
        private struct Frame
        {
            public T Stop;
            public IReadOnlyList<T> Siblings;
            public int Index;
        }

        private readonly Func<T, IReadOnlyList<T>> _next;
        private readonly List<Frame> _path = new List<Frame>();
        private IReadOnlyList<T> _siblings = Array.Empty<T>();
        private int _index;

        public RouteCursor(Func<T, IReadOnlyList<T>> next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        /// <summary>Where the cursor is; null before the first <see cref="Reset"/>.</summary>
        public T Current { get; private set; }

        /// <summary>How many steps forward of its origin the cursor is.</summary>
        public int Depth => _path.Count;

        /// <summary>Start over at a stop (the one the player stands on): no path, no siblings.</summary>
        public void Reset(T origin) => Reset(origin, null);

        /// <summary>Start over at a stop that is one of several to choose among now (the choice in
        /// focus): cycling walks them, as at any fork ahead.</summary>
        public void Reset(T origin, IReadOnlyList<T> siblings)
        {
            Current = origin;
            _path.Clear();
            int index = siblings != null ? IndexOf(siblings, origin) : -1;
            _siblings = index >= 0 ? siblings : Array.Empty<T>();
            _index = index >= 0 ? index : 0;
        }

        /// <summary>Start over at a stop and walk on to one ahead of it: the path walked is the one
        /// <see cref="Back"/> retraces, each fork left at the branch that leads there. False, the cursor
        /// left at the origin, when no stop within <paramref name="maxDepth"/> steps ahead is the target.</summary>
        public bool ResetAlong(T origin, Func<T, bool> isTarget, int maxDepth)
        {
            if (isTarget == null) throw new ArgumentNullException(nameof(isTarget));
            Reset(origin);
            var turns = new List<int>();
            if (!Find(origin, isTarget, maxDepth, turns)) return false;
            foreach (int turn in turns)
            {
                Forward();
                for (int i = 0; i < turn; i++) Cycle(1);
            }
            return true;
        }

        // The sibling taken at each step from a stop to the first target ahead of it, depth first.
        private bool Find(T stop, Func<T, bool> isTarget, int depth, List<int> turns)
        {
            if (depth <= 0) return false;
            var next = _next(stop);
            if (next == null) return false;
            for (int i = 0; i < next.Count; i++)
            {
                turns.Add(i);
                if (isTarget(next[i]) || Find(next[i], isTarget, depth - 1, turns)) return true;
                turns.RemoveAt(turns.Count - 1);
            }
            return false;
        }

        private static int IndexOf(IReadOnlyList<T> list, T item)
        {
            for (int i = 0; i < list.Count; i++)
                if (ReferenceEquals(list[i], item) || Equals(list[i], item)) return i;
            return -1;
        }

        /// <summary>On to what follows the current stop: the first of them when the route forks.</summary>
        public RouteStep<T> Forward()
        {
            var next = _next(Current);
            if (next == null || next.Count == 0) return Stay(RouteMove.NoForward);
            _path.Add(new Frame { Stop = Current, Siblings = _siblings, Index = _index });
            _siblings = next;
            _index = 0;
            Current = next[0];
            return Arrive();
        }

        /// <summary>Back to the stop this one was reached from, among its own siblings.</summary>
        public RouteStep<T> Back()
        {
            if (_path.Count == 0) return Stay(RouteMove.NoBackward);
            var frame = _path[_path.Count - 1];
            _path.RemoveAt(_path.Count - 1);
            Current = frame.Stop;
            _siblings = frame.Siblings;
            _index = frame.Index;
            return Arrive();
        }

        /// <summary>To the next (+1) or previous (-1) sibling at this fork; no wrap.</summary>
        public RouteStep<T> Cycle(int direction)
        {
            int target = _index + Math.Sign(direction);
            if (_siblings.Count <= 1 || target < 0 || target >= _siblings.Count) return Stay(RouteMove.Edge);
            _index = target;
            Current = _siblings[_index];
            return new RouteStep<T> { Stop = Current, Move = RouteMove.Moved, Index = _index, Count = _siblings.Count };
        }

        private RouteStep<T> Arrive() => new RouteStep<T>
        {
            Stop = Current,
            Move = RouteMove.Moved,
            ArrivedAtFork = _siblings.Count > 1,
            Index = _index,
            Count = _siblings.Count,
        };

        private RouteStep<T> Stay(RouteMove move) => new RouteStep<T>
        {
            Stop = Current,
            Move = move,
            Index = _index,
            Count = _siblings.Count,
        };
    }
}

using System;
using System.Collections.Generic;
using MonsterTrainAccessibility.Core;
using UnityEngine;

namespace MonsterTrainAccessibility.Module.UI
{
    /// <summary>Actions run a few frames on: a key's feedback that has to wait for the game to redraw
    /// (the shop's rows after a reroll, a button's caption after its toggle). Ticked by the module;
    /// what is pending dies with the generation.</summary>
    internal static class Later
    {
        private struct Pending { public int Frame; public Action Action; public string Name; }

        private static readonly List<Pending> _pending = new List<Pending>();

        public static void Frames(int frames, Action action, string name)
            => _pending.Add(new Pending { Frame = Time.frameCount + frames, Action = action, Name = name });

        public static void Tick()
        {
            if (_pending.Count == 0) return;
            int now = Time.frameCount;
            for (int i = 0; i < _pending.Count; i++)
            {
                var p = _pending[i];
                if (now < p.Frame) continue;
                _pending.RemoveAt(i--);
                try { p.Action(); }
                catch (Exception e) { CoreLog.Warning("Later: " + p.Name + " failed: " + e.Message); }
            }
        }
    }
}

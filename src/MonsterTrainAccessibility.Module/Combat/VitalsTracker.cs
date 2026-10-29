using System;
using System.Collections.Generic;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Core.Events;
using MonsterTrainAccessibility.Core.Strings;

namespace MonsterTrainAccessibility.Module.Combat
{
    /// <summary>
    /// Each unit's health and statuses, watched through the unit's own signals: a heal is reported at
    /// once ("Fel healed 5."), a status change as its delta ("Fel Rage +3."). A health loss waits for the
    /// game's damage signal, which follows it in the same call when the loss is damage: the loss is then
    /// "took 10 damage" (the hit as dealt, overkill included); a loss no damage claims (a maximum lowered,
    /// health set) is reported by its delta at the end of the frame. A damage signal claims the loss only
    /// when the hit accounts for it, so damage to an untracked unit (the pyre) never lands on another.
    /// </summary>
    internal sealed class VitalsTracker
    {
        private readonly Dictionary<CharacterState, Tracked> _tracked = new Dictionary<CharacterState, Tracked>();
        private readonly List<Loss> _losses = new List<Loss>();
        private CombatManager _combat;

        public void Start(CombatManager combat)
        {
            _combat = combat;
            _combat?.characterDamageSignal.AddListener(OnDamage);
        }

        public void Stop()
        {
            _combat?.characterDamageSignal.RemoveListener(OnDamage);
            _combat = null;
            foreach (var t in _tracked.Values) Unsubscribe(t);
            _tracked.Clear();
            _losses.Clear();
        }

        /// <summary>Every unit on the train now (both sides), at the start of a battle.</summary>
        public void Prime(AllGameManagers managers)
        {
            var units = new List<CharacterState>();
            managers.GetMonsterManager()?.AddCharactersToList(units);
            managers.GetHeroManager()?.AddCharactersToList(units);
            foreach (var unit in units) Track(unit);
        }

        public static bool Skip(CharacterState c) => c == null || c.PreviewMode || c.SpawnedInPreviewMode || c.IsDestroyed;

        public void Track(CharacterState unit)
        {
            if (Skip(unit) || unit.IsPyreHeart() || _tracked.ContainsKey(unit)) return;
            var t = new Tracked(this, unit);
            _tracked[unit] = t;
            unit.AddHpChangedListener(t.HpChanged);
            unit.AddStatusEffectChangedListener(t.StatusChanged);
        }

        public void Remove(CharacterState unit)
        {
            if (unit == null || !_tracked.TryGetValue(unit, out var t)) return;
            Unsubscribe(t);
            _tracked.Remove(unit);
            _losses.RemoveAll(l => ReferenceEquals(l.Unit, unit));
        }

        /// <summary>The losses no damage claimed this frame, by their delta.</summary>
        public void FlushPending()
        {
            if (_losses.Count == 0) return;
            var pending = _losses.ToArray();
            _losses.Clear();
            foreach (var loss in pending)
                if (_tracked.ContainsKey(loss.Unit)) Hp(loss.Unit, loss.Old, loss.New);
        }

        private static void Unsubscribe(Tracked t)
        {
            try
            {
                var state = t.Unit.PrimaryStateInformation;
                state?.hpChangedSignal.RemoveListener(t.HpChanged);
                state?.statusEffectChangedSignal.RemoveListener(t.StatusChanged);
            }
            catch (Exception e) { CoreLog.Warning("vitals: unsubscribing " + Name(t.Unit) + " failed: " + e.Message); }
        }

        private void OnHp(Tracked t, CharacterState unit, int hp)
        {
            if (Skip(unit) || !ReferenceEquals(unit, t.Unit)) return;
            int old = t.Hp;
            if (old == hp) return;
            t.Hp = hp;
            if (hp > old) Hp(unit, old, hp);
            else _losses.Add(new Loss { Unit = unit, Old = old, New = hp });
        }

        // The hit just dealt: the latest loss it accounts for (health down by the hit, or to 0 by it).
        private void OnDamage(int damage, CharacterState.ApplyDamageParams damageParams)
        {
            if (damage <= 0) return;
            for (int i = _losses.Count - 1; i >= 0; i--)
            {
                var loss = _losses[i];
                if (loss.Old - loss.New != Math.Min(damage, loss.Old)) continue;
                _losses.RemoveAt(i);
                if (!_tracked.ContainsKey(loss.Unit)) return;
                string name = Name(loss.Unit);
                GameEvents.Raise(EventKinds.Damage, damage == 1 ? Strings.Event("damaged_one", name) : Strings.Event("damaged", name, damage), Side(loss.Unit));
                return;
            }
        }

        private static void Hp(CharacterState unit, int old, int hp)
        {
            string name = Name(unit);
            int delta = hp - old;
            if (delta < 0)
                GameEvents.Raise(EventKinds.Hp, -delta == 1 ? Strings.Event("damaged_one", name) : Strings.Event("damaged", name, -delta), Side(unit));
            else if (delta > 0)
                GameEvents.Raise(EventKinds.Hp, delta == 1 ? Strings.Event("healed_one", name) : Strings.Event("healed", name, delta), Side(unit));
        }

        private void OnStatus(Tracked t, string statusId, int count)
        {
            var unit = t.Unit;
            if (Skip(unit) || string.IsNullOrWhiteSpace(statusId) || Hidden(statusId)) return;
            t.Statuses.TryGetValue(statusId, out int old);
            if (old == count) return;
            if (count > 0) t.Statuses[statusId] = count;
            else t.Statuses.Remove(statusId);
            string status = StatusName(statusId);
            int delta = count - old;
            GameEvents.Raise(EventKinds.Status, delta > 0
                ? Strings.Event("status_increased", Name(unit), status, delta)
                : Strings.Event("status_decreased", Name(unit), status, -delta), Side(unit));
        }

        private static bool Hidden(string statusId)
        {
            var data = AllGameManagers.Instance?.GetStatusEffectManager()?.GetStatusEffectDataById(statusId, expectToFind: false);
            return data != null && data.IsHidden();
        }

        internal static string StatusName(string statusId)
        {
            try
            {
                string name = StatusEffectManager.GetLocalizedName(statusId, 1, inBold: false, showStacks: false);
                return string.IsNullOrWhiteSpace(name) ? statusId : name;
            }
            catch (Exception e)
            {
                CoreLog.Warning("vitals: no name for status " + statusId + ": " + e.Message);
                return statusId;
            }
        }

        internal static string Name(CharacterState unit) => unit?.GetName();

        /// <summary>The side a unit fights on, for the events' source filter.</summary>
        internal static EventSide Side(CharacterState unit)
        {
            if (unit == null) return EventSide.None;
            return unit.IsPyreHeart() || unit.GetTeamType() == Team.Type.Monsters ? EventSide.Friendly : EventSide.Enemy;
        }

        private struct Loss
        {
            public CharacterState Unit;
            public int Old;
            public int New;
        }

        private sealed class Tracked
        {
            public readonly CharacterState Unit;
            public int Hp;
            public readonly Dictionary<string, int> Statuses = new Dictionary<string, int>();
            public readonly Action<CharacterState, int> HpChanged;
            public readonly Action<string, int> StatusChanged;

            public Tracked(VitalsTracker owner, CharacterState unit)
            {
                Unit = unit;
                Hp = unit.GetHP();
                var stacks = new List<CharacterState.StatusEffectStack>();
                unit.GetStatusEffects(ref stacks);
                foreach (var stack in stacks)
                    if (stack?.State != null && stack.Count > 0 && !stack.State.IsHidden())
                        Statuses[stack.State.GetStatusId()] = stack.Count;
                HpChanged = (c, hp) => owner.OnHp(this, c, hp);
                StatusChanged = (id, count) => owner.OnStatus(this, id, count);
            }
        }
    }
}

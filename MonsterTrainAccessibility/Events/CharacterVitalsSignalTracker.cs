using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Core;
using ShinyShoe;

namespace MonsterTrainAccessibility.Events
{
    internal sealed class CharacterVitalsSignalTracker
    {
        private static readonly PropertyInfo PrimaryStateInformationProperty =
            AccessTools.Property(typeof(CharacterState), "PrimaryStateInformation")!;

        private static readonly FieldInfo HpChangedSignalField =
            AccessTools.Field(PrimaryStateInformationProperty.PropertyType, "hpChangedSignal")!;

        private static readonly FieldInfo StatusEffectChangedSignalField =
            AccessTools.Field(PrimaryStateInformationProperty.PropertyType, "statusEffectChangedSignal")!;

        private readonly AllGameManagers _managers;
        private readonly MonsterManager _monsterManager;
        private readonly HeroManager _heroManager;
        private readonly CombatManager _combatManager;
        private readonly Dictionary<CharacterState, TrackedCharacter> _tracked =
            new Dictionary<CharacterState, TrackedCharacter>();
        private readonly List<CharacterState> _characterBuffer = new List<CharacterState>();
        private readonly List<PendingHpLoss> _pendingHpLosses = new List<PendingHpLoss>();
        private bool _started;

        public CharacterVitalsSignalTracker(AllGameManagers managers)
        {
            _managers = managers;
            _monsterManager = managers?.GetMonsterManager();
            _heroManager = managers?.GetHeroManager();
            _combatManager = managers?.GetCombatManager();
        }

        public void Start()
        {
            if (_started)
            {
                return;
            }

            _started = true;
            _combatManager?.characterDamageSignal.AddListener(OnCharacterDamage);
        }

        public void Stop()
        {
            if (!_started)
            {
                return;
            }

            _started = false;
            _combatManager?.characterDamageSignal.RemoveListener(OnCharacterDamage);
            Clear();
        }

        public void Prime()
        {
            _characterBuffer.Clear();
            AddCharacters(_characterBuffer);
            for (int i = 0; i < _characterBuffer.Count; i++)
            {
                Track(_characterBuffer[i]);
            }
        }

        public void Track(CharacterState character)
        {
            if (ShouldSkip(character) || character.IsPyreHeart() || _tracked.ContainsKey(character))
            {
                return;
            }

            TrackedCharacter tracked = new TrackedCharacter(this, character);
            _tracked[character] = tracked;
            character.AddHpChangedListener(tracked.HpChanged);
            character.AddStatusEffectChangedListener(tracked.StatusChanged);
        }

        public void Remove(CharacterState character)
        {
            if (character == null || !_tracked.TryGetValue(character, out TrackedCharacter tracked))
            {
                return;
            }

            Unsubscribe(tracked);
            _tracked.Remove(character);
            RemovePending(character);
        }

        public void Clear()
        {
            foreach (TrackedCharacter tracked in _tracked.Values)
            {
                Unsubscribe(tracked);
            }

            _tracked.Clear();
            _pendingHpLosses.Clear();
            _characterBuffer.Clear();
        }

        public void FlushPending()
        {
            while (_pendingHpLosses.Count > 0)
            {
                PendingHpLoss pending = _pendingHpLosses[0];
                _pendingHpLosses.RemoveAt(0);
                if (_tracked.ContainsKey(pending.Character))
                {
                    EventDispatcher.Enqueue(new HpChangedEvent(pending.Character, pending.OldHp, pending.NewHp));
                }
            }
        }

        public static bool ShouldSkip(CharacterState character)
        {
            return character == null || character.PreviewMode || character.SpawnedInPreviewMode || character.IsDestroyed;
        }

        private void OnHpChanged(CharacterState character, int newHp)
        {
            if (!_started || ShouldSkip(character) || !_tracked.TryGetValue(character, out TrackedCharacter tracked))
            {
                return;
            }

            int oldHp = tracked.Hp;
            if (oldHp == newHp)
            {
                return;
            }

            tracked.Hp = newHp;
            if (newHp > oldHp)
            {
                EventDispatcher.Enqueue(new HpChangedEvent(character, oldHp, newHp));
                return;
            }

            _pendingHpLosses.Add(new PendingHpLoss(character, oldHp, newHp));
        }

        private void OnStatusChanged(CharacterState character, string statusId, int newCount)
        {
            if (!_started || ShouldSkip(character) || string.IsNullOrWhiteSpace(statusId) || IsHiddenStatus(statusId) ||
                !_tracked.TryGetValue(character, out TrackedCharacter tracked))
            {
                return;
            }

            tracked.Statuses.TryGetValue(statusId, out int oldCount);
            if (oldCount == newCount)
            {
                return;
            }

            if (newCount > 0)
            {
                tracked.Statuses[statusId] = newCount;
            }
            else
            {
                tracked.Statuses.Remove(statusId);
            }

            EventDispatcher.Enqueue(new StatusChangedEvent(character, statusId, oldCount, newCount));
        }

        private void OnCharacterDamage(int damage, CharacterState.ApplyDamageParams damageParams)
        {
            if (!_started || damage <= 0)
            {
                return;
            }

            while (_pendingHpLosses.Count > 0)
            {
                int index = _pendingHpLosses.Count - 1;
                PendingHpLoss pending = _pendingHpLosses[index];
                _pendingHpLosses.RemoveAt(index);
                if (!_tracked.ContainsKey(pending.Character))
                {
                    continue;
                }

                EventDispatcher.Enqueue(new CharacterDamagedEvent(
                    pending.Character,
                    damage));
                return;
            }
        }

        private void AddCharacters(List<CharacterState> characters)
        {
            _monsterManager?.AddCharactersToList(characters);
            _heroManager?.AddCharactersToList(characters);
        }

        private void Unsubscribe(TrackedCharacter tracked)
        {
            try
            {
                object state = PrimaryStateInformationProperty.GetValue(tracked.Character);
                Signal<CharacterState, int> hpSignal = HpChangedSignalField.GetValue(state) as Signal<CharacterState, int>;
                Signal<string, int> statusSignal = StatusEffectChangedSignalField.GetValue(state) as Signal<string, int>;
                hpSignal!.RemoveListener(tracked.HpChanged);
                statusSignal!.RemoveListener(tracked.StatusChanged);
            }
            catch (Exception ex)
            {
                Log.Error("[AccessibilityMod] Failed to unsubscribe character vitals signals: " + ex);
            }
        }

        private void RemovePending(CharacterState character)
        {
            if (_pendingHpLosses.Count == 0)
            {
                return;
            }

            for (int i = _pendingHpLosses.Count - 1; i >= 0; i--)
            {
                if (ReferenceEquals(_pendingHpLosses[i].Character, character))
                {
                    _pendingHpLosses.RemoveAt(i);
                }
            }
        }

        private bool IsHiddenStatus(string statusId)
        {
            StatusEffectData data = _managers?.GetStatusEffectManager()?.GetStatusEffectDataById(statusId, expectToFind: false);
            return data != null && data.IsHidden();
        }

        private sealed class TrackedCharacter
        {
            private readonly CharacterVitalsSignalTracker _owner;

            public TrackedCharacter(CharacterVitalsSignalTracker owner, CharacterState character)
            {
                _owner = owner;
                Character = character;
                Hp = character.GetHP();
                Statuses = ReadStatuses(character);
                HpChanged = OnHpChanged;
                StatusChanged = OnStatusChanged;
            }

            public CharacterState Character { get; }
            public int Hp { get; set; }
            public Dictionary<string, int> Statuses { get; }
            public Action<CharacterState, int> HpChanged { get; }
            public Action<string, int> StatusChanged { get; }

            private void OnHpChanged(CharacterState character, int newHp)
            {
                _owner.OnHpChanged(character, newHp);
            }

            private void OnStatusChanged(string statusId, int newCount)
            {
                _owner.OnStatusChanged(Character, statusId, newCount);
            }

            private static Dictionary<string, int> ReadStatuses(CharacterState character)
            {
                Dictionary<string, int> statuses = new Dictionary<string, int>();
                List<CharacterState.StatusEffectStack> stacks = new List<CharacterState.StatusEffectStack>();
                character.GetStatusEffects(ref stacks);
                for (int i = 0; i < stacks.Count; i++)
                {
                    CharacterState.StatusEffectStack stack = stacks[i];
                    if (stack == null || stack.State == null || stack.Count <= 0 || stack.State.IsHidden())
                    {
                        continue;
                    }

                    statuses[stack.State.GetStatusId()] = stack.Count;
                }

                return statuses;
            }
        }

        private readonly struct PendingHpLoss
        {
            public PendingHpLoss(CharacterState character, int oldHp, int newHp)
            {
                Character = character;
                OldHp = oldHp;
                NewHp = newHp;
            }

            public CharacterState Character { get; }
            public int OldHp { get; }
            public int NewHp { get; }
        }
    }
}

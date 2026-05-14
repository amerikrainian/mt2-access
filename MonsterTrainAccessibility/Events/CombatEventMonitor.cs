using System.Collections;
using System.Collections.Generic;
using System;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Localization;
using ShinyShoe;

namespace MonsterTrainAccessibility.Events
{
    internal sealed class CombatEventMonitor : ICharacterNotifications
    {
        private readonly CombatManager _combatManager;
        private readonly CardManager _cardManager;
        private readonly PlayerManager _playerManager;
        private readonly SaveManager _saveManager;
        private readonly MonsterManager _monsterManager;
        private readonly HeroManager _heroManager;
        private readonly CharacterVitalsSignalTracker _vitalsTracker;
        private readonly HashSet<CharacterState> _announcedDeaths = new HashSet<CharacterState>();
        private readonly HashSet<string> _announcedRelicTriggers = new HashSet<string>();
        private readonly HashSet<string> _announcedCardModifierRelicTriggers = new HashSet<string>();
        private int _lastTurn = -1;
        private int _lastTurnModifier;
        private CombatManager.Phase _lastPhase;
        private PlayerManager.MoonPhase _lastMoonPhase;
        private bool _hasLastMoonPhase;
        private bool _started;

        public CombatEventMonitor(AllGameManagers managers, CharacterVitalsSignalTracker vitalsTracker)
        {
            _combatManager = managers?.GetCombatManager();
            _cardManager = managers?.GetCardManager();
            _playerManager = managers?.GetPlayerManager();
            _saveManager = managers?.GetSaveManager();
            _monsterManager = managers?.GetMonsterManager();
            _heroManager = managers?.GetHeroManager();
            _vitalsTracker = vitalsTracker;
        }

        public void Start()
        {
            if (_started)
            {
                return;
            }

            _started = true;

            _combatManager?.turnCountSignal.AddListener(OnTurnCountChanged);
            _combatManager?.combatPhaseSignal.AddListener(OnCombatPhaseChanged);
            _combatManager?.pyreArmorChangedSignal.AddListener(OnPyreArmorChanged);
            _combatManager?.characterAbilityAvailableSignal.AddListener(OnAbilityAvailable);
            _combatManager?.characterAbilityUnavailableSignal.AddListener(OnAbilityUnavailable);
            _combatManager?.characterRemovedSignal.AddListener(OnCombatCharacterRemoved);
            Log.Info("[AccessibilityMod] CombatEventMonitor subscribed to CombatManager.characterRemovedSignal.");

            _cardManager?.deckShuffledSignal.AddListener(OnDeckShuffled);

            _playerManager?.energyChangedSignal.AddListener(OnEnergyChanged);
            _playerManager?.moonPhaseChangedSignal.AddListener(OnMoonPhaseChanged);
            if (_playerManager != null)
            {
                _lastMoonPhase = _playerManager.CurrentMoonPhase;
                _hasLastMoonPhase = true;
            }

            _saveManager?.pyreHPChangedSignal.AddListener(OnPyreHpChanged);
            RelicManager.RelicTriggered.AddListener(OnRelicTriggered);

            _monsterManager?.AddCharactersNotification(this);
            _heroManager?.AddCharactersNotification(this);
        }

        public void Stop()
        {
            if (!_started)
            {
                return;
            }

            _started = false;

            _combatManager?.turnCountSignal.RemoveListener(OnTurnCountChanged);
            _combatManager?.combatPhaseSignal.RemoveListener(OnCombatPhaseChanged);
            _combatManager?.pyreArmorChangedSignal.RemoveListener(OnPyreArmorChanged);
            _combatManager?.characterAbilityAvailableSignal.RemoveListener(OnAbilityAvailable);
            _combatManager?.characterAbilityUnavailableSignal.RemoveListener(OnAbilityUnavailable);
            _combatManager?.characterRemovedSignal.RemoveListener(OnCombatCharacterRemoved);
            Log.Info("[AccessibilityMod] CombatEventMonitor unsubscribed from CombatManager.characterRemovedSignal.");

            _cardManager?.deckShuffledSignal.RemoveListener(OnDeckShuffled);

            _playerManager?.energyChangedSignal.RemoveListener(OnEnergyChanged);
            _playerManager?.moonPhaseChangedSignal.RemoveListener(OnMoonPhaseChanged);
            _saveManager?.pyreHPChangedSignal.RemoveListener(OnPyreHpChanged);
            RelicManager.RelicTriggered.RemoveListener(OnRelicTriggered);

            _monsterManager?.RemoveCharactersNotification(this);
            _heroManager?.RemoveCharactersNotification(this);
            _announcedDeaths.Clear();
            _announcedRelicTriggers.Clear();
            _announcedCardModifierRelicTriggers.Clear();
            _hasLastMoonPhase = false;
        }

        public IEnumerator CharacterAdded(CharacterState character, CardState fromCard)
        {
            if (!ShouldSkip(character))
            {
                _vitalsTracker.Track(character);
                EventDispatcher.Enqueue(new CharacterSpawnedEvent(character));
            }

            yield break;
        }

        public IEnumerator CharacterRemoved(CharacterState character, SpawnPoint usedPoint)
        {
            if (character != null)
            {
                _vitalsTracker.Remove(character);
            }

            yield break;
        }

        public void CharacterSpawnPointChanged(CharacterState character, SpawnPoint prev, SpawnPoint current)
        {
            if (CharacterVitalsSignalTracker.ShouldSkip(character) || prev == null || current == null)
            {
                return;
            }

            int oldFloor = prev.GetRoomOwner()?.GetRoomIndex() + 1 ?? 0;
            int newFloor = current.GetRoomOwner()?.GetRoomIndex() + 1 ?? 0;
            if (oldFloor > 0 && newFloor > 0 && oldFloor != newFloor)
            {
                EventDispatcher.Enqueue(new CharacterMovedEvent(character, oldFloor, newFloor));
            }
        }

        public void CharacterStatusEffectApplied(CharacterState character, StatusEffectState statusEffect, bool spawnEffect)
        {
            // Status deltas are announced from CharacterState.statusEffectChangedSignal.
        }

        public IEnumerator NoMoreHeroes()
        {
            EventDispatcher.Enqueue(new BasicEvent(Message.Localized("events", "COMBAT.NO_MORE_ENEMIES")));
            yield break;
        }

        private static bool ShouldSkip(CharacterState character)
        {
            return CharacterVitalsSignalTracker.ShouldSkip(character);
        }

        private void OnTurnCountChanged(int turnCounter, int turnModifier, int totalWaves, bool isLoopingScenario)
        {
            if (_lastTurn == turnCounter && _lastTurnModifier == turnModifier)
            {
                return;
            }

            _lastTurn = turnCounter;
            _lastTurnModifier = turnModifier;
            _announcedRelicTriggers.Clear();
            EventDispatcher.Enqueue(new TurnEvent(turnCounter + turnModifier, totalWaves, isLoopingScenario));
        }

        private void OnCombatPhaseChanged(CombatManager.Phase phase)
        {
            if (_lastPhase != phase)
            {
                _lastPhase = phase;
                _announcedRelicTriggers.Clear();
            }

            switch (phase)
            {
                case CombatManager.Phase.MonsterTurn:
                    EventDispatcher.Enqueue(new BasicEvent(Message.Localized("events", "COMBAT.PLAYER_ACTION")));
                    break;
                case CombatManager.Phase.Combat:
                    EventDispatcher.Enqueue(new BasicEvent(Message.Localized("events", "COMBAT.ROOM_COMBAT")));
                    break;
                case CombatManager.Phase.HeroTurn:
                    EventDispatcher.Enqueue(new BasicEvent(Message.Localized("events", "COMBAT.ENEMY_TURN")));
                    break;
                case CombatManager.Phase.EndOfCombat:
                    EventDispatcher.Enqueue(new BasicEvent(Message.Localized("events", "COMBAT.ENDED")));
                    break;
            }
        }

        private void OnPyreArmorChanged(int armor)
        {
            EventDispatcher.Enqueue(new PyreArmorEvent(armor));
        }

        private void OnAbilityAvailable(CharacterState character)
        {
            if (!ShouldSkip(character))
            {
                EventDispatcher.Enqueue(new UnitAbilityEvent(character, available: true));
            }
        }

        private void OnAbilityUnavailable(CharacterState character)
        {
            if (!ShouldSkip(character))
            {
                EventDispatcher.Enqueue(new UnitAbilityEvent(character, available: false));
            }
        }

        private void OnCombatCharacterRemoved(CharacterState character)
        {
            if (ShouldSkip(character) || !_announcedDeaths.Add(character))
            {
                return;
            }

            _vitalsTracker.Remove(character);
            EventDispatcher.Enqueue(new CharacterDeathEvent(character));
            Log.Info("[AccessibilityMod] Queued character death event from CombatManager.characterRemovedSignal: " +
                Message.RawCleaned(character?.GetName())?.Resolve());
        }

        private void OnDeckShuffled(bool initialShuffle)
        {
            if (!initialShuffle)
            {
                EventDispatcher.Enqueue(new DeckShuffledEvent());
            }
        }

        private void OnEnergyChanged(int energy)
        {
            EventDispatcher.Enqueue(new EnergyChangedEvent(energy));
        }

        private void OnMoonPhaseChanged(PlayerManager.MoonPhase phase)
        {
            if (_hasLastMoonPhase && _lastMoonPhase == phase)
            {
                return;
            }

            _lastMoonPhase = phase;
            _hasLastMoonPhase = true;
            EventDispatcher.Enqueue(new MoonPhaseChangedEvent(phase));
        }

        private void OnPyreHpChanged(SaveManager.PyreHPChangedSignalData data)
        {
            EventDispatcher.Enqueue(new PyreHpEvent(data.prevHP, data.newHP));
        }

        private void OnRelicTriggered(RelicState relic, IRelicEffect effect)
        {
            if (ReplayAccessibilityState.IsSuppressed)
            {
                Log.Info("[AccessibilityMod] Skipped artifact trigger during replay suppression: " + RelicTriggerDebugLabel(relic, effect));
                return;
            }

            if (effect is ICardModifierRelicEffect)
            {
                string cardModifierKey = RelicEffectKey(relic, effect);
                if (_announcedCardModifierRelicTriggers.Add(cardModifierKey))
                {
                    Log.Info("[AccessibilityMod] Skipped passive card modifier artifact trigger: " + RelicTriggerDebugLabel(relic, effect));
                }
                else
                {
                    Log.Info("[AccessibilityMod] Suppressed duplicate passive card modifier artifact trigger: " + RelicTriggerDebugLabel(relic, effect));
                }
                return;
            }

            string key = RelicTriggerKey(relic, effect);
            if (!_announcedRelicTriggers.Add(key))
            {
                Log.Info("[AccessibilityMod] Suppressed duplicate artifact trigger: " + RelicTriggerDebugLabel(relic, effect));
                return;
            }

            Log.Info("[AccessibilityMod] Queued artifact trigger: " + RelicTriggerDebugLabel(relic, effect));
            EventDispatcher.Enqueue(new RelicTriggeredEvent(relic));
        }

        private static string RelicEffectKey(RelicState relic, IRelicEffect effect)
        {
            string relicId = relic?.GetRelicDataID() ?? string.Empty;
            string effectType = effect?.GetType().FullName ?? string.Empty;
            return relicId + "|" + effectType;
        }

        private string RelicTriggerKey(RelicState relic, IRelicEffect effect)
        {
            CombatManager.Phase phase = _combatManager != null ? _combatManager.GetCombatPhase() : _lastPhase;
            return RelicEffectKey(relic, effect) + "|" + phase + "|" + _lastTurn + "|" + _lastTurnModifier;
        }

        private string RelicTriggerDebugLabel(RelicState relic, IRelicEffect effect)
        {
            Message name = Message.RawCleaned(relic?.GetName());
            string relicName = name?.Resolve() ?? "<unknown>";
            string relicId = relic?.GetRelicDataID() ?? "<unknown>";
            string effectType = effect?.GetType().Name ?? "<unknown>";
            CombatManager.Phase phase = _combatManager != null ? _combatManager.GetCombatPhase() : _lastPhase;
            return relicName + " (" + relicId + "), effect " + effectType + ", phase " + phase + ", turn " + _lastTurn;
        }
    }
}

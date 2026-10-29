using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Core.Events;
using MonsterTrainAccessibility.Core.Strings;

namespace MonsterTrainAccessibility.Module.Combat
{
    /// <summary>
    /// The battle's events, heard as they happen and kept in the events buffer (<see cref="GameEvents"/>):
    /// the turn and the phase, ember, the moon, shuffles, the pyre, artifacts firing, units arriving,
    /// moving floors, dying, their health, statuses and abilities (<see cref="VitalsTracker"/>). Listens
    /// while a battle is on (the game's battle screen up) and lets go when it ends; every listener comes
    /// off in <see cref="Stop"/>, which the module's teardown calls too, so a reload never leaves an old
    /// generation listening. The game's own signals feed it, plus one hook for enemies changing floors
    /// (the game tells its listeners of its own units' moves only).
    /// </summary>
    internal sealed class CombatEvents : ICharacterNotifications
    {
        private static CombatEvents s_live;

        private readonly AllGameManagers _managers;
        private readonly VitalsTracker _vitals = new VitalsTracker();
        private readonly HashSet<CharacterState> _died = new HashSet<CharacterState>();
        private readonly HashSet<string> _relicsThisTurn = new HashSet<string>();
        private int _turn = -1;
        private int _turnModifier;
        private CombatManager.Phase? _phase;
        private PlayerManager.MoonPhase _moon;

        private CombatEvents(AllGameManagers managers) { _managers = managers; }

        /// <summary>Per frame: listen while a battle is on, stop when it is over; report what the frame's
        /// health losses came to.</summary>
        public static void Tick()
        {
            bool battle = InBattle();
            if (battle && s_live == null)
            {
                s_live = new CombatEvents(AllGameManagers.Instance);
                s_live.Start();
            }
            else if (!battle && s_live != null) Shutdown();
            s_live?._vitals.FlushPending();
        }

        public static void Shutdown()
        {
            s_live?.Stop();
            s_live = null;
        }

        private static bool InBattle()
        {
            var managers = AllGameManagers.Instance;
            var screens = managers != null ? managers.GetScreenManager() : null;
            return screens != null && screens.GetScreenActive(global::ScreenName.Game) && managers.GetCombatManager() != null;
        }

        private static bool s_reloadSawMenu;

        /// <summary>The game is reloading the run (a battle restarted, a turn undone): from the press until
        /// the run is back in its battle, by way of the main menu, none of which is the player's to hear.
        /// The game keeps its own flag up longer, through the battle's start (its popups included).</summary>
        public static bool Reloading
        {
            get
            {
                var managers = AllGameManagers.Instance;
                var save = managers != null ? managers.GetSaveManager() : null;
                var screens = managers != null ? managers.GetScreenManager() : null;
                var reload = save != null ? save.restartBattleCoroutine : null;
                if (save == null || screens == null || !(save.IsInRestartBattleMode || (reload != null && reload.IsRunning())))
                {
                    s_reloadSawMenu = false;
                    return false;
                }
                bool menu = screens.GetScreenActive(global::ScreenName.MainMenu);
                if (menu) s_reloadSawMenu = true;
                return !(s_reloadSawMenu && !menu && screens.GetScreenActive(global::ScreenName.Game));
            }
        }

        /// <summary>A battle setting itself up (reloading, or not yet past its start): its pyre arriving,
        /// its ember and armor set from nothing. Only its turn and phase are said.</summary>
        public static bool SettingUp
        {
            get
            {
                if (Reloading) return true;
                var managers = AllGameManagers.Instance;
                var combat = managers != null ? managers.GetCombatManager() : null;
                var screens = managers != null ? managers.GetScreenManager() : null;
                return combat != null && screens != null && screens.GetScreenActive(global::ScreenName.Game)
                    && combat.GetCombatPhase() == CombatManager.Phase.Start;
            }
        }

        public static bool Muted(EventKind kind) => kind != EventKinds.Turn && kind != EventKinds.Phase && SettingUp;

        /// <summary>Whether the game is replaying what was done (an undo, a run continued): its events
        /// are logged, not spoken.</summary>
        public static bool Replaying()
        {
            var managers = AllGameManagers.Instance;
            if (managers == null) return false;
            var save = managers.GetSaveManager();
            if (save != null && save.IsInUndoMode) return true;
            var replay = managers.GetReplayManager();
            return replay != null && (replay.IsUndoing() || replay.IsContinuingRun());
        }

        private void Start()
        {
            var combat = _managers.GetCombatManager();
            combat.turnCountSignal.AddListener(OnTurn);
            combat.combatPhaseSignal.AddListener(OnPhase);
            combat.pyreArmorChangedSignal.AddListener(OnPyreArmor);
            combat.characterAbilityAvailableSignal.AddListener(OnAbilityReady);
            combat.characterAbilityUnavailableSignal.AddListener(OnAbilityUnavailable);
            combat.characterRemovedSignal.AddListener(OnRemoved);
            _managers.GetCardManager()?.deckShuffledSignal.AddListener(OnShuffled);
            var player = _managers.GetPlayerManager();
            if (player != null)
            {
                player.energyChangedSignal.AddListener(OnEmber);
                player.moonPhaseChangedSignal.AddListener(OnMoon);
                _moon = player.CurrentMoonPhase;
            }
            _managers.GetSaveManager()?.pyreHPChangedSignal.AddListener(OnPyreHp);
            RelicManager.RelicTriggered.AddListener(OnRelic);
            _managers.GetMonsterManager()?.AddCharactersNotification(this);
            _managers.GetHeroManager()?.AddCharactersNotification(this);
            _vitals.Start(combat);
            _vitals.Prime(_managers);
            GameEvents.Quotes.Clear();
            CoreLog.Info("combat events: listening");
        }

        private void Stop()
        {
            var combat = _managers.GetCombatManager();
            if (combat != null)
            {
                combat.turnCountSignal.RemoveListener(OnTurn);
                combat.combatPhaseSignal.RemoveListener(OnPhase);
                combat.pyreArmorChangedSignal.RemoveListener(OnPyreArmor);
                combat.characterAbilityAvailableSignal.RemoveListener(OnAbilityReady);
                combat.characterAbilityUnavailableSignal.RemoveListener(OnAbilityUnavailable);
                combat.characterRemovedSignal.RemoveListener(OnRemoved);
            }
            _managers.GetCardManager()?.deckShuffledSignal.RemoveListener(OnShuffled);
            var player = _managers.GetPlayerManager();
            if (player != null)
            {
                player.energyChangedSignal.RemoveListener(OnEmber);
                player.moonPhaseChangedSignal.RemoveListener(OnMoon);
            }
            _managers.GetSaveManager()?.pyreHPChangedSignal.RemoveListener(OnPyreHp);
            RelicManager.RelicTriggered.RemoveListener(OnRelic);
            _managers.GetMonsterManager()?.RemoveCharactersNotification(this);
            _managers.GetHeroManager()?.RemoveCharactersNotification(this);
            _vitals.Stop();
            CoreLog.Info("combat events: stopped");
        }

        // ---- the units (ICharacterNotifications) ----

        public IEnumerator CharacterAdded(CharacterState character, CardState fromCard)
        {
            if (!VitalsTracker.Skip(character))
            {
                _vitals.Track(character);
                GameEvents.Raise(EventKinds.Spawned, Strings.Event("spawned", VitalsTracker.Name(character), character.GetCurrentRoomIndex() + 1),
                    VitalsTracker.Side(character));
            }
            yield break;
        }

        public IEnumerator CharacterRemoved(CharacterState character, SpawnPoint usedPoint)
        {
            _vitals.Remove(character);
            yield break;
        }

        public void CharacterSpawnPointChanged(CharacterState character, SpawnPoint prev, SpawnPoint current)
            => Moved(character, prev, current);

        public void CharacterStatusEffectApplied(CharacterState character, StatusEffectState statusEffect, bool spawnEffect) { }

        public IEnumerator NoMoreHeroes()
        {
            GameEvents.Raise(EventKinds.Phase, Strings.Event("enemies_cleared"));
            yield break;
        }

        private static void Moved(CharacterState character, SpawnPoint prev, SpawnPoint current)
        {
            if (VitalsTracker.Skip(character) || prev == null || current == null) return;
            int from = (prev.GetRoomOwner()?.GetRoomIndex() ?? -1) + 1;
            int to = (current.GetRoomOwner()?.GetRoomIndex() ?? -1) + 1;
            if (from > 0 && to > 0 && from != to)
                GameEvents.Raise(EventKinds.Moved, Strings.Event("moved", VitalsTracker.Name(character), to), VitalsTracker.Side(character));
        }

        /// <summary>Enemies changing floors: the game's hero manager tells no listener, so its own
        /// chokepoint is watched while a battle is being listened to.</summary>
        [HarmonyPatch(typeof(HeroManager), nameof(HeroManager.OnSpawnPointChanged))]
        private static class EnemyMoves
        {
            private static void Postfix(CharacterState characterState, SpawnPoint prevPoint, SpawnPoint newPoint)
            {
                if (s_live == null) return;
                try { Moved(characterState, prevPoint, newPoint); }
                catch (System.Exception e) { CoreLog.Warning("combat events: enemy move: " + e.Message); }
            }
        }

        // ---- the battle ----

        private void OnTurn(int turn, int modifier, int totalWaves, bool looping)
        {
            if (_turn == turn && _turnModifier == modifier) return;
            _turn = turn;
            _turnModifier = modifier;
            _relicsThisTurn.Clear();
            int shown = turn + modifier;
            string text = shown <= 0 ? Strings.Event("deployment")
                : looping || totalWaves <= 0 ? Strings.Event("turn", shown)
                : Strings.Event("turn_of", shown, totalWaves);
            GameEvents.Raise(EventKinds.Turn, text);
        }

        private void OnPhase(CombatManager.Phase phase)
        {
            if (phase == CombatManager.Phase.Start || phase == CombatManager.Phase.PreCombat || phase == CombatManager.Phase.BossActionPreCombat)
                Screens.BattleScreen.TurnOpening();
            if (_phase == phase) return;
            var previous = _phase;
            _phase = phase;
            _relicsThisTurn.Clear();
            // Ending the turn passes through the enemy's phase on the way out of the player's (the game
            // leaves its queue-clearing phase that way) before combat: not the enemy's turn yet.
            if (phase == CombatManager.Phase.HeroTurn && previous == CombatManager.Phase.MonsterTurnQueueClear) return;
            string key;
            switch (phase)
            {
                case CombatManager.Phase.MonsterTurn: key = "player_action"; break;
                case CombatManager.Phase.Combat: key = "room_combat"; break;
                case CombatManager.Phase.HeroTurn: key = "enemy_turn"; break;
                case CombatManager.Phase.EndOfCombat: key = "combat_ended"; break;
                default: return;
            }
            GameEvents.Raise(EventKinds.Phase, Strings.Event(key));
        }

        private void OnPyreArmor(int armor) => GameEvents.Raise(EventKinds.PyreArmor, Strings.Event("pyre_armor", armor), EventSide.Friendly);

        private void OnAbilityReady(CharacterState c)
        {
            if (!VitalsTracker.Skip(c)) GameEvents.Raise(EventKinds.Ability, Strings.Event("ability_ready", VitalsTracker.Name(c)), VitalsTracker.Side(c));
        }

        private void OnAbilityUnavailable(CharacterState c)
        {
            if (!VitalsTracker.Skip(c)) GameEvents.Raise(EventKinds.Ability, Strings.Event("ability_unavailable", VitalsTracker.Name(c)), VitalsTracker.Side(c));
        }

        // A unit taken off the train dead (the game's removal of the dead), once per unit.
        private void OnRemoved(CharacterState c)
        {
            if (VitalsTracker.Skip(c) || !_died.Add(c)) return;
            _vitals.Remove(c);
            GameEvents.Raise(EventKinds.Died, Strings.Event("died", VitalsTracker.Name(c)), VitalsTracker.Side(c));
        }

        private void OnShuffled(bool initial)
        {
            if (!initial) GameEvents.Raise(EventKinds.DeckShuffled, Strings.Event("deck_shuffled"));
        }

        private void OnEmber(int ember) => GameEvents.Raise(EventKinds.Ember, Strings.Event("ember", ember));

        private void OnMoon(PlayerManager.MoonPhase phase)
        {
            if (phase == _moon) return;
            _moon = phase;
            GameEvents.Raise(EventKinds.MoonPhase, phase.LocalizedName());
        }

        private void OnPyreHp(SaveManager.PyreHPChangedSignalData data)
        {
            int delta = data.newHP - data.prevHP;
            if (delta < 0) GameEvents.Raise(EventKinds.PyreHp, Strings.Event("pyre_damaged", -delta), EventSide.Friendly);
            else if (delta > 0) GameEvents.Raise(EventKinds.PyreHp, Strings.Event("pyre_healed", delta), EventSide.Friendly);
        }

        // An artifact firing, once per effect a phase of a turn; an artifact that only changes cards
        // (a cost, a stat) fires on every look at the hand and is never reported. Not while replaying.
        private void OnRelic(RelicState relic, IRelicEffect effect)
        {
            if (relic == null || Replaying() || effect is ICardModifierRelicEffect) return;
            var combat = _managers.GetCombatManager();
            string key = relic.GetRelicDataID() + "|" + effect?.GetType().FullName + "|" + combat?.GetCombatPhase() + "|" + _turn + "|" + _turnModifier;
            if (!_relicsThisTurn.Add(key)) return;
            string name = relic.GetName();
            if (!string.IsNullOrWhiteSpace(name)) GameEvents.Raise(EventKinds.RelicTriggered, Strings.Event("relic_triggered", name), EventSide.Friendly);
        }
    }
}

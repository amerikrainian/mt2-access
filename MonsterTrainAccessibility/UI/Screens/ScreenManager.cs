using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Buffers;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Input;
using MonsterTrainAccessibility.Updates;
using MonsterTrainAccessibility.UI.Elements;
using UnityEngine;

namespace MonsterTrainAccessibility.UI.Screens
{
    public static class ScreenManager
    {
        private static readonly List<Screen> Screens = new List<Screen>();
        private static readonly GameScreenSynchronizer SyncState = new GameScreenSynchronizer();
        private static HudNavigationScreen _hudNavigationScreen;

        public static Screen CurrentScreen
        {
            get
            {
                if (Screens.Count == 0)
                {
                    return null;
                }

                return Screens[Screens.Count - 1].DeepestActiveScreen();
            }
        }

        public static void Initialize()
        {
            if (Screens.Count == 0)
            {
                PushScreen(new DefaultScreen());
            }
        }

        public static void Shutdown()
        {
            StartupAnnouncement.Reset();
            SyncState.Shutdown();
            _hudNavigationScreen = null;
            while (Screens.Count > 0)
            {
                Screen screen = Screens[Screens.Count - 1];
                Screens.RemoveAt(Screens.Count - 1);
                try { screen.OnPop(); } catch (Exception ex) { Log.Warn("Screen pop failed on shutdown: " + ex); }
            }
        }

        public static void RegisterUIScreen<TScreen>(global::ScreenName screenName, Func<TScreen, GameScreen> factory)
            where TScreen : global::UIScreen
        {
            SyncState.RegisterUIScreen(screenName, factory);
        }

        public static void RegisterTransition<TTransition>(Func<TTransition, GameObject, GameScreen> factory)
            where TTransition : global::ScreenTransition
        {
            SyncState.RegisterTransition(factory);
        }

        public static void PushScreen(Screen screen)
        {
            if (screen == null)
            {
                return;
            }

            if (Screens.Count > 0)
            {
                try { Screens[Screens.Count - 1].DeepestActiveScreen().OnUnfocus(); } catch (Exception ex) { Log.Warn("Screen unfocus failed: " + ex); }
            }

            Screens.Add(screen);
            try
            {
                screen.OnPush();
                screen.DeepestActiveScreen().OnFocus();
            }
            catch (Exception ex)
            {
                Log.Warn("Screen push failed for " + screen.GetType().Name + ": " + ex);
            }
        }

        public static void RemoveScreen(Screen screen)
        {
            if (screen == null)
            {
                return;
            }

            int index = Screens.IndexOf(screen);
            if (index < 0)
            {
                return;
            }

            if (index == 0 && Screens.Count == 1)
            {
                Log.Warn("[AccessibilityMod] Refusing to remove the last Screen.");
                return;
            }

            bool wasTop = index == Screens.Count - 1;

            try
            {
                if (wasTop)
                {
                    screen.DeepestActiveScreen().OnUnfocus();
                }
            }
            catch (Exception ex)
            {
                Log.Warn("Screen unfocus failed: " + ex);
            }

            if (screen.ActiveChild != null)
            {
                screen.RemoveChild(screen.ActiveChild);
            }

            Screens.RemoveAt(index);

            try { screen.OnPop(); } catch (Exception ex) { Log.Warn("Screen pop failed: " + ex); }

            if (wasTop && Screens.Count > 0)
            {
                try { Screens[Screens.Count - 1].DeepestActiveScreen().OnFocus(); } catch (Exception ex) { Log.Warn("Screen focus failed: " + ex); }
            }
        }

        public static void ReplaceScreen(Screen old, Screen replacement)
        {
            if (old == null || replacement == null)
            {
                return;
            }

            int index = Screens.IndexOf(old);
            if (index < 0)
            {
                return;
            }

            bool wasTop = index == Screens.Count - 1;

            try
            {
                if (wasTop)
                {
                    old.DeepestActiveScreen().OnUnfocus();
                }
            }
            catch (Exception ex)
            {
                Log.Warn("Screen unfocus failed: " + ex);
            }

            if (old.ActiveChild != null)
            {
                old.RemoveChild(old.ActiveChild);
            }

            try { old.OnPop(); } catch (Exception ex) { Log.Warn("Screen pop failed: " + ex); }

            Screens[index] = replacement;
            try
            {
                replacement.OnPush();
                if (wasTop)
                {
                    replacement.DeepestActiveScreen().OnFocus();
                }
            }
            catch (Exception ex)
            {
                Log.Warn("Screen replacement failed: " + ex);
            }
        }

        public static void RemoveFromTree(Screen screen)
        {
            if (screen == null)
            {
                return;
            }

            if (screen.Parent != null)
            {
                screen.Parent.RemoveChild(screen);
            }
            else
            {
                RemoveScreen(screen);
            }
        }

        public static IEnumerable<Screen> WalkScreensDeepestFirst()
        {
            for (int i = Screens.Count - 1; i >= 0; i--)
            {
                foreach (Screen screen in WalkTreeDeepestFirst(Screens[i]))
                {
                    yield return screen;
                }
            }
        }

        internal static HashSet<string> GetAlwaysEnabledBuffers()
        {
            HashSet<string> keys = new HashSet<string>(StringComparer.Ordinal)
            {
                "ui",
                "events"
            };

            for (int i = 0; i < Screens.Count; i++)
            {
                try
                {
                    Screens[i].ConfigurePersistentBuffers(keys);
                    ConfigurePersistentBuffersRecursive(Screens[i].ActiveChild, keys);
                }
                catch (Exception ex)
                {
                    Log.Info("[AccessibilityMod] ConfigurePersistentBuffers failed: " + ex);
                }
            }

            return keys;
        }

        internal static void ConfigureBuffers(BufferManager buffers)
        {
            if (buffers == null)
            {
                return;
            }

            for (int i = 0; i < Screens.Count; i++)
            {
                try
                {
                    Screens[i].ConfigureBuffers(buffers);
                    ConfigureBuffersRecursive(Screens[i].ActiveChild, buffers);
                }
                catch (Exception ex)
                {
                    Log.Info("[AccessibilityMod] ConfigureBuffers failed: " + ex);
                }
            }
        }

        public static bool DispatchAction(InputAction action, InputActionState state)
        {
            if (action == null)
            {
                return false;
            }

            for (int i = Screens.Count - 1; i >= 0; i--)
            {
                (bool claimed, bool propagate) result = DispatchInTree(Screens[i], action, state);
                if (result.claimed && !result.propagate)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool HasClaimForAction(InputAction action)
        {
            if (action == null)
            {
                return false;
            }

            for (int i = Screens.Count - 1; i >= 0; i--)
            {
                if (HasClaimInTree(Screens[i], action.Key))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool BlocksGameInputForAction(InputAction action)
        {
            if (action == null)
            {
                return false;
            }

            for (int i = Screens.Count - 1; i >= 0; i--)
            {
                (bool claimed, bool propagate) result = ClaimInTree(Screens[i], action);
                if (result.claimed)
                {
                    return !result.propagate;
                }
            }

            return false;
        }

        public static UIElement ResolveElement(GameObject go)
        {
            if (go == null)
            {
                return null;
            }

            for (int i = Screens.Count - 1; i >= 0; i--)
            {
                UIElement result = ResolveElementInTree(Screens[i], go);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        public static bool Sync()
        {
            bool changed = SyncState.Sync();
            changed |= SyncHudNavigation();
            return changed;
        }

        internal static bool FocusFirstHudSoul(global::Hud hud)
        {
            if (_hudNavigationScreen == null || hud == null || !_hudNavigationScreen.IsForHud(hud))
            {
                return false;
            }

            return _hudNavigationScreen.FocusFirstSoul();
        }

        public static void HandleUIScreenStateChanged(global::UIScreen screen, bool active)
        {
            SyncState.HandleUIScreenStateChanged(screen, active);
        }

        public static void HandleTransitionStateChanged(global::ScreenTransition transition, GameObject owner, bool active)
        {
            SyncState.HandleTransitionStateChanged(transition, owner, active);
        }

        private static void TryRemove(Screen screen)
        {
            if (screen == null)
            {
                return;
            }

            if (Screens.Contains(screen))
            {
                RemoveScreen(screen);
            }
            else if (screen.Parent != null)
            {
                screen.Parent.RemoveChild(screen);
            }
        }

        private static bool SyncHudNavigation()
        {
            global::ScreenManager gameScreenManager = GameManagers.GetScreenManager();
            global::Hud hud = gameScreenManager?.GetScreen(global::ScreenName.Hud) as global::Hud;
            bool active = hud != null &&
                hud.IsHudNavigationEnabled() &&
                IsHudNavigationEligible(gameScreenManager);

            if (active)
            {
                if (_hudNavigationScreen != null && IsScreenInTree(_hudNavigationScreen))
                {
                    if (Screens.Count > 0 && ReferenceEquals(Screens[Screens.Count - 1], _hudNavigationScreen))
                    {
                        return false;
                    }

                    TryRemove(_hudNavigationScreen);
                    _hudNavigationScreen = null;
                }

                _hudNavigationScreen = new HudNavigationScreen(hud);
                PushScreen(_hudNavigationScreen);
                return true;
            }

            if (_hudNavigationScreen != null)
            {
                TryRemove(_hudNavigationScreen);
                _hudNavigationScreen = null;
                return true;
            }

            return false;
        }

        private static bool IsHudNavigationEligible(global::ScreenManager gameScreenManager)
        {
            if (gameScreenManager == null)
            {
                return false;
            }

            if (gameScreenManager.GetTopScreen(ignoreDialog: false) == global::ScreenName.Dialog)
            {
                return false;
            }

            switch (gameScreenManager.GetTopScreen(ignoreDialog: true))
            {
                case global::ScreenName.Game:
                case global::ScreenName.BattleIntro:
                case global::ScreenName.Map:
                case global::ScreenName.SoulSaviorMap:
                case global::ScreenName.Merchant:
                case global::ScreenName.StoryEvent:
                case global::ScreenName.ChampionUpgrade:
                case global::ScreenName.Draft:
                case global::ScreenName.RelicChoice:
                case global::ScreenName.Elixir:
                case global::ScreenName.SoulChoice:
                case global::ScreenName.EndlessMutatorDraft:
                case global::ScreenName.Reward:
                case global::ScreenName.RegionSelection:
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsScreenInTree(Screen target)
        {
            if (target == null)
            {
                return false;
            }

            for (int i = 0; i < Screens.Count; i++)
            {
                if (IsScreenInTree(Screens[i], target))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsScreenInTree(Screen current, Screen target)
        {
            for (Screen screen = current; screen != null; screen = screen.ActiveChild)
            {
                if (ReferenceEquals(screen, target))
                {
                    return true;
                }
            }

            return false;
        }

        private static IEnumerable<Screen> WalkTreeDeepestFirst(Screen screen)
        {
            if (screen.ActiveChild != null)
            {
                foreach (Screen child in WalkTreeDeepestFirst(screen.ActiveChild))
                {
                    yield return child;
                }
            }

            yield return screen;
        }

        private static void ConfigurePersistentBuffersRecursive(Screen screen, HashSet<string> keys)
        {
            if (screen == null)
            {
                return;
            }

            screen.ConfigurePersistentBuffers(keys);
            ConfigurePersistentBuffersRecursive(screen.ActiveChild, keys);
        }

        private static void ConfigureBuffersRecursive(Screen screen, BufferManager buffers)
        {
            if (screen == null)
            {
                return;
            }

            screen.ConfigureBuffers(buffers);
            ConfigureBuffersRecursive(screen.ActiveChild, buffers);
        }

        private static (bool claimed, bool propagate) DispatchInTree(Screen screen, InputAction action, InputActionState state)
        {
            if (screen.ActiveChild != null)
            {
                (bool claimed, bool propagate) childResult = DispatchInTree(screen.ActiveChild, action, state);
                if (childResult.claimed && !childResult.propagate)
                {
                    return childResult;
                }
            }

            if (screen.HasClaimed(action.Key))
            {
                switch (state)
                {
                    case InputActionState.JustPressed: screen.OnActionJustPressed(action); break;
                    case InputActionState.Pressed: screen.OnActionPressed(action); break;
                    case InputActionState.JustReleased: screen.OnActionJustReleased(action); break;
                }
                return (true, screen.ShouldPropagate(action.Key));
            }

            return (false, false);
        }

        private static bool HasClaimInTree(Screen screen, string actionKey)
        {
            if (screen.ActiveChild != null && HasClaimInTree(screen.ActiveChild, actionKey))
            {
                return true;
            }

            return screen.HasClaimed(actionKey);
        }

        private static (bool claimed, bool propagate) ClaimInTree(Screen screen, InputAction action)
        {
            if (screen.ActiveChild != null)
            {
                (bool claimed, bool propagate) childResult = ClaimInTree(screen.ActiveChild, action);
                if (childResult.claimed)
                {
                    return childResult;
                }
            }

            return screen.HasClaimed(action.Key)
                ? (true, !screen.BlocksGameInput(action))
                : (false, false);
        }

        private static UIElement ResolveElementInTree(Screen screen, GameObject go)
        {
            if (screen.ActiveChild != null)
            {
                UIElement result = ResolveElementInTree(screen.ActiveChild, go);
                if (result != null)
                {
                    return result;
                }
            }

            for (Transform current = go.transform; current != null; current = current.parent)
            {
                UIElement element = screen.GetElement(current.gameObject);
                if (element != null)
                {
                    return element;
                }
            }

            return null;
        }

        private sealed class GameScreenSynchronizer
        {
            private sealed class UIScreenFactoryRegistration
            {
                public Type ExpectedScreenType;
                public Func<global::UIScreen, GameScreen> Factory;
            }

            private sealed class TransitionFactoryRegistration
            {
                public Type TransitionType;
                public Func<global::ScreenTransition, GameObject, GameScreen> Factory;
            }

            private sealed class ActiveTransitionRegistration
            {
                public int Order;
                public global::ScreenTransition Transition;
                public GameObject Owner;
                public GameScreen Screen;
            }

            private readonly Dictionary<global::ScreenName, UIScreenFactoryRegistration> _uiScreenFactories =
                new Dictionary<global::ScreenName, UIScreenFactoryRegistration>();
            private readonly List<TransitionFactoryRegistration> _transitionFactories = new List<TransitionFactoryRegistration>();
            private readonly Dictionary<int, ActiveTransitionRegistration> _activeTransitions = new Dictionary<int, ActiveTransitionRegistration>();
            private static readonly FieldInfo DialogActiveStackField = AccessTools.Field(typeof(global::DialogScreen), "activeDialogStack")!;

            private int _nextTransitionOrder;
            private global::UIScreen _currentUIScreen;
            private global::ScreenName _currentUIScreenName = global::ScreenName.NONE;
            private int _currentUIScreenKey;
            private GameScreen _currentUIScreenScreen;
            private int _currentDialogScreenKey;
            private GameScreen _currentDialogScreen;

            public void RegisterUIScreen<TScreen>(global::ScreenName screenName, Func<TScreen, GameScreen> factory)
                where TScreen : global::UIScreen
            {
                if (factory == null)
                {
                    return;
                }

                _uiScreenFactories[screenName] = new UIScreenFactoryRegistration
                {
                    ExpectedScreenType = typeof(TScreen),
                    Factory = screen => factory((TScreen)screen)
                };
            }

            public void RegisterTransition<TTransition>(Func<TTransition, GameObject, GameScreen> factory)
                where TTransition : global::ScreenTransition
            {
                if (factory == null)
                {
                    return;
                }

                _transitionFactories.Add(new TransitionFactoryRegistration
                {
                    TransitionType = typeof(TTransition),
                    Factory = (transition, owner) => factory((TTransition)transition, owner)
                });
            }

            public void Shutdown()
            {
                ActiveTransitionRegistration[] transitions = new ActiveTransitionRegistration[_activeTransitions.Count];
                _activeTransitions.Values.CopyTo(transitions, 0);
                for (int i = transitions.Length - 1; i >= 0; i--)
                {
                    TryRemove(transitions[i].Screen);
                }
                _activeTransitions.Clear();

                if (_currentUIScreenScreen != null)
                {
                    TryRemove(_currentUIScreenScreen);
                    _currentUIScreenScreen = null;
                }
                if (_currentDialogScreen != null)
                {
                    TryRemove(_currentDialogScreen);
                    _currentDialogScreen = null;
                }
                _currentUIScreen = null;
                _currentUIScreenKey = 0;
                _currentUIScreenName = global::ScreenName.NONE;
                _currentDialogScreenKey = 0;

                _uiScreenFactories.Clear();
                _transitionFactories.Clear();
                _nextTransitionOrder = 0;
            }

            public bool Sync()
            {
                bool changed = SyncCurrentUIScreen();
                changed |= SyncCurrentDialogScreen();
                changed |= PruneInactiveTransitions();
                return changed;
            }

            public void HandleUIScreenStateChanged(global::UIScreen screen, bool active)
            {
                Sync();
            }

            public void HandleTransitionStateChanged(global::ScreenTransition transition, GameObject owner, bool active)
            {
                if (transition == null)
                {
                    return;
                }

                int key = transition.GetInstanceID();
                if (!active)
                {
                    CloseTransition(key);
                    return;
                }

                Func<global::ScreenTransition, GameObject, GameScreen> factory = ResolveTransitionFactory(transition);
                if (factory == null)
                {
                    return;
                }

                ActiveTransitionRegistration existing;
                if (_activeTransitions.TryGetValue(key, out existing))
                {
                    existing.Owner = owner;
                    existing.Transition = transition;
                    return;
                }

                GameScreen child;
                try
                {
                    child = factory(transition, owner);
                }
                catch (Exception ex)
                {
                    Log.Warn("Transition factory threw: " + ex);
                    return;
                }

                if (child == null)
                {
                    return;
                }

                PushScreen(child);
                _activeTransitions[key] = new ActiveTransitionRegistration
                {
                    Order = ++_nextTransitionOrder,
                    Transition = transition,
                    Owner = owner,
                    Screen = child
                };
            }

            private bool SyncCurrentUIScreen()
            {
                global::ScreenManager gameScreenManager = GameManagers.GetScreenManager();
                global::UIScreen nextScreen = null;
                global::ScreenName nextScreenName = global::ScreenName.NONE;
                int nextScreenKey = 0;

                if (gameScreenManager != null)
                {
                    nextScreenName = gameScreenManager.GetTopScreen(ignoreDialog: true);
                    nextScreen = gameScreenManager.GetScreen(nextScreenName) as global::UIScreen;
                    nextScreenKey = nextScreen != null ? nextScreen.GetInstanceID() : 0;
                }

                if (nextScreenKey == _currentUIScreenKey &&
                    nextScreenName == _currentUIScreenName &&
                    ReferenceEquals(nextScreen, _currentUIScreen))
                {
                    return false;
                }

                if (_currentUIScreenScreen != null)
                {
                    TryRemove(_currentUIScreenScreen);
                    _currentUIScreenScreen = null;
                }

                _currentUIScreen = nextScreen;
                _currentUIScreenKey = nextScreenKey;
                _currentUIScreenName = nextScreenName;

                UIScreenFactoryRegistration registration;
                if (nextScreen != null && _uiScreenFactories.TryGetValue(nextScreenName, out registration))
                {
                    if (!registration.ExpectedScreenType.IsInstanceOfType(nextScreen))
                    {
                        Log.Warn("[AccessibilityMod] UI screen type mismatch for " + nextScreenName
                            + ": expected " + registration.ExpectedScreenType.Name
                            + " but got " + nextScreen.GetType().Name
                            + ". Skipping wrapper push to avoid wrong-screen registration.");
                        return true;
                    }

                    GameScreen wrapper = null;
                    try
                    {
                        wrapper = registration.Factory(nextScreen);
                    }
                    catch (Exception ex)
                    {
                        Log.Warn("UI screen factory threw for " + nextScreenName + ": " + ex);
                    }

                    if (wrapper != null)
                    {
                        _currentUIScreenScreen = wrapper;
                        PushScreen(wrapper);
                    }
                }

                return true;
            }

            private bool SyncCurrentDialogScreen()
            {
                global::ScreenManager gameScreenManager = GameManagers.GetScreenManager();
                global::DialogScreen dialogScreen = null;
                global::Dialog topDialog = null;
                int dialogKey = 0;

                if (gameScreenManager != null &&
                    gameScreenManager.GetTopScreen(ignoreDialog: false) == global::ScreenName.Dialog)
                {
                    dialogScreen = gameScreenManager.GetScreen(global::ScreenName.Dialog) as global::DialogScreen;
                    topDialog = TopDialog(dialogScreen);
                    dialogKey = topDialog != null ? topDialog.GetInstanceID() : 0;
                }

                if (dialogKey == _currentDialogScreenKey)
                {
                    return false;
                }

                if (_currentDialogScreen != null)
                {
                    TryRemove(_currentDialogScreen);
                    _currentDialogScreen = null;
                }

                _currentDialogScreenKey = dialogKey;
                if (dialogScreen != null && topDialog != null)
                {
                    _currentDialogScreen = new AccessibleDialogScreen(dialogScreen);
                    PushScreen(_currentDialogScreen);
                }

                return true;
            }

            private static global::Dialog TopDialog(global::DialogScreen dialogScreen)
            {
                List<global::Dialog> stack = dialogScreen != null
                    ? DialogActiveStackField.GetValue(dialogScreen) as List<global::Dialog>
                    : null;
                return stack != null && stack.Count > 0 ? stack[stack.Count - 1] : null;
            }

            private bool PruneInactiveTransitions()
            {
                if (_activeTransitions.Count == 0)
                {
                    return false;
                }

                List<int> keysToClose = null;
                foreach (KeyValuePair<int, ActiveTransitionRegistration> pair in _activeTransitions)
                {
                    ActiveTransitionRegistration registration = pair.Value;
                    if (registration == null ||
                        registration.Transition == null ||
                        registration.Owner == null ||
                        !registration.Transition.Active ||
                        !registration.Owner.activeInHierarchy)
                    {
                        if (keysToClose == null)
                        {
                            keysToClose = new List<int>();
                        }
                        keysToClose.Add(pair.Key);
                    }
                }

                if (keysToClose == null)
                {
                    return false;
                }

                for (int i = 0; i < keysToClose.Count; i++)
                {
                    CloseTransition(keysToClose[i]);
                }

                return true;
            }

            private Func<global::ScreenTransition, GameObject, GameScreen> ResolveTransitionFactory(global::ScreenTransition transition)
            {
                Type transitionType = transition.GetType();
                for (int i = 0; i < _transitionFactories.Count; i++)
                {
                    TransitionFactoryRegistration registration = _transitionFactories[i];
                    if (registration.TransitionType.IsAssignableFrom(transitionType))
                    {
                        return registration.Factory;
                    }
                }
                return null;
            }

            private void CloseTransition(int key)
            {
                ActiveTransitionRegistration registration;
                if (!_activeTransitions.TryGetValue(key, out registration))
                {
                    return;
                }

                _activeTransitions.Remove(key);
                TryRemove(registration.Screen);
            }
        }
    }
}

using System;
using MonsterTrainAccessibility.Buffers;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Events;
using MonsterTrainAccessibility.Speech;
using MonsterTrainAccessibility.UI;
using MonsterTrainAccessibility.UI.Elements;
using MonsterTrainAccessibility.UI.Screens;
using ShinyShoe;
using UnityEngine;
using UnityEngine.EventSystems;
using ModScreenManager = MonsterTrainAccessibility.UI.Screens.ScreenManager;

namespace MonsterTrainAccessibility.Core
{
    internal sealed class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        private static readonly FocusContext _focusContext = new FocusContext();
        private static UIElement _focusedElement;
        private static UIElement _activeElement;
        private static GameObject _focusedGameObject;
        private static bool _focusDirty;
        private static string _lastAnnouncedText;
        private static UIElement _lastAnnouncedElement;
        private static GameObject _lastAnnouncedGameObject;
        private static string _suppressNextFocusAnnouncementText;
        private static string _lastRestoreBailReason;
        private static InputDeviceType _lastLoggedInputMode = (InputDeviceType)(-1);
        private static bool _loggedFirstUpdate;
        private static bool _loggedInputManagerUnavailable;
        private static int _lastTickFrame = -1;
        private static bool _pendingScreenChangeFocusAnnouncement;
        private static int _deferFocusAnnouncementsUntilFrame = -1;
        private static bool _replaySuppressionActive;
        private static readonly ResourceEventMonitor _resourceEventMonitor = new ResourceEventMonitor();

        public static UIManager Initialize()
        {
            if (Instance != null)
            {
                return Instance;
            }

            GameObject host = new GameObject("MonsterTrainAccessibility.UIManager");
            DontDestroyOnLoad(host);
            Instance = host.AddComponent<UIManager>();
            return Instance;
        }

        public static void Shutdown()
        {
            if (Instance == null)
            {
                return;
            }

            UIManager manager = Instance;
            Instance = null;
            SetActiveElement(null);
            _focusContext.Reset();
            _focusedElement = null;
            _activeElement = null;
            _focusedGameObject = null;
            _focusDirty = false;
            _lastAnnouncedText = null;
            _lastAnnouncedElement = null;
            _lastAnnouncedGameObject = null;
            _suppressNextFocusAnnouncementText = null;
            _lastRestoreBailReason = null;
            _lastLoggedInputMode = (InputDeviceType)(-1);
            _loggedFirstUpdate = false;
            _loggedInputManagerUnavailable = false;
            _lastTickFrame = -1;
            _pendingScreenChangeFocusAnnouncement = false;
            _deferFocusAnnouncementsUntilFrame = -1;
            _replaySuppressionActive = false;
            _resourceEventMonitor.Shutdown();
            if (manager != null)
            {
                Destroy(manager.gameObject);
            }
        }

        public static void SetFocusedElement(UIElement element)
        {
            if (ReplayAccessibilityState.IsSuppressed && element != null)
            {
                return;
            }

            if (ReferenceEquals(_focusedElement, element))
            {
                return;
            }
            _focusedElement = element;
            _focusedGameObject = null;
            _focusDirty = true;
        }

        public static void SetFocusedControl(GameObject go, UIElement element)
        {
            if (ReplayAccessibilityState.IsSuppressed && element != null)
            {
                return;
            }

            if (ReferenceEquals(_focusedElement, element) && ReferenceEquals(_focusedGameObject, go))
            {
                return;
            }
            if (ReferenceEquals(_focusedElement, element))
            {
                _focusedGameObject = go;
                return;
            }
            _focusedElement = element;
            _focusedGameObject = go;
            _focusDirty = true;
        }

        public static Message GetFocusedAnnouncement()
        {
            if (_focusedElement == null)
            {
                return null;
            }

            FocusContext context = new FocusContext();
            return context.BuildAnnouncement(_focusedElement);
        }

        public static void ForceReannounceCurrentFocus()
        {
            if (ReplayAccessibilityState.IsSuppressed)
            {
                return;
            }

            _lastAnnouncedText = null;
            _lastAnnouncedElement = null;
            _lastAnnouncedGameObject = null;
            _suppressNextFocusAnnouncementText = null;
            if (_focusedElement != null)
            {
                _focusDirty = true;
            }
        }

        public static void SuppressNextFocusAnnouncementIfTextMatches(string text)
        {
            _suppressNextFocusAnnouncementText = string.IsNullOrEmpty(text) ? null : text;
        }

        public static void RefreshBuffersFor(UIElement element)
        {
            if (element != null)
            {
                UpdateBuffersFor(element);
            }
        }

        private void Update()
        {
            Tick();
        }

        public static void Tick()
        {
            if (_lastTickFrame == Time.frameCount)
            {
                return;
            }

            _lastTickFrame = Time.frameCount;

            if (!_loggedFirstUpdate)
            {
                _loggedFirstUpdate = true;
                Log.Info("[AccessibilityMod] UIManager.Update active");
            }

            bool screenChanged = ModScreenManager.Sync();
            if (screenChanged)
            {
                _focusContext.Reset();
                _lastAnnouncedText = null;
                _lastAnnouncedElement = null;
                _lastAnnouncedGameObject = null;
                _suppressNextFocusAnnouncementText = null;
                _pendingScreenChangeFocusAnnouncement = true;
                _deferFocusAnnouncementsUntilFrame = Time.frameCount + 1;
                TryUpgradeFocusedElementFromRegistry();
            }

            ModScreenManager.CurrentScreen?.OnUpdate();
            _resourceEventMonitor.Update();
            EventDispatcher.Flush();

            bool replaySuppressed = ReplayAccessibilityState.IsSuppressed;
            if (replaySuppressed != _replaySuppressionActive)
            {
                SetReplaySuppressionActive(replaySuppressed);
            }

            if (replaySuppressed)
            {
                return;
            }

            LogInputModeIfChanged();

            global::InputManager inputManager = global::InputManager.Inst;
            bool navigationMode = inputManager != null && inputManager.currentInputModeUsesNavigation;

            global::MonsterTrainAccessibility.UI.Screens.Screen currentScreen = ModScreenManager.CurrentScreen;
            if (currentScreen?.ShouldAcceptGameSelection() != false)
            {
                GameObject current = ResolveFocusedGameObject();

                if (current != _focusedGameObject)
                {
                    UIElement resolved = current != null ? ResolveElement(current) : null;
                    LogFocusTransition(current, resolved);
                    _focusedGameObject = current;
                    _focusedElement = resolved;
                    _focusDirty = true;
                }
            }

            if (!_focusDirty)
            {
                if (_pendingScreenChangeFocusAnnouncement &&
                    _focusedElement != null &&
                    Time.frameCount > _deferFocusAnnouncementsUntilFrame)
                {
                    _focusDirty = true;
                }
                else
                {
                    return;
                }
            }

            if (_pendingScreenChangeFocusAnnouncement)
            {
                if (Time.frameCount <= _deferFocusAnnouncementsUntilFrame)
                {
                    return;
                }

                if (_focusedElement == null)
                {
                    return;
                }

                _pendingScreenChangeFocusAnnouncement = false;
            }

            if (!navigationMode)
            {
                _focusDirty = false;
                SetActiveElement(null);
                return;
            }

            if (currentScreen != null && !currentScreen.ShouldAnnounceFocus(_focusedElement))
            {
                _focusDirty = false;
                return;
            }

            _focusDirty = false;

            if (_focusedElement == null)
            {
                SetActiveElement(null);
                _lastAnnouncedText = null;
                _lastAnnouncedGameObject = null;
                return;
            }

            SetActiveElement(_focusedElement);

            Message announcement;
            try
            {
                announcement = _focusContext.BuildAnnouncement(_focusedElement);
            }
            catch (Exception ex)
            {
                Log.Warn("Focus announcement failed: " + ex);
                return;
            }

            if (announcement == null)
            {
                if (Settings.VerboseFocusLogging?.Value == true)
                {
                    Log.Info("[AccessibilityMod] Focus: BuildAnnouncement returned null for element " + _focusedElement.GetType().Name);
                }
                return;
            }

            string text;
            try
            {
                text = announcement.Resolve();
            }
            catch (Exception ex)
            {
                Log.Warn("Focus announcement resolve failed: " + ex);
                return;
            }

            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            UpdateBuffersFor(_focusedElement);

            if (!string.IsNullOrEmpty(_suppressNextFocusAnnouncementText))
            {
                bool suppress = string.Equals(text, _suppressNextFocusAnnouncementText, StringComparison.Ordinal);
                _suppressNextFocusAnnouncementText = null;
                if (suppress)
                {
                    _lastAnnouncedText = text;
                    _lastAnnouncedElement = _focusedElement;
                    _lastAnnouncedGameObject = _focusedGameObject;
                    if (Settings.VerboseFocusLogging?.Value == true)
                    {
                        Log.Info("[AccessibilityMod] Focus: suppressing unchanged rebuild announcement '" + text + "'");
                    }
                    return;
                }
            }

            bool sameElement = ReferenceEquals(_focusedElement, _lastAnnouncedElement);
            bool sameTarget = _focusedGameObject != null && ReferenceEquals(_focusedGameObject, _lastAnnouncedGameObject);
            if (text == _lastAnnouncedText && (sameElement || sameTarget))
            {
                if (Settings.VerboseFocusLogging?.Value == true && !string.IsNullOrEmpty(text))
                {
                    Log.Info("[AccessibilityMod] Focus: suppressing duplicate announcement '" + text + "'");
                }
                return;
            }

            _lastAnnouncedText = text;
            _lastAnnouncedElement = _focusedElement;
            _lastAnnouncedGameObject = _focusedGameObject;
            SpeechManager.Output(text);
        }

        private static void UpdateBuffersFor(UIElement element)
        {
            try
            {
                if (element == null)
                {
                    return;
                }

                BufferManager buffers = BufferManager.Instance;
                buffers.ResetToAlwaysEnabled(ModScreenManager.GetAlwaysEnabledBuffers());
                ModScreenManager.ConfigureBuffers(buffers);
                string currentBufferKey = element.HandleBuffers(buffers);
                if (!string.IsNullOrEmpty(currentBufferKey))
                {
                    buffers.SetCurrentBuffer(currentBufferKey);
                }
            }
            catch (Exception ex)
            {
                Log.Info("[AccessibilityMod] Focus buffer update failed: " + ex);
            }
        }

        private static void SetActiveElement(UIElement element)
        {
            if (ReferenceEquals(_activeElement, element))
            {
                return;
            }

            if (_activeElement != null && _activeElement.IsFocused)
            {
                _activeElement.Unfocus();
            }

            _activeElement = element;

            if (_activeElement != null && !_activeElement.IsFocused)
            {
                _activeElement.Focus();
            }
        }

        private static void SetReplaySuppressionActive(bool active)
        {
            _replaySuppressionActive = active;
            _focusContext.Reset();
            _focusedElement = null;
            _focusedGameObject = null;
            _focusDirty = false;
            _lastAnnouncedText = null;
            _lastAnnouncedElement = null;
            _lastAnnouncedGameObject = null;
            _suppressNextFocusAnnouncementText = null;

            if (active)
            {
                SetActiveElement(null);
                _pendingScreenChangeFocusAnnouncement = false;
                _deferFocusAnnouncementsUntilFrame = -1;
                Log.Info("[AccessibilityMod] Suppressing focus during undo/continue replay.");
                return;
            }

            _pendingScreenChangeFocusAnnouncement = true;
            _deferFocusAnnouncementsUntilFrame = Time.frameCount + 1;
            Log.Info("[AccessibilityMod] Resuming focus after undo/continue replay.");
        }

        private static void LogFocusTransition(GameObject current, UIElement resolved)
        {
            if (Settings.VerboseFocusLogging?.Value != true)
            {
                return;
            }

            if (current == null)
            {
                Log.Info("[AccessibilityMod] Focus: cleared (no selected game object)");
                return;
            }

            string source = resolved != null ? "registry" : "null";
            string screenName = ModScreenManager.CurrentScreen?.GetType().Name ?? "<none>";
            Log.Info("[AccessibilityMod] Focus: '" + current.name + "' on screen " + screenName + " -> element=" + source);
        }

        internal static void NotifyGameObjectSelected(GameObject go)
        {
            if (ReplayAccessibilityState.IsSuppressed)
            {
                return;
            }

            if (go == null || !go.activeInHierarchy)
            {
                return;
            }

            UIElement element = ModScreenManager.ResolveElement(go) ?? ProxyFactory.Create(go);
            SetFocusedControl(go, element);
        }

        private static void TryUpgradeFocusedElementFromRegistry()
        {
            if (ModScreenManager.CurrentScreen?.ShouldAcceptGameSelection() == false)
            {
                return;
            }

            if (_focusedGameObject == null)
            {
                return;
            }

            UIElement resolved = ModScreenManager.ResolveElement(_focusedGameObject);
            if (resolved == null || ReferenceEquals(resolved, _focusedElement))
            {
                return;
            }

            _focusedElement = resolved;
            _focusDirty = true;

            if (Settings.VerboseFocusLogging?.Value == true)
            {
                Log.Info("[AccessibilityMod] Focus: upgraded selected object from screen registry after screen sync");
            }
        }

        private static UIElement ResolveElement(GameObject current)
        {
            return ModScreenManager.ResolveElement(current) ?? ProxyFactory.Create(current);
        }

        private static GameObject ResolveFocusedGameObject()
        {
            if (ReplayAccessibilityState.IsSuppressed)
            {
                return null;
            }

            if (ModScreenManager.CurrentScreen?.ShouldAcceptGameSelection() == false)
            {
                return null;
            }

            EventSystem eventSystem = EventSystem.current;
            GameObject current = eventSystem != null ? eventSystem.currentSelectedGameObject : null;
            if (current != null && current.activeInHierarchy)
            {
                return current;
            }

            if (TryRestoreNavigationSelection())
            {
                return eventSystem != null ? eventSystem.currentSelectedGameObject : null;
            }

            return null;
        }

        private static bool TryRestoreNavigationSelection()
        {
            if (ReplayAccessibilityState.IsSuppressed)
            {
                LogRestoreBail("undo/continue replay is active");
                return false;
            }

            global::InputManager inputManager = global::InputManager.Inst;
            global::ScreenManager screenManager = GameManagers.GetScreenManager();
            if (inputManager == null || screenManager == null)
            {
                LogRestoreBail("managers unavailable");
                return false;
            }

            if (!inputManager.currentInputModeUsesNavigation)
            {
                LogRestoreBail("input mode is " + inputManager.currentInputDeviceMode + " (not navigation)");
                return false;
            }

            global::ScreenName topScreenName = screenManager.GetTopScreen(ignoreDialog: false);
            global::UIScreen topScreen = screenManager.GetScreen(topScreenName) as global::UIScreen;
            if (topScreen == null || !topScreen.AllowUINavigation())
            {
                LogRestoreBail("top screen " + topScreenName + " does not allow UI navigation");
                return false;
            }

            global::MonsterTrainAccessibility.UI.Screens.Screen wrapperScreen = ModScreenManager.CurrentScreen;
            if (wrapperScreen != null && !wrapperScreen.ShouldRestoreNavigationFocus())
            {
                LogRestoreBail("screen wrapper suppressed focus restore on " + topScreenName);
                return false;
            }

            if (inputManager.GetSelectedGameUIComponent()?.CanBeSelected() == true)
            {
                LogRestoreBail("a selectable is already active");
                return false;
            }

            screenManager.SelectDefaultGameObject();
            GameObject restored = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (restored == null)
            {
                LogRestoreBail("SelectDefaultGameObject did not select anything on " + topScreenName);
                return false;
            }

            Log.Info("[AccessibilityMod] Restored missing navigation focus on " + topScreenName);
            return true;
        }

        private static void LogRestoreBail(string reason)
        {
            if (Settings.VerboseFocusLogging?.Value != true)
            {
                return;
            }

            if (string.Equals(reason, _lastRestoreBailReason, StringComparison.Ordinal))
            {
                return;
            }

            _lastRestoreBailReason = reason;
            Log.Info("[AccessibilityMod] Focus restore skipped: " + reason);
        }

        private static void LogInputModeIfChanged()
        {
            global::InputManager inputManager = global::InputManager.Inst;
            if (inputManager == null)
            {
                if (!_loggedInputManagerUnavailable)
                {
                    _loggedInputManagerUnavailable = true;
                    Log.Info("[AccessibilityMod] InputManager.Inst unavailable in UIManager.Update");
                }
                return;
            }

            InputDeviceType mode = inputManager.currentInputDeviceMode;
            if (mode == _lastLoggedInputMode)
            {
                return;
            }

            _lastLoggedInputMode = mode;
            Log.Info("[AccessibilityMod] Input device mode -> " + mode);
        }
    }
}

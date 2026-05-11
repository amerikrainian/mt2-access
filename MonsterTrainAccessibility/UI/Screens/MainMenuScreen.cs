using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Speech;
using MonsterTrainAccessibility.UI;
using MonsterTrainAccessibility.UI.Elements;
using TMPro;
using UnityEngine;
using GameButton = ShinyShoe.GameUISelectableButton;
using MenuButton = MainMenuButton;

namespace MonsterTrainAccessibility.UI.Screens
{
    internal sealed class MainMenuScreen : GameScreen
    {
        private static readonly MethodInfo CollectMenuButtonsMethod = AccessTools.Method(typeof(global::MainMenuScreen), "CollectMenuButtons")!;
        private static readonly FieldInfo MenuButtonsField = AccessTools.Field(typeof(global::MainMenuScreen), "menuButtons")!;
        private static readonly FieldInfo StartRunOptionsDialogField = AccessTools.Field(typeof(global::MainMenuScreen), "startRunOptionsDialog")!;
        private readonly global::MainMenuScreen _screen;
        private StartRunOptionsDialogScreen _startRunOptionsDialogScreen;

        public MainMenuScreen(global::MainMenuScreen screen)
            : base(screen != null ? screen.transform : null)
        {
            _screen = screen;
        }

        public override void OnPush()
        {
            base.OnPush();
            if (!FocusGameSelectedMenuButton())
            {
                ListContainer navigable = RootElement as ListContainer;
                navigable?.FocusFirst();
            }
            SpeechManager.Output(new Message("menu.main.opened"));

            global::ShinyShoe.AppManager.PlatformServices.PlayerDisplayInfoChanged += HandlePlayerDisplayInfoChanged;
            TrackUnsubscribe(() => global::ShinyShoe.AppManager.PlatformServices.PlayerDisplayInfoChanged -= HandlePlayerDisplayInfoChanged);
        }

        protected override void BuildRegistry()
        {
            ListContainer menuButtons = new ListContainer { NavigationAxis = NavigationAxis.Vertical };
            RootElement = menuButtons;

            foreach (MenuButton menuButton in CollectLiveMenuButtons())
            {
                AddMenuButton(menuButtons, menuButton, RequiresDlc(menuButton));
            }

            foreach (string fieldName in new[] { "dialogueFirstButton", "dialogueSecondButton", "dialogueThirdButton", "dialogueFourthButton" })
            {
                AddDialogueButton(menuButtons, fieldName);
            }

            AddUserButton(
                menuButtons,
                "changeUserButton",
                isChangeUser: true);
            AddUserButton(
                menuButtons,
                "nonSwitchableUserButton",
                isChangeUser: false);
            AddProgressionObjective();
        }

        public override void OnUpdate()
        {
            SyncStartRunOptionsDialog();
        }

        private bool FocusGameSelectedMenuButton()
        {
            ListContainer menuButtons = RootElement as ListContainer;
            if (menuButtons == null)
            {
                return false;
            }

            if (TryFocusGameComponent(menuButtons, global::InputManager.Inst?.GetSelectedGameUIComponent()))
            {
                return true;
            }

            return TryFocusGameComponent(menuButtons, _screen?.GetUIToSelectWhenInteractable(resetToDefault: true));
        }

        private bool TryFocusGameComponent(ListContainer menuButtons, global::ShinyShoe.IGameUIComponent component)
        {
            GameObject target = component?.component != null ? component.component.gameObject : null;
            GameObject registeredTarget;
            UIElement element = ResolveRegisteredElement(target, out registeredTarget);
            if (element == null || !element.IsVisible)
            {
                return false;
            }

            menuButtons.SetFocusTo(element);
            UIManager.SetFocusedControl(registeredTarget ?? target, element);
            return true;
        }

        private UIElement ResolveRegisteredElement(GameObject target, out GameObject registeredTarget)
        {
            registeredTarget = null;
            for (Transform current = target != null ? target.transform : null; current != null; current = current.parent)
            {
                UIElement element = base.GetElement(current.gameObject);
                if (element != null)
                {
                    registeredTarget = current.gameObject;
                    return element;
                }
            }

            return null;
        }

        public override UIElement GetElement(GameObject go)
        {
            SyncStartRunOptionsDialog();

            if (_startRunOptionsDialogScreen != null)
            {
                UIElement dialogElement = ResolveInStartRunOptionsDialog(go);
                if (dialogElement != null)
                {
                    return dialogElement;
                }
            }

            return base.GetElement(go);
        }

        private void SyncStartRunOptionsDialog()
        {
            if (_startRunOptionsDialogScreen != null && _startRunOptionsDialogScreen.Parent == null)
            {
                _startRunOptionsDialogScreen = null;
            }

            global::StartRunOptionsDialog dialog = Get<global::GameModeOptionsDialog>(_screen, StartRunOptionsDialogField) as global::StartRunOptionsDialog;
            if (dialog != null && dialog.IsOpen)
            {
                if (_startRunOptionsDialogScreen == null)
                {
                    _startRunOptionsDialogScreen = new StartRunOptionsDialogScreen(dialog);
                    PushChild(_startRunOptionsDialogScreen);
                }
                return;
            }

            if (_startRunOptionsDialogScreen != null)
            {
                RemoveChild(_startRunOptionsDialogScreen);
                _startRunOptionsDialogScreen = null;
            }
        }

        private UIElement ResolveInStartRunOptionsDialog(GameObject go)
        {
            if (go == null)
            {
                return null;
            }

            for (Transform current = go.transform; current != null; current = current.parent)
            {
                UIElement element = _startRunOptionsDialogScreen.GetElement(current.gameObject);
                if (element != null)
                {
                    return element;
                }
            }

            return null;
        }

        private void AddDialogueButton(ListContainer container, string fieldName)
        {
            MenuButton dialogueButton = ReflectionUtil.GetFieldValue<MenuButton>(_screen, fieldName, "MainMenuScreen");
            if (dialogueButton == null)
            {
                return;
            }

            AddMenuButton(container, dialogueButton, requiresDlc: false);
        }

        private void HandlePlayerDisplayInfoChanged()
        {
            GameObject current = UnityEngine.EventSystems.EventSystem.current?.currentSelectedGameObject;
            GameButton changeUserButton = ReflectionUtil.GetFieldValue<GameButton>(_screen, "changeUserButton", "MainMenuScreen");
            GameButton nonSwitchableUserButton = ReflectionUtil.GetFieldValue<GameButton>(_screen, "nonSwitchableUserButton", "MainMenuScreen");
            if (IsSameOrChild(current, changeUserButton) || IsSameOrChild(current, nonSwitchableUserButton))
            {
                SpeechManager.Output(new Message("menu.main.current_user", global::ShinyShoe.AppManager.PlatformServices.GetPlayerDisplayName()));
            }
        }

        private void AddMenuButton(Container container, MenuButton menuButton, bool requiresDlc = false)
        {
            if (menuButton?.Button == null)
            {
                return;
            }

            ProxyMainMenuButton element = new ProxyMainMenuButton(_screen, menuButton, requiresDlc);

            container.Add(element);
            Register(element, menuButton.Button.gameObject, menuButton.gameObject);
        }

        internal static Message ResolveMenuButtonLabelMessage(global::MainMenuScreen screen, MenuButton menuButton)
        {
            string label = ResolveMenuButtonLabel(menuButton);
            if (!string.IsNullOrWhiteSpace(label))
            {
                return Message.RawCleaned(label);
            }

            MenuButton settingsButton = ReflectionUtil.GetFieldValue<MenuButton>(screen, "settingsButton", "MainMenuScreen");
            return ReferenceEquals(menuButton, settingsButton)
                ? Message.Localized("ui", "HUD.SETTINGS")
                : null;
        }

        private IEnumerable<MenuButton> CollectLiveMenuButtons()
        {
            CollectMenuButtonsMethod.Invoke(_screen, null);
            List<MenuButton> menuButtons = (List<MenuButton>)MenuButtonsField.GetValue(_screen);
            if (menuButtons == null)
            {
                yield break;
            }

            for (int i = 0; i < menuButtons.Count; i++)
            {
                MenuButton menuButton = menuButtons[i];
                if (menuButton != null)
                {
                    yield return menuButton;
                }
            }
        }

        private bool RequiresDlc(MenuButton menuButton)
        {
            MenuButton soulSaviorButton = ReflectionUtil.GetFieldValue<MenuButton>(_screen, "soulSaviorButton", "MainMenuScreen");
            return ReferenceEquals(menuButton, soulSaviorButton);
        }

        private void AddUserButton(Container container, string fieldName, bool isChangeUser)
        {
            GameButton button = ReflectionUtil.GetFieldValue<GameButton>(_screen, fieldName, "MainMenuScreen");
            if (button == null)
            {
                return;
            }

            AddToContainer(
                container,
                button.gameObject,
                isChangeUser ? (UIElement)new ProxyIntroChangeUserButton(button) : new ProxyIntroCurrentUserButton(button));
        }

        private void AddProgressionObjective()
        {
            global::ProgressionObjectiveUI progressionObjectiveUI = ReflectionUtil.GetFieldValue<global::ProgressionObjectiveUI>(_screen, "progressionObjectiveUI", "MainMenuScreen");
            if (progressionObjectiveUI == null)
            {
                return;
            }

            Register(
                progressionObjectiveUI.gameObject,
                new ProxyMainMenuProgressionObjective(progressionObjectiveUI));
        }

        private static string ResolveMenuButtonLabel(MenuButton menuButton)
        {
            return AccessibilityText.ReadTextFromField<TMPro.TextMeshProUGUI>(menuButton, "label", "MainMenuButton");
        }

        private static bool IsSameOrChild(GameObject selected, Component component)
        {
            return selected != null &&
                component != null &&
                (selected == component.gameObject || selected.transform.IsChildOf(component.transform));
        }

        internal static Message ResolveMenuButtonState(MenuButton menuButton, bool requiresDlc)
        {
            if (requiresDlc && !global::ShinyShoe.AppManager.PlatformServices.IsDlcInstalled(global::ShinyShoe.DLC.Railforged))
            {
                return new Message("menu.main.dlc_required");
            }

            if (menuButton.IsLocked())
            {
                return new Message("state.locked");
            }

            if (!menuButton.Button.interactable)
            {
                return new Message("state.disabled");
            }

            return null;
        }

        private static T Get<T>(object owner, FieldInfo field) where T : class
        {
            return owner != null ? field.GetValue(owner) as T : null;
        }
    }
}

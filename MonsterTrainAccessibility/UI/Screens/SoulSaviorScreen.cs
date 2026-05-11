using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Elements;
using ShinyShoe;
using UnityEngine;
using GameButton = ShinyShoe.GameUISelectableButton;
using MenuButton = MainMenuButton;

namespace MonsterTrainAccessibility.UI.Screens
{
    internal sealed class SoulSaviorScreen : GameScreen
    {
        private static readonly FieldInfo DialogueFirstButtonField = AccessTools.Field(typeof(global::SoulSaviorScreen), "dialogueFirstButton")!;
        private static readonly FieldInfo DialogueSecondButtonField = AccessTools.Field(typeof(global::SoulSaviorScreen), "dialogueSecondButton")!;
        private static readonly FieldInfo MainMenuButtonField = AccessTools.Field(typeof(global::SoulSaviorScreen), "mainMenuButton")!;
        private static readonly FieldInfo StartSoulSaviorRunButtonField = AccessTools.Field(typeof(global::SoulSaviorScreen), "startSoulSaviorRunButton")!;
        private static readonly FieldInfo SoulforgeButtonField = AccessTools.Field(typeof(global::SoulSaviorScreen), "soulforgeButton")!;
        private static readonly FieldInfo SettingsButtonField = AccessTools.Field(typeof(global::SoulSaviorScreen), "settingsButton")!;
        private static readonly FieldInfo ChangeUserButtonField = AccessTools.Field(typeof(global::SoulSaviorScreen), "changeUserButton")!;
        private static readonly FieldInfo NonSwitchableUserButtonField = AccessTools.Field(typeof(global::SoulSaviorScreen), "nonSwitchableUserButton")!;
        private static readonly FieldInfo RunOptionsDialogField = AccessTools.Field(typeof(global::SoulSaviorScreen), "soulSaviorRunOptionsDialog")!;
        private static readonly FieldInfo ProgressionObjectiveField = AccessTools.Field(typeof(global::SoulSaviorScreen), "progressionObjectiveUI")!;

        private readonly global::SoulSaviorScreen _screen;
        private SoulSaviorRunOptionsDialogScreen _runOptionsDialogScreen;

        public SoulSaviorScreen(global::SoulSaviorScreen screen)
        {
            _screen = screen;
        }

        public override void OnPush()
        {
            base.OnPush();
            if (!FocusGameSelectedButton())
            {
                (RootElement as ListContainer)?.FocusFirst();
            }
        }

        public override void OnUpdate()
        {
            SyncRunOptionsDialog();
        }

        public override UIElement GetElement(GameObject go)
        {
            SyncRunOptionsDialog();
            if (_runOptionsDialogScreen != null)
            {
                UIElement dialogElement = ResolveInRunOptionsDialog(go);
                if (dialogElement != null)
                {
                    return dialogElement;
                }
            }

            return base.GetElement(go);
        }

        protected override void BuildRegistry()
        {
            ListContainer root = new ListContainer
            {
                AnnounceName = false,
                NavigationAxis = NavigationAxis.Vertical
            };
            RootElement = root;

            AddMenuButton(root, Get<MenuButton>(_screen, DialogueFirstButtonField));
            AddMenuButton(root, Get<MenuButton>(_screen, DialogueSecondButtonField));
            AddMenuButton(root, Get<MenuButton>(_screen, StartSoulSaviorRunButtonField));
            AddMenuButton(root, Get<MenuButton>(_screen, SoulforgeButtonField));
            AddMenuButton(root, Get<MenuButton>(_screen, MainMenuButtonField));
            AddMenuButton(root, Get<MenuButton>(_screen, SettingsButtonField));
            AddUserButton(root, Get<GameButton>(_screen, ChangeUserButtonField), isChangeUser: true);
            AddUserButton(root, Get<GameButton>(_screen, NonSwitchableUserButtonField), isChangeUser: false);
            AddProgressionObjective();
        }

        private bool FocusGameSelectedButton()
        {
            ListContainer root = RootElement as ListContainer;
            if (root == null)
            {
                return false;
            }

            return TryFocusGameComponent(root, global::InputManager.Inst?.GetSelectedGameUIComponent()) ||
                TryFocusGameComponent(root, _screen?.GetDefaultGameUISelectable());
        }

        private bool TryFocusGameComponent(ListContainer root, IGameUIComponent component)
        {
            GameObject target = component?.component != null ? component.component.gameObject : null;
            UIElement element = ResolveRegisteredElement(target);
            if (element == null || !element.IsVisible)
            {
                return false;
            }

            root.SetFocusTo(element);
            UIManager.SetFocusedControl(target, element);
            return true;
        }

        private UIElement ResolveRegisteredElement(GameObject target)
        {
            for (Transform current = target != null ? target.transform : null; current != null; current = current.parent)
            {
                UIElement element = base.GetElement(current.gameObject);
                if (element != null)
                {
                    return element;
                }
            }

            return null;
        }

        private void AddMenuButton(ListContainer root, MenuButton menuButton)
        {
            if (menuButton?.Button == null)
            {
                return;
            }

            LabeledButton element = new LabeledButton(
                menuButton.Button,
                () => ResolveMenuButtonLabel(menuButton),
                tooltip: null);
            root.Add(element);
            Register(element, menuButton.Button.gameObject, menuButton.gameObject);
        }

        private void AddUserButton(ListContainer root, GameButton button, bool isChangeUser)
        {
            if (button == null)
            {
                return;
            }

            AddToContainer(
                root,
                button.gameObject,
                isChangeUser ? (UIElement)new ProxyIntroChangeUserButton(button) : new ProxyIntroCurrentUserButton(button));
        }

        private void AddProgressionObjective()
        {
            global::ProgressionObjectiveUI progressionObjective = Get<global::ProgressionObjectiveUI>(_screen, ProgressionObjectiveField);
            if (progressionObjective == null)
            {
                return;
            }

            Register(
                progressionObjective.gameObject,
                new ProxyMainMenuProgressionObjective(progressionObjective));
        }

        private Message ResolveMenuButtonLabel(MenuButton menuButton)
        {
            string label = AccessibilityText.ReadTextFromField<TMPro.TextMeshProUGUI>(menuButton, "label", "MainMenuButton");
            if (!string.IsNullOrWhiteSpace(label))
            {
                return Message.RawCleaned(label);
            }

            return ReferenceEquals(menuButton, Get<MenuButton>(_screen, SettingsButtonField))
                ? Message.Localized("ui", "HUD.SETTINGS")
                : null;
        }

        private void SyncRunOptionsDialog()
        {
            if (_runOptionsDialogScreen != null && _runOptionsDialogScreen.Parent == null)
            {
                _runOptionsDialogScreen = null;
            }

            global::SoulSaviorRunOptionsDialog dialog = Get<global::SoulSaviorRunOptionsDialog>(_screen, RunOptionsDialogField);
            if (dialog != null && dialog.IsOpen)
            {
                if (_runOptionsDialogScreen == null)
                {
                    _runOptionsDialogScreen = new SoulSaviorRunOptionsDialogScreen(dialog);
                    PushChild(_runOptionsDialogScreen);
                }
                return;
            }

            if (_runOptionsDialogScreen != null)
            {
                RemoveChild(_runOptionsDialogScreen);
                _runOptionsDialogScreen = null;
            }
        }

        private UIElement ResolveInRunOptionsDialog(GameObject go)
        {
            if (go == null || _runOptionsDialogScreen == null)
            {
                return null;
            }

            for (Transform current = go.transform; current != null; current = current.parent)
            {
                UIElement element = _runOptionsDialogScreen.GetElement(current.gameObject);
                if (element != null)
                {
                    return element;
                }
            }

            return null;
        }

        private static T Get<T>(object owner, FieldInfo field) where T : class
        {
            return owner != null ? field.GetValue(owner) as T : null;
        }

        private sealed class SoulSaviorRunOptionsDialogScreen : GameScreen
        {
            private static readonly FieldInfo ContinueGameOptionField = AccessTools.Field(typeof(global::SoulSaviorRunOptionsDialog), "continueGameOption")!;
            private static readonly FieldInfo NewGameOptionField = AccessTools.Field(typeof(global::SoulSaviorRunOptionsDialog), "newGameOption")!;
            private static readonly FieldInfo BackButtonField = AccessTools.Field(typeof(global::SoulSaviorRunOptionsDialog), "backButton")!;
            private static readonly FieldInfo PlayButtonField = AccessTools.Field(typeof(global::GameModeOption), "playButton")!;

            private readonly global::SoulSaviorRunOptionsDialog _dialog;

            public SoulSaviorRunOptionsDialogScreen(global::SoulSaviorRunOptionsDialog dialog)
            {
                _dialog = dialog;
            }

            public override bool ShouldRestoreNavigationFocus() => false;
            public override bool ShouldAcceptGameSelection() => false;

            public override void OnPush()
            {
                base.OnPush();
                (RootElement as ListContainer)?.FocusFirst();
            }

            public override void OnUpdate()
            {
                if (_dialog == null || !_dialog.IsOpen)
                {
                    ScreenManager.RemoveFromTree(this);
                }
            }

            protected override void BuildRegistry()
            {
                ListContainer root = new ListContainer
                {
                    AnnounceName = false,
                    NavigationAxis = NavigationAxis.Vertical
                };
                RootElement = root;

                AddOption(root, Get<global::GameModeOption>(_dialog, ContinueGameOptionField));
                AddOption(root, Get<global::GameModeOption>(_dialog, NewGameOptionField));
                AddButton(root, Get<GameButton>(_dialog, BackButtonField));
            }

            private void AddOption(ListContainer root, global::GameModeOption option)
            {
                if (option == null)
                {
                    return;
                }

                GameButton button = (Get<global::ProcessingSpinnerButton>(option, PlayButtonField))?.Button;
                if (button == null)
                {
                    return;
                }

                ProxyStartRunOptionButton element = new ProxyStartRunOptionButton(option, button, null);
                root.Add(element);
                Register(button.gameObject, element);
            }

            private void AddButton(ListContainer root, GameButton button)
            {
                if (button == null)
                {
                    return;
                }

                LabeledButton element = new LabeledButton(button, () => Message.FromText(AuthoredLabelReader.Read(button)));
                root.Add(element);
                Register(button.gameObject, element);
            }
        }
    }
}

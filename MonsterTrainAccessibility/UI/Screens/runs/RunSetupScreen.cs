using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI;
using MonsterTrainAccessibility.UI.Elements;
using ShinyShoe;
using TMPro;
using UnityEngine;

namespace MonsterTrainAccessibility.UI.Screens
{
    internal sealed class RunSetupScreen : GameScreen
    {
        private static readonly FieldInfo MainClassInfoField = AccessTools.Field(typeof(global::RunSetupScreen), "mainClassInfo")!;
        private static readonly FieldInfo SubClassInfoField = AccessTools.Field(typeof(global::RunSetupScreen), "subClassInfo")!;
        private static readonly FieldInfo PyreHeartInfoField = AccessTools.Field(typeof(global::RunSetupScreen), "pyreHeartInfo")!;
        private static readonly FieldInfo SaveManagerField = AccessTools.Field(typeof(global::RunSetupScreen), "saveManager")!;
        private static readonly FieldInfo ClanSelectionDialogField = AccessTools.Field(typeof(global::RunSetupScreen), "clanSelectionDialog")!;
        private static readonly FieldInfo PyreHeartSelectionDialogField = AccessTools.Field(typeof(global::RunSetupScreen), "pyreHeartSelectionDialog")!;
        private static readonly FieldInfo MutatorSelectionDialogField = AccessTools.Field(typeof(global::RunSetupScreen), "mutatorSelectionDialog")!;
        private static readonly FieldInfo FtueCovenantSelectionDialogField = AccessTools.Field(typeof(global::RunSetupScreen), "ftueCovenantSelectionDialog")!;
        private static readonly FieldInfo CovenantSelectionUIField = AccessTools.Field(typeof(global::RunSetupScreen), "covenantSelectionUI")!;
        private static readonly FieldInfo MainClassButtonField = AccessTools.Field(typeof(global::RunSetupScreen), "mainClassButton")!;
        private static readonly FieldInfo MainClassSwapChampionButtonField = AccessTools.Field(typeof(global::RunSetupScreen), "mainClassSwapChampionButton")!;
        private static readonly FieldInfo SubClassButtonField = AccessTools.Field(typeof(global::RunSetupScreen), "subClassButton")!;
        private static readonly FieldInfo SubClassSwapChampionButtonField = AccessTools.Field(typeof(global::RunSetupScreen), "subClassSwapChampionButton")!;
        private static readonly FieldInfo PyreHeartButtonField = AccessTools.Field(typeof(global::RunSetupScreen), "pyreHeartButton")!;
        private static readonly FieldInfo CovenantButtonField = AccessTools.Field(typeof(global::RunSetupScreen), "covenantButton")!;
        private static readonly FieldInfo ToggleEnableMutatorsButtonField = AccessTools.Field(typeof(global::RunSetupScreen), "toggleEnableMutatorsButton")!;
        private static readonly FieldInfo ToggleEnableMutatorsButtonLabelField = AccessTools.Field(typeof(global::RunSetupScreen), "toggleEnableMutatorsButtonLabel")!;
        private static readonly FieldInfo EditMutatorsButtonField = AccessTools.Field(typeof(global::RunSetupScreen), "editMutatorsButton")!;
        private static readonly FieldInfo StartButtonField = AccessTools.Field(typeof(global::RunSetupScreen), "startButton")!;
        private static readonly FieldInfo SettingsButtonField = AccessTools.Field(typeof(global::RunSetupScreen), "settingsButton")!;
        private static readonly FieldInfo BackButtonField = AccessTools.Field(typeof(global::RunSetupScreen), "backButton")!;
        private static readonly FieldInfo ChallengeCovenantUIField = AccessTools.Field(typeof(global::CovenantSelectionUI), "covenantUI")!;
        private static readonly FieldInfo ChallengeCovenantAllGameDataField = AccessTools.Field(typeof(global::ChallengeCovenantUI), "allGameData")!;

        private readonly global::RunSetupScreen _screen;
        private ClanSelectionDialogScreen _clanSelectionScreen;
        private PyreHeartSelectionDialogScreen _pyreHeartSelectionScreen;
        private MutatorSelectionDialogScreen _mutatorSelectionScreen;
        private RunSetupFtueCovenantSelectionScreen _ftueCovenantSelectionScreen;
        private string _championSignature;

        public RunSetupScreen(global::RunSetupScreen screen)
        {
            _screen = screen;
        }

        public override void OnPush()
        {
            base.OnPush();
            (RootElement as ListContainer)?.FocusFirst();
        }

        public override void OnFocus()
        {
            base.OnFocus();
            SyncChildDialogs();
            if (ActiveChild != null)
            {
                return;
            }

            ListContainer root = RootElement as ListContainer;
            if (root == null)
            {
                return;
            }

            if (root.FocusIndex >= 0)
            {
                root.SetFocusIndex(root.FocusIndex);
                return;
            }

            root.FocusFirst();
        }

        public override void OnUpdate()
        {
            SyncChildDialogs();
            RefreshChampionFocusIfSelectionChanged();
        }

        public override UIElement GetElement(GameObject go)
        {
            SyncChildDialogs();
            UIElement childElement = ResolveInActiveChild(go);
            if (childElement != null)
            {
                return childElement;
            }
            return base.GetElement(go);
        }

        protected override void BuildRegistry()
        {
            ListContainer root = new ListContainer
            {
                AnnounceName = false,
                AnnouncePosition = true,
                NavigationAxis = NavigationAxis.Vertical
            };
            RootElement = root;

            GameUISelectableButton covenantButton = Get<GameUISelectableButton>(_screen, CovenantButtonField);
            GameUISelectableButton editMutatorsButton = Get<GameUISelectableButton>(_screen, EditMutatorsButtonField);
            GameUISelectableButton startButton = Get<GameUISelectableButton>(_screen, StartButtonField);
            GameUISelectableButton settingsButton = Get<GameUISelectableButton>(_screen, SettingsButtonField);
            GameUISelectableButton backButton = Get<GameUISelectableButton>(_screen, BackButtonField);
            GameUISelectableButton toggleEnableMutatorsButton = Get<GameUISelectableButton>(_screen, ToggleEnableMutatorsButtonField);
            global::RunSetupClassLevelInfoUI mainInfo = Get<global::RunSetupClassLevelInfoUI>(_screen, MainClassInfoField);
            global::RunSetupClassLevelInfoUI subInfo = Get<global::RunSetupClassLevelInfoUI>(_screen, SubClassInfoField);
            global::CovenantSelectionUI covenantSelection = Get<global::CovenantSelectionUI>(_screen, CovenantSelectionUIField);
            SaveManager saveManager = Get<SaveManager>(_screen, SaveManagerField);

            AddClanSummary(root, Get<GameUISelectableButton>(_screen, MainClassButtonField),
                mainInfo,
                saveManager,
                "ScreenRunSetup_MainClassTitle",
                "ScreenClassSelection_ClassLabel");
            AddChampionButton(root, Get<GameUISelectableButton>(_screen, MainClassSwapChampionButtonField), mainInfo, saveManager, () => SwapChampionLabel(
                mainInfo,
                "RUN_SETUP.SWAP_MAIN_CHAMPION"));
            AddClanSummary(root, Get<GameUISelectableButton>(_screen, SubClassButtonField),
                subInfo,
                saveManager,
                "ScreenRunSetup_SubclassTitle",
                "ScreenClassSelection_SubclassLabel");
            AddChampionButton(root, Get<GameUISelectableButton>(_screen, SubClassSwapChampionButtonField), subInfo, saveManager, () => SwapChampionLabel(
                subInfo,
                "RUN_SETUP.SWAP_ALLIED_CHAMPION"));
            AddPyreHeartSummary(root, Get<GameUISelectableButton>(_screen, PyreHeartButtonField), Get<global::PyreHeartInfoUI>(_screen, PyreHeartInfoField));
            AddCovenant(root, covenantButton, covenantSelection);
            AddButton(root, toggleEnableMutatorsButton, () => Message.RawCleaned(FirstText(ReadLabel(_screen, ToggleEnableMutatorsButtonLabelField), ResolveButtonLabel(toggleEnableMutatorsButton))));
            AddButton(root, editMutatorsButton, () => GameButtonLabel(editMutatorsButton));
            AddButton(root, startButton, () => GameButtonLabel(startButton));
            AddButton(root, settingsButton, () => Message.RawCleaned(ResolveSettingsLabel(settingsButton)));
            AddButton(root, backButton, () => GameButtonLabel(backButton));
            _championSignature = ChampionSignature(mainInfo, subInfo);
        }

        private void RefreshChampionFocusIfSelectionChanged()
        {
            global::RunSetupClassLevelInfoUI mainInfo = Get<global::RunSetupClassLevelInfoUI>(_screen, MainClassInfoField);
            global::RunSetupClassLevelInfoUI subInfo = Get<global::RunSetupClassLevelInfoUI>(_screen, SubClassInfoField);
            string signature = ChampionSignature(mainInfo, subInfo);
            if (string.Equals(signature, _championSignature, System.StringComparison.Ordinal))
            {
                return;
            }

            _championSignature = signature;
            if ((RootElement as ListContainer)?.FocusedChild is ProxyRunSetupChampionButton)
            {
                UIManager.ForceReannounceCurrentFocus();
            }
        }

        private static string ChampionSignature(global::RunSetupClassLevelInfoUI mainInfo, global::RunSetupClassLevelInfoUI subInfo)
        {
            return ChampionSignaturePart(mainInfo) + "|" + ChampionSignaturePart(subInfo);
        }

        private static string ChampionSignaturePart(global::RunSetupClassLevelInfoUI info)
        {
            if (info == null)
            {
                return string.Empty;
            }

            string classId = info.ClassData != null ? info.ClassData.GetID() : info.RandomId;
            return (classId ?? string.Empty) + ":" + info.ChampionIndex.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        private void SyncChildDialogs()
        {
            if (_clanSelectionScreen != null && _clanSelectionScreen.Parent == null)
            {
                _clanSelectionScreen = null;
            }
            if (_pyreHeartSelectionScreen != null && _pyreHeartSelectionScreen.Parent == null)
            {
                _pyreHeartSelectionScreen = null;
            }
            if (_mutatorSelectionScreen != null && _mutatorSelectionScreen.Parent == null)
            {
                _mutatorSelectionScreen = null;
            }
            if (_ftueCovenantSelectionScreen != null && _ftueCovenantSelectionScreen.Parent == null)
            {
                _ftueCovenantSelectionScreen = null;
            }

            global::RunSetupClanSelectionUI clanDialog = Get<global::RunSetupClanSelectionUI>(_screen, ClanSelectionDialogField);
            if (clanDialog != null && clanDialog.IsOpen)
            {
                if (_clanSelectionScreen == null)
                {
                    _clanSelectionScreen = new ClanSelectionDialogScreen(clanDialog, Get<SaveManager>(_screen, SaveManagerField));
                    PushChild(_clanSelectionScreen);
                }
            }
            else if (_clanSelectionScreen != null)
            {
                RemoveChild(_clanSelectionScreen);
                _clanSelectionScreen = null;
            }

            global::RunSetupPyreHeartSelectionUI pyreDialog = Get<global::RunSetupPyreHeartSelectionUI>(_screen, PyreHeartSelectionDialogField);
            if (pyreDialog != null && pyreDialog.IsOpen)
            {
                if (_pyreHeartSelectionScreen == null)
                {
                    _pyreHeartSelectionScreen = new PyreHeartSelectionDialogScreen(pyreDialog, Get<SaveManager>(_screen, SaveManagerField));
                    PushChild(_pyreHeartSelectionScreen);
                }
            }
            else if (_pyreHeartSelectionScreen != null)
            {
                RemoveChild(_pyreHeartSelectionScreen);
                _pyreHeartSelectionScreen = null;
            }

            global::MutatorSelectionDialog mutatorDialog = Get<global::MutatorSelectionDialog>(_screen, MutatorSelectionDialogField);
            if (mutatorDialog != null && mutatorDialog.IsOpen)
            {
                if (_mutatorSelectionScreen == null)
                {
                    _mutatorSelectionScreen = new MutatorSelectionDialogScreen(mutatorDialog);
                    PushChild(_mutatorSelectionScreen);
                }
            }
            else if (_mutatorSelectionScreen != null)
            {
                RemoveChild(_mutatorSelectionScreen);
                _mutatorSelectionScreen = null;
            }

            global::RunSetupFtueCovenantSelectionUI ftueCovenantDialog = Get<global::RunSetupFtueCovenantSelectionUI>(_screen, FtueCovenantSelectionDialogField);
            if (ftueCovenantDialog != null && ftueCovenantDialog.IsConsumingInputs())
            {
                if (_ftueCovenantSelectionScreen == null)
                {
                    _ftueCovenantSelectionScreen = new RunSetupFtueCovenantSelectionScreen(ftueCovenantDialog);
                    PushChild(_ftueCovenantSelectionScreen);
                }
            }
            else if (_ftueCovenantSelectionScreen != null)
            {
                RemoveChild(_ftueCovenantSelectionScreen);
                _ftueCovenantSelectionScreen = null;
            }
        }

        private void AddButton(ListContainer root, GameUISelectableButton button, System.Func<Message> label)
        {
            if (button == null)
            {
                return;
            }

            ProxyRunSetupButton element = new ProxyRunSetupButton(button, label);
            root.Add(element);
            Register(button.gameObject, element);
        }

        private void AddChampionButton(ListContainer root, GameUISelectableButton button, global::RunSetupClassLevelInfoUI info, SaveManager saveManager, System.Func<Message> label)
        {
            if (button == null)
            {
                return;
            }

            ProxyRunSetupChampionButton element = new ProxyRunSetupChampionButton(button, info, saveManager, label);
            root.Add(element);
            Register(button.gameObject, element);
        }

        private void AddCovenant(ListContainer root, GameUISelectableButton button, global::CovenantSelectionUI covenant)
        {
            if (button == null)
            {
                return;
            }

            ProxyRunSetupCovenant element = new ProxyRunSetupCovenant(
                button,
                covenant,
                () => Message.RawCleaned(ResolveCovenantLabel(covenant, button)));
            root.Add(element);
            Register(button.gameObject, element);
        }

        private void AddClanSummary(ListContainer root, GameUISelectableButton button, global::RunSetupClassLevelInfoUI info, SaveManager saveManager, params string[] labelTerms)
        {
            if (button == null)
            {
                return;
            }

            ProxyRunSetupClanSummary element = new ProxyRunSetupClanSummary(button, info, saveManager, labelTerms);
            root.Add(element);
            Register(button.gameObject, element);
        }

        private void AddPyreHeartSummary(ListContainer root, GameUISelectableButton button, global::PyreHeartInfoUI info)
        {
            if (button == null)
            {
                return;
            }

            ProxyRunSetupPyreHeartSummary element = new ProxyRunSetupPyreHeartSummary(button, info);
            root.Add(element);
            Register(button.gameObject, element);
        }

        private static Message GameButtonLabel(GameUISelectableButton button)
        {
            return Message.RawCleaned(ResolveButtonLabel(button));
        }

        private static Message SwapChampionLabel(global::RunSetupClassLevelInfoUI info, string key)
        {
            string champion = ChampionName(info);
            return !string.IsNullOrWhiteSpace(champion)
                ? Message.Localized("ui", key, new { champion })
                : Message.Localized("messages", "setup.swap_champion");
        }

        private static string ChampionName(global::RunSetupClassLevelInfoUI info)
        {
            global::ChampionData champion = info?.ClassData?.GetChampionData(info.ChampionIndex);
            return champion?.championCardData != null ? champion.championCardData.GetName() : string.Empty;
        }

        private UIElement ResolveInActiveChild(GameObject go)
        {
            if (go == null || ActiveChild == null)
            {
                return null;
            }

            for (Transform current = go.transform; current != null; current = current.parent)
            {
                UIElement element = ActiveChild.GetElement(current.gameObject);
                if (element != null)
                {
                    return element;
                }
            }

            return null;
        }

        private static string ReadLabel(object owner, FieldInfo field)
        {
            TMP_Text text = Get<TMP_Text>(owner, field);
            return AccessibilityText.ReadLocalizedText(text);
        }

        private static T Get<T>(object owner, FieldInfo field) where T : class
        {
            return owner != null ? field.GetValue(owner) as T : null;
        }

        private abstract class ItemDialogScreen<TItem> : GameScreen
            where TItem : MonoBehaviour
        {
            protected override void BuildRegistry()
            {
                ListContainer root = new ListContainer
                {
                    AnnounceName = false,
                    AnnouncePosition = true,
                    NavigationAxis = NavigationAxis.Vertical
                };
                RootElement = root;

                IReadOnlyList<TItem> items = GetItems();
                if (items == null)
                {
                    return;
                }

                for (int i = 0; i < items.Count; i++)
                {
                    AddItem(root, items[i]);
                }
            }

            public override void OnPush()
            {
                base.OnPush();
                (RootElement as ListContainer)?.FocusFirst();
            }

            public override void OnUpdate()
            {
                if (!IsOpen())
                {
                    ScreenManager.RemoveFromTree(this);
                }
            }

            protected abstract bool IsOpen();
            protected abstract IReadOnlyList<TItem> GetItems();
            protected abstract GameUISelectableButton GetButton(TItem item);
            protected abstract UIElement CreateItemElement(TItem item, GameUISelectableButton button);

            private void AddItem(ListContainer root, TItem item)
            {
                GameUISelectableButton button = GetButton(item);
                if (button == null)
                {
                    return;
                }

                UIElement element = CreateItemElement(item, button);
                if (element == null)
                {
                    return;
                }

                root.Add(element);
                Register(button.gameObject, element);
            }
        }

        private sealed class ClanSelectionDialogScreen : ItemDialogScreen<global::RunSetupClanSelectionItemUI>
        {
            private static readonly FieldInfo ClassSelectionIconItemField = AccessTools.Field(typeof(global::RunSetupClanSelectionUI), "classSelectionIconItem")!;
            private static readonly FieldInfo ClanItemButtonField = AccessTools.Field(typeof(global::RunSetupClanSelectionItemUI), "button")!;
            private readonly global::RunSetupClanSelectionUI _dialog;
            private readonly SaveManager _saveManager;

            public ClanSelectionDialogScreen(global::RunSetupClanSelectionUI dialog, SaveManager saveManager)
            {
                _dialog = dialog;
                _saveManager = saveManager;
            }

            protected override bool IsOpen() => _dialog != null && _dialog.IsOpen;

            protected override IReadOnlyList<global::RunSetupClanSelectionItemUI> GetItems()
            {
                global::RunSetupClanSelectionLayoutUI layout = Get<global::RunSetupClanSelectionLayoutUI>(_dialog, ClassSelectionIconItemField);
                return layout?.ClassIcons;
            }

            protected override GameUISelectableButton GetButton(global::RunSetupClanSelectionItemUI item)
            {
                return Get<GameUISelectableButton>(item, ClanItemButtonField);
            }

            protected override UIElement CreateItemElement(global::RunSetupClanSelectionItemUI item, GameUISelectableButton button)
            {
                return new ProxyRunSetupClanChoice(item, _saveManager);
            }
        }

        private sealed class PyreHeartSelectionDialogScreen : ItemDialogScreen<global::RunSetupPyreHeartSelectionItemUI>
        {
            private static readonly FieldInfo PyreHeartSelectionUIField = AccessTools.Field(typeof(global::RunSetupPyreHeartSelectionUI), "pyreHeartSelectionUI")!;
            private static readonly FieldInfo PyreItemButtonField = AccessTools.Field(typeof(global::RunSetupPyreHeartSelectionItemUI), "button")!;
            private readonly global::RunSetupPyreHeartSelectionUI _dialog;
            private readonly SaveManager _saveManager;

            public PyreHeartSelectionDialogScreen(global::RunSetupPyreHeartSelectionUI dialog, SaveManager saveManager)
            {
                _dialog = dialog;
                _saveManager = saveManager;
            }

            protected override bool IsOpen() => _dialog != null && _dialog.IsOpen;

            protected override IReadOnlyList<global::RunSetupPyreHeartSelectionItemUI> GetItems()
            {
                global::RunSetupPyreHeartSelectionLayoutUI layout = Get<global::RunSetupPyreHeartSelectionLayoutUI>(_dialog, PyreHeartSelectionUIField);
                return layout?.PyreHeartIcons;
            }

            protected override GameUISelectableButton GetButton(global::RunSetupPyreHeartSelectionItemUI item)
            {
                return Get<GameUISelectableButton>(item, PyreItemButtonField);
            }

            protected override UIElement CreateItemElement(global::RunSetupPyreHeartSelectionItemUI item, GameUISelectableButton button)
            {
                return new ProxyRunSetupPyreHeartChoice(item, _dialog, _saveManager);
            }
        }

        private sealed class MutatorSelectionDialogScreen : GameScreen
        {
            private static readonly FieldInfo MutatorButtonsField = AccessTools.Field(typeof(global::MutatorSelectionDialog), "mutatorButtons")!;
            private static readonly FieldInfo SlowlyActivateMutatorButtonsCoroutineField = AccessTools.Field(typeof(global::MutatorSelectionDialog), "slowlyActivateMutatorButtonsCoroutine")!;
            private static readonly FieldInfo RemoveAllButtonField = AccessTools.Field(typeof(global::MutatorSelectionDialog), "removeAllButton")!;
            private static readonly FieldInfo RandomizeButtonField = AccessTools.Field(typeof(global::MutatorSelectionDialog), "randomizeButton")!;
            private static readonly FieldInfo PreviewButtonField = AccessTools.Field(typeof(global::MutatorSelectionDialog), "previewButton")!;
            private static readonly FieldInfo PreviewButtonLabelField = AccessTools.Field(typeof(global::MutatorSelectionDialog), "previewButtonLabel")!;
            private static readonly FieldInfo MutatorSelectable1Field = AccessTools.Field(typeof(global::MutatorSelectionDialog), "mutatorSelectable1")!;
            private static readonly FieldInfo MutatorSelectable2Field = AccessTools.Field(typeof(global::MutatorSelectionDialog), "mutatorSelectable2")!;
            private static readonly FieldInfo MutatorSelectable3Field = AccessTools.Field(typeof(global::MutatorSelectionDialog), "mutatorSelectable3")!;
            private static readonly FieldInfo TooltipProvider1Field = AccessTools.Field(typeof(global::MutatorSelectionDialog), "tooltipProvider1")!;
            private static readonly FieldInfo TooltipProvider2Field = AccessTools.Field(typeof(global::MutatorSelectionDialog), "tooltipProvider2")!;
            private static readonly FieldInfo TooltipProvider3Field = AccessTools.Field(typeof(global::MutatorSelectionDialog), "tooltipProvider3")!;

            private readonly global::MutatorSelectionDialog _dialog;
            private int _registeredMutatorCount = -1;
            private bool _mutatorActivationComplete;
            private bool _initialMutatorFocusDone;

            public MutatorSelectionDialogScreen(global::MutatorSelectionDialog dialog)
            {
                _dialog = dialog;
            }

            public override void OnPush()
            {
                base.OnPush();
                TryFocusFirstMutator();
            }

            public override void OnUpdate()
            {
                if (_dialog == null || !_dialog.IsOpen)
                {
                    ScreenManager.RemoveFromTree(this);
                    return;
                }

                int mutatorCount = MutatorButtonCount();
                if (mutatorCount != _registeredMutatorCount)
                {
                    BuildRegistry();
                }

                bool becameReady = UpdateMutatorActivationComplete();
                TryFocusFirstMutator();
                if (becameReady)
                {
                    UIManager.ForceReannounceCurrentFocus();
                }
            }

            public override bool ShouldAnnounceFocus(UIElement element)
            {
                return _mutatorActivationComplete && base.ShouldAnnounceFocus(element);
            }

            protected override void BuildRegistry()
            {
                ClearRegistry();
                ListContainer root = new ListContainer
                {
                    AnnounceName = false,
                    AnnouncePosition = true,
                    NavigationAxis = NavigationAxis.Vertical
                };
                RootElement = root;

                AddMutatorButtons(root);
                AddPreview(root, MutatorSelectable1Field, TooltipProvider1Field);
                AddPreview(root, MutatorSelectable2Field, TooltipProvider2Field);
                AddPreview(root, MutatorSelectable3Field, TooltipProvider3Field);
                AddButton(root, Get<GameUISelectableButton>(_dialog, RemoveAllButtonField), () => Message.Localized("ui", "RUN_SETUP.MUTATORS.REMOVE_ALL"));
                AddButton(root, Get<GameUISelectableButton>(_dialog, RandomizeButtonField), () => Message.Localized("ui", "RUN_SETUP.MUTATORS.RANDOMIZE"));
                AddButton(root, Get<GameUISelectableButton>(_dialog, PreviewButtonField), () => PreviewButtonLabel());
                _registeredMutatorCount = MutatorButtonCount();
            }

            private void TryFocusFirstMutator()
            {
                if (_initialMutatorFocusDone || !_mutatorActivationComplete)
                {
                    return;
                }

                ListContainer root = RootElement as ListContainer;
                if (root == null)
                {
                    return;
                }

                UIElement focusedChild = root.FocusedChild;
                if (focusedChild is ProxyMutatorButton && focusedChild.IsVisible)
                {
                    root.SetFocusTo(focusedChild);
                    _initialMutatorFocusDone = true;
                    return;
                }

                for (int i = 0; i < root.Children.Count; i++)
                {
                    if (root.Children[i] is ProxyMutatorButton && root.Children[i].IsVisible)
                    {
                        root.SetFocusIndex(i);
                        _initialMutatorFocusDone = true;
                        return;
                    }
                }
            }

            private bool UpdateMutatorActivationComplete()
            {
                if (_mutatorActivationComplete)
                {
                    return false;
                }

                if (MutatorButtonCount() == 0)
                {
                    return false;
                }

                CoroutineController activation = Get<CoroutineController>(_dialog, SlowlyActivateMutatorButtonsCoroutineField);
                if (activation != null && activation.IsRunning())
                {
                    return false;
                }

                _mutatorActivationComplete = true;
                return true;
            }

            private int MutatorButtonCount()
            {
                List<MutatorButtonUI> mutators = Get<List<MutatorButtonUI>>(_dialog, MutatorButtonsField);
                return mutators?.Count ?? 0;
            }

            private void AddMutatorButtons(ListContainer root)
            {
                List<MutatorButtonUI> mutators = Get<List<MutatorButtonUI>>(_dialog, MutatorButtonsField);
                if (mutators == null)
                {
                    return;
                }

                for (int i = 0; i < mutators.Count; i++)
                {
                    MutatorButtonUI mutator = mutators[i];
                    if (mutator == null || mutator.Button == null)
                    {
                        continue;
                    }

                    ProxyMutatorButton element = new ProxyMutatorButton(mutator);
                    root.Add(element);
                    Register(element, mutator.gameObject, mutator.Button.gameObject);
                }
            }

            private void AddPreview(ListContainer root, FieldInfo selectableField, FieldInfo tooltipField)
            {
                GameUISelectableWithNavigation selectable = Get<GameUISelectableWithNavigation>(_dialog, selectableField);
                TooltipProviderComponent tooltip = Get<TooltipProviderComponent>(_dialog, tooltipField);
                if (selectable == null || selectable.component == null)
                {
                    return;
                }

                GameObject target = selectable.component.gameObject;
                GameObject tooltipTarget = tooltip != null ? tooltip.gameObject : target;
                ProxyRunSetupMutatorPreview element = new ProxyRunSetupMutatorPreview(selectable, tooltip);
                root.Add(element);
                Register(element, target, tooltipTarget);
            }

            private void AddButton(ListContainer root, GameUISelectableButton button, System.Func<Message> label)
            {
                if (button == null)
                {
                    return;
                }

                ProxyRunSetupButton element = new ProxyRunSetupButton(button, label);
                root.Add(element);
                Register(button.gameObject, element);
            }

            private Message PreviewButtonLabel()
            {
                string label = FirstText(ReadLabel(_dialog, PreviewButtonLabelField), ResolveButtonLabel(Get<GameUISelectableButton>(_dialog, PreviewButtonField)));
                return !string.IsNullOrWhiteSpace(label)
                    ? Message.FromText(label)
                    : Message.Localized("ui", "RUN_SETUP.MUTATORS.PREVIEW");
            }
        }

        private static string ResolveCovenantLabel(global::CovenantSelectionUI covenantSelectionUI, GameUISelectableButton button)
        {
            string value = FirstText(
                TooltipText.FirstTitle(covenantSelectionUI?.CovenantUITooltipProvider),
                ResolveCovenantLevelLabel(covenantSelectionUI),
                ResolveButtonLabel(button));
            return FormatGameLabel(value, "ScreenRunSetup_CovenantRank", "ScreenClassSelection_CovenantLabel");
        }

        private static string ResolveButtonLabel(GameUISelectableButton button)
        {
            return FirstText(
                GameUIButtonSupport.ResolveLabel(button),
                AuthoredLabelReader.Read(button));
        }

        private static string ResolveSettingsLabel(GameUISelectableButton button)
        {
            return FirstText(
                ResolveButtonLabel(button),
                LocalizeTerm("ScreenSettings_SettingsOptions"));
        }

        private static string ResolveCovenantLevelLabel(global::CovenantSelectionUI covenantSelectionUI)
        {
            if (covenantSelectionUI == null)
            {
                return string.Empty;
            }

            int level = covenantSelectionUI.CurrentLevel;
            if (level <= 0)
            {
                return FirstText(
                    LocalizeTerm("ScreenRunSetup_NoCovenantTitle"),
                    LocalizeTerm("ScreenClassSelection_CovenantDisabled_Title"));
            }

            global::ChallengeCovenantUI covenantUI = Get<global::ChallengeCovenantUI>(covenantSelectionUI, ChallengeCovenantUIField);
            global::AllGameData allGameData = Get<global::AllGameData>(covenantUI, ChallengeCovenantAllGameDataField);
            global::ChallengeCovenantDisplayData displayData = allGameData?.GetChallengeCovenantDisplayData();
            return displayData != null ? displayData.GetChallengeLevelString(level) : string.Empty;
        }

        private static string FormatGameLabel(string value, params string[] labelTerms)
        {
            value = Message.Clean(value);
            string label = LocalizeFirstTerm(labelTerms);
            if (string.IsNullOrWhiteSpace(label))
            {
                return value;
            }
            if (string.IsNullOrWhiteSpace(value))
            {
                return label;
            }

            string format = LocalizeTerm("TextFormat_Colon");
            if (!string.IsNullOrWhiteSpace(format))
            {
                return Message.Clean(string.Format(format, label, value));
            }

            return Message.Clean(label + ": " + value);
        }

        private static string LocalizeFirstTerm(params string[] terms)
        {
            if (terms == null)
            {
                return string.Empty;
            }

            for (int i = 0; i < terms.Length; i++)
            {
                string text = LocalizeTerm(terms[i]);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    return text;
                }
            }

            return string.Empty;
        }

        private static string LocalizeTerm(string term)
        {
            if (string.IsNullOrWhiteSpace(term) || !term.HasTranslation())
            {
                return string.Empty;
            }

            return AccessibilityText.LocalizeTerm(term);
        }

        private static string FirstText(params string[] values)
        {
            if (values == null)
            {
                return string.Empty;
            }

            for (int i = 0; i < values.Length; i++)
            {
                string value = Message.Clean(values[i]);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return string.Empty;
        }
    }
}

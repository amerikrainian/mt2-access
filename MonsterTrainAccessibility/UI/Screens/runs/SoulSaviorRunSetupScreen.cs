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
    internal sealed class SoulSaviorRunSetupScreen : GameScreen
    {
        private static readonly FieldInfo AllGameDataField = AccessTools.Field(typeof(global::SoulSaviorRunSetupScreen), "allGameData")!;
        private static readonly FieldInfo MainClassInfoField = AccessTools.Field(typeof(global::SoulSaviorRunSetupScreen), "mainClassInfo")!;
        private static readonly FieldInfo SubClassInfoField = AccessTools.Field(typeof(global::SoulSaviorRunSetupScreen), "subClassInfo")!;
        private static readonly FieldInfo PyreHeartInfoField = AccessTools.Field(typeof(global::SoulSaviorRunSetupScreen), "pyreHeartInfo")!;
        private static readonly FieldInfo DifficultyTierInfoField = AccessTools.Field(typeof(global::SoulSaviorRunSetupScreen), "difficultyTierInfo")!;
        private static readonly FieldInfo SaveManagerField = AccessTools.Field(typeof(global::SoulSaviorRunSetupScreen), "saveManager")!;
        private static readonly FieldInfo ClanSelectionDialogField = AccessTools.Field(typeof(global::SoulSaviorRunSetupScreen), "clanSelectionDialog")!;
        private static readonly FieldInfo PyreHeartSelectionDialogField = AccessTools.Field(typeof(global::SoulSaviorRunSetupScreen), "pyreHeartSelectionDialog")!;
        private static readonly FieldInfo DifficultyTierSelectionDialogField = AccessTools.Field(typeof(global::SoulSaviorRunSetupScreen), "difficultyTierSelectionDialog")!;
        private static readonly FieldInfo SoulSelectionDialogField = AccessTools.Field(typeof(global::SoulSaviorRunSetupScreen), "soulSelectionDialog")!;
        private static readonly FieldInfo SoulIdsField = AccessTools.Field(typeof(global::SoulSaviorRunSetupScreen), "soulIds")!;
        private static readonly FieldInfo MainClassButtonField = AccessTools.Field(typeof(global::SoulSaviorRunSetupScreen), "mainClassButton")!;
        private static readonly FieldInfo MainClassSwapChampionButtonField = AccessTools.Field(typeof(global::SoulSaviorRunSetupScreen), "mainClassSwapChampionButton")!;
        private static readonly FieldInfo SubClassButtonField = AccessTools.Field(typeof(global::SoulSaviorRunSetupScreen), "subClassButton")!;
        private static readonly FieldInfo SubClassSwapChampionButtonField = AccessTools.Field(typeof(global::SoulSaviorRunSetupScreen), "subClassSwapChampionButton")!;
        private static readonly FieldInfo PyreHeartButtonField = AccessTools.Field(typeof(global::SoulSaviorRunSetupScreen), "pyreHeartButton")!;
        private static readonly FieldInfo DifficultyTierButtonField = AccessTools.Field(typeof(global::SoulSaviorRunSetupScreen), "difficultyTierButton")!;
        private static readonly FieldInfo EditSoulsButtonField = AccessTools.Field(typeof(global::SoulSaviorRunSetupScreen), "editSoulsButton")!;
        private static readonly FieldInfo StartButtonField = AccessTools.Field(typeof(global::SoulSaviorRunSetupScreen), "startButton")!;
        private static readonly FieldInfo SettingsButtonField = AccessTools.Field(typeof(global::SoulSaviorRunSetupScreen), "settingsButton")!;
        private static readonly FieldInfo BackButtonField = AccessTools.Field(typeof(global::SoulSaviorRunSetupScreen), "backButton")!;

        private readonly global::SoulSaviorRunSetupScreen _screen;
        private ClanSelectionDialogScreen _clanSelectionScreen;
        private PyreHeartSelectionDialogScreen _pyreHeartSelectionScreen;
        private DifficultyTierSelectionDialogScreen _difficultyTierSelectionScreen;
        private SoulSelectionDialogScreen _soulSelectionScreen;
        private string _championSignature;

        public SoulSaviorRunSetupScreen(global::SoulSaviorRunSetupScreen screen)
        {
            _screen = screen;
        }

        public override void OnPush()
        {
            base.OnPush();
            (RootElement as ListContainer)?.FocusFirst();
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

            SaveManager saveManager = Get<SaveManager>(_screen, SaveManagerField);
            AllGameData allGameData = Get<AllGameData>(_screen, AllGameDataField);
            global::RunSetupClassLevelInfoUI mainInfo = Get<global::RunSetupClassLevelInfoUI>(_screen, MainClassInfoField);
            global::RunSetupClassLevelInfoUI subInfo = Get<global::RunSetupClassLevelInfoUI>(_screen, SubClassInfoField);

            AddClanSummary(root, Get<GameUISelectableButton>(_screen, MainClassButtonField), mainInfo, "ScreenRunSetup_MainClassTitle", "ScreenClassSelection_ClassLabel");
            AddChampionButton(root, Get<GameUISelectableButton>(_screen, MainClassSwapChampionButtonField), mainInfo, saveManager, () => SwapChampionLabel(mainInfo, "RUN_SETUP.SWAP_MAIN_CHAMPION"));
            AddClanSummary(root, Get<GameUISelectableButton>(_screen, SubClassButtonField), subInfo, "ScreenRunSetup_SubclassTitle", "ScreenClassSelection_SubclassLabel");
            AddChampionButton(root, Get<GameUISelectableButton>(_screen, SubClassSwapChampionButtonField), subInfo, saveManager, () => SwapChampionLabel(subInfo, "RUN_SETUP.SWAP_ALLIED_CHAMPION"));
            AddPyreHeartSummary(root, Get<GameUISelectableButton>(_screen, PyreHeartButtonField), Get<global::PyreHeartInfoUI>(_screen, PyreHeartInfoField));
            AddDifficultyTier(root, Get<GameUISelectableButton>(_screen, DifficultyTierButtonField), Get<global::DifficultyTierInfoUI>(_screen, DifficultyTierInfoField), allGameData);
            AddSoulSummary(root, Get<GameUISelectableButton>(_screen, EditSoulsButtonField), allGameData);
            AddButton(root, Get<GameUISelectableButton>(_screen, StartButtonField), () => GameButtonLabel(Get<GameUISelectableButton>(_screen, StartButtonField)));
            AddButton(root, Get<GameUISelectableButton>(_screen, SettingsButtonField), () => Message.RawCleaned(ResolveSettingsLabel(Get<GameUISelectableButton>(_screen, SettingsButtonField))));
            AddButton(root, Get<GameUISelectableButton>(_screen, BackButtonField), () => GameButtonLabel(Get<GameUISelectableButton>(_screen, BackButtonField)));

            _championSignature = ChampionSignature(mainInfo, subInfo);
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
            if (_difficultyTierSelectionScreen != null && _difficultyTierSelectionScreen.Parent == null)
            {
                _difficultyTierSelectionScreen = null;
            }
            if (_soulSelectionScreen != null && _soulSelectionScreen.Parent == null)
            {
                _soulSelectionScreen = null;
            }

            SaveManager saveManager = Get<SaveManager>(_screen, SaveManagerField);
            AllGameData allGameData = Get<AllGameData>(_screen, AllGameDataField);

            global::RunSetupClanSelectionUI clanDialog = Get<global::RunSetupClanSelectionUI>(_screen, ClanSelectionDialogField);
            if (clanDialog != null && clanDialog.IsOpen)
            {
                if (_clanSelectionScreen == null)
                {
                    _clanSelectionScreen = new ClanSelectionDialogScreen(clanDialog, saveManager);
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
                    _pyreHeartSelectionScreen = new PyreHeartSelectionDialogScreen(pyreDialog, saveManager);
                    PushChild(_pyreHeartSelectionScreen);
                }
            }
            else if (_pyreHeartSelectionScreen != null)
            {
                RemoveChild(_pyreHeartSelectionScreen);
                _pyreHeartSelectionScreen = null;
            }

            global::RunSetupDifficultyTierSelectionUI difficultyDialog = Get<global::RunSetupDifficultyTierSelectionUI>(_screen, DifficultyTierSelectionDialogField);
            if (difficultyDialog != null && difficultyDialog.IsOpen)
            {
                if (_difficultyTierSelectionScreen == null)
                {
                    _difficultyTierSelectionScreen = new DifficultyTierSelectionDialogScreen(difficultyDialog, allGameData, saveManager);
                    PushChild(_difficultyTierSelectionScreen);
                }
            }
            else if (_difficultyTierSelectionScreen != null)
            {
                RemoveChild(_difficultyTierSelectionScreen);
                _difficultyTierSelectionScreen = null;
            }

            global::SoulSelectionDialog soulDialog = Get<global::SoulSelectionDialog>(_screen, SoulSelectionDialogField);
            if (soulDialog != null && soulDialog.IsOpen)
            {
                if (_soulSelectionScreen == null)
                {
                    _soulSelectionScreen = new SoulSelectionDialogScreen(soulDialog, saveManager);
                    PushChild(_soulSelectionScreen);
                }
            }
            else if (_soulSelectionScreen != null)
            {
                RemoveChild(_soulSelectionScreen);
                _soulSelectionScreen = null;
            }
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

        private void AddClanSummary(ListContainer root, GameUISelectableButton button, global::RunSetupClassLevelInfoUI info, params string[] labelTerms)
        {
            if (button == null)
            {
                return;
            }

            ProxyRunSetupClanSummary element = new ProxyRunSetupClanSummary(button, info, labelTerms);
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

        private void AddDifficultyTier(ListContainer root, GameUISelectableButton button, global::DifficultyTierInfoUI info, AllGameData allGameData)
        {
            if (button == null)
            {
                return;
            }

            ProxyDifficultyTierSummary element = new ProxyDifficultyTierSummary(button, info, allGameData);
            root.Add(element);
            Register(button.gameObject, element);
        }

        private void AddSoulSummary(ListContainer root, GameUISelectableButton button, AllGameData allGameData)
        {
            if (button == null)
            {
                return;
            }

            ProxySoulSaviorSoulsButton element = new ProxySoulSaviorSoulsButton(button, allGameData, () => Get<List<string>>(_screen, SoulIdsField));
            root.Add(element);
            Register(button.gameObject, element);
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

        private sealed class DifficultyTierSelectionDialogScreen : ItemDialogScreen<global::RunSetupDifficultyTierSelectionItemUI>
        {
            private static readonly FieldInfo TierSelectionItemsField = AccessTools.Field(typeof(global::RunSetupDifficultyTierSelectionUI), "tierSelectionItems")!;
            private readonly global::RunSetupDifficultyTierSelectionUI _dialog;
            private readonly AllGameData _allGameData;
            private readonly SaveManager _saveManager;

            public DifficultyTierSelectionDialogScreen(global::RunSetupDifficultyTierSelectionUI dialog, AllGameData allGameData, SaveManager saveManager)
            {
                _dialog = dialog;
                _allGameData = allGameData;
                _saveManager = saveManager;
            }

            protected override bool IsOpen() => _dialog != null && _dialog.IsOpen;

            protected override IReadOnlyList<global::RunSetupDifficultyTierSelectionItemUI> GetItems()
            {
                return Get<List<global::RunSetupDifficultyTierSelectionItemUI>>(_dialog, TierSelectionItemsField);
            }

            protected override GameUISelectableButton GetButton(global::RunSetupDifficultyTierSelectionItemUI item)
            {
                return new ProxyDifficultyTierChoice(item, _allGameData, _saveManager).Button;
            }

            protected override UIElement CreateItemElement(global::RunSetupDifficultyTierSelectionItemUI item, GameUISelectableButton button)
            {
                return new ProxyDifficultyTierChoice(item, _allGameData, _saveManager);
            }
        }

        private sealed class SoulSelectionDialogScreen : GameScreen
        {
            private static readonly FieldInfo TabsField = AccessTools.Field(typeof(global::SoulSelectionDialog), "tabs")!;
            private static readonly FieldInfo UnitSoulItemsField = AccessTools.Field(typeof(global::SoulSelectionDialog), "unitSoulItems")!;
            private static readonly FieldInfo SpellSoulItemsField = AccessTools.Field(typeof(global::SoulSelectionDialog), "spellSoulItems")!;
            private static readonly FieldInfo ArtifactSoulItemsField = AccessTools.Field(typeof(global::SoulSelectionDialog), "artifactSoulItems")!;
            private static readonly FieldInfo RemoveAllButtonField = AccessTools.Field(typeof(global::SoulSelectionDialog), "removeAllButton")!;
            private static readonly FieldInfo RandomizeButtonField = AccessTools.Field(typeof(global::SoulSelectionDialog), "randomizeButton")!;
            private static readonly FieldInfo PreviewButtonField = AccessTools.Field(typeof(global::SoulSelectionDialog), "previewButton")!;
            private static readonly FieldInfo BackButtonField = AccessTools.Field(typeof(global::SoulSelectionDialog), "backButton")!;
            private static readonly FieldInfo DescriptionLabelField = AccessTools.Field(typeof(global::SoulSelectionDialog), "descriptionLabel")!;
            private static readonly FieldInfo ChosenSoulsField = AccessTools.Field(typeof(global::SoulSelectionDialog), "chosenSouls")!;
            private static readonly FieldInfo MaxSoulsField = AccessTools.Field(typeof(global::SoulSelectionDialog), "maxSouls")!;
            private static readonly FieldInfo SoulsNeededFor2ndSoulField = AccessTools.Field(typeof(global::SoulSelectionDialog), "soulsNeededFor2ndSoul")!;
            private static readonly FieldInfo SoulsNeededFor3rdSoulField = AccessTools.Field(typeof(global::SoulSelectionDialog), "soulsNeededFor3rdSoul")!;

            private readonly global::SoulSelectionDialog _dialog;
            private readonly SaveManager _saveManager;

            public SoulSelectionDialogScreen(global::SoulSelectionDialog dialog, SaveManager saveManager)
            {
                _dialog = dialog;
                _saveManager = saveManager;
            }

            public override void OnPush()
            {
                base.OnPush();
                TryFocusFirstSoul();
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
                    AnnouncePosition = true,
                    NavigationAxis = NavigationAxis.Vertical
                };
                RootElement = root;

                AddSummary(root);
                AddTabs(root);
                AddSoulItems(root, Get<List<global::RunSetupSoulSelectionItemUI>>(_dialog, UnitSoulItemsField));
                AddSoulItems(root, Get<List<global::RunSetupSoulSelectionItemUI>>(_dialog, SpellSoulItemsField));
                AddSoulItems(root, Get<List<global::RunSetupSoulSelectionItemUI>>(_dialog, ArtifactSoulItemsField));
                AddButton(root, Get<GameUISelectableButton>(_dialog, PreviewButtonField), () => Message.Localized("ui", "RUN_SETUP.SOULS.PREVIEW"));
                AddButton(root, Get<GameUISelectableButton>(_dialog, RandomizeButtonField), () => Message.Localized("ui", "RUN_SETUP.SOULS.RANDOMIZE"));
                AddButton(root, Get<GameUISelectableButton>(_dialog, RemoveAllButtonField), () => Message.Localized("ui", "RUN_SETUP.SOULS.REMOVE_ALL"));
                AddButton(root, Get<GameUISelectableButton>(_dialog, BackButtonField), () => GameButtonLabel(Get<GameUISelectableButton>(_dialog, BackButtonField)));
            }

            private void AddSummary(ListContainer root)
            {
                TMP_Text description = Get<TMP_Text>(_dialog, DescriptionLabelField);
                GameObject target = description != null ? description.gameObject : _dialog?.gameObject;
                GameObjectElement element = new GameObjectElement(
                    target,
                    label: SoulSummary);
                root.Add(element);
                Register(target, element);
            }

            private Message SoulSummary()
            {
                List<Message> parts = new List<Message>();
                TMP_Text description = Get<TMP_Text>(_dialog, DescriptionLabelField);
                Message descriptionText = Message.FromText(AccessibilityText.ReadLocalizedText(description));
                if (descriptionText != null)
                {
                    parts.Add(descriptionText);
                }

                int maxSouls = GetInt(_dialog, MaxSoulsField);
                int selectedSouls = SelectedSoulCount(maxSouls);
                parts.Add(Message.Localized("ui", "RUN_SETUP.SOULS.SELECTED_COUNT", new { selected = selectedSouls, max = maxSouls }));

                if (maxSouls < 2)
                {
                    parts.Add(Message.Localized("ui", "RUN_SETUP.SOULS.SECOND_SLOT_REQUIREMENT", new { count = GetInt(_dialog, SoulsNeededFor2ndSoulField) }));
                }

                if (maxSouls < 3)
                {
                    parts.Add(Message.Localized("ui", "RUN_SETUP.SOULS.THIRD_SLOT_REQUIREMENT", new { count = GetInt(_dialog, SoulsNeededFor3rdSoulField) }));
                }

                parts.RemoveAll(part => part == null);
                return parts.Count > 0 ? Message.JoinLines(parts) : null;
            }

            private int SelectedSoulCount(int maxSouls)
            {
                List<string> chosenSouls = Get<List<string>>(_dialog, ChosenSoulsField);
                if (chosenSouls == null)
                {
                    return 0;
                }

                for (int i = 0; i < chosenSouls.Count; i++)
                {
                    if (chosenSouls[i] == "random")
                    {
                        return maxSouls;
                    }
                }

                return chosenSouls.Count;
            }

            private void TryFocusFirstSoul()
            {
                ListContainer root = RootElement as ListContainer;
                if (root == null)
                {
                    return;
                }

                for (int i = 0; i < root.Children.Count; i++)
                {
                    if (root.Children[i] is ProxySoulSelectionItem && root.Children[i].IsVisible)
                    {
                        root.SetFocusIndex(i);
                        return;
                    }
                }

                root.FocusFirst();
            }

            private void AddTabs(ListContainer root)
            {
                List<global::SettingsTab> tabs = Get<List<global::SettingsTab>>(_dialog, TabsField);
                if (tabs == null || tabs.Count == 0)
                {
                    return;
                }

                ListContainer tabList = new ListContainer
                {
                    AnnounceName = false,
                    AnnouncePosition = true,
                    NavigationAxis = NavigationAxis.Horizontal
                };

                for (int i = 0; i < tabs.Count; i++)
                {
                    ProxySettingsTab element = new ProxySettingsTab(tabs[i]);
                    if (element.Button == null)
                    {
                        continue;
                    }

                    tabList.Add(element);
                    Register(element.Button.gameObject, element);
                }

                if (tabList.Children.Count > 0)
                {
                    root.Add(tabList);
                }
            }

            private void AddSoulItems(ListContainer root, IReadOnlyList<global::RunSetupSoulSelectionItemUI> items)
            {
                if (items == null)
                {
                    return;
                }

                for (int i = 0; i < items.Count; i++)
                {
                    ProxySoulSelectionItem element = new ProxySoulSelectionItem(items[i], _saveManager);
                    if (element.Button == null)
                    {
                        continue;
                    }

                    root.Add(element);
                    Register(element.Button.gameObject, element);
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

            private static int GetInt(object owner, FieldInfo field)
            {
                return owner != null ? (int)field.GetValue(owner) : 0;
            }
        }
    }
}

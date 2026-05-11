using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Speech;
using MonsterTrainAccessibility.UI.Elements;
using ShinyShoe;
using UnityEngine;

namespace MonsterTrainAccessibility.UI.Screens
{
    internal sealed class KeyMappingScreen : ListNavigationGameScreen
    {
        private static readonly FieldInfo RowsField = AccessTools.Field(typeof(global::KeyMappingScreen), "rows")!;
        private static readonly FieldInfo ResetButtonField = AccessTools.Field(typeof(global::KeyMappingScreen), "resetButton")!;
        private static readonly FieldInfo CurrentlyRemappingField = AccessTools.Field(typeof(global::KeyMappingScreen), "currentlyRemapping")!;
        private static readonly FieldInfo RebindButtonField = AccessTools.Field(typeof(global::KeyMappingRow), "rebindButton")!;
        private static readonly FieldInfo AddButtonField = AccessTools.Field(typeof(global::KeyMappingRow), "addButton")!;
        private static readonly FieldInfo DeleteButtonField = AccessTools.Field(typeof(global::KeyMappingRow), "deleteButton")!;

        private readonly global::KeyMappingScreen _screen;
        private string _announcedRemappingKey;
        private bool _wasRemapping;
        private bool _forceRebuildAnnouncement;

        public KeyMappingScreen(global::KeyMappingScreen screen)
        {
            _screen = screen;
            RootList.AnnouncePosition = true;
        }

        public override bool ShouldRestoreNavigationFocus() => false;

        public override void OnUpdate()
        {
            bool isRemapping = IsRemapping();
            if (_wasRemapping && !isRemapping)
            {
                _forceRebuildAnnouncement = true;
            }

            base.OnUpdate();
            AnnounceRemappingMode();

            if (_forceRebuildAnnouncement)
            {
                _forceRebuildAnnouncement = false;
                UIManager.ForceReannounceCurrentFocus();
            }

            _wasRemapping = isRemapping;
        }

        protected override void PopulateList()
        {
            List<global::KeyMappingRow> rows = Get<List<global::KeyMappingRow>>(_screen, RowsField);
            if (rows != null)
            {
                for (int i = 0; i < rows.Count; i++)
                {
                    AddRow(rows[i]);
                }
            }

            GameUISelectableButton resetButton = Get<GameUISelectableButton>(_screen, ResetButtonField);
            if (resetButton != null)
            {
                AddElement(new LabeledButton(
                    resetButton,
                    () => Message.Localized("ui", "KEY_MAPPING.RESET"),
                    visibility: () => resetButton.gameObject.activeInHierarchy),
                    resetButton.gameObject);
            }
        }

        protected override string BuildSignature()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            List<global::KeyMappingRow> rows = Get<List<global::KeyMappingRow>>(_screen, RowsField);
            if (rows != null)
            {
                for (int i = 0; i < rows.Count; i++)
                {
                    global::KeyMappingRow row = rows[i];
                    global::KeyMappingScreen.KeyMappingRowData data = row?.keyMappingData;
                    sb.Append(row != null && row.gameObject.activeInHierarchy)
                        .Append(':')
                        .Append(data?.mappingDef?.name)
                        .Append(':')
                        .Append(data?.keyDef?.name)
                        .Append('|');
                }
            }

            GameUISelectableButton resetButton = Get<GameUISelectableButton>(_screen, ResetButtonField);
            sb.Append(";reset:").Append(resetButton != null && resetButton.gameObject.activeInHierarchy);
            return sb.ToString();
        }

        private void AddRow(global::KeyMappingRow row)
        {
            if (row == null || !row.gameObject.activeInHierarchy || row.keyMappingData == null)
            {
                return;
            }

            global::KeyMappingScreen.KeyMappingRowData data = row.keyMappingData;
            KeyMappingRowContainer container = new KeyMappingRowContainer(data.mappingDef?.name)
            {
                AnnounceName = false,
                AnnouncePosition = true
            };

            KeyCellElement keyElement = new KeyCellElement(
                row.gameObject,
                () => Message.RawCleaned(RowSummary(data)));
            container.Add(keyElement);

            AddButton(container, Get<GameUISelectableButton>(row, RebindButtonField), "KEY_MAPPING.REBIND");
            AddButton(container, Get<GameUISelectableButton>(row, AddButtonField), "KEY_MAPPING.ADD");
            AddButton(container, Get<GameUISelectableButton>(row, DeleteButtonField), "KEY_MAPPING.DELETE");

            if (container.Children.Count > 0)
            {
                RootList.Add(container);
                Register(row.gameObject, keyElement);
            }
        }

        private void AnnounceRemappingMode()
        {
            global::KeyMappingScreen.KeyMappingRowData data =
                CurrentlyRemappingField.GetValue(_screen) as global::KeyMappingScreen.KeyMappingRowData;
            if (data == null)
            {
                _announcedRemappingKey = null;
                return;
            }

            string key = data.mappingDef?.name ?? string.Empty;
            if (string.Equals(key, _announcedRemappingKey, System.StringComparison.Ordinal))
            {
                return;
            }

            _announcedRemappingKey = key;
            SpeechManager.Output(Message.Localized("ui", "KEY_MAPPING.PRESS_KEY", new { control = MappingName(data) }));
        }

        protected override bool ShouldSuppressUnchangedRebuildAnnouncement(
            UIElement oldFocused,
            UIElement newFocused,
            string oldAnnouncement,
            string newAnnouncement)
        {
            return !_forceRebuildAnnouncement;
        }

        private void AddButton(ListContainer container, GameUISelectableButton button, string fallbackLabelKey)
        {
            if (container == null || button == null)
            {
                return;
            }

            GameObject target = button.gameObject;
            GameObjectElement element = new GameObjectElement(
                target,
                "button",
                () => ButtonLabel(button, fallbackLabelKey),
                status: () => GameButtonElement.StateMessage(button),
                visibility: () => target.activeInHierarchy);
            container.Add(element);
            Register(target, element);
        }

        private static Message ButtonLabel(GameUISelectableButton button, string fallbackLabelKey)
        {
            Message label = Message.RawCleaned(GameUIButtonSupport.ResolveLabel(button));
            return label ?? Message.Localized("ui", fallbackLabelKey);
        }

        private bool IsRemapping()
        {
            return CurrentlyRemappingField.GetValue(_screen) != null;
        }

        private static string RowSummary(global::KeyMappingScreen.KeyMappingRowData data)
        {
            string mapping = MappingName(data);
            string key = KeyText(data);
            if (string.IsNullOrWhiteSpace(mapping))
            {
                return key;
            }

            if (string.IsNullOrWhiteSpace(key))
            {
                return mapping;
            }

            return mapping + ", " + key;
        }

        private static string KeyText(global::KeyMappingScreen.KeyMappingRowData data)
        {
            return data?.keyDef != null
                ? data.keyDef.GetDisplayName()
                : "ScreenKeyMapping_UnsetKey".Localize();
        }

        private static string MappingName(global::KeyMappingScreen.KeyMappingRowData data)
        {
            return data?.mappingDef?.GetDisplayName() ?? string.Empty;
        }

        private static T Get<T>(object owner, FieldInfo field) where T : class
        {
            return owner != null ? field.GetValue(owner) as T : null;
        }

        protected override void RestoreFocusAfterRebuild(int oldIndex, UIElement oldFocused)
        {
            KeyMappingRowContainer oldRow = oldFocused as KeyMappingRowContainer;
            if (oldRow == null || string.IsNullOrEmpty(oldRow.MappingDefName))
            {
                base.RestoreFocusAfterRebuild(oldIndex, oldFocused);
                return;
            }

            int childIndex = oldRow.FocusIndex;
            for (int i = 0; i < RootList.Children.Count; i++)
            {
                KeyMappingRowContainer row = RootList.Children[i] as KeyMappingRowContainer;
                if (row != null && string.Equals(row.MappingDefName, oldRow.MappingDefName, System.StringComparison.Ordinal))
                {
                    RootList.SetFocusIndex(i);
                    if (childIndex >= 0)
                    {
                        row.SetFocusIndex(childIndex);
                    }
                    return;
                }
            }

            base.RestoreFocusAfterRebuild(oldIndex, oldFocused);
        }

        private sealed class KeyMappingRowContainer : ListContainer
        {
            public KeyMappingRowContainer(string mappingDefName)
                : base(null, NavigationAxis.Horizontal)
            {
                MappingDefName = mappingDefName;
            }

            public string MappingDefName { get; }

            public override Message GetPositionString(UIElement child)
            {
                if (child != null && Children.Count > 0 && ReferenceEquals(child, Children[0]))
                {
                    return Parent?.GetPositionString(this);
                }

                return null;
            }
        }

        private sealed class KeyCellElement : CustomElement, INavigationTargetElement
        {
            private readonly GameObject _target;

            public KeyCellElement(GameObject target, System.Func<Message> label)
                : base(label: label)
            {
                _target = target;
            }

            public void SelectForNavigation()
            {
                if (_target == null)
                {
                    return;
                }

                UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(_target);
            }
        }
    }
}

using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.UI.Elements;
using ShinyShoe;
using UnityEngine;

namespace MonsterTrainAccessibility.UI.Screens
{
    internal sealed class TrainCosmeticsScreen : ListNavigationGameScreen
    {
        private static readonly FieldInfo TopRoomSelectionUIField = AccessTools.Field(typeof(global::TrainCosmeticsScreen), "topRoomSelectionUI")!;
        private static readonly FieldInfo MidRoomSelectionUIField = AccessTools.Field(typeof(global::TrainCosmeticsScreen), "midRoomSelectionUI")!;
        private static readonly FieldInfo BtmRoomSelectionUIField = AccessTools.Field(typeof(global::TrainCosmeticsScreen), "btmRoomSelectionUI")!;
        private static readonly FieldInfo RoomSelectionDialogField = AccessTools.Field(typeof(global::TrainCosmeticsScreen), "roomSelectionDialog")!;
        private static readonly FieldInfo BackButtonField = AccessTools.Field(typeof(global::TrainCosmeticsScreen), "backButton")!;
        private static readonly FieldInfo TopRoomDataField = AccessTools.Field(typeof(global::TrainCosmeticsScreen), "topRoomData")!;
        private static readonly FieldInfo MidRoomDataField = AccessTools.Field(typeof(global::TrainCosmeticsScreen), "midRoomData")!;
        private static readonly FieldInfo BtmRoomDataField = AccessTools.Field(typeof(global::TrainCosmeticsScreen), "btmRoomData")!;
        private static readonly FieldInfo SelectedSelectionUIField = AccessTools.Field(typeof(global::TrainCosmeticsScreen), "selectedSelectionUI")!;
        private static readonly FieldInfo DialogItemsField = AccessTools.Field(typeof(global::TrainCosmeticRoomSelectionUI), "trainCosmeticsRoomSelectionItems")!;
        private static readonly FieldInfo DialogSaveManagerField = AccessTools.Field(typeof(global::TrainCosmeticRoomSelectionUI), "saveManager")!;

        private readonly global::TrainCosmeticsScreen _screen;
        private TrainRoomSelectionDialogScreen _dialogScreen;

        public TrainCosmeticsScreen(global::TrainCosmeticsScreen screen)
        {
            _screen = screen;
        }

        public override void OnFocus()
        {
            SyncDialog();
            if (ActiveChild != null)
            {
                return;
            }

            base.OnFocus();
        }

        public override void OnUpdate()
        {
            SyncDialog();
            if (ActiveChild != null)
            {
                return;
            }

            base.OnUpdate();
        }

        public override UIElement GetElement(GameObject go)
        {
            SyncDialog();
            return base.GetElement(go);
        }

        protected override void PopulateList()
        {
            AddRoomButton(Get<global::TrainCosmeticsSelectionUI>(_screen, TopRoomSelectionUIField), () => Get<RoomData>(_screen, TopRoomDataField));
            AddRoomButton(Get<global::TrainCosmeticsSelectionUI>(_screen, MidRoomSelectionUIField), () => Get<RoomData>(_screen, MidRoomDataField));
            AddRoomButton(Get<global::TrainCosmeticsSelectionUI>(_screen, BtmRoomSelectionUIField), () => Get<RoomData>(_screen, BtmRoomDataField));
            AddBackButton();
        }

        protected override string BuildSignature()
        {
            return "main:" +
                Get<RoomData>(_screen, TopRoomDataField)?.GetID() + "|" +
                Get<RoomData>(_screen, MidRoomDataField)?.GetID() + "|" +
                Get<RoomData>(_screen, BtmRoomDataField)?.GetID();
        }

        protected override void RestoreFocusAfterRebuild(int oldIndex, UIElement oldFocused)
        {
            RootList.SetFocusIndex(oldIndex);
        }

        private void SyncDialog()
        {
            global::TrainCosmeticRoomSelectionUI dialog = Get<global::TrainCosmeticRoomSelectionUI>(_screen, RoomSelectionDialogField);
            if (IsDialogOpen(dialog))
            {
                if (_dialogScreen == null || !ReferenceEquals(_dialogScreen.Dialog, dialog))
                {
                    if (_dialogScreen != null && ReferenceEquals(ActiveChild, _dialogScreen))
                    {
                        RemoveChild(_dialogScreen);
                    }

                    _dialogScreen = new TrainRoomSelectionDialogScreen(this, dialog);
                    PushChild(_dialogScreen);
                }

                return;
            }

            if (_dialogScreen != null)
            {
                TrainRoomSelectionDialogScreen dialogScreen = _dialogScreen;
                _dialogScreen = null;
                if (ReferenceEquals(ActiveChild, dialogScreen))
                {
                    RemoveChild(dialogScreen);
                }

                FocusSelectedRoomButton();
            }
        }

        private void DialogClosed(TrainRoomSelectionDialogScreen dialogScreen)
        {
            if (!ReferenceEquals(_dialogScreen, dialogScreen))
            {
                return;
            }

            _dialogScreen = null;
            FocusSelectedRoomButton();
        }

        private void AddRoomButton(global::TrainCosmeticsSelectionUI selection, System.Func<RoomData> room)
        {
            if (selection == null)
            {
                return;
            }

            ProxyTrainRoomCosmeticButton element = new ProxyTrainRoomCosmeticButton(selection, room);
            if (element.Button == null)
            {
                return;
            }

            AddElement(element, selection.gameObject, element.Button.gameObject);
        }

        private void AddBackButton()
        {
            GameUISelectableButton button = Get<GameUISelectableButton>(_screen, BackButtonField);
            if (button == null)
            {
                return;
            }

            LabeledButton element = new LabeledButton(button, () => AuthoredLabelReader.ReadMessage(button));
            AddElement(element, button.gameObject);
        }

        private bool FocusSelectedRoomButton()
        {
            global::TrainCosmeticsSelectionUI selected = Get<global::TrainCosmeticsSelectionUI>(_screen, SelectedSelectionUIField);
            if (selected == null)
            {
                return false;
            }

            for (int i = 0; i < RootList.Children.Count; i++)
            {
                if (RootList.Children[i] is ProxyTrainRoomCosmeticButton roomButton &&
                    ReferenceEquals(roomButton.Selection, selected))
                {
                    RootList.SetFocusIndex(i);
                    return true;
                }
            }

            return false;
        }

        private sealed class TrainRoomSelectionDialogScreen : ListNavigationGameScreen
        {
            private readonly TrainCosmeticsScreen _owner;

            public TrainRoomSelectionDialogScreen(TrainCosmeticsScreen owner, global::TrainCosmeticRoomSelectionUI dialog)
            {
                _owner = owner;
                Dialog = dialog;
            }

            public global::TrainCosmeticRoomSelectionUI Dialog { get; }

            public override void OnUpdate()
            {
                if (!IsDialogOpen(Dialog))
                {
                    _owner.DialogClosed(this);
                    ScreenManager.RemoveFromTree(this);
                    return;
                }

                base.OnUpdate();
            }

            protected override void PopulateList()
            {
                List<global::TrainCosmeticRoomSelectionItemUI> items = Get<List<global::TrainCosmeticRoomSelectionItemUI>>(Dialog, DialogItemsField);
                SaveManager saveManager = Get<SaveManager>(Dialog, DialogSaveManagerField);
                if (items == null)
                {
                    return;
                }

                for (int i = 0; i < items.Count; i++)
                {
                    ProxyTrainRoomCosmeticChoice element = new ProxyTrainRoomCosmeticChoice(items[i], saveManager);
                    if (element.Button == null)
                    {
                        continue;
                    }

                    AddElement(element, items[i].gameObject, element.Button.gameObject);
                }
            }

            protected override string BuildSignature()
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder("dialog:");
                List<global::TrainCosmeticRoomSelectionItemUI> items = Get<List<global::TrainCosmeticRoomSelectionItemUI>>(Dialog, DialogItemsField);
                SaveManager saveManager = Get<SaveManager>(Dialog, DialogSaveManagerField);
                if (items != null)
                {
                    for (int i = 0; i < items.Count; i++)
                    {
                        RoomData room = items[i]?.RoomData;
                        sb.Append(room?.GetID()).Append(':');
                        sb.Append(room != null && saveManager != null && saveManager.GetMetagameSave().HasUnlockedRoomCosmetic(room.GetID()) ? '1' : '0').Append('|');
                    }
                }

                return sb.ToString();
            }

            protected override void RestoreFocusAfterRebuild(int oldIndex, UIElement oldFocused)
            {
                RootList.SetFocusIndex(oldIndex);
            }
        }

        private static bool IsDialogOpen(global::TrainCosmeticRoomSelectionUI dialog)
        {
            return dialog != null && dialog.gameObject.activeInHierarchy;
        }

        private static T Get<T>(object owner, FieldInfo field) where T : class
        {
            return owner != null ? field.GetValue(owner) as T : null;
        }
    }
}

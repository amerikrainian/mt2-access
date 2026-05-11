using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Input;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Elements;
using ShinyShoe;

namespace MonsterTrainAccessibility.UI.Screens
{
    internal sealed class AccessibleDialogScreen : ListNavigationGameScreen
    {
        private static readonly FieldInfo ActiveDialogStackField = AccessTools.Field(typeof(global::DialogScreen), "activeDialogStack")!;
        private static readonly FieldInfo DialogDataField = AccessTools.Field(typeof(global::Dialog), "data")!;
        private static readonly FieldInfo Button1Field = AccessTools.Field(typeof(global::Dialog), "button1")!;
        private static readonly FieldInfo Button2Field = AccessTools.Field(typeof(global::Dialog), "button2")!;

        private readonly global::DialogScreen _screen;
        private string _lastAnnouncedContent;

        public AccessibleDialogScreen(global::DialogScreen screen)
        {
            _screen = screen;
            RootList.AnnouncePosition = false;
        }

        public override bool ShouldRestoreNavigationFocus() => false;

        public override bool ShouldAcceptGameSelection() => false;

        public override bool BlocksGameInput(InputAction action)
        {
            if (IsActionOnlyDialog())
            {
                return false;
            }

            return base.BlocksGameInput(action);
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            AnnounceContentWhenStable();
        }

        protected override void PopulateList()
        {
            global::Dialog dialog = TopDialog;
            if (dialog == null)
            {
                return;
            }

            global::Dialog.Data data = Get<global::Dialog.Data>(dialog, DialogDataField);
            AddButton(Get<GameUISelectableButton>(dialog, Button1Field), data?.button1Text);
            AddButton(Get<GameUISelectableButton>(dialog, Button2Field), data?.button2Text);
        }

        protected override string BuildSignature()
        {
            global::Dialog dialog = TopDialog;
            if (dialog == null)
            {
                return "none";
            }

            GameUISelectableButton button1 = Get<GameUISelectableButton>(dialog, Button1Field);
            GameUISelectableButton button2 = Get<GameUISelectableButton>(dialog, Button2Field);
            global::Dialog.Data data = Get<global::Dialog.Data>(dialog, DialogDataField);
            return dialog.GetInstanceID()
                + "|content:" + ReadContent(data)
                + "|b1:" + ButtonSignature(button1)
                + "|b2:" + ButtonSignature(button2);
        }

        private void AddButton(GameUISelectableButton button, string label)
        {
            if (button == null || !button.gameObject.activeInHierarchy || !Message.ShouldAdd(Message.Clean(label)))
            {
                return;
            }

            LabeledButton element = new LabeledButton(button, Message.FromText(label));
            AddElement(element, button.gameObject);
        }

        private void AnnounceContentWhenStable()
        {
            string text = ReadContent(Get<global::Dialog.Data>(TopDialog, DialogDataField));
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            if (string.Equals(text, _lastAnnouncedContent, System.StringComparison.Ordinal))
            {
                return;
            }

            _lastAnnouncedContent = text;
            Events.EventDispatcher.Enqueue(new Events.BasicEvent(Message.Raw(text)));
        }

        private global::Dialog TopDialog
        {
            get
            {
                List<global::Dialog> stack = Get<List<global::Dialog>>(_screen, ActiveDialogStackField);
                return stack != null && stack.Count > 0 ? stack[stack.Count - 1] : null;
            }
        }

        private bool IsActionOnlyDialog()
        {
            global::Dialog.Data data = Get<global::Dialog.Data>(TopDialog, DialogDataField);
            return data != null && data.hideAllButtons && data.applyScreenInput != null;
        }

        private static string ButtonSignature(GameUISelectableButton button)
        {
            if (button == null)
            {
                return "null";
            }

            return button.gameObject.activeInHierarchy
                + ":" + button.interactable
                + ":" + button.name;
        }

        private static string ReadContent(global::Dialog.Data data)
        {
            return Message.Clean(data?.content);
        }

        private static T Get<T>(object owner, FieldInfo field) where T : class
        {
            return owner != null ? field.GetValue(owner) as T : null;
        }
    }
}

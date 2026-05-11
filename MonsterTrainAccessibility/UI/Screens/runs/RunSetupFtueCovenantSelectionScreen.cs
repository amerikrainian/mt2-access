using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Elements;
using ShinyShoe;
using TMPro;

namespace MonsterTrainAccessibility.UI.Screens
{
    internal sealed class RunSetupFtueCovenantSelectionScreen : GameScreen
    {
        private static readonly FieldInfo ButtonCov0Field = AccessTools.Field(typeof(global::RunSetupFtueCovenantSelectionUI), "buttonCov0")!;
        private static readonly FieldInfo ButtonCov1Field = AccessTools.Field(typeof(global::RunSetupFtueCovenantSelectionUI), "buttonCov1")!;
        private static readonly FieldInfo LabelCov0Field = AccessTools.Field(typeof(global::RunSetupFtueCovenantSelectionUI), "labelCov0")!;
        private static readonly FieldInfo LabelCov1Field = AccessTools.Field(typeof(global::RunSetupFtueCovenantSelectionUI), "labelCov1")!;

        private readonly global::RunSetupFtueCovenantSelectionUI _dialog;

        public RunSetupFtueCovenantSelectionScreen(global::RunSetupFtueCovenantSelectionUI dialog)
        {
            _dialog = dialog;
        }

        public override bool ShouldAcceptGameSelection() => false;

        public override void OnPush()
        {
            base.OnPush();
            (RootElement as ListContainer)?.FocusFirst();
        }

        public override void OnFocus()
        {
            if (!IsOpen)
            {
                ScreenManager.RemoveFromTree(this);
                return;
            }

            if ((RootElement as ListContainer)?.FocusIndex >= 0)
            {
                (RootElement as ListContainer)?.SetFocusIndex((RootElement as ListContainer).FocusIndex);
                return;
            }

            (RootElement as ListContainer)?.FocusFirst();
        }

        public override void OnUpdate()
        {
            if (!IsOpen)
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

            AddChoice(root, Get<GameUISelectableButton>(_dialog, ButtonCov0Field), Get<TMP_Text>(_dialog, LabelCov0Field));
            AddChoice(root, Get<GameUISelectableButton>(_dialog, ButtonCov1Field), Get<TMP_Text>(_dialog, LabelCov1Field));
        }

        private void AddChoice(ListContainer root, GameUISelectableButton button, TMP_Text label)
        {
            if (button == null)
            {
                return;
            }

            LabeledButton element = new LabeledButton(
                button,
                () => Message.RawCleaned(AccessibilityText.ReadLocalizedText(label)),
                tooltip: () => ChoiceTooltip(button, label));
            root.Add(element);
            Register(element, button.gameObject);
        }

        private static Message ChoiceTooltip(GameUISelectableButton button, TMP_Text label)
        {
            if (button == null)
            {
                return null;
            }

            string labelText = Message.Clean(AccessibilityText.ReadLocalizedText(label));
            List<Message> parts = new List<Message>();
            HashSet<string> seen = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            if (Message.ShouldAdd(labelText))
            {
                seen.Add(labelText);
            }

            TMP_Text[] texts = button.GetComponentsInChildren<TMP_Text>(includeInactive: true);
            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = texts[i];
                if (text == null || text == label || !text.enabled || !text.gameObject.activeInHierarchy)
                {
                    continue;
                }

                string value = Message.Clean(AccessibilityText.ReadLocalizedText(text));
                if (!Message.ShouldAdd(value) || !seen.Add(value))
                {
                    continue;
                }

                parts.Add(Message.Raw(value));
            }

            return parts.Count > 0 ? Message.Join(" ", parts) : null;
        }

        private static T Get<T>(object owner, FieldInfo field) where T : class
        {
            return owner != null ? field.GetValue(owner) as T : null;
        }

        private bool IsOpen => _dialog != null && _dialog.IsConsumingInputs();
    }
}

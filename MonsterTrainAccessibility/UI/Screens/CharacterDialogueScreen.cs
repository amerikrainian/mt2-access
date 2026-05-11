using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Events;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Elements;
using ShinyShoe;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MonsterTrainAccessibility.UI.Screens
{
    internal sealed class CharacterDialogueScreen : ListNavigationGameScreen
    {
        private static readonly FieldInfo DialogueTextField = AccessTools.Field(typeof(global::ShinyShoe.CharacterDialogueScreen), "dialogueText")!;
        private static readonly FieldInfo SpeakerLabelField = AccessTools.Field(typeof(global::ShinyShoe.CharacterDialogueScreen), "speakerLabel")!;
        private static readonly FieldInfo SkipHintLabelField = AccessTools.Field(typeof(global::ShinyShoe.CharacterDialogueScreen), "label")!;
        private static readonly FieldInfo IsPhraseAnimatingField = AccessTools.Field(typeof(global::ShinyShoe.CharacterDialogueScreen), "isPhraseAnimating")!;
        private static readonly FieldInfo NextIndicatorField = AccessTools.Field(typeof(global::ShinyShoe.CharacterDialogueScreen), "nextIndicator")!;

        private readonly global::ShinyShoe.CharacterDialogueScreen _screen;
        private string _lastAnnouncedText;
        private string _lastFocusText;
        private bool _wasAdvanceReady;
        private bool _hasAnnouncedContinueFocus;
        private bool _suppressNextContinueFocusAnnouncement;
        private bool _forceNextContinueFocusAnnouncement;
        private UIElement _continueElement;
        private UIElement _skipElement;

        public CharacterDialogueScreen(global::ShinyShoe.CharacterDialogueScreen screen)
        {
            _screen = screen;
            RootList.AnnouncePosition = false;
        }

        public override bool ShouldRestoreNavigationFocus() => false;

        public override void OnPush()
        {
            base.OnPush();
            FocusDialogueOrWait();
        }

        public override bool BlocksGameInput(Input.InputAction action)
        {
            if (action?.Key == "ui_accept" || action?.Key == "ui_select")
            {
                return true;
            }

            return base.BlocksGameInput(action);
        }

        public override bool OnActionJustPressed(Input.InputAction action)
        {
            bool handled = base.OnActionJustPressed(action);
            if (IsManualNavigationAction(action?.Key) && ReferenceEquals(RootList.FocusedChild, _continueElement))
            {
                _forceNextContinueFocusAnnouncement = true;
            }

            return handled;
        }

        public override void OnFocus()
        {
            if (RootList.FocusIndex >= 0 && RootList.FocusedChild != null && RootList.FocusedChild.IsVisible)
            {
                RootList.SetFocusIndex(RootList.FocusIndex);
            }

            FocusDialogueOrWait();
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            SyncNavigationSelection();
            UpdateDialogueFocus();
            AnnounceDialogueIfReady();
        }

        public override bool ShouldAnnounceFocus(UIElement element)
        {
            if (IsPhraseAnimating())
            {
                return false;
            }

            if (ReferenceEquals(element, _continueElement))
            {
                if (_forceNextContinueFocusAnnouncement)
                {
                    _forceNextContinueFocusAnnouncement = false;
                    _suppressNextContinueFocusAnnouncement = false;
                    _hasAnnouncedContinueFocus = true;
                    return base.ShouldAnnounceFocus(element);
                }

                if (_suppressNextContinueFocusAnnouncement)
                {
                    _suppressNextContinueFocusAnnouncement = false;
                    return false;
                }

                _hasAnnouncedContinueFocus = true;
            }

            return base.ShouldAnnounceFocus(element);
        }

        protected override void PopulateList()
        {
            TMP_Text skipHint = Get<TMP_Text>(_screen, SkipHintLabelField);

            _continueElement = new ProxyCharacterDialogueContinue(this);
            AddElement(
                _continueElement,
                _screen.gameObject);

            _skipElement = new ProxyCharacterDialogueSkip(this);
            AddElement(_skipElement, skipHint != null ? skipHint.gameObject : null);
        }

        protected override string BuildSignature()
        {
            return CurrentDialogueText()
                + "|"
                + CanAdvanceDialogue().ToString(System.Globalization.CultureInfo.InvariantCulture)
                + "|"
                + IsPhraseAnimating().ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        protected override void RestoreFocusAfterRebuild(int oldIndex, UIElement oldFocused)
        {
            if (oldFocused is ProxyCharacterDialogueContinue && !CanAdvanceDialogue())
            {
                return;
            }

            base.RestoreFocusAfterRebuild(oldIndex, oldFocused);
        }

        private void AnnounceDialogueIfReady()
        {
            if (IsPhraseAnimating())
            {
                return;
            }

            string text = CurrentDialogueText();
            if (string.IsNullOrWhiteSpace(text) || string.Equals(text, _lastAnnouncedText, System.StringComparison.Ordinal))
            {
                return;
            }

            _lastAnnouncedText = text;
            EventDispatcher.Enqueue(new BasicEvent(Message.Raw(text)));
        }

        private void UpdateDialogueFocus()
        {
            string text = CurrentDialogueText();
            bool hasText = !string.IsNullOrWhiteSpace(text);
            if (hasText && !string.Equals(text, _lastFocusText, System.StringComparison.Ordinal))
            {
                _lastFocusText = text;
                _wasAdvanceReady = false;
            }

            bool advanceReady = CanAdvanceDialogue();
            if (!_wasAdvanceReady && advanceReady && _continueElement != null)
            {
                _suppressNextContinueFocusAnnouncement = _hasAnnouncedContinueFocus;
                RootList.SetFocusTo(_continueElement, selectForNavigation: false);
            }

            _wasAdvanceReady = advanceReady;
        }

        private void SyncNavigationSelection()
        {
            GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (selected == null)
            {
                return;
            }

            ClearGameSelection();
            UIElement focused = RootList.FocusedChild;
            if (focused != null)
            {
                UIManager.SetFocusedElement(focused);
            }
        }

        internal string CurrentDialogueText()
        {
            TMP_Text speaker = Get<TMP_Text>(_screen, SpeakerLabelField);
            TMP_Text dialogue = Get<TMP_Text>(_screen, DialogueTextField);
            string speakerText = AccessibilityText.ReadLocalizedText(speaker);
            string dialogueText = AccessibilityText.ReadLocalizedText(dialogue);
            return Message.JoinText(speakerText, dialogueText);
        }

        private bool IsPhraseAnimating()
        {
            object value = IsPhraseAnimatingField.GetValue(_screen);
            return value is bool isAnimating && isAnimating;
        }

        internal bool CanAdvanceDialogue()
        {
            if (IsPhraseAnimating() || string.IsNullOrWhiteSpace(CurrentDialogueText()))
            {
                return false;
            }

            UnityEngine.UI.Image nextIndicator = Get<UnityEngine.UI.Image>(_screen, NextIndicatorField);
            return nextIndicator != null && nextIndicator.gameObject.activeInHierarchy;
        }

        internal bool DispatchDialogueInput(InputManager.Controls control)
        {
            CoreInputControlMapping mapping = new CoreInputControlMapping(control, InputType.None, fake: true);
            CoreSignals.ControlMappingTriggered.Dispatch(mapping);
            return true;
        }

        private void FocusDialogueOrWait()
        {
        }

        internal static void ClearGameSelection()
        {
            global::InputManager.Inst?.SelectGameUIComponent(null);
        }

        private static bool IsManualNavigationAction(string actionKey)
        {
            return actionKey == "ui_up" || actionKey == "ui_down";
        }

        private static T Get<T>(object owner, FieldInfo field) where T : class
        {
            return owner != null ? field.GetValue(owner) as T : null;
        }
    }
}

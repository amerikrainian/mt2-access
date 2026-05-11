using System;
using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Localization;
using ShinyShoe;
using TMPro;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyChallengeSubmitButton : UIElement, IActivatableElement, INavigationTargetElement
    {
        private static readonly FieldInfo SubmitNewRunButtonField = AccessTools.Field(typeof(global::ChallengeSubmitButton), "newRunButton")!;
        private static readonly FieldInfo SubmitContinueRunButtonField = AccessTools.Field(typeof(global::ChallengeSubmitButton), "continueRunButton")!;
        private static readonly FieldInfo SubmitNewRunButtonLabelField = AccessTools.Field(typeof(global::ChallengeSubmitButton), "newRunButtonLabel")!;
        private static readonly FieldInfo SubmitContinueRunButtonLabelField = AccessTools.Field(typeof(global::ChallengeSubmitButton), "continueRunButtonLabel")!;
        private static readonly FieldInfo SubmitTimeLeftLabelField = AccessTools.Field(typeof(global::ChallengeSubmitButton), "timeLeftLabel")!;

        private readonly global::ChallengeSubmitButton _submit;
        private readonly Func<bool> _activate;

        public ProxyChallengeSubmitButton(global::ChallengeSubmitButton submit, Func<bool> activate)
        {
            _submit = submit;
            _activate = activate;
        }

        private GameUISelectableButton ActiveButton
        {
            get
            {
                GameUISelectableButton cont = Get<GameUISelectableButton>(_submit, SubmitContinueRunButtonField);
                if (cont != null && cont.gameObject.activeInHierarchy)
                {
                    return cont;
                }

                return Get<GameUISelectableButton>(_submit, SubmitNewRunButtonField);
            }
        }

        public override bool IsVisible => ActiveButton != null && ActiveButton.gameObject.activeInHierarchy;
        public override string GetTypeKey() => "button";

        public override Message GetLabel()
        {
            TMP_Text label = ReferenceEquals(ActiveButton, Get<GameUISelectableButton>(_submit, SubmitContinueRunButtonField))
                ? Get<TMP_Text>(_submit, SubmitContinueRunButtonLabelField)
                : Get<TMP_Text>(_submit, SubmitNewRunButtonLabelField);
            Message text = Message.FromText(AccessibilityText.ReadLocalizedText(label));
            return text ?? Message.RawCleaned(GameUIButtonSupport.ResolveLabel(ActiveButton));
        }

        public override Message GetStatusString() => GameButtonElement.StateMessage(ActiveButton);

        public override Message GetExtrasString()
        {
            TMP_Text text = Get<TMP_Text>(_submit, SubmitTimeLeftLabelField);
            return Message.FromText(AccessibilityText.ReadLocalizedText(text));
        }

        public void SelectForNavigation()
        {
            if (ActiveButton != null && InputManager.Inst != null)
            {
                InputManager.Inst.SelectGameUIComponent(ActiveButton, allowClearingSelection: false);
            }
        }

        public bool Activate() => _activate != null && _activate();

        private static T Get<T>(object owner, FieldInfo field) where T : class
        {
            return owner != null ? field.GetValue(owner) as T : null;
        }
    }
}

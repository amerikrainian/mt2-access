using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HarmonyLib;
using MonsterTrainAccessibility.Localization;
using ShinyShoe;
using TMPro;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyVictoryButton : GameObjectElement
    {
        private static readonly FieldInfo CollectButtonField = AccessTools.Field(typeof(global::VictoryUI), "collectButton")!;
        private static readonly FieldInfo CollectButtonLabelField = AccessTools.Field(typeof(global::VictoryUI), "collectButtonLabel")!;
        private static readonly FieldInfo CollectButtonKeyField = AccessTools.Field(typeof(global::VictoryUI), "collectButtonKey")!;
        private static readonly FieldInfo ContinueButtonKeyField = AccessTools.Field(typeof(global::VictoryUI), "continueButtonKey")!;

        private readonly global::VictoryUI _victory;
        private readonly GameUISelectableButton _button;

        private ProxyVictoryButton(global::VictoryUI victory, GameUISelectableButton button)
            : base(
                target: button != null ? button.gameObject : null,
                typeKey: "button",
                label: null)
        {
            _victory = victory;
            _button = button;
        }

        public static ProxyVictoryButton Create(global::VictoryUI victory)
        {
            GameUISelectableButton button = victory != null ? CollectButtonField.GetValue(victory) as GameUISelectableButton : null;
            return button != null ? new ProxyVictoryButton(victory, button) : null;
        }

        public override bool IsVisible => _button != null && _button.gameObject.activeInHierarchy;
        public override Message GetLabel() => Label(_victory);
        public override Message GetStatusString() => GameButtonElement.StateMessage(_button);

        public GameUISelectableButton Button => _button;

        public static void AppendSignature(StringBuilder sb, global::VictoryUI victory)
        {
            if (sb == null)
            {
                return;
            }

            GameUISelectableButton button = victory != null ? CollectButtonField.GetValue(victory) as GameUISelectableButton : null;
            sb.Append(";button:").Append(button != null && button.gameObject.activeInHierarchy);
            TMP_Text collectButtonLabel = victory != null ? CollectButtonLabelField.GetValue(victory) as TMP_Text : null;
            sb.Append(':').Append(AccessibilityText.ReadLocalizedText(collectButtonLabel));
        }

        private static Message Label(global::VictoryUI victory)
        {
            if (victory == null)
            {
                return null;
            }

            TMP_Text label = CollectButtonLabelField.GetValue(victory) as TMP_Text;
            string text = AccessibilityText.ReadLocalizedText(label);
            if (!string.IsNullOrWhiteSpace(text))
            {
                return Message.RawCleaned(text);
            }

            FieldInfo keyField = ProxyRewardItem.HasVisibleVictoryRewards(victory)
                ? CollectButtonKeyField
                : ContinueButtonKeyField;
            string key = keyField.GetValue(victory) as string;
            return !string.IsNullOrWhiteSpace(key) ? Message.FromText(AccessibilityText.LocalizeTerm(key)) : null;
        }
    }
}

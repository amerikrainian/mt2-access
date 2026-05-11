using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Localization;
using ShinyShoe;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyRunSetupPyreHeartSummary : GameObjectElement
    {
        private static readonly FieldInfo RandomTitleKeyField = AccessTools.Field(typeof(global::PyreHeartInfoUI), "randomTitleKey")!;
        private static readonly FieldInfo RandomDescriptionKeyField = AccessTools.Field(typeof(global::PyreHeartInfoUI), "randomDescriptionKey")!;

        private readonly GameUISelectableButton _button;
        private readonly global::PyreHeartInfoUI _info;

        public ProxyRunSetupPyreHeartSummary(GameUISelectableButton button, global::PyreHeartInfoUI info)
            : base(
                button != null ? button.gameObject : null,
                typeKey: "button",
                label: null)
        {
            _button = button;
            _info = info;
        }

        public override bool IsVisible => _button != null && _button.gameObject.activeInHierarchy;

        public override Message GetLabel()
        {
            return Message.RawCleaned(FormatGameLabel(
                ResolvePyreHeartName(),
                "ScreenRunSetup_PyreHeart",
                "ScreenChallengeProgress_PyreHeart"));
        }

        public override Message GetStatusString() => GameButtonElement.StateMessage(_button);

        public override Message GetTooltip()
        {
            CharacterData character = _info?.PyreHeartCharacterData;
            if (character == null)
            {
                return Message.FromText(AccessibilityText.LocalizeTerm(RandomDescriptionKeyField.GetValue(_info) as string));
            }

            PyreHeartData pyre = character.GetPyreHeartData();
            PyreArtifactData artifact = pyre?.GetPyreArtifact();
            List<Message> parts = new List<Message>
            {
                Message.Localized("ui", "RUN_SETUP.PYRE_HEART_ATTACK", new { attack = pyre?.GetAttack() ?? 0 }),
                Message.Localized("ui", "RUN_SETUP.PYRE_HEART_HEALTH", new { health = pyre?.GetStartingHP() ?? 0 }),
                artifact != null ? Message.Localized("ui", "RUN_SETUP.PYRE_HEART_BONUS", new { bonus = artifact.GetName() }) : null,
                Message.FromText(artifact?.GetDescription())
            };
            parts.RemoveAll(part => part == null);
            return parts.Count > 0 ? Message.JoinLines(parts) : null;
        }

        private string ResolvePyreHeartName()
        {
            CharacterData character = _info?.PyreHeartCharacterData;
            return character != null
                ? AccessibilityText.LocalizeTerm(character.GetNameKey())
                : AccessibilityText.LocalizeTerm(RandomTitleKeyField.GetValue(_info) as string);
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

            string format = AccessibilityText.LocalizeTerm("TextFormat_Colon");
            return !string.IsNullOrWhiteSpace(format)
                ? Message.Clean(string.Format(format, label, value))
                : Message.Clean(label + ": " + value);
        }

        private static string LocalizeFirstTerm(params string[] terms)
        {
            if (terms == null)
            {
                return string.Empty;
            }

            for (int i = 0; i < terms.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(terms[i]) && terms[i].HasTranslation())
                {
                    string text = AccessibilityText.LocalizeTerm(terms[i]);
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        return text;
                    }
                }
            }

            return string.Empty;
        }
    }
}

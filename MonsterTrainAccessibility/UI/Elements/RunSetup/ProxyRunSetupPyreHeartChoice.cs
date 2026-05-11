using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Localization;
using ShinyShoe;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyRunSetupPyreHeartChoice : GameObjectElement
    {
        private static readonly FieldInfo ButtonField = AccessTools.Field(typeof(global::RunSetupPyreHeartSelectionItemUI), "button")!;
        private static readonly FieldInfo RandomTitleKeyField = AccessTools.Field(typeof(global::RunSetupPyreHeartSelectionItemUI), "randomTitleKey")!;
        private static readonly FieldInfo DialogRandomDescriptionKeyField = AccessTools.Field(typeof(global::RunSetupPyreHeartSelectionUI), "randomPyreHeartDescriptionKey")!;

        private readonly global::RunSetupPyreHeartSelectionItemUI _item;
        private readonly global::RunSetupPyreHeartSelectionUI _dialog;
        private readonly SaveManager _saveManager;
        private readonly GameUISelectableButton _button;

        public ProxyRunSetupPyreHeartChoice(global::RunSetupPyreHeartSelectionItemUI item, global::RunSetupPyreHeartSelectionUI dialog, SaveManager saveManager)
            : base(
                ButtonFor(item)?.gameObject,
                typeKey: "button",
                label: null)
        {
            _item = item;
            _dialog = dialog;
            _saveManager = saveManager;
            _button = ButtonFor(item);
        }

        public GameUISelectableButton Button => _button;

        public override bool IsVisible => _item != null && _item.gameObject.activeInHierarchy;

        public override Message GetLabel()
        {
            global::RunSetupPyreHeartSelectionLayoutUI.PyreHeartOptionData data = _item?.PyreHeartOptionData;
            if (data == null)
            {
                return null;
            }

            return data.pyreCharacterData != null
                ? Message.RawCleaned(AccessibilityText.LocalizeTerm(data.pyreCharacterData.GetNameKey()))
                : Message.RawCleaned(AccessibilityText.LocalizeTerm(RandomTitleKeyField.GetValue(_item) as string));
        }

        public override Message GetStatusString()
        {
            return _item?.PyreHeartOptionData?.isLocked == true
                ? Message.Localized("messages", "state.locked")
                : GameButtonElement.StateMessage(_button);
        }

        public override Message GetTooltip()
        {
            global::RunSetupPyreHeartSelectionLayoutUI.PyreHeartOptionData data = _item?.PyreHeartOptionData;
            CharacterData character = data?.pyreCharacterData;
            PyreHeartData pyre = character?.GetPyreHeartData();
            if (data == null)
            {
                return null;
            }

            if (data.IsRandom)
            {
                return Message.FromText(AccessibilityText.LocalizeTerm(DialogRandomDescriptionKeyField.GetValue(_dialog) as string));
            }

            if (character == null || pyre == null)
            {
                return null;
            }

            PyreArtifactData artifact = pyre.GetPyreArtifact();
            List<Message> parts = new List<Message>
            {
                Message.Localized("ui", "RUN_SETUP.PYRE_HEART_ATTACK", new { attack = pyre.GetAttack() }),
                Message.Localized("ui", "RUN_SETUP.PYRE_HEART_HEALTH", new { health = pyre.GetStartingHP() }),
                artifact != null ? Message.Localized("ui", "RUN_SETUP.PYRE_HEART_BONUS", new { bonus = artifact.GetName() }) : null,
                Message.FromText(artifact?.GetDescription())
            };

            if (data.isLocked)
            {
                parts.Add(UnlockText(pyre.GetUnlockData(), _saveManager));
            }

            parts.RemoveAll(part => part == null);
            return parts.Count > 0 ? Message.JoinLines(parts) : null;
        }

        private static Message UnlockText(UnlockCriteria criteria, SaveManager saveManager)
        {
            if (criteria == null)
            {
                return null;
            }

            List<Message> parts = new List<Message>
            {
                Message.FromText(AccessibilityText.LocalizeTerm(criteria.GetDescriptionKey(), criteria))
            };

            if (saveManager != null && saveManager.TryGetUnlockCriteriaProgress(criteria, out int currentValue, out int unlockValue))
            {
                parts.Add(Message.FromText(string.Format(AccessibilityText.LocalizeTerm("TextFormat_Divide"), currentValue, unlockValue)));
            }

            parts.RemoveAll(part => part == null);
            return parts.Count > 0 ? Message.JoinLines(parts) : null;
        }

        private static GameUISelectableButton ButtonFor(global::RunSetupPyreHeartSelectionItemUI item)
        {
            return item != null ? ButtonField.GetValue(item) as GameUISelectableButton : null;
        }
    }
}

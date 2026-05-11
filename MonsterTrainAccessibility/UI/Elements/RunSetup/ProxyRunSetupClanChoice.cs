using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Localization;
using ShinyShoe;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyRunSetupClanChoice : GameObjectElement
    {
        private static readonly FieldInfo ButtonField = AccessTools.Field(typeof(global::RunSetupClanSelectionItemUI), "button")!;
        private static readonly FieldInfo RandomTitleKeyField = AccessTools.Field(typeof(global::RunSetupClanSelectionItemUI), "randomTitleKey")!;
        private static readonly FieldInfo RandomLegacyEraTitleKeyField = AccessTools.Field(typeof(global::RunSetupClanSelectionItemUI), "randomLegacyEraTitleKey")!;
        private static readonly FieldInfo RandomYoungEraTitleKeyField = AccessTools.Field(typeof(global::RunSetupClanSelectionItemUI), "randomYoungEraTitleKey")!;

        private readonly global::RunSetupClanSelectionItemUI _item;
        private readonly SaveManager _saveManager;
        private readonly GameUISelectableButton _button;

        public ProxyRunSetupClanChoice(global::RunSetupClanSelectionItemUI item, SaveManager saveManager)
            : base(
                ButtonFor(item)?.gameObject,
                typeKey: "button",
                label: null)
        {
            _item = item;
            _saveManager = saveManager;
            _button = ButtonFor(item);
        }

        public GameUISelectableButton Button => _button;

        public override bool IsVisible => _item != null && _item.gameObject.activeInHierarchy;

        public override Message GetLabel()
        {
            global::RunSetupClanSelectionLayoutUI.ClanOptionData data = _item?.ClassOptionData;
            if (data == null)
            {
                return null;
            }

            if (!data.IsRandom)
            {
                return Message.RawCleaned(data.clanData != null ? AccessibilityText.LocalizeTerm(data.clanData.GetTitleKey()) : string.Empty);
            }

            return Message.RawCleaned(LocalizeRandomTitle(data));
        }

        public override Message GetStatusString()
        {
            return _item?.ClassOptionData?.isLocked == true
                ? Message.Localized("messages", "state.locked")
                : GameButtonElement.StateMessage(_button);
        }

        public override Message GetTooltip()
        {
            global::RunSetupClanSelectionLayoutUI.ClanOptionData data = _item?.ClassOptionData;
            ClassData clan = data?.clanData;
            if (data == null || data.IsRandom || clan == null)
            {
                return null;
            }

            List<Message> parts = new List<Message>
            {
                Message.FromText(clan.GetDescription()),
                Message.Localized("ui", "RUN_SETUP.CLAN_LEVEL", new { level = _saveManager?.GetClassLevelInMetagame(clan.GetID()) ?? 0 })
            };

            if (data.isLocked)
            {
                parts.Add(UnlockText(clan.GetUnlockCriteria(), _saveManager, clan.GetRequiredDlc()));
            }

            parts.RemoveAll(part => part == null);
            return parts.Count > 0 ? Message.JoinLines(parts) : null;
        }

        private string LocalizeRandomTitle(global::RunSetupClanSelectionLayoutUI.ClanOptionData data)
        {
            FieldInfo field = RandomTitleKeyField;
            if (data.randomId == global::RunSetupClanSelectionLayoutUI.RandomIdLegacyEraOnly)
            {
                field = RandomLegacyEraTitleKeyField;
            }
            else if (data.randomId == global::RunSetupClanSelectionLayoutUI.RandomIdYoungEraOnly)
            {
                field = RandomYoungEraTitleKeyField;
            }

            return AccessibilityText.LocalizeTerm(field.GetValue(_item) as string);
        }

        private static Message UnlockText(UnlockCriteria criteria, SaveManager saveManager, DLC requiredDlc)
        {
            if (criteria == null)
            {
                return null;
            }

            List<Message> parts = new List<Message>();
            if (saveManager != null && !saveManager.IsDlcAvailableWhenStartingRun(requiredDlc))
            {
                parts.Add(Message.FromText(AccessibilityText.LocalizeTerm("Locked_Requires_DLC_" + requiredDlc)));
            }

            parts.Add(Message.FromText(AccessibilityText.LocalizeTerm(criteria.GetDescriptionKey(), criteria)));
            if (saveManager != null && saveManager.TryGetUnlockCriteriaProgress(criteria, out int currentValue, out int unlockValue))
            {
                parts.Add(Message.FromText(string.Format(AccessibilityText.LocalizeTerm("TextFormat_Divide"), currentValue, unlockValue)));
            }

            parts.RemoveAll(part => part == null);
            return parts.Count > 0 ? Message.JoinLines(parts) : null;
        }

        private static GameUISelectableButton ButtonFor(global::RunSetupClanSelectionItemUI item)
        {
            return item != null ? ButtonField.GetValue(item) as GameUISelectableButton : null;
        }
    }
}

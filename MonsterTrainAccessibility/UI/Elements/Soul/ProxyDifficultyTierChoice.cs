using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Localization;
using ShinyShoe;
using UnityEngine;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyDifficultyTierChoice : GameObjectElement
    {
        private static readonly FieldInfo ButtonField = AccessTools.Field(typeof(global::RunSetupDifficultyTierSelectionItemUI), "button")!;
        private static readonly FieldInfo SelectedIndicatorField = AccessTools.Field(typeof(global::RunSetupDifficultyTierSelectionItemUI), "selectedIndicator")!;

        private readonly global::RunSetupDifficultyTierSelectionItemUI _item;
        private readonly GameUISelectableButton _button;
        private readonly AllGameData _allGameData;
        private readonly SaveManager _saveManager;

        public ProxyDifficultyTierChoice(
            global::RunSetupDifficultyTierSelectionItemUI item,
            AllGameData allGameData,
            SaveManager saveManager)
            : base(
                ButtonFor(item)?.gameObject,
                typeKey: "button",
                label: null)
        {
            _item = item;
            _button = ButtonFor(item);
            _allGameData = allGameData;
            _saveManager = saveManager;
        }

        public GameUISelectableButton Button => _button;
        public override bool IsVisible => _item != null && _item.gameObject.activeInHierarchy;

        public override Message GetLabel()
        {
            return Message.FromText(_allGameData?.GetDifficultyTierDisplayData()?.GetDifficultyTierName(_item?.DifficutyTier ?? 1));
        }

        public override Message GetStatusString()
        {
            if (_item?.IsLocked == true)
            {
                return Message.Localized("messages", "state.locked");
            }

            if (IsSelected())
            {
                return Message.Localized("messages", "state.selected");
            }

            return GameButtonElement.StateMessage(_button);
        }

        public override Message GetTooltip()
        {
            DifficultyTierDisplayData display = _allGameData?.GetDifficultyTierDisplayData();
            int tier = _item?.DifficutyTier ?? 1;
            List<Message> parts = new List<Message>
            {
                Message.FromText(display?.GetDifficultyTierDescription(tier))
            };

            if (_item?.IsLocked == true)
            {
                parts.Add(UnlockText(display, tier));
            }

            parts.RemoveAll(part => part == null);
            return parts.Count > 0 ? Message.JoinLines(parts) : null;
        }

        private bool IsSelected()
        {
            GameObject selected = _item != null ? SelectedIndicatorField.GetValue(_item) as GameObject : null;
            return selected != null && selected.activeInHierarchy;
        }

        private Message UnlockText(DifficultyTierDisplayData display, int tier)
        {
            if (display == null)
            {
                return null;
            }

            List<UnlockCriteria> criteriaList = display.GetUnlockCriteriaList();
            UnlockCriteria criteria = tier > 0 && tier <= criteriaList.Count ? criteriaList[tier - 1] : null;
            if (criteria == null)
            {
                return Message.FromText(display.GetUnlockCriteria(tier));
            }

            List<Message> parts = new List<Message>
            {
                Message.FromText(AccessibilityText.LocalizeTerm(criteria.GetDescriptionKey(), criteria))
            };

            if (_saveManager != null && _saveManager.TryGetUnlockCriteriaProgress(criteria, out int currentValue, out int unlockValue))
            {
                parts.Add(Message.FromText(string.Format(AccessibilityText.LocalizeTerm("TextFormat_Divide"), currentValue, unlockValue)));
            }

            parts.RemoveAll(part => part == null);
            return parts.Count > 0 ? Message.Join(" ", parts) : Message.FromText(display.GetUnlockCriteria(tier));
        }

        private static GameUISelectableButton ButtonFor(global::RunSetupDifficultyTierSelectionItemUI item)
        {
            return item != null ? ButtonField.GetValue(item) as GameUISelectableButton : null;
        }
    }
}

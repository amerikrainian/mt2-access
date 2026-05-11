using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Screens;
using MonsterTrainAccessibility.Util;
using ShinyShoe;
using TMPro;
using UnityEngine;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyDragonsHoardDialogItem : GameObjectElement
    {
        private static readonly FieldInfo SelectionItemLevelField = AccessTools.Field(typeof(global::DragonsHoardRewardSelectionItem), "dragonsHoardLevel")!;
        private static readonly FieldInfo SelectionItemCurrentHighlightField = AccessTools.Field(typeof(global::DragonsHoardRewardSelectionItem), "currentLootLevelHighlight")!;

        private readonly global::DragonsHoardRewardSelectionItem _item;

        public ProxyDragonsHoardDialogItem(global::DragonsHoardRewardSelectionItem item, IGameUIComponent selectable)
            : base(
                selectable,
                typeKey: null,
                label: null)
        {
            _item = item;
        }

        public override bool IsVisible => _item != null && _item.gameObject.activeInHierarchy;
        public override Message GetLabel()
        {
            string level = AccessibilityText.ReadLocalizedText(Get<TMP_Text>(_item, SelectionItemLevelField));
            List<Message> parts = new List<Message>();
            if (!string.IsNullOrWhiteSpace(level))
            {
                parts.Add(Message.Localized("ui", "DRAGONS_HOARD.LEVEL", new { level }));
            }

            RewardNodeData node = _item?.RewardNodeData;
            if (node != null)
            {
                IReadOnlyList<RewardData> rewards = node.GetRewards();
                for (int i = 0; i < rewards.Count; i++)
                {
                    if (rewards[i] != null)
                    {
                        MessageList.Add(parts, Message.FromText(rewards[i].RewardTitle));
                    }
                }
            }

            return parts.Count > 0 ? Message.JoinLines(parts) : null;
        }

        public override Message GetStatusString()
        {
            if (_item == null)
            {
                return null;
            }

            GameObject currentHighlight = Get<GameObject>(_item, SelectionItemCurrentHighlightField);
            if (currentHighlight != null && currentHighlight.activeInHierarchy)
            {
                return new Message("state.selected");
            }

            if (_item.LockedRoot.activeInHierarchy)
            {
                return Message.Localized("ui", "STATES.LOCKED");
            }

            return null;
        }

        private static T Get<T>(object owner, FieldInfo field) where T : class
        {
            return owner != null ? field.GetValue(owner) as T : null;
        }
    }
}

using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Buffers;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI;
using MonsterTrainAccessibility.Util;
using ShinyShoe;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyTrainRoomCosmeticChoice : GameObjectElement
    {
        private static readonly FieldInfo ButtonField = AccessTools.Field(typeof(global::TrainCosmeticRoomSelectionItemUI), "button")!;

        private readonly global::TrainCosmeticRoomSelectionItemUI _item;
        private readonly SaveManager _saveManager;

        public ProxyTrainRoomCosmeticChoice(global::TrainCosmeticRoomSelectionItemUI item, SaveManager saveManager)
            : base(
                ButtonFor(item),
                typeKey: "button",
                label: null)
        {
            _item = item;
            _saveManager = saveManager;
        }

        public GameUISelectableButton Button => ButtonFor(_item);
        public override bool IsVisible => _item != null && _item.gameObject.activeInHierarchy && Button != null;

        public override Message GetLabel()
        {
            RoomData room = _item?.RoomData;
            List<Message> parts = new List<Message>();
            MessageList.Add(parts, Message.FromText(room?.GetName()));
            MessageList.Add(parts, LockState(room));
            MessageList.Add(parts, Message.FromText(room?.GetDescription()));
            return parts.Count > 0 ? Message.Join(", ", parts) : null;
        }

        public override Message GetTooltip()
        {
            RoomData room = _item?.RoomData;
            List<Message> parts = new List<Message>();
            MessageList.Add(parts, Message.FromText(room?.GetDescription()));
            MessageList.Add(parts, UnlockText(room));
            return parts.Count > 0 ? Message.JoinLines(parts) : null;
        }

        internal override string HandleBuffers(BufferManager buffers)
        {
            LineBuffer uiBuffer = buffers?.GetBuffer("ui");
            if (uiBuffer != null)
            {
                RoomData room = _item?.RoomData;
                uiBuffer.Clear();
                uiBuffer.Add(Message.FromText(room?.GetName()));
                uiBuffer.Add(Message.FromText(room?.GetDescription()));
                uiBuffer.Add(LockState(room));
                uiBuffer.Add(UnlockText(room));
                buffers.EnableBuffer("ui", true);
            }

            return "ui";
        }

        private Message LockState(RoomData room)
        {
            return IsUnlocked(room) ? null : Message.Localized("ui", "STATES.LOCKED");
        }

        private Message UnlockText(RoomData room)
        {
            if (room == null || IsUnlocked(room))
            {
                return null;
            }

            UnlockCriteria criteria = room.GetUnlockCriteria();
            if (criteria == null)
            {
                return null;
            }

            List<Message> parts = new List<Message>();
            MessageList.Add(parts, Message.FromText(AccessibilityText.LocalizeTerm(criteria.GetDescriptionKey(), criteria)));
            if (_saveManager != null && _saveManager.TryGetUnlockCriteriaProgress(criteria, out int currentValue, out int unlockValue))
            {
                MessageList.Add(parts, Message.FromText(string.Format(AccessibilityText.LocalizeTerm("TextFormat_Divide"), currentValue, unlockValue)));
            }

            return parts.Count > 0 ? Message.Join(" ", parts) : null;
        }

        private bool IsUnlocked(RoomData room)
        {
            return room != null && _saveManager != null && _saveManager.GetMetagameSave().HasUnlockedRoomCosmetic(room.GetID());
        }

        private static GameUISelectableButton ButtonFor(global::TrainCosmeticRoomSelectionItemUI item)
        {
            return item != null ? ButtonField.GetValue(item) as GameUISelectableButton : null;
        }
    }
}

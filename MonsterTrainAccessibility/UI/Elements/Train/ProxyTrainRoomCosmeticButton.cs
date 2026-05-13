using System;
using MonsterTrainAccessibility.Buffers;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI;
using MonsterTrainAccessibility.Util;
using ShinyShoe;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyTrainRoomCosmeticButton : GameObjectElement
    {
        private readonly global::TrainCosmeticsSelectionUI _selection;
        private readonly Func<RoomData> _room;

        public ProxyTrainRoomCosmeticButton(global::TrainCosmeticsSelectionUI selection, Func<RoomData> room)
            : base(
                selection?.EditCosmeticButton,
                typeKey: "button",
                label: null)
        {
            _selection = selection;
            _room = room;
        }

        public GameUISelectableButton Button => _selection?.EditCosmeticButton;
        public global::TrainCosmeticsSelectionUI Selection => _selection;
        public override bool IsVisible => Button != null && Button.gameObject.activeInHierarchy;

        public override Message GetLabel()
        {
            return Summary();
        }

        public override Message GetTooltip()
        {
            return Message.FromText(_room()?.GetDescription());
        }

        internal override string HandleBuffers(BufferManager buffers)
        {
            LineBuffer uiBuffer = buffers?.GetBuffer("ui");
            if (uiBuffer != null)
            {
                RoomData room = _room();
                uiBuffer.Clear();
                uiBuffer.Add(Text(_selection?.HeaderText));
                uiBuffer.Add(Message.FromText(room?.GetName()));
                uiBuffer.Add(Message.FromText(room?.GetDescription()));
                buffers.EnableBuffer("ui", true);
            }

            return "ui";
        }

        private Message Summary()
        {
            RoomData room = _room();
            System.Collections.Generic.List<Message> parts = new System.Collections.Generic.List<Message>();
            MessageList.Add(parts, Text(_selection?.HeaderText));
            MessageList.Add(parts, Message.FromText(room?.GetName()));
            MessageList.Add(parts, Message.FromText(room?.GetDescription()));
            return parts.Count > 0 ? Message.Join(", ", parts) : null;
        }

        private static Message Text(TMPro.TMP_Text text)
        {
            return text != null ? Message.RawCleaned(AccessibilityText.ReadLocalizedText(text)) : null;
        }
    }
}

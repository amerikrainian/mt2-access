using System.Collections.Generic;
using MonsterTrainAccessibility.Buffers;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Presentation.Relics;
using MonsterTrainAccessibility.Util;
using ShinyShoe;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxySoulInfoItem : GameObjectElement
    {
        private readonly SoulInfoUI _item;
        private readonly IGameUIComponent _selectable;

        public ProxySoulInfoItem(SoulInfoUI item, IGameUIComponent selectable, string typeKey = "button")
            : base(
                target: selectable?.component != null ? selectable.component.gameObject : item?.gameObject,
                typeKey: typeKey,
                label: null)
        {
            _item = item;
            _selectable = selectable;
        }

        public override bool IsVisible =>
            (_selectable?.component != null && _selectable.component.gameObject.activeInHierarchy) ||
            (_item != null && _item.gameObject.activeInHierarchy);
        public override Message GetLabel() => Message.FromText(SoulData()?.GetName());

        public override Message GetTooltip()
        {
            List<Message> parts = new List<Message>();
            SoulData data = SoulData();
            MessageList.Add(parts, Message.FromText(data?.GetDescription()));
            AddSoulContext(parts, data);
            return parts.Count > 0 ? Message.JoinLines(parts) : null;
        }

        internal override string HandleBuffers(BufferManager buffers)
        {
            base.HandleBuffers(buffers);
            PresentationBuffer<RelicPresentationSource> buffer = buffers?.GetBuffer("relic") as PresentationBuffer<RelicPresentationSource>;
            SoulData data = SoulData();
            if (buffer == null || data == null)
            {
                return "ui";
            }

            List<Message> context = new List<Message>();
            AddSoulContext(context, data);
            buffer.Bind(RelicPresentationSource.FromState(new SoulState(data), includeDynamicInfo: false), context);
            buffers.EnableBuffer("relic", true);
            return "relic";
        }

        private SoulData SoulData()
        {
            return _item?.GetSoulData();
        }

        private static void AddSoulContext(List<Message> parts, SoulData data)
        {
            if (data == null)
            {
                return;
            }

            MessageList.Add(parts, Message.Localized("ui", "SOUL.TIER", new { tier = data.GetTierLevel() }));
        }
    }
}

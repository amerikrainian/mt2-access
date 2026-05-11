using System.Collections.Generic;
using MonsterTrainAccessibility.Buffers;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Presentation.Relics;
using MonsterTrainAccessibility.Util;
using ShinyShoe;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxySoulSaviorSoulsButton : GameObjectElement
    {
        private readonly GameUISelectableButton _button;
        private readonly AllGameData _allGameData;
        private readonly System.Func<IReadOnlyList<string>> _soulIds;

        public ProxySoulSaviorSoulsButton(
            GameUISelectableButton button,
            AllGameData allGameData,
            System.Func<IReadOnlyList<string>> soulIds)
            : base(
                button != null ? button.gameObject : null,
                typeKey: "button",
                label: null)
        {
            _button = button;
            _allGameData = allGameData;
            _soulIds = soulIds;
        }

        public override bool IsVisible => _button != null && _button.gameObject.activeInHierarchy;

        public override Message GetLabel()
        {
            Message selected = SelectedSoulsLabel();
            return selected != null
                ? Message.Localized("ui", "RUN_SETUP.SOULS.LABEL", new { souls = selected.Resolve() })
                : Message.Localized("ui", "HUD.SOULS");
        }

        public override Message GetStatusString() => GameButtonElement.StateMessage(_button);

        public override Message GetTooltip()
        {
            List<Message> parts = new List<Message>();
            List<SoulState> states = SelectedSoulStates();
            for (int i = 0; i < states.Count; i++)
            {
                MessageList.Add(parts, Message.FromText(states[i].GetName()));
                MessageList.Add(parts, ProxyRelicInfo.FromState(states[i], includeDynamicInfo: false));
            }

            return parts.Count > 0 ? Message.JoinLines(parts) : null;
        }

        internal override string HandleBuffers(BufferManager buffers)
        {
            base.HandleBuffers(buffers);
            PresentationBuffer<RelicPresentationSource> buffer = buffers?.GetBuffer("relic") as PresentationBuffer<RelicPresentationSource>;
            if (buffer == null)
            {
                return "ui";
            }

            List<SoulState> states = SelectedSoulStates();
            if (states.Count == 0)
            {
                return "ui";
            }

            List<RelicPresentationSource> sources = new List<RelicPresentationSource>();
            for (int i = 0; i < states.Count; i++)
            {
                sources.Add(RelicPresentationSource.FromState(states[i], includeDynamicInfo: false));
            }

            buffer.BindMany(sources);
            buffers.EnableBuffer("relic", true);
            return "relic";
        }

        private Message SelectedSoulsLabel()
        {
            IReadOnlyList<string> ids = _soulIds != null ? _soulIds() : null;
            if (ids == null || ids.Count == 0)
            {
                return null;
            }

            if (ContainsRandom(ids))
            {
                return Message.Localized("messages", "setup.random");
            }

            List<Message> names = new List<Message>();
            for (int i = 0; i < ids.Count; i++)
            {
                SoulData soul = _allGameData?.FindSoulData(ids[i]);
                MessageList.Add(names, Message.FromText(soul?.GetName()));
            }

            return names.Count > 0 ? Message.Join(", ", names) : null;
        }

        private List<SoulState> SelectedSoulStates()
        {
            List<SoulState> states = new List<SoulState>();
            IReadOnlyList<string> ids = _soulIds != null ? _soulIds() : null;
            if (ids == null || ContainsRandom(ids))
            {
                return states;
            }

            for (int i = 0; i < ids.Count; i++)
            {
                SoulData soul = _allGameData?.FindSoulData(ids[i]);
                if (soul != null)
                {
                    states.Add(new SoulState(soul));
                }
            }

            return states;
        }

        private static bool ContainsRandom(IReadOnlyList<string> ids)
        {
            if (ids == null)
            {
                return false;
            }

            for (int i = 0; i < ids.Count; i++)
            {
                if (ids[i] == "random")
                {
                    return true;
                }
            }

            return false;
        }
    }
}

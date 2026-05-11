using System.Collections.Generic;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Elements;

namespace MonsterTrainAccessibility.Presentation.Relics
{
    internal sealed class RelicPresentationSource
    {
        private RelicPresentationSource(
            RelicTooltipProvider provider,
            RelicState state,
            bool includeDynamicInfo,
            IEnumerable<Message> contextLines)
        {
            Provider = provider;
            State = state;
            IncludeDynamicInfo = includeDynamicInfo;
            ContextLines = Copy(contextLines);
        }

        public RelicTooltipProvider Provider { get; }
        public RelicState State { get; }
        public bool IncludeDynamicInfo { get; }
        public IReadOnlyList<Message> ContextLines { get; }

        public static RelicPresentationSource FromProvider(RelicTooltipProvider provider, IEnumerable<Message> contextLines = null)
        {
            return provider != null ? new RelicPresentationSource(provider, null, false, contextLines) : null;
        }

        public static RelicPresentationSource FromState(RelicState state, bool includeDynamicInfo, IEnumerable<Message> contextLines = null)
        {
            return state != null ? new RelicPresentationSource(null, state, includeDynamicInfo, contextLines) : null;
        }

        public Message Label()
        {
            return State != null ? Message.FromText(State.GetName()) : ProxyRelicInfo.Label(Provider);
        }

        public Message Tooltip()
        {
            return State != null ? ProxyRelicInfo.Tooltip(State, IncludeDynamicInfo) : ProxyRelicInfo.Tooltip(Provider);
        }

        public Message Description()
        {
            return State != null ? ProxyRelicInfo.FromState(State, IncludeDynamicInfo) : ProxyRelicInfo.Description(Provider);
        }

        public IReadOnlyList<Message> ExtraTooltipParts()
        {
            return State != null
                ? ProxyRelicInfo.ExtraTooltipParts(State, IncludeDynamicInfo)
                : ProxyRelicInfo.ExtraTooltipParts(Provider);
        }

        private static IReadOnlyList<Message> Copy(IEnumerable<Message> messages)
        {
            List<Message> copy = new List<Message>();
            if (messages == null)
            {
                return copy;
            }

            foreach (Message message in messages)
            {
                if (message != null)
                {
                    copy.Add(message);
                }
            }

            return copy;
        }
    }
}

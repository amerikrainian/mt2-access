using MonsterTrainAccessibility.Buffers;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Presentation;
using MonsterTrainAccessibility.Presentation.Relics;
using MonsterTrainAccessibility.UI;
using MonsterTrainAccessibility.Util;
using ShinyShoe;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyRelicInfo : GameObjectElement
    {
        private readonly RelicInfoUI _relic;
        private readonly RelicState _state;
        private readonly bool _includeDynamicInfo;

        public ProxyRelicInfo(RelicInfoUI relic, string typeKey = "button")
            : base(
                relic?.SelectableUI,
                typeKey: typeKey,
                label: null)
        {
            _relic = relic;
        }

        private ProxyRelicInfo(RelicState state, bool includeDynamicInfo)
            : base((UnityEngine.GameObject)null, typeKey: "button", label: null)
        {
            _state = state;
            _includeDynamicInfo = includeDynamicInfo;
        }

        public static ProxyRelicInfo FromRelicData(RelicData relicData, bool includeDynamicInfo)
        {
            return relicData != null ? new ProxyRelicInfo(new RelicState(relicData), includeDynamicInfo) : null;
        }

        public RelicInfoUI Relic => _relic;
        public RelicState State => _state;
        public bool IncludeDynamicInfoForModel => _includeDynamicInfo;
        public override bool IsVisible => _state != null || (_relic != null && _relic.gameObject.activeInHierarchy);
        public override Message GetLabel() => _state != null ? FocusSummary(_state, _includeDynamicInfo) : FocusSummary(_relic);
        public override Message GetTooltip() => _state != null ? Tooltip(_state, _includeDynamicInfo) : Tooltip(_relic);

        internal override string HandleBuffers(BufferManager buffers)
        {
            return HandleBuffers(buffers, null);
        }

        internal string HandleBuffers(BufferManager buffers, List<Message> beforeLabel)
        {
            base.HandleBuffers(buffers);
            PresentationBuffer<RelicPresentationSource> buffer = buffers?.GetBuffer("relic") as PresentationBuffer<RelicPresentationSource>;
            if (buffer == null || (_relic == null && _state == null))
            {
                return "ui";
            }

            if (_state != null)
            {
                buffer.Bind(RelicPresentationSource.FromState(_state, _includeDynamicInfo), beforeLabel);
            }
            else
            {
                buffer.Bind(RelicPresentationSource.FromProvider(_relic), beforeLabel);
            }
            buffers.EnableBuffer("relic", true);
            return "relic";
        }

        private static readonly FieldInfo RelicInfoIncludeDynamicInfoField = AccessTools.Field(typeof(global::RelicInfoUI), "includeDynamicInfo")!;

        public static Message Label(global::RelicTooltipProvider relic)
        {
            return Message.FromText(LabelText(relic));
        }

        public static Message FocusSummary(global::RelicTooltipProvider relic)
        {
            return relic != null ? FocusSummary(BuildContent(relic)) : null;
        }

        public static Message FocusSummary(global::RelicState state, bool includeDynamicInfo)
        {
            return state != null ? FocusSummary(BuildContent(state, includeDynamicInfo)) : null;
        }

        public static Message Tooltip(global::RelicTooltipProvider relic)
        {
            List<Message> parts = TooltipParts(relic);
            return parts.Count > 0 ? Message.JoinLines(parts) : null;
        }

        public static List<Message> ExtraTooltipParts(global::RelicTooltipProvider relic)
        {
            List<Message> parts = new List<Message>();
            if (relic == null)
            {
                return parts;
            }

            RelicContent content = BuildContent(relic);
            AddTooltips(parts, content.Tooltips, content.Name, content.Description?.Resolve(), bodyFirst: false);
            return parts;
        }

        public static List<Message> ExtraTooltipParts(global::RelicState state, bool includeDynamicInfo)
        {
            List<Message> parts = new List<Message>();
            if (state == null)
            {
                return parts;
            }

            RelicContent content = BuildContent(state, includeDynamicInfo);
            AddTooltips(parts, content.Tooltips, content.Name, content.Description?.Resolve(), bodyFirst: false);
            return parts;
        }

        public static Message FromData(global::RelicData relicData, bool includeDynamicInfo)
        {
            if (relicData == null)
            {
                return null;
            }

            RelicState state = new RelicState(relicData);
            return Description(state, includeDynamicInfo);
        }

        public static Message FromState(global::RelicState state, bool includeDynamicInfo)
        {
            return Description(state, includeDynamicInfo);
        }

        public static Message Description(global::RelicTooltipProvider relic)
        {
            if (relic == null)
            {
                return null;
            }

            return BuildContent(relic).Description;
        }

        public static Message Description(global::RelicState state, bool includeDynamicInfo)
        {
            if (state == null)
            {
                return null;
            }

            return BuildContent(state, includeDynamicInfo).Description;
        }

        public static Message Tooltip(global::RelicState state, bool includeDynamicInfo)
        {
            List<Message> parts = TooltipParts(state, includeDynamicInfo);
            return parts.Count > 0 ? Message.JoinLines(parts) : null;
        }

        private static List<Message> TooltipParts(global::RelicTooltipProvider relic)
        {
            List<Message> parts = new List<Message>();
            if (relic == null)
            {
                return parts;
            }

            RelicContent content = BuildContent(relic);
            MessageList.Add(parts, content.Description);
            AddTooltips(parts, content.Tooltips, content.Name, content.Description?.Resolve(), bodyFirst: false);
            return parts;
        }

        private static List<Message> TooltipParts(global::RelicState state, bool includeDynamicInfo)
        {
            List<Message> parts = new List<Message>();
            if (state == null)
            {
                return parts;
            }

            RelicContent content = BuildContent(state, includeDynamicInfo);
            MessageList.Add(parts, content.Description);
            AddTooltips(parts, content.Tooltips, content.Name, content.Description?.Resolve(), bodyFirst: false);
            return parts;
        }

        private static string LabelText(global::RelicTooltipProvider relic)
        {
            RelicState state = relic?.GetRelicState();
            if (state != null)
            {
                return state.GetName();
            }

            RelicInfoUI info = relic as RelicInfoUI;
            return info?.relicData != null ? info.relicData.GetName() : string.Empty;
        }

        private static string DescriptionText(global::RelicTooltipProvider relic)
        {
            if (relic == null)
            {
                return string.Empty;
            }

            RelicState state = relic.GetRelicState();
            if (state != null)
            {
                RelicManager relicManager = AllGameManagers.Instance?.OrNull()?.GetRelicManager().OrNull();
                return AccessibilityLocalizationScope.Run(() => state.GetDescription(relicManager, IncludeDynamicInfo(relic)));
            }

            RelicInfoUI info = relic as RelicInfoUI;
            return info?.relicData != null ? info.relicData.GetDescription() : string.Empty;
        }

        private static RelicContent BuildContent(global::RelicTooltipProvider relic)
        {
            string name = LabelText(relic);
            bool includeDynamicInfo = IncludeDynamicInfo(relic);
            RelicState state = relic?.GetRelicState();
            List<TooltipContent> tooltips = CreateTooltips(state, includeDynamicInfo);
            Message description = Message.FromText(DescriptionText(relic)) ?? MainTooltipDescription(tooltips);

            if (relic?.Tooltips != null)
            {
                MergeMissingTooltips(tooltips, relic.Tooltips);
            }

            return new RelicContent(name, description, tooltips);
        }

        private static RelicContent BuildContent(global::RelicState state, bool includeDynamicInfo)
        {
            string name = state?.GetName() ?? string.Empty;
            List<TooltipContent> tooltips = CreateTooltips(state, includeDynamicInfo);
            RelicManager relicManager = AllGameManagers.Instance?.OrNull()?.GetRelicManager().OrNull();
            Message description = Message.FromText(
                state != null
                    ? AccessibilityLocalizationScope.Run(() => state.GetDescription(relicManager, includeDynamicInfo))
                    : string.Empty) ?? MainTooltipDescription(tooltips);

            return new RelicContent(name, description, tooltips);
        }

        private static void MergeMissingTooltips(List<TooltipContent> target, List<TooltipContent> source)
        {
            if (target == null || source == null)
            {
                return;
            }

            HashSet<string> seen = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < target.Count; i++)
            {
                TooltipContent tooltip = target[i];
                seen.Add(MessageList.TooltipKey(tooltip));
            }

            for (int i = 0; i < source.Count; i++)
            {
                TooltipContent tooltip = source[i];
                string key = MessageList.TooltipKey(tooltip);
                if (seen.Add(key))
                {
                    target.Add(tooltip);
                }
            }
        }

        private static List<TooltipContent> CreateTooltips(global::RelicState state, bool includeDynamicInfo)
        {
            List<TooltipContent> tooltips = new List<TooltipContent>();
            if (state == null)
            {
                return tooltips;
            }

            StatusEffectManager statusEffectManager = StatusEffectManager.Instance.OrNull();
            if (statusEffectManager == null)
            {
                return tooltips;
            }

            RelicManager relicManager = AllGameManagers.Instance?.OrNull()?.GetRelicManager().OrNull();
            TooltipGenerator.OptionalFlags flags = includeDynamicInfo
                ? TooltipGenerator.OptionalFlags.DynamicInfo
                : TooltipGenerator.OptionalFlags.MainItem;
            AccessibilityLocalizationScope.Run(() =>
                TooltipGenerator.GetRelicTooltips(tooltips, state, statusEffectManager, relicManager, flags));
            return tooltips;
        }

        private static Message MainTooltipDescription(List<TooltipContent> tooltips)
        {
            if (tooltips == null || tooltips.Count == 0)
            {
                return null;
            }

            for (int i = 0; i < tooltips.Count; i++)
            {
                Message body = Message.FromText(tooltips[i].body);
                if (body != null)
                {
                    return body;
                }
            }

            return null;
        }

        private static bool IncludeDynamicInfo(global::RelicTooltipProvider relic)
        {
            RelicInfoUI info = relic as RelicInfoUI;
            return info != null && (bool)RelicInfoIncludeDynamicInfoField.GetValue(info);
        }

        private static Message FocusSummary(RelicContent content)
        {
            if (content == null)
            {
                return null;
            }

            List<Message> parts = new List<Message>();
            MessageList.Add(parts, Message.FromText(content.Name));
            MessageList.Add(parts, content.Description);
            return parts.Count > 0 ? Message.JoinLines(parts) : null;
        }

        private static void AddTooltips(List<Message> parts, List<TooltipContent> tooltips, string relicName, string relicDescription, bool bodyFirst)
        {
            if (tooltips == null)
            {
                return;
            }

            MessageList.Deduper text = new MessageList.Deduper();
            string cleanName = Message.Clean(relicName);
            string cleanDescription = Message.Clean(relicDescription);
            for (int i = 0; i < tooltips.Count; i++)
            {
                TooltipContent tooltip = tooltips[i];
                if (tooltip.IsEmpty())
                {
                    continue;
                }

                string title = Message.Clean(tooltip.title);
                string body = Message.Clean(tooltip.body);
                if (IsMainRelicTooltip(title, body, cleanName, cleanDescription))
                {
                    continue;
                }

                text.AddTitleBody(parts, title, body, tooltip.tooltipId, bodyFirst);
            }
        }

        private static bool IsMainRelicTooltip(string title, string body, string relicName, string relicDescription)
        {
            if (!string.IsNullOrWhiteSpace(relicName) &&
                string.Equals(title, relicName, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return !string.IsNullOrWhiteSpace(relicDescription) &&
                string.Equals(body, relicDescription, System.StringComparison.OrdinalIgnoreCase);
        }

        private sealed class RelicContent
        {
            public RelicContent(string name, Message description, List<TooltipContent> tooltips)
            {
                Name = name ?? string.Empty;
                Description = description;
                Tooltips = tooltips ?? new List<TooltipContent>();
            }

            public string Name { get; }

            public Message Description { get; }

            public List<TooltipContent> Tooltips { get; }
        }

    }
}

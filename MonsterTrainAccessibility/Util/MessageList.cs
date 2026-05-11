using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI;

namespace MonsterTrainAccessibility.Util
{
    internal static class MessageList
    {
        private static readonly Regex SpriteTagPattern = new Regex(
            "<sprite\\s+[^>]*>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        public static List<Message> Dedupe(IEnumerable<Message> messages)
        {
            List<Message> deduped = new List<Message>();
            if (messages == null)
            {
                return deduped;
            }

            foreach (Message message in messages)
            {
                Add(deduped, message);
            }

            return deduped;
        }

        public static void Add(List<Message> parts, Message message)
        {
            if (parts == null || message == null)
            {
                return;
            }

            string text = message.Resolve();
            if (!Message.ShouldAdd(text))
            {
                return;
            }

            string normalized = NormalizeForDedupe(text);
            for (int i = 0; i < parts.Count; i++)
            {
                string existing = parts[i]?.Resolve();
                if (string.Equals(NormalizeForDedupe(existing), normalized, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            parts.Add(message);
        }

        public static Message Tooltip(global::TooltipContent tooltip, bool bodyFirst = false)
        {
            if (tooltip.IsEmpty())
            {
                return null;
            }

            List<Message> parts = new List<Message>();
            AddTooltip(parts, tooltip, bodyFirst: bodyFirst);
            return parts.Count > 0 ? Message.JoinLines(parts) : null;
        }

        public static Message TooltipList(IList<global::TooltipContent> tooltips, bool bodyFirst = false)
        {
            List<Message> parts = TooltipParts(tooltips, bodyFirst);
            return parts.Count > 0 ? Message.JoinLines(parts) : null;
        }

        public static List<Message> TooltipParts(IList<global::TooltipContent> tooltips, bool bodyFirst = false)
        {
            List<Message> parts = new List<Message>();
            if (tooltips == null || tooltips.Count == 0)
            {
                return parts;
            }

            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < tooltips.Count; i++)
            {
                AddTooltip(parts, tooltips[i], seen, bodyFirst);
            }

            return parts;
        }

        public static List<Message> TooltipBufferParts(IList<global::TooltipContent> tooltips)
        {
            List<Message> parts = new List<Message>();
            if (tooltips == null || tooltips.Count == 0)
            {
                return parts;
            }

            List<List<Message>> groups = new List<List<Message>>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < tooltips.Count; i++)
            {
                List<Message> group = new List<Message>();
                AddTooltip(group, tooltips[i], seen, bodyFirst: true);
                if (group.Count > 0)
                {
                    groups.Add(group);
                }
            }

            for (int i = groups.Count - 1; i >= 0; i--)
            {
                for (int j = 0; j < groups[i].Count; j++)
                {
                    Add(parts, groups[i][j]);
                }
            }

            return parts;
        }

        public static Message FirstTooltip(IList<global::TooltipContent> tooltips, bool bodyFirst = false)
        {
            if (tooltips == null)
            {
                return null;
            }

            for (int i = 0; i < tooltips.Count; i++)
            {
                global::TooltipContent tooltip = tooltips[i];
                if (!tooltip.IsEmpty())
                {
                    return Tooltip(tooltip, bodyFirst);
                }
            }

            return null;
        }

        public static void AddTooltip(
            List<Message> parts,
            global::TooltipContent tooltip,
            HashSet<string> seen = null,
            bool bodyFirst = false)
        {
            if (parts == null || tooltip.IsEmpty())
            {
                return;
            }

            Message title = TooltipTitle(tooltip);
            Message body = Message.FromText(tooltip.body);
            AddTitleBody(parts, title, body, tooltip.tooltipId, bodyFirst, seen);
        }

        public static void AddTitleBody(
            List<Message> parts,
            string title,
            string body,
            string key = null,
            bool bodyFirst = false,
            HashSet<string> seen = null)
        {
            AddTitleBody(parts, Message.FromText(title), Message.FromText(body), key, bodyFirst, seen);
        }

        public static void AddTitleBody(
            List<Message> parts,
            Message title,
            Message body,
            string key = null,
            bool bodyFirst = false,
            HashSet<string> seen = null)
        {
            if (parts == null)
            {
                return;
            }

            string resolvedTitle = title?.Resolve();
            string resolvedBody = body?.Resolve();
            if (!Message.ShouldAdd(resolvedTitle) && !Message.ShouldAdd(resolvedBody))
            {
                return;
            }

            string dedupeKey = !string.IsNullOrWhiteSpace(key)
                ? key
                : (resolvedTitle ?? string.Empty) + "\n" + (resolvedBody ?? string.Empty);
            if (seen != null && !seen.Add(dedupeKey))
            {
                return;
            }

            if (bodyFirst)
            {
                Add(parts, body);
                Add(parts, title);
                return;
            }

            if (!Message.ShouldAdd(resolvedTitle))
            {
                Add(parts, body);
                return;
            }

            if (!Message.ShouldAdd(resolvedBody))
            {
                Add(parts, title);
                return;
            }

            Add(parts, Message.Join(": ", title, body));
        }

        public static bool TryAddTooltip(
            List<global::TooltipContent> tooltips,
            HashSet<string> seen,
            global::TooltipContent tooltip,
            bool allowDuplicates = false)
        {
            if (tooltip.IsEmpty())
            {
                return false;
            }

            if (allowDuplicates || seen.Add(TooltipKey(tooltip)))
            {
                tooltips.Add(tooltip);
                return true;
            }

            return false;
        }

        public static string TooltipKey(global::TooltipContent tooltip)
        {
            string identity = TooltipIdentity(tooltip);
            if (!string.IsNullOrWhiteSpace(identity))
            {
                return identity;
            }

            if (!string.IsNullOrWhiteSpace(tooltip.tooltipId))
            {
                return tooltip.tooltipId;
            }

            return Message.Clean(tooltip.title) + "\n" + Message.Clean(tooltip.body);
        }

        public static string TooltipIdentity(global::TooltipContent tooltip)
        {
            if (tooltip.IsEmpty())
            {
                return string.Empty;
            }

            string title = TooltipTitle(tooltip)?.Resolve();
            string body = Message.FromText(tooltip.body)?.Resolve();
            string normalizedTitle = NormalizeForDedupe(title);
            string normalizedBody = NormalizeForDedupe(body);
            if (Message.ShouldAdd(normalizedTitle) || Message.ShouldAdd(normalizedBody))
            {
                return normalizedTitle + "\n" + normalizedBody;
            }

            return NormalizeTooltipId(tooltip.tooltipId);
        }

        public static Message TooltipTitle(global::TooltipContent tooltip)
        {
            Message semanticTitle = TooltipSemanticTitle(tooltip);
            bool containsSpriteTag = ContainsSpriteTag(tooltip.title);
            string strippedRaw = containsSpriteTag
                ? Message.Clean(SpriteTagPattern.Replace(tooltip.title ?? string.Empty, string.Empty))
                : string.Empty;

            if (semanticTitle == null)
            {
                return containsSpriteTag ? Message.FromText(strippedRaw) : Message.FromText(tooltip.title);
            }

            if (containsSpriteTag &&
                (!Message.ShouldAdd(strippedRaw) || string.Equals(strippedRaw, semanticTitle.Resolve(), StringComparison.OrdinalIgnoreCase)))
            {
                return semanticTitle;
            }

            Message rawTitle = containsSpriteTag ? Message.FromText(strippedRaw) : Message.FromText(tooltip.title);
            if (!Message.ShouldAdd(rawTitle?.Resolve()) || LooksLikeInternalIdentifier(tooltip.title))
            {
                return semanticTitle;
            }

            string resolvedRaw = rawTitle.Resolve();
            string resolvedSemantic = semanticTitle.Resolve();
            if (string.Equals(resolvedRaw, resolvedSemantic, StringComparison.OrdinalIgnoreCase))
            {
                return semanticTitle;
            }

            if (containsSpriteTag && resolvedRaw.IndexOf(resolvedSemantic, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return rawTitle;
            }

            return semanticTitle;
        }

        public static string NormalizeForDedupe(string text)
        {
            if (!Message.ShouldAdd(text))
            {
                return string.Empty;
            }

            return Message.Clean(text).Trim();
        }

        private static Message TooltipSemanticTitle(global::TooltipContent tooltip)
        {
            string tooltipId = NormalizeTooltipId(tooltip.tooltipId);
            if (string.IsNullOrWhiteSpace(tooltipId))
            {
                string title = tooltip.title ?? string.Empty;
                if (!LooksLikeInternalIdentifier(title))
                {
                    return null;
                }

                tooltipId = NormalizeTooltipId(title);
            }

            if (tooltipId.StartsWith("unit_ability", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            Message status = GameLocStrings.StatusName(tooltipId, showStacks: false);
            if (status != null)
            {
                return status;
            }

            Message characterTrigger = GameLocStrings.CharacterTriggerName(tooltipId);
            return characterTrigger ?? GameLocStrings.CardTriggerName(tooltipId);
        }

        private static bool ContainsSpriteTag(string text)
        {
            return !string.IsNullOrWhiteSpace(text) &&
                text.IndexOf("<sprite", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool LooksLikeInternalIdentifier(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            string cleaned = Message.Clean(text).Trim();
            return cleaned.StartsWith("Trigger_", StringComparison.OrdinalIgnoreCase) ||
                cleaned.StartsWith("StatusEffect_", StringComparison.OrdinalIgnoreCase) ||
                (cleaned.IndexOf(' ') < 0 && cleaned.IndexOf('_') >= 0);
        }

        private static string NormalizeTooltipId(string tooltipId)
        {
            if (string.IsNullOrWhiteSpace(tooltipId))
            {
                return string.Empty;
            }

            string normalized = tooltipId.Trim();
            int separatorIndex = normalized.IndexOf('-');
            if (separatorIndex > 0)
            {
                normalized = normalized.Substring(0, separatorIndex);
            }

            const string triggerPrefix = "Trigger_";
            if (normalized.StartsWith(triggerPrefix, StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring(triggerPrefix.Length);
            }

            return normalized;
        }

        internal sealed class Deduper
        {
            private readonly HashSet<string> _seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            public void AddTooltip(List<Message> parts, global::TooltipContent tooltip, bool bodyFirst = false)
            {
                MessageList.AddTooltip(parts, tooltip, _seen, bodyFirst);
            }

            public void AddTitleBody(List<Message> parts, string title, string body, string key = null, bool bodyFirst = false)
            {
                MessageList.AddTitleBody(parts, title, body, key, bodyFirst, _seen);
            }
        }
    }
}

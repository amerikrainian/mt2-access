using System;
using System.Collections.Generic;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Util;
using UnityEngine;

namespace MonsterTrainAccessibility.UI
{
    internal static class TooltipText
    {
        public static string FirstTitle(TooltipProviderComponent provider)
        {
            return provider == null || provider.Tooltips == null || provider.Tooltips.Count == 0
                ? string.Empty
                : Message.Clean(provider.Tooltips[0].title);
        }

        public static string FirstBody(TooltipProviderComponent provider)
        {
            return provider == null || provider.Tooltips == null || provider.Tooltips.Count == 0
                ? string.Empty
                : Message.Clean(provider.Tooltips[0].body);
        }

        public static Message ForComponent(Component component)
        {
            if (component == null)
            {
                return null;
            }

            List<Message> parts = new List<Message>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            ITooltipProvider[] providers = component.GetComponents<ITooltipProvider>();
            if (providers == null)
            {
                return null;
            }

            for (int i = 0; i < providers.Length; i++)
            {
                ITooltipProvider provider = providers[i];
                if (provider == null || provider.Tooltips == null)
                {
                    continue;
                }

                for (int j = 0; j < provider.Tooltips.Count; j++)
                {
                    MessageList.AddTooltip(parts, provider.Tooltips[j], seen);
                }
            }

            return parts.Count > 0 ? Localization.Message.JoinLines(parts) : null;
        }
    }
}

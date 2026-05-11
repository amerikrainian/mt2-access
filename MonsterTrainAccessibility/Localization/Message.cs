using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using HarmonyLib;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.UI;
using UnityEngine;

namespace MonsterTrainAccessibility.Localization
{
    public sealed class Message
    {
        public static readonly Message Empty = new Message();

        private enum Flavor { Empty, Positional, Named, Raw, Composite }

        private readonly Flavor _flavor;
        private readonly string _table;
        private readonly string _key;
        private readonly object[] _positional;
        private readonly object _namedArgs;
        private readonly string _rawText;
        private readonly List<Message> _parts;
        private readonly string _separator;
        private static readonly Regex SpriteTagPattern = new Regex(
            "<sprite\\s+[^>]*?name\\s*=\\s*(?:\"(?<name>[^\"]+)\"|'(?<name>[^']+)'|(?<name>[^\\s>]+))[^>]*>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        private static readonly Regex RichTextTagPattern = new Regex("<[^>]+>", RegexOptions.Compiled);
        private static readonly FieldInfo DisplayDataField = AccessTools.Field(typeof(global::StatusEffectManager), "displayData")!;
        private static readonly FieldInfo CardEffectDisplayDataField = AccessTools.Field(typeof(global::StatusEffectsDisplayData), "cardEffectDisplayData")!;
        private static readonly Dictionary<string, Func<string>> DynamicSpriteLabels =
            new Dictionary<string, Func<string>>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, Func<string>> StaticSpriteLabels =
            new Dictionary<string, Func<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["Attack"] = () => LocalizationManager.Get("ui", "ICON.ATTACK"),
                ["Armor"] = () => LocalizationManager.Get("ui", "ICON.ARMOR"),
                ["Ember"] = () => LocalizationManager.Get("ui", "ICON.EMBER"),
                ["Gold"] = () => LocalizationManager.Get("ui", "ICON.GOLD"),
                ["Health"] = () => LocalizationManager.Get("ui", "ICON.HEALTH"),
                ["PyreHealth"] = () => LocalizationManager.Get("ui", "PYRE"),
                ["Capacity"] = () => LocalizationManager.Get("ui", "ICON.CAPACITY"),
                ["DragonsHoard"] = () => LocalizationManager.Get("ui", "ICON.DRAGONS_HOARD"),
                ["Forge"] = () => LocalizationManager.Get("ui", "ICON.FORGE"),
                ["Xcost"] = () => LocalizationManager.Get("ui", "ICON.X_COST"),
                ["DeploymentEmber"] = () => LocalizationManager.Get("ui", "ICON.DEPLOYMENT_EMBER"),
                ["Ability"] = () => LocalizationManager.Get("ui", "ICON.ABILITY"),
                ["ChargedEchoes"] = () => LocalizationManager.Get("ui", "ICON.CHARGED_ECHO"),
                ["CorruptionSlot"] = () => LocalizationManager.Get("ui", "ICON.CHARGED_ECHO_SLOT"),
                ["Enchanted"] = ResolveEnchantLabel
            };
        private static readonly HashSet<string> UnknownSpriteNames =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static bool _spriteLabelsInitialized;

        private Message()
        {
            _flavor = Flavor.Empty;
        }

        public Message(string key, params object[] args)
        {
            _flavor = Flavor.Positional;
            _key = key ?? string.Empty;
            _positional = args ?? Array.Empty<object>();
        }

        private Message(string table, string key, object namedArgs)
        {
            _flavor = Flavor.Named;
            _table = table;
            _key = key ?? string.Empty;
            _namedArgs = namedArgs;
        }

        private Message(string rawText, bool _)
        {
            _flavor = Flavor.Raw;
            _rawText = rawText ?? string.Empty;
        }

        private Message(List<Message> parts, string separator)
        {
            _flavor = Flavor.Composite;
            _parts = parts;
            _separator = separator ?? string.Empty;
        }

        public static Message Raw(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return Empty;
            }
            return new Message(text, true);
        }

        public static Message RawCleaned(string text)
        {
            return string.IsNullOrWhiteSpace(text) ? null : Raw(text);
        }

        public static string Clean(string text)
        {
            return NormalizeResolvedText(text);
        }

        public static Message FromText(string text)
        {
            string cleaned = Clean(text);
            return ShouldAdd(cleaned) ? Raw(cleaned) : null;
        }

        public static bool ShouldAdd(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            string trimmed = text.Trim();
            return trimmed != "." && trimmed != "," && trimmed != ";";
        }

        public static string JoinText(params string[] parts)
        {
            if (parts == null || parts.Length == 0)
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < parts.Length; i++)
            {
                string cleaned = Clean(parts[i]);
                if (!ShouldAdd(cleaned))
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append(' ');
                }

                builder.Append(cleaned);
            }

            return builder.ToString();
        }

        public static Message Localized(string table, string key)
        {
            return new Message(table, key, null);
        }

        public static Message Localized(string table, string key, object namedArgs)
        {
            return new Message(table, key, namedArgs);
        }

        public static Message Join(string separator, params Message[] parts)
        {
            if (parts == null || parts.Length == 0)
            {
                return Empty;
            }

            List<Message> list = new List<Message>(parts.Length);
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i] != null)
                {
                    list.Add(parts[i]);
                }
            }

            if (list.Count == 0)
            {
                return Empty;
            }

            return new Message(list, separator);
        }

        public static Message Join(string separator, IReadOnlyList<Message> parts)
        {
            if (parts == null || parts.Count == 0)
            {
                return Empty;
            }

            List<Message> list = new List<Message>(parts.Count);
            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i] != null)
                {
                    list.Add(parts[i]);
                }
            }

            if (list.Count == 0)
            {
                return Empty;
            }

            return new Message(list, separator);
        }

        public static Message JoinLines(params Message[] parts)
        {
            return Join("\n", parts);
        }

        public static Message JoinLines(IReadOnlyList<Message> parts)
        {
            return Join("\n", parts);
        }

        public static Message operator +(Message a, Message b)
        {
            if (a == null || a._flavor == Flavor.Empty)
            {
                return b ?? Empty;
            }
            if (b == null || b._flavor == Flavor.Empty)
            {
                return a;
            }
            return Join(", ", a, b);
        }

        public static Message Sep(string separator)
        {
            return string.IsNullOrEmpty(separator) ? Empty : Raw(separator);
        }

        public string Resolve()
        {
            switch (_flavor)
            {
                case Flavor.Empty:
                    return string.Empty;

                case Flavor.Raw:
                    return NormalizeResolvedText(_rawText);

                case Flavor.Positional:
                    {
                        string template = LocalizationManager.Get(_key);
                        if (_positional == null || _positional.Length == 0)
                        {
                            return NormalizeResolvedText(template);
                        }
                        try
                        {
                            return NormalizeResolvedText(string.Format(CultureInfo.InvariantCulture, template, _positional));
                        }
                        catch (FormatException)
                        {
                            return NormalizeResolvedText(template);
                        }
                    }

                case Flavor.Named:
                    {
                        string template = LocalizationManager.Get(_table, _key);
                        if (_namedArgs == null)
                        {
                            return NormalizeResolvedText(template);
                        }
                        return NormalizeResolvedText(SubstituteNamed(template, _namedArgs));
                    }

                case Flavor.Composite:
                    {
                        StringBuilder sb = new StringBuilder();
                        bool first = true;
                        for (int i = 0; i < _parts.Count; i++)
                        {
                            string resolved = _parts[i].Resolve();
                            if (string.IsNullOrEmpty(resolved))
                            {
                                continue;
                            }
                            if (!first)
                            {
                                sb.Append(_separator);
                            }
                            sb.Append(resolved);
                            first = false;
                        }
                        return _separator.IndexOf('\n') >= 0
                            ? NormalizeResolvedMultilineText(sb.ToString())
                            : NormalizeResolvedText(sb.ToString());
                    }
            }

            return string.Empty;
        }

        internal static string NormalizeResolvedText(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            string collapsed = value
                .Replace("<br>", ". ")
                .Replace("<br/>", ". ")
                .Replace("<br />", ". ")
                .Replace("<br >", ". ")
                .Replace('\r', ' ')
                .Replace('\n', ' ')
                .Replace('\t', ' ');
            collapsed = RichTextTagPattern.Replace(SpriteTagPattern.Replace(collapsed, ReplaceSpriteTag), string.Empty);
            collapsed = CleanPlainText(collapsed)
                .Replace(" .", ".")
                .Replace(" ,", ",")
                .Replace(" ;", ";")
                .Replace(" :", ":");
            return collapsed;
        }

        private static string NormalizeResolvedMultilineText(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            string[] lines = value.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < lines.Length; i++)
            {
                string line = NormalizeResolvedText(lines[i]);
                if (!ShouldAdd(line))
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append('\n');
                }

                builder.Append(line);
            }

            return builder.ToString();
        }

        internal static string ResolveSpriteLabel(string spriteName)
        {
            if (string.IsNullOrWhiteSpace(spriteName))
            {
                return string.Empty;
            }

            EnsureSpriteLabelsInitialized();

            if (DynamicSpriteLabels.TryGetValue(spriteName, out Func<string> dynamicGetter))
            {
                return CleanPlainText(AccessibilityLocalizationScope.Run(dynamicGetter));
            }

            if (StaticSpriteLabels.TryGetValue(spriteName, out Func<string> staticGetter))
            {
                return CleanPlainText(staticGetter());
            }

            if (UnknownSpriteNames.Add(spriteName))
            {
                Log.Info("[AccessibilityMod] Unknown TMP sprite in game text: " + spriteName);
            }

            return string.Empty;
        }

        internal static void ResetSpriteLabels()
        {
            _spriteLabelsInitialized = false;
            DynamicSpriteLabels.Clear();
            UnknownSpriteNames.Clear();
        }

        private static string ReplaceSpriteTag(Match match)
        {
            string label = ResolveSpriteLabel(match.Groups["name"].Value);
            return string.IsNullOrWhiteSpace(label) ? string.Empty : " " + label + " ";
        }

        private static void EnsureSpriteLabelsInitialized()
        {
            if (_spriteLabelsInitialized)
            {
                return;
            }

            global::StatusEffectManager manager = global::StatusEffectManager.Instance;
            if (manager == null)
            {
                return;
            }

            DynamicSpriteLabels.Clear();
            RegisterStatusEffectLabels(manager);
            RegisterTriggerLabels(manager);
            RegisterCardEffectLabels(manager);
            _spriteLabelsInitialized = true;
        }

        private static void RegisterStatusEffectLabels(global::StatusEffectManager manager)
        {
            List<global::StatusEffectData> statuses = manager.GetAllStatusEffectsData()?.GetStatusEffectData();
            if (statuses == null)
            {
                return;
            }

            for (int i = 0; i < statuses.Count; i++)
            {
                global::StatusEffectData status = statuses[i];
                string spriteName = status?.GetIcon()?.name;
                string statusId = status?.GetStatusId();
                if (string.IsNullOrWhiteSpace(spriteName) || string.IsNullOrWhiteSpace(statusId))
                {
                    continue;
                }

                DynamicSpriteLabels[spriteName] = () => global::StatusEffectManager.GetLocalizedName(
                    statusId,
                    1,
                    inBold: false,
                    showStacks: false);
            }
        }

        private static void RegisterTriggerLabels(global::StatusEffectManager manager)
        {
            global::StatusEffectsDisplayData displayData = DisplayDataField.GetValue(manager) as global::StatusEffectsDisplayData;
            if (displayData == null)
            {
                return;
            }

            Array triggers = Enum.GetValues(typeof(global::CharacterTriggerData.Trigger));
            for (int i = 0; i < triggers.Length; i++)
            {
                global::CharacterTriggerData.Trigger trigger = (global::CharacterTriggerData.Trigger)triggers.GetValue(i);
                Sprite icon = displayData.GetDisplayData(trigger, null).iconSprite;
                if (icon == null)
                {
                    continue;
                }

                global::CharacterTriggerData.Trigger capturedTrigger = trigger;
                DynamicSpriteLabels[icon.name] = () => global::CharacterTriggerData.GetKeywordText(capturedTrigger, inBold: false);
            }
        }

        private static void RegisterCardEffectLabels(global::StatusEffectManager manager)
        {
            global::StatusEffectsDisplayData displayData = DisplayDataField.GetValue(manager) as global::StatusEffectsDisplayData;
            IDictionary cardEffectDisplayData = CardEffectDisplayDataField.GetValue(displayData) as IDictionary;
            if (cardEffectDisplayData == null)
            {
                return;
            }

            foreach (DictionaryEntry entry in cardEffectDisplayData)
            {
                string identifier = entry.Key as string;
                if (string.IsNullOrWhiteSpace(identifier) || entry.Value == null)
                {
                    continue;
                }

                FieldInfo iconField = AccessTools.Field(entry.Value.GetType(), "icon")!;
                Sprite icon = iconField.GetValue(entry.Value) as Sprite;
                if (icon == null || string.IsNullOrWhiteSpace(icon.name))
                {
                    continue;
                }

                string capturedIdentifier = identifier;
                DynamicSpriteLabels[icon.name] = () => ResolveCardEffectIdentifier(capturedIdentifier);
            }
        }

        private static string ResolveCardEffectIdentifier(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
            {
                return string.Empty;
            }

            foreach (string key in new[] { identifier + "_TooltipTitle", identifier + "_CardText", identifier })
            {
                if (key.HasTranslation())
                {
                    return AccessibilityText.LocalizeTerm(key);
                }
            }

            return string.Empty;
        }

        private static string ResolveEnchantLabel()
        {
            foreach (string key in new[] { "CardEffectEnchant_TooltipTitle", "CardEffectEnchant_CardText" })
            {
                if (key.HasTranslation())
                {
                    return AccessibilityText.LocalizeTerm(key);
                }
            }

            return string.Empty;
        }

        private static string SubstituteNamed(string template, object namedArgs)
        {
            if (string.IsNullOrEmpty(template))
            {
                return string.Empty;
            }

            PropertyInfo[] props = namedArgs.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            string result = template;
            for (int i = 0; i < props.Length; i++)
            {
                PropertyInfo prop = props[i];
                object value = prop.GetValue(namedArgs, null);
                string token = "{" + prop.Name + "}";
                result = result.Replace(token, value == null ? string.Empty : Convert.ToString(value, CultureInfo.InvariantCulture));
            }

            return result;
        }

        private static string CleanRawText(string value)
        {
            return NormalizeResolvedText(value);
        }

        private static string CleanPlainText(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            string collapsed = value.Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ').Trim();
            collapsed = UnwrapMissingLocalizationMarker(collapsed);
            while (collapsed.Contains("  "))
            {
                collapsed = collapsed.Replace("  ", " ");
            }

            return collapsed;
        }

        private static string UnwrapMissingLocalizationMarker(string value)
        {
            const string prefix = "KEY>>";
            const string suffix = "<<";
            if (value.StartsWith(prefix, StringComparison.Ordinal) &&
                value.EndsWith(suffix, StringComparison.Ordinal) &&
                value.Length > prefix.Length + suffix.Length)
            {
                return value.Substring(prefix.Length, value.Length - prefix.Length - suffix.Length).Trim();
            }

            return value;
        }
    }
}

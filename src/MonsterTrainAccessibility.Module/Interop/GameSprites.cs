using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Core.Strings;

namespace MonsterTrainAccessibility.Module.Interop
{
    /// <summary>
    /// The icons game text draws inline (<c>&lt;sprite name="Xcost"&gt;</c> in a card's text or a
    /// tooltip) as words, so nothing the game shows is lost when the speech filter strips the markup.
    /// An icon the game names reads by the game's own name, live in its current language: a status
    /// effect's icon by the status's name, a trigger's by its keyword, a card effect's by its tooltip
    /// title. The few icons the game never names (ember, attack, health, X cost...) read by the mod's
    /// <c>icon.*</c> strings, ahead of the game's tables. An icon neither knows is logged once and read by
    /// its own name, spelled out (Core's <see cref="Speech.SpriteName"/>).
    /// Core's <see cref="Speech.IconWords"/> seam, so every spoken line and buffer line passes through it.
    /// </summary>
    internal static class GameSprites
    {
        private static readonly Regex SpriteTag = new Regex(
            "<sprite\\s+[^>]*?name\\s*=\\s*(?:\"(?<name>[^\"]+)\"|'(?<name>[^']+)'|(?<name>[^\\s>]+))[^>]*>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // The game's icon names for the icons only the mod names.
        private static readonly Dictionary<string, string> Fixed = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Attack"] = "attack",
            ["Armor"] = "armor",
            ["Ember"] = "ember",
            ["Gold"] = "gold",
            ["Health"] = "health",
            ["PyreHealth"] = "pyre",
            ["Capacity"] = "capacity",
            ["DragonsHoard"] = "dragons_hoard",
            ["Forge"] = "forge",
            ["Xcost"] = "x_cost",
            ["DeploymentEmber"] = "deployment_ember",
            ["Ability"] = "ability",
            ["ChargedEchoes"] = "charged_echo",
            ["CorruptionSlot"] = "charged_echo_slot",
        };

        private static Dictionary<string, Func<string>> _named;
        private static readonly HashSet<string> Unknown = new HashSet<string>(StringComparer.Ordinal);

        private static readonly Regex LineBreakTag = new Regex(@"<br\s*/?>", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>The text with each inline icon replaced by its word, and each &lt;br&gt; a line break (the
        /// speech filter strips tags whole, so a break drawn by markup would otherwise run two
        /// sentences together: "Consume.Apply").</summary>
        public static string Words(string text)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf('<') < 0) return text;
            text = LineBreakTag.Replace(text, "\n");
            if (text.IndexOf("<sprite", StringComparison.OrdinalIgnoreCase) < 0) return text;
            return SpriteTag.Replace(text, m =>
            {
                string word = Word(m.Groups["name"].Value);
                return string.IsNullOrEmpty(word) ? " " : " " + word + " ";
            });
        }

        private static readonly Regex AnyTag = new Regex("<[^>]*>", RegexOptions.Compiled);

        /// <summary>The text without an icon whose word it already says: a keyword drawn beside its own
        /// icon ("[Shift icon] Shift", a tooltip's title), which read as a word would say the keyword
        /// twice ("Shift Shift"). An icon saying something the text does not ("[ember] 2") stays.</summary>
        public static string WithoutEchoedIcons(string text)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf("<sprite", StringComparison.OrdinalIgnoreCase) < 0) return text;
            string said = AnyTag.Replace(SpriteTag.Replace(text, " "), " ");
            return SpriteTag.Replace(text, m =>
            {
                string word = Word(m.Groups["name"].Value);
                return !string.IsNullOrWhiteSpace(word) && said.IndexOf(word.Trim(), StringComparison.OrdinalIgnoreCase) >= 0 ? "" : m.Value;
            });
        }

        private static string Word(string sprite)
        {
            if (string.IsNullOrWhiteSpace(sprite)) return null;
            // The few icons the game never names first: a lookup through its tables can come back with
            // the icon's raw id ("Xcost") for those.
            if (Fixed.TryGetValue(sprite, out string key)) return Strings.Icon(key);
            var named = Named();
            if (named != null && named.TryGetValue(sprite, out Func<string> name))
            {
                string word = name();
                if (!string.IsNullOrWhiteSpace(word)) return word;
            }
            if (Unknown.Add(sprite)) CoreLog.Info("GameSprites: no word for the icon " + sprite + ", read by its own name");
            return Speech.SpriteName(sprite);
        }

        // The icons the game names, gathered once its status effect manager exists: the names
        // themselves are read at speak time, so they follow the game's language.
        private static Dictionary<string, Func<string>> Named()
        {
            if (_named != null) return _named;
            var manager = StatusEffectManager.Instance;
            if (manager == null) return null;
            var named = new Dictionary<string, Func<string>>(StringComparer.Ordinal);
            var statuses = manager.GetAllStatusEffectsData()?.GetStatusEffectData();
            if (statuses != null)
            {
                foreach (var status in statuses)
                {
                    string sprite = status?.GetIcon()?.name;
                    string id = status?.GetStatusId();
                    if (string.IsNullOrWhiteSpace(sprite) || string.IsNullOrWhiteSpace(id)) continue;
                    named[sprite] = () => StatusEffectManager.GetLocalizedName(id, 1, inBold: false, showStacks: false);
                }
            }
            var display = manager.displayData;
            if (display != null)
            {
                foreach (CharacterTriggerData.Trigger trigger in Enum.GetValues(typeof(CharacterTriggerData.Trigger)))
                {
                    var icon = display.GetDisplayData(trigger, null).iconSprite;
                    if (icon == null || named.ContainsKey(icon.name)) continue;
                    var t = trigger;
                    named[icon.name] = () => CharacterTriggerData.GetKeywordText(t, inBold: false);
                }
                if (display.cardEffectDisplayData != null)
                {
                    foreach (var entry in display.cardEffectDisplayData)
                    {
                        string id = entry.Key;
                        var icon = entry.Value?.icon;
                        if (string.IsNullOrWhiteSpace(id) || icon == null || named.ContainsKey(icon.name)) continue;
                        named[icon.name] = () => CardEffectName(id);
                    }
                }
            }
            _named = named;
            return _named;
        }

        // A card effect's name: its tooltip title, else its card text term, else the term itself.
        // Through GameText, which leaves the term's parse cache to the game's own context.
        private static string CardEffectName(string id)
        {
            foreach (string key in new[] { id + "_TooltipTitle", id + "_CardText", id })
                if (key.HasTranslation()) return GameText.Term(key);
            return null;
        }
    }
}

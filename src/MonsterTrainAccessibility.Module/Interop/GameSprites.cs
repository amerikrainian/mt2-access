using System;
using System.Collections.Generic;
using System.Linq;
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
    /// title; the coin by the HUD's name for it ("Coin"); a key a tutorial draws by the game's name for
    /// the key ("Up Arrow"). The few icons the game never names (ember, attack, health, X cost, a pad
    /// button...) read by the mod's <c>icon.*</c> strings, ahead of the game's tables. An icon neither knows is logged once and read by
    /// its own name, spelled out (Core's <see cref="Speech.SpriteName"/>).
    /// Core's <see cref="Speech.IconWords"/> seam, so every spoken line and buffer line passes through it.
    /// </summary>
    internal static class GameSprites
    {
        private static readonly Regex SpriteTag = new Regex(
            "<sprite\\s+[^>]*?name\\s*=\\s*(?:\"(?<name>[^\"]+)\"|'(?<name>[^']+)'|(?<name>[^\\s>]+))[^>]*>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Icons the game names in its own interface though never beside the icon: the coin, which rules
        // text draws as the "Gold" icon, by the HUD's name for the counter it draws ("Coin").
        private static readonly Dictionary<string, string> Termed = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Gold"] = "HudTooltip_Gold_Title",
        };

        // The game's icon names for the icons only the mod names.
        private static readonly Dictionary<string, string> Fixed = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Attack"] = "attack",
            ["Armor"] = "armor",
            ["Ember"] = "ember",
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

        // Each named icon's names: several statuses can share one icon (Pyregel and Pyrestain both draw
        // StatusPyregel), its own status first.
        private static Dictionary<string, List<Func<string>>> _named;
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

        /// <summary>A title without an icon whose word it already says: a keyword drawn beside its own
        /// icon ("[Shift icon] Shift", a tooltip's title), which read as a word would say the keyword
        /// twice ("Shift Shift"). An icon saying something the title does not ("[ember] 2", the ability
        /// icon before an ability's name) stays; one nothing names (the Enchant effect's, a mod's) goes
        /// when the title has words of its own, as its spelled-out sprite name would only be noise
        /// ("Enchanted Enchant").</summary>
        public static string WithoutEchoedIcons(string text)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf("<sprite", StringComparison.OrdinalIgnoreCase) < 0) return text;
            string said = AnyTag.Replace(SpriteTag.Replace(text, " "), " ");
            bool words = said.Any(char.IsLetter);
            return SpriteTag.Replace(text, m =>
            {
                if (words && !Known(m.Groups["name"].Value)) return "";
                // A name can carry markup of its own (a status named by a token: "<b>Troop</b>").
                foreach (string word in Words(m.Groups["name"].Value, all: true))
                {
                    string plain = AnyTag.Replace(word, "").Trim();
                    if (plain.Length > 0 && said.IndexOf(plain, StringComparison.OrdinalIgnoreCase) >= 0) return "";
                }
                return m.Value;
            });
        }

        // Whether the game (or the mod) names the icon, rather than its sprite's name standing in.
        private static bool Known(string sprite)
        {
            if (string.IsNullOrWhiteSpace(sprite)) return false;
            if (Termed.ContainsKey(sprite) || Fixed.ContainsKey(sprite)) return true;
            var named = Named();
            if (named == null || !named.TryGetValue(sprite, out var names)) return false;
            foreach (var name in names)
                if (!string.IsNullOrWhiteSpace(name())) return true;
            return false;
        }

        private static string Word(string sprite)
        {
            foreach (string word in Words(sprite, all: false)) return word;
            return null;
        }

        // An icon's word (the first), or every word it could stand for (an icon several statuses share
        // is echoed by any of their names).
        private static IEnumerable<string> Words(string sprite, bool all)
        {
            if (string.IsNullOrWhiteSpace(sprite)) yield break;
            // The few icons the game never names first: a lookup through its tables can come back with
            // the icon's raw id ("Xcost") for those.
            if (Termed.TryGetValue(sprite, out string term))
            {
                string termed = GameText.Term(term);
                if (!string.IsNullOrWhiteSpace(termed))
                {
                    yield return termed;
                    yield break;
                }
            }
            if (Fixed.TryGetValue(sprite, out string key))
            {
                yield return Strings.Icon(key);
                yield break;
            }
            var named = Named();
            bool any = false;
            if (named != null && named.TryGetValue(sprite, out var names))
            {
                foreach (var name in names)
                {
                    string word = name();
                    if (string.IsNullOrWhiteSpace(word)) continue;
                    any = true;
                    yield return word;
                    if (!all) yield break;
                }
            }
            if (any) yield break;
            if (Unknown.Add(sprite)) CoreLog.Info("GameSprites: no word for the icon " + sprite + ", read by its own name");
            yield return Speech.SpriteName(sprite);
        }

        // The icons the game names, gathered once its status effect manager exists: the names
        // themselves are read at speak time, so they follow the game's language.
        private static Dictionary<string, List<Func<string>>> Named()
        {
            if (_named != null) return _named;
            var manager = StatusEffectManager.Instance;
            if (manager == null) return null;
            var named = new Dictionary<string, List<Func<string>>>(StringComparer.Ordinal);
            var statuses = manager.GetAllStatusEffectsData()?.GetStatusEffectData();
            if (statuses != null)
            {
                foreach (var status in statuses)
                {
                    string sprite = status?.GetIcon()?.name;
                    string id = status?.GetStatusId();
                    if (string.IsNullOrWhiteSpace(sprite) || string.IsNullOrWhiteSpace(id)) continue;
                    if (!named.TryGetValue(sprite, out var names)) named[sprite] = names = new List<Func<string>>();
                    Func<string> name = () => StatusEffectManager.GetLocalizedName(id, 1, inBold: false, showStacks: false);
                    // The status the icon is named for ("StatusPyregel": pyregel) speaks for it.
                    bool own = string.Equals(sprite.Replace(" ", ""), "Status" + id.Replace(" ", "").Replace("_", ""), StringComparison.OrdinalIgnoreCase);
                    if (own) names.Insert(0, name);
                    else names.Add(name);
                }
            }
            var display = manager.displayData;
            if (display != null)
            {
                // Triggers and card effects share icons too, with each other and with statuses (the
                // generic trigger icon is Ascension's, Summon's and more; Hatch draws the Shell status's):
                // every keyword drawing an icon is one of its names, so a title beside its own icon never
                // says it twice, while the icon's word stays the first: a status's, else the first trigger's.
                foreach (CharacterTriggerData.Trigger trigger in Enum.GetValues(typeof(CharacterTriggerData.Trigger)))
                {
                    var icon = display.GetDisplayData(trigger, null).iconSprite;
                    if (icon == null) continue;
                    if (!named.TryGetValue(icon.name, out var names)) named[icon.name] = names = new List<Func<string>>();
                    var t = trigger;
                    names.Add(() => CharacterTriggerData.GetKeywordText(t, inBold: false));
                }
                if (display.cardEffectDisplayData != null)
                {
                    foreach (var entry in display.cardEffectDisplayData)
                    {
                        string id = entry.Key;
                        var icon = entry.Value?.icon;
                        if (string.IsNullOrWhiteSpace(id) || icon == null) continue;
                        if (!named.TryGetValue(icon.name, out var names)) named[icon.name] = names = new List<Func<string>>();
                        names.Add(() => CardEffectName(id));
                    }
                }
            }
            AddControlGlyphs(named);
            _named = named;
            return _named;
        }

        // The game's control icons (a tutorial's "Press [Up]"), from its own table of them: a key by the
        // game's name for it ("Up Arrow"), a pad button by the mod's.
        private static void AddControlGlyphs(Dictionary<string, List<Func<string>>> named)
        {
            var images = global::InputManager.Inst != null ? global::InputManager.Inst._controlMapImages : null;
            if (images == null)
            {
                CoreLog.Warning("GameSprites: the game has no control icon table; key icons read by their own names");
                return;
            }
            if (images._keyboardButtons != null)
            {
                foreach (var key in images._keyboardButtons)
                {
                    var code = key.key;
                    AddGlyph(named, key.spriteNameInAtlas, () => ShinyShoe.KeyCodeExtensions.GetLocalizedDisplayName(code));
                }
            }
            if (images._gamepadImages == null) return;
            foreach (var set in images._gamepadImages)
            {
                if (set?.buttons == null) continue;
                foreach (var button in set.buttons)
                {
                    string word = PadButtonWord(set.product, button.button);
                    if (word != null) AddGlyph(named, button.spriteNameInAtlas, () => Strings.Icon(word));
                }
            }
        }

        private static void AddGlyph(Dictionary<string, List<Func<string>>> named, string sprite, Func<string> name)
        {
            if (string.IsNullOrWhiteSpace(sprite)) return;
            if (!named.TryGetValue(sprite, out var names)) named[sprite] = names = new List<Func<string>>();
            names.Add(name);
        }

        // The icon key of a pad button the game draws in text; null for one it never draws.
        private static string PadButtonWord(ShinyShoe.GamepadIconSet set, ShinyShoe.GamepadButton button)
        {
            switch (button)
            {
                case ShinyShoe.GamepadButton.DpadUp: return "dpad_up";
                case ShinyShoe.GamepadButton.DpadDown: return "dpad_down";
                case ShinyShoe.GamepadButton.DpadLeft: return "dpad_left";
                case ShinyShoe.GamepadButton.DpadRight: return "dpad_right";
                case ShinyShoe.GamepadButton.Action1: return set == ShinyShoe.GamepadIconSet.Ps5 ? "pad_cross" : "pad_a";
                default: return null;
            }
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

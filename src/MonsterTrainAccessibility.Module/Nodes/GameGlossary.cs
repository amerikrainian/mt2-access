using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using I2.Loc;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Core.Text;
using MonsterTrainAccessibility.Module.Interop;

namespace MonsterTrainAccessibility.Module.Nodes
{
    /// <summary>
    /// Every definition the game can show, read from the registries the game names keywords by, so a
    /// keyword a patch or a mod adds is in it without a line here:
    /// <list type="bullet">
    /// <item>status effects: <c>StatusEffectManager.StatusIdToLocalizationExpression</c>, each drawn by
    /// <c>TooltipUI.GetStatusEffectTooltipContents</c>, as a card's tooltip draws it;</item>
    /// <item>unit triggers (<c>CharacterTriggerData.TriggerToLocalizationExpression</c>) and card triggers
    /// (<c>CardTriggerTypeMethods.TriggerToLocalizationExpression</c>), by their keyword and tooltip texts;</item>
    /// <item>card traits: every <c>CardTraitState</c> in the game and the assemblies built on it (a
    /// mod's), titled and described as <c>TooltipUI.InitCardTrait</c> does (its own override, else the
    /// type's keys);</item>
    /// <item>explicit keyword tooltips: every I2 term pair <c>X_TooltipTitle</c> / <c>X_TooltipText</c>
    /// (or <c>X_TooltipBody</c>): cost types, card effects without a status, room effects;</item>
    /// <item>the HUD's tooltips, <c>HudTooltip_X_Title</c> / <c>HudTooltip_X_Body</c> (Forge Points,
    /// Charged Echoes, the piles), less the slots their widgets fill with live values.</item>
    /// </list>
    /// A name taken twice keeps its first source, in the order above.
    /// <para>What a definition refers to is read from its body's source text, never from this language's
    /// wording: the replacement tokens in this language's text and in the English (<c>[armor]</c>, a
    /// token naming the definition it renders as in English), and the English text's own bold,
    /// matched against the English names. So every language gets the links the English has (Valor's
    /// Armor, though the Russian writes "броню" by hand), plus any token only its own text carries
    /// (German Burst names Strike).</para>
    /// <para>The index (names and links, and the calls that draw each body) is the current language's,
    /// rebuilt when the language or a registry's size changes (a mod registers while the game loads its
    /// data); bodies are drawn on every read.</para>
    /// </summary>
    internal static class GameGlossary
    {
        private const string TitleSuffix = "_TooltipTitle";
        private const string TextSuffix = "_TooltipText";
        private const string BodySuffix = "_TooltipBody";
        private const string HudPrefix = "HudTooltip_";
        private const string HudTitleSuffix = "_Title";
        private const string HudBodySuffix = "_Body";
        private const string EnglishCode = "en-US";
        private static readonly Regex Unfilled = new Regex(@" ?\{\d+\}", RegexOptions.Compiled);
        private static readonly Regex Token = new Regex(@"\[([^\[\]]+)\]", RegexOptions.Compiled);

        // A definition as found: its id, how it is drawn now, and the keys its title and body come from
        // (null where a trait draws itself and no key is known).
        private sealed class Found
        {
            public string Id, Title, TitleKey, BodyKey;
            public Func<string> Body;
        }

        private static Glossary _current;
        private static string _builtFor;

        public static Glossary Current
        {
            get
            {
                string key = Key();
                if (_current == null || key != _builtFor)
                {
                    _current = Build();
                    _builtFor = key;
                }
                return _current;
            }
        }

        // What the index depends on: the language, and how much each registry holds. Not the assemblies:
        // a mod's are all loaded by BepInEx before the game shows anything, and assemblies loaded later
        // (the REPL's, a reload's) declare no traits.
        private static string Key()
        {
            int terms = 0;
            foreach (var source in LocalizationManager.Sources) terms += source.mTerms.Count;
            return LocalizationManager.CurrentLanguageCode + "|" + StatusEffectManager.StatusIdToLocalizationExpression.Count
                + "|" + CharacterTriggerData.TriggerToLocalizationExpression.Count + "|" + CardTriggerTypeMethods.TriggerToLocalizationExpression.Count
                + "|" + terms;
        }

        private static Glossary Build()
        {
            var found = new List<Found>();
            int statuses = AddStatuses(found);
            int unitTriggers = AddUnitTriggers(found);
            int cardTriggers = AddCardTriggers(found);
            int traits = AddTraits(found);
            int terms = AddTermPairs(found, hud: false);
            int hud = AddTermPairs(found, hud: true);

            string english = LocalizationManager.GetLanguageFromCode(EnglishCode) ?? "English";
            string language = LocalizationManager.CurrentLanguage;
            // A title can itself be a token (the Infused trait's is "[infused]"): its English name is the
            // token's English text.
            var englishNames = new Glossary(found.Select(f => new GlossaryEntry(
                Glossary.Normalize(Expanded(f.TitleKey != null ? Raw(f.TitleKey, english) : null, english)), f.Id, null, null)), inflect: true);
            var tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var entries = new List<GlossaryEntry>();
            foreach (var f in found)
            {
                var references = f.BodyKey != null ? References(f, english, language, englishNames, tokens) : null;
                var title = f.Title;
                var body = f.Body;
                entries.Add(new GlossaryEntry(Glossary.Normalize(title), f.Id, body, () => TooltipReader.Format(title, body()), references));
            }
            bool inflect = LocalizationManager.CurrentLanguageCode.StartsWith("en", StringComparison.OrdinalIgnoreCase);
            var glossary = new Glossary(entries, inflect);
            int linked = glossary.Entries.Count(e => e.References != null && e.References.Count > 0);
            int keyless = glossary.Entries.Count(e => e.References == null);
            CoreLog.Info("Glossary (" + LocalizationManager.CurrentLanguageCode + "): " + glossary.Entries.Count + " definitions from "
                + statuses + " statuses, " + unitTriggers + " unit triggers, " + cardTriggers + " card triggers, " + traits
                + " traits, " + terms + " tooltip terms, " + hud + " HUD tooltips; " + glossary.Duplicates.Count + " names taken twice; "
                + linked + " refer to others, " + keyless + " have no source text to read links from");
            return glossary;
        }

        // A definition's links, in the order its English text makes them, then any only this language's
        // text makes: tokens in either, and the English text's hand-bolded names.
        private static List<string> References(Found f, string english, string language, Glossary englishNames, Dictionary<string, string> tokens)
        {
            var ids = new List<string>();
            void Add(string id)
            {
                if (id != null && id != f.Id && !ids.Contains(id)) ids.Add(id);
            }
            string source = Raw(f.BodyKey, english);
            foreach (Match m in Token.Matches(source ?? "")) Add(TokenId(m.Groups[1].Value, englishNames, english, tokens));
            foreach (var mention in Glossary.Mentions(source)) Add(englishNames.Resolve(mention)?.Id);
            if (language != english)
                foreach (Match m in Token.Matches(Raw(f.BodyKey, language) ?? "")) Add(TokenId(m.Groups[1].Value, englishNames, english, tokens));
            return ids;
        }

        // The definition a replacement token names: the one named as the token renders in English, which is
        // what the reader is shown. Never by the token's spelling: status ids are not the names ([buff] is
        // the Buff keyword, while the status "buff" is Rage). Parameters ([codeint0], [effect0.power]) are
        // no replacement: none.
        private static string TokenId(string token, Glossary englishNames, string english, Dictionary<string, string> tokens)
        {
            if (tokens.TryGetValue(token, out var known)) return known;
            var replacement = Replacement(token);
            string id = replacement != null ? englishNames.Find(Glossary.Normalize(Raw(replacement.ReplacementTextKey, english)))?.Id : null;
            tokens[token] = id;
            return id;
        }

        // A source text with its replacement tokens rendered in the given language (one level: a token's text
        // names its keyword); parameters are left as they are.
        private static string Expanded(string text, string language)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return Token.Replace(text, m =>
            {
                var replacement = Replacement(m.Groups[1].Value);
                return replacement != null ? Raw(replacement.ReplacementTextKey, language) ?? m.Value : m.Value;
            });
        }

        private static ReplacementStringData Replacement(string token)
        {
            foreach (var handler in LocalizationManager.ParamManagers.OfType<LocalizationGlobalParameterHandler>())
            {
                var replacement = handler.LookupReplacement(token);
                if (replacement != null) return replacement;
            }
            return null;
        }

        // A term's source text in a language, tokens unexpanded; null when it has none there. Every lookup
        // here goes through GameText, which leaves the game's parse cache as it found it: a context-free
        // lookup parsed first would drop a term's [effect0.power] for the game's own text all session.
        private static string Raw(string key, string language) => GameText.Source(key, language);

        // A definition the game would draw, or none: a blank or missing text is not one.
        private static void Add(List<Found> found, string id, string title, string titleKey, string bodyKey, Func<string> body)
        {
            if (Glossary.Normalize(title).Length == 0 || Missing(title)) return;
            string first;
            try { first = body(); }
            catch (Exception e)
            {
                CoreLog.Warning("Glossary: " + id + " has no body: " + e.Message);
                return;
            }
            if (Missing(first)) return;
            found.Add(new Found { Id = id, Title = title, TitleKey = titleKey, BodyKey = bodyKey, Body = body });
        }

        // I2 answers a term it lacks with its key in markers.
        private static bool Missing(string text) => string.IsNullOrWhiteSpace(text) || text.StartsWith("KEY>>", StringComparison.Ordinal);

        private static bool Has(string key) => !string.IsNullOrEmpty(key) && key.HasTranslation();

        private static int AddStatuses(List<Found> found)
        {
            int n = found.Count;
            var manager = StatusEffectManager.Instance;
            foreach (var pair in StatusEffectManager.StatusIdToLocalizationExpression)
            {
                string id = pair.Key;
                string title = StatusEffectManager.GetLocalizedName(id);
                string titleKey = pair.Value + "_CardText";
                string bodyKey = StatusEffectManager.GetLocalizedCardTooltipTextKey(id);
                // The game's own drawing needs the status's data (its per-stack amount); a status named in
                // the registry without data is read from its keys, as a card would name it.
                if (manager != null && manager.GetStatusEffectDataById(id, expectToFind: false) != null)
                {
                    Add(found, "status:" + id, title, titleKey, bodyKey, () =>
                    {
                        TooltipUI.GetStatusEffectTooltipContents(id, StatusEffectManager.Instance, out _, out var body);
                        return body;
                    });
                }
                else
                {
                    CoreLog.Info("Glossary: status " + id + " has no data; its definition is read from its keys");
                    Add(found, "status:" + id, title, titleKey, bodyKey, () => GameText.TryTerm(bodyKey));
                }
            }
            return found.Count - n;
        }

        private static int AddUnitTriggers(List<Found> found)
        {
            int n = found.Count;
            foreach (var pair in CharacterTriggerData.TriggerToLocalizationExpression)
            {
                if (string.IsNullOrEmpty(pair.Value)) continue;
                var trigger = pair.Key;
                Add(found, "unit trigger:" + trigger, CharacterTriggerData.GetKeywordText(trigger), pair.Value + "_CardText", pair.Value + TextSuffix,
                    () => CharacterTriggerData.GetTooltipText(trigger));
            }
            return found.Count - n;
        }

        private static int AddCardTriggers(List<Found> found)
        {
            int n = found.Count;
            foreach (var pair in CardTriggerTypeMethods.TriggerToLocalizationExpression)
            {
                if (string.IsNullOrEmpty(pair.Value)) continue;
                var trigger = pair.Key;
                CardTriggerTypeMethods.GetLocalizedName(trigger, out var title);
                Add(found, "card trigger:" + trigger, title, pair.Value + "_CardText", pair.Value + TextSuffix, () =>
                {
                    CardTriggerTypeMethods.GetLocalizedTooltipText(trigger, out var body);
                    return body;
                });
            }
            return found.Count - n;
        }

        private static int AddTraits(List<Found> found)
        {
            int n = found.Count;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in Types(assembly))
                {
                    if (!IsTrait(type)) continue;
                    string name = type.Name;
                    // InitCardTrait asks the trait first, then falls back to its type's keys. A trait
                    // that cannot answer without a card (its override reads its data) is read by keys,
                    // its title by the _TooltipTitle such overrides nearly always name. The overrides read
                    // the type's own keys too, so those are the source text either way.
                    var bare = Bare(type);
                    string titleKey = Has(name + TitleSuffix) ? name + TitleSuffix : CardTraitData.GetTraitCardTextLocalizationKey(name);
                    string bodyKey = CardTraitData.GetTraitTooltipTextLocalizationKey(name);
                    string title = Ask(bare, t => t.GetCardTooltipTitle());
                    if (string.IsNullOrEmpty(title)) title = GameText.TryTerm(titleKey);
                    bool own = !string.IsNullOrEmpty(Ask(bare, t => t.GetCardTooltipText()));
                    Add(found, "trait:" + name, CardTextHelper.RemoveBold(title ?? ""), Has(titleKey) ? titleKey : null, Has(bodyKey) ? bodyKey : null, own
                        ? (Func<string>)(() => bare.GetCardTooltipText())
                        : () => GameText.TryTerm(bodyKey));
                }
            }
            return found.Count - n;
        }

        // Only the game and what builds on it can declare a trait: an assembly that does not reference the
        // game is never opened (some hold types this Mono cannot load).
        private static IEnumerable<Type> Types(Assembly assembly)
        {
            var game = typeof(CardTraitState).Assembly;
            if (assembly.IsDynamic) return new Type[0];
            if (assembly != game)
            {
                string name = game.GetName().Name;
                bool builds = false;
                try { builds = assembly.GetReferencedAssemblies().Any(r => r.Name == name); }
                catch (Exception e) { CoreLog.Warning("Glossary: " + assembly.GetName().Name + "'s references could not be read: " + e.Message); }
                if (!builds) return new Type[0];
            }
            try { return assembly.GetTypes(); }
            catch (ReflectionTypeLoadException e)
            {
                CoreLog.Warning("Glossary: " + assembly.GetName().Name + " loaded " + e.Types.Length + " types partly: " + e.Message);
                return e.Types;
            }
            catch (Exception e)
            {
                CoreLog.Warning("Glossary: " + assembly.GetName().Name + "'s types could not be read: " + e.Message);
                return new Type[0];
            }
        }

        private static bool IsTrait(Type type)
        {
            if (type == null) return false;
            try { return !type.IsAbstract && typeof(CardTraitState).IsAssignableFrom(type); }
            catch (Exception e)
            {
                CoreLog.Warning("Glossary: " + type.FullName + " could not be checked for a trait: " + e.Message);
                return false;
            }
        }

        private static CardTraitState Bare(Type type)
        {
            if (type.GetConstructor(Type.EmptyTypes) == null) return null;
            try { return (CardTraitState)Activator.CreateInstance(type); }
            catch (Exception e)
            {
                CoreLog.Warning("Glossary: trait " + type.Name + " could not be made bare: " + e.Message);
                return null;
            }
        }

        // A bare trait's answer, or null when it needs a card to give one (some read their data).
        private static string Ask(CardTraitState trait, Func<CardTraitState, string> question)
        {
            if (trait == null) return null;
            try { return question(trait); }
            catch (Exception e)
            {
                CoreLog.Info("Glossary: trait " + trait.GetType().Name + " answers only on a card (" + e.GetType().Name + "); read by its keys");
                return null;
            }
        }

        // Keyword tooltips (X_TooltipTitle with X_TooltipText, or X_TooltipBody), then the HUD's
        // (HudTooltip_X_Title with HudTooltip_X_Body). A HUD body leaves slots for the values its widget
        // fills ("+{0} attack for each Forge Point"); a definition has none to give, so they go.
        private static int AddTermPairs(List<Found> found, bool hud)
        {
            int n = found.Count;
            string titleSuffix = hud ? HudTitleSuffix : TitleSuffix;
            string[] bodySuffixes = hud ? new[] { HudBodySuffix } : new[] { TextSuffix, BodySuffix };
            foreach (var source in LocalizationManager.Sources)
            {
                foreach (var term in source.mTerms)
                {
                    string titleKey = term?.Term;
                    if (titleKey == null || !titleKey.EndsWith(titleSuffix, StringComparison.Ordinal)) continue;
                    if (hud != titleKey.StartsWith(HudPrefix, StringComparison.Ordinal)) continue;
                    string stem = titleKey.Substring(0, titleKey.Length - titleSuffix.Length);
                    string bodyKey = null;
                    foreach (var suffix in bodySuffixes)
                        if (source.GetTermData(stem + suffix) != null) { bodyKey = stem + suffix; break; }
                    if (bodyKey == null) continue;
                    Func<string> body = hud ? (Func<string>)(() => Unfilled.Replace(GameText.TryTerm(bodyKey) ?? "", "")) : () => GameText.TryTerm(bodyKey);
                    Add(found, (hud ? "hud:" : "term:") + titleKey, GameText.TryTerm(titleKey), titleKey, bodyKey, body);
                }
            }
            return found.Count - n;
        }
    }
}

using System.Collections.Generic;
using MonsterTrainAccessibility.Core;

namespace MonsterTrainAccessibility.Module.Interop
{
    /// <summary>
    /// The game's own words, from I2 Localization in the game's current language: a term the game
    /// shows somewhere ("Locked", "Covenant Outpost") is spoken in the game's wording rather than an
    /// authored one. A term the game lacks reads as null and is logged, never spoken as its key.
    /// <para>A lookup here leaves the game's parse cache as it found it. I2 parses a term's
    /// parameters once and caches the result on the term (<c>TermData.ParsedCache</c>), and the parse
    /// depends on the context of the call that makes it: without the game's card-effect context an
    /// <c>[effect0.power]</c> is not recognised and is dropped. Were our context-free lookup the first,
    /// the game's own text for that term would lose its numbers for the rest of the session
    /// ("draw + next turn"). So whatever our lookup parsed is undone, and the game's next call parses
    /// the term with its own context.</para>
    /// </summary>
    internal static class GameText
    {
        // What the game's localization returns for a term it does not have.
        private const string MissingPrefix = "KEY>>";

        public static string Term(string key)
        {
            var data = string.IsNullOrEmpty(key) ? null : I2.Loc.LocalizationManager.GetTermData(key);
            var before = data != null ? data.ParsedCache : null;
            int forms = before != null ? before.pluralParsed.Count : 0;
            var unparsed = Unparsed(before);
            string text = I2.Loc.LocalizationManager.GetTranslation(key);
            if (data != null) Restore(data, before, forms, unparsed);
            if (string.IsNullOrWhiteSpace(text) || text.StartsWith(MissingPrefix, System.StringComparison.Ordinal))
            {
                CoreLog.Warning("GameText: no game term " + key);
                return null;
            }
            return text;
        }

        // The plural forms of a cache that are not parsed yet: absent, or created without their tokens.
        private static HashSet<int> Unparsed(TranslationParsed cache)
        {
            var unparsed = new HashSet<int>();
            if (cache == null) return unparsed;
            for (int i = 0; i < cache.pluralParsed.Count; i++)
                if (cache.pluralParsed[i] == null || cache.pluralParsed[i].tokens.Count == 0) unparsed.Add(i);
            return unparsed;
        }

        // Undo the lookup's parse: a cache it created goes; in one that was there, a form it added or
        // parsed loses its tokens, which I2 re-parses (with the caller's context) on the next use.
        private static void Restore(I2.Loc.TermData data, TranslationParsed before, int forms, HashSet<int> unparsed)
        {
            if (data.ParsedCache != before)
            {
                data.ParsedCache = before;
                return;
            }
            if (before == null) return;
            for (int i = 0; i < before.pluralParsed.Count; i++)
            {
                var form = before.pluralParsed[i];
                if (form != null && (i >= forms || unparsed.Contains(i))) form.tokens.Clear();
            }
        }
    }
}

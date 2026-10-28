using MonsterTrainAccessibility.Core;

namespace MonsterTrainAccessibility.Module.Interop
{
    /// <summary>
    /// The game's own words, from I2 Localization in the game's current language: a term the game
    /// shows somewhere ("Locked", "Covenant Outpost") is spoken in the game's wording rather than an
    /// authored one. A term the game lacks reads as null and is logged, never spoken as its key.
    /// </summary>
    internal static class GameText
    {
        // What the game's localization returns for a term it does not have.
        private const string MissingPrefix = "KEY>>";

        public static string Term(string key)
        {
            string text = I2.Loc.LocalizationManager.GetTranslation(key);
            if (string.IsNullOrWhiteSpace(text) || text.StartsWith(MissingPrefix, System.StringComparison.Ordinal))
            {
                CoreLog.Warning("GameText: no game term " + key);
                return null;
            }
            return text;
        }
    }
}

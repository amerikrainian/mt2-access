using MonsterTrainAccessibility.Contracts;

namespace MonsterTrainAccessibility.Module.UI
{
    /// <summary>
    /// How the mod reads what it reads, toggled in the mod menu's Settings and kept in the host's settings
    /// store (the BepInEx config), read on every use. Declared here rather than in Contracts, so a new one
    /// needs no game restart.
    /// </summary>
    internal static class Preferences
    {
        /// <summary>A card's text marks what an upgrade added, which the game draws in its upgrade colour
        /// ("Multistrike 1. new: Ability: Savior."). On unless turned off.</summary>
        public static bool MarkUpgrades
        {
            get => Get("card_mark_upgrades", true);
            set => Set("card_mark_upgrades", value);
        }

        /// <summary>A place for a new unit reads by the units on either side of it ("Between Shield
        /// Steward, Fel"), rather than by its number and the unit standing there. On unless turned off.</summary>
        public static bool PlacesByNeighbours
        {
            get => Get("battle_places_by_neighbours", true);
            set => Set("battle_places_by_neighbours", value);
        }

        private static ISettingsStore Store => ModuleMain.Host?.Settings?.Store;

        private static bool Get(string key, bool fallback)
        {
            var store = Store;
            return store != null ? store.GetBool(key, fallback) : fallback;
        }

        private static void Set(string key, bool value) => Store?.SetBool(key, value);
    }
}

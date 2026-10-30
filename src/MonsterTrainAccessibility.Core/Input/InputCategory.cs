namespace MonsterTrainAccessibility.Core.Input
{
    /// <summary>
    /// The input layer an action belongs to. Screens declare which categories they use
    /// (<see cref="Screens.Screen.InputCategories"/>, in priority order); the live set each frame is the
    /// union of every active screen's declaration walked focus-first, plus <see cref="Global"/>, which is
    /// always on. Within the live set, an identical chord bound in two categories resolves to the
    /// higher-priority (deeper) one (shadowing), so the same arrows can navigate a menu when focused and
    /// drive something else when not. Conflict prevention therefore only applies WITHIN a category.
    /// The key list lists the categories in this order.
    /// </summary>
    public enum InputCategory
    {
        /// <summary>Always live, even when focus mode is off (focus toggle, mod menu, key help).</summary>
        Global,
        /// <summary>Screen/menu navigation: live when the focused screen declares it.</summary>
        UI,
        /// <summary>The battle's read keys, declared by the battle screen.</summary>
        Battle,
        /// <summary>The run map's own keys (the route preview on Ctrl+arrows), declared by the map
        /// alone: anything opened over it (the HUD) gets the chords back for their usual keys.</summary>
        Map,
        /// <summary>The run's keys, live on every screen of a run (Run info, coin, the pyre).</summary>
        Run,
    }
}

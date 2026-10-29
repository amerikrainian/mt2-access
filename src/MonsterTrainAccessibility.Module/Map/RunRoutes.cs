namespace MonsterTrainAccessibility.Module.Map
{
    /// <summary>The route of the run in progress, by its kind: the one place a kind of run with a route
    /// of its own shape is told apart. Soul Savior's map is a normal run's, its rings in regions of two
    /// (<see cref="RegionRunSetupHelper.GetRegionStartDistance"/>) after the first ring: the region
    /// after the one in progress is not chosen yet, so its route ends with the battle that closes the
    /// region, as the game's own map scrolls no further.</summary>
    internal static class RunRoutes
    {
        public static IRunRoute For(global::MapScreen map)
        {
            var save = map != null ? map.saveManager : null;
            if (save == null || !save.HasRun()) return null;
            return save.IsRegionRun ? new TrainRoute(map, RegionEnd(save.GetCurrentDistance())) : new TrainRoute(map);
        }

        // The last ring of the region the train is in: the first ring is a region of its own.
        private static int RegionEnd(int ring)
            => ring <= 0 ? 0 : RegionRunSetupHelper.GetRegionEndDistance((ring - 1) / 2);
    }
}

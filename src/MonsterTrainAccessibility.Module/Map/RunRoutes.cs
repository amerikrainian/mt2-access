using MonsterTrainAccessibility.Core;

namespace MonsterTrainAccessibility.Module.Map
{
    /// <summary>The route of the run in progress, by its kind: the one place a kind of run with a route
    /// of its own shape is told apart (Soul Savior's regions answer null until they have one).</summary>
    internal static class RunRoutes
    {
        private static bool _loggedRegion;

        public static IRunRoute For(global::MapScreen map)
        {
            var save = map != null ? map.saveManager : null;
            if (save == null || !save.HasRun()) return null;
            if (save.IsRegionRun)
            {
                if (!_loggedRegion) CoreLog.Warning("RunRoutes: Soul Savior's map has no route reader yet");
                _loggedRegion = true;
                return null;
            }
            return new TrainRoute(map);
        }
    }
}

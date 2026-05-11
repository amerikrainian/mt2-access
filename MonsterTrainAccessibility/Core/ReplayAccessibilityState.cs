namespace MonsterTrainAccessibility.Core
{
    internal static class ReplayAccessibilityState
    {
        public static bool IsSuppressed
        {
            get
            {
                SaveManager saveManager = GameManagers.GetSaveManager();
                if (saveManager?.IsInUndoMode == true)
                {
                    return true;
                }

                ReplayManager replayManager = GameManagers.GetAll()?.GetReplayManager();
                return replayManager != null &&
                    (replayManager.IsUndoing() || replayManager.IsContinuingRun());
            }
        }
    }
}

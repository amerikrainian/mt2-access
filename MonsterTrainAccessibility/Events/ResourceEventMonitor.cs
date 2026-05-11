using MonsterTrainAccessibility.Core;

namespace MonsterTrainAccessibility.Events
{
    internal sealed class ResourceEventMonitor
    {
        private SaveManager _saveManager;

        public void Update()
        {
            SaveManager current = GameManagers.GetSaveManager();
            if (ReferenceEquals(current, _saveManager))
            {
                return;
            }

            Unsubscribe();
            _saveManager = current;
            if (_saveManager != null)
            {
                _saveManager.goldChangedSignal.AddListener(OnGoldChanged);
                _saveManager.forgePointsChangedSignal.AddListener(OnForgePointsChanged);
            }
        }

        public void Shutdown()
        {
            Unsubscribe();
        }

        private void Unsubscribe()
        {
            if (_saveManager != null)
            {
                _saveManager.goldChangedSignal.RemoveListener(OnGoldChanged);
                _saveManager.forgePointsChangedSignal.RemoveListener(OnForgePointsChanged);
                _saveManager = null;
            }
        }

        private void OnGoldChanged(SaveManager.GoldChangedSignalData data)
        {
            if (!data.IsTotalReset)
            {
                EventDispatcher.Enqueue(new GoldChangedEvent(data.prevGold, data.newGold));
            }
        }

        private void OnForgePointsChanged(int newPoints, int oldPoints)
        {
            int actualNewPoints = _saveManager != null ? _saveManager.GetForgePoints() : System.Math.Max(0, newPoints);
            EventDispatcher.Enqueue(new ForgePointsChangedEvent(oldPoints, actualNewPoints));
        }
    }
}

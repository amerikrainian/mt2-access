namespace MonsterTrainAccessibility.Core
{
    internal static class GameManagers
    {
        public static global::AllGameManagers GetAll()
        {
            return global::AllGameManagers.Instance;
        }

        public static global::CombatManager GetCombatManager()
        {
            return GetAll()?.GetCombatManager();
        }

        public static global::CardManager GetCardManager()
        {
            return GetAll()?.GetCardManager();
        }

        public static global::PlayerManager GetPlayerManager()
        {
            return GetAll()?.GetPlayerManager();
        }

        public static global::SaveManager GetSaveManager()
        {
            return GetAll()?.GetSaveManager();
        }

        public static global::PopupNotificationManager GetPopupNotificationManager()
        {
            return GetAll()?.GetPopupNotificationManager();
        }

        public static global::PopupNotificationManagerScreenSpace GetPopupNotificationManagerScreenSpace()
        {
            return GetAll()?.GetPopupNotificationManagerScreenSpace();
        }

        public static global::ScreenManager GetScreenManager()
        {
            return GetAll()?.GetScreenManager();
        }
    }
}

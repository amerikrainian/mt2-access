namespace MonsterTrainAccessibility.Patches
{
    internal static class CompendiumEnemyDetailsHooks
    {
        public static void CompendiumEnemyDetailsUI_Set_Postfix(global::CompendiumEnemyDetailsUI __instance)
        {
            UI.Screens.CompendiumScreen.RefreshEnemyDetailsBuffers(__instance);
        }
    }
}

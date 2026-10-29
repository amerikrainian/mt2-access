using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Module.Nodes;

namespace MonsterTrainAccessibility.Module.Screens
{
    public sealed partial class CompendiumScreen
    {
        // ---- Light Forge Upgrades ----

        // A row of clans and a row of champions, each chosen by landing on it; then the chosen
        // champion's upgrade paths.
        private static void LightForge(GraphBuilder b, CompendiumSectionChampUpgrades forge)
        {
            // A vertical move into either row lands on its chosen item: landing chooses.
            var clans = forge.clanOptionButtons.FindAll(c => c != null && c.ClassData != null && c.gameObject.activeInHierarchy);
            b.StartRow(entry: () => System.Math.Max(0, clans.FindIndex(c => forge.selectedClass == c.ClassData)));
            foreach (var clan in clans)
            {
                var c = clan;
                b.AddItem(ControlId.Structural("logbook:forge:clan:" + clan.ClassData.GetID()),
                    ClanNodes.ClanOption(clan, () => forge.selectedClass == c.ClassData));
            }
            b.EndRow();
            var champions = forge.championOptionButtons.FindAll(c => c != null && c.gameObject.activeInHierarchy);
            b.StartRow(entry: () => System.Math.Max(0, champions.FindIndex(ClanNodes.ChampionSelected)));
            for (int i = 0; i < champions.Count; i++)
                b.AddItem(ControlId.Structural("logbook:forge:champion:" + i), ClanNodes.ChampionOption(champions[i]));
            b.EndRow();

            for (int i = 0; i < forge.upgradeUIs.Count; i++)
                UpgradeTreeNodes.Tree(b, "logbook:forge:tree:" + i, forge.upgradeUIs[i]);
        }
    }
}

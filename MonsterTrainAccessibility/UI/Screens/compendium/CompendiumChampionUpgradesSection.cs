using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Elements;
using ShinyShoe;
using TMPro;

using MonsterTrainAccessibility.UI.Elements.Compendium;

namespace MonsterTrainAccessibility.UI.Screens
{
    internal sealed class CompendiumChampionUpgradesSection : ICompendiumSection
    {
        private static readonly FieldInfo ChampClanButtonsField = AccessTools.Field(typeof(global::CompendiumSectionChampUpgrades), "clanOptionButtons")!;
        private static readonly FieldInfo ChampChampionButtonsField = AccessTools.Field(typeof(global::CompendiumSectionChampUpgrades), "championOptionButtons")!;
        private static readonly FieldInfo ChampUpgradeUIsField = AccessTools.Field(typeof(global::CompendiumSectionChampUpgrades), "upgradeUIs")!;
        private static readonly FieldInfo ChampSelectedClassField = AccessTools.Field(typeof(global::CompendiumSectionChampUpgrades), "selectedClass")!;
        private static readonly FieldInfo ChampSelectedChampionIndexField = AccessTools.Field(typeof(global::CompendiumSectionChampUpgrades), "selectedChampionIndex")!;
        private static readonly FieldInfo UpgradeTreeTitleLabelField = AccessTools.Field(typeof(global::UpgradeTreeUI), "titleLabel")!;
        private static readonly FieldInfo UpgradeTreeCardUIField = AccessTools.Field(typeof(global::UpgradeTreeUI), "cardUI")!;
        private static readonly FieldInfo UpgradeTreeUpgradeDatasField = AccessTools.Field(typeof(global::UpgradeTreeUI), "upgradeDatas")!;
        private static readonly FieldInfo UpgradeTreeDiscoveryStatusesField = AccessTools.Field(typeof(global::UpgradeTreeUI), "discoveryStatuses")!;
        private static readonly FieldInfo UpgradeTreeLevelNodesField = AccessTools.Field(typeof(global::UpgradeTreeUI), "levelNodes")!;

        public void Populate(CompendiumScreen screen, global::CompendiumSection section)
        {
            global::CompendiumSectionChampUpgrades upgrades = section as global::CompendiumSectionChampUpgrades;
            if (upgrades == null)
            {
                return;
            }

            AddClans(screen, upgrades);
            AddChampions(screen, upgrades);
            AddTrees(screen, upgrades);
        }

        public string Signature(global::CompendiumSection section)
        {
            return CountActive<global::ClanOptionButton>(section) + ":" + CountActive<global::ChampionOptionButton>(section) + ":" + CountActive<global::UpgradeLevelNode>(section);
        }

        private static void AddClans(CompendiumScreen screen, global::CompendiumSectionChampUpgrades section)
        {
            List<global::ClanOptionButton> clans = global::MonsterTrainAccessibility.Core.ReflectionUtil.Get<List<global::ClanOptionButton>>(section, ChampClanButtonsField);
            if (clans == null)
            {
                return;
            }

            for (int i = 0; i < clans.Count; i++)
            {
                global::ClanOptionButton clan = clans[i];
                if (clan == null || clan.Button == null)
                {
                    continue;
                }

                screen.AddAccessibleElement(new ProxyCompendiumClanOptionButton(
                    clan,
                    () => global::MonsterTrainAccessibility.Core.ReflectionUtil.Get<ClassData>(section, ChampSelectedClassField)),
                    clan.gameObject,
                    clan.Button.gameObject);
            }
        }

        private static void AddChampions(CompendiumScreen screen, global::CompendiumSectionChampUpgrades section)
        {
            List<global::ChampionOptionButton> champions = global::MonsterTrainAccessibility.Core.ReflectionUtil.Get<List<global::ChampionOptionButton>>(section, ChampChampionButtonsField);
            if (champions == null)
            {
                return;
            }

            for (int i = 0; i < champions.Count; i++)
            {
                global::ChampionOptionButton champion = champions[i];
                if (champion == null || champion.Button == null)
                {
                    continue;
                }

                screen.AddAccessibleElement(new ProxyCompendiumChampionOptionButton(
                    champion,
                    () => global::MonsterTrainAccessibility.Core.ReflectionUtil.Get<ClassData>(section, ChampSelectedClassField),
                    () => global::MonsterTrainAccessibility.Core.ReflectionUtil.Get<int>(section, ChampSelectedChampionIndexField)),
                    champion.gameObject,
                    champion.Button.gameObject);
            }
        }

        private static void AddTrees(CompendiumScreen screen, global::CompendiumSectionChampUpgrades section)
        {
            List<global::UpgradeTreeUI> trees = global::MonsterTrainAccessibility.Core.ReflectionUtil.Get<List<global::UpgradeTreeUI>>(section, ChampUpgradeUIsField);
            if (trees == null)
            {
                return;
            }

            for (int i = 0; i < trees.Count; i++)
            {
                AddUpgradeTree(screen, trees[i]);
            }
        }

        private static void AddUpgradeTree(CompendiumScreen screen, global::UpgradeTreeUI tree)
        {
            if (tree == null)
            {
                return;
            }

            TMP_Text title = global::MonsterTrainAccessibility.Core.ReflectionUtil.Get<TMP_Text>(tree, UpgradeTreeTitleLabelField);
            screen.AddAccessibleElement(new ProxyCompendiumUpgradeTreeTitle(tree, title), tree.gameObject);

            List<global::CardUpgradeData> upgrades = global::MonsterTrainAccessibility.Core.ReflectionUtil.Get<List<global::CardUpgradeData>>(tree, UpgradeTreeUpgradeDatasField);
            List<bool> discovered = global::MonsterTrainAccessibility.Core.ReflectionUtil.Get<List<bool>>(tree, UpgradeTreeDiscoveryStatusesField);
            List<global::UpgradeLevelNode> nodes = global::MonsterTrainAccessibility.Core.ReflectionUtil.Get<List<global::UpgradeLevelNode>>(tree, UpgradeTreeLevelNodesField);
            if (upgrades != null && nodes != null)
            {
                for (int i = 0; i < upgrades.Count && i < nodes.Count; i++)
                {
                    int level = i + 1;
                    global::UpgradeLevelNode node = nodes[i];
                    global::CardUpgradeData upgrade = upgrades[i];
                    int index = i;
                    screen.AddAccessibleElement(new ProxyCompendiumUpgradeLevelNode(
                        node,
                        upgrade,
                        level,
                        () => discovered == null || (index < discovered.Count && discovered[index])),
                        node.gameObject,
                        node.Button != null ? node.Button.gameObject : null);
                }
            }

            global::CardUI card = global::MonsterTrainAccessibility.Core.ReflectionUtil.Get<global::CardUI>(tree, UpgradeTreeCardUIField);
            if (card != null)
            {
                screen.AddAccessibleElement(new CompendiumCardElement(card));
            }
        }
        private static int CountActive<T>(global::CompendiumSection section) where T : UnityEngine.Component
        {
            int count = 0;
            T[] items = section.GetComponentsInChildren<T>(includeInactive: true);
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] != null && items[i].gameObject.activeInHierarchy)
                {
                    count++;
                }
            }

            return count;
        }
    }
}

using UnityEngine;

using MonsterTrainAccessibility.UI.Elements.Compendium;

namespace MonsterTrainAccessibility.UI.Screens
{
    internal interface ICompendiumSection
    {
        void Populate(CompendiumScreen screen, global::CompendiumSection section);
        string Signature(global::CompendiumSection section);
    }

    internal static class CompendiumSectionRegistry
    {
        private static readonly ICompendiumSection Checklist = new CompendiumChecklistSection();
        private static readonly ICompendiumSection Cards = new CompendiumCardsSection();
        private static readonly ICompendiumSection Enemies = new CompendiumEnemiesSection();
        private static readonly ICompendiumSection Blessings = new CompendiumBlessingsSection();
        private static readonly ICompendiumSection Stats = new CompendiumStatsSection();
        private static readonly ICompendiumSection ChampionUpgrades = new CompendiumChampionUpgradesSection();
        private static readonly ICompendiumSection CardFrames = new CompendiumCardFramesSection();

        public static void Populate(CompendiumScreen screen, global::CompendiumSection section)
        {
            if (screen == null || section == null)
            {
                return;
            }

            screen.AddAccessibleElement(new ProxyCompendiumSectionHeader(section),
                section.gameObject);

            Resolve(section.Section)?.Populate(screen, section);
        }

        public static string Signature(global::CompendiumSection section)
        {
            if (section == null)
            {
                return string.Empty;
            }

            return section.Section + ":" + CountActiveDirectChildren(section) + ":" + (Resolve(section.Section)?.Signature(section) ?? string.Empty);
        }

        private static ICompendiumSection Resolve(global::CompendiumScreen.Section section)
        {
            switch (section)
            {
                case global::CompendiumScreen.Section.Checklist:
                    return Checklist;
                case global::CompendiumScreen.Section.Cards:
                    return Cards;
                case global::CompendiumScreen.Section.Enemies:
                    return Enemies;
                case global::CompendiumScreen.Section.Blessings:
                    return Blessings;
                case global::CompendiumScreen.Section.Stats:
                    return Stats;
                case global::CompendiumScreen.Section.ChampUpgrades:
                    return ChampionUpgrades;
                case global::CompendiumScreen.Section.CardFrames:
                    return CardFrames;
                default:
                    return null;
            }
        }

        private static int CountActiveDirectChildren(global::CompendiumSection section)
        {
            int count = 0;
            Transform root = section.transform;
            for (int i = 0; i < root.childCount; i++)
            {
                if (root.GetChild(i).gameObject.activeInHierarchy)
                {
                    count++;
                }
            }

            return count;
        }
    }
}

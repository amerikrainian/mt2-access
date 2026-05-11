using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Elements;
using ShinyShoe;

using MonsterTrainAccessibility.UI.Elements.Compendium;

namespace MonsterTrainAccessibility.UI.Screens
{
    internal sealed class CompendiumStatsSection : ICompendiumSection
    {
        private static readonly FieldInfo StatsLeaderboardPageField = AccessTools.Field(typeof(global::CompendiumSectionStats), "statsLeaderboardPage")!;
        private static readonly FieldInfo StatsRunStatsPageField = AccessTools.Field(typeof(global::CompendiumSectionStats), "runStatsPage")!;
        private static readonly FieldInfo StatsEndlessRunStatsPageField = AccessTools.Field(typeof(global::CompendiumSectionStats), "endlessRunStatsPage")!;
        private static readonly FieldInfo StatsRunTypesPageField = AccessTools.Field(typeof(global::CompendiumSectionStats), "runTypesPage")!;
        private static readonly FieldInfo StatsSoulSaviorPageField = AccessTools.Field(typeof(global::CompendiumSectionStats), "soulSaviorStatsPage")!;
        private static readonly FieldInfo StatsLeaderboardButtonField = AccessTools.Field(typeof(global::CompendiumSectionStats), "statsLeaderboardButton")!;
        private static readonly FieldInfo StatsRunStatsButtonField = AccessTools.Field(typeof(global::CompendiumSectionStats), "runStatsButton")!;
        private static readonly FieldInfo StatsEndlessRunStatsButtonField = AccessTools.Field(typeof(global::CompendiumSectionStats), "endlessRunStatsButton")!;
        private static readonly FieldInfo StatsRunTypesButtonField = AccessTools.Field(typeof(global::CompendiumSectionStats), "runTypesButton")!;
        private static readonly FieldInfo StatsSoulSaviorButtonField = AccessTools.Field(typeof(global::CompendiumSectionStats), "soulSaviorStatsButton")!;
        private static readonly FieldInfo StatsCurrentPageField = AccessTools.Field(typeof(global::CompendiumSectionStats), "currentPage")!;
        private static readonly FieldInfo RunStatsSectionsField = AccessTools.Field(typeof(global::CompendiumRunStatsPage), "sections")!;
        private static readonly FieldInfo LeaderboardRowsField = AccessTools.Field(typeof(global::CompendiumStatsLeaderboardPage), "statRows")!;
        private static readonly FieldInfo RunStatRowsField = AccessTools.Field(typeof(global::RunStatRowSection), "rows")!;

        public void Populate(CompendiumScreen screen, global::CompendiumSection section)
        {
            global::CompendiumSectionStats stats = section as global::CompendiumSectionStats;
            if (stats == null)
            {
                return;
            }

            AddStatsPageButton(screen, stats, StatsRunStatsButtonField, StatsRunStatsPageField, "COMPENDIUM.STATS.RUN");
            AddStatsPageButton(screen, stats, StatsEndlessRunStatsButtonField, StatsEndlessRunStatsPageField, "COMPENDIUM.STATS.ENDLESS");
            AddStatsPageButton(screen, stats, StatsSoulSaviorButtonField, StatsSoulSaviorPageField, "COMPENDIUM.STATS.SOUL_SAVIOR");
            AddStatsPageButton(screen, stats, StatsRunTypesButtonField, StatsRunTypesPageField, "COMPENDIUM.STATS.RUN_TYPES");
            AddStatsPageButton(screen, stats, StatsLeaderboardButtonField, StatsLeaderboardPageField, "COMPENDIUM.STATS.LEADERBOARD");

            global::CompendiumPage page = global::MonsterTrainAccessibility.Core.ReflectionUtil.Get<global::CompendiumPage>(stats, StatsCurrentPageField);
            if (page is global::CompendiumRunStatsPage runStats)
            {
                AddRunStats(screen, runStats);
            }
            else if (page is global::CompendiumStatsLeaderboardPage leaderboard)
            {
                AddLeaderboard(screen, leaderboard);
            }
        }

        public string Signature(global::CompendiumSection section)
        {
            return CountActive<global::RunStatRow>(section) + ":" + CountActive<global::PlayerStatRow>(section);
        }

        private static void AddStatsPageButton(CompendiumScreen screen, global::CompendiumSectionStats section, FieldInfo buttonField, FieldInfo pageField, string key)
        {
            GameUISelectableButton button = global::MonsterTrainAccessibility.Core.ReflectionUtil.Get<GameUISelectableButton>(section, buttonField);
            if (button == null)
            {
                return;
            }

            screen.AddAccessibleElement(new StatefulLabeledButton(
                button,
                key,
                () => ReferenceEquals(
                    global::MonsterTrainAccessibility.Core.ReflectionUtil.Get<global::CompendiumPage>(section, pageField),
                    global::MonsterTrainAccessibility.Core.ReflectionUtil.Get<global::CompendiumPage>(section, StatsCurrentPageField))
                        ? Message.Localized("messages", "state.selected")
                        : null),
                button.gameObject);
        }

        private static void AddRunStats(CompendiumScreen screen, global::CompendiumRunStatsPage page)
        {
            List<global::RunStatRowSection> sections = global::MonsterTrainAccessibility.Core.ReflectionUtil.Get<List<global::RunStatRowSection>>(page, RunStatsSectionsField);
            if (sections == null)
            {
                return;
            }

            for (int sectionIndex = 0; sectionIndex < sections.Count; sectionIndex++)
            {
                List<global::RunStatRow> rows = global::MonsterTrainAccessibility.Core.ReflectionUtil.Get<List<global::RunStatRow>>(sections[sectionIndex], RunStatRowsField);
                if (rows == null)
                {
                    continue;
                }

                for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
                {
                    global::RunStatRow row = rows[rowIndex];
                    if (row == null)
                    {
                        continue;
                    }

                    screen.AddAccessibleElement(new ProxyCompendiumRunStatRow(row), row.gameObject);
                }
            }
        }

        private static void AddLeaderboard(CompendiumScreen screen, global::CompendiumStatsLeaderboardPage page)
        {
            List<global::PlayerStatRow> rows = global::MonsterTrainAccessibility.Core.ReflectionUtil.Get<List<global::PlayerStatRow>>(page, LeaderboardRowsField);
            if (rows == null)
            {
                return;
            }

            for (int i = 0; i < rows.Count; i++)
            {
                global::PlayerStatRow row = rows[i];
                if (row == null)
                {
                    continue;
                }

                screen.AddAccessibleElement(new ProxyCompendiumPlayerStatRow(row), row.gameObject);
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

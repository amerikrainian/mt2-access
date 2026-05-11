using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Core;

using MonsterTrainAccessibility.UI.Elements.Compendium;

namespace MonsterTrainAccessibility.UI.Screens
{
    internal sealed class CompendiumChecklistSection : ICompendiumSection
    {
        private static readonly FieldInfo CurrentChecklistPageField = AccessTools.Field(typeof(global::CompendiumSectionChecklist), "currentPage")!;
        private static readonly FieldInfo ClanChecklistVictoryItemsField = AccessTools.Field(typeof(global::ClanChecklistSection), "victoryItems")!;
        private static readonly FieldInfo CovenantRankMeterField = AccessTools.Field(typeof(global::CompendiumSectionChecklist), "covenantRankMeter")!;
        private static readonly FieldInfo WinStreakUIsField = AccessTools.Field(typeof(global::CompendiumSectionChecklist), "winstreakUIs")!;
        private static readonly FieldInfo EndlessUIField = AccessTools.Field(typeof(global::CompendiumSectionChecklist), "endlessUI")!;
        private static readonly FieldInfo SpChallengeProgressUIField = AccessTools.Field(typeof(global::CompendiumSectionChecklist), "spChallengeProgressUI")!;

        public void Populate(CompendiumScreen screen, global::CompendiumSection section)
        {
            global::CompendiumSectionChecklist checklist = section as global::CompendiumSectionChecklist;
            if (checklist == null)
            {
                return;
            }

            global::ChecklistPage currentPage = global::MonsterTrainAccessibility.Core.ReflectionUtil.Get<global::ChecklistPage>(checklist, CurrentChecklistPageField);
            if (currentPage != null)
            {
                screen.AddAccessibleElement(new ProxyCompendiumChecklistPage(currentPage), currentPage.gameObject);
            }

            AddMetaWidgets(screen, checklist);

            foreach (global::ClanChecklistSection clan in checklist.GetComponentsInChildren<global::ClanChecklistSection>(includeInactive: true))
            {
                if (clan == null)
                {
                    continue;
                }

                screen.AddAccessibleElement(new ProxyCompendiumClanChecklist(clan), clan.gameObject);

                List<global::SubclanVictoryItem> victories = global::MonsterTrainAccessibility.Core.ReflectionUtil.Get<List<global::SubclanVictoryItem>>(clan, ClanChecklistVictoryItemsField);
                if (victories == null)
                {
                    continue;
                }

                for (int i = 0; i < victories.Count; i++)
                {
                    global::SubclanVictoryItem victory = victories[i];
                    if (victory == null)
                    {
                        continue;
                    }

                    screen.AddAccessibleElement(new ProxyCompendiumSubclanVictory(victory), victory.gameObject);
                }
            }
        }

        public string Signature(global::CompendiumSection section)
        {
            return CountActive<global::ClanChecklistSection>(section) + ":" +
                CountActive<global::SubclanVictoryItem>(section) + ":" +
                CountActive<global::ChecklistWinStreakUI>(section) + ":" +
                CountActive<global::ChecklistEndlessUI>(section) + ":" +
                CountActive<global::SpChallengeProgressUI>(section);
        }

        private static void AddMetaWidgets(CompendiumScreen screen, global::CompendiumSectionChecklist checklist)
        {
            global::CovenantRankMeter covenant = global::MonsterTrainAccessibility.Core.ReflectionUtil.Get<global::CovenantRankMeter>(checklist, CovenantRankMeterField);
            if (covenant != null)
            {
                screen.AddAccessibleElement(new ProxyCompendiumCovenantRankMeter(covenant), covenant.gameObject);
            }

            List<global::ChecklistWinStreakUI> winStreaks = global::MonsterTrainAccessibility.Core.ReflectionUtil.Get<List<global::ChecklistWinStreakUI>>(checklist, WinStreakUIsField);
            if (winStreaks != null)
            {
                for (int i = 0; i < winStreaks.Count; i++)
                {
                    global::ChecklistWinStreakUI streak = winStreaks[i];
                    if (streak != null)
                    {
                        screen.AddAccessibleElement(new ProxyCompendiumWinStreak(streak), streak.gameObject);
                    }
                }
            }

            global::ChecklistEndlessUI endless = global::MonsterTrainAccessibility.Core.ReflectionUtil.Get<global::ChecklistEndlessUI>(checklist, EndlessUIField);
            if (endless != null)
            {
                screen.AddAccessibleElement(new ProxyCompendiumEndlessRecord(endless), endless.gameObject);
            }

            global::SpChallengeProgressUI progress = global::MonsterTrainAccessibility.Core.ReflectionUtil.Get<global::SpChallengeProgressUI>(checklist, SpChallengeProgressUIField);
            if (progress != null)
            {
                screen.AddAccessibleElement(new ProxyCompendiumSpChallengeProgress(progress), progress.gameObject);
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

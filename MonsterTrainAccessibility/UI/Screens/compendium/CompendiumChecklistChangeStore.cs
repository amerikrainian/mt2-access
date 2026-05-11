using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MonsterTrainAccessibility.Localization;

namespace MonsterTrainAccessibility.UI.Screens
{
    internal static class CompendiumChecklistChangeStore
    {
        private sealed class Entry
        {
            public readonly List<ChecklistChangeData> Changes = new List<ChecklistChangeData>();
            public readonly List<CardData> MasteredCards = new List<CardData>();
        }

        private static readonly ConditionalWeakTable<global::CompendiumScreen, Entry> Entries =
            new ConditionalWeakTable<global::CompendiumScreen, Entry>();

        private static readonly FieldInfo SaveManagerField = AccessTools.Field(typeof(global::CompendiumScreen), "saveManager")!;
        private static readonly List<CardData> PendingMasteredCards = new List<CardData>();

        public static void CaptureMasteredCards(IReadOnlyList<global::UnlockScreen.UnlockDisplayData> unlocks)
        {
            PendingMasteredCards.Clear();
            if (unlocks == null)
            {
                return;
            }

            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < unlocks.Count; i++)
            {
                global::UnlockScreen.UnlockDisplayData unlock = unlocks[i];
                if (unlock?.source != global::UnlockScreen.UnlockSource.CardMastery ||
                    unlock.masteredCardDatas == null)
                {
                    continue;
                }

                for (int j = 0; j < unlock.masteredCardDatas.Count; j++)
                {
                    CardData card = unlock.masteredCardDatas[j];
                    string id = card?.GetID();
                    if (string.IsNullOrWhiteSpace(id) || !seen.Add(id))
                    {
                        continue;
                    }

                    PendingMasteredCards.Add(card);
                }
            }
        }

        public static void Set(global::CompendiumScreen screen, IReadOnlyList<ChecklistChangeData> changes)
        {
            if (screen == null)
            {
                return;
            }

            Entries.Remove(screen);
            if (changes == null || changes.Count == 0)
            {
                return;
            }

            Entry entry = new Entry();
            for (int i = 0; i < changes.Count; i++)
            {
                if (changes[i] != null)
                {
                    entry.Changes.Add(changes[i]);
                }
            }

            for (int i = 0; i < PendingMasteredCards.Count; i++)
            {
                if (PendingMasteredCards[i] != null)
                {
                    entry.MasteredCards.Add(PendingMasteredCards[i]);
                }
            }
            PendingMasteredCards.Clear();

            if (entry.Changes.Count > 0)
            {
                Entries.Add(screen, entry);
            }
        }

        public static void Clear(global::CompendiumScreen screen)
        {
            if (screen != null)
            {
                Entries.Remove(screen);
            }
        }

        public static bool HasChanges(global::CompendiumScreen screen)
        {
            return screen != null && Entries.TryGetValue(screen, out Entry entry) && entry.Changes.Count > 0;
        }

        public static IReadOnlyList<Message> Messages(global::CompendiumScreen screen)
        {
            if (screen == null || !Entries.TryGetValue(screen, out Entry entry))
            {
                return Array.Empty<Message>();
            }

            SaveManager saveManager = SaveManagerField.GetValue(screen) as SaveManager;
            List<Message> messages = new List<Message>(entry.Changes.Count);
            for (int i = 0; i < entry.Changes.Count; i++)
            {
                Message message = Describe(entry.Changes[i], saveManager, entry.MasteredCards);
                if (message != null)
                {
                    messages.Add(message);
                }
            }

            return messages;
        }

        private static Message Describe(ChecklistChangeData change, SaveManager saveManager, IReadOnlyList<CardData> masteredCards)
        {
            if (change == null)
            {
                return null;
            }

            switch (change.feature)
            {
                case ChecklistFeature.CovenantRank:
                    return Message.Localized("ui", "COMPENDIUM.CHECKLIST_CHANGE.COVENANT", new
                    {
                        old = change.oldValue,
                        current = change.newValue
                    });

                case ChecklistFeature.ClassUnlock:
                    return Message.Localized("ui", "COMPENDIUM.CHECKLIST_CHANGE.CLASS_UNLOCK", new
                    {
                        clan = ClassName(saveManager, change.mainClassId)
                    });

                case ChecklistFeature.ClassLevel:
                    return Message.Localized("ui", "COMPENDIUM.CHECKLIST_CHANGE.CLASS_LEVEL", new
                    {
                        clan = ClassName(saveManager, change.mainClassId),
                        old = change.oldValue,
                        current = change.newValue
                    });

                case ChecklistFeature.ClanComboVictory:
                    return Message.Localized("ui", "COMPENDIUM.CHECKLIST_CHANGE.CLAN_COMBO", new
                    {
                        main = ClassName(saveManager, change.mainClassId),
                        allied = ClassName(saveManager, change.subClassId),
                        rank = change.newValue
                    });

                case ChecklistFeature.CardsMastered:
                    {
                        string cards = MasteredCardNamesFor(change, masteredCards);
                        if (!string.IsNullOrWhiteSpace(cards))
                        {
                            return Message.Localized("ui", "COMPENDIUM.CHECKLIST_CHANGE.CARDS_MASTERED_EXACT", new
                            {
                                clan = ClassName(saveManager, change.mainClassId),
                                cards
                            });
                        }

                        return Message.Localized("ui", "COMPENDIUM.CHECKLIST_CHANGE.CARDS_MASTERED", new
                        {
                            clan = ClassName(saveManager, change.mainClassId),
                            old = change.oldValue,
                            current = change.newValue
                        });
                    }

                case ChecklistFeature.BestWinStreak:
                    return Message.Localized("ui", "COMPENDIUM.CHECKLIST_CHANGE.WIN_STREAK", new
                    {
                        old = change.oldValue,
                        current = change.newValue
                    });

                default:
                    return Message.Localized("ui", "COMPENDIUM.CHECKLIST_CHANGE.UNKNOWN", new
                    {
                        feature = change.feature.ToString(),
                        old = change.oldValue,
                        current = change.newValue
                    });
            }
        }

        private static string ClassName(SaveManager saveManager, string classId)
        {
            if (string.IsNullOrWhiteSpace(classId))
            {
                return LocalizationManager.Get("ui", "COMPENDIUM.CHECKLIST_CHANGE.CLANLESS");
            }

            IReadOnlyList<ClassData> classes = saveManager?.GetBalanceData()?.GetClassDatas();
            if (classes != null)
            {
                for (int i = 0; i < classes.Count; i++)
                {
                    ClassData classData = classes[i];
                    if (classData != null && string.Equals(classData.GetID(), classId, StringComparison.Ordinal))
                    {
                        return Message.Clean(classData.GetTitle());
                    }
                }
            }

            return classId;
        }

        private static string MasteredCardNamesFor(ChecklistChangeData change, IReadOnlyList<CardData> masteredCards)
        {
            if (change == null || masteredCards == null || masteredCards.Count == 0)
            {
                return string.Empty;
            }

            string changeClassId = NormalizeId(change.mainClassId);
            List<string> names = new List<string>();
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < masteredCards.Count; i++)
            {
                CardData card = masteredCards[i];
                if (card == null ||
                    card.GetRequiredDLC() != change.dlc ||
                    !string.Equals(NormalizeId(card.GetLinkedClassID()), changeClassId, StringComparison.Ordinal))
                {
                    continue;
                }

                string id = card.GetID();
                string name = Message.Clean(card.GetName());
                if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name) || !seen.Add(id))
                {
                    continue;
                }

                names.Add(name);
            }

            if (names.Count == 0 && change.feature == ChecklistFeature.CardsMastered)
            {
                for (int i = 0; i < masteredCards.Count; i++)
                {
                    CardData card = masteredCards[i];
                    string id = card?.GetID();
                    string name = Message.Clean(card?.GetName());
                    if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name) || !seen.Add(id))
                    {
                        continue;
                    }

                    names.Add(name);
                }
            }

            return string.Join(", ", names.ToArray());
        }

        private static string NormalizeId(string id)
        {
            return string.IsNullOrWhiteSpace(id) ? string.Empty : id;
        }
    }
}

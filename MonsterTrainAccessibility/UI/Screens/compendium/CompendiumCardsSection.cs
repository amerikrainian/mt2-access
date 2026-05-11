using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Core;

using MonsterTrainAccessibility.UI.Elements.Compendium;

namespace MonsterTrainAccessibility.UI.Screens
{
    internal sealed class CompendiumCardsSection : ICompendiumSection
    {
        private static readonly FieldInfo CardsFilterToolbarField = AccessTools.Field(typeof(global::CompendiumSectionCards), "filterToolbar")!;
        private static readonly FieldInfo SearchFilterInputField = AccessTools.Field(typeof(global::SearchFilterUI), "inputField")!;

        public void Populate(CompendiumScreen screen, global::CompendiumSection section)
        {
            global::CompendiumSectionCards cards = section as global::CompendiumSectionCards;
            if (cards == null)
            {
                return;
            }

            CompendiumFilters.AddToolbar(screen, global::MonsterTrainAccessibility.Core.ReflectionUtil.Get<global::FilterToolbar>(cards, CardsFilterToolbarField));

            foreach (global::CardUI card in cards.GetComponentsInChildren<global::CardUI>(includeInactive: true))
            {
                if (card == null)
                {
                    continue;
                }

                screen.AddAccessibleElement(new CompendiumCardElement(card));
            }

            foreach (global::LockedCardUI locked in cards.GetComponentsInChildren<global::LockedCardUI>(includeInactive: true))
            {
                if (locked == null || locked.SelectableUI == null)
                {
                    continue;
                }

                screen.AddAccessibleElement(new ProxyCompendiumLockedCard(locked), locked.gameObject);
            }
        }

        public string Signature(global::CompendiumSection section)
        {
            return CountActive<global::CardUI>(section) + ":" + CountActive<global::LockedCardUI>(section) + ":" + SearchSignature(section);
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

        private static string SearchSignature(global::CompendiumSection section)
        {
            global::SearchFilterUI search = section != null ? section.GetComponentInChildren<global::SearchFilterUI>(includeInactive: true) : null;
            global::InputFieldContainer input = global::MonsterTrainAccessibility.Core.ReflectionUtil.Get<global::InputFieldContainer>(search, SearchFilterInputField);
            return input?.text ?? string.Empty;
        }
    }
}

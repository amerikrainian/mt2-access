using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Presentation.Compendium;
using MonsterTrainAccessibility.UI.Elements;
using ShinyShoe;

using MonsterTrainAccessibility.UI.Elements.Compendium;

namespace MonsterTrainAccessibility.UI.Screens
{
    internal sealed class CompendiumEnemiesSection : ICompendiumSection
    {
        private static readonly FieldInfo EnemyBaseGameButtonField = AccessTools.Field(typeof(global::CompendiumSectionEnemies), "baseGameButton")!;
        private static readonly FieldInfo EnemyRailforgedButtonField = AccessTools.Field(typeof(global::CompendiumSectionEnemies), "railforgedPageButton")!;
        private static readonly FieldInfo EnemyCharacterDataUIField = AccessTools.Field(typeof(global::CompendiumSectionEnemies), "characterDataUI")!;
        private static readonly FieldInfo EnemyVariantButtonsField = AccessTools.Field(typeof(global::CompendiumSectionEnemies), "variantButtons")!;
        private static readonly FieldInfo SearchFilterInputField = AccessTools.Field(typeof(global::SearchFilterUI), "inputField")!;

        public void Populate(CompendiumScreen screen, global::CompendiumSection section)
        {
            global::CompendiumSectionEnemies enemies = section as global::CompendiumSectionEnemies;
            if (enemies == null)
            {
                return;
            }

            AddButton(screen, global::MonsterTrainAccessibility.Core.ReflectionUtil.Get<GameUISelectableButton>(enemies, EnemyBaseGameButtonField), "COMPENDIUM.ENEMIES.BASE_GAME");
            AddButton(screen, global::MonsterTrainAccessibility.Core.ReflectionUtil.Get<GameUISelectableButton>(enemies, EnemyRailforgedButtonField), "COMPENDIUM.ENEMIES.RAILFORGED");

            foreach (global::SearchFilterUI search in enemies.GetComponentsInChildren<global::SearchFilterUI>(includeInactive: true))
            {
                CompendiumFilters.AddSearchFilter(screen, search, Message.Localized("ui", "COMPENDIUM.SEARCH"));
            }

            foreach (global::DropdownFilterUI dropdown in enemies.GetComponentsInChildren<global::DropdownFilterUI>(includeInactive: true))
            {
                CompendiumFilters.AddDropdownFilter(screen, dropdown);
            }

            global::CompendiumEnemyDetailsUI details = global::MonsterTrainAccessibility.Core.ReflectionUtil.Get<global::CompendiumEnemyDetailsUI>(enemies, EnemyCharacterDataUIField);
            foreach (global::CharacterButtonUI button in enemies.GetComponentsInChildren<global::CharacterButtonUI>(includeInactive: true))
            {
                if (button == null || button.Button == null)
                {
                    continue;
                }

                screen.AddAccessibleElement(new ProxyCompendiumEnemyButton(
                        button,
                        () => CompendiumEnemyPresentationSource.FromDetails(details)),
                    button.gameObject,
                    button.Button.gameObject);
            }

            List<GameUISelectableButton> variants = global::MonsterTrainAccessibility.Core.ReflectionUtil.Get<List<GameUISelectableButton>>(enemies, EnemyVariantButtonsField);
            if (variants != null)
            {
                for (int i = 0; i < variants.Count; i++)
                {
                    int index = i + 1;
                    GameUISelectableButton variant = variants[i];
                    screen.AddAccessibleElement(new ProxyCompendiumEnemyVariantButton(variant, index),
                        variant != null ? variant.gameObject : null);
                }
            }

            AddEnemyDetails(screen, details);
        }

        public string Signature(global::CompendiumSection section)
        {
            return CountActive<global::CharacterButtonUI>(section) + ":" + CountActive<global::CompendiumEnemyDetailsUI>(section) + ":" + SearchSignature(section);
        }

        private static void AddButton(CompendiumScreen screen, GameUISelectableButton button, string key)
        {
            if (button == null)
            {
                return;
            }

            screen.AddAccessibleElement(new LabeledButton(button, key),
                button.gameObject);
        }

        private static void AddEnemyDetails(CompendiumScreen screen, global::CompendiumEnemyDetailsUI details)
        {
            if (details == null)
            {
                return;
            }

            screen.AddAccessibleElement(new ProxyCompendiumEnemyDetails(details), details.gameObject);
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

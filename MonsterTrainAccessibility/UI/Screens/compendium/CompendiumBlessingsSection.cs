using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.UI.Elements;
using TMPro;

using MonsterTrainAccessibility.UI.Elements.Compendium;

namespace MonsterTrainAccessibility.UI.Screens
{
    internal sealed class CompendiumBlessingsSection : ICompendiumSection
    {
        private static readonly FieldInfo BlessingsCountLabelField = AccessTools.Field(typeof(global::CompendiumSectionBlessings), "countLabel")!;
        private static readonly FieldInfo BlessingsCollectionUIsField = AccessTools.Field(typeof(global::CompendiumSectionBlessings), "collectionUIs")!;
        private static readonly FieldInfo RelicCollectionTooltipProviderField = AccessTools.Field(typeof(global::CompendiumRelicCollection), "tooltipProvider")!;
        private static readonly FieldInfo RelicCollectionRelicUIsField = AccessTools.Field(typeof(global::CompendiumRelicCollection), "relicUIs")!;

        public void Populate(CompendiumScreen screen, global::CompendiumSection section)
        {
            global::CompendiumSectionBlessings blessings = section as global::CompendiumSectionBlessings;
            if (blessings == null)
            {
                return;
            }

            TMP_Text count = global::MonsterTrainAccessibility.Core.ReflectionUtil.Get<TMP_Text>(blessings, BlessingsCountLabelField);
            if (count != null)
            {
                screen.AddAccessibleElement(new ProxyCompendiumBlessingsProgress(count), count.gameObject);
            }

            List<global::CompendiumRelicCollection> collections = global::MonsterTrainAccessibility.Core.ReflectionUtil.Get<List<global::CompendiumRelicCollection>>(blessings, BlessingsCollectionUIsField);
            if (collections == null)
            {
                return;
            }

            for (int i = 0; i < collections.Count; i++)
            {
                AddCollection(screen, collections[i]);
            }
        }

        public string Signature(global::CompendiumSection section)
        {
            return CountActive<global::CompendiumRelicCollection>(section) + ":" + CountActive<global::CompendiumRelicUI>(section);
        }

        private static void AddCollection(CompendiumScreen screen, global::CompendiumRelicCollection collection)
        {
            if (collection == null)
            {
                return;
            }

            TooltipProviderComponent provider = global::MonsterTrainAccessibility.Core.ReflectionUtil.Get<TooltipProviderComponent>(collection, RelicCollectionTooltipProviderField);
            screen.AddAccessibleElement(new ProxyCompendiumRelicCollection(collection, provider), collection.gameObject);

            List<global::CompendiumRelicUI> relics = global::MonsterTrainAccessibility.Core.ReflectionUtil.Get<List<global::CompendiumRelicUI>>(collection, RelicCollectionRelicUIsField);
            if (relics == null)
            {
                return;
            }

            for (int i = 0; i < relics.Count; i++)
            {
                global::CompendiumRelicUI relic = relics[i];
                if (relic == null || relic.SelectableUI == null)
                {
                    continue;
                }

                screen.AddAccessibleElement(new ProxyRelicIcon(relic), relic.gameObject, relic.SelectableUI.component != null ? relic.SelectableUI.component.gameObject : null);
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

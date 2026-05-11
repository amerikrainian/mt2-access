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
    internal sealed class CompendiumCardFramesSection : ICompendiumSection
    {
        private static readonly FieldInfo CardFramesNoMasteryButtonField = AccessTools.Field(typeof(global::CompendiumSectionCardFrames), "noMasteryFrameButton")!;
        private static readonly FieldInfo CardFramesNoMasteryLabelField = AccessTools.Field(typeof(global::CompendiumSectionCardFrames), "noMasteryFrameButtonLabel")!;
        private static readonly FieldInfo CardFramesOptionUIsField = AccessTools.Field(typeof(global::CompendiumSectionCardFrames), "cardFrameOptionUIs")!;

        public void Populate(CompendiumScreen screen, global::CompendiumSection section)
        {
            global::CompendiumSectionCardFrames cardFrames = section as global::CompendiumSectionCardFrames;
            if (cardFrames == null)
            {
                return;
            }

            GameUISelectableButton noMastery = global::MonsterTrainAccessibility.Core.ReflectionUtil.Get<GameUISelectableButton>(cardFrames, CardFramesNoMasteryButtonField);
            TMP_Text noMasteryLabel = global::MonsterTrainAccessibility.Core.ReflectionUtil.Get<TMP_Text>(cardFrames, CardFramesNoMasteryLabelField);
            screen.AddAccessibleElement(new TextOrLabeledButton(noMastery, noMasteryLabel, "COMPENDIUM.CARD_FRAMES.NONE"),
                noMastery != null ? noMastery.gameObject : null);

            List<global::CardFrameOptionUI> frames = global::MonsterTrainAccessibility.Core.ReflectionUtil.Get<List<global::CardFrameOptionUI>>(cardFrames, CardFramesOptionUIsField);
            if (frames == null)
            {
                return;
            }

            for (int i = 0; i < frames.Count; i++)
            {
                global::CardFrameOptionUI frame = frames[i];
                if (frame == null || frame.Button == null)
                {
                    continue;
                }

                screen.AddAccessibleElement(new ProxyCompendiumCardFrameOption(frame),
                    frame.gameObject,
                    frame.Button.gameObject);
            }
        }

        public string Signature(global::CompendiumSection section)
        {
            return CountActive<global::CardFrameOptionUI>(section).ToString(System.Globalization.CultureInfo.InvariantCulture);
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

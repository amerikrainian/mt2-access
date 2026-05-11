using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.UI;
using MonsterTrainAccessibility.UI.Elements;
using ShinyShoe;
using UnityEngine;

namespace MonsterTrainAccessibility.UI.Screens
{
    internal sealed class SoulforgeScreen : GameScreen
    {
        private static readonly FieldInfo TabsField = AccessTools.Field(typeof(global::SoulforgeScreen), "tabs")!;
        private static readonly FieldInfo UnitSoulItemsField = AccessTools.Field(typeof(global::SoulforgeScreen), "unitSoulItems")!;
        private static readonly FieldInfo SpellSoulItemsField = AccessTools.Field(typeof(global::SoulforgeScreen), "spellSoulItems")!;
        private static readonly FieldInfo ArtifactSoulItemsField = AccessTools.Field(typeof(global::SoulforgeScreen), "artifactSoulItems")!;
        private static readonly FieldInfo BackButtonField = AccessTools.Field(typeof(global::SoulforgeScreen), "backButton")!;
        private static readonly FieldInfo SaveManagerField = AccessTools.Field(typeof(global::SoulforgeScreen), "saveManager")!;

        private readonly global::SoulforgeScreen _screen;

        public SoulforgeScreen(global::SoulforgeScreen screen)
        {
            _screen = screen;
        }

        public override void OnPush()
        {
            base.OnPush();
            TryFocusFirstSoul();
        }

        protected override void BuildRegistry()
        {
            ListContainer root = new ListContainer
            {
                AnnounceName = false,
                AnnouncePosition = true,
                NavigationAxis = NavigationAxis.Vertical
            };
            RootElement = root;

            AddTabs(root);
            SaveManager saveManager = Get<SaveManager>(_screen, SaveManagerField);
            AddSoulItems(root, Get<List<global::SoulSelectionItemUI>>(_screen, UnitSoulItemsField), saveManager);
            AddSoulItems(root, Get<List<global::SoulSelectionItemUI>>(_screen, SpellSoulItemsField), saveManager);
            AddSoulItems(root, Get<List<global::SoulSelectionItemUI>>(_screen, ArtifactSoulItemsField), saveManager);
            AddButton(root, Get<GameUISelectableButton>(_screen, BackButtonField));
        }

        private void AddTabs(ListContainer root)
        {
            List<global::SettingsTab> tabs = Get<List<global::SettingsTab>>(_screen, TabsField);
            if (tabs == null || tabs.Count == 0)
            {
                return;
            }

            ListContainer tabList = new ListContainer
            {
                AnnounceName = false,
                AnnouncePosition = true,
                NavigationAxis = NavigationAxis.Horizontal
            };

            for (int i = 0; i < tabs.Count; i++)
            {
                ProxySettingsTab element = new ProxySettingsTab(tabs[i]);
                if (element.Button == null)
                {
                    continue;
                }

                tabList.Add(element);
                Register(element.Button.gameObject, element);
            }

            if (tabList.Children.Count > 0)
            {
                root.Add(tabList);
            }
        }

        private void AddSoulItems(ListContainer root, IReadOnlyList<global::SoulSelectionItemUI> items, SaveManager saveManager)
        {
            if (items == null)
            {
                return;
            }

            for (int i = 0; i < items.Count; i++)
            {
                ProxySoulSelectionItem element = new ProxySoulSelectionItem(items[i], saveManager);
                if (element.Button == null)
                {
                    continue;
                }

                root.Add(element);
                Register(element.Button.gameObject, element);
            }
        }

        private void AddButton(ListContainer root, GameUISelectableButton button)
        {
            if (button == null)
            {
                return;
            }

            LabeledButton element = new LabeledButton(button, () => AuthoredLabelReader.ReadMessage(button));
            root.Add(element);
            Register(button.gameObject, element);
        }

        private void TryFocusFirstSoul()
        {
            ListContainer root = RootElement as ListContainer;
            if (root == null)
            {
                return;
            }

            for (int i = 0; i < root.Children.Count; i++)
            {
                if (root.Children[i] is ProxySoulSelectionItem && root.Children[i].IsVisible)
                {
                    root.SetFocusIndex(i);
                    return;
                }
            }

            root.FocusFirst();
        }

        private static T Get<T>(object owner, FieldInfo field) where T : class
        {
            return owner != null ? field.GetValue(owner) as T : null;
        }
    }
}

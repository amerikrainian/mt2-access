using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Elements;
using ShinyShoe;
using TMPro;

namespace MonsterTrainAccessibility.UI.Screens
{
    internal sealed class SoulProgressionScreen : ListNavigationGameScreen
    {
        private static readonly FieldInfo HeaderLabelField = AccessTools.Field(typeof(global::SoulProgressionScreen), "headerLabel")!;
        private static readonly FieldInfo CollectButtonField = AccessTools.Field(typeof(global::SoulProgressionScreen), "collectButton")!;
        private static readonly FieldInfo SoulProgressionUIField = AccessTools.Field(typeof(global::SoulProgressionScreen), "soulProgressionUI")!;
        private static readonly FieldInfo SoulsUnlockProgressField = AccessTools.Field(typeof(global::SoulProgressionScreen), "soulsUnlockProgress")!;
        private static readonly FieldInfo SaveManagerField = AccessTools.Field(typeof(global::SoulProgressionScreen), "saveManager")!;
        private static readonly FieldInfo SoulProgressItemUIsField = AccessTools.Field(typeof(global::SoulProgressionUI), "soulProgressItemUIs")!;

        private readonly global::SoulProgressionScreen _screen;

        public SoulProgressionScreen(global::SoulProgressionScreen screen)
        {
            _screen = screen;
            RootList.AnnouncePosition = false;
        }

        public override bool ShouldRestoreNavigationFocus() => false;

        protected override void PopulateList()
        {
            TMP_Text header = Get<TMP_Text>(_screen, HeaderLabelField);
            if (header != null)
            {
                AddElement(new ProxyUnlockText(header), header.gameObject);
            }

            AddSoulProgressItems();
            AddButton(Get<GameUISelectableButton>(_screen, CollectButtonField));
        }

        protected override string BuildSignature()
        {
            List<global::GameOverScreen.SoulUnlockProgressInfo> items = ProgressItems();
            List<global::SoulProgressItemUI> itemUIs = ProgressItemUIs();
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append(items != null ? items.Count : 0).Append(':')
                .Append(itemUIs != null ? itemUIs.Count : 0).Append('|');
            if (items != null)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    global::GameOverScreen.SoulUnlockProgressInfo item = items[i];
                    sb.Append(item.unlockObj?.GetID()).Append(':')
                        .Append(item.nextUnlockObj?.GetID()).Append(':')
                        .Append(item.runStartValue).Append(':')
                        .Append(item.currentValue).Append(':')
                        .Append(item.unlockValue).Append(':')
                        .Append(item.isLevelUp).Append(':')
                        .Append(item.isUnlock).Append(':')
                        .Append(item.isNextCriteria).Append('|');
                }
            }

            GameUISelectableButton button = Get<GameUISelectableButton>(_screen, CollectButtonField);
            sb.Append(button != null && button.gameObject.activeInHierarchy ? '1' : '0');
            return sb.ToString();
        }

        private void AddSoulProgressItems()
        {
            List<global::GameOverScreen.SoulUnlockProgressInfo> items = ProgressItems();
            if (items == null)
            {
                return;
            }

            SaveManager saveManager = Get<SaveManager>(_screen, SaveManagerField);
            List<global::SoulProgressItemUI> itemUIs = ProgressItemUIs();
            for (int i = 0; i < items.Count; i++)
            {
                global::SoulProgressItemUI itemUI = itemUIs != null && i < itemUIs.Count ? itemUIs[i] : null;
                GameUISelectableWithNavigation selectable = itemUI != null ? itemUI.GetSelectable() : null;
                ProxySoulProgressionItem element = new ProxySoulProgressionItem(items[i], selectable, saveManager);
                AddElement(
                    element,
                    itemUI != null ? itemUI.gameObject : null,
                    selectable != null ? selectable.gameObject : null);
            }
        }

        private void AddButton(GameUISelectableButton button)
        {
            if (button == null)
            {
                return;
            }

            ProxyUnlockButton element = new ProxyUnlockButton(button);
            AddElement(element, button.gameObject);
        }

        private List<global::GameOverScreen.SoulUnlockProgressInfo> ProgressItems()
        {
            return Get<List<global::GameOverScreen.SoulUnlockProgressInfo>>(_screen, SoulsUnlockProgressField);
        }

        private List<global::SoulProgressItemUI> ProgressItemUIs()
        {
            global::SoulProgressionUI ui = Get<global::SoulProgressionUI>(_screen, SoulProgressionUIField);
            return Get<List<global::SoulProgressItemUI>>(ui, SoulProgressItemUIsField);
        }

        private static T Get<T>(object owner, FieldInfo field) where T : class
        {
            return owner != null ? field.GetValue(owner) as T : null;
        }
    }
}

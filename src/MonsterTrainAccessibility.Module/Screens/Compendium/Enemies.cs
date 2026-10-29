using System;
using System.Collections.Generic;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.Strings;
using MonsterTrainAccessibility.Core.UI;
using MonsterTrainAccessibility.Module.Interop;
using MonsterTrainAccessibility.Module.Nodes;
using MonsterTrainAccessibility.Module.UI;
using ShinyShoe;
using TMPro;

namespace MonsterTrainAccessibility.Module.Screens
{
    public sealed partial class CompendiumScreen
    {
        // ---- Enemies ----

        // One stop, top to bottom: the lists as tabs (base game, Soul Savior) with the list's filters
        // beside them; the search, on a line of its own (Left and Right are its caret's); the enemies as
        // a row, landing on the chosen one; the chosen enemy's variants as a row; its details.
        private void Enemies(GraphBuilder b, CompendiumSectionEnemies enemies)
        {
            bool soulSavior = SoulSaviorList(enemies);
            bool tabs = enemies.hasRailforgedDLC;
            bool filters = Shown(enemies.covenantRankFilterUI) || Shown(enemies.difficultyTierFilterUI);
            if (tabs || filters)
            {
                b.StartRow(entry: () => tabs && SoulSaviorList(enemies) ? 1 : 0);
                if (tabs)
                {
                    b.AddItem(ControlId.Structural("logbook:enemies:base"), ListTab(enemies.baseGameButton, () => !SoulSaviorList(enemies)));
                    b.AddItem(ControlId.Structural("logbook:enemies:soulsavior"), ListTab(enemies.railforgedPageButton, () => SoulSaviorList(enemies)));
                }
                FilterNodes.Dropdown(b, "logbook:enemies:covenant", enemies.covenantRankFilterUI);
                FilterNodes.Dropdown(b, "logbook:enemies:tier", enemies.difficultyTierFilterUI);
                b.EndRow();
            }
            FilterNodes.Search(b, _enemySearch, enemies.searchFilterUI);

            var shown = new List<CharacterButtonUI>();
            foreach (var enemy in soulSavior ? enemies.railforgedCharacterButtons : enemies.baseGameCharacterButtons)
                if (enemy != null && enemy.CharacterEntry?.CharacterData != null && enemy.gameObject.activeInHierarchy) shown.Add(enemy);
            if (shown.Count > 0)
            {
                b.PushContext(GameText.Term("Compendium_Section_Enemies"), Strings.RoleList);
                b.StartRow(entry: () => System.Math.Max(0, shown.FindIndex(e => e.IsChosen)));
                foreach (var enemy in shown)
                    b.AddItem(ControlId.Structural("logbook:enemy:" + enemy.CharacterEntry.CharacterData.GetID()), EnemyNodes.Enemy(enemy));
                b.EndRow();
                b.PopContext();
            }

            if (enemies.variantButtonRoot != null && enemies.variantButtonRoot.activeInHierarchy)
            {
                var variants = new List<int>();
                for (int i = 0; i < enemies.variantButtons.Count; i++)
                    if (enemies.variantButtons[i] != null && enemies.variantButtons[i].gameObject.activeInHierarchy) variants.Add(i);
                if (variants.Count > 0)
                {
                    b.StartRow(entry: () => System.Math.Max(0, variants.IndexOf((int)enemies.selectedTab)));
                    foreach (int i in variants)
                        b.AddItem(ControlId.Structural("logbook:enemy:variant:" + i), EnemyNodes.Variant(enemies.variantButtons[i], i));
                    b.EndRow();
                }
            }
            EnemyNodes.Details(b, "logbook:enemy:details", enemies.characterDataUI);
        }

        // As FilterNodes.Dropdown declares one: shown, with its dropdown.
        private static bool Shown(DropdownFilterUI filter) => filter != null && filter.dropdown != null && filter.gameObject.activeInHierarchy;

        private static bool SoulSaviorList(CompendiumSectionEnemies enemies)
            => enemies.currentScrollRectRootGameObject != null && enemies.railforgedEnemiesScrollRectRoot != null
               && enemies.currentScrollRectRootGameObject == enemies.railforgedEnemiesScrollRectRoot.gameObject;

        // The game shows one button, the way to the other list; both read as tabs, landing on the other
        // one pressing it.
        private static NodeVtable ListTab(GameUISelectableButton button, Func<bool> open)
        {
            Func<string> name = () =>
            {
                foreach (var text in button.GetComponentsInChildren<TMP_Text>(true))
                    if (!string.IsNullOrWhiteSpace(text.Content()) && text.GetComponentInParent<GameUIControlMapping>() == null) return text.Content();
                return null;
            };
            Action show = () => { if (!open() && button.gameObject.activeInHierarchy) GameInput.Submit(button); };
            return new NodeVtable
            {
                ControlType = ControlTypes.Tab,
                Announcements = new List<NodeAnnouncement> { GameNodes.LabelPart(name) },
                SearchText = name,
                OnFocus = show,
                OnActivate = show,
            };
        }
    }
}

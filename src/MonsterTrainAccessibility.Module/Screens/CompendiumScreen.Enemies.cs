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

        // The lists as tabs (base game, Soul Savior) and the list's filter; the search; the enemies;
        // the chosen enemy's variants and details.
        private void Enemies(GraphBuilder b, CompendiumSectionEnemies enemies)
        {
            bool soulSavior = SoulSaviorList(enemies);
            if (enemies.hasRailforgedDLC)
            {
                b.StartRow(entry: () => SoulSaviorList(enemies) ? 1 : 0);
                b.AddItem(ControlId.Structural("logbook:enemies:base"), ListTab(enemies.baseGameButton, () => !SoulSaviorList(enemies)));
                b.AddItem(ControlId.Structural("logbook:enemies:soulsavior"), ListTab(enemies.railforgedPageButton, () => SoulSaviorList(enemies)));
                b.EndRow();
            }
            FilterNodes.Dropdown(b, "logbook:enemies:covenant", enemies.covenantRankFilterUI);
            FilterNodes.Dropdown(b, "logbook:enemies:tier", enemies.difficultyTierFilterUI);

            b.BeginStop("search");
            FilterNodes.Search(b, _enemySearch, enemies.searchFilterUI);

            b.BeginStop("enemies");
            b.PushContext(GameText.Term("Compendium_Section_Enemies"), Strings.RoleList);
            foreach (var enemy in soulSavior ? enemies.railforgedCharacterButtons : enemies.baseGameCharacterButtons)
            {
                var data = enemy != null ? enemy.CharacterEntry?.CharacterData : null;
                if (data == null || !enemy.gameObject.activeInHierarchy) continue;
                b.AddItem(ControlId.Structural("logbook:enemy:" + data.GetID()), EnemyNodes.Enemy(enemy));
            }
            b.PopContext();

            b.BeginStop("details");
            if (enemies.variantButtonRoot != null && enemies.variantButtonRoot.activeInHierarchy)
            {
                b.StartRow(entry: () => System.Math.Max(0, (int)enemies.selectedTab));
                for (int i = 0; i < enemies.variantButtons.Count; i++)
                {
                    var variant = enemies.variantButtons[i];
                    if (variant != null && variant.gameObject.activeInHierarchy)
                        b.AddItem(ControlId.Structural("logbook:enemy:variant:" + i), EnemyNodes.Variant(variant, i));
                }
                b.EndRow();
            }
            EnemyNodes.Details(b, "logbook:enemy:details", enemies.characterDataUI);
        }

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
                    if (!string.IsNullOrWhiteSpace(text.text) && text.GetComponentInParent<GameUIControlMapping>() == null) return text.text;
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

using System.Collections.Generic;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.Input;
using MonsterTrainAccessibility.Core.Strings;
using MonsterTrainAccessibility.Core.UI;
using MonsterTrainAccessibility.Module.Interop;
using MonsterTrainAccessibility.Module.Nodes;
using MonsterTrainAccessibility.Module.UI;
using ShinyShoe;
using UnityEngine;
using Screen = MonsterTrainAccessibility.Core.Screens.Screen;

namespace MonsterTrainAccessibility.Module.Screens
{
    /// <summary>
    /// The run's HUD (<see cref="global::Hud"/>) as one list, which Tab opens anywhere in a run (on any
    /// screen that declares the run keys) and Tab or Escape closes: the pyre, coin, deck and Dragon's
    /// Hoard; the clans, Covenant (or Soul Savior's difficulty) and ring; the bosses ahead, a row; Soul
    /// Savior's souls, a row (<see cref="HudNodes.Soul"/>); the battle's own lines while in one; the train's stats; the
    /// artifacts, then the enemy's, the mutators and the Titan essences when there are any, each a row
    /// (as the train's stats are); game speed,
    /// the map, settings. Each in the game's words, its tooltips in the buffer; the deck, the hoard,
    /// game speed, the map and settings press through the game.
    /// </summary>
    public sealed class HudScreen : Screen
    {
        public const string ActionKey = "run.hud";

        private static readonly InputCategory[] Categories = { InputCategory.Run, InputCategory.UI };
        private static bool s_open;

        public override string Key => "run.hud";
        public override int Layer => 15;
        public override bool Exclusive => true;
        public override IReadOnlyList<InputCategory> InputCategories => Categories;

        private static global::Hud Hud => GameScreens.Active<global::Hud>(global::ScreenName.Hud);

        /// <summary>A run is on: the map or a battle is open (the HUD also stands behind the Outpost).</summary>
        public static bool Available
        {
            get
            {
                var manager = AllGameManagers.Instance != null ? AllGameManagers.Instance.GetScreenManager() : null;
                return Hud != null && manager != null && (manager.IsMapOpen() || manager.GetScreenActive(global::ScreenName.Game));
            }
        }

        public static void Toggle()
        {
            if (!s_open && !Available) return;
            s_open = !s_open;
        }

        public override bool IsActive()
        {
            if (s_open && !Available) s_open = false;
            return s_open;
        }

        public override void Build(GraphBuilder b)
        {
            var hud = Hud;
            if (hud == null) return;
            b.PushContext(Strings.HudTitle, Strings.RoleList);
            // The pyre presses into its heart's ability, a battle's action: a button only in a battle.
            var pyre = hud.towerHPUI;
            if (Shown(pyre))
                b.AddItem(Id("pyre"), HudNodes.Count(pyre, "HudTooltip_Health_Title", InBattle() ? pyre.pyreButton : null));
            if (Shown(hud.pyreAttackUI)) b.AddItem(Id("attack"), HudNodes.Count(hud.pyreAttackUI, "HudTooltip_PyreAttack_Title"));
            if (Shown(hud.pyreArmorUI)) b.AddItem(Id("armor"), HudNodes.Count(hud.pyreArmorUI, "HudTooltip_PyreArmor_Title"));
            if (Shown(hud.goldUI)) b.AddItem(Id("gold"), HudNodes.Count(hud.goldUI, "HudTooltip_Gold_Title"));
            if (Shown(hud.deckButton))
                b.AddItem(Id("deck"), Leaving(HudNodes.Count(hud.deckButton, "HudTooltip_Deck_Title", hud.deckButton.GetComponent<GameUISelectableButton>())));
            if (Shown(hud.dragonsHoardUI)) b.AddItem(Id("hoard"), Leaving(HudNodes.Count(hud.dragonsHoardUI, "HudTooltip_DragonsHoard_Title", hud.dragonsHoardButton)));
            if (Shown(hud.forgePointsUI)) b.AddItem(Id("forge"), HudNodes.ForgePoints(hud.forgePointsUI));
            Line(b, "clans", hud.classesUI);
            if (Shown(hud.covenantUI)) b.AddItem(Id("covenant"), StartingConditionsNodes.Covenant(hud.covenantUI));
            Line(b, "difficulty", hud.difficultyTierUI);
            Line(b, "ring", hud.runDistanceUI);
            Line(b, "endless", hud.endlessRunDistanceUI);
            Bosses(b, hud);
            Souls(b, hud.soulEquipUI);
            Line(b, "moon", hud.lunaCovenUI);
            Line(b, "battle", hud.battleInfoUI);
            TrainStats(b, hud.trainStatsUI);
            Relics(b, "artifacts", Strings.HudArtifacts, hud.blessingCollectionUI);
            Relics(b, "enemy", Strings.HudEnemyArtifacts, hud.sinCollectionUI);
            Relics(b, "mutators", GameText.Term("ScreenRunSetup_Mutators"), hud.mutatorCollectionUI);
            Relics(b, "essences", GameText.Term("Hud_ChallengeInfo_TFBUnlocks"), hud.tfbUnlockCollectionUI);
            if (Shown(hud.gameSpeedUI)) b.AddItem(Id("speed"), HudNodes.Tooltipped(hud.gameSpeedUI, hud.gameSpeedUI.gameUISelectable));
            Button(b, "map", hud.minimapButton, "ScreenKeyMapping_Control_Minimap");
            Button(b, "settings", hud.settingsButton, "ScreenMainMenu_Settings");
            b.PopContext();
        }

        public override IEnumerable<ElementAction> GetActions()
        {
            yield return new ElementAction(ActionIds.Back, Strings.Get("bind.ui.back"), _ => s_open = false);
        }

        private static ControlId Id(string key) => ControlId.Structural("hud:" + key);

        // A widget read by its tooltips (the clans, the ring, the moon), while the game shows it.
        private static void Line(GraphBuilder b, string key, Component widget)
        {
            if (Shown(widget) && TooltipReader.Lines(widget).Count > 0) b.AddItem(Id(key), TooltipReader.Item(widget));
        }

        // The bosses the run is headed for, as the game marks them, in order, each by its tooltips: its
        // name and story, then its power. The game fills one list of its marks by the kind of run (the
        // two share widgets).
        private static void Bosses(GraphBuilder b, global::Hud hud)
        {
            var save = AllGameManagers.Instance != null ? AllGameManagers.Instance.GetSaveManager() : null;
            var list = save != null && save.IsRegionRun ? hud.soulSaviorBossTargetUis : hud.bossTargetUis;
            var targets = new List<BossTargetUI>();
            if (list != null)
                foreach (var target in list)
                    if (Shown(target) && TooltipReader.Lines(target).Count > 0) targets.Add(target);
            if (targets.Count == 0) return;
            b.StartRow();
            for (int i = 0; i < targets.Count; i++) b.AddItem(Id("boss:" + i), TooltipReader.Item(targets[i]));
            b.EndRow();
        }

        // Soul Savior's souls under the game's "Souls", a row; while the game has them folded away (its
        // dropdown, as in a battle), its button that unfolds them.
        private static void Souls(GraphBuilder b, SoulEquipUI equip)
        {
            if (!Shown(equip)) return;
            string header = ChallengeProgressScreen.HeaderOf(equip, "Text souls");
            var layout = equip.GetSoulHudItemsLayout();
            var items = equip.GetSoulHudItems();
            if (layout == null || !layout.gameObject.activeInHierarchy || items == null)
            {
                var dropdown = equip.GetDropdownButton();
                if (Shown(dropdown)) b.AddItem(Id("souls"), GameWidgets.Button(dropdown, () => header));
                return;
            }
            b.PushContext(header, Strings.RoleList);
            b.StartRow();
            for (int i = 0; i < items.Count; i++)
                if (Shown(items[i]) && items[i].GetSoulState() != null)
                    b.AddItem(Id("soul:" + items[i].GetSoulState().GetRelicDataID()), HudNodes.Soul(items[i], equip, () => s_open = false));
            b.EndRow();
            b.PopContext();
        }

        // The train's stats, a row, which the game shows only on hover: their numbers are kept up regardless.
        private static void TrainStats(GraphBuilder b, TrainStatsUI stats)
        {
            if (!Shown(stats)) return;
            var numbers = new List<TMPro.TMP_Text>();
            foreach (var number in new[] { stats.energyPerTurnLabel, stats.deploymentPhaseEnergyPerTurnLabel, stats.cardsPerTurnLabel, stats.capacityPerFloorLabel })
                if (number != null && !string.IsNullOrWhiteSpace(number.Content())) numbers.Add(number);
            if (numbers.Count == 0) return;
            b.PushContext(Strings.HudTrainStats, Strings.RoleList);
            b.StartRow();
            for (int i = 0; i < numbers.Count; i++) b.AddItem(Id("train:" + i), HudNodes.TrainStat(numbers[i]));
            b.EndRow();
            b.PopContext();
        }

        // A relic collection, all its pages, a row: one item per relic.
        private static void Relics(GraphBuilder b, string key, string title, RelicCollectionUI collection)
        {
            if (!Shown(collection) || collection.relicIcons == null) return;
            var icons = new List<RelicIconUI>();
            foreach (var icon in collection.relicIcons)
                if (icon != null && icon.CurrentRelicId != null && icon.Tooltips != null && icon.Tooltips.Count > 0) icons.Add(icon);
            if (icons.Count == 0) return;
            b.PushContext(title, Strings.RoleList);
            b.StartRow();
            // By place, not by id: a run can hold two of the same artifact.
            for (int i = 0; i < icons.Count; i++) b.AddItem(Id(key + ":" + i), HudNodes.Relic(icons[i]));
            b.EndRow();
            b.PopContext();
        }

        private static void Button(GraphBuilder b, string key, GameUISelectableButton button, string term)
        {
            if (!Shown(button)) return;
            b.AddItem(Id(key), Leaving(GameWidgets.Button(button, () => GameText.Term(term))));
        }

        private static bool Shown(Component widget) => widget != null && widget.gameObject.activeInHierarchy;

        // A press that opens another screen (the deck, the hoard, the map, settings) closes the list, as
        // the game's own HUD navigation ends: the screen it opens is in front, not under the list.
        private static NodeVtable Leaving(NodeVtable vt)
        {
            var press = vt.OnActivate;
            if (press != null) vt.OnActivate = () =>
            {
                s_open = false;
                press();
            };
            return vt;
        }

        private static bool InBattle()
        {
            var manager = AllGameManagers.Instance != null ? AllGameManagers.Instance.GetScreenManager() : null;
            return manager != null && manager.GetScreenActive(global::ScreenName.Game);
        }
    }
}

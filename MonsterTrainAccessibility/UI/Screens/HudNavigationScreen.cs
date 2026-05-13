using System;
using MonsterTrainAccessibility.Util;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Buffers;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Input;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI;
using MonsterTrainAccessibility.UI.Elements;
using ShinyShoe;
using TMPro;
using UnityEngine;

namespace MonsterTrainAccessibility.UI.Screens
{
    internal sealed class HudNavigationScreen : GameScreen
    {
        private static readonly FieldInfo DeckButtonField = AccessTools.Field(typeof(global::Hud), "deckButton")!;
        private static readonly FieldInfo GoldUIField = AccessTools.Field(typeof(global::Hud), "goldUI")!;
        private static readonly FieldInfo TowerHPUIField = AccessTools.Field(typeof(global::Hud), "towerHPUI")!;
        private static readonly FieldInfo PyreAttackUIField = AccessTools.Field(typeof(global::Hud), "pyreAttackUI")!;
        private static readonly FieldInfo PyreArmorUIField = AccessTools.Field(typeof(global::Hud), "pyreArmorUI")!;
        private static readonly FieldInfo TrainStatsUIField = AccessTools.Field(typeof(global::Hud), "trainStatsUI")!;
        private static readonly FieldInfo ClassesUIField = AccessTools.Field(typeof(global::Hud), "classesUI")!;
        private static readonly FieldInfo DifficultyTierUIField = AccessTools.Field(typeof(global::Hud), "difficultyTierUI")!;
        private static readonly FieldInfo CovenantUIField = AccessTools.Field(typeof(global::Hud), "covenantUI")!;
        private static readonly FieldInfo RunDistanceUIField = AccessTools.Field(typeof(global::Hud), "runDistanceUI")!;
        private static readonly FieldInfo EndlessRunDistanceUIField = AccessTools.Field(typeof(global::Hud), "endlessRunDistanceUI")!;
        private static readonly FieldInfo BossTargetUisField = AccessTools.Field(typeof(global::Hud), "bossTargetUis")!;
        private static readonly FieldInfo SoulSaviorBossTargetUisField = AccessTools.Field(typeof(global::Hud), "soulSaviorBossTargetUis")!;
        private static readonly FieldInfo LunaCovenUIField = AccessTools.Field(typeof(global::Hud), "lunaCovenUI")!;
        private static readonly FieldInfo BattleTurnCounterField = AccessTools.Field(typeof(global::Hud), "battleTurnCounter")!;
        private static readonly FieldInfo MinimapButtonField = AccessTools.Field(typeof(global::Hud), "minimapButton")!;
        private static readonly FieldInfo SettingsButtonField = AccessTools.Field(typeof(global::Hud), "settingsButton")!;
        private static readonly FieldInfo GameSpeedUIField = AccessTools.Field(typeof(global::Hud), "gameSpeedUI")!;
        private static readonly FieldInfo DragonsHoardButtonField = AccessTools.Field(typeof(global::Hud), "dragonsHoardButton")!;
        private static readonly FieldInfo DragonsHoardShortcutButtonField = AccessTools.Field(typeof(global::Hud), "dragonsHoardShortcutButton")!;
        private static readonly FieldInfo DragonsHoardUIField = AccessTools.Field(typeof(global::Hud), "dragonsHoardUI")!;
        private static readonly FieldInfo ForgePointsUIField = AccessTools.Field(typeof(global::Hud), "forgePointsUI")!;
        private static readonly FieldInfo BlessingCollectionUIField = AccessTools.Field(typeof(global::Hud), "blessingCollectionUI")!;
        private static readonly FieldInfo SinCollectionUIField = AccessTools.Field(typeof(global::Hud), "sinCollectionUI")!;
        private static readonly FieldInfo MutatorCollectionUIField = AccessTools.Field(typeof(global::Hud), "mutatorCollectionUI")!;
        private static readonly FieldInfo TfbUnlockCollectionUIField = AccessTools.Field(typeof(global::Hud), "tfbUnlockCollectionUI")!;
        private static readonly FieldInfo SoulEquipUIField = AccessTools.Field(typeof(global::Hud), "soulEquipUI")!;
        private static readonly FieldInfo RelicIconsField = AccessTools.Field(typeof(global::RelicCollectionUI), "relicIcons")!;

        private readonly global::Hud _hud;
        private readonly ListContainer _root = new ListContainer
        {
            AnnounceName = false,
            AnnouncePosition = false,
            NavigationAxis = NavigationAxis.Vertical
        };
        private string _signature;

        public HudNavigationScreen(global::Hud hud)
        {
            _hud = hud;
            ClaimAction("buffer_prev_item");
            ClaimAction("buffer_next_item");
            ClaimAction("buffer_prev");
            ClaimAction("buffer_next");
        }

        public override void OnPush()
        {
            base.OnPush();
            _root.FocusFirst();
        }

        public override void OnUpdate()
        {
            if (_hud == null || !_hud.IsHudNavigationEnabled())
            {
                ScreenManager.RemoveFromTree(this);
                return;
            }

            string nextSignature = BuildSignature();
            if (!string.Equals(nextSignature, _signature, StringComparison.Ordinal))
            {
                int oldIndex = _root.FocusIndex;
                BuildRegistry();
                _root.SetFocusIndex(oldIndex);
            }
        }

        public override bool OnActionJustPressed(InputAction action)
        {
            switch (action?.Key)
            {
                case "buffer_prev_item":
                    BufferManager.Instance.PreviousItem();
                    return true;
                case "buffer_next_item":
                    BufferManager.Instance.NextItem();
                    return true;
                case "buffer_prev":
                    BufferManager.Instance.PreviousBuffer();
                    return true;
                case "buffer_next":
                    BufferManager.Instance.NextBuffer();
                    return true;
                default:
                    return base.OnActionJustPressed(action);
            }
        }

        protected override void BuildRegistry()
        {
            ClearRegistry();
            _root.Clear();
            RootElement = _root;

            AddDeck();
            AddGold();
            AddPyreHealth();
            AddPyreAttack();
            AddPyreArmor();
            AddClasses();
            AddCovenantOrDifficulty();
            AddDistance();
            HashSet<string> bossTargetsSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            AddBossTargets(Get<List<global::BossTargetUI>>(_hud, BossTargetUisField), bossTargetsSeen);
            AddBossTargets(Get<List<global::BossTargetUI>>(_hud, SoulSaviorBossTargetUisField), bossTargetsSeen);
            AddLunaCoven();
            AddBattleTurnCounter();
            AddTrainStats();
            AddRelicCollection(Get<global::PaginatedRelicCollectionUI>(_hud, BlessingCollectionUIField), "HUD.RELICS");
            AddRelicCollection(Get<global::DelayedDisplayRelicCollectionUI>(_hud, SinCollectionUIField), "HUD.ENEMY_RELICS");
            AddRelicCollection(Get<global::DelayedDisplayRelicCollectionUI>(_hud, MutatorCollectionUIField), "HUD.MUTATORS");
            AddRelicCollection(Get<global::DelayedDisplayRelicCollectionUI>(_hud, TfbUnlockCollectionUIField), "HUD.UNLOCKS");
            AddSoulEquip();
            AddDragonsHoard();
            AddForgePoints();
            AddGameSpeed();
            AddHudButton(Get<GameUISelectableButton>(_hud, MinimapButtonField), "HUD.MINIMAP");
            AddHudButton(Get<GameUISelectableButton>(_hud, SettingsButtonField), "HUD.SETTINGS");

            _signature = BuildSignature();
        }

        private void AddDeck()
        {
            global::DeckCountUI deck = Get<global::DeckCountUI>(_hud, DeckButtonField);
            if (deck == null)
            {
                return;
            }

            ProxyDeckButton element = new ProxyDeckButton(deck);
            AddElement(element, deck.gameObject);
        }

        private void AddGold()
        {
            global::GoldUI gold = Get<global::GoldUI>(_hud, GoldUIField);
            if (gold == null)
            {
                return;
            }

            AddElement(new ProxyGoldUI(gold), gold.gameObject);
        }

        private void AddPyreHealth()
        {
            global::TowerHPUI pyre = Get<global::TowerHPUI>(_hud, TowerHPUIField);
            if (pyre == null)
            {
                return;
            }

            ProxyTowerHP element = new ProxyTowerHP(pyre);
            AddElement(element, pyre.gameObject);
        }

        private void AddPyreAttack()
        {
            global::PyreAttackUI attack = Get<global::PyreAttackUI>(_hud, PyreAttackUIField);
            if (attack != null)
            {
                AddElement(new ProxyPyreAttack(attack), attack.gameObject);
            }
        }

        private void AddPyreArmor()
        {
            global::PyreArmorUI armor = Get<global::PyreArmorUI>(_hud, PyreArmorUIField);
            if (armor != null)
            {
                AddElement(new ProxyPyreArmor(armor), armor.gameObject);
            }
        }

        private void AddClasses()
        {
            global::ChosenClassesUI classes = Get<global::ChosenClassesUI>(_hud, ClassesUIField);
            if (classes == null)
            {
                return;
            }

            ProxyClassesUI element = new ProxyClassesUI(classes);
            _root.Add(element);
            Register(classes.gameObject, element);
            Register(classes.SelectableUI?.component != null ? classes.SelectableUI.component.gameObject : null, element);
        }

        private void AddCovenantOrDifficulty()
        {
            global::ChallengeCovenantUI covenant = Get<global::ChallengeCovenantUI>(_hud, CovenantUIField);
            if (covenant != null)
            {
                AddElement(new ProxyTooltipInfo(covenant, "HUD.COVENANT"), covenant.gameObject);
            }

            global::DifficultyTierUI difficulty = Get<global::DifficultyTierUI>(_hud, DifficultyTierUIField);
            if (difficulty != null)
            {
                AddElement(new ProxyTooltipInfo(difficulty, "HUD.DIFFICULTY"), difficulty.gameObject);
            }
        }

        private void AddDistance()
        {
            global::RunDistanceDisplay distance = Get<global::RunDistanceDisplay>(_hud, RunDistanceUIField);
            if (distance != null)
            {
                AddElement(new ProxyTooltipInfo(distance, "HUD.DISTANCE"), distance.gameObject);
            }

            global::EndlessRunDistanceDisplay endless = Get<global::EndlessRunDistanceDisplay>(_hud, EndlessRunDistanceUIField);
            if (endless != null)
            {
                AddElement(new ProxyTooltipInfo(endless, "HUD.DISTANCE"), endless.gameObject);
            }
        }

        private void AddBossTargets(List<global::BossTargetUI> targets, HashSet<string> seenTitles)
        {
            if (targets == null)
            {
                return;
            }

            for (int i = 0; i < targets.Count; i++)
            {
                global::BossTargetUI target = targets[i];
                if (target == null || !target.gameObject.activeInHierarchy)
                {
                    continue;
                }

                string title = ProxyBossTarget.Title(target);
                if (!string.IsNullOrWhiteSpace(title) && seenTitles != null && !seenTitles.Add(title))
                {
                    continue;
                }

                AddElement(new ProxyBossTarget(target, currentOnly: false), target.gameObject);
            }
        }

        private void AddLunaCoven()
        {
            global::LunaCovenUI luna = Get<global::LunaCovenUI>(_hud, LunaCovenUIField);
            if (luna == null)
            {
                return;
            }

            AddElement(new ProxyLunaCoven(luna), luna.gameObject);
        }

        private void AddBattleTurnCounter()
        {
            global::BattleTurnCounter counter = Get<global::BattleTurnCounter>(_hud, BattleTurnCounterField);
            if (counter == null)
            {
                return;
            }

            AddElement(new ProxyBattleTurnCounter(counter), counter.gameObject);
        }

        private void AddTrainStats()
        {
            global::TrainStatsUI stats = Get<global::TrainStatsUI>(_hud, TrainStatsUIField);
            if (stats == null)
            {
                return;
            }

            AddElement(new ProxyTrainStatsUI(stats), stats.gameObject);
        }

        private void AddRelicCollection(global::RelicCollectionUI collection, string labelKey)
        {
            List<RelicIconUI> relics = Get<List<RelicIconUI>>(collection, RelicIconsField);
            if (collection == null || relics == null || relics.Count == 0)
            {
                return;
            }

            ListContainer group = new ListContainer(Message.Localized("ui", labelKey).Resolve(), NavigationAxis.Horizontal);
            for (int i = 0; i < relics.Count; i++)
            {
                RelicIconUI relic = relics[i];
                if (relic == null)
                {
                    continue;
                }

                ProxyRelicIcon element = new ProxyRelicIcon(relic);
                group.Add(element);
                Register(relic.gameObject, element);
                Register(relic.SelectableUI?.component != null ? relic.SelectableUI.component.gameObject : null, element);
            }

            if (group.Children.Count > 0)
            {
                _root.Add(group);
            }
        }

        private void AddSoulEquip()
        {
            global::SoulEquipUI souls = Get<global::SoulEquipUI>(_hud, SoulEquipUIField);
            if (souls == null)
            {
                return;
            }

            AddHudButton(souls.GetDropdownButton(), "HUD.SOULS", souls);
            List<global::SoulHudItemUI> soulItems = souls.GetSoulHudItems();
            if (soulItems == null || soulItems.Count == 0)
            {
                return;
            }

            ListContainer group = new ListContainer(Message.Localized("ui", "HUD.SOULS").Resolve(), NavigationAxis.Horizontal);
            for (int i = 0; i < soulItems.Count; i++)
            {
                global::SoulHudItemUI soulItem = soulItems[i];
                if (soulItem == null)
                {
                    continue;
                }

                ProxySoulHudItem element = new ProxySoulHudItem(soulItem);
                group.Add(element);
                Register(element, soulItem.gameObject, soulItem.GetSoulItemButton()?.gameObject);
            }

            if (group.Children.Count > 0)
            {
                _root.Add(group);
            }
        }

        private void AddDragonsHoard()
        {
            global::DragonsHoardUI hoard = Get<global::DragonsHoardUI>(_hud, DragonsHoardUIField);
            if (hoard == null)
            {
                return;
            }

            GameUISelectableButton button = Get<GameUISelectableButton>(_hud, DragonsHoardButtonField) ?? Get<GameUISelectableButton>(_hud, DragonsHoardShortcutButtonField);
            AddElement(new ProxyDragonsHoard(hoard, button), button?.gameObject);
        }

        private void AddForgePoints()
        {
            global::ForgePointsUI forge = Get<global::ForgePointsUI>(_hud, ForgePointsUIField);
            if (forge == null)
            {
                return;
            }

            GameUISelectableButton button = forge.GetForgingToggleButton();
            AddElement(new ProxyForgePoints(forge, button), button?.gameObject);
        }

        private void AddGameSpeed()
        {
            global::GameSpeedUI speed = Get<global::GameSpeedUI>(_hud, GameSpeedUIField);
            if (speed == null)
            {
                return;
            }

            ProxyGameSpeed element = new ProxyGameSpeed(speed);
            AddElement(element, speed.gameObject);
        }

        private void AddHudButton(GameUISelectableButton button, string labelKey, Component tooltipSource = null)
        {
            if (button == null)
            {
                return;
            }

            AddElement(new ProxyHudButton(button, "ui", labelKey, tooltipSource), button.gameObject);
        }

        private void AddElement(UIElement element, params GameObject[] targets)
        {
            if (element == null)
            {
                return;
            }

            _root.Add(element);
            Register(element, targets);
        }

        private string BuildSignature()
        {
            return CountActiveTargets(_root).ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        private static int CountActiveTargets(UIElement element)
        {
            Container container = element as Container;
            if (container == null)
            {
                return element != null && element.IsVisible ? 1 : 0;
            }

            int count = 0;
            for (int i = 0; i < container.Children.Count; i++)
            {
                count += CountActiveTargets(container.Children[i]);
            }
            return count;
        }

        private static T Get<T>(object owner, FieldInfo field) where T : class
        {
            return owner != null ? field.GetValue(owner) as T : null;
        }
    }
}

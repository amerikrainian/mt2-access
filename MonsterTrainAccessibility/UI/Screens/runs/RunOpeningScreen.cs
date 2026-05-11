using System;
using MonsterTrainAccessibility.Util;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI;
using MonsterTrainAccessibility.UI.Elements;
using ShinyShoe;
using TMPro;
using UnityEngine;

namespace MonsterTrainAccessibility.UI.Screens
{
    internal sealed class RunOpeningScreen : GameScreen
    {
        private static readonly FieldInfo BossDetailsUIsField = AccessTools.Field(typeof(global::RunOpeningScreen), "bossDetailsUIs")!;
        private static readonly FieldInfo ConfirmButtonField = AccessTools.Field(typeof(global::RunOpeningScreen), "confirmButton")!;
        private static readonly FieldInfo PyreHeartInfoUIField = AccessTools.Field(typeof(global::RunOpeningScreen), "pyreHeartInfoUI")!;
        private static readonly FieldInfo CardUIsField = AccessTools.Field(typeof(global::RunOpeningScreen), "cardUIs")!;
        private static readonly FieldInfo SoulHudInfoUisField = AccessTools.Field(typeof(global::RunOpeningSoulSaviorScreen), "soulHudInfoUis")!;
        private static readonly FieldInfo BossTooltipProviderField = AccessTools.Field(typeof(global::BossDetailsUI), "tooltipProvider")!;
        private static readonly FieldInfo BossTitleLabelField = AccessTools.Field(typeof(global::BossDetailsUI), "titleLabel")!;
        private static readonly FieldInfo BossDescriptionLabelField = AccessTools.Field(typeof(global::BossDetailsUI), "descriptionLabel")!;
        private static readonly FieldInfo PyreTitleLabelField = AccessTools.Field(typeof(global::PyreHeartInfoUI), "titleLabel")!;
        private static readonly HashSet<string> LoggedBossFallbacks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private readonly global::RunOpeningScreen _screen;

        public RunOpeningScreen(global::RunOpeningScreen screen)
        {
            _screen = screen;
        }

        public override void OnPush()
        {
            base.OnPush();
            (RootElement as ListContainer)?.FocusFirst();
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

            AddChosenClasses(root);
            AddCovenantOrDifficulty(root);
            AddDistance(root);
            AddBossTargets(root);
            AddBossDetails(root);
            AddPyreHeart(root);
            AddCards(root);
            AddSouls(root);
            GameUISelectableButton confirmButton = Get<GameUISelectableButton>(_screen, ConfirmButtonField);
            AddButton(root, confirmButton, () => ConfirmButtonLabel(confirmButton));
        }

        private void AddChosenClasses(ListContainer root)
        {
            global::ChosenClassesUI[] items = _screen.GetComponentsInChildren<global::ChosenClassesUI>(includeInactive: true);
            for (int i = 0; i < items.Length; i++)
            {
                global::ChosenClassesUI item = items[i];
                if (item == null)
                {
                    continue;
                }

                GameObject target = item.SelectableUI?.component?.gameObject ?? item.gameObject;
                AddTarget(root, target, new LabeledButton(target, () => Message.FromText(TooltipText(item))));
            }
        }

        private void AddCovenantOrDifficulty(ListContainer root)
        {
            global::ChallengeCovenantUI[] covenants = _screen.GetComponentsInChildren<global::ChallengeCovenantUI>(includeInactive: true);
            for (int i = 0; i < covenants.Length; i++)
            {
                global::ChallengeCovenantUI covenant = covenants[i];
                AddTarget(root, covenant.gameObject, new LabeledButton(covenant.gameObject, () => Message.FromText(TooltipText(covenant))));
            }

            global::DifficultyTierUI[] difficulties = _screen.GetComponentsInChildren<global::DifficultyTierUI>(includeInactive: true);
            for (int i = 0; i < difficulties.Length; i++)
            {
                global::DifficultyTierUI difficulty = difficulties[i];
                AddTarget(root, difficulty.gameObject, new LabeledButton(difficulty.gameObject, () => Message.FromText(TooltipText(difficulty))));
            }
        }

        private void AddDistance(ListContainer root)
        {
            global::RunDistanceDisplay[] distances = _screen.GetComponentsInChildren<global::RunDistanceDisplay>(includeInactive: true);
            for (int i = 0; i < distances.Length; i++)
            {
                global::RunDistanceDisplay distance = distances[i];
                AddTarget(root, distance.gameObject, new LabeledButton(distance.gameObject, () => Message.FromText(TooltipText(distance))));
            }

            global::EndlessRunDistanceDisplay[] endlessDistances = _screen.GetComponentsInChildren<global::EndlessRunDistanceDisplay>(includeInactive: true);
            for (int i = 0; i < endlessDistances.Length; i++)
            {
                global::EndlessRunDistanceDisplay distance = endlessDistances[i];
                AddTarget(root, distance.gameObject, new LabeledButton(distance.gameObject, () => Message.FromText(TooltipText(distance))));
            }
        }

        private void AddBossTargets(ListContainer root)
        {
            global::BossTargetUI[] targets = _screen.GetComponentsInChildren<global::BossTargetUI>(includeInactive: true);
            for (int i = 0; i < targets.Length; i++)
            {
                global::BossTargetUI target = targets[i];
                AddTarget(root, target.gameObject, new GameObjectElement(target.gameObject, () => Message.FromText(TooltipText(target))));
            }
        }

        private void AddBossDetails(ListContainer root)
        {
            List<global::BossDetailsUI> details = Get<List<global::BossDetailsUI>>(_screen, BossDetailsUIsField);
            if (details == null)
            {
                return;
            }

            ListContainer bosses = new ListContainer(Message.Localized("ui", "RUN_OPENING.BOSSES").Resolve(), NavigationAxis.Horizontal)
            {
                AnnouncePosition = false
            };

            for (int i = 0; i < details.Count; i++)
            {
                global::BossDetailsUI boss = details[i];
                if (boss == null)
                {
                    continue;
                }

                ProxyRunOpeningBossDetails element = new ProxyRunOpeningBossDetails(boss);
                bosses.Add(element);
                Register(boss.gameObject, element);
            }

            if (bosses.Children.Count > 0)
            {
                root.Add(bosses);
            }
        }

        private void AddPyreHeart(ListContainer root)
        {
            global::PyreHeartInfoUI pyre = Get<global::PyreHeartInfoUI>(_screen, PyreHeartInfoUIField);
            if (pyre == null)
            {
                return;
            }

            GameObject target = pyre.GetDefaultGameUISelectable()?.component?.gameObject ?? pyre.gameObject;
            AddTarget(root, target, new LabeledButton(target, () => Message.FromText(PyreHeartLabel(pyre))));
        }

        private void AddCards(ListContainer root)
        {
            List<CardUI> cards = Get<List<CardUI>>(_screen, CardUIsField);
            if (cards == null)
            {
                return;
            }

            ListContainer cardGroup = new ListContainer(Message.Localized("ui", "RUN_OPENING.CARDS").Resolve(), NavigationAxis.Horizontal)
            {
                AnnouncePosition = true
            };

            for (int i = 0; i < cards.Count; i++)
            {
                CardUI card = cards[i];
                if (card == null)
                {
                    continue;
                }

                IGameUIComponent selectable = card.SelectableUI;
                if (selectable == null || selectable.component == null)
                {
                    continue;
                }

                ProxyCombatCard element = new ProxyCombatCard(card, selectable);
                cardGroup.Add(element);
                Register(element, card.gameObject, selectable.component.gameObject);
            }

            if (cardGroup.Children.Count > 0)
            {
                root.Add(cardGroup);
            }
        }

        private void AddSouls(ListContainer root)
        {
            global::RunOpeningSoulSaviorScreen soulScreen = _screen as global::RunOpeningSoulSaviorScreen;
            if (soulScreen == null)
            {
                return;
            }

            List<global::SoulInfoUI> souls = Get<List<global::SoulInfoUI>>(soulScreen, SoulHudInfoUisField);
            if (souls == null)
            {
                return;
            }

            for (int i = 0; i < souls.Count; i++)
            {
                global::SoulInfoUI soul = souls[i];
                if (soul == null)
                {
                    continue;
                }

                GameObject target = soul.SelectableUI?.component?.gameObject ?? soul.gameObject;
                AddTarget(root, target, new LabeledButton(target, () => Message.FromText(soul.GetSoulData()?.GetName())));
            }
        }

        private void AddButton(ListContainer root, GameUISelectableButton button, Func<string> label)
        {
            if (button == null)
            {
                return;
            }

            AddTarget(root, button.gameObject, new LabeledButton(button, () => Message.FromText(ConfirmButtonLabel(button))));
        }

        private void AddTarget(ListContainer root, GameObject target, UIElement element)
        {
            if (target == null || element == null)
            {
                return;
            }

            root.Add(element);
            Register(target, element);
        }

        private static string GameButtonLabel(GameUISelectableButton button)
        {
            return Message.Clean(GameUIButtonSupport.ResolveLabel(button));
        }

        internal static string ConfirmButtonLabel(GameUISelectableButton button)
        {
            string label = GameButtonLabel(button);
            if (!string.IsNullOrWhiteSpace(label))
            {
                return label;
            }

            return AccessibilityText.LocalizeTerm("OK");
        }

        internal static string PyreHeartLabel(global::PyreHeartInfoUI pyre)
        {
            return pyre?.PyreHeartCharacterData != null
                ? AccessibilityText.LocalizeTerm(pyre.PyreHeartCharacterData.GetNameKey())
                : ReadLabel(pyre, PyreTitleLabelField);
        }

        internal static string TooltipText(Component component)
        {
            Message tooltip = global::MonsterTrainAccessibility.UI.TooltipText.ForComponent(component);
            string text = tooltip?.Resolve();
            if (!string.IsNullOrWhiteSpace(text))
            {
                return text;
            }

            return string.Empty;
        }

        internal static Message BossLabel(global::BossDetailsUI boss)
        {
            TooltipProviderComponent provider = Get<TooltipProviderComponent>(boss, BossTooltipProviderField);
            Message semantic = MessageList.FirstTooltip(provider?.Tooltips);
            if (semantic != null)
            {
                return semantic;
            }

            LogBossFallback(boss, "label");
            return Message.FromText(Message.JoinText(
                ReadLabel(boss, BossTitleLabelField),
                ReadLabel(boss, BossDescriptionLabelField)));
        }

        internal static Message BossTooltip(global::BossDetailsUI boss)
        {
            TooltipProviderComponent provider = Get<TooltipProviderComponent>(boss, BossTooltipProviderField);
            Message semantic = MessageList.TooltipList(provider?.Tooltips);
            if (semantic != null)
            {
                return semantic;
            }

            LogBossFallback(boss, "tooltip");
            return BossLabel(boss);
        }

        private static void LogBossFallback(global::BossDetailsUI boss, string context)
        {
            string key = context + ":" + boss?.gameObject?.name;
            if (!string.IsNullOrWhiteSpace(key) && LoggedBossFallbacks.Add(key))
            {
                Log.Info("[AccessibilityMod] Run opening boss text using rendered fallback for " + key);
            }
        }

        private static string ReadLabel(object owner, FieldInfo field)
        {
            TMP_Text text = Get<TMP_Text>(owner, field);
            return AccessibilityText.ReadLocalizedText(text);
        }

        private static T Get<T>(object owner, FieldInfo field) where T : class
        {
            return owner != null ? field.GetValue(owner) as T : null;
        }
    }
}

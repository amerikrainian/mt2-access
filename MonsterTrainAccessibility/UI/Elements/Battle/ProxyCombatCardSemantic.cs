using System;
using System.Collections.Generic;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI;
using MonsterTrainAccessibility.Util;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed partial class ProxyCombatCard
    {
        internal static Message GeneratedGameplayText(CardState card)
        {
            if (card == null)
            {
                return null;
            }

            AllGameManagers managers = AllGameManagers.Instance.OrNull();
            CardStatistics cardStatistics = managers?.GetCardStatistics();
            if (cardStatistics == null)
            {
                return null;
            }

            string text = AccessibilityLocalizationScope.Run(() => Message.Clean(card.GetCardText(
                cardStatistics,
                managers?.GetSaveManager(),
                includeCurrentTraitEffectText: true)));
            if (!Message.ShouldAdd(text))
            {
                return null;
            }

            return Message.Raw(text);
        }

        internal static void AddUpgradeSummary(CardState card, List<Message> parts, bool includeAbilityDescriptions, bool includeStatusTooltips)
        {
            if (card == null)
            {
                return;
            }

            AddUpgradeStates(parts, card.GetCardStateModifiers()?.GetCardUpgrades(), card, includeAbilityDescriptions, includeStatusTooltips);
            AddUpgradeStates(parts, card.GetTemporaryCardStateModifiers()?.GetCardUpgrades(), card, includeAbilityDescriptions, includeStatusTooltips);
        }

        private static void AddUpgradeStates(List<Message> parts, List<CardUpgradeState> upgrades, CardState sourceCard, bool includeAbilityDescriptions, bool includeStatusTooltips)
        {
            if (upgrades == null)
            {
                return;
            }

            for (int i = 0; i < upgrades.Count; i++)
            {
                AddUpgradeState(parts, upgrades[i], sourceCard, includeAbilityDescriptions, includeStatusTooltips);
            }
        }

        private static void AddUpgradeState(List<Message> parts, CardUpgradeState upgrade, CardState sourceCard, bool includeAbilityDescriptions, bool includeStatusTooltips)
        {
            if (upgrade == null)
            {
                return;
            }

            List<Message> details = new List<Message>();
            if (includeAbilityDescriptions)
            {
                AddUnitAbilityUpgradeDescription(details, upgrade.GetUnitAbilityUpgrade(), sourceCard);
            }

            AddAmount(details, "CARD_UPGRADE.ATTACK", upgrade.GetAttackDamage());
            AddAmount(details, "CARD_UPGRADE.HEALTH", upgrade.GetAdditionalHP());
            AddAmount(details, "CARD_UPGRADE.UNHEALED_HEALTH", upgrade.GetAdditionalUnhealedHP());
            AddAmount(details, "CARD_UPGRADE.COST", upgrade.GetCostReduction());
            AddAmount(details, "CARD_UPGRADE.X_COST", upgrade.GetXCostReduction());
            AddAmount(details, "CARD_UPGRADE.HEAL", upgrade.GetAdditionalHeal());
            AddAmount(details, "CARD_UPGRADE.SIZE", upgrade.GetAdditionalSize());
            AddAmount(details, "CARD_UPGRADE.EQUIPMENT", upgrade.GetAdditionalEquipmentLimit());
            AddAmount(details, "CARD_UPGRADE.UPGRADE_SLOTS", upgrade.GetAdditionalUpgradeSlotCount());
            AddStatusUpgradeSummary(details, upgrade.GetStatusEffectUpgrades(), includeStatusTooltips);
            if (includeStatusTooltips)
            {
                AddCharacterTriggerUpgradeSummary(details, upgrade.GetTriggerUpgrades());
            }

            for (int i = 0; i < details.Count; i++)
            {
                MessageList.Add(parts, details[i]);
            }
            AddUpgradeTitle(parts, upgrade);
        }

        private static void AddUpgradeTitle(List<Message> parts, CardUpgradeState upgrade)
        {
            string key = upgrade?.GetUpgradeTitleKey();
            if (string.IsNullOrWhiteSpace(key) || !key.HasTranslation())
            {
                return;
            }

            MessageList.Add(parts, Message.FromText(AccessibilityText.LocalizeTerm(key)));
        }

        private static void AddCharacterTriggerUpgradeSummary(List<Message> parts, List<CharacterTriggerData> triggers)
        {
            if (triggers == null)
            {
                return;
            }

            StatusEffectManager manager = StatusEffectManager.Instance;
            if (manager == null)
            {
                return;
            }

            for (int i = 0; i < triggers.Count; i++)
            {
                CharacterTriggerData trigger = triggers[i];
                if (trigger == null || CharacterTriggerData.IsSpecialAbilityTrigger(trigger.GetTrigger()))
                {
                    continue;
                }

                try
                {
                    TooltipContent tooltip = AccessibilityLocalizationScope.Run(() =>
                        TooltipGenerator.GetTriggerDefinitionTooltipContent(trigger, i, "accessibilitycardupgrade", manager));
                    MessageList.AddTooltip(parts, tooltip, bodyFirst: true);
                }
                catch (Exception ex)
                {
                    Log.Info("[AccessibilityMod] Failed to describe card upgrade character trigger: " + ex);
                }
            }
        }

        private static void AddStatusUpgradeSummary(List<Message> parts, List<StatusEffectStackData> statuses, bool includeStatusTooltips)
        {
            if (statuses == null)
            {
                return;
            }

            for (int i = 0; i < statuses.Count; i++)
            {
                StatusEffectStackData status = statuses[i];
                AddStatusName(parts, status);
                if (includeStatusTooltips && status != null)
                {
                    AddStatusTooltip(parts, status.statusId);
                }
            }
        }

        private static void AddStatusName(List<Message> parts, StatusEffectStackData status)
        {
            if (status == null || string.IsNullOrWhiteSpace(status.statusId))
            {
                return;
            }

            string name = GameLocStrings.StatusName(status.statusId, status.count, showStacks: false)?.Resolve();
            parts.Add(status.count > 1
                ? Message.Localized("ui", "CARD_UPGRADE.STATUS", new { status = name, amount = status.count })
                : Message.Localized("ui", "CARD_UPGRADE.STATUS_ONLY", new { status = name }));
        }

        private static void AddAmount(List<Message> parts, string key, int amount)
        {
            if (amount != 0)
            {
                parts.Add(Message.Localized("ui", key, new { amount }));
            }
        }

        private static void AddStatusTooltip(List<Message> parts, string statusId)
        {
            StatusEffectManager manager = StatusEffectManager.Instance;
            if (manager == null || string.IsNullOrWhiteSpace(statusId))
            {
                return;
            }

            try
            {
                string title = string.Empty;
                string body = string.Empty;
                AccessibilityLocalizationScope.Run(() =>
                    TooltipUI.GetStatusEffectTooltipContents(statusId, manager, out title, out body));
                MessageList.AddTitleBody(parts, title, body, statusId, bodyFirst: false);
            }
            catch (Exception ex)
            {
                Log.Info("[AccessibilityMod] Failed to describe card upgrade status effect " + statusId + ": " + ex);
            }
        }

        private static void AddUnitAbilityUpgradeDescription(List<Message> parts, CardData ability, CardState sourceCard)
        {
            if (ability == null)
            {
                return;
            }

            List<string> descriptions = new List<string>();
            AddUnitAbilityTooltipDescription(ability, sourceCard, descriptions);
            AddHealerEffectDescription(ability, descriptions);
            AddAbilityCardDescriptions(ability, descriptions);
            for (int i = descriptions.Count - 1; i >= 0; i--)
            {
                MessageList.Add(parts, Message.FromText(descriptions[i]));
            }
        }

        private static void AddUnitAbilityTooltipDescription(CardData ability, CardState sourceCard, List<string> descriptions)
        {
            if (ability == null)
            {
                return;
            }

            try
            {
                SaveManager saveManager = AllGameManagers.Instance.OrNull()?.GetSaveManager();
                CardState abilityState = new CardState(ability, saveManager);
                string body = AccessibilityLocalizationScope.Run(() =>
                    TooltipUI.AppendUnitAbilityDescToStatusEffectBody(string.Empty, abilityState, null, sourceCard));
                AddDescription(descriptions, body);
            }
            catch (Exception ex)
            {
                Log.Info("[AccessibilityMod] Failed to add unit ability tooltip description: " + ex);
            }
        }

        private static void AddHealerEffectDescription(CardData ability, List<string> descriptions)
        {
            if (ability == null)
            {
                return;
            }

            try
            {
                CardState abilityState = new CardState(ability, AllGameManagers.Instance.OrNull()?.GetSaveManager());
                CharacterData characterData = abilityState.GetSpawnCharacterData();
                AddHealerEffectDescription(characterData?.GetHealerEffect(), descriptions);
            }
            catch (Exception ex)
            {
                Log.Info("[AccessibilityMod] Failed to add unit ability healer description: " + ex);
            }
        }

        private static void AddHealerEffectDescription(CardEffectData healerEffect, List<string> descriptions)
        {
            if (healerEffect == null)
            {
                return;
            }

            Message body = Message.FromText(AccessibilityText.LocalizeTerm(
                "CharacterAbility_CardEffectHeal_TooltipText",
                new LocalizedIntegers(healerEffect.GetParamInt())));
            if (body != null)
            {
                AddDescription(descriptions, body.Resolve());
            }
        }

        private static void AddAbilityCardDescriptions(CardData ability, List<string> descriptions)
        {
            if (ability == null)
            {
                return;
            }

            try
            {
                SaveManager saveManager = AllGameManagers.Instance.OrNull()?.GetSaveManager();
                CardState abilityState = new CardState(ability, saveManager);
                List<CardEffectState> effects = abilityState.GetEffectStates();
                for (int i = 0; i < effects.Count; i++)
                {
                    IEnumerable<TooltipContent> effectTooltips = AccessibilityLocalizationScope.Run(() =>
                        CardTooltipHelper.GetCardEffectTooltipContents(effects[i], TooltipUI.TitleStyle.Normal, saveManager));
                    foreach (TooltipContent tooltip in effectTooltips)
                    {
                        AddDescription(descriptions, tooltip.body);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Info("[AccessibilityMod] Failed to add unit ability card description: " + ex);
            }
        }

        private static void AddDescription(List<string> descriptions, string description)
        {
            string body = Message.Clean(description);
            if (string.IsNullOrWhiteSpace(body) || descriptions.Contains(body))
            {
                return;
            }

            descriptions.Add(body);
        }
    }
}

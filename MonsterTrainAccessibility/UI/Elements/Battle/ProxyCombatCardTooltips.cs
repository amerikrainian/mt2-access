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
        internal static List<TooltipContent> CollectTooltips(CardState card, SaveManager saveManager)
        {
            List<TooltipContent> tooltips = new List<TooltipContent>();
            if (card == null)
            {
                return tooltips;
            }

            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            CollectCardEffectTooltips(card, tooltips, seen, saveManager);
            CollectCardTraitTooltips(card, tooltips, seen);
            CollectCardTriggerTooltips(card, tooltips, seen, saveManager);
            CollectAppliedCardUpgradeTooltips(card, tooltips, seen, saveManager);
            CollectCardStatusEffectTooltips(card, tooltips, seen);
            CollectGraftedEquipmentTooltips(card, tooltips, seen, saveManager);
            CollectSpawnedCharacterTooltips(card, tooltips, seen, saveManager);
            return tooltips;
        }

        private static void CollectCardTraitTooltips(CardState card, List<TooltipContent> tooltips, HashSet<string> seen)
        {
            List<CardTraitState> traits = card.GetTraitStates();
            if (traits == null)
            {
                return;
            }

            for (int i = 0; i < traits.Count; i++)
            {
                CardTraitState trait = traits[i];
                if (trait == null)
                {
                    continue;
                }

                if (!trait.GetTooltipSuppressed())
                {
                    AddTooltip(tooltips, seen, CreateCardTraitTooltip(trait));
                }

                CollectAdditionalTraitTooltips(trait, tooltips, seen);
                CollectTraitUpgradeTooltips(trait, tooltips, seen);
            }
        }

        private static TooltipContent CreateCardTraitTooltip(CardTraitState trait)
        {
            if (trait == null)
            {
                return default(TooltipContent);
            }

            string typeName = trait.GetType().Name;
            string title = FirstText(
                trait.GetCardTooltipTitle(),
                LocalizeIfPresent(CardTraitData.GetTraitCardTextLocalizationKey(typeName)));
            string body = FirstText(
                trait.GetCardTooltipText(),
                LocalizeIfPresent(
                    CardTraitData.GetTraitTooltipTextLocalizationKey(typeName),
                    new CardEffectLocalizationContext(trait.GetCardTraitData())));

            return new TooltipContent(
                title,
                body,
                TooltipDesigner.TooltipDesignType.Default,
                trait.GetCardTooltipId());
        }

        private static void CollectAdditionalTraitTooltips(CardTraitState trait, List<TooltipContent> tooltips, HashSet<string> seen)
        {
            if (trait == null)
            {
                return;
            }

            try
            {
                List<TooltipContent> additionalTooltips = new List<TooltipContent>();
                AccessibilityLocalizationScope.Run(() => trait.CreateAdditionalTooltips(additionalTooltips));
                for (int i = 0; i < additionalTooltips.Count; i++)
                {
                    AddTooltip(tooltips, seen, additionalTooltips[i]);
                }
            }
            catch (Exception ex)
            {
                Log.Info("[AccessibilityMod] Failed to collect additional card trait tooltip: " + ex);
            }
        }

        private static void CollectTraitUpgradeTooltips(CardTraitState trait, List<TooltipContent> tooltips, HashSet<string> seen)
        {
            if (trait == null || trait.GetStatusEffectTooltipsSuppressed())
            {
                return;
            }

            IReadOnlyList<CardUpgradeData> upgrades = trait.GetUpgradeData();
            if (upgrades == null || upgrades.Count == 0)
            {
                return;
            }

            for (int i = 0; i < upgrades.Count; i++)
            {
                CardUpgradeData upgrade = upgrades[i];
                if (upgrade == null)
                {
                    continue;
                }

                try
                {
                    IEnumerable<TooltipContent> upgradeTooltips = AccessibilityLocalizationScope.Run(() =>
                        CardTooltipHelper.GenerateTooltipContentForInternalUpgrade(upgrade));
                    foreach (TooltipContent tooltip in upgradeTooltips)
                    {
                        AddTooltip(tooltips, seen, tooltip);
                    }
                }
                catch (Exception ex)
                {
                    Log.Info("[AccessibilityMod] Failed to collect card trait upgrade tooltip: " + ex);
                }
            }
        }

        private static void CollectCardEffectTooltips(CardState card, List<TooltipContent> tooltips, HashSet<string> seen, SaveManager saveManager)
        {
            List<CardEffectState> effects = card.GetEffectStates();
            if (effects == null)
            {
                return;
            }

            CharacterData spawnedCharacter = card.GetSpawnCharacterData();
            for (int i = 0; i < effects.Count; i++)
            {
                CardEffectState effect = effects[i];
                if (effect == null)
                {
                    continue;
                }

                if (spawnedCharacter != null && ReferenceEquals(effect.GetParamCharacterData(), spawnedCharacter))
                {
                    if (card.GetCardType() != CardType.Monster)
                    {
                        AddTooltip(tooltips, seen, CreateSummonedUnitTooltip(spawnedCharacter, saveManager));
                    }
                    continue;
                }

                try
                {
                    IEnumerable<TooltipContent> effectTooltips = AccessibilityLocalizationScope.Run(() =>
                        CardTooltipHelper.GetCardEffectTooltipContents(effect, TooltipUI.TitleStyle.Normal, saveManager));
                    foreach (TooltipContent tooltip in effectTooltips)
                    {
                        AddTooltip(tooltips, seen, tooltip);
                    }
                }
                catch (Exception ex)
                {
                    Log.Info("[AccessibilityMod] Failed to collect card effect tooltip: " + ex);
                }

                CollectSetUnitAbilityTooltips(effect, card, tooltips, seen, saveManager);
                CollectCardUpgradeTooltips(effect.GetParamCardUpgradeData(), card, tooltips, seen, saveManager);
            }
        }

        private static void CollectSetUnitAbilityTooltips(CardEffectState effect, CardState sourceCard, List<TooltipContent> tooltips, HashSet<string> seen, SaveManager saveManager)
        {
            if (effect == null || !(effect.GetCardEffect() is CardEffectSetUnitAbility))
            {
                return;
            }

            CardData ability = effect.GetParamCardData();
            if (ability == null)
            {
                return;
            }

            try
            {
                CardState abilityState = UnitOrRoomAbilityCardStateCache.Instance.Get(ability, saveManager?.RelicManager, saveManager);
                AddTooltip(tooltips, seen, CreateUnitAbilityTooltip(ability, abilityState, sourceCard));
                CollectCardEffectTooltips(abilityState, tooltips, seen, saveManager);
                CollectCardTraitTooltips(abilityState, tooltips, seen);
                CollectCardTriggerTooltips(abilityState, tooltips, seen, saveManager);
                CollectCardStatusEffectTooltips(abilityState, tooltips, seen);
            }
            catch (Exception ex)
            {
                Log.Info("[AccessibilityMod] Failed to collect set unit ability tooltip " + ability.GetName() + ": " + ex);
            }
        }

        private static void CollectCardUpgradeTooltips(CardUpgradeData upgrade, CardState sourceCard, List<TooltipContent> tooltips, HashSet<string> seen, SaveManager saveManager)
        {
            if (upgrade == null)
            {
                return;
            }

            CollectUpgradeAbilityTooltips(ResolveUnitAbilityUpgrade(upgrade, sourceCard), sourceCard, tooltips, seen, saveManager);
            CollectUpgradeAbilityTooltips(upgrade.GetRoomAbilityUpgrade(), sourceCard, tooltips, seen, saveManager);

            List<CardTriggerEffectData> triggerUpgrades = upgrade.GetCardTriggerUpgrades();
            if (triggerUpgrades != null)
            {
                for (int i = 0; i < triggerUpgrades.Count; i++)
                {
                    CollectCardTriggerUpgradeTooltips(triggerUpgrades[i], sourceCard, tooltips, seen, saveManager);
                }
            }

            List<CharacterTriggerData> characterTriggers = upgrade.GetCharacterTriggerUpgrades();
            if (characterTriggers != null)
            {
                for (int i = 0; i < characterTriggers.Count; i++)
                {
                    CollectCharacterTriggerUpgradeTooltips(characterTriggers[i], i, tooltips, seen, saveManager);
                }
            }
        }

        private static CardData ResolveUnitAbilityUpgrade(CardUpgradeData upgrade, CardState sourceCard)
        {
            return upgrade != null ? ResolveUnitAbilityUpgrade(upgrade.GetUnitAbilityUpgrade(), sourceCard) : null;
        }

        private static CardData ResolveUnitAbilityUpgrade(CardData ability, CardState sourceCard)
        {
            if (ability == null || sourceCard == null || !sourceCard.IsSpawnerCard())
            {
                return ability;
            }

            CharacterData characterData = sourceCard.GetSpawnCharacterData();
            return characterData != null
                ? CharacterState.GetInitialUnitAbility(characterData, sourceCard, out bool _)
                : ability;
        }

        private static void CollectAppliedCardUpgradeTooltips(CardState card, List<TooltipContent> tooltips, HashSet<string> seen, SaveManager saveManager)
        {
            if (card == null)
            {
                return;
            }

            CollectAppliedCardUpgradeTooltips(card.GetCardStateModifiers()?.GetCardUpgrades(), card, tooltips, seen, saveManager);
            CollectAppliedCardUpgradeTooltips(card.GetTemporaryCardStateModifiers()?.GetCardUpgrades(), card, tooltips, seen, saveManager);
        }

        private static void CollectAppliedCardUpgradeTooltips(List<CardUpgradeState> upgrades, CardState sourceCard, List<TooltipContent> tooltips, HashSet<string> seen, SaveManager saveManager)
        {
            if (upgrades == null)
            {
                return;
            }

            for (int i = 0; i < upgrades.Count; i++)
            {
                CollectAppliedCardUpgradeTooltips(upgrades[i], sourceCard, tooltips, seen, saveManager);
            }
        }

        private static void CollectAppliedCardUpgradeTooltips(CardUpgradeState upgrade, CardState sourceCard, List<TooltipContent> tooltips, HashSet<string> seen, SaveManager saveManager)
        {
            if (upgrade == null)
            {
                return;
            }

            CollectStatusEffectStackTooltips(upgrade.GetStatusEffectUpgrades(), tooltips, seen);
            CollectUpgradeAbilityTooltips(ResolveUnitAbilityUpgrade(upgrade.GetUnitAbilityUpgrade(), sourceCard), sourceCard, tooltips, seen, saveManager);
            CollectUpgradeAbilityTooltips(upgrade.GetRoomAbilityUpgrade(), sourceCard, tooltips, seen, saveManager);

            List<CardTriggerEffectData> cardTriggers = upgrade.GetCardTriggerUpgrades();
            if (cardTriggers != null)
            {
                for (int i = 0; i < cardTriggers.Count; i++)
                {
                    CollectCardTriggerUpgradeTooltips(cardTriggers[i], sourceCard, tooltips, seen, saveManager);
                }
            }

            List<CharacterTriggerData> characterTriggers = upgrade.GetTriggerUpgrades();
            if (characterTriggers != null)
            {
                for (int i = 0; i < characterTriggers.Count; i++)
                {
                    CollectCharacterTriggerUpgradeTooltips(characterTriggers[i], i, tooltips, seen, saveManager);
                }
            }
        }

        private static void CollectStatusEffectStackTooltips(List<StatusEffectStackData> statuses, List<TooltipContent> tooltips, HashSet<string> seen)
        {
            StatusEffectManager manager = StatusEffectManager.Instance;
            if (manager == null || statuses == null)
            {
                return;
            }

            for (int i = 0; i < statuses.Count; i++)
            {
                StatusEffectStackData status = statuses[i];
                if (status == null || string.IsNullOrWhiteSpace(status.statusId))
                {
                    continue;
                }

                try
                {
                    string title = string.Empty;
                    string body = string.Empty;
                    AccessibilityLocalizationScope.Run(() =>
                        TooltipUI.GetStatusEffectTooltipContents(status.statusId, manager, out title, out body));
                    AddTooltip(tooltips, seen, new TooltipContent(title, body, TooltipDesigner.TooltipDesignType.Default, status.statusId));
                }
                catch (Exception ex)
                {
                    Log.Info("[AccessibilityMod] Failed to collect card upgrade status effect " + status.statusId + ": " + ex);
                }
            }
        }

        private static void CollectUpgradeAbilityTooltips(CardData ability, CardState sourceCard, List<TooltipContent> tooltips, HashSet<string> seen, SaveManager saveManager)
        {
            if (ability == null)
            {
                return;
            }

            try
            {
                CardState abilityState = new CardState(ability, saveManager);
                AddTooltip(tooltips, seen, CreateUnitAbilityTooltip(ability, abilityState, sourceCard));
                CollectCardEffectTooltips(abilityState, tooltips, seen, saveManager);
                CollectCardTraitTooltips(abilityState, tooltips, seen);
                CollectCardTriggerTooltips(abilityState, tooltips, seen, saveManager);
                CollectCardStatusEffectTooltips(abilityState, tooltips, seen);
            }
            catch (Exception ex)
            {
                Log.Info("[AccessibilityMod] Failed to collect card upgrade ability tooltip: " + ex);
            }
        }

        private static void CollectCardTriggerUpgradeTooltips(CardTriggerEffectData triggerData, CardState sourceCard, List<TooltipContent> tooltips, HashSet<string> seen, SaveManager saveManager)
        {
            if (triggerData == null || triggerData.GetTriggerKeywordTooltipSuppressed())
            {
                return;
            }

            try
            {
                CardTriggerEffectState triggerState = new CardTriggerEffectState();
                triggerState.Setup(triggerData, sourceCard, saveManager);
                IEnumerable<TooltipContent> triggerTooltips = AccessibilityLocalizationScope.Run(() =>
                    CardTooltipHelper.GetCardTriggerTooltipContents(triggerState));
                foreach (TooltipContent tooltip in triggerTooltips)
                {
                    AddTooltip(tooltips, seen, tooltip);
                }

                List<CardEffectData> effects = triggerData.GetCardEffects();
                for (int i = 0; i < effects.Count; i++)
                {
                    IEnumerable<TooltipContent> effectTooltips = AccessibilityLocalizationScope.Run(() =>
                        CardTooltipHelper.GetCardEffectTooltipContents(effects[i], saveManager));
                    foreach (TooltipContent tooltip in effectTooltips)
                    {
                        AddTooltip(tooltips, seen, tooltip);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Info("[AccessibilityMod] Failed to collect card upgrade trigger tooltip: " + ex);
            }
        }

        private static void CollectCharacterTriggerUpgradeTooltips(CharacterTriggerData trigger, int index, List<TooltipContent> tooltips, HashSet<string> seen, SaveManager saveManager)
        {
            if (trigger == null)
            {
                return;
            }

            if (trigger.GetHideVisualAndIgnoreSilence())
            {
                if (trigger.AllowAdditionalTooltipsWhenVisualIsHidden)
                {
                    CollectAdditionalTriggerEffectTooltips(trigger, tooltips, seen);
                }
                return;
            }

            StatusEffectManager manager = StatusEffectManager.Instance;
            if (manager == null)
            {
                return;
            }

            try
            {
                TooltipContent tooltip = AccessibilityLocalizationScope.Run(() =>
                    TooltipGenerator.GetTriggerDefinitionTooltipContent(trigger, index, "accessibilitycard", manager));
                AddTooltip(tooltips, seen, tooltip);
            }
            catch (Exception ex)
            {
                Log.Info("[AccessibilityMod] Failed to collect character trigger upgrade tooltip: " + ex);
            }

            CollectCharacterTriggerEffectTooltips(trigger, tooltips, seen, saveManager);
        }

        private static void CollectCharacterTriggerEffectTooltips(CharacterTriggerData trigger, List<TooltipContent> tooltips, HashSet<string> seen, SaveManager saveManager)
        {
            IReadOnlyList<CardEffectData> effects = trigger.GetEffects();
            if (effects == null)
            {
                return;
            }

            for (int i = 0; i < effects.Count; i++)
            {
                CardEffectData effect = effects[i];
                if (effect == null)
                {
                    continue;
                }

                try
                {
                    IEnumerable<TooltipContent> effectTooltips = AccessibilityLocalizationScope.Run(() =>
                        CardTooltipHelper.GetCardEffectTooltipContents(effect, saveManager));
                    foreach (TooltipContent tooltip in effectTooltips)
                    {
                        AddTooltip(tooltips, seen, tooltip);
                    }
                }
                catch (Exception ex)
                {
                    Log.Info("[AccessibilityMod] Failed to collect character trigger effect tooltip: " + ex);
                }
            }
        }

        private static void CollectAdditionalTriggerEffectTooltips(CharacterTriggerData trigger, List<TooltipContent> tooltips, HashSet<string> seen)
        {
            IReadOnlyList<CardEffectData> effects = trigger.GetEffects();
            if (effects == null)
            {
                return;
            }

            for (int i = 0; i < effects.Count; i++)
            {
                CardEffectData effect = effects[i];
                if (effect == null)
                {
                    continue;
                }

                AdditionalTooltipData[] additionalTooltips = effect.GetAdditionalTooltips();
                for (int j = 0; j < additionalTooltips.Length; j++)
                {
                    try
                    {
                        TooltipContent tooltip;
                        if (CardTooltipHelper.TryGetAdditionalTooltipContent(additionalTooltips[j], out tooltip))
                        {
                            AddTooltip(tooltips, seen, tooltip);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Info("[AccessibilityMod] Failed to collect hidden character trigger effect tooltip: " + ex);
                    }
                }
            }
        }

        private static void CollectCardTriggerTooltips(CardState card, List<TooltipContent> tooltips, HashSet<string> seen, SaveManager saveManager)
        {
            List<CardTriggerEffectState> triggers = card.GetTriggers();
            if (triggers == null)
            {
                return;
            }

            for (int i = 0; i < triggers.Count; i++)
            {
                CardTriggerEffectState trigger = triggers[i];
                if (trigger == null || trigger.GetTriggerKeywordTooltipSuppressed())
                {
                    continue;
                }

                bool skipTriggerDefinition = card.GetCardType() == CardType.Spell && trigger.GetTrigger() == CardTriggerType.OnCast;
                if (!skipTriggerDefinition)
                {
                    try
                    {
                        IEnumerable<TooltipContent> triggerTooltips = AccessibilityLocalizationScope.Run(() =>
                            CardTooltipHelper.GetCardTriggerTooltipContents(trigger));
                        foreach (TooltipContent tooltip in triggerTooltips)
                        {
                            AddTooltip(tooltips, seen, tooltip);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Info("[AccessibilityMod] Failed to collect card trigger tooltip: " + ex);
                    }
                }

                try
                {
                    List<CardEffectState> effects = trigger.GetCardEffectParams();
                    for (int effectIndex = 0; effectIndex < effects.Count; effectIndex++)
                    {
                        IEnumerable<TooltipContent> effectTooltips = AccessibilityLocalizationScope.Run(() =>
                            CardTooltipHelper.GetCardEffectTooltipContents(effects[effectIndex], TooltipUI.TitleStyle.Normal, saveManager));
                        foreach (TooltipContent tooltip in effectTooltips)
                        {
                            AddTooltip(tooltips, seen, tooltip);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Info("[AccessibilityMod] Failed to collect card trigger effect tooltip: " + ex);
                }
            }
        }

        private static void CollectCardStatusEffectTooltips(CardState card, List<TooltipContent> tooltips, HashSet<string> seen)
        {
            StatusEffectManager manager = StatusEffectManager.Instance;
            if (manager == null)
            {
                return;
            }

            List<StatusEffectStackData> effects = new List<StatusEffectStackData>();
            if (!card.TryGetStatusEffects(effects))
            {
                return;
            }

            for (int i = 0; i < effects.Count; i++)
            {
                StatusEffectStackData effect = effects[i];
                if (effect == null || string.IsNullOrWhiteSpace(effect.statusId))
                {
                    continue;
                }

                try
                {
                    string title = string.Empty;
                    string body = string.Empty;
                    AccessibilityLocalizationScope.Run(() =>
                        TooltipUI.GetStatusEffectTooltipContents(effect.statusId, manager, out title, out body));
                    AddTooltip(tooltips, seen, new TooltipContent(title, body, TooltipDesigner.TooltipDesignType.Default, effect.statusId));
                }
                catch (Exception ex)
                {
                    Log.Info("[AccessibilityMod] Failed to collect card status effect " + effect.statusId + ": " + ex);
                }
            }
        }

        private static void CollectGraftedEquipmentTooltips(CardState card, List<TooltipContent> tooltips, HashSet<string> seen, SaveManager saveManager)
        {
            if (card == null || card.IsGraftSevered())
            {
                return;
            }

            try
            {
                if (!card.TryGetGraftedEquipment(out CardState graftedEquipment, saveManager) || graftedEquipment == null)
                {
                    return;
                }

                AddStatusEffectTooltip("unit_grafted_equipment", tooltips, seen, "grafted_equipment_status");
                string body = AccessibilityLocalizationScope.Run(() =>
                    Message.Clean(TooltipUI.GetGraftedEquipmentDescription(graftedEquipment, card)));
                AddTooltip(
                    tooltips,
                    seen,
                    new TooltipContent(
                        graftedEquipment.GetTitle(),
                        Message.ShouldAdd(body) ? body : null,
                        TooltipDesigner.TooltipDesignType.Default,
                        "grafted_equipment:" + graftedEquipment.GetCardDataID()));

                CollectCardEffectTooltips(graftedEquipment, tooltips, seen, saveManager);
                CollectCardTraitTooltips(graftedEquipment, tooltips, seen);
                CollectCardTriggerTooltips(graftedEquipment, tooltips, seen, saveManager);
                CollectAppliedCardUpgradeTooltips(graftedEquipment, tooltips, seen, saveManager);
                CollectCardStatusEffectTooltips(graftedEquipment, tooltips, seen);
            }
            catch (Exception ex)
            {
                Log.Info("[AccessibilityMod] Failed to collect grafted equipment tooltip: " + ex);
            }
        }

        private static void AddStatusEffectTooltip(string statusId, List<TooltipContent> tooltips, HashSet<string> seen, string dedupeKey)
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
                AddTooltip(tooltips, seen, new TooltipContent(title, body, TooltipDesigner.TooltipDesignType.Default, dedupeKey ?? statusId));
            }
            catch (Exception ex)
            {
                Log.Info("[AccessibilityMod] Failed to collect status effect " + statusId + ": " + ex);
            }
        }

        private static void CollectSpawnedCharacterTooltips(CardState card, List<TooltipContent> tooltips, HashSet<string> seen, SaveManager saveManager)
        {
            CharacterData characterData = card.GetSpawnCharacterData();
            if (characterData == null)
            {
                return;
            }

            CollectCharacterAbilityTooltips(characterData, tooltips, seen, saveManager);
            CollectCharacterTriggerTooltips(characterData, tooltips, seen, saveManager);
            CollectCardStatusEffectTooltips(card, tooltips, seen);
        }

        private static TooltipContent CreateSummonedUnitTooltip(CharacterData characterData, SaveManager saveManager)
        {
            if (characterData == null)
            {
                return default(TooltipContent);
            }

            try
            {
                string body = BuildSummonedUnitTooltipBody(characterData, saveManager);
                return new TooltipContent(
                    AccessibilityText.LocalizeTerm("CardEffectSpawnMonster_TooltipTitle"),
                    Message.ShouldAdd(body) ? body : null,
                    TooltipDesigner.TooltipDesignType.Default,
                    "summoned_unit:" + characterData.GetID());
            }
            catch (Exception ex)
            {
                Log.Info("[AccessibilityMod] Failed to create summoned unit tooltip " + characterData.GetName() + ": " + ex);
                return default(TooltipContent);
            }
        }

        private static string BuildSummonedUnitTooltipBody(CharacterData characterData, SaveManager saveManager)
        {
            string details = AccessibilityLocalizationScope.Run(() =>
                Message.Clean(CardTooltipHelper.GetTooltipContentForGeneratedCharacter(
                    characterData,
                    saveManager,
                    includeName: false)));
            List<Message> header = new List<Message>();
            MessageList.Add(header, Message.FromText(characterData.GetName()));
            int size = characterData.GetSize();
            if (size > 0)
            {
                MessageList.Add(header, Message.Localized("ui", "CARD_STATS.SIZE", new { size }));
            }

            Message summary = header.Count > 0 ? Message.Join(", ", header) : null;
            if (!Message.ShouldAdd(details))
            {
                return summary?.Resolve();
            }

            return summary != null
                ? Message.Join(". ", summary, Message.Raw(details)).Resolve()
                : details;
        }

        private static void CollectCharacterAbilityTooltips(CharacterData characterData, List<TooltipContent> tooltips, HashSet<string> seen, SaveManager saveManager)
        {
            AddTooltip(tooltips, seen, CreateHealerEffectTooltip(characterData.GetHealerEffect()));

            CardData ability = characterData.GetUnitAbilityCardData();
            if (ability == null)
            {
                return;
            }

            try
            {
                CardState abilityState = new CardState(ability, saveManager);
                AddTooltip(tooltips, seen, CreateUnitAbilityTooltip(ability, abilityState, null));
                CollectCardEffectTooltips(abilityState, tooltips, seen, saveManager);
                CollectCardTraitTooltips(abilityState, tooltips, seen);
                CollectCardTriggerTooltips(abilityState, tooltips, seen, saveManager);
                CollectCardStatusEffectTooltips(abilityState, tooltips, seen);
            }
            catch (Exception ex)
            {
                Log.Info("[AccessibilityMod] Failed to collect spawned character ability tooltips: " + ex);
            }
        }

        private static void CollectCharacterTriggerTooltips(CharacterData characterData, List<TooltipContent> tooltips, HashSet<string> seen, SaveManager saveManager)
        {
            StatusEffectManager manager = StatusEffectManager.Instance;
            if (manager == null)
            {
                return;
            }

            IReadOnlyList<CharacterTriggerData> triggers = characterData.GetTriggers();
            if (triggers == null)
            {
                return;
            }

            for (int i = 0; i < triggers.Count; i++)
            {
                CharacterTriggerData trigger = triggers[i];
                if (trigger == null ||
                    CharacterTriggerData.IsSpecialAbilityTrigger(trigger.GetTrigger()))
                {
                    continue;
                }

                try
                {
                    if (CharacterTriggerData.ShouldDisplayOnCharacterTooltips(trigger.GetTrigger()))
                    {
                        TooltipContent definitionTooltip = AccessibilityLocalizationScope.Run(() =>
                            TooltipGenerator.GetTriggerDefinitionTooltipContent(trigger, i, "accessibilitycard", manager));
                        AddTooltip(tooltips, seen, definitionTooltip);
                    }

                    IReadOnlyList<CardEffectData> triggerEffects = trigger.GetEffects();
                    for (int effectIndex = 0; effectIndex < triggerEffects.Count; effectIndex++)
                    {
                        IEnumerable<TooltipContent> effectTooltips = AccessibilityLocalizationScope.Run(() =>
                            CardTooltipHelper.GetCardEffectTooltipContents(triggerEffects[effectIndex], saveManager));
                        foreach (TooltipContent tooltip in effectTooltips)
                        {
                            AddTooltip(tooltips, seen, tooltip);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Info("[AccessibilityMod] Failed to collect spawned character trigger tooltip: " + ex);
                }
            }
        }

        private static TooltipContent CreateUnitAbilityTooltip(CardData ability, CardState abilityState, CardState sourceCard)
        {
            if (ability == null || abilityState == null)
            {
                return default(TooltipContent);
            }

            try
            {
                StatusEffectManager manager = StatusEffectManager.Instance;
                string title = manager != null
                    ? AccessibilityLocalizationScope.Run(() =>
                        TooltipUI.FormatTitleWithIcon(AccessibilityText.LocalizeTerm(ability.GetNameKey()), manager.GetTMPSpriteTag("unit_ability")))
                    : ability.GetName();
                string body = AccessibilityLocalizationScope.Run(() =>
                    TooltipUI.AppendUnitAbilityDescToStatusEffectBody(string.Empty, abilityState, null, sourceCard));
                return new TooltipContent(title, body, TooltipDesigner.TooltipDesignType.Ability, "unit_ability:" + ability.GetID());
            }
            catch (Exception ex)
            {
                Log.Info("[AccessibilityMod] Failed to create unit ability tooltip " + ability.GetName() + ": " + ex);
                return default(TooltipContent);
            }
        }

        private static TooltipContent CreateHealerEffectTooltip(CardEffectData healerEffect)
        {
            if (healerEffect == null)
            {
                return default(TooltipContent);
            }

            try
            {
                StatusEffectManager manager = StatusEffectManager.Instance;
                if (manager == null)
                {
                    return default(TooltipContent);
                }

                string title = AccessibilityLocalizationScope.Run(() =>
                    TooltipUI.FormatTitleWithIcon(
                        AccessibilityText.LocalizeTerm("CharacterAbility_CardEffectHeal_CardText"),
                        manager.GetTMPSpriteTag(healerEffect)));
                string body = AccessibilityText.LocalizeTerm(
                    "CharacterAbility_CardEffectHeal_TooltipText",
                    new LocalizedIntegers(healerEffect.GetParamInt()));
                return new TooltipContent(title, body, TooltipDesigner.TooltipDesignType.Default, healerEffect.GetEffectStateName());
            }
            catch (Exception ex)
            {
                Log.Info("[AccessibilityMod] Failed to create healer effect tooltip: " + ex);
                return default(TooltipContent);
            }
        }

        private static void AddTooltip(List<TooltipContent> tooltips, HashSet<string> seen, TooltipContent tooltip)
        {
            MessageList.TryAddTooltip(tooltips, seen, tooltip);
        }

        private static string FirstText(params string[] values)
        {
            if (values == null)
            {
                return string.Empty;
            }

            for (int i = 0; i < values.Length; i++)
            {
                string value = Message.Clean(values[i]);
                if (Message.ShouldAdd(value))
                {
                    return value;
                }
            }

            return string.Empty;
        }

        private static string LocalizeIfPresent(string key)
        {
            return !string.IsNullOrWhiteSpace(key) && key.HasTranslation()
                ? AccessibilityText.LocalizeTerm(key)
                : string.Empty;
        }

        private static string LocalizeIfPresent(string key, ILocalizationParameterContext context)
        {
            return !string.IsNullOrWhiteSpace(key) && key.HasTranslation()
                ? AccessibilityText.LocalizeTerm(key, context)
                : string.Empty;
        }
    }
}

using System;
using System.Collections.Generic;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Presentation;
using MonsterTrainAccessibility.UI;
using MonsterTrainAccessibility.Util;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyCardUpgrade : ProxyElement
    {
        private readonly CardUpgradeData _upgrade;

        public ProxyCardUpgrade(CardUpgradeData upgrade)
            : base(null)
        {
            _upgrade = upgrade;
        }

        public override bool IsVisible => _upgrade != null;

        public override Message GetLabel()
        {
            return PresentationRenderer.Label(PhaseRegistry.CardUpgrades.Build(_upgrade));
        }

        public override Message GetTooltip()
        {
            return PresentationRenderer.FocusTooltip(PhaseRegistry.CardUpgrades.Build(_upgrade));
        }

        internal static Message Label(CardUpgradeData upgrade)
        {
            return upgrade != null
                ? Message.FromText(AccessibilityText.LocalizeTerm(upgrade.GetUpgradeDescriptionKey(), new CardEffectLocalizationContext(upgrade)))
                : null;
        }

        internal static List<Message> TooltipParts(CardUpgradeData upgrade)
        {
            List<Message> parts = new List<Message>();
            if (upgrade == null)
            {
                return parts;
            }

            CardEffectLocalizationContext context = new CardEffectLocalizationContext(upgrade);
            MessageList.Add(parts, Message.FromText(AccessibilityText.LocalizeTerm(upgrade.GetUpgradeDescriptionKey(), context)));
            int generatedFallbackThreshold = parts.Count;
            AddUpgradeDataStats(parts, upgrade);
            AddUpgradeDataStatusTooltips(parts, upgrade);
            AddUpgradeDataTraitTooltips(parts, upgrade);
            AddInternalUpgradeTooltips(parts, upgrade);
            if (parts.Count == generatedFallbackThreshold)
            {
                AddGeneratedCardText(parts, upgrade);
            }

            return parts;
        }

        private static void AddUpgradeDataStats(List<Message> parts, CardUpgradeData upgrade)
        {
            AddAmount(parts, "CARD_UPGRADE.ATTACK", upgrade.GetBonusDamage());
            AddAmount(parts, "CARD_UPGRADE.HEALTH", upgrade.GetBonusHP());
            AddAmount(parts, "CARD_UPGRADE.UNHEALED_HEALTH", upgrade.GetUnhealedBonusHP());
            AddAmount(parts, "CARD_UPGRADE.COST", upgrade.GetCostReduction());
            AddAmount(parts, "CARD_UPGRADE.X_COST", upgrade.GetXCostReduction());
            AddAmount(parts, "CARD_UPGRADE.HEAL", upgrade.GetBonusHeal());
            AddAmount(parts, "CARD_UPGRADE.SIZE", upgrade.GetBonusSize());
            AddAmount(parts, "CARD_UPGRADE.EQUIPMENT", upgrade.GetBonusEquipment());
            AddAmount(parts, "CARD_UPGRADE.UPGRADE_SLOTS", upgrade.GetBonusUpgradeSlotCount());

            List<StatusEffectStackData> statuses = upgrade.GetStatusEffectUpgrades();
            if (statuses == null)
            {
                return;
            }

            for (int i = 0; i < statuses.Count; i++)
            {
                AddStatusName(parts, statuses[i]);
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

        private static void AddUpgradeDataStatusTooltips(List<Message> parts, CardUpgradeData upgrade)
        {
            StatusEffectManager statusEffectManager = StatusEffectManager.Instance;
            if (statusEffectManager == null)
            {
                return;
            }

            List<StatusEffectStackData> statuses = upgrade.GetStatusEffectUpgrades();
            if (statuses == null)
            {
                return;
            }

            MessageList.Deduper text = new MessageList.Deduper();
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
                        TooltipUI.GetStatusEffectTooltipContents(status.statusId, statusEffectManager, out title, out body));
                    text.AddTitleBody(parts, title, body, status.statusId, bodyFirst: false);
                }
                catch (Exception ex)
                {
                    Log.Info("[AccessibilityMod] Failed to describe merchant enhancer status " + status.statusId + ": " + ex);
                }
            }
        }

        private static void AddUpgradeDataTraitTooltips(List<Message> parts, CardUpgradeData upgrade)
        {
            StatusEffectManager statusEffectManager = StatusEffectManager.Instance;
            if (statusEffectManager == null)
            {
                return;
            }

            List<CardTraitData> traits = upgrade.GetTraitDataUpgrades();
            if (traits == null)
            {
                return;
            }

            MessageList.Deduper text = new MessageList.Deduper();
            for (int i = 0; i < traits.Count; i++)
            {
                CardTraitData trait = traits[i];
                if (trait == null || trait.GetTooltipSuppressed())
                {
                    continue;
                }

                string traitStateName = trait.GetTraitStateName();
                if (!TooltipContainer.TraitSupportedInTooltips(traitStateName))
                {
                    continue;
                }

                try
                {
                    Type traitType = ResolveTraitType(traitStateName);
                    if (traitType == null)
                    {
                        Log.Info("[AccessibilityMod] Failed to describe card upgrade trait " + traitStateName + ": trait type not found.");
                        continue;
                    }

                    CardTraitState state = (CardTraitState)Activator.CreateInstance(traitType);
                    state.Setup(trait, CardState.None);
                    string title = state.GetCardTooltipTitle();
                    if (string.IsNullOrEmpty(title))
                    {
                        title = CardTraitData.GetTraitCardTextLocalizationKey(state.GetType().Name).Localize();
                    }

                    string body = state.GetCardTooltipText();
                    if (string.IsNullOrEmpty(body))
                    {
                        body = CardTraitData.GetTraitTooltipTextLocalizationKey(state.GetType().Name)
                            .Localize(new CardEffectLocalizationContext(state.GetCardTraitData()));
                    }

                    text.AddTitleBody(parts, title, body, traitStateName, bodyFirst: false);

                    List<TooltipContent> additionalTooltips = new List<TooltipContent>();
                    state.CreateAdditionalTooltips(additionalTooltips);
                    for (int j = 0; j < additionalTooltips.Count; j++)
                    {
                        text.AddTooltip(parts, additionalTooltips[j]);
                    }
                }
                catch (Exception ex)
                {
                    Log.Info("[AccessibilityMod] Failed to describe card upgrade trait " + traitStateName + ": " + ex);
                }
            }
        }

        private static Type ResolveTraitType(string traitStateName)
        {
            if (string.IsNullOrWhiteSpace(traitStateName))
            {
                return null;
            }

            Type direct = Type.GetType(traitStateName);
            if (direct != null)
            {
                return direct;
            }

            System.Reflection.Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                Type candidate = assemblies[i].GetType(traitStateName);
                if (candidate != null)
                {
                    return candidate;
                }
            }

            return null;
        }

        private static void AddInternalUpgradeTooltips(List<Message> parts, CardUpgradeData upgrade)
        {
            try
            {
                MessageList.Deduper text = new MessageList.Deduper();
                IEnumerable<TooltipContent> tooltips = AccessibilityLocalizationScope.Run(() =>
                    CardTooltipHelper.GenerateTooltipContentForInternalUpgrade(upgrade));
                foreach (TooltipContent tooltip in tooltips)
                {
                    text.AddTooltip(parts, tooltip);
                }
            }
            catch (Exception ex)
            {
                Log.Info("[AccessibilityMod] Failed to describe internal card upgrade tooltip: " + ex);
            }
        }

        private static void AddGeneratedCardText(List<Message> parts, CardUpgradeData upgrade)
        {
            try
            {
                SaveManager saveManager = AllGameManagers.Instance?.OrNull()?.GetSaveManager();
                RelicManager relicManager = AllGameManagers.Instance?.OrNull()?.GetRelicManager();
                string cardText = string.Empty;
                AccessibilityLocalizationScope.Run(() => upgrade.GetCardText(out cardText, null, relicManager, saveManager));
                string cleaned = Message.Clean(cardText);
                if (ShouldIncludeGeneratedCardText(cleaned))
                {
                    MessageList.Add(parts, Message.FromText(cleaned));
                }
            }
            catch (Exception ex)
            {
                Log.Info("[AccessibilityMod] Failed to describe merchant enhancer card text: " + ex);
            }
        }

        private static bool ShouldIncludeGeneratedCardText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            for (int i = 0; i < text.Length; i++)
            {
                if (char.IsLetter(text[i]))
                {
                    return true;
                }
            }

            return false;
        }
    }
}

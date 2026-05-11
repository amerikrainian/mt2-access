using MonsterTrainAccessibility.Buffers;
using System;
using System.Collections.Generic;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Presentation;
using MonsterTrainAccessibility.Util;
using ShinyShoe;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed partial class ProxyCombatCard : GameObjectElement
    {
        private readonly CardUI _cardUI;
        private readonly CardState _cardState;
        private readonly CardSelectionBehaviour _selectionBehaviour;
        private readonly Func<CardUI, CardState, List<Message>> _bufferBottomParts;
        private readonly Func<CardUI, bool> _isSelected;
        private readonly Func<CardState, Message> _focusSummary;

        public ProxyCombatCard(
            CardUI cardUI,
            IGameUIComponent selectable,
            CardSelectionBehaviour selectionBehaviour = null,
            Func<CardUI, CardState, List<Message>> bufferBottomParts = null,
            Func<CardUI, bool> isSelected = null,
            Func<CardState, Message> focusSummary = null)
            : base(
                selectable,
                typeKey: null,
                label: null)
        {
            _cardUI = cardUI;
            _selectionBehaviour = selectionBehaviour;
            _bufferBottomParts = bufferBottomParts;
            _isSelected = isSelected;
            _focusSummary = focusSummary;
        }

        private ProxyCombatCard(CardState cardState)
            : base((UnityEngine.GameObject)null, typeKey: null, label: null)
        {
            _cardState = cardState;
        }

        public static ProxyCombatCard FromState(CardState cardState)
        {
            return cardState != null ? new ProxyCombatCard(cardState) : null;
        }

        public CardState Card => _cardState ?? _cardUI?.GetCardState();
        public override bool IsVisible => _cardState != null || (_cardUI != null && _cardUI.gameObject.activeInHierarchy);
        public override Message GetLabel()
        {
            Message summary = _focusSummary != null ? _focusSummary(Card) : FocusSummary(Card);
            return IsSelected()
                ? Message.Join(", ", Message.Localized("messages", "state.selected"), summary)
                : summary;
        }
        public override Message GetTooltip() => AccessibilitySummary(Card);

        private bool IsSelected()
        {
            return _cardUI != null && _isSelected?.Invoke(_cardUI) == true;
        }

        public override void SelectForNavigation()
        {
            if (SyncGameFocusedCard())
            {
                return;
            }

            base.SelectForNavigation();
        }

        public override bool Activate()
        {
            SyncGameFocusedCard();
            return base.Activate();
        }

        internal bool PrepareForNativeSubmit()
        {
            return SyncGameFocusedCard();
        }

        private bool SyncGameFocusedCard()
        {
            if (_cardUI == null || _selectionBehaviour == null)
            {
                return false;
            }

            int cardIndex = _selectionBehaviour.FindCardIndex(_cardUI);
            if (cardIndex < 0)
            {
                return false;
            }

            _selectionBehaviour.FocusCard(cardIndex, snapFocus: true, refresh: true);
            _selectionBehaviour.NavigateToFocusedCard();
            return true;
        }

        internal override string HandleBuffers(BufferManager buffers)
        {
            return HandleBuffers(buffers, null);
        }

        internal string HandleBuffers(BufferManager buffers, List<Message> beforeLabel)
        {
            base.HandleBuffers(buffers);
            PresentationBuffer<CardState> buffer = buffers?.GetBuffer("card") as PresentationBuffer<CardState>;
            CardState card = Card;
            if (buffer == null || card == null)
            {
                return "ui";
            }

            buffer.Bind(card, beforeLabel, _bufferBottomParts?.Invoke(_cardUI, card));
            buffers.EnableBuffer("card", true);
            return "card";
        }

        public static Message Cost(CardState card)
        {
            if (card == null)
            {
                return null;
            }

            if (card.IsNonPlayableEnergyCostType())
            {
                return Message.Localized("combat", "CARD.UNPLAYABLE");
            }

            if (card.IsConsumeRemainingEnergyCostType())
            {
                return Message.Localized("combat", "CARD.X_COST");
            }

            AllGameManagers managers = AllGameManagers.Instance.OrNull();
            CardStatistics cardStatistics = managers?.GetCardStatistics();
            if (cardStatistics == null)
            {
                return null;
            }

            int cost = card.GetCost(cardStatistics, managers?.GetMonsterManager(), managers?.GetRelicManager());
            return Message.Localized("combat", "CARD.COST", new { cost });
        }

        public static Message TypeName(CardState card)
        {
            if (card == null || card.GetCardType() == CardType.Invalid)
            {
                return null;
            }

            return Message.RawCleaned(AccessibilityLocalizationScope.Run(() => card.GetCardType().GetLocalizedName()));
        }

        public static Message GameplayText(CardState card)
        {
            return GeneratedGameplayText(card);
        }

        public static Message Description(CardState card)
        {
            return AccessibilitySummary(card);
        }

        internal static List<Message> LoreLines(CardState card)
        {
            List<Message> lines = new List<Message>();
            if (card == null)
            {
                return lines;
            }

            AddLoreKeys(lines, card.GetCardLoreTooltipKeys(), "card " + card.GetAssetName());
            CharacterData spawn = card.GetSpawnCharacterData();
            if (spawn != null)
            {
                AddLoreKeys(lines, spawn.GetCharacterLoreTooltipKeys(), "character " + spawn.name);
            }

            return lines;
        }

        internal static Message AccessibilitySummaryWithLore(CardState card)
        {
            List<Message> lines = new List<Message>(PresentationRenderer.BufferLines(PhaseRegistry.Cards.Build(card)));
            InsertLoreAfterTitle(lines, LoreLines(card));
            return lines.Count > 0 ? Message.JoinLines(lines) : null;
        }

        public static Message FocusSummary(CardState card)
        {
            if (IsCompactElixirCard(card))
            {
                return CompactElixirFocusSummary(card);
            }

            return PresentationRenderer.FocusSummary(PhaseRegistry.Cards.Build(card));
        }

        public static Message AccessibilitySummary(CardState card)
        {
            return PresentationRenderer.FocusTooltip(PhaseRegistry.Cards.Build(card));
        }

        private static void InsertLoreAfterTitle(List<Message> lines, List<Message> lore)
        {
            if (lines == null || lore == null || lore.Count == 0)
            {
                return;
            }

            int insertAt = lines.Count > 0 ? 1 : 0;
            for (int i = lore.Count - 1; i >= 0; i--)
            {
                if (lore[i] != null)
                {
                    lines.Insert(insertAt, lore[i]);
                }
            }
        }

        private static void AddLoreKeys(List<Message> lines, List<string> keys, string owner)
        {
            if (lines == null || keys == null)
            {
                return;
            }

            for (int i = 0; i < keys.Count; i++)
            {
                string key = keys[i];
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                if (!key.HasTranslation())
                {
                    Log.Info("[AccessibilityMod] Card lore missing localization for " + owner + ": " + key);
                    continue;
                }

                string text = AccessibilityLocalizationScope.Run(() => Message.Clean(key.Localize()));
                MessageList.Add(lines, Message.FromText(text));
            }
        }

        private static bool IsCompactElixirCard(CardState card)
        {
            return card != null &&
                (card.HasTrait<CardTraitInfusion>() || card.HasTrait<CardTraitCraftedSpike>());
        }

        private static Message CompactElixirFocusSummary(CardState card)
        {
            List<Message> parts = new List<Message>();
            MessageList.Add(parts, Message.FromText(card.GetTitle()));
            MessageList.Add(parts, CompactElixirEffectText(card));
            return parts.Count > 0 ? Message.Join(", ", parts) : null;
        }

        private static Message CompactElixirEffectText(CardState card)
        {
            if (card == null || card.GetCardType() != CardType.Spell)
            {
                return null;
            }

            List<Message> parts = new List<Message>();
            List<CardTriggerEffectState> triggers = card.GetTriggers();
            for (int i = 0; i < triggers.Count; i++)
            {
                CardTriggerEffectState trigger = triggers[i];
                if (trigger == null ||
                    trigger.GetTrigger() != CardTriggerType.OnCast ||
                    trigger.GetTriggerKeywordCardTextSuppressed())
                {
                    continue;
                }

                string descriptionKey = trigger.GetDescriptionKey();
                if (string.IsNullOrWhiteSpace(descriptionKey) || !descriptionKey.HasTranslation())
                {
                    continue;
                }

                string text = AccessibilityLocalizationScope.Run(() =>
                    Message.Clean(descriptionKey.Localize(new CardEffectLocalizationContext(trigger, null, card))));
                MessageList.Add(parts, Message.FromText(text));
            }

            return parts.Count > 0 ? Message.Join(" ", parts) : null;
        }

        private static Message Metadata(CardState card)
        {
            if (card == null)
            {
                return null;
            }

            List<Message> parts = new List<Message>();
            MessageList.Add(parts, MetadataTypeName(card));
            MessageList.Add(parts, Rarity(card));

            return parts.Count > 0 ? Message.Join(", ", parts) : null;
        }

        private static Message MetadataTypeName(CardState card)
        {
            if (card == null)
            {
                return null;
            }

            List<Message> parts = new List<Message>();
            MessageList.Add(parts, CardTypeCardText(card));
            if (IsBannerUnit(card))
            {
                MessageList.Add(parts, Message.Localized("ui", "CARD_METADATA.BANNER_UNIT"));
            }

            if (parts.Count == 0)
            {
                MessageList.Add(parts, TypeName(card));
            }

            return parts.Count > 0 ? Message.Join(", ", parts) : null;
        }

        private static Message CardTypeCardText(CardState card)
        {
            if (card == null || card.GetCardType() == CardType.Invalid)
            {
                return null;
            }

            string text = AccessibilityLocalizationScope.Run(() =>
            {
                card.GetCardTypeCardText(out string cardText);
                return cardText;
            });
            return Message.FromText(text);
        }

        private static Message Rarity(CardState card)
        {
            if (card == null)
            {
                return null;
            }

            CollectableRarity rarity = card.GetRarity();
            if (rarity == CollectableRarity.Unset)
            {
                return null;
            }

            return Message.Localized("ui", "CARD_RARITY." + rarity.ToString().ToUpperInvariant());
        }

        private static bool IsBannerUnit(CardState card)
        {
            if (card == null || !card.IsSpawnerCard())
            {
                return false;
            }

            CharacterData character = card.GetSpawnCharacterData();
            List<SubtypeData> subtypes = character?.GetSubtypes();
            if (subtypes == null)
            {
                return false;
            }

            for (int i = 0; i < subtypes.Count; i++)
            {
                if (subtypes[i]?.Key == "SubtypesData_BannerUnit")
                {
                    return true;
                }
            }

            return false;
        }

        private static void AddUnitStats(CardState card, List<Message> parts)
        {
            bool isUnit = card != null && card.IsSpawnerCard();
            bool isEquipment = card != null && card.GetCardType() == CardType.Equipment;
            if (!isUnit && !isEquipment)
            {
                return;
            }

            int attack = card.GetTotalAttackDamage();
            int health = (int)card.GetHealth();
            int size = card.GetSize();
            if (isUnit && size > 0)
            {
                parts.Add(Message.Localized("ui", "CARD_STATS.SIZE", new { size }));
            }
            if (isUnit || health > 0)
            {
                parts.Add(Message.Localized("ui", "CARD_STATS.HEALTH", new { health }));
            }
            if (isUnit || attack > 0)
            {
                parts.Add(Message.Localized("combat", "CREATURE.ATTACK", new { attack }));
            }
        }

    }
}

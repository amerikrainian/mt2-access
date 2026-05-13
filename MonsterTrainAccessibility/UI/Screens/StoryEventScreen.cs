using System;
using MonsterTrainAccessibility.Util;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Buffers;
using MonsterTrainAccessibility.Events;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Presentation;
using MonsterTrainAccessibility.Presentation.Relics;
using MonsterTrainAccessibility.Presentation.Rewards;
using MonsterTrainAccessibility.UI;
using MonsterTrainAccessibility.UI.Elements;
using ShinyShoe;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using AccessiblePresentation = MonsterTrainAccessibility.Presentation.Presentation;

namespace MonsterTrainAccessibility.UI.Screens
{
    internal sealed class StoryEventScreen : ListNavigationGameScreen
    {
        private static readonly FieldInfo ContentLabelField = AccessTools.Field(typeof(global::StoryEventScreen), "contentLabel")!;
        private static readonly FieldInfo CurrentChoiceItemsField = AccessTools.Field(typeof(global::StoryEventScreen), "currentChoiceItems")!;
        private static readonly FieldInfo ContinueButtonField = AccessTools.Field(typeof(global::StoryEventScreen), "continueButton")!;
        private static readonly FieldInfo SaveManagerField = AccessTools.Field(typeof(global::StoryEventScreen), "saveManager")!;
        private static readonly FieldInfo RelicManagerField = AccessTools.Field(typeof(global::StoryEventScreen), "relicManager")!;
        private static readonly FieldInfo ChoiceLabelField = AccessTools.Field(typeof(global::StoryChoiceItem), "label")!;
        private static readonly FieldInfo ChoiceOptionalRewardLabelField = AccessTools.Field(typeof(global::StoryChoiceItem), "optionalRewardLabel")!;

        private readonly global::StoryEventScreen _screen;
        private string _lastAnnouncedContent;
        private string _lastAnnouncedContentKey;

        public StoryEventScreen(global::StoryEventScreen screen)
        {
            _screen = screen;
            RootList.AnnouncePosition = false;
        }

        public override bool ShouldRestoreNavigationFocus() => false;

        public override void OnUpdate()
        {
            base.OnUpdate();
            SyncTransientSelection();
            AnnounceContentIfChanged();
        }

        protected override void PopulateList()
        {
            List<StoryChoiceItem> choices = Get<List<StoryChoiceItem>>(_screen, CurrentChoiceItemsField);
            if (choices != null)
            {
                for (int i = 0; i < choices.Count; i++)
                {
                    AddChoice(choices[i]);
                }
            }

            GameUISelectableButton continueButton = Get<GameUISelectableButton>(_screen, ContinueButtonField);
            if (continueButton != null)
            {
                LabeledButton element = new LabeledButton(continueButton, "DIALOGUE.CONTINUE");
                AddElement(element, continueButton.gameObject);
            }
        }

        protected override string BuildSignature()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            TMP_Text content = Get<TMP_Text>(_screen, ContentLabelField);
            sb.Append(content != null && content.gameObject.activeInHierarchy).Append('|');
            List<StoryChoiceItem> choices = Get<List<StoryChoiceItem>>(_screen, CurrentChoiceItemsField);
            if (choices != null)
            {
                for (int i = 0; i < choices.Count; i++)
                {
                    StoryChoiceItem choice = choices[i];
                    sb.Append(choice != null && choice.gameObject.activeInHierarchy)
                        .Append(':')
                        .Append(ReadText(Get<TMP_Text>(choice, ChoiceLabelField)))
                        .Append(':')
                        .Append(ReadText(Get<TMP_Text>(choice, ChoiceOptionalRewardLabelField)))
                        .Append('|');
                }
            }

            GameUISelectableButton continueButton = Get<GameUISelectableButton>(_screen, ContinueButtonField);
            sb.Append(";continue:").Append(continueButton != null && continueButton.gameObject.activeInHierarchy);
            return sb.ToString();
        }

        private void AnnounceContentIfChanged()
        {
            TMP_Text content = Get<TMP_Text>(_screen, ContentLabelField);
            string text = content != null && content.gameObject.activeInHierarchy
                ? Message.Clean(AccessibilityText.ReadLocalizedText(content))
                : string.Empty;

            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            string contentKey = ContentAnnouncementKey(text);
            if (string.Equals(contentKey, _lastAnnouncedContentKey, StringComparison.Ordinal))
            {
                return;
            }

            string announcement = text;
            if (!string.IsNullOrWhiteSpace(_lastAnnouncedContent) &&
                text.StartsWith(_lastAnnouncedContent, StringComparison.Ordinal))
            {
                announcement = Message.Clean(text.Substring(_lastAnnouncedContent.Length));
            }

            _lastAnnouncedContent = text;
            _lastAnnouncedContentKey = contentKey;
            if (!string.IsNullOrWhiteSpace(announcement))
            {
                EventDispatcher.Enqueue(new BasicEvent(Message.Raw(announcement)));
            }
        }

        private static string ContentAnnouncementKey(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            System.Text.StringBuilder key = new System.Text.StringBuilder(text.Length);
            bool inTag = false;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '<')
                {
                    inTag = true;
                    continue;
                }

                if (inTag)
                {
                    if (c == '>')
                    {
                        inTag = false;
                    }

                    continue;
                }

                if (!char.IsWhiteSpace(c))
                {
                    key.Append(char.ToLowerInvariant(c));
                }
            }

            return key.ToString();
        }

        private void SyncTransientSelection()
        {
            GameUISelectableButton continueButton = Get<GameUISelectableButton>(_screen, ContinueButtonField);
            if (continueButton == null || !continueButton.gameObject.activeInHierarchy)
            {
                return;
            }

            GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (selected == null || selected.GetComponent<StoryChoiceItem>() == null)
            {
                return;
            }

            global::InputManager.Inst?.SelectGameUIComponent(continueButton, allowClearingSelection: true, reselect: true);
        }

        private void AddChoice(StoryChoiceItem choice)
        {
            if (choice == null || choice.button == null)
            {
                return;
            }

            ProxyStoryEventChoice element = new ProxyStoryEventChoice(_screen, choice);
            AddElement(element, choice.gameObject, choice.button.gameObject);
        }

        internal static Message ChoiceLabel(StoryChoiceItem choice)
        {
            if (choice == null)
            {
                return null;
            }

            string text = ReadText(Get<TMP_Text>(choice, ChoiceLabelField));
            if (Message.ShouldAdd(text))
            {
                return Message.Raw(text);
            }

            return Message.RawCleaned(choice.choiceText);
        }

        internal static Message ChoiceStatus(StoryChoiceItem choice)
        {
            if (choice == null)
            {
                return null;
            }

            string text = ReadText(Get<TMP_Text>(choice, ChoiceOptionalRewardLabelField));
            return Message.ShouldAdd(text) ? Message.Raw(text) : null;
        }

        internal static Message ChoiceRewards(global::StoryEventScreen screen, StoryChoiceItem choice)
        {
            List<StoryChoiceData.RewardInfo> rewards = choice?.GetRewardsInfo();
            if (rewards == null || rewards.Count == 0)
            {
                return null;
            }

            SaveManager saveManager = Get<SaveManager>(screen, SaveManagerField);
            RelicManager relicManager = Get<RelicManager>(screen, RelicManagerField);
            List<Message> parts = new List<Message>();
            for (int i = 0; i < rewards.Count; i++)
            {
                StoryChoiceData.RewardInfo reward = rewards[i];
                Message preview = RewardPreviewTooltip(reward, saveManager, relicManager);
                if (preview != null)
                {
                    parts.Add(preview);
                }
            }

            parts = MessageList.Dedupe(parts);
            return parts.Count > 0 ? Message.Join(". ", parts) : ChoiceStatus(choice);
        }

        internal static string HandleChoiceBuffers(global::StoryEventScreen screen, StoryChoiceItem choice, BufferManager buffers)
        {
            if (screen == null || choice == null || buffers == null)
            {
                return null;
            }

            List<StoryChoiceData.RewardInfo> rewards = choice.GetRewardsInfo();
            if (rewards == null || rewards.Count == 0)
            {
                return null;
            }

            SaveManager saveManager = Get<SaveManager>(screen, SaveManagerField);
            RelicManager relicManager = Get<RelicManager>(screen, RelicManagerField);
            if (saveManager == null)
            {
                return null;
            }

            LineBuffer uiBuffer = buffers.GetBuffer("ui");
            if (uiBuffer == null)
            {
                return null;
            }

            List<Message> lines = ChoiceBufferContext(choice);
            for (int i = 0; i < rewards.Count; i++)
            {
                AddRewardInfoBufferLines(lines, rewards[i], saveManager, relicManager);
            }

            lines = MessageList.Dedupe(lines);
            if (lines.Count == 0)
            {
                return null;
            }

            uiBuffer.Clear();
            for (int i = 0; i < lines.Count; i++)
            {
                uiBuffer.Add(lines[i]);
            }

            buffers.EnableBuffer("ui", true);
            return "ui";
        }

        private static Message RewardPreviewTooltip(StoryChoiceData.RewardInfo reward, SaveManager saveManager, RelicManager relicManager)
        {
            if (reward == null || saveManager == null)
            {
                return null;
            }

            switch (reward.previewType)
            {
                case StoryChoiceData.PreviewType.Card:
                    return CardPreviewTooltip(saveManager.GetAllGameData().FindCardDataByName(reward.dataKey), saveManager, relicManager);
                case StoryChoiceData.PreviewType.Relic:
                case StoryChoiceData.PreviewType.Relic_Name:
                    return RelicPreviewTooltip(saveManager.GetAllGameData().FindCollectableRelicDataByName(reward.dataKey));
                case StoryChoiceData.PreviewType.Upgrade:
                    return UpgradePreviewTooltip(saveManager.GetAllGameData().FindEnhancerDataByName(reward.dataKey));
                case StoryChoiceData.PreviewType.Reward:
                    return RewardDataPreviewTooltip(saveManager.GetAllGameData().FindRewardDataByName(reward.dataKey), saveManager, relicManager);
                case StoryChoiceData.PreviewType.Coin:
                    return CoinPreviewTooltip(reward, saveManager);
                case StoryChoiceData.PreviewType.DeckReward:
                    return DeckRewardPreviewTooltip(reward, saveManager, relicManager);
                case StoryChoiceData.PreviewType.DeckRewards:
                    return DeckRewardsPreviewTooltip(reward, saveManager, relicManager);
                case StoryChoiceData.PreviewType.DelayedEnhanceReward:
                    return DelayedEnhancePreviewTooltip(reward, saveManager, relicManager);
                default:
                    return Message.FromText(reward.dataKey);
            }
        }

        private static Message CardPreviewTooltip(CardData cardData, SaveManager saveManager, RelicManager relicManager)
        {
            CardState cardState = CreateCardState(cardData, saveManager, relicManager);
            return cardState != null ? ProxyCombatCard.Description(cardState) : null;
        }

        private static Message RelicPreviewTooltip(RelicData relicData)
        {
            return relicData != null ? ProxyRelicInfo.FromData(relicData, includeDynamicInfo: true) : null;
        }

        private static Message UpgradePreviewTooltip(EnhancerData enhancerData)
        {
            if (enhancerData == null)
            {
                return null;
            }

            CardUpgradeData upgrade = FindCardUpgrade(enhancerData);
            Message tooltip = new ProxyCardUpgrade(upgrade).GetTooltip();
            return tooltip ?? ProxyRelicInfo.FromData(enhancerData, includeDynamicInfo: true);
        }

        private static Message RewardDataPreviewTooltip(GrantableRewardData rewardData, SaveManager saveManager, RelicManager relicManager)
        {
            if (rewardData == null)
            {
                return null;
            }

            if (rewardData is GrantUpgradedCachedCardRewardData upgradedCachedCard)
            {
                return UpgradedCardTooltip(
                    upgradedCachedCard.GetCardData(saveManager),
                    saveManager,
                    relicManager,
                    new List<CardUpgradeData> { upgradedCachedCard.GetCardUpgradeData() },
                    upgradedCachedCard.GetCardState(saveManager)?.GetCardStateModifiers(),
                    null);
            }

            if (rewardData is BuildCardRewardData buildCard)
            {
                return UpgradedCardTooltip(
                    buildCard.GetCardData(),
                    saveManager,
                    relicManager,
                    buildCard.GetUpgradeDatas(saveManager),
                    null,
                    null);
            }

            if (rewardData is CardRewardData cardReward)
            {
                return CardPreviewTooltip(cardReward.GetCardData(), saveManager, relicManager);
            }

            if (rewardData is RelicRewardData relicReward)
            {
                return relicReward.UseCachedCard
                    ? DelayedEnhancePreviewTooltip(
                        new StoryChoiceData.RewardInfo
                        {
                            previewType = StoryChoiceData.PreviewType.DelayedEnhanceReward,
                            dataKey = rewardData.name
                        },
                        saveManager,
                        relicManager)
                    : RelicPreviewTooltip(relicReward.GetRelicData());
            }

            return ProxyRewardItem.Description(rewardData) ?? ProxyRewardItem.Tooltip(rewardData);
        }

        private static Message DeckRewardPreviewTooltip(StoryChoiceData.RewardInfo reward, SaveManager saveManager, RelicManager relicManager)
        {
            if (reward == null)
            {
                return null;
            }

            return RewardDataPreviewTooltip(saveManager.GetAllGameData().FindRewardDataByName(reward.dataKey), saveManager, relicManager);
        }

        private static Message DeckRewardsPreviewTooltip(StoryChoiceData.RewardInfo reward, SaveManager saveManager, RelicManager relicManager)
        {
            if (reward == null || string.IsNullOrWhiteSpace(reward.dataKey))
            {
                return null;
            }

            List<Message> parts = new List<Message>();
            string[] rewardIds = reward.dataKey.Split(',');
            for (int i = 0; i < rewardIds.Length; i++)
            {
                string rewardId = rewardIds[i]?.Trim();
                if (string.IsNullOrWhiteSpace(rewardId))
                {
                    continue;
                }

                Message tooltip = RewardDataPreviewTooltip(saveManager.GetAllGameData().FindRewardDataByName(rewardId), saveManager, relicManager);
                if (tooltip != null)
                {
                    parts.Add(tooltip);
                }
            }

            parts = MessageList.Dedupe(parts);
            return parts.Count > 0 ? Message.Join(". ", parts) : null;
        }

        private static Message DelayedEnhancePreviewTooltip(StoryChoiceData.RewardInfo reward, SaveManager saveManager, RelicManager relicManager)
        {
            if (reward == null)
            {
                return null;
            }

            GrantableRewardData rewardData = saveManager.GetAllGameData().FindRewardDataByName(reward.dataKey);
            RelicRewardData relicReward = rewardData as RelicRewardData;
            if (relicReward == null || !relicReward.UseCachedCard)
            {
                return null;
            }

            CardState cachedCard = saveManager.GetEventCachedCardState();
            if (cachedCard == null)
            {
                return null;
            }

            CardData cardData = saveManager.GetAllGameData().FindCardData(cachedCard.GetCardDataID());
            if (cardData == null)
            {
                return null;
            }

            List<CardUpgradeData> upgrades = new List<CardUpgradeData>();
            List<RelicEffectData> effects = relicReward.GetRelicData()?.GetEffects();
            if (effects != null)
            {
                for (int i = 0; i < effects.Count; i++)
                {
                    CardUpgradeData upgrade = effects[i]?.GetParamCardUpgradeData();
                    if (upgrade != null)
                    {
                        upgrades.Add(upgrade);
                    }
                }
            }

            return UpgradedCardTooltip(
                cardData,
                saveManager,
                relicManager,
                upgrades,
                cachedCard.GetCardStateModifiers(),
                cachedCard.GetTemporaryCardStateModifiers());
        }

        private static void AddRewardInfoBufferLines(
            List<Message> lines,
            StoryChoiceData.RewardInfo reward,
            SaveManager saveManager,
            RelicManager relicManager)
        {
            if (lines == null || reward == null || saveManager == null)
            {
                return;
            }

            switch (reward.previewType)
            {
                case StoryChoiceData.PreviewType.Card:
                    AddCardBufferLines(lines, CreateCardState(saveManager.GetAllGameData().FindCardDataByName(reward.dataKey), saveManager, relicManager));
                    return;
                case StoryChoiceData.PreviewType.Relic:
                case StoryChoiceData.PreviewType.Relic_Name:
                    AddRelicBufferLines(lines, saveManager.GetAllGameData().FindCollectableRelicDataByName(reward.dataKey));
                    return;
                case StoryChoiceData.PreviewType.Upgrade:
                    AddRelicBufferLines(lines, saveManager.GetAllGameData().FindEnhancerDataByName(reward.dataKey));
                    return;
                case StoryChoiceData.PreviewType.Reward:
                case StoryChoiceData.PreviewType.DeckReward:
                    AddRewardDataBufferLines(
                        lines,
                        saveManager.GetAllGameData().FindRewardDataByName(reward.dataKey),
                        saveManager,
                        relicManager);
                    return;
                case StoryChoiceData.PreviewType.DelayedEnhanceReward:
                    AddCardBufferLines(lines, CreateDelayedEnhancedCardState(reward, saveManager, relicManager));
                    return;
                case StoryChoiceData.PreviewType.DeckRewards:
                    AddDeckRewardsBufferLines(lines, reward, saveManager, relicManager);
                    return;
                case StoryChoiceData.PreviewType.Coin:
                    MessageList.Add(lines, CoinPreviewTooltip(reward, saveManager));
                    return;
            }
        }

        private static void AddRewardDataBufferLines(
            List<Message> lines,
            GrantableRewardData rewardData,
            SaveManager saveManager,
            RelicManager relicManager)
        {
            if (lines == null || rewardData == null)
            {
                return;
            }

            if (rewardData is GrantUpgradedCachedCardRewardData upgradedCachedCard)
            {
                AddCardBufferLines(
                    lines,
                    CreateUpgradedCardState(
                        upgradedCachedCard.GetCardData(saveManager),
                        saveManager,
                        relicManager,
                        new List<CardUpgradeData> { upgradedCachedCard.GetCardUpgradeData() },
                        upgradedCachedCard.GetCardState(saveManager)?.GetCardStateModifiers(),
                        null));
                return;
            }

            if (rewardData is BuildCardRewardData buildCard)
            {
                AddCardBufferLines(
                    lines,
                    CreateUpgradedCardState(
                        buildCard.GetCardData(),
                        saveManager,
                        relicManager,
                        buildCard.GetUpgradeDatas(saveManager),
                        null,
                        null));
                return;
            }

            if (rewardData is CardRewardData cardReward)
            {
                AddCardBufferLines(lines, CreateCardState(cardReward.GetCardData(), saveManager, relicManager));
                return;
            }

            if (rewardData is RelicRewardData relicReward)
            {
                if (relicReward.UseCachedCard)
                {
                    AddCardBufferLines(
                        lines,
                        CreateDelayedEnhancedCardState(
                            new StoryChoiceData.RewardInfo
                            {
                                previewType = StoryChoiceData.PreviewType.DelayedEnhanceReward,
                                dataKey = rewardData.name
                            },
                            saveManager,
                            relicManager));
                    return;
                }

                AddRelicBufferLines(lines, relicReward.GetRelicData());
                return;
            }

            AddPresentationLines(lines, PhaseRegistry.Rewards.Build(new RewardPresentationSource(rewardData)));
        }

        private static void AddDeckRewardsBufferLines(
            List<Message> lines,
            StoryChoiceData.RewardInfo reward,
            SaveManager saveManager,
            RelicManager relicManager)
        {
            if (lines == null || reward == null || string.IsNullOrWhiteSpace(reward.dataKey))
            {
                return;
            }

            string[] rewardIds = reward.dataKey.Split(',');
            for (int i = 0; i < rewardIds.Length; i++)
            {
                string rewardId = rewardIds[i]?.Trim();
                if (string.IsNullOrWhiteSpace(rewardId))
                {
                    continue;
                }

                AddRewardDataBufferLines(
                    lines,
                    saveManager.GetAllGameData().FindRewardDataByName(rewardId),
                    saveManager,
                    relicManager);
            }
        }

        private static void AddCardBufferLines(List<Message> lines, CardState cardState)
        {
            if (lines == null || cardState == null)
            {
                return;
            }

            AddPresentationLines(lines, PhaseRegistry.Cards.Build(cardState));
        }

        private static void AddRelicBufferLines(List<Message> lines, RelicData relicData)
        {
            if (lines == null || relicData == null)
            {
                return;
            }

            AddPresentationLines(
                lines,
                PhaseRegistry.Relics.Build(RelicPresentationSource.FromState(new RelicState(relicData), includeDynamicInfo: true)));
        }

        private static void AddPresentationLines(List<Message> lines, AccessiblePresentation presentation)
        {
            IReadOnlyList<Message> rendered = PresentationRenderer.BufferLines(presentation);
            for (int i = 0; i < rendered.Count; i++)
            {
                MessageList.Add(lines, rendered[i]);
            }
        }

        private static List<Message> ChoiceBufferContext(StoryChoiceItem choice)
        {
            List<Message> context = new List<Message>();
            MessageList.Add(context, ChoiceLabel(choice));
            MessageList.Add(context, ChoiceStatus(choice));
            return context;
        }

        private static Message CoinPreviewTooltip(StoryChoiceData.RewardInfo reward, SaveManager saveManager)
        {
            if (reward == null)
            {
                return null;
            }

            if (!int.TryParse(reward.dataKey, out int amount))
            {
                return Message.FromText(reward.dataKey);
            }

            amount = saveManager.GetAdjustedGoldAmount(amount, isReward: true);
            return Message.Localized("ui", "REWARD.GOLD", new { amount });
        }

        private static Message UpgradedCardTooltip(
            CardData cardData,
            SaveManager saveManager,
            RelicManager relicManager,
            List<CardUpgradeData> upgrades,
            CardStateModifiers permanentModifiers,
            CardStateModifiers temporaryModifiers)
        {
            CardState cardState = CreateUpgradedCardState(
                cardData,
                saveManager,
                relicManager,
                upgrades,
                permanentModifiers,
                temporaryModifiers);
            return cardState != null ? ProxyCombatCard.Description(cardState) : null;
        }

        private static CardState CreateUpgradedCardState(
            CardData cardData,
            SaveManager saveManager,
            RelicManager relicManager,
            List<CardUpgradeData> upgrades,
            CardStateModifiers permanentModifiers,
            CardStateModifiers temporaryModifiers)
        {
            if (cardData == null || saveManager == null)
            {
                return null;
            }

            CardState cardState = new CardState(cardData, saveManager, setupStartingUpgrades: false);
            relicManager?.ApplyCardStateModifiers(cardState);
            if (permanentModifiers != null)
            {
                cardState.CopyPermanentUpgrades(permanentModifiers, saveManager.GetAllGameData(), saveManager);
            }

            if (temporaryModifiers != null)
            {
                cardState.CopyTemporaryUpgrades(temporaryModifiers, saveManager.GetAllGameData(), saveManager);
            }

            if (upgrades != null)
            {
                for (int i = 0; i < upgrades.Count; i++)
                {
                    CardUpgradeData upgrade = upgrades[i];
                    if (upgrade == null)
                    {
                        continue;
                    }

                    CardUpgradeState state = Activator.CreateInstance<CardUpgradeState>();
                    state.Setup(upgrade);
                    cardState.ApplyPermanentUpgrade(state, saveManager, ignoreUpgradeAnimation: true);
                }
            }

            return cardState;
        }

        private static CardState CreateDelayedEnhancedCardState(
            StoryChoiceData.RewardInfo reward,
            SaveManager saveManager,
            RelicManager relicManager)
        {
            if (reward == null || saveManager == null)
            {
                return null;
            }

            GrantableRewardData rewardData = saveManager.GetAllGameData().FindRewardDataByName(reward.dataKey);
            RelicRewardData relicReward = rewardData as RelicRewardData;
            if (relicReward == null || !relicReward.UseCachedCard)
            {
                return null;
            }

            CardState cachedCard = saveManager.GetEventCachedCardState();
            if (cachedCard == null)
            {
                return null;
            }

            CardData cardData = saveManager.GetAllGameData().FindCardData(cachedCard.GetCardDataID());
            if (cardData == null)
            {
                return null;
            }

            List<CardUpgradeData> upgrades = new List<CardUpgradeData>();
            List<RelicEffectData> effects = relicReward.GetRelicData()?.GetEffects();
            if (effects != null)
            {
                for (int i = 0; i < effects.Count; i++)
                {
                    CardUpgradeData upgrade = effects[i]?.GetParamCardUpgradeData();
                    if (upgrade != null)
                    {
                        upgrades.Add(upgrade);
                    }
                }
            }

            return CreateUpgradedCardState(
                cardData,
                saveManager,
                relicManager,
                upgrades,
                cachedCard.GetCardStateModifiers(),
                cachedCard.GetTemporaryCardStateModifiers());
        }

        private static CardState CreateCardState(CardData cardData, SaveManager saveManager, RelicManager relicManager)
        {
            if (cardData == null || saveManager == null)
            {
                return null;
            }

            CardState cardState = new CardState(cardData, saveManager);
            relicManager?.ApplyCardStateModifiers(cardState);
            return cardState;
        }

        private static CardUpgradeData FindCardUpgrade(RelicData relicData)
        {
            if (relicData == null)
            {
                return null;
            }

            RelicEffectData direct = relicData.GetFirstRelicEffectData<RelicEffectCardUpgrade>();
            CardUpgradeData upgrade = direct?.GetParamCardUpgradeData();
            if (upgrade != null)
            {
                return upgrade;
            }

            List<RelicEffectData> effects = relicData.GetEffects();
            if (effects == null)
            {
                return null;
            }

            for (int i = 0; i < effects.Count; i++)
            {
                upgrade = effects[i]?.GetParamCardUpgradeData();
                if (upgrade != null)
                {
                    return upgrade;
                }
            }

            return null;
        }

        private static string ReadText(TMP_Text text)
        {
            return text != null && text.gameObject.activeInHierarchy
                ? Message.Clean(AccessibilityText.ReadLocalizedText(text))
                : string.Empty;
        }

        private static T Get<T>(object owner, FieldInfo field) where T : class
        {
            return owner != null ? field.GetValue(owner) as T : null;
        }
    }
}

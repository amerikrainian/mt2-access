using MonsterTrainAccessibility.Buffers;
using MonsterTrainAccessibility.Localization;
using ShinyShoe;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyEndlessMutatorPair : GameObjectElement
    {
        private readonly EndlessMutatorPairUI _pairUi;

        public ProxyEndlessMutatorPair(EndlessMutatorPairUI pairUi, IGameUIComponent selectable)
            : base(selectable, typeKey: "button", label: null)
        {
            _pairUi = pairUi;
        }

        private EndlessMutatorPairRewardData.EndlessMutatorPair Pair => _pairUi?.MutatorPair;

        public override bool IsVisible => _pairUi != null && _pairUi.gameObject.activeInHierarchy && Pair != null;

        public override Message GetLabel() => Label(Pair);

        public override Message GetTooltip()
        {
            EndlessMutatorPairRewardData.EndlessMutatorPair pair = Pair;
            if (pair == null)
            {
                return null;
            }

            return Message.JoinLines(
                Entry("ENDLESS_MODIFIERS.POSITIVE", pair.positive),
                Entry("ENDLESS_MODIFIERS.NEGATIVE", pair.negative));
        }

        internal override string HandleBuffers(BufferManager buffers)
        {
            LineBuffer buffer = buffers?.GetBuffer("ui");
            if (buffer == null)
            {
                return base.HandleBuffers(buffers);
            }

            buffer.Clear();
            EndlessMutatorPairRewardData.EndlessMutatorPair pair = Pair;
            if (pair != null)
            {
                buffer.Add(Entry("ENDLESS_MODIFIERS.NEGATIVE", pair.negative));
                buffer.Add(Entry("ENDLESS_MODIFIERS.POSITIVE", pair.positive));
            }
            buffers.EnableBuffer("ui", true);
            return "ui";
        }

        public static Message Label(EndlessMutatorPairRewardData.EndlessMutatorPair pair)
        {
            if (pair == null)
            {
                return null;
            }

            return Message.Join(", ",
                Entry("ENDLESS_MODIFIERS.POSITIVE", pair.positive),
                Entry("ENDLESS_MODIFIERS.NEGATIVE", pair.negative));
        }

        private static Message Entry(string key, RewardData reward)
        {
            string text = ModifierText(reward);
            return Message.ShouldAdd(text)
                ? Message.Localized("ui", key, new { text })
                : null;
        }

        private static string ModifierText(RewardData reward)
        {
            if (reward == null)
            {
                return string.Empty;
            }

            string title = Message.Clean(reward.RewardTitle);
            string description = Message.Clean(Description(reward));
            if (!Message.ShouldAdd(description) || string.Equals(title, description, System.StringComparison.OrdinalIgnoreCase))
            {
                return title;
            }

            if (!Message.ShouldAdd(title))
            {
                return description;
            }

            return title + ". " + description;
        }

        private static string Description(RewardData reward)
        {
            RelicRewardData relicReward = reward as RelicRewardData;
            RelicData relicData = relicReward?.GetRelicData();
            if (relicData != null)
            {
                return relicData.GetDescription();
            }

            SaveManager saveManager = AllGameManagers.Instance != null ? AllGameManagers.Instance.GetSaveManager() : null;
            if (saveManager != null)
            {
                string text = reward.GetDescriptionText(saveManager);
                if (Message.ShouldAdd(text))
                {
                    return text;
                }
            }

            return reward.RewardDescription;
        }
    }
}

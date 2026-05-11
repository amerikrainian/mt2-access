using System.Reflection;
using System.Text;
using HarmonyLib;
using MonsterTrainAccessibility.Buffers;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Presentation;
using MonsterTrainAccessibility.UI.Elements;
using ShinyShoe;
using TMPro;

namespace MonsterTrainAccessibility.UI.Screens
{
    internal sealed class RunSummaryCardElement : GameObjectElement
    {
        private static readonly FieldInfo CountLabelField = AccessTools.Field(typeof(global::CardSummaryItem), "countLabel")!;

        private readonly global::CardSummaryItem _item;

        public RunSummaryCardElement(global::CardSummaryItem item)
            : base(
                item?.SelectableUI,
                typeKey: null,
                label: null)
        {
            _item = item;
        }

        public override bool IsVisible => _item != null && _item.gameObject.activeInHierarchy && _item.CardState != null;
        public override Message GetLabel() => CardLabel(_item);
        public override Message GetTooltip() => ProxyCombatCard.AccessibilitySummary(_item?.CardState);

        internal override string HandleBuffers(BufferManager buffers)
        {
            base.HandleBuffers(buffers);
            PresentationBuffer<CardState> buffer = buffers?.GetBuffer("card") as PresentationBuffer<CardState>;
            global::CardState card = _item?.CardState;
            if (buffer == null || card == null)
            {
                return "ui";
            }

            buffer.Bind(card);
            buffers.EnableBuffer("card", true);
            return "card";
        }

        private static Message CardLabel(global::CardSummaryItem item)
        {
            Message summary = ProxyCombatCard.FocusSummary(item?.CardState);
            TMP_Text count = item != null ? CountLabelField.GetValue(item) as TMP_Text : null;
            int countValue = ReadCount(count);
            return countValue <= 1
                ? summary
                : Message.Join(" ", Message.Localized("ui", "RUN_SUMMARY.CARD_COUNT_PREFIX", new { count = countValue }), summary);
        }

        private static int ReadCount(TMP_Text count)
        {
            if (count == null || !count.gameObject.activeInHierarchy)
            {
                return 1;
            }

            string text = AccessibilityText.ReadLocalizedText(count);
            if (string.IsNullOrWhiteSpace(text))
            {
                return 1;
            }

            StringBuilder digits = new StringBuilder();
            for (int i = 0; i < text.Length; i++)
            {
                if (char.IsDigit(text[i]))
                {
                    digits.Append(text[i]);
                }
            }

            int value;
            return int.TryParse(digits.ToString(), out value) ? value : 1;
        }
    }
}

using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Screens;
using TMPro;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal enum RunSummaryTextPart
    {
        Player,
        Sharecode,
        Date,
        RunType,
        OutcomeDefault,
        OutcomeEndless,
        RunTime,
        Score,
        Covenant
    }

    internal sealed class ProxyRunSummaryText : UIElement
    {
        private static readonly FieldInfo UserIdLabelField = AccessTools.Field(typeof(global::RunSummaryDetailsUI), "userIdLabel")!;
        private static readonly FieldInfo SharecodeLabelField = AccessTools.Field(typeof(global::RunSummaryDetailsUI), "sharecodeLabel")!;
        private static readonly FieldInfo TimeLabelField = AccessTools.Field(typeof(global::RunSummaryDetailsUI), "timeLabel")!;
        private static readonly FieldInfo RunTypeLabelField = AccessTools.Field(typeof(global::RunSummaryDetailsUI), "runTypeLabel")!;
        private static readonly FieldInfo OutcomeLabelDefaultField = AccessTools.Field(typeof(global::RunSummaryDetailsUI), "outcomeLabelDefault")!;
        private static readonly FieldInfo OutcomeLabelEndlessField = AccessTools.Field(typeof(global::RunSummaryDetailsUI), "outcomeLabelEndless")!;
        private static readonly FieldInfo RunTimeLabelField = AccessTools.Field(typeof(global::RunSummaryDetailsUI), "runTimeLabel")!;
        private static readonly FieldInfo ScoreLabelField = AccessTools.Field(typeof(global::RunSummaryDetailsUI), "scoreLabel")!;
        private static readonly FieldInfo CovenantLabelField = AccessTools.Field(typeof(global::RunSummaryDetailsUI), "covenantLabel")!;

        private readonly global::RunSummaryDetailsUI _details;
        private readonly RunSummaryTextPart _part;
        private readonly string _labelKey;

        public ProxyRunSummaryText(global::RunSummaryDetailsUI details, RunSummaryTextPart part, string labelKey)
        {
            _details = details;
            _part = part;
            _labelKey = labelKey;
        }

        public override bool IsVisible => HasText(Text());
        public override Message GetLabel()
        {
            Message value = AccessibleScreenText.Text(Text());
            return value != null
                ? Message.Join(", ", Message.Localized("ui", _labelKey), value)
                : null;
        }

        public TMP_Text Text() => Get<TMP_Text>(_details, FieldFor(_part));

        public static string Signature(global::RunSummaryDetailsUI details)
        {
            return TextSignature(details, UserIdLabelField) + "|" +
                TextSignature(details, SharecodeLabelField) + "|" +
                TextSignature(details, TimeLabelField) + "|" +
                TextSignature(details, RunTypeLabelField) + "|" +
                TextSignature(details, OutcomeLabelDefaultField) + "|" +
                TextSignature(details, OutcomeLabelEndlessField) + "|" +
                TextSignature(details, RunTimeLabelField) + "|" +
                TextSignature(details, ScoreLabelField) + "|" +
                TextSignature(details, CovenantLabelField);
        }

        private static FieldInfo FieldFor(RunSummaryTextPart part)
        {
            switch (part)
            {
                case RunSummaryTextPart.Player:
                    return UserIdLabelField;
                case RunSummaryTextPart.Sharecode:
                    return SharecodeLabelField;
                case RunSummaryTextPart.Date:
                    return TimeLabelField;
                case RunSummaryTextPart.RunType:
                    return RunTypeLabelField;
                case RunSummaryTextPart.OutcomeDefault:
                    return OutcomeLabelDefaultField;
                case RunSummaryTextPart.OutcomeEndless:
                    return OutcomeLabelEndlessField;
                case RunSummaryTextPart.RunTime:
                    return RunTimeLabelField;
                case RunSummaryTextPart.Score:
                    return ScoreLabelField;
                case RunSummaryTextPart.Covenant:
                    return CovenantLabelField;
                default:
                    return UserIdLabelField;
            }
        }

        private static bool HasText(TMP_Text text)
        {
            return text != null &&
                text.gameObject.activeInHierarchy &&
                !string.IsNullOrWhiteSpace(AccessibilityText.ReadLocalizedText(text));
        }

        private static string TextSignature(object owner, FieldInfo field)
        {
            TMP_Text text = Get<TMP_Text>(owner, field);
            return text != null && text.gameObject.activeInHierarchy
                ? AccessibilityText.ReadLocalizedText(text)
                : string.Empty;
        }

        private static T Get<T>(object owner, FieldInfo field) where T : class
        {
            return owner != null ? field.GetValue(owner) as T : null;
        }
    }
}

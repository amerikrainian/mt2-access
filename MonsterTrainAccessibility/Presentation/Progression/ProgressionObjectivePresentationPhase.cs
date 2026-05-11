using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI;
using TMPro;
using UnityEngine;

namespace MonsterTrainAccessibility.Presentation.Progression
{
    internal sealed class ProgressionObjectivePresentationPhase : IPhase<ProgressionObjectivePresentationSource>
    {
        private static readonly FieldInfo TitleRootField = AccessTools.Field(typeof(global::ProgressionObjectiveUI), "titleRoot")!;
        private static readonly FieldInfo TitleLabelField = AccessTools.Field(typeof(global::ProgressionObjectiveUI), "titleLabel")!;
        private static readonly FieldInfo DescriptionRootField = AccessTools.Field(typeof(global::ProgressionObjectiveUI), "descriptionRoot")!;
        private static readonly FieldInfo DescriptionLabelField = AccessTools.Field(typeof(global::ProgressionObjectiveUI), "descriptionLabel")!;
        private static readonly FieldInfo NumericLabelField = AccessTools.Field(typeof(global::ProgressionObjectiveUI), "numericLabel")!;

        public bool Matches(PresentationContext<ProgressionObjectivePresentationSource> ctx)
        {
            return ctx?.Source?.Objective != null;
        }

        public void Apply(PresentationContext<ProgressionObjectivePresentationSource> ctx, PresentationBuilder builder)
        {
            global::ProgressionObjectiveUI objective = ctx.Source.Objective;
            builder.SetTitle(ActiveText(objective, TitleRootField, TitleLabelField));
            builder.SetSubtitle(ActiveText(objective, DescriptionRootField, DescriptionLabelField));
            builder.SetDescription(Text(objective, NumericLabelField));
        }

        private static Message ActiveText(global::ProgressionObjectiveUI objective, FieldInfo rootField, FieldInfo textField)
        {
            GameObject root = Get<GameObject>(objective, rootField);
            return root != null && root.activeInHierarchy ? Text(objective, textField) : null;
        }

        private static Message Text(global::ProgressionObjectiveUI objective, FieldInfo field)
        {
            return Message.RawCleaned(AccessibilityText.ReadLocalizedText(Get<TMP_Text>(objective, field)));
        }

        private static T Get<T>(object owner, FieldInfo field) where T : class
        {
            return owner != null ? field.GetValue(owner) as T : null;
        }
    }
}

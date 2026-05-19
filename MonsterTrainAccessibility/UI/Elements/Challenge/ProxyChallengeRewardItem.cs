using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Presentation;
using MonsterTrainAccessibility.Presentation.Progression;
using MonsterTrainAccessibility.Presentation.Verbosity;
using MonsterTrainAccessibility.UI;
using TMPro;
using UnityEngine;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyChallengeRewardItem : GameObjectElement
    {
        private static readonly FieldInfo TextLabelField = AccessTools.Field(typeof(global::ChallengeRewardsItemUI), "textLabel")!;
        private static readonly FieldInfo LockedRootField = AccessTools.Field(typeof(global::ChallengeRewardsItemUI), "lockedRoot")!;
        private static readonly FieldInfo ProgressionObjectiveUIField = AccessTools.Field(typeof(global::ChallengeRewardsItemUI), "progressionObjectiveUI")!;

        private readonly global::ChallengeRewardsItemUI _item;

        public ProxyChallengeRewardItem(global::ChallengeRewardsItemUI item)
            : base(
                item?.RewardSelectable,
                typeKey: null,
                label: null)
        {
            _item = item;
        }

        public override bool IsVisible => _item != null && _item.gameObject.activeInHierarchy;

        public override Message GetLabel()
        {
            TMPro.TMP_Text label = Get<TMPro.TMP_Text>(_item, TextLabelField);
            return Message.RawCleaned(AccessibilityText.ReadText(label));
        }

        public override Message GetStatusString()
        {
            UnityEngine.GameObject locked = Get<UnityEngine.GameObject>(_item, LockedRootField);
            return locked != null && locked.activeInHierarchy
                ? Message.Localized("messages", "state.locked")
                : null;
        }

        public override Message GetTooltip()
        {
            global::ProgressionObjectiveUI objective = Get<global::ProgressionObjectiveUI>(_item, ProgressionObjectiveUIField);
            if (objective == null)
            {
                return null;
            }

            return PresentationRenderer.FocusTooltip(
                PhaseRegistry.ProgressionObjectives.Build(new ProgressionObjectivePresentationSource(objective)),
                VerbosityRegistry.ForSource<ProgressionObjectivePresentationSource>());
        }

        private static T Get<T>(object owner, FieldInfo field) where T : class
        {
            return owner != null ? field.GetValue(owner) as T : null;
        }
    }
}

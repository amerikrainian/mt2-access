using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Localization;
using TMPro;
using UnityEngine;

namespace MonsterTrainAccessibility.UI
{
    internal static class GameUIButtonSupport
    {
        public static string ResolveLabel(global::ShinyShoe.GameUISelectableButton button)
        {
            if (button == null)
            {
                return string.Empty;
            }

            global::MainMenuButton mainMenuButton = button.GetComponentInParent<global::MainMenuButton>();
            if (mainMenuButton != null && ReferenceEquals(mainMenuButton.Button, button))
            {
                string label = AccessibilityText.ReadTextFromField<TMP_Text>(mainMenuButton, "label", "MainMenuButton");
                if (!string.IsNullOrWhiteSpace(label))
                {
                    return label;
                }
            }

            global::ChangeButtonLabelWhenInteractable changeLabel = button.GetComponent<global::ChangeButtonLabelWhenInteractable>();
            if (changeLabel != null)
            {
                TMP_Text label = ReflectionUtil.GetFieldValue<TMP_Text>(changeLabel, "label", "ChangeButtonLabelWhenInteractable");
                string text = AccessibilityText.ReadText(label);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    return text;
                }
            }

            global::GameSelectableButtonLabelReference labelReference = FindLabelReference(button);
            if (labelReference != null)
            {
                string text = AccessibilityText.ReadLocalizedText(labelReference.label);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    return text;
                }
            }

            TooltipProviderComponent tooltipProvider = button.GetComponent<TooltipProviderComponent>();
            string tooltipTitle = TooltipText.FirstTitle(tooltipProvider);
            if (!string.IsNullOrWhiteSpace(tooltipTitle))
            {
                return tooltipTitle;
            }

            return string.Empty;
        }

        private static global::GameSelectableButtonLabelReference FindLabelReference(global::ShinyShoe.GameUISelectableButton button)
        {
            if (button == null)
            {
                return null;
            }

            global::GameSelectableButtonLabelReference direct = button.GetComponent<global::GameSelectableButtonLabelReference>();
            if (HasReadableLabel(direct))
            {
                return direct;
            }

            direct = button.GetComponentInChildren<global::GameSelectableButtonLabelReference>(includeInactive: true);
            if (HasReadableLabel(direct))
            {
                return direct;
            }

            direct = button.GetComponentInParent<global::GameSelectableButtonLabelReference>();
            if (HasReadableLabel(direct))
            {
                return direct;
            }

            Transform buttonTransform = button.transform;
            for (Transform root = buttonTransform.parent; root != null; root = root.parent)
            {
                global::GameSelectableButtonLabelReference best = FindNearestOwnedLabelReference(button, root);
                if (best != null)
                {
                    return best;
                }

                if (root.GetComponent<global::UIScreen>() != null ||
                    root.GetComponent<global::ScreenDialog>() != null ||
                    root.GetComponent<global::GameModeOptionsDialog>() != null)
                {
                    break;
                }
            }

            return null;
        }

        private static global::GameSelectableButtonLabelReference FindNearestOwnedLabelReference(global::ShinyShoe.GameUISelectableButton button, Transform root)
        {
            if (button == null || root == null)
            {
                return null;
            }

            global::GameSelectableButtonLabelReference[] labels =
                root.GetComponentsInChildren<global::GameSelectableButtonLabelReference>(includeInactive: true);
            global::GameSelectableButtonLabelReference best = null;
            int bestDistance = int.MaxValue;
            for (int i = 0; i < labels.Length; i++)
            {
                global::GameSelectableButtonLabelReference label = labels[i];
                if (!HasReadableLabel(label))
                {
                    continue;
                }

                global::ShinyShoe.GameUISelectableButton owningButton = label.GetComponentInParent<global::ShinyShoe.GameUISelectableButton>();
                if (owningButton != null && owningButton != button)
                {
                    continue;
                }

                Transform ancestor = LowestCommonAncestor(button.transform, label.transform);
                if (ancestor == null || ancestor != root && !ancestor.IsChildOf(root))
                {
                    continue;
                }

                int distance = TransformDistance(button.transform, ancestor) + TransformDistance(label.transform, ancestor);
                if (distance < bestDistance)
                {
                    best = label;
                    bestDistance = distance;
                }
            }

            return best;
        }

        private static bool HasReadableLabel(global::GameSelectableButtonLabelReference reference)
        {
            return reference != null && !string.IsNullOrWhiteSpace(AccessibilityText.ReadLocalizedText(reference.label));
        }

        private static Transform LowestCommonAncestor(Transform a, Transform b)
        {
            for (Transform left = a; left != null; left = left.parent)
            {
                for (Transform right = b; right != null; right = right.parent)
                {
                    if (left == right)
                    {
                        return left;
                    }
                }
            }

            return null;
        }

        private static int TransformDistance(Transform child, Transform ancestor)
        {
            int distance = 0;
            for (Transform current = child; current != null && current != ancestor; current = current.parent)
            {
                distance++;
            }

            return distance;
        }

        public static string ResolveState(global::ShinyShoe.GameUISelectableButton button)
        {
            if (button == null)
            {
                return string.Empty;
            }

            if (button.state == global::ShinyShoe.GameUISelectableButton.State.Locked)
            {
                return LocalizationManager.Get("state.locked");
            }

            if (!button.interactable || button.state == global::ShinyShoe.GameUISelectableButton.State.Disabled)
            {
                return LocalizationManager.Get("state.disabled");
            }

            return string.Empty;
        }
    }
}

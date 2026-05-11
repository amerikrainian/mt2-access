using System;
using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI;
using MonsterTrainAccessibility.UI.Elements;
using ShinyShoe;
using TMPro;
using UnityEngine;

namespace MonsterTrainAccessibility.UI.Screens
{
    internal sealed class StartRunOptionsDialogScreen : GameScreen
    {
        private static readonly FieldInfo StandardRunOptionField = AccessTools.Field(typeof(global::StartRunOptionsDialog), "standardRunOption")!;
        private static readonly FieldInfo StandardRunResumeOptionField = AccessTools.Field(typeof(global::StartRunOptionsDialog), "standardRunResumeOption")!;
        private static readonly FieldInfo DailyChallengeOptionField = AccessTools.Field(typeof(global::StartRunOptionsDialog), "dailyChallengeOption")!;
        private static readonly FieldInfo CustomChallengeOptionField = AccessTools.Field(typeof(global::StartRunOptionsDialog), "customChallengeOption")!;
        private static readonly FieldInfo ContinueGameOptionField = AccessTools.Field(typeof(global::StartRunOptionsDialog), "continueGameOption")!;
        private static readonly FieldInfo NewGameOptionField = AccessTools.Field(typeof(global::StartRunOptionsDialog), "newGameOption")!;
        private static readonly FieldInfo BackButtonField = AccessTools.Field(typeof(global::StartRunOptionsDialog), "backButton")!;
        private static readonly FieldInfo PlayButtonField = AccessTools.Field(typeof(global::GameModeOption), "playButton")!;
        private static readonly FieldInfo GameModeDisplayTitleLabelField = AccessTools.Field(typeof(global::GameModeDisplay), "titleLabel")!;
        private static readonly FieldInfo GameModeDisplayDescriptionLabelField = AccessTools.Field(typeof(global::GameModeDisplay), "descriptionLabel")!;
        private static readonly FieldInfo GameModeDisplayNormalGameTitleKeyField = AccessTools.Field(typeof(global::GameModeDisplay), "normalGameTitleKey")!;
        private static readonly FieldInfo GameModeDisplayNormalGameDescriptionKeyField = AccessTools.Field(typeof(global::GameModeDisplay), "normalGameDescriptionKey")!;
        private static readonly FieldInfo ContinueClassesUIField = AccessTools.Field(typeof(global::ContinueGameOption), "classesUI")!;
        private static readonly FieldInfo ContinueCovenantUIField = AccessTools.Field(typeof(global::ContinueGameOption), "covenantUI")!;
        private static readonly FieldInfo ContinueDifficultyTierUIField = AccessTools.Field(typeof(global::ContinueGameOption), "difficultyTierUI")!;
        private static readonly FieldInfo ContinueDistanceUIField = AccessTools.Field(typeof(global::ContinueGameOption), "distanceUI")!;
        private static readonly FieldInfo ContinueEndlessDistanceUIField = AccessTools.Field(typeof(global::ContinueGameOption), "endlessDistanceUI")!;
        private static readonly FieldInfo RunDistanceTooltipProviderField = AccessTools.Field(typeof(global::RunDistanceDisplay), "tooltipProvider")!;
        private static readonly FieldInfo EndlessDistanceTooltipProviderField = AccessTools.Field(typeof(global::EndlessRunDistanceDisplay), "tooltipProvider")!;

        private readonly global::StartRunOptionsDialog _dialog;

        public StartRunOptionsDialogScreen(global::StartRunOptionsDialog dialog)
        {
            _dialog = dialog;
        }

        public override bool ShouldRestoreNavigationFocus() => false;

        public override bool ShouldAcceptGameSelection() => false;

        public override void OnPush()
        {
            base.OnPush();
            ListContainer navigable = RootElement as ListContainer;
            navigable?.FocusFirst();
        }

        public override void OnFocus()
        {
            base.OnFocus();
            ListContainer navigable = RootElement as ListContainer;
            if (navigable == null)
            {
                return;
            }

            if (navigable.FocusIndex >= 0)
            {
                navigable.SetFocusIndex(navigable.FocusIndex);
                return;
            }

            navigable.FocusFirst();
        }

        public override void OnUpdate()
        {
            if (_dialog == null || !_dialog.IsOpen)
            {
                ScreenManager.RemoveFromTree(this);
            }
        }

        protected override void BuildRegistry()
        {
            ListContainer root = new ListContainer
            {
                AnnounceName = false,
                AnnouncePosition = true,
                NavigationAxis = NavigationAxis.Vertical
            };
            RootElement = root;

            Transform resumeGroup = Get<Transform>(_dialog, StandardRunResumeOptionField);
            AddGameModeOption(root, Get<global::GameModeOption>(_dialog, StandardRunOptionField), resumeGroup);
            AddGameModeOption(root, Get<global::ContinueGameOption>(_dialog, ContinueGameOptionField), resumeGroup);
            AddGameModeOption(root, Get<global::GameModeOption>(_dialog, NewGameOptionField), null);
            AddGameModeOption(root, Get<global::GameModeOption>(_dialog, DailyChallengeOptionField), null);
            AddGameModeOption(root, Get<global::GameModeOption>(_dialog, CustomChallengeOptionField), null);
            AddButton(root, Get<GameUISelectableButton>(_dialog, BackButtonField), null);
        }

        private void AddGameModeOption(ListContainer root, global::GameModeOption option, Transform displayRoot)
        {
            if (option == null)
            {
                return;
            }

            global::ProcessingSpinnerButton spinnerButton = Get<global::ProcessingSpinnerButton>(option, PlayButtonField);
            GameUISelectableButton button = spinnerButton?.Button;
            GameModeDisplay display = ResolveDisplay(option, displayRoot);
            AddButton(root, new ProxyStartRunOptionButton(option, button, display), button);
        }

        private void AddButton(ListContainer root, GameUISelectableButton button, Func<Message> label, Func<Message> tooltip = null)
        {
            if (button == null)
            {
                return;
            }

            LabeledButton element = new LabeledButton(
                button,
                label ?? (() => Message.FromText(AuthoredLabelReader.Read(button))));
            AddButton(root, element, button);
        }

        private void AddButton(ListContainer root, UIElement element, GameUISelectableButton button)
        {
            if (button == null || element == null)
            {
                return;
            }

            GameObject target = button.gameObject;
            root.Add(element);
            Register(target, element);
        }

        internal static Message ResolveOptionLabel(global::GameModeOption option, GameUISelectableButton button, GameModeDisplay display)
        {
            if (option is global::ContinueGameOption)
            {
                string continueLabel = ReadAuthoredOptionLabel(option, button);
                if (IsUsableGameLabel(continueLabel))
                {
                    return Message.RawCleaned(continueLabel);
                }

                continueLabel = Message.Clean(GameUIButtonSupport.ResolveLabel(button));
                if (IsUsableGameLabel(continueLabel))
                {
                    return Message.RawCleaned(continueLabel);
                }
            }

            string label = ReadGameModeDisplayText(display, GameModeDisplayTitleLabelField, GameModeDisplayNormalGameTitleKeyField);
            if (!string.IsNullOrWhiteSpace(label))
            {
                return Message.RawCleaned(label);
            }

            label = ReadAuthoredOptionLabel(option, button);
            if (IsUsableGameLabel(label))
            {
                return Message.RawCleaned(label);
            }

            label = Message.Clean(GameUIButtonSupport.ResolveLabel(button));
            if (IsUsableGameLabel(label))
            {
                return Message.RawCleaned(label);
            }

            return null;
        }

        private static string ReadAuthoredOptionLabel(global::GameModeOption option, GameUISelectableButton button)
        {
            string label = Message.Clean(AuthoredLabelReader.Read(button));
            if (!string.IsNullOrWhiteSpace(label))
            {
                return label;
            }

            return Message.Clean(AuthoredLabelReader.Read(option));
        }

        private static GameModeDisplay ResolveDisplay(global::GameModeOption option, Transform displayRoot)
        {
            if (option == null)
            {
                return null;
            }

            GameModeDisplay direct = FirstDisplay(option.transform);
            if (direct != null)
            {
                return direct;
            }

            if (displayRoot != null)
            {
                GameModeDisplay nearest = NearestDisplay(displayRoot, option.transform);
                if (nearest != null)
                {
                    return nearest;
                }
            }

            for (Transform current = option.transform.parent; current != null; current = current.parent)
            {
                GameModeDisplay nearest = NearestDisplay(current, option.transform);
                if (nearest != null)
                {
                    return nearest;
                }

                if (current.GetComponent<global::StartRunOptionsDialog>() != null)
                {
                    break;
                }
            }

            return null;
        }

        private static GameModeDisplay FirstDisplay(Transform root)
        {
            return root != null ? root.GetComponentInChildren<GameModeDisplay>(includeInactive: true) : null;
        }

        private static GameModeDisplay NearestDisplay(Transform root, Transform option)
        {
            if (root == null || option == null)
            {
                return null;
            }

            GameModeDisplay[] displays = root.GetComponentsInChildren<GameModeDisplay>(includeInactive: true);
            GameModeDisplay best = null;
            int bestDistance = int.MaxValue;
            for (int i = 0; i < displays.Length; i++)
            {
                GameModeDisplay display = displays[i];
                if (display == null)
                {
                    continue;
                }

                Transform ancestor = LowestCommonAncestor(display.transform, option);
                if (ancestor == null || ancestor != root && !ancestor.IsChildOf(root))
                {
                    continue;
                }

                int distance = TransformDistance(display.transform, ancestor) + TransformDistance(option, ancestor);
                if (distance < bestDistance)
                {
                    best = display;
                    bestDistance = distance;
                }
            }

            return best;
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

        internal static Message ResolveOptionDetails(global::GameModeOption option, GameModeDisplay display)
        {
            if (option == null)
            {
                return null;
            }

            global::ContinueGameOption continueOption = option as global::ContinueGameOption;
            if (continueOption != null)
            {
                return ResolveContinueDetails(continueOption);
            }

            Message description = Message.RawCleaned(ReadGameModeDisplayText(display, GameModeDisplayDescriptionLabelField, GameModeDisplayNormalGameDescriptionKeyField));
            if (description != null)
            {
                return description;
            }

            return null;
        }

        private static Message ResolveContinueDetails(global::ContinueGameOption option)
        {
            System.Collections.Generic.List<Message> parts = new System.Collections.Generic.List<Message>();

            AddTooltipProvider(parts, Get<ChosenClassesUI>(option, ContinueClassesUIField)?.GetComponent<TooltipProviderComponent>());
            AddTooltipProvider(parts, Get<ChallengeCovenantUI>(option, ContinueCovenantUIField)?.TooltipProvider);
            AddTooltipProvider(parts, Get<DifficultyTierUI>(option, ContinueDifficultyTierUIField)?.TooltipProvider);

            RunDistanceDisplay distance = Get<RunDistanceDisplay>(option, ContinueDistanceUIField);
            AddTooltipProvider(parts, Get<TooltipProviderComponent>(distance, RunDistanceTooltipProviderField));

            EndlessRunDistanceDisplay endlessDistance = Get<EndlessRunDistanceDisplay>(option, ContinueEndlessDistanceUIField);
            AddTooltipProvider(parts, Get<TooltipProviderComponent>(endlessDistance, EndlessDistanceTooltipProviderField));

            return parts.Count > 0 ? Message.JoinLines(parts) : null;
        }

        private static string ReadGameModeDisplayText(GameModeDisplay display, FieldInfo labelField, FieldInfo keyField)
        {
            string key = Get<string>(display, keyField);
            if (!string.IsNullOrWhiteSpace(key))
            {
                return Message.Clean(AccessibilityLocalizationScope.Run(() => I2.Loc.LocalizationManager.GetTranslation(key)));
            }

            string text = ReadDisplayText(Get<TMP_Text>(display, labelField));
            if (!string.IsNullOrWhiteSpace(text))
            {
                return text;
            }

            return string.Empty;
        }

        private static void AddTooltipProvider(System.Collections.Generic.List<Message> parts, TooltipProviderComponent provider)
        {
            if (provider?.Tooltips == null)
            {
                return;
            }

            for (int i = 0; i < provider.Tooltips.Count; i++)
            {
                TooltipContent tooltip = provider.Tooltips[i];
                if (tooltip.IsEmpty())
                {
                    continue;
                }

                Message entry = TooltipEntry(tooltip);
                if (entry != null)
                {
                    parts.Add(entry);
                }
            }
        }

        private static Message TooltipEntry(TooltipContent tooltip)
        {
            Message title = Message.RawCleaned(tooltip.title);
            Message body = Message.RawCleaned(tooltip.body);
            if (title != null && body != null)
            {
                return Message.Join(", ", title, body);
            }

            return title ?? body;
        }

        private static string ReadDisplayText(TMP_Text text)
        {
            if (text == null)
            {
                return string.Empty;
            }

            I2.Loc.Localize localize = text.GetComponent<I2.Loc.Localize>();
            string localized = TranslateLocalizeComponent(localize);
            if (!string.IsNullOrWhiteSpace(localized))
            {
                return localized;
            }

            return Message.Clean(text.text);
        }

        private static string TranslateLocalizeComponent(I2.Loc.Localize localize)
        {
            if (localize == null)
            {
                return string.Empty;
            }

            string term = !string.IsNullOrWhiteSpace(localize.FinalTerm) ? localize.FinalTerm : localize.Term;
            if (string.IsNullOrWhiteSpace(term))
            {
                return string.Empty;
            }

            string translation = AccessibilityLocalizationScope.Run(() => I2.Loc.LocalizationManager.GetTranslation(term));
            return Message.Clean(translation);
        }

        private static bool IsGenericButtonLabel(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            string cleaned = Message.Clean(value);
            string buttonRole = LocalizationManager.Get("role.button");
            return string.Equals(cleaned, "Button", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(cleaned, buttonRole, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(cleaned, "Play", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(cleaned, "Locked", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(cleaned, LocalizationManager.Get("state.locked"), StringComparison.OrdinalIgnoreCase) ||
                string.Equals(cleaned, LocalizationManager.Get("state.disabled"), StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsUsableGameLabel(string value)
        {
            return IsReadableLabel(value) && !IsGenericButtonLabel(value);
        }

        private static bool IsReadableLabel(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            for (int i = 0; i < value.Length; i++)
            {
                if (char.IsLetter(value[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static T Get<T>(object owner, FieldInfo field) where T : class
        {
            return owner != null ? field.GetValue(owner) as T : null;
        }
    }
}

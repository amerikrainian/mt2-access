using System.Reflection;
using MonsterTrainAccessibility.Util;
using System.Collections.Generic;
using MonsterTrainAccessibility.Buffers;
using HarmonyLib;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Speech;
using MonsterTrainAccessibility.UI.Elements;
using ShinyShoe;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MonsterTrainAccessibility.UI.Screens
{
    internal sealed class BattleIntroScreen : GameScreen
    {
        private static readonly FieldInfo TrialInfoUIField = AccessTools.Field(typeof(global::BattleIntroScreen), "trialInfoUI")!;
        private static readonly FieldInfo TrialToggleButtonField = AccessTools.Field(typeof(global::BattleIntroScreen), "trialToggleButton")!;
        private static readonly FieldInfo FightButtonField = AccessTools.Field(typeof(global::BattleIntroScreen), "fightButton")!;
        private static readonly FieldInfo IsFightButtonAvailableField = AccessTools.Field(typeof(global::BattleIntroScreen), "isFightButtonAvailable")!;
        private static readonly FieldInfo SaveManagerField = AccessTools.Field(typeof(global::BattleIntroScreen), "saveManager")!;
        private static readonly FieldInfo TrialDataField = AccessTools.Field(typeof(global::BattleIntroScreen), "trialData")!;
        private static readonly FieldInfo RelicInfoUIsField = AccessTools.Field(typeof(global::BattleIntroScreen), "relicInfoUIs")!;
        private static readonly FieldInfo BossSinRelicInfoUIsField = AccessTools.Field(typeof(global::BattleIntroScreen), "bossSinRelicInfoUIs")!;
        private static readonly FieldInfo EnemyDisplaysField = AccessTools.Field(typeof(global::BattleIntroScreen), "enemyDisplays")!;
        private static readonly FieldInfo BigBossDisplayField = AccessTools.Field(typeof(global::BattleIntroScreen), "bigBossDisplay")!;
        private static readonly FieldInfo TrueFinalBossDisplaysField = AccessTools.Field(typeof(global::BattleIntroScreen), "trueFinalBossDisplays")!;
        private static readonly FieldInfo BattleNameLabelField = AccessTools.Field(typeof(global::BattleIntroScreen), "battleNameLabel")!;
        private static readonly FieldInfo BattleDescriptionLabelField = AccessTools.Field(typeof(global::BattleIntroScreen), "battleDescriptionLabel")!;
        private static readonly FieldInfo BattleIntroEnemyTooltipProviderField = AccessTools.Field(typeof(global::BattleIntroEnemy), "tooltipProvider")!;
        private static readonly FieldInfo TrialInfoRewardTitleLabelField = AccessTools.Field(typeof(global::TrialInfoUI), "rewardTitleLabel")!;
        private static readonly FieldInfo ShowTitanTrialField = AccessTools.Field(typeof(global::BattleIntroScreen), "showTitanTrial")!;

        private readonly global::BattleIntroScreen _screen;
        private UIElement _scenarioElement;
        private UIElement _fightButtonElement;
        private UIElement _trialToggleElement;
        private bool _allowManualFocusAnnouncements;
        private bool _initialFocusApplied;
        private bool _scenarioAnnounced;
        private int _blockSubmitUntilFrame;

        public BattleIntroScreen(global::BattleIntroScreen screen)
        {
            _screen = screen;
            ClaimAction("buffer_prev_item");
            ClaimAction("buffer_next_item");
            ClaimAction("buffer_prev");
            ClaimAction("buffer_next");
        }

        public override void OnPush()
        {
            base.OnPush();
            _allowManualFocusAnnouncements = false;
            _initialFocusApplied = false;
            _scenarioAnnounced = false;
            _blockSubmitUntilFrame = Time.frameCount + 12;
        }

        public override void OnUpdate()
        {
            if (!_initialFocusApplied && IsStableForInitialAnnouncement())
            {
                ListContainer root = RootElement as ListContainer;
                if (_scenarioElement != null)
                {
                    root?.SetFocusTo(_scenarioElement, selectForNavigation: false);
                }
                else
                {
                    root?.FocusFirst();
                }

                Core.UIManager.RefreshBuffersFor(root?.FocusedChild);
                _initialFocusApplied = true;
            }

            if (_scenarioAnnounced || !IsReadyForScenarioAnnouncement())
            {
                return;
            }

            Message scenario = _scenarioElement?.GetLabel();
            if (scenario != null)
            {
                SpeechManager.Output(scenario);
            }

            _scenarioAnnounced = true;
        }

        public override bool ShouldAnnounceFocus(UIElement element) => _allowManualFocusAnnouncements;

        public override bool ShouldRestoreNavigationFocus() => false;

        public override bool ShouldAcceptGameSelection() => _allowManualFocusAnnouncements;

        public override bool BlocksGameInput(Input.InputAction action)
        {
            if (action?.Key == "ui_accept" || action?.Key == "ui_select")
            {
                if (Time.frameCount <= _blockSubmitUntilFrame)
                {
                    return true;
                }

                return !IsNativeSubmitFocused();
            }

            return base.BlocksGameInput(action);
        }

        public override bool OnActionJustPressed(Input.InputAction action)
        {
            if ((action?.Key == "ui_accept" || action?.Key == "ui_select") && IsNativeSubmitFocused())
            {
                return true;
            }

            if (_initialFocusApplied && IsDirectionalNavigationAction(action?.Key))
            {
                _allowManualFocusAnnouncements = true;
            }

            if (IsBufferNavigationAction(action?.Key))
            {
                return HandleBufferNavigation(action.Key);
            }

            return base.OnActionJustPressed(action);
        }

        protected override void BuildRegistry()
        {
            ListContainer root = new ListContainer
            {
                AnnounceName = false,
                AnnouncePosition = false,
                NavigationAxis = NavigationAxis.Vertical
            };
            RootElement = root;
            _fightButtonElement = null;
            _trialToggleElement = null;

            AddScenario(root);
            AddEnemies(root);
            AddTrial(root, Get<TrialInfoUI>(_screen, TrialInfoUIField), Get<GameUISelectableToggle>(_screen, TrialToggleButtonField));
            AddButton(root, Get<GameUISelectableButton>(_screen, FightButtonField));
        }

        private void AddScenario(ListContainer root)
        {
            SaveManager saveManager = Get<SaveManager>(_screen, SaveManagerField);
            ScenarioData scenario = saveManager?.GetCurrentScenarioData();
            if (scenario == null)
            {
                return;
            }

            TMP_Text titleLabel = Get<TMP_Text>(_screen, BattleNameLabelField);
            global::MultilineTextFitter descriptionFitter = Get<global::MultilineTextFitter>(_screen, BattleDescriptionLabelField);
            TMP_Text descriptionLabel = descriptionFitter != null ? descriptionFitter.GetComponent<TMP_Text>() : null;
            GameObject titleTarget = titleLabel != null ? titleLabel.gameObject : _screen.gameObject;

            ProxyBattleIntroScenarioTitle element = new ProxyBattleIntroScenarioTitle(
                titleTarget,
                titleLabel,
                scenario);
            _scenarioElement = element;
            root.Add(element);
            Register(_screen.gameObject, element);
            Register(titleTarget, element);

            ProxyBattleIntroScenarioDescription description = new ProxyBattleIntroScenarioDescription(
                descriptionLabel,
                scenario,
                _screen.gameObject);
            root.Add(description);
            Register(descriptionLabel != null ? descriptionLabel.gameObject : null, description);
        }

        private void AddEnemies(ListContainer root)
        {
            AddEnemyList(root, Get<List<BattleIntroEnemy>>(_screen, EnemyDisplaysField));
            AddEnemy(root, Get<BattleIntroEnemy>(_screen, BigBossDisplayField));
            AddEnemyList(root, Get<List<BattleIntroEnemy>>(_screen, TrueFinalBossDisplaysField));
        }

        private void AddEnemyList(ListContainer root, List<BattleIntroEnemy> enemies)
        {
            if (enemies == null)
            {
                return;
            }

            for (int i = 0; i < enemies.Count; i++)
            {
                AddEnemy(root, enemies[i]);
            }
        }

        private void AddEnemy(ListContainer root, BattleIntroEnemy enemy)
        {
            if (enemy == null)
            {
                return;
            }

            ProxyBattleIntroEnemy element = new ProxyBattleIntroEnemy(enemy);
            root.Add(element);
            Register(enemy.gameObject, element);
        }

        private void AddTrial(ListContainer root, TrialInfoUI trialInfo, GameUISelectableToggle toggle)
        {
            if (trialInfo == null && toggle == null)
            {
                return;
            }

            ProxyBattleIntroTrialToggle element = new ProxyBattleIntroTrialToggle(_screen, trialInfo, toggle);
            _trialToggleElement = toggle != null ? element : null;
            root.Add(element);
            Register(toggle != null ? toggle.gameObject : null, element);
            Register(trialInfo != null ? trialInfo.gameObject : null, element);
            if (toggle != null)
            {
                RegisterOwnedSelectableTargets(toggle.gameObject, element);
            }
        }

        private void AddButton(ListContainer root, GameUISelectableButton button)
        {
            if (button == null)
            {
                return;
            }

            ProxyBattleIntroFightButton element = new ProxyBattleIntroFightButton(button);
            _fightButtonElement = element;
            root.Add(element);
            Register(button.gameObject, element);
        }

        private static T Get<T>(object owner, FieldInfo field) where T : class
        {
            return owner != null ? field.GetValue(owner) as T : null;
        }

        private void RegisterOwnedSelectableTargets(GameObject owner, UIElement element)
        {
            if (owner == null || element == null)
            {
                return;
            }

            MonoBehaviour[] behaviours = owner.GetComponentsInChildren<MonoBehaviour>(includeInactive: true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IGameUIComponent component && component.component != null)
                {
                    Register(component.component.gameObject, element);
                }
            }
        }

        internal static Message FightButtonLabel(GameUISelectableButton button)
        {
            return Message.FromText(FirstText(
                GameUIButtonSupport.ResolveLabel(button),
                LocalizeTerm("ScreenBattleIntro_Button")));
        }

        internal static string ScenarioDescription(TMP_Text descriptionLabel, ScenarioData scenario)
        {
            return FirstText(
                AccessibilityText.ReadLocalizedText(descriptionLabel),
                scenario != null ? scenario.GetBattleDescription() : string.Empty);
        }

        internal static Message EnemyLabel(BattleIntroEnemy enemy)
        {
            TooltipProviderComponent provider = Get<TooltipProviderComponent>(enemy, BattleIntroEnemyTooltipProviderField);
            string title = TooltipText.FirstTitle(provider);
            return Message.FromText(title);
        }

        internal static Message EnemyTooltip(BattleIntroEnemy enemy)
        {
            TooltipProviderComponent provider = Get<TooltipProviderComponent>(enemy, BattleIntroEnemyTooltipProviderField);
            return TooltipText.ForComponent(provider);
        }

        internal static Message TrialRewardLabel(global::BattleIntroScreen screen, TrialInfoUI trialInfo)
        {
            TMP_Text rewardTitleLabel = Get<TMP_Text>(trialInfo, TrialInfoRewardTitleLabelField);
            string authoredTitle = AccessibilityText.ReadLocalizedText(rewardTitleLabel);
            if (!string.IsNullOrWhiteSpace(authoredTitle))
            {
                return Message.FromText(authoredTitle);
            }

            TrialData trialData = Get<TrialData>(screen, TrialDataField);
            SaveManager saveManager = Get<SaveManager>(screen, SaveManagerField);
            TrialInfo activeTrialInfo = saveManager?.GetCurrentActiveTrialInfo();
            RewardData reward = trialData != null && activeTrialInfo != null ? trialData.GetGrantableReward(activeTrialInfo) : null;
            if (reward != null)
            {
                return RewardLabel(reward);
            }

            return null;
        }

        internal static Message TrialToggleLabel(global::BattleIntroScreen screen, GameUISelectableToggle toggle)
        {
            TrialData trialData = Get<TrialData>(screen, TrialDataField);
            SinsData sin = trialData?.Sin;
            SaveManager saveManager = Get<SaveManager>(screen, SaveManagerField);
            if (sin == null)
            {
                sin = TitanTrialSin(screen, saveManager);
            }

            if (sin != null)
            {
                return Message.FromText(FormatLabelValue(FirstText(LocalizeTerm("ScreenBattleIntro_TrialTitle"), LocalizeTerm("Trial")), sin.GetName()));
            }

            Message tooltip = TrialToggleTooltip(screen, toggle);
            return Message.FromText(FormatLabelValue(FirstText(LocalizeTerm("ScreenBattleIntro_TrialTitle"), LocalizeTerm("Trial")), tooltip?.Resolve()));
        }

        internal static Message TrialToggleTooltip(global::BattleIntroScreen screen, GameUISelectableToggle toggle)
        {
            TrialData trialData = Get<TrialData>(screen, TrialDataField);
            SinsData sin = trialData?.Sin;
            SaveManager saveManager = Get<SaveManager>(screen, SaveManagerField);
            if (sin == null)
            {
                sin = TitanTrialSin(screen, saveManager);
            }

            if (sin != null)
            {
                int tier = saveManager != null ? saveManager.RegionRunDifficultyTier() : 0;
                return Message.FromText(sin.GetScaledDescription(tier));
            }

            return toggle != null ? AccessibleScreenText.Tooltip(toggle.transform) : null;
        }

        private static SinsData TitanTrialSin(global::BattleIntroScreen screen, SaveManager saveManager)
        {
            if (screen == null || saveManager == null || !(bool)ShowTitanTrialField.GetValue(screen))
            {
                return null;
            }

            return saveManager.GetCurrentScenarioData()?.GetTitanTrialSin() as SinsData;
        }

        internal static List<Message> TrialRelicMessages(global::BattleIntroScreen screen)
        {
            List<Message> parts = new List<Message>();
            AddTrialRelicMessages(parts, Get<List<RelicInfoUI>>(screen, RelicInfoUIsField));
            AddTrialRelicMessages(parts, Get<List<RelicInfoUI>>(screen, BossSinRelicInfoUIsField));
            return parts;
        }

        private static void AddTrialRelicMessages(List<Message> parts, List<RelicInfoUI> relics)
        {
            if (parts == null || relics == null)
            {
                return;
            }

            for (int i = 0; i < relics.Count; i++)
            {
                RelicInfoUI relic = relics[i];
                Message detail = TrialRelicMessage(relic);
                if (detail != null)
                {
                    parts.Add(detail);
                }
            }
        }

        private static Message TrialRelicMessage(RelicInfoUI relic)
        {
            List<Message> parts = MessageList.Dedupe(ProxyRelicInfo.ExtraTooltipParts(relic));
            if (parts.Count == 0)
            {
                return null;
            }

            List<Message> filtered = new List<Message>();
            for (int i = 0; i < parts.Count; i++)
            {
                string candidate = Message.Clean(parts[i]?.Resolve());
                if (string.IsNullOrWhiteSpace(candidate))
                {
                    continue;
                }

                bool redundant = false;
                for (int j = 0; j < parts.Count; j++)
                {
                    if (i == j)
                    {
                        continue;
                    }

                    string other = Message.Clean(parts[j]?.Resolve());
                    if (string.IsNullOrWhiteSpace(other) ||
                        other.Length >= candidate.Length)
                    {
                        continue;
                    }

                    if (candidate.IndexOf(other, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        redundant = true;
                        break;
                    }
                }

                if (!redundant)
                {
                    filtered.Add(parts[i]);
                }
            }

            return filtered.Count > 0 ? Message.JoinLines(filtered) : null;
        }

        private static Message RewardLabel(RewardData reward)
        {
            if (reward is GrantableRewardData grantableReward)
            {
                return ProxyRewardItem.Label(grantableReward);
            }

            List<Message> parts = new List<Message>();
            string title = FirstText(reward.RewardTitle, reward.RewardDescription);
            AddPart(parts, title);
            if (!string.Equals(Message.Clean(title), Message.Clean(reward.RewardDescription), System.StringComparison.OrdinalIgnoreCase))
            {
                AddPart(parts, reward.RewardDescription);
            }
            return parts.Count > 0 ? Message.Join(", ", parts) : null;
        }

        private static string FormatLabelValue(string label, string value)
        {
            label = Message.Clean(label);
            value = Message.Clean(value);
            if (string.IsNullOrWhiteSpace(label))
            {
                return value;
            }
            if (string.IsNullOrWhiteSpace(value))
            {
                return label;
            }

            string format = LocalizeTerm("TextFormat_Colon");
            return !string.IsNullOrWhiteSpace(format)
                ? Message.Clean(string.Format(format, label, value))
                : Message.Clean(label + ": " + value);
        }

        internal static string FirstText(params string[] values)
        {
            if (values == null)
            {
                return string.Empty;
            }

            for (int i = 0; i < values.Length; i++)
            {
                string value = Message.Clean(values[i]);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return string.Empty;
        }

        private static string LocalizeTerm(string term)
        {
            return !string.IsNullOrWhiteSpace(term) && term.HasTranslation()
                ? AccessibilityText.LocalizeTerm(term)
                : string.Empty;
        }

        private static void AddPart(List<Message> parts, string text)
        {
            if (!string.IsNullOrWhiteSpace(text))
            {
                parts.Add(Message.RawCleaned(text));
            }
        }

        private bool IsStableForInitialAnnouncement()
        {
            GameUISelectableButton fightButton = Get<GameUISelectableButton>(_screen, FightButtonField);
            bool available = (bool?)IsFightButtonAvailableField.GetValue(_screen) == true;
            return available && fightButton != null && fightButton.gameObject.activeInHierarchy;
        }

        private bool IsReadyForScenarioAnnouncement()
        {
            if (!IsStableForInitialAnnouncement())
            {
                return false;
            }

            GameUISelectableButton fightButton = Get<GameUISelectableButton>(_screen, FightButtonField);
            GameObject selected = EventSystem.current?.currentSelectedGameObject;
            return fightButton != null && ReferenceEquals(selected, fightButton.gameObject);
        }

        private static bool IsDirectionalNavigationAction(string actionKey)
        {
            switch (actionKey)
            {
                case "ui_up":
                case "ui_down":
                case "ui_left":
                case "ui_right":
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsBufferNavigationAction(string actionKey)
        {
            switch (actionKey)
            {
                case "buffer_prev_item":
                case "buffer_next_item":
                case "buffer_prev":
                case "buffer_next":
                    return true;
                default:
                    return false;
            }
        }

        private bool IsFightButtonFocused()
        {
            ListContainer root = RootElement as ListContainer;
            return ReferenceEquals(root?.FocusedChild, _fightButtonElement);
        }

        private bool IsTrialToggleFocused()
        {
            ListContainer root = RootElement as ListContainer;
            return _trialToggleElement != null && ReferenceEquals(root?.FocusedChild, _trialToggleElement);
        }

        private bool IsNativeSubmitFocused()
        {
            return IsFightButtonFocused() || IsTrialToggleFocused();
        }

        private static bool HandleBufferNavigation(string actionKey)
        {
            switch (actionKey)
            {
                case "buffer_prev_item":
                    BufferManager.Instance.PreviousItem();
                    return true;
                case "buffer_next_item":
                    BufferManager.Instance.NextItem();
                    return true;
                case "buffer_prev":
                    BufferManager.Instance.PreviousBuffer();
                    return true;
                case "buffer_next":
                    BufferManager.Instance.NextBuffer();
                    return true;
                default:
                    return false;
            }
        }
    }
}

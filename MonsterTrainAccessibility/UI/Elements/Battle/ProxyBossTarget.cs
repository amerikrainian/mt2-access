using System;
using System.Collections.Generic;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.UI;
using MonsterTrainAccessibility.Util;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyBossTarget : GameObjectElement
    {
        private readonly global::BossTargetUI _target;
        private readonly bool _currentOnly;

        public ProxyBossTarget(global::BossTargetUI target, bool currentOnly)
            : base(
                target: target != null ? target.gameObject : null,
                typeKey: null,
                label: null)
        {
            _target = target;
            _currentOnly = currentOnly;
        }

        public override bool IsVisible => _target != null && _target.gameObject.activeInHierarchy && (!_currentOnly || IsCurrentBossTarget(_target));
        public override Message GetLabel() => Label(_target);
        public override Message GetStatusString() => _currentOnly || IsCurrentBossTarget(_target) ? ProxyCombatCreature.CurrentBossIntent().Status : null;
        public override Message GetTooltip() => Tooltip(_target, _currentOnly || IsCurrentBossTarget(_target));

        public static Message Label(global::BossTargetUI target)
        {
            string title = Title(target);
            return !string.IsNullOrWhiteSpace(title)
                ? Message.FromText(title)
                : Message.Localized("ui", "HUD.BOSS_TARGET");
        }

        public static Message Tooltip(global::BossTargetUI target, bool includeIntent)
        {
            List<Message> parts = new List<Message>();
            if (includeIntent)
            {
                MessageList.Add(parts, ProxyCombatCreature.CurrentBossIntent().Tooltip);
            }

            MessageList.Add(parts, target != null ? TooltipText.ForComponent(target) : null);
            return parts.Count > 0 ? Message.JoinLines(parts) : null;
        }

        public static bool IsCurrentBossTarget(global::BossTargetUI target)
        {
            SaveManager saveManager = GameManagers.GetSaveManager();
            if (target == null || saveManager == null || !saveManager.IsInBattle())
            {
                return false;
            }

            ScenarioData currentScenario = saveManager.GetCurrentScenarioData();
            if (currentScenario == null)
            {
                return false;
            }

            string currentTitle = Message.Clean(currentScenario.GetBattleName());
            string targetTitle = Title(target);
            return !string.IsNullOrWhiteSpace(currentTitle) &&
                string.Equals(currentTitle, targetTitle, StringComparison.OrdinalIgnoreCase);
        }

        public static string Title(global::BossTargetUI target)
        {
            if (target == null)
            {
                return string.Empty;
            }

            TooltipProviderComponent provider = target.GetComponent<TooltipProviderComponent>();
            string title = TooltipText.FirstTitle(provider);
            return Message.Clean(title);
        }
    }
}

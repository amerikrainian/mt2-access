using System;
using System.Collections.Generic;
using MonsterTrainAccessibility.Buffers;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Util;
using ShinyShoe;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyRegionSelectionButton : ButtonProxy
    {
        private readonly RegionSelectionButton _button;
        private readonly ScenarioData _scenario;
        private readonly SaveManager _saveManager;
        private readonly bool _finalBoss;
        private readonly Func<RegionSelectionButton> _selectedButton;
        private readonly Action<RegionSelectionButton, bool> _selectButton;
        private readonly Func<Message> _lockedTooltip;

        public ProxyRegionSelectionButton(
            RegionSelectionButton button,
            ScenarioData scenario,
            SaveManager saveManager,
            bool finalBoss,
            Func<RegionSelectionButton> selectedButton,
            Action<RegionSelectionButton, bool> selectButton,
            Func<Message> lockedTooltip = null)
            : base(button?.Button)
        {
            _button = button;
            _scenario = scenario;
            _saveManager = saveManager;
            _finalBoss = finalBoss;
            _selectedButton = selectedButton;
            _selectButton = selectButton;
            _lockedTooltip = lockedTooltip;
        }

        public override bool IsVisible => _button != null && _button.gameObject.activeInHierarchy;

        public override Message GetLabel()
        {
            Message boss = Message.FromText(_scenario?.GetBattleName());
            if (_finalBoss)
            {
                return Message.Localized("ui", "REGION_SELECTION.FINAL_BOSS", new { boss = boss?.Resolve() ?? string.Empty });
            }

            return Message.Localized("ui", "REGION_SELECTION.REGION", new
            {
                index = _button.RegionIndex + 1,
                boss = boss?.Resolve() ?? string.Empty
            });
        }

        public override Message GetStatusString()
        {
            List<Message> parts = new List<Message>();
            if (ReferenceEquals(_selectedButton?.Invoke(), _button))
            {
                MessageList.Add(parts, Message.Localized("messages", "state.selected"));
            }

            if (_finalBoss)
            {
                if (_button.Button?.interactable != true)
                {
                    MessageList.Add(parts, Message.Localized("messages", "state.locked"));
                }
            }
            else if (_saveManager?.HasRegionBeenVisited(_button.RegionIndex) == true)
            {
                MessageList.Add(parts, Message.Localized("ui", "REGION_SELECTION.DEFEATED"));
            }
            else if (_button.Button?.interactable == false)
            {
                MessageList.Add(parts, Message.Localized("messages", "state.disabled"));
            }

            return parts.Count > 0 ? Message.Join(", ", parts) : null;
        }

        public override Message GetTooltip()
        {
            List<Message> parts = new List<Message>();
            MessageList.Add(parts, Message.FromText(_scenario?.GetBattleDescription()));
            MessageList.Add(parts, EnemyRelicSummary());

            if (_finalBoss && _button.Button?.interactable != true)
            {
                MessageList.Add(parts, _lockedTooltip?.Invoke());
            }

            return parts.Count > 0 ? Message.JoinLines(parts) : null;
        }

        public override bool Activate()
        {
            base.SelectForNavigation();
            SelectRegion();
            UIManager.ForceReannounceCurrentFocus();
            return true;
        }

        public override void SelectForNavigation()
        {
            // Region choice should be explicit. Arrow navigation inspects a region;
            // Enter selects it and updates the reward preview.
        }

        internal override string HandleBuffers(BufferManager buffers)
        {
            base.HandleBuffers(buffers);
            return "ui";
        }

        private void SelectRegion()
        {
            if (_button?.Button?.CanBeActivated() == true)
            {
                _selectButton?.Invoke(_button, _finalBoss);
            }
        }

        private Message EnemyRelicSummary()
        {
            RelicState state = EnemyRelicState();
            if (state == null)
            {
                return null;
            }

            List<Message> parts = new List<Message>
            {
                Message.Localized("ui", "REGION_SELECTION.BOSS_RELIC", new { relic = state.GetName() }),
                ProxyRelicInfo.Tooltip(state, includeDynamicInfo: true)
            };
            return Message.JoinLines(MessageList.Dedupe(parts));
        }

        private RelicState EnemyRelicState()
        {
            RelicData[] relics = _scenario?.GetEnemyRelicData();
            return relics != null && relics.Length > 0 && relics[0] != null
                ? new RelicState(relics[0])
                : null;
        }
    }
}

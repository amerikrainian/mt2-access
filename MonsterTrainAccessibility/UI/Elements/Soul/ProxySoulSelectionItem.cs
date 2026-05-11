using System.Collections.Generic;
using MonsterTrainAccessibility.Buffers;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Presentation.Relics;
using MonsterTrainAccessibility.Util;
using ShinyShoe;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxySoulSelectionItem : GameObjectElement
    {
        private readonly GameUISelectableButton _button;
        private readonly System.Func<SoulState> _state;
        private readonly System.Func<SoulData> _data;
        private readonly System.Func<bool> _isLocked;
        private readonly System.Func<bool> _isChosen;
        private readonly SaveManager _saveManager;

        public ProxySoulSelectionItem(global::SoulSelectionItemUI item, SaveManager saveManager)
            : this(
                item?.Button,
                () => item?.SoulState,
                () => item?.SoulData,
                () => item?.IsLocked == true,
                () => item?.Chosen == true,
                saveManager)
        {
        }

        public ProxySoulSelectionItem(global::RunSetupSoulSelectionItemUI item, SaveManager saveManager)
            : this(
                item?.Button,
                () => item?.SoulState,
                () => item?.SoulData,
                () => item?.IsLocked == true,
                () => item?.Chosen == true,
                saveManager)
        {
        }

        private ProxySoulSelectionItem(
            GameUISelectableButton button,
            System.Func<SoulState> state,
            System.Func<SoulData> data,
            System.Func<bool> isLocked,
            System.Func<bool> isChosen,
            SaveManager saveManager)
            : base(
                button != null ? button.gameObject : null,
                typeKey: "button",
                label: null)
        {
            _button = button;
            _state = state;
            _data = data;
            _isLocked = isLocked;
            _isChosen = isChosen;
            _saveManager = saveManager;
        }

        public GameUISelectableButton Button => _button;
        public override bool IsVisible => _button != null && _button.gameObject.activeInHierarchy;
        public override Message GetLabel() => Message.FromText(_state()?.GetName() ?? _data()?.GetName());

        public override Message GetStatusString()
        {
            if (_isLocked())
            {
                return Message.Localized("messages", "state.locked");
            }

            if (_isChosen())
            {
                return Message.Localized("messages", "state.selected");
            }

            return GameButtonElement.StateMessage(_button);
        }

        public override Message GetTooltip()
        {
            List<Message> parts = new List<Message>();
            MessageList.Add(parts, Message.FromText(_data()?.GetDescription()));
            AddSoulContext(parts);
            return parts.Count > 0 ? Message.JoinLines(parts) : null;
        }

        internal override string HandleBuffers(BufferManager buffers)
        {
            base.HandleBuffers(buffers);
            PresentationBuffer<RelicPresentationSource> buffer = buffers?.GetBuffer("relic") as PresentationBuffer<RelicPresentationSource>;
            SoulState state = _state();
            if (buffer == null || state == null)
            {
                return "ui";
            }

            List<Message> context = new List<Message>();
            AddSoulContext(context);
            buffer.Bind(RelicPresentationSource.FromState(state, includeDynamicInfo: false), context);
            buffers.EnableBuffer("relic", true);
            return "relic";
        }

        private void AddSoulContext(List<Message> parts)
        {
            SoulData data = _data();
            if (data == null)
            {
                return;
            }

            MessageList.Add(parts, Message.Localized("ui", "SOUL.TIER", new { tier = data.GetTierLevel() }));
            MessageList.Add(parts, UnlockContext(data));
        }

        private Message UnlockContext(SoulData data)
        {
            if (_saveManager == null || data == null)
            {
                return null;
            }

            if (!SoulHelper.HasUnlockedSoul(data, _saveManager) && data.GetTierLevel() == 1)
            {
                return UnlockText(data.GetUnlockCriteria());
            }

            SoulData nextTier = data.GetNextTier();
            if (nextTier != null)
            {
                return UnlockText(nextTier.GetUnlockCriteria());
            }

            return null;
        }

        private Message UnlockText(UnlockCriteria criteria)
        {
            if (criteria == null)
            {
                return null;
            }

            List<Message> parts = new List<Message>
            {
                Message.FromText(AccessibilityText.LocalizeTerm(criteria.GetDescriptionKey(), criteria))
            };

            if (_saveManager != null && _saveManager.TryGetUnlockCriteriaProgress(criteria, out int currentValue, out int unlockValue))
            {
                parts.Add(Message.FromText(string.Format(AccessibilityText.LocalizeTerm("TextFormat_Divide"), currentValue, unlockValue)));
            }

            parts.RemoveAll(part => part == null);
            return parts.Count > 0 ? Message.Join(" ", parts) : null;
        }
    }
}

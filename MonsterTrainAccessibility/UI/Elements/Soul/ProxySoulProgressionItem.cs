using System.Collections.Generic;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Util;
using ShinyShoe;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxySoulProgressionItem : GameObjectElement
    {
        private readonly global::GameOverScreen.SoulUnlockProgressInfo _info;
        private readonly SaveManager _saveManager;

        public ProxySoulProgressionItem(
            global::GameOverScreen.SoulUnlockProgressInfo info,
            GameUISelectableWithNavigation selectable,
            SaveManager saveManager)
            : base(selectable != null ? selectable.gameObject : null, typeKey: null, label: null)
        {
            _info = info;
            _saveManager = saveManager;
        }

        public override bool IsVisible => Soul != null;

        public override Message GetLabel()
        {
            SoulData soul = Soul;
            if (soul == null)
            {
                return null;
            }

            if (_info.isUnlock)
            {
                return Message.Localized("ui", "SOUL_PROGRESSION.UNLOCKED", new { soul = soul.GetName() });
            }

            if (_info.isLevelUp)
            {
                return Message.Localized("ui", "SOUL_PROGRESSION.LEVEL_UP", new { soul = soul.GetName() });
            }

            return Message.FromText(soul.GetName());
        }

        public override Message GetStatusString()
        {
            if (_info.unlockValue <= 0)
            {
                return null;
            }

            return Message.Localized("ui", "SOUL_PROGRESSION.PROGRESS", new
            {
                current = System.Math.Min(_info.currentValue, _info.unlockValue),
                total = _info.unlockValue
            });
        }

        public override Message GetTooltip()
        {
            SoulData soul = Soul;
            if (soul == null)
            {
                return null;
            }

            List<Message> parts = new List<Message>
            {
                Message.FromText(soul.GetDescription()),
                Message.Localized("ui", "SOUL.TIER", new { tier = soul.GetTierLevel() })
            };

            MessageList.Add(parts, UnlockText(UnlockCriteria));
            return parts.Count > 0 ? Message.JoinLines(parts) : null;
        }

        private SoulData Soul => _info.unlockObj as SoulData;

        private UnlockCriteria UnlockCriteria
        {
            get
            {
                if (_info.isNextCriteria && _info.nextUnlockObj is SoulData nextSoul)
                {
                    return nextSoul.GetUnlockCriteria();
                }

                return Soul?.GetUnlockCriteria();
            }
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

            return Message.Join(" ", parts);
        }
    }
}

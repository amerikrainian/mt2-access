using MonsterTrainAccessibility.UI.Screens;
using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Localization;

namespace MonsterTrainAccessibility.UI.Elements.Compendium
{
    internal sealed class ProxyCompendiumCardFrameOption : ProxyCompendiumGameButton
    {
        private static readonly FieldInfo UnlockCriteriaField = AccessTools.Field(typeof(global::CardFrameOptionUI), "unlockCriteria")!;

        private readonly global::CardFrameOptionUI _frame;

        public ProxyCompendiumCardFrameOption(global::CardFrameOptionUI frame)
            : base(frame?.Button)
        {
            _frame = frame;
        }

        public override bool IsVisible => _frame != null && _frame.gameObject.activeInHierarchy;

        public override Message GetLabel()
        {
            return Message.Localized("ui", "COMPENDIUM.CARD_FRAMES.FRAME", new { frame = _frame?.FrameType.ToString() });
        }

        public override Message GetStatusString()
        {
            return ButtonState(_frame?.Button);
        }

        public override Message GetTooltip()
        {
            global::IUnlockedMasteryCriteria criteria = global::MonsterTrainAccessibility.Core.ReflectionUtil.Get<global::IUnlockedMasteryCriteria>(_frame, UnlockCriteriaField);
            if (criteria == null)
            {
                return null;
            }

            return Message.Join(", ",
                Message.FromText(criteria.GetUnlockInstructions()),
                Message.FromText(criteria.GetProgressString()));
        }
    }
}

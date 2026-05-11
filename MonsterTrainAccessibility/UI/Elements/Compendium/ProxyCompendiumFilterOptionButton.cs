using MonsterTrainAccessibility.UI.Screens;
using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Util;
using TMPro;

namespace MonsterTrainAccessibility.UI.Elements.Compendium
{
    internal sealed class ProxyCompendiumFilterOptionButton : ProxyCompendiumGameButton
    {
        private static readonly FieldInfo LabelField = AccessTools.Field(typeof(global::FilterOptionButton), "label")!;
        private static readonly FieldInfo SelectedLabelField = AccessTools.Field(typeof(global::FilterOptionButton), "labelSelected")!;

        private readonly global::FilterOptionButton _option;

        public ProxyCompendiumFilterOptionButton(global::FilterOptionButton option)
            : base(option?.Button)
        {
            _option = option;
        }

        public override bool IsVisible => _option != null && _option.gameObject.activeInHierarchy;

        public override Message GetLabel()
        {
            Message tooltipLabel = FirstTooltipLabel();
            if (tooltipLabel != null)
            {
                return tooltipLabel;
            }

            Message selected = Screens.CompendiumScreen.TextOrNull(global::MonsterTrainAccessibility.Core.ReflectionUtil.Get<TMP_Text>(_option, SelectedLabelField));
            if (IsUsableLabel(selected))
            {
                return selected;
            }

            Message label = Screens.CompendiumScreen.TextOrNull(global::MonsterTrainAccessibility.Core.ReflectionUtil.Get<TMP_Text>(_option, LabelField));
            return IsUsableLabel(label) ? label : null;
        }

        public override Message GetStatusString()
        {
            return _option != null && _option.isLockedWithoutDlc ? Message.Localized("messages", "state.locked") : null;
        }

        public override Message GetTooltip()
        {
            return MessageList.TooltipList(_option?.Tooltips);
        }

        private Message FirstTooltipLabel()
        {
            if (_option?.Tooltips == null)
            {
                return null;
            }

            for (int i = 0; i < _option.Tooltips.Count; i++)
            {
                Message title = MessageList.TooltipTitle(_option.Tooltips[i]);
                if (IsUsableLabel(title))
                {
                    return title;
                }
            }

            return null;
        }

        private static bool IsUsableLabel(Message label)
        {
            string text = label?.Resolve();
            return Message.ShouldAdd(text) && text.Trim() != "?";
        }
    }
}

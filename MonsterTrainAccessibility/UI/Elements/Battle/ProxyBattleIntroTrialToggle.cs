using System.Collections.Generic;
using MonsterTrainAccessibility.Buffers;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Presentation;
using MonsterTrainAccessibility.UI.Screens;
using ShinyShoe;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyBattleIntroTrialToggle : ToggleElement
    {
        private readonly global::BattleIntroScreen _screen;
        private readonly TrialInfoUI _trialInfo;
        private readonly GameUISelectableToggle _toggle;

        public ProxyBattleIntroTrialToggle(global::BattleIntroScreen screen, TrialInfoUI trialInfo, GameUISelectableToggle toggle)
            : base(toggle)
        {
            _screen = screen;
            _trialInfo = trialInfo;
            _toggle = toggle;
        }

        public override bool IsVisible =>
            (_toggle != null && _toggle.gameObject.activeInHierarchy) ||
            (_trialInfo != null && _trialInfo.gameObject.activeInHierarchy);

        public override Message GetLabel()
        {
            return Screens.BattleIntroScreen.TrialToggleLabel(_screen, _toggle);
        }

        public override Message GetTooltip()
        {
            return Screens.BattleIntroScreen.TrialToggleTooltip(_screen, _toggle);
        }

        internal override string HandleBuffers(BufferManager buffers)
        {
            LineBuffer uiBuffer = buffers?.GetBuffer("ui");
            if (uiBuffer != null)
            {
                uiBuffer.Clear();
                PresentationBuilder builder = new PresentationBuilder();
                builder.SetTitle(GetLabel());
                builder.SetDescription(GetTooltip());
                builder.AddSection(SectionKind.Context, null, GetStatusString());
                builder.AddSection(SectionKind.Context, null, Screens.BattleIntroScreen.TrialRewardLabel(_screen, _trialInfo));

                List<Message> relicMessages = Screens.BattleIntroScreen.TrialRelicMessages(_screen);
                if (relicMessages != null)
                {
                    for (int i = 0; i < relicMessages.Count; i++)
                    {
                        builder.AddSection(SectionKind.Tooltip, null, relicMessages[i]);
                    }
                }

                IReadOnlyList<Message> lines = PresentationRenderer.BufferLines(builder.Build());
                for (int i = 0; i < lines.Count; i++)
                {
                    uiBuffer.Add(lines[i]);
                }

                buffers.EnableBuffer("ui", true);
            }

            return "ui";
        }
    }
}

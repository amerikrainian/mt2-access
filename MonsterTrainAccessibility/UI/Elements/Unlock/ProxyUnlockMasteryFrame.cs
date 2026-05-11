using MonsterTrainAccessibility.Localization;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyUnlockMasteryFrame : GameObjectElement
    {
        private readonly CardFramePreviewUI _preview;
        private readonly MasteryFrameType _frameType;
        private readonly string _instructions;

        public ProxyUnlockMasteryFrame(CardFramePreviewUI preview, MasteryFrameType frameType, string instructions)
            : base(preview != null ? preview.gameObject : null, label: null)
        {
            _preview = preview;
            _frameType = frameType;
            _instructions = instructions;
        }

        public override bool IsVisible => _frameType != MasteryFrameType.None &&
            (_preview == null || _preview.gameObject.activeInHierarchy);

        public override Message GetLabel()
        {
            return Message.Localized("ui", "UNLOCK.MASTERY_FRAME", new { frame = FormatFrame(_frameType) });
        }

        public override Message GetTooltip()
        {
            return Message.FromText(_instructions);
        }

        private static string FormatFrame(MasteryFrameType frameType)
        {
            string value = frameType.ToString();
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            System.Text.StringBuilder builder = new System.Text.StringBuilder(value.Length + 4);
            for (int i = 0; i < value.Length; i++)
            {
                char current = value[i];
                if (i > 0 && char.IsUpper(current) && !char.IsUpper(value[i - 1]))
                {
                    builder.Append(' ');
                }
                builder.Append(current);
            }

            return builder.ToString();
        }
    }
}

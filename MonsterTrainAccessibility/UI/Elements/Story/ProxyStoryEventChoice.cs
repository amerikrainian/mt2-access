using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Buffers;
using MonsterTrainAccessibility.UI.Screens;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyStoryEventChoice : GameObjectElement
    {
        private readonly global::StoryEventScreen _screen;
        private readonly global::StoryChoiceItem _choice;

        public ProxyStoryEventChoice(global::StoryEventScreen screen, global::StoryChoiceItem choice)
            : base(
                choice?.button != null ? choice.button.gameObject : null,
                typeKey: "button",
                label: null)
        {
            _screen = screen;
            _choice = choice;
        }

        public override bool IsVisible => _choice != null && _choice.gameObject.activeInHierarchy;

        public override Message GetLabel()
        {
            return Screens.StoryEventScreen.ChoiceLabel(_choice);
        }

        public override Message GetStatusString()
        {
            return Screens.StoryEventScreen.ChoiceStatus(_choice);
        }

        public override Message GetTooltip()
        {
            return Screens.StoryEventScreen.ChoiceRewards(_screen, _choice);
        }

        internal override string HandleBuffers(BufferManager buffers)
        {
            return Screens.StoryEventScreen.HandleChoiceBuffers(_screen, _choice, buffers) ?? base.HandleBuffers(buffers);
        }
    }
}

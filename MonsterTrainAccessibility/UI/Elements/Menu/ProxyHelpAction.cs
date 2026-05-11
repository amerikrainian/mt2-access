using MonsterTrainAccessibility.Help;
using MonsterTrainAccessibility.Localization;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyHelpAction : UIElement
    {
        private readonly HelpActionEntry _entry;

        public ProxyHelpAction(HelpActionEntry entry)
        {
            _entry = entry;
        }

        public override Message GetLabel()
        {
            return _entry?.Label;
        }

        public override Message GetStatusString()
        {
            return _entry?.Bindings;
        }
    }
}

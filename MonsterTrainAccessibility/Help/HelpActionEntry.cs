using MonsterTrainAccessibility.Localization;

namespace MonsterTrainAccessibility.Help
{
    internal sealed class HelpActionEntry
    {
        public HelpActionEntry(Message label, Message bindings)
        {
            Label = label;
            Bindings = bindings;
        }

        public Message Label { get; }
        public Message Bindings { get; }
    }
}

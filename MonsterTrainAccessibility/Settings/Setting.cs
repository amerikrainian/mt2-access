using MonsterTrainAccessibility.Localization;

namespace MonsterTrainAccessibility.ModSettings
{
    internal abstract class Setting
    {
        protected Setting(string key, Message label)
        {
            Key = key;
            Label = label;
        }

        public string Key { get; }
        public Message Label { get; }
        public int SortPriority { get; set; } = int.MaxValue;
    }
}

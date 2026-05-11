using System;

namespace MonsterTrainAccessibility.Events
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    internal sealed class EventSettingsAttribute : Attribute
    {
        public EventSettingsAttribute(
            string key,
            string labelKey = null,
            string categoryKey = null,
            bool defaultAnnounce = true)
        {
            Key = key;
            LabelKey = labelKey;
            CategoryKey = categoryKey;
            DefaultAnnounce = defaultAnnounce;
        }

        public string Key { get; }
        public string LabelKey { get; }
        public string CategoryKey { get; }
        public bool DefaultAnnounce { get; }
        public bool HasSourceFilter { get; set; }
        public bool DefaultPlayer { get; set; } = true;
        public bool DefaultEnemies { get; set; } = true;
        public bool DefaultBuffer { get; set; } = true;
    }
}

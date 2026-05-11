using System;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.ModSettings;

namespace MonsterTrainAccessibility.Events
{
    internal sealed class EventDescriptor
    {
        public EventDescriptor(
            Type eventType,
            EventSettingsAttribute attribute,
            Message label,
            BoolSetting announce,
            BoolSetting buffer,
            BoolSetting playerSource,
            BoolSetting enemySource)
        {
            EventType = eventType;
            Attribute = attribute;
            Label = label;
            Announce = announce;
            Buffer = buffer;
            PlayerSource = playerSource;
            EnemySource = enemySource;
        }

        public Type EventType { get; }
        public EventSettingsAttribute Attribute { get; }
        public string Key => Attribute.Key;
        public Message Label { get; }
        public BoolSetting Announce { get; }
        public BoolSetting Buffer { get; }
        public BoolSetting PlayerSource { get; }
        public BoolSetting EnemySource { get; }
    }
}

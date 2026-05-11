using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.ModSettings;

namespace MonsterTrainAccessibility.Events
{
    internal static class EventRegistry
    {
        private const string EventSection = "Event Announcements";

        private static readonly Dictionary<Type, EventDescriptor> DescriptorsByType =
            new Dictionary<Type, EventDescriptor>();

        private static readonly List<EventDescriptor> DescriptorsList = new List<EventDescriptor>();
        private static bool _initialized;
        private static ConfigFile _config;

        public static IReadOnlyList<EventDescriptor> Descriptors
        {
            get
            {
                EnsureInitialized();
                return DescriptorsList;
            }
        }

        public static CategorySetting EventCategory { get; private set; }

        public static void Initialize(ConfigFile config)
        {
            _config = config;
            EnsureInitialized();
        }

        public static bool ShouldAnnounce(GameEvent evt)
        {
            if (evt == null)
            {
                return false;
            }

            EnsureInitialized();
            EventDescriptor descriptor;
            if (!DescriptorsByType.TryGetValue(evt.GetType(), out descriptor))
            {
                return evt.ShouldAnnounce();
            }

            return evt.ShouldAnnounce() && descriptor.Announce.Value && PassesSourceFilter(descriptor, evt.Source);
        }

        public static bool ShouldBuffer(GameEvent evt)
        {
            if (evt == null)
            {
                return false;
            }

            EnsureInitialized();
            if (!evt.ShouldAddToBuffer())
            {
                return false;
            }

            EventDescriptor descriptor;
            if (!DescriptorsByType.TryGetValue(evt.GetType(), out descriptor))
            {
                return true;
            }

            return descriptor.Buffer.Value && PassesSourceFilter(descriptor, evt.Source);
        }

        public static bool PassesSourceFilter(EventDescriptor descriptor, EventSource source)
        {
            if (descriptor == null || !descriptor.Attribute.HasSourceFilter || source == null)
            {
                return true;
            }

            if (source.IsPlayerControlled)
            {
                return descriptor.PlayerSource == null || descriptor.PlayerSource.Value;
            }

            if (source.IsEnemyControlled)
            {
                return descriptor.EnemySource == null || descriptor.EnemySource.Value;
            }

            return true;
        }

        private static void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            if (_config == null)
            {
                Log.Warn("[AccessibilityMod] EventRegistry initialized before config was available.");
                return;
            }

            _initialized = true;
            DescriptorsByType.Clear();
            DescriptorsList.Clear();
            EventCategory = new CategorySetting("events", Message.Localized("ui", "EVENT_SETTINGS.CATEGORY"));
            global::MonsterTrainAccessibility.ModSettings.ModSettings.Register(EventCategory);

            Type baseType = typeof(GameEvent);
            Type attributeType = typeof(EventSettingsAttribute);
            Type[] types = Assembly.GetExecutingAssembly().GetTypes();
            bool removedLegacyEntries = false;
            for (int i = 0; i < types.Length; i++)
            {
                Type type = types[i];
                if (!baseType.IsAssignableFrom(type) || type.IsAbstract)
                {
                    continue;
                }

                EventSettingsAttribute attribute = type.GetCustomAttribute(attributeType) as EventSettingsAttribute;
                if (attribute == null)
                {
                    Log.Info("[AccessibilityMod] Event has no settings attribute: " + type.Name);
                    continue;
                }

                removedLegacyEntries |= RemoveLegacyEventEntries(attribute);
                EventDescriptor descriptor = CreateDescriptor(type, attribute);
                DescriptorsByType[type] = descriptor;
                DescriptorsList.Add(descriptor);
                EventCategory.Add(CreateEventCategory(descriptor));
            }

            if (removedLegacyEntries)
            {
                _config.Save();
            }

            DescriptorsList.Sort((left, right) => string.Compare(
                left.Label.Resolve(),
                right.Label.Resolve(),
                StringComparison.CurrentCultureIgnoreCase));
        }

        private static EventDescriptor CreateDescriptor(Type type, EventSettingsAttribute attribute)
        {
            string labelKey = !string.IsNullOrWhiteSpace(attribute.LabelKey)
                ? attribute.LabelKey
                : "EVENT_SETTINGS." + NormalizeKey(attribute.Key);

            Message label = Message.Localized("ui", labelKey);
            string descriptionLabel = label.Resolve();
            BoolSetting announce = new BoolSetting(
                _config,
                EventSection,
                attribute.Key + ".announce",
                Message.Localized("ui", "EVENT_SETTINGS.ANNOUNCE"),
                attribute.DefaultAnnounce,
                "Announce " + descriptionLabel + " events.");

            BoolSetting buffer = new BoolSetting(
                _config,
                EventSection,
                attribute.Key + ".buffer",
                Message.Localized("ui", "EVENT_SETTINGS.BUFFER"),
                attribute.DefaultBuffer,
                "Add " + descriptionLabel + " events to the events buffer.");

            BoolSetting player = null;
            BoolSetting enemy = null;
            if (attribute.HasSourceFilter)
            {
                player = new BoolSetting(
                    _config,
                    EventSection,
                    attribute.Key + ".sources.friendly",
                    Message.Localized("ui", "EVENT_SETTINGS.SOURCE_PLAYER"),
                    attribute.DefaultPlayer,
                    "Allow " + descriptionLabel + " events from friendly units.");

                enemy = new BoolSetting(
                    _config,
                    EventSection,
                    attribute.Key + ".sources.enemy",
                    Message.Localized("ui", "EVENT_SETTINGS.SOURCE_ENEMY"),
                    attribute.DefaultEnemies,
                    "Allow " + descriptionLabel + " events from enemy units.");
            }

            return new EventDescriptor(type, attribute, label, announce, buffer, player, enemy);
        }

        private static CategorySetting CreateEventCategory(EventDescriptor descriptor)
        {
            CategorySetting category = new CategorySetting(descriptor.Key, descriptor.Label);
            category.Add(descriptor.Announce);
            category.Add(descriptor.Buffer);

            if (descriptor.PlayerSource != null || descriptor.EnemySource != null)
            {
                CategorySetting sources = new CategorySetting("sources", Message.Localized("ui", "EVENT_SETTINGS.SOURCES"));
                sources.Add(descriptor.PlayerSource);
                sources.Add(descriptor.EnemySource);
                category.Add(sources);
            }

            return category;
        }

        private static bool RemoveLegacyEventEntries(EventSettingsAttribute attribute)
        {
            bool removed = false;
            string key = attribute.Key;
            removed |= _config.Remove(new ConfigDefinition(EventSection, key + ".Announce"));
            removed |= _config.Remove(new ConfigDefinition(EventSection, key + ".Buffer"));
            removed |= _config.Remove(new ConfigDefinition(EventSection, key + ".Sources.Player"));
            removed |= _config.Remove(new ConfigDefinition(EventSection, key + ".Sources.Enemy"));
            return removed;
        }

        private static string NormalizeKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return "UNKNOWN";
            }

            char[] chars = key.ToUpperInvariant().ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (!char.IsLetterOrDigit(chars[i]))
                {
                    chars[i] = '_';
                }
            }

            return new string(chars);
        }
    }
}

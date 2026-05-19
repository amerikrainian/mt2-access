using BepInEx.Configuration;
using MonsterTrainAccessibility.Localization;

namespace MonsterTrainAccessibility.ModSettings
{
    internal sealed class BoolSetting : Setting
    {
        private readonly ConfigEntry<bool> _entry;

        public BoolSetting(
            ConfigFile config,
            string section,
            string key,
            Message label,
            bool defaultValue,
            string description)
            : base(key, label)
        {
            _entry = config.Bind(section, key, defaultValue, new ConfigDescription(description ?? string.Empty));
        }

        public bool Value
        {
            get => _entry.Value;
            set => _entry.Value = value;
        }

        public bool DefaultValue => (bool)_entry.DefaultValue;

        public void ResetToDefault()
        {
            Value = DefaultValue;
        }
    }
}

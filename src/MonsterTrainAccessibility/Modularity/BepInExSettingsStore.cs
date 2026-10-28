using System.Collections.Generic;
using BepInEx.Configuration;
using MonsterTrainAccessibility.Contracts;

namespace MonsterTrainAccessibility.Modularity
{
    /// <summary>
    /// Persists the mod's settings through BepInEx's <see cref="ConfigFile"/> (the host plugin's own
    /// Config), so each setting lands in a single TOML file under BepInEx/config that survives game
    /// restarts and is editable by hand. Each key binds a <see cref="ConfigEntry{T}"/> once and is then
    /// reused; setting its value auto-saves the file.
    /// </summary>
    internal sealed class BepInExSettingsStore : ISettingsStore
    {
        private const string Section = "Settings";
        private readonly ConfigFile _config;
        private readonly Dictionary<string, ConfigEntry<bool>> _bools = new Dictionary<string, ConfigEntry<bool>>();
        private readonly Dictionary<string, ConfigEntry<int>> _ints = new Dictionary<string, ConfigEntry<int>>();
        private readonly Dictionary<string, ConfigEntry<string>> _strings = new Dictionary<string, ConfigEntry<string>>();

        public BepInExSettingsStore(ConfigFile config) => _config = config;

        private ConfigEntry<T> Bind<T>(Dictionary<string, ConfigEntry<T>> cache, string key, T defaultValue)
        {
            if (!cache.TryGetValue(key, out ConfigEntry<T> entry))
            {
                entry = _config.Bind(Section, key, defaultValue);
                cache[key] = entry;
            }
            return entry;
        }

        public bool GetBool(string key, bool defaultValue) => Bind(_bools, key, defaultValue).Value;
        public void SetBool(string key, bool value) => Bind(_bools, key, value).Value = value;

        public int GetInt(string key, int defaultValue) => Bind(_ints, key, defaultValue).Value;
        public void SetInt(string key, int value) => Bind(_ints, key, value).Value = value;

        public string GetString(string key, string defaultValue) => Bind(_strings, key, defaultValue ?? "").Value;
        public void SetString(string key, string value) => Bind(_strings, key, value ?? "").Value = value ?? "";
    }
}

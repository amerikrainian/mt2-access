namespace MonsterTrainAccessibility.Contracts
{
    /// <summary>
    /// The persistence seam behind the mod's settings: load a value (returning the default when nothing
    /// is stored) and save one. An interface so nothing here references BepInEx or files; the host
    /// implements it over its BepInEx ConfigFile, and tests over an in-memory fake.
    /// </summary>
    public interface ISettingsStore
    {
        bool GetBool(string key, bool defaultValue);
        void SetBool(string key, bool value);

        int GetInt(string key, int defaultValue);
        void SetInt(string key, int value);

        string GetString(string key, string defaultValue);
        void SetString(string key, string value);
    }
}

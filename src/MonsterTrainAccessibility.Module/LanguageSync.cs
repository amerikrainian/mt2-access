using System;
using System.IO;
using HarmonyLib;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Core.Strings;

namespace MonsterTrainAccessibility.Module
{
    /// <summary>
    /// The mod's authored strings follow the GAME's language (I2 Localization's current language code,
    /// which the settings screen's language option sets through <see cref="LanguageManager"/>), not the
    /// system's: lang/&lt;code&gt;.txt beside the plugin, English (the defaults) when there is no file
    /// for it. Nothing is polled. A switch, and the game's first language at boot, arrive through
    /// <see cref="LanguageManager.HandleLanguageChanged"/>, which the game calls once its preferences
    /// exist; a hot reload, which comes long after, reads the code already in effect at Start.
    /// </summary>
    [HarmonyPatch(typeof(LanguageManager), nameof(LanguageManager.HandleLanguageChanged))]
    internal static class LanguageSync
    {
        private static string _langDir;
        private static string _applied;
        private static Action _onFirstLanguage;

        /// <summary>Whether the game's language has been read: the launch line and the update line
        /// wait for it, so they are spoken in the language the game came up in.</summary>
        public static bool Resolved => _applied != null;

        /// <summary>The language code whose strings are in effect ("de"), or null before the first read.</summary>
        public static string Applied => _applied;

        /// <summary>Begin following the game's language. <paramref name="onFirstLanguage"/> runs once,
        /// right after the first language is in place.</summary>
        public static void Start(string pluginDir, Action onFirstLanguage)
        {
            _langDir = Path.Combine(pluginDir, "lang");
            _onFirstLanguage = onFirstLanguage;
            Apply(CurrentCode());
        }

        /// <summary>Module teardown: the hook goes with this generation's Harmony id.</summary>
        public static void Stop()
        {
            _onFirstLanguage = null;
        }

        // After the game applied (or refused) a language: read back what is in effect.
        private static void Postfix()
        {
            try { Apply(CurrentCode()); }
            catch (Exception e) { CoreLog.Error("LanguageSync: applying a language switch failed: " + e); }
        }

        // Empty before the game's preferences have loaded at boot.
        private static string CurrentCode()
        {
            string code = I2.Loc.LocalizationManager.CurrentLanguageCode;
            return string.IsNullOrWhiteSpace(code) ? null : code;
        }

        private static void Apply(string code)
        {
            if (code == null || code == _applied) return;
            bool first = _applied == null;
            _applied = code;
            Load(code);
            if (!first || _onFirstLanguage == null) return;
            try { _onFirstLanguage(); }
            catch (Exception e) { CoreLog.Error("LanguageSync: the first-language callback failed: " + e); }
        }

        private static void Load(string code)
        {
            try
            {
                foreach (var name in LanguageFiles.Candidates(code))
                {
                    string path = Path.Combine(_langDir, name);
                    if (!File.Exists(path)) continue;
                    var report = Strings.LoadTranslation(File.ReadAllLines(path));
                    foreach (var problem in report.Problems()) CoreLog.Warning("lang " + name + ": " + problem);
                    CoreLog.Info("LanguageSync: " + code + ", " + report.Applied + " strings from " + name);
                    return;
                }
                Strings.LoadTranslation(null);
                CoreLog.Info("LanguageSync: " + code + ", no lang file for it, English");
            }
            catch (Exception e)
            {
                Strings.LoadTranslation(null);
                CoreLog.Error("LanguageSync: loading the strings of " + code + " failed, English: " + e);
            }
        }
    }
}

using System;
using System.IO;
using BepInEx;
using BepInEx.Logging;

namespace MonsterTrainAccessibility.Host
{
    /// <summary>
    /// Removes the single-DLL layout every release before 0.3.0 shipped, for a player who extracted the
    /// new zip over it by hand (the installer removes it itself). The old plugin has this plugin's GUID:
    /// BepInEx loads the highest version of a GUID and skips the rest unloaded ("Skipping ... because a
    /// newer version exists"), so its file is free to delete here, and on the next launch nothing is
    /// skipped. Only the files the old layout alone used go: its plugin, its deps.json and pdb, its JSON
    /// strings. Its loose System.Text.Json family stays, since another mod may resolve those from the
    /// plugins folder while the game runs.
    /// </summary>
    internal static class LegacyCleanup
    {
        private const string LegacyPlugin = "MonsterTrainAccessibility.dll";

        private static readonly string[] Companions =
        {
            "MonsterTrainAccessibility.deps.json",
            "MonsterTrainAccessibility.pdb",
        };

        private static readonly string[] StringFiles =
        {
            "de.json", "en.json", "es.json", "fr.json", "ja.json", "ko.json", "pt.json", "ru.json", "zh-CN.json", "zh-TW.json",
        };

        public static void Run(ManualLogSource log)
        {
            try
            {
                string plugins = Paths.PluginPath;
                string legacy = Path.Combine(plugins, LegacyPlugin);
                if (!File.Exists(legacy)) return;
                // Never this plugin itself: ours lives in plugins\MonsterTrainAccessibility\.
                if (string.Equals(Path.GetFullPath(legacy), Path.GetFullPath(typeof(Plugin).Assembly.Location), StringComparison.OrdinalIgnoreCase)) return;

                int removed = 0;
                Delete(legacy, log, ref removed);
                foreach (var name in Companions) Delete(Path.Combine(plugins, name), log, ref removed);
                string strings = Path.Combine(Path.Combine(plugins, "Localization"), "Strings");
                foreach (var name in StringFiles) Delete(Path.Combine(strings, name), log, ref removed);
                DeleteIfEmpty(strings, log);
                DeleteIfEmpty(Path.GetDirectoryName(strings), log);
                log.LogInfo("Removed the pre-0.3.0 single-file install (" + removed + " files) from " + plugins);
            }
            catch (Exception e)
            {
                log.LogWarning("Removing the pre-0.3.0 install failed: " + e);
            }
        }

        private static void Delete(string path, ManualLogSource log, ref int removed)
        {
            if (!File.Exists(path)) return;
            try
            {
                File.Delete(path);
                removed++;
            }
            catch (Exception e)
            {
                log.LogWarning("Could not delete " + path + " (delete it by hand, or the old version loads beside this one): " + e.Message);
            }
        }

        private static void DeleteIfEmpty(string dir, ManualLogSource log)
        {
            try
            {
                if (Directory.Exists(dir) && Directory.GetFileSystemEntries(dir).Length == 0) Directory.Delete(dir);
            }
            catch (Exception e)
            {
                log.LogWarning("Could not remove the empty folder " + dir + ": " + e.Message);
            }
        }
    }
}

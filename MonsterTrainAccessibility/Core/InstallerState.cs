using System;
using System.IO;

namespace MonsterTrainAccessibility.Core
{
    internal static class InstallerState
    {
        public static void WriteInstalledVersion(string version)
        {
            if (string.IsNullOrWhiteSpace(version))
            {
                return;
            }

            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                if (string.IsNullOrWhiteSpace(appData))
                {
                    Log.Info("[AccessibilityMod] Installer state skipped: ApplicationData path unavailable.");
                    return;
                }

                string directory = Path.Combine(appData, "MonsterTrainAccessibility");
                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, "version"), version.Trim());
            }
            catch (Exception ex)
            {
                Log.Info("[AccessibilityMod] Failed to write installer version state: " + ex.Message);
            }
        }
    }
}

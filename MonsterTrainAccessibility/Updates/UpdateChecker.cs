using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using MonsterTrainAccessibility.Core;

namespace MonsterTrainAccessibility.Updates
{
    internal static class UpdateChecker
    {
        private const string ApiUrl = "https://api.github.com/repos/amerikrainian/mt2-access/releases/latest";
        private const string UserAgent = "MonsterTrainAccessibility-mod";

        public static string LatestRemoteVersion { get; private set; }

        public static void Run()
        {
            _ = RunAsync();
        }

        private static async Task RunAsync()
        {
            try
            {
                using (HttpClient http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) })
                {
                    http.DefaultRequestHeaders.Add("User-Agent", UserAgent);
                    http.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");

                    string json = await http.GetStringAsync(ApiUrl);
                    using (JsonDocument doc = JsonDocument.Parse(json))
                    {
                        if (!doc.RootElement.TryGetProperty("tag_name", out JsonElement tagElement))
                        {
                            Log.Info("[AccessibilityMod] Update check: response missing tag_name.");
                            return;
                        }

                        string tag = tagElement.GetString();
                        if (string.IsNullOrWhiteSpace(tag))
                        {
                            Log.Info("[AccessibilityMod] Update check: empty tag_name.");
                            return;
                        }

                        string remote = NormalizeVersion(tag);
                        string local = global::MonsterTrainAccessibility.MonsterTrainAccessibility.VERSION;

                        if (IsNewer(remote, local))
                        {
                            LatestRemoteVersion = remote;
                            Log.Info("[AccessibilityMod] Update available: " + remote + " (current: " + local + ").");
                        }
                        else
                        {
                            Log.Info("[AccessibilityMod] Up to date (latest: " + remote + ", current: " + local + ").");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Info("[AccessibilityMod] Update check failed: " + ex.Message);
            }
        }

        private static string NormalizeVersion(string tag)
        {
            string trimmed = tag.Trim();
            if (trimmed.Length > 0 && (trimmed[0] == 'v' || trimmed[0] == 'V'))
            {
                trimmed = trimmed.Substring(1);
            }

            return trimmed;
        }

        private static bool IsNewer(string remote, string local)
        {
            int[] remoteParts = ParseVersion(remote);
            int[] localParts = ParseVersion(local);
            int length = Math.Max(remoteParts.Length, localParts.Length);

            for (int i = 0; i < length; i++)
            {
                int remotePart = i < remoteParts.Length ? remoteParts[i] : 0;
                int localPart = i < localParts.Length ? localParts[i] : 0;
                if (remotePart > localPart) return true;
                if (remotePart < localPart) return false;
            }

            return false;
        }

        private static int[] ParseVersion(string version)
        {
            string[] parts = version.Split('.');
            int[] result = new int[parts.Length];

            for (int i = 0; i < parts.Length; i++)
            {
                if (!int.TryParse(parts[i], out result[i]))
                {
                    result[i] = 0;
                }
            }

            return result;
        }
    }
}

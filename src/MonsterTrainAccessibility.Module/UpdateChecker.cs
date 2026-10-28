using System;
using System.Net.Http;
using System.Threading.Tasks;
using MonsterTrainAccessibility.Core;

namespace MonsterTrainAccessibility.Module
{
    /// <summary>
    /// Fetches the newest GitHub release on a thread-pool thread and holds the answer for the module's
    /// tick to announce: the request never touches the main thread. Only a release strictly newer than
    /// the running build ever surfaces; up to date, ahead of the release (a dev build), offline or
    /// rate-limited all stay spoken-silent with a log line. Started once per module load (a dev reload
    /// asks again; a player's game loads the module once).
    /// </summary>
    internal sealed class UpdateChecker
    {
        private const string ApiUrl = "https://api.github.com/repos/amerikrainian/mt2-access/releases/latest";

        private volatile string _newerVersion;

        /// <summary>The version to announce, set once the background request found a release strictly
        /// newer than the running build; null before that, and forever when none is.</summary>
        public string NewerVersion => _newerVersion;

        public void Start(string local)
        {
            Task.Run(() => Check(local));
        }

        // The whole request on the thread-pool thread, blocking there: an async state machine here
        // would need the compiler's NullableAttribute, which the interop assemblies shadow.
        private void Check(string local)
        {
            try
            {
                string json;
                using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) })
                {
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("MonsterTrainAccessibility-mod");
                    client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
                    json = client.GetStringAsync(ApiUrl).GetAwaiter().GetResult();
                }
                string remote = UpdateCheck.LatestVersion(json);
                if (remote == null)
                {
                    CoreLog.Warning("update check: release payload named no version");
                    return;
                }
                if (UpdateCheck.IsNewer(remote, local))
                {
                    CoreLog.Info("update check: " + remote + " available (running " + local + ")");
                    _newerVersion = remote;
                }
                else
                {
                    CoreLog.Info("update check: up to date (latest " + remote + ", running " + local + ")");
                }
            }
            catch (Exception e)
            {
                CoreLog.Warning("update check: failed (" + e.Message + ")");
            }
        }
    }
}

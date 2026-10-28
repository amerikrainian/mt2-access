using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using BepInEx.Logging;
using MonsterTrainAccessibility.Contracts;
using MonsterTrainAccessibility.Modularity;

namespace MonsterTrainAccessibility.Dev
{
    /// <summary>
    /// In-process dev driver, on by default (set MTA_NO_DEV=1 to disable). The HTTP server binds
    /// 127.0.0.1 only, so it is reachable from this machine alone. An external driver can:
    ///   POST /eval             body = C# source, run against the live game (REPL state persists across
    ///                          calls); returns output + result/errors, then a "speech:" section with any
    ///                          lines spoken as a consequence (?speech=0 skips, ?settle=MS tunes).
    ///   POST /input            body = a verb (up|down|left|right|confirm|back|tab|prev|home|end|
    ///                          secondary|tooltip|read) or any registered action key ("ui.down",
    ///                          "mod.focus"). Routed through the module's own input dispatch when it is
    ///                          loaded, else the uGUI fallback injector.
    ///   POST /type             body = text appended to the focused input field.
    ///   POST /wait?timeout=MS  body = C# bool expression evaluated each frame on the main thread.
    ///   POST /reload           rebuild the feature side from its freshly built DLLs, no restart.
    ///   GET  /module            module type + generation, DLL write times, and the Harmony patch table.
    ///   GET  /actions           every registered input action key (the /input vocabulary).
    ///   GET  /typeinfo?name=X   find a type by simple name (everything loaded) and print its members.
    ///   GET  /focus             the current uGUI selection (name/path/text), independent of speech.
    ///   GET  /nav               our navigator's own focus state (screen, path, focused node).
    ///   GET  /gui               raw dump of the active uGUI hierarchy.
    ///   GET  /speech?since=N    lines the mod has spoken since cursor N; &wait=MS long-polls.
    ///   GET  /log?since=N       the mod's log lines (same cursor protocol; &grep=S filters).
    ///   GET  /screenshot        capture a PNG of the current frame; returns the file path.
    ///   GET  /health            liveness.
    ///
    /// Eval / input / reload / screenshot run on the Unity main thread: HTTP requests enqueue a job and
    /// block until <see cref="Pump"/> executes it. Not shipped to players.
    /// </summary>
    internal sealed class DevServer
    {
        public const string DisableEnv = "MTA_NO_DEV";
        public const string PortEnv = "MTA_DEV_PORT";
        private const int DefaultPort = 8772;

        private sealed class Job
        {
            public Func<string> Work;
            public string Result = "";
            public readonly ManualResetEventSlim Done = new ManualResetEventSlim(false);
        }

        // A /wait in flight: the compiled predicate, evaluated once per frame from Pump until it turns
        // true, throws, or the HTTP thread gives up (Cancelled).
        private sealed class WaitJob
        {
            public Func<bool> Predicate;
            public string Outcome;
            public readonly ManualResetEventSlim Done = new ManualResetEventSlim(false);
            public readonly System.Diagnostics.Stopwatch Elapsed = System.Diagnostics.Stopwatch.StartNew();
            public volatile bool Cancelled;
        }

        private readonly ModuleLoader _loader;
        private readonly ManualLogSource _log;
        private readonly LineLog _speech = new LineLog();
        private readonly LineLog _logLines = new LineLog();
        private readonly CSharpEvaluator _evaluator;
        private readonly ConcurrentQueue<Job> _jobs = new ConcurrentQueue<Job>();
        private readonly List<WaitJob> _waits = new List<WaitJob>(); // guarded by lock(_waits)
        private DevHttpServer _http;
        private DevLogListener _logListener;
        private bool _enabled;
        private bool _warmedUp;

        public DevServer(ModuleLoader loader, ManualLogSource log)
        {
            _loader = loader;
            _log = log;
            _evaluator = new CSharpEvaluator(() => new[] { loader.CoreImage, loader.ModuleImage });
        }

        /// <summary>Stand up the loopback server unless MTA_NO_DEV=1.</summary>
        public void Start()
        {
            if (Environment.GetEnvironmentVariable(DisableEnv) == "1")
            {
                _log.LogInfo("Dev server disabled (" + DisableEnv + "=1)");
                return;
            }

            int port = DefaultPort;
            string p = Environment.GetEnvironmentVariable(PortEnv);
            if (!string.IsNullOrEmpty(p))
                int.TryParse(p, out port);

            // Tap every line the mod speaks into the ring buffer, tagging whether it interrupted or
            // queued, and which class spoke it.
            SpeechPipeline.Spoken = (text, interrupt, source) => _speech.Add(
                (interrupt ? "[interrupt] " : "[queue] ")
                + (string.IsNullOrEmpty(source) ? "" : "[" + source + "] ")
                + text);

            // Mirror every BepInEx log event into the /log ring so the driver reads the log in-band.
            _logListener = new DevLogListener(_logLines);
            Logger.Listeners.Add(_logListener);

            try
            {
                _http = new DevHttpServer(port, HandleRequest, _log.LogWarning);
                _http.Start();
                _enabled = true;
                _log.LogInfo("Dev server on http://127.0.0.1:" + port + " (POST /eval, GET /speech)");
            }
            catch (Exception e)
            {
                _log.LogError("Dev server failed to start: " + e);
            }
        }

        /// <summary>Run queued main-thread jobs and pending /wait predicates. Once per frame from the pump.</summary>
        public void Pump()
        {
            if (!_enabled)
                return;
            if (!_warmedUp)
            {
                // The first Roslyn compile loads its deps through a one-time cold assembly resolve that
                // fails the first eval. Absorb it here on the main thread so the first real /eval is clean.
                _warmedUp = true;
                _evaluator.Eval("1");
            }
            while (_jobs.TryDequeue(out Job job))
            {
                try
                {
                    job.Result = job.Work() ?? "";
                }
                catch (Exception e)
                {
                    job.Result = "[host error] " + e + "\n";
                }
                job.Done.Set();
            }
            PumpWaits();
        }

        private void PumpWaits()
        {
            lock (_waits)
            {
                for (int i = _waits.Count - 1; i >= 0; i--)
                {
                    WaitJob w = _waits[i];
                    if (w.Cancelled)
                    {
                        _waits.RemoveAt(i);
                        continue;
                    }
                    try
                    {
                        if (!w.Predicate())
                            continue;
                        w.Outcome = "[true] after " + w.Elapsed.ElapsedMilliseconds + "ms\n";
                    }
                    catch (Exception e)
                    {
                        w.Outcome = "[exception] " + e + "\n";
                    }
                    w.Done.Set();
                    _waits.RemoveAt(i);
                }
            }
        }

        /// <summary>Run <paramref name="work"/> on the main thread (next Pump) and block for its result.</summary>
        private string OnMainThread(Func<string> work, int timeoutSeconds = 30)
        {
            var job = new Job { Work = work };
            _jobs.Enqueue(job);
            if (!job.Done.Wait(TimeSpan.FromSeconds(timeoutSeconds)))
                return "[timeout] main thread did not run the job within " + timeoutSeconds + "s (frozen / not pumping?)\n";
            return job.Result;
        }

        // Runs on the HTTP thread.
        private string HandleRequest(string method, string path, string body)
        {
            string route = path;
            var query = new Dictionary<string, string>(StringComparer.Ordinal);
            int q = path.IndexOf('?');
            if (q >= 0)
            {
                route = path.Substring(0, q);
                foreach (string kv in path.Substring(q + 1).Split('&'))
                {
                    int eq = kv.IndexOf('=');
                    if (eq > 0)
                        query[kv.Substring(0, eq)] = Uri.UnescapeDataString(kv.Substring(eq + 1));
                }
            }

            if (route == "/eval" && method == "POST")
            {
                if (string.IsNullOrWhiteSpace(body))
                    return "[empty] POST C# source as the request body\n";
                return EvalWithSpeech(body, query);
            }

            if (route == "/input" && method == "POST")
            {
                string verb = (body ?? "").Trim();
                return OnMainThread(() => DriveInput(verb));
            }

            if (route == "/type" && method == "POST")
                return OnMainThread(() => TextInjector.Type(body ?? ""));

            if (route == "/wait" && method == "POST")
                return Wait(body, QueryInt(query, "timeout", 10000, 100, 120000));

            if (route == "/reload" && method == "POST")
                return OnMainThread(ReloadModule);

            if (route == "/module" && method == "GET")
                return OnMainThread(() => ModuleInspector.Describe(_loader));

            if (route == "/actions" && method == "GET")
                return OnMainThread(() =>
                {
                    var driver = _loader.Module as IDevDriver;
                    return driver != null ? driver.ListActions() : "[no module] nav driver unavailable\n";
                });

            if (route == "/typeinfo" && method == "GET")
                return OnMainThread(() => TypeFinder.Describe(query.TryGetValue("name", out string n) ? n : ""));

            if (route == "/focus" && method == "GET")
                return OnMainThread(FocusInspector.Describe);

            if (route == "/nav" && method == "GET")
                return OnMainThread(DescribeNav);

            if (route == "/gui" && method == "GET")
                return OnMainThread(GuiInspector.Describe);

            if (route == "/screenshot" && method == "GET")
                return Screenshot();

            if (route == "/speech" && method == "GET")
                return ReadLines(_speech, query);

            if (route == "/log" && method == "GET")
                return ReadLines(_logLines, query, query.TryGetValue("grep", out string g) ? g : null);

            if (route == "/health" || route == "/")
                return "ok\n";

            return "[404] " + method + " " + route + "\n";
        }

        // Shared /speech and /log read: cursor render, optional long-poll, optional substring filter.
        private static string ReadLines(LineLog log, Dictionary<string, string> query, string grep = null)
        {
            long since = 0;
            if (query.TryGetValue("since", out string s))
                long.TryParse(s, out since);
            int wait = QueryInt(query, "wait", 0, 0, 120000);
            if (wait > 0)
                log.WaitForNew(since, wait);
            string lines = log.Render(since, out long next);
            if (!string.IsNullOrEmpty(grep))
            {
                var kept = new System.Text.StringBuilder();
                foreach (string line in lines.Split('\n'))
                    if (line.IndexOf(grep, StringComparison.OrdinalIgnoreCase) >= 0)
                        kept.Append(line).Append('\n');
                lines = kept.ToString();
            }
            return "cursor: " + next + "\n" + lines;
        }

        // Run an eval, then read back what it caused the mod to SAY: announcements land on later frames,
        // so after the eval returns we wait for a quiet window and append whatever arrived.
        private string EvalWithSpeech(string code, Dictionary<string, string> query)
        {
            bool withSpeech = !query.TryGetValue("speech", out string sp) || sp != "0";
            int settle = QueryInt(query, "settle", 250, 0, 2000);

            long cursor = _speech.End;
            string result = OnMainThread(() => _evaluator.Eval(code));
            if (!withSpeech || settle == 0)
                return result;

            var overall = System.Diagnostics.Stopwatch.StartNew();
            long seen = cursor;
            while (overall.ElapsedMilliseconds < 5000 && _speech.WaitForNew(seen, settle))
                seen = _speech.End;

            string spoken = _speech.Render(cursor, out _);
            return spoken.Length == 0 ? result : result + "speech:\n" + spoken;
        }

        private string Wait(string body, int timeoutMs)
        {
            if (string.IsNullOrWhiteSpace(body))
                return "[empty] POST a C# bool expression as the request body\n";

            Func<bool> predicate = null;
            string error = null;
            OnMainThread(() =>
            {
                error = _evaluator.CompilePredicate(body, out predicate);
                return "";
            });
            if (error != null)
                return error;

            var wait = new WaitJob { Predicate = predicate };
            lock (_waits)
                _waits.Add(wait);
            if (wait.Done.Wait(timeoutMs))
                return wait.Outcome;
            wait.Cancelled = true;
            return "[timeout] not true within " + timeoutMs + "ms\n";
        }

        private static int QueryInt(Dictionary<string, string> query, string key, int fallback, int min, int max)
        {
            if (!query.TryGetValue(key, out string raw) || !int.TryParse(raw, out int value))
                return fallback;
            return Math.Max(min, Math.Min(max, value));
        }

        // Drive input. Prefer our own dispatch (the module's IDevDriver): it routes a UI action into our
        // navigator and any other key to its handler, exactly as a real key press would. With no module
        // loaded, fall back to the uGUI selection injector so an unmigrated screen can still be poked.
        private string DriveInput(string verb)
        {
            string v = (verb ?? "").Trim();
            var driver = _loader.Module as IDevDriver;
            if (driver != null)
            {
                string action = VerbToAction(v.ToLowerInvariant()) ?? v;
                string r = driver.DispatchAction(action);
                if (r != null)
                    return "[mod] " + r + "\n";
                return "[unknown action] " + v + "\n" + driver.ListActions();
            }
            return "[game] " + InputInjector.Inject(v.ToLowerInvariant());
        }

        // Map a dev verb to the UI action key our navigator understands; null = pass the verb through
        // as a raw action key.
        private static string VerbToAction(string v)
        {
            switch (v)
            {
                case "up": return "ui.up";
                case "down": return "ui.down";
                case "left": return "ui.left";
                case "right": return "ui.right";
                case "confirm": case "enter": case "ok": return "ui.activate";
                case "back": case "escape": case "cancel": return "ui.back";
                case "tab": case "next": return "ui.next";
                case "prev": case "shifttab": case "shift-tab": return "ui.prev";
                case "home": return "ui.home";
                case "end": return "ui.end";
                case "secondary": case "backspace": return "ui.secondary";
                case "tooltip": case "space": return "ui.tooltip";
                case "read": return "ui.readFocus";
                default: return null;
            }
        }

        private string DescribeNav()
        {
            var driver = _loader.Module as IDevDriver;
            return driver != null ? driver.DescribeNav() : "[no module] nav driver unavailable\n";
        }

        // Reload the feature side, then reset the evaluator so /eval recompiles against fresh types.
        private string ReloadModule()
        {
            bool ok = _loader.Reload();
            _evaluator.Reset();
            return (ok ? "reloaded\n" : "[reload failed] see /log\n") + ModuleInspector.Describe(_loader);
        }

        private string Screenshot()
        {
            string path = Path.Combine(Path.GetTempPath(), "mt2access_shot.png");
            DateTime requestedAt = DateTime.UtcNow;
            OnMainThread(() =>
            {
                try
                {
                    if (File.Exists(path))
                        File.Delete(path);
                }
                catch (Exception e)
                {
                    _log.LogWarning("screenshot: could not delete stale " + path + ": " + e.Message);
                }
                UnityEngine.ScreenCapture.CaptureScreenshot(path);
                return "requested";
            });

            var timer = System.Diagnostics.Stopwatch.StartNew();
            while (timer.Elapsed.TotalSeconds < 8)
            {
                try
                {
                    if (File.Exists(path) && File.GetLastWriteTimeUtc(path) >= requestedAt)
                    {
                        long size = new FileInfo(path).Length;
                        if (size > 0)
                        {
                            Thread.Sleep(60);
                            if (new FileInfo(path).Length == size)
                                return path + "\n";
                        }
                    }
                }
                catch (Exception e)
                {
                    _log.LogWarning("screenshot: probe failed: " + e.Message);
                }
                Thread.Sleep(50);
            }
            return "[timeout] screenshot not written within 8s\n";
        }
    }
}

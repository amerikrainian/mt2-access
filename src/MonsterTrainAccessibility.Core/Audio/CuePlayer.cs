using System;
using System.Collections.Generic;

namespace MonsterTrainAccessibility.Core.Audio
{
    /// <summary>
    /// The one door the game's events knock on: <see cref="Play"/> a cue, and it plays unless that
    /// cue's interval says it played too recently. The interval is read live from the settings, so a
    /// change applies to the next event; a blank interval lets every event through. The clock is a
    /// seam (unscaled seconds), so the gate is unit-tested without an engine.
    /// </summary>
    public sealed class CuePlayer
    {
        private readonly IAudioEngine _engine;
        private readonly SoundVolumes _settings;
        private readonly Func<double> _now;
        private readonly Dictionary<AudioCue, double> _lastPlayed = new Dictionary<AudioCue, double>();

        public CuePlayer(IAudioEngine engine, SoundVolumes settings, Func<double> now)
        {
            _engine = engine;
            _settings = settings;
            _now = now;
        }

        /// <summary>Play the cue now unless its interval gates it. True when it played.</summary>
        public bool Play(AudioCue cue, float volume = 1f, float pan = 0f, float pitch = 1f)
        {
            double now = _now();
            double? interval = _settings.Interval(cue).Seconds;
            if (interval != null && _lastPlayed.TryGetValue(cue, out double last) && now - last < interval.Value) return false;
            _lastPlayed[cue] = now;
            _engine.PlayCue(cue, volume, pan, pitch);
            return true;
        }

        /// <summary>Forget when each cue last played (a new fight).</summary>
        public void Reset() => _lastPlayed.Clear();
    }
}

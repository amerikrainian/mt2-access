using System;
using System.Collections.Generic;
using System.Globalization;
using MonsterTrainAccessibility.Contracts;

namespace MonsterTrainAccessibility.Core.Audio
{
    /// <summary>
    /// The baseline playback volume of every mod sound, in percent: a cue's effective volume is this
    /// value plus that cue's saved offset, so one adjustment moves every sound together while their
    /// relative levels hold. Stepped from the sounds screen, persisted through the settings store
    /// beside the per-sound offsets.
    /// </summary>
    public sealed class MasterVolume
    {
        public const string Key = "sound_master";
        public const int DefaultVolume = 100;

        private readonly ISettingsStore _store;

        /// <summary>Current volume in percent, 0..<see cref="SoundVolume.MaxVolume"/>.</summary>
        public int Value { get; private set; }

        public MasterVolume(ISettingsStore store)
        {
            _store = store;
            string stored = store.GetString(Key, DefaultVolume.ToString(CultureInfo.InvariantCulture));
            Value = int.TryParse(stored, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
                ? SoundVolume.ClampVolume(parsed) : DefaultVolume;
        }

        /// <summary>Step the volume up (+1) or down (-1), clamped. True when the value moved; a move
        /// persists at once.</summary>
        public bool Adjust(int direction)
        {
            int next = SoundVolume.ClampVolume(Value + direction * SoundVolume.Step);
            if (next == Value) return false;
            Value = next;
            _store.SetString(Key, next.ToString(CultureInfo.InvariantCulture));
            return true;
        }
    }

    /// <summary>
    /// The saved playback volume of one mod sound, stored as a signed offset from the master volume:
    /// the effective (and displayed) <see cref="Value"/> is master plus the offset, clamped to
    /// 0..<see cref="MaxVolume"/> percent of the cue's natural level. Stepping re-derives the offset
    /// against the current master, so a later master move carries every sound with it while their
    /// relative levels hold. Persisted under "sound_volume_&lt;cue&gt;".
    /// </summary>
    public sealed class SoundVolume
    {
        public const int Step = 10;
        // Unity's AudioSource cannot amplify (its volume stops at 1), so a cue is at most as loud as
        // its file: louder means a louder file.
        public const int MaxVolume = 100;

        private readonly ISettingsStore _store;
        private readonly MasterVolume _master;
        private int _offset;

        public AudioCue Cue { get; }
        public string Key => "sound_volume_" + AudioCues.FileName(Cue);

        /// <summary>Effective volume in percent, 0..<see cref="MaxVolume"/>.</summary>
        public int Value => ClampVolume(_master.Value + _offset);

        /// <summary>The gain factor applied to the cue's natural playback volume.</summary>
        public float Gain => Value / 100f;

        public SoundVolume(AudioCue cue, ISettingsStore store, MasterVolume master)
        {
            Cue = cue;
            _store = store;
            _master = master;
            _offset = ParseOffset(store.GetString(Key, FormatOffset(0)));
        }

        /// <summary>Step the effective volume up (+1) or down (-1), clamped. True when the value
        /// moved; a move persists at once.</summary>
        public bool Adjust(int direction)
        {
            int next = ClampVolume(Value + direction * Step);
            if (next == Value) return false;
            _offset = next - _master.Value;
            _store.SetString(Key, FormatOffset(_offset));
            return true;
        }

        internal static int ClampVolume(int value) => value < 0 ? 0 : value > MaxVolume ? MaxVolume : value;

        // Offsets persist with an explicit sign ("+10", "-40", "+0").
        private static string FormatOffset(int offset) => offset.ToString("+0;-0", CultureInfo.InvariantCulture);

        private static int ParseOffset(string stored)
        {
            stored = (stored ?? "").Trim();
            if (!int.TryParse(stored, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)) return 0;
            if (stored[0] == '+' || stored[0] == '-') return parsed;
            return ClampVolume(parsed) - MasterVolume.DefaultVolume; // a bare number: an absolute percent
        }
    }

    /// <summary>
    /// How often, at most, one cue fires: the least number of seconds between two plays of it, or
    /// blank (the default) for every event. Persisted under "sound_interval_&lt;cue&gt;" as the
    /// seconds the player typed, invariant, "" when blank. A fight throws several damage events a
    /// second, so a damage cue at every event is a rattle; the interval thins it to a pulse.
    /// </summary>
    public sealed class CueInterval
    {
        private readonly ISettingsStore _store;

        public AudioCue Cue { get; }
        public string Key => "sound_interval_" + AudioCues.FileName(Cue);

        /// <summary>The seconds between plays, or null when blank (every event).</summary>
        public double? Seconds { get; private set; }

        public CueInterval(AudioCue cue, ISettingsStore store)
        {
            Cue = cue;
            _store = store;
            Seconds = Parse(store.GetString(Key, ""));
        }

        /// <summary>Set from what the player typed: blank clears it, a number of seconds (a decimal
        /// point or comma, "1.5", "0,5") sets it. False for anything else, the value unchanged.</summary>
        public bool TrySet(string typed)
        {
            string text = (typed ?? "").Trim();
            if (text.Length == 0)
            {
                Seconds = null;
                _store.SetString(Key, "");
                return true;
            }
            double? parsed = Parse(text);
            if (parsed == null) return false;
            Seconds = parsed;
            _store.SetString(Key, parsed.Value.ToString("0.###", CultureInfo.InvariantCulture));
            return true;
        }

        /// <summary>The seconds as a number to read back ("1.5"), or null when blank.</summary>
        public string Text => Seconds?.ToString("0.###", CultureInfo.InvariantCulture);

        private static double? Parse(string text)
        {
            text = (text ?? "").Trim().Replace(',', '.');
            if (text.Length == 0) return null;
            if (!double.TryParse(text, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite,
                CultureInfo.InvariantCulture, out double seconds)) return null;
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0) return null;
            return seconds;
        }
    }

    /// <summary>
    /// The per-sound settings table: the <see cref="Master"/> baseline plus one <see cref="SoundVolume"/>
    /// and one <see cref="CueInterval"/> per <see cref="AudioCue"/>, in declaration order (the order the
    /// sounds screen lists them). The audio path reads <see cref="Gain"/> and <see cref="Interval"/>
    /// live on every play.
    /// </summary>
    public sealed class SoundVolumes
    {
        private readonly SoundVolume[] _volumes;
        private readonly CueInterval[] _intervals;

        public MasterVolume Master { get; }
        public IReadOnlyList<SoundVolume> All { get; }
        public IReadOnlyList<CueInterval> Intervals { get; }

        public SoundVolumes(ISettingsStore store)
        {
            Master = new MasterVolume(store);
            var cues = (AudioCue[])Enum.GetValues(typeof(AudioCue));
            _volumes = new SoundVolume[cues.Length];
            _intervals = new CueInterval[cues.Length];
            foreach (var cue in cues)
            {
                _volumes[(int)cue] = new SoundVolume(cue, store, Master);
                _intervals[(int)cue] = new CueInterval(cue, store);
            }
            All = _volumes;
            Intervals = _intervals;
        }

        public float Gain(AudioCue cue) => _volumes[(int)cue].Gain;
        public CueInterval Interval(AudioCue cue) => _intervals[(int)cue];
    }
}

namespace MonsterTrainAccessibility.Core.Audio
{
    /// <summary>
    /// The cue playback backend. Kept an interface so Core stays engine-free; the implementation (the
    /// Unity audio sources of ours, routed into the game's SFX mixer group) lives in the module.
    /// Callers hand a finished volume and pan.
    /// </summary>
    public interface IAudioEngine
    {
        /// <summary>Whether the output is usable (false once it failed to open or died).</summary>
        bool Available { get; }

        /// <summary>Fire a one-shot cue at <paramref name="volume"/> (0..2, 1 as authored) and stereo
        /// <paramref name="pan"/> (-1 hard left .. 1 hard right). <paramref name="pitch"/> is a
        /// playback-rate multiplier (1 = as authored). A missing device or asset means silence
        /// (logged by the engine), never a throw.</summary>
        void PlayCue(AudioCue cue, float volume, float pan, float pitch = 1f);
    }

    /// <summary>
    /// The engine every caller plays through: scales each cue by that cue's saved
    /// <see cref="SoundVolumes"/> gain on top of the volume the caller computed, so the player's
    /// per-sound setting applies to all playback while the natural dynamics stay the caller's. The
    /// gain is read live on every play, so an adjustment reaches the very next cue.
    /// </summary>
    public sealed class VolumeScaledEngine : IAudioEngine
    {
        private readonly IAudioEngine _inner;
        private readonly SoundVolumes _volumes;

        public VolumeScaledEngine(IAudioEngine inner, SoundVolumes volumes)
        {
            _inner = inner;
            _volumes = volumes;
        }

        public bool Available => _inner.Available;

        public void PlayCue(AudioCue cue, float volume, float pan, float pitch = 1f)
            => _inner.PlayCue(cue, volume * _volumes.Gain(cue), pan, pitch);
    }

    /// <summary>An engine with no output: what plays before the module has one, and in tests.</summary>
    public sealed class SilentEngine : IAudioEngine
    {
        public bool Available => false;
        public void PlayCue(AudioCue cue, float volume, float pan, float pitch = 1f) { }
    }
}

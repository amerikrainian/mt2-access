using System;
using System.Collections.Generic;
using System.IO;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Core.Audio;
using UnityEngine;
using UnityEngine.Audio;
using Object = UnityEngine.Object;

namespace MonsterTrainAccessibility.Module.Audio
{
    /// <summary>
    /// The cue engine over Unity's audio, which this game plays everything through (its
    /// <c>CoreAudioSystem</c> pools AudioSources into an AudioMixer): a hidden object of ours holding a
    /// few AudioSources, each sent into the game's own SFX mixer group so the player's sound-effects
    /// volume governs the cues (the master output until that group can be reached), one clip per cue
    /// decoded from assets/audio on first play and kept. Our own sources, not the game's pool, so a
    /// cue's pan and pitch never stay behind on a source the game plays its sounds through. An
    /// AudioSource cannot amplify: a volume above 1 plays at 1. A missing or unreadable file logs once
    /// and stays silent. Released on module teardown, clips and object both: a reload builds its own.
    /// </summary>
    internal sealed class UnityCueEngine : IAudioEngine, IDisposable
    {
        private const int Voices = 4;

        private readonly string _assetRoot;
        private readonly Dictionary<AudioCue, AudioClip> _clips = new Dictionary<AudioCue, AudioClip>();
        private readonly HashSet<AudioCue> _warned = new HashSet<AudioCue>();
        private GameObject _host;
        private AudioSource[] _sources;
        private int _next;
        private bool _failed;
        private bool _routed;
        private bool _routeWarned;

        public UnityCueEngine(string assetRoot) => _assetRoot = assetRoot;

        public bool Available => !_failed;

        private bool EnsureStarted()
        {
            if (_sources != null) return true;
            if (_failed) return false;
            try
            {
                _host = new GameObject("MonsterTrainAccessibility.Cues") { hideFlags = HideFlags.HideAndDontSave };
                Object.DontDestroyOnLoad(_host);
                _sources = new AudioSource[Voices];
                for (int i = 0; i < Voices; i++)
                {
                    var source = _host.AddComponent<AudioSource>();
                    source.playOnAwake = false;
                    source.loop = false;
                    source.spatialBlend = 0f;
                    _sources[i] = source;
                }
                CoreLog.Info("audio: cue sources opened");
                return true;
            }
            catch (Exception e)
            {
                _failed = true;
                CoreLog.Warning("audio: Unity audio unavailable; cues disabled: " + e.Message);
                return false;
            }
        }

        // Send our sources into the game's SFX mixer group, so its sound-effects slider applies. The
        // group is the sound manager's, which exists only once the game's managers are up: tried on each
        // play until it is reached, the cues playing to the master output meanwhile.
        private void TryRoute()
        {
            try
            {
                AudioMixerGroup group = AllGameManagers.Instance?.GetSoundManager()?.audioSystem?.AudioSystemData?.MainSfxMixerGroup;
                if (group == null)
                {
                    if (!_routeWarned) { _routeWarned = true; CoreLog.Warning("audio: the game's SFX mixer group is not up yet; cues play to the master output until it is"); }
                    return;
                }
                foreach (var source in _sources) source.outputAudioMixerGroup = group;
                _routed = true;
                CoreLog.Info("audio: cues routed into the game's SFX mixer group");
            }
            catch (Exception e)
            {
                if (!_routeWarned) { _routeWarned = true; CoreLog.Warning("audio: SFX mixer group routing failed: " + e.Message); }
            }
        }

        public void PlayCue(AudioCue cue, float volume, float pan, float pitch = 1f)
        {
            if (volume <= 0f || !EnsureStarted()) return;
            if (!_routed) TryRoute();
            try
            {
                if (!TryLoad(cue, out var clip)) return;
                var source = NextSource();
                source.Stop();
                source.clip = clip;
                source.volume = Math.Min(1f, volume);
                source.panStereo = Math.Max(-1f, Math.Min(1f, pan));
                source.pitch = pitch;
                source.Play();
            }
            catch (Exception e)
            {
                CoreLog.Warning("audio: cue " + cue + " failed: " + e.Message);
            }
        }

        // The first idle source, else the one after the last used (the oldest sound gives way).
        private AudioSource NextSource()
        {
            for (int i = 0; i < Voices; i++)
            {
                var source = _sources[(_next + i) % Voices];
                if (!source.isPlaying) { _next = (_next + i + 1) % Voices; return source; }
            }
            var oldest = _sources[_next];
            _next = (_next + 1) % Voices;
            return oldest;
        }

        private bool TryLoad(AudioCue cue, out AudioClip clip)
        {
            if (_clips.TryGetValue(cue, out clip)) return true;
            string path = Path.Combine(_assetRoot, AudioCues.RelativePath(cue));
            if (!File.Exists(path))
            {
                if (_warned.Add(cue)) CoreLog.Warning("audio: no file for " + cue + " at " + path);
                return false;
            }
            WavFile wav;
            try { wav = WavFile.Parse(File.ReadAllBytes(path)); }
            catch (Exception e)
            {
                if (_warned.Add(cue)) CoreLog.Warning("audio: cannot read " + path + ": " + e.Message);
                return false;
            }
            clip = AudioClip.Create("MonsterTrainAccessibility." + AudioCues.FileName(cue), Math.Max(1, wav.Frames), wav.Channels, wav.SampleRate, false);
            clip.SetData(wav.Samples, 0);
            clip.hideFlags = HideFlags.HideAndDontSave;
            _clips[cue] = clip;
            return true;
        }

        public void Dispose()
        {
            try
            {
                if (_host != null) Object.Destroy(_host);
                foreach (var clip in _clips.Values) if (clip != null) Object.Destroy(clip);
            }
            catch (Exception e) { CoreLog.Warning("audio: release failed: " + e.Message); }
            _clips.Clear();
            _host = null;
            _sources = null;
        }
    }
}

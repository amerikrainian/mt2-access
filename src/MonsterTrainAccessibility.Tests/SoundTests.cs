using System;
using System.Collections.Generic;
using MonsterTrainAccessibility.Contracts;
using MonsterTrainAccessibility.Core.Audio;
using MonsterTrainAccessibility.Core.Strings;
using Xunit;

namespace MonsterTrainAccessibility.Tests
{
    // The sound settings and the cue gate, over an in-memory store and a recording engine.
    public class SoundTests
    {
        private sealed class MemoryStore : ISettingsStore
        {
            public readonly Dictionary<string, string> Values = new Dictionary<string, string>();
            public bool GetBool(string key, bool defaultValue) => Values.TryGetValue(key, out var v) ? v == "true" : defaultValue;
            public void SetBool(string key, bool value) => Values[key] = value ? "true" : "false";
            public int GetInt(string key, int defaultValue) => Values.TryGetValue(key, out var v) ? int.Parse(v) : defaultValue;
            public void SetInt(string key, int value) => Values[key] = value.ToString();
            public string GetString(string key, string defaultValue) => Values.TryGetValue(key, out var v) ? v : defaultValue;
            public void SetString(string key, string value) => Values[key] = value;
        }

        private sealed class FakeEngine : IAudioEngine
        {
            public readonly List<(AudioCue Cue, float Volume)> Plays = new List<(AudioCue, float)>();
            public bool Available => true;
            public void PlayCue(AudioCue cue, float volume, float pan, float pitch = 1f) => Plays.Add((cue, volume));
        }

        [Fact]
        public void EveryCueHasALabelAndAFileName()
        {
            foreach (AudioCue cue in Enum.GetValues(typeof(AudioCue)))
            {
                Assert.False(string.IsNullOrWhiteSpace(Strings.SoundLabel(cue)), "no label for " + cue);
                Assert.Matches("^[a-z_]+$", AudioCues.FileName(cue));
            }
            Assert.Equal("team_friendly", AudioCues.FileName(AudioCue.TeamFriendly));
        }

        [Fact]
        public void AVolumeIsAnOffsetFromTheMasterAndMovesWithIt()
        {
            var store = new MemoryStore();
            var sounds = new SoundVolumes(store);
            var enemy = sounds.All[(int)AudioCue.TeamEnemy];
            Assert.Equal(100, enemy.Value);
            Assert.True(enemy.Adjust(-1));
            Assert.Equal(90, enemy.Value);
            Assert.Equal("-10", store.Values[enemy.Key]);
            Assert.False(sounds.Master.Adjust(+1)); // the master starts at the ceiling: no amplifying
            Assert.True(sounds.Master.Adjust(-1));
            Assert.Equal(80, enemy.Value); // the offset rides the master
            Assert.Equal(0.8f, sounds.Gain(AudioCue.TeamEnemy), 3);
            for (int i = 0; i < 30; i++) enemy.Adjust(+1);
            Assert.Equal(SoundVolume.MaxVolume, enemy.Value); // clamped
            Assert.False(enemy.Adjust(+1));

            // Reloaded from the store, the same values come back.
            var again = new SoundVolumes(store);
            Assert.Equal(90, again.Master.Value);
            Assert.Equal(SoundVolume.MaxVolume, again.All[(int)AudioCue.TeamEnemy].Value);
        }

        [Fact]
        public void AnIntervalIsBlankByDefaultAndTakesSecondsOrBlank()
        {
            var store = new MemoryStore();
            var sounds = new SoundVolumes(store);
            var interval = sounds.Interval(AudioCue.TeamFriendly);
            Assert.Null(interval.Seconds);
            Assert.True(interval.TrySet("1.5"));
            Assert.Equal(1.5, interval.Seconds);
            Assert.Equal("1.5", store.Values[interval.Key]);
            Assert.True(interval.TrySet("0,25")); // a comma is a decimal point too
            Assert.Equal(0.25, interval.Seconds);
            Assert.False(interval.TrySet("abc"));
            Assert.False(interval.TrySet("-1"));
            Assert.Equal(0.25, interval.Seconds); // a rejected value changes nothing
            Assert.True(interval.TrySet("  "));
            Assert.Null(interval.Seconds);
            Assert.Equal("", store.Values[interval.Key]);
            Assert.Null(new SoundVolumes(store).Interval(AudioCue.TeamFriendly).Seconds);
        }

        [Fact]
        public void TheGateLetsEveryEventThroughUntilAnIntervalIsSet()
        {
            var store = new MemoryStore();
            var sounds = new SoundVolumes(store);
            var engine = new FakeEngine();
            double now = 0;
            var player = new CuePlayer(new VolumeScaledEngine(engine, sounds), sounds, () => now);

            Assert.True(player.Play(AudioCue.TeamFriendly));
            now = 0.01;
            Assert.True(player.Play(AudioCue.TeamFriendly)); // blank: every event
            Assert.Equal(2, engine.Plays.Count);

            sounds.Interval(AudioCue.TeamFriendly).TrySet("1");
            now = 0.5;
            Assert.False(player.Play(AudioCue.TeamFriendly)); // too soon after the last
            Assert.True(player.Play(AudioCue.TeamEnemy));  // another cue keeps its own clock
            now = 1.01;
            Assert.True(player.Play(AudioCue.TeamFriendly));
            Assert.Equal(4, engine.Plays.Count);

            // The volume setting scales what reaches the engine.
            sounds.All[(int)AudioCue.TeamFriendly].Adjust(-1);
            now = 3;
            Assert.True(player.Play(AudioCue.TeamFriendly));
            Assert.Equal(0.9f, engine.Plays[engine.Plays.Count - 1].Volume, 3);

            player.Reset();
            now = 3.1;
            Assert.True(player.Play(AudioCue.TeamFriendly)); // a new fight starts the clocks over
        }

        // A WAV file as an editor writes it: RIFF, a fmt chunk, an odd-sized chunk to skip (padded to
        // a word), then the data.
        private static byte[] Wav(int format, int channels, int rate, int bits, byte[] data, int fmtSize = 16)
        {
            var ms = new System.IO.MemoryStream();
            var w = new System.IO.BinaryWriter(ms);
            w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); w.Write(0); w.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
            w.Write(System.Text.Encoding.ASCII.GetBytes("fmt ")); w.Write(fmtSize);
            w.Write((ushort)(fmtSize >= 40 ? 0xFFFE : format)); w.Write((ushort)channels); w.Write(rate);
            w.Write(rate * channels * bits / 8); w.Write((ushort)(channels * bits / 8)); w.Write((ushort)bits);
            if (fmtSize >= 40) { w.Write((ushort)22); w.Write((ushort)bits); w.Write(0); w.Write((ushort)format); w.Write(new byte[14]); }
            w.Write(System.Text.Encoding.ASCII.GetBytes("LIST")); w.Write(3); w.Write(new byte[] { 1, 2, 3, 0 });
            w.Write(System.Text.Encoding.ASCII.GetBytes("data")); w.Write(data.Length); w.Write(data);
            return ms.ToArray();
        }

        [Fact]
        public void AWavDecodesToFloatSamples()
        {
            var pcm16 = WavFile.Parse(Wav(1, 2, 44100, 16, new byte[] { 0x00, 0x40, 0x00, 0xC0 }));
            Assert.Equal(2, pcm16.Channels);
            Assert.Equal(44100, pcm16.SampleRate);
            Assert.Equal(1, pcm16.Frames);
            Assert.Equal(0.5f, pcm16.Samples[0], 4);
            Assert.Equal(-0.5f, pcm16.Samples[1], 4);

            var pcm24 = WavFile.Parse(Wav(1, 1, 48000, 24, new byte[] { 0x00, 0x00, 0xC0 }));
            Assert.Equal(-0.5f, pcm24.Samples[0], 4);

            var pcm8 = WavFile.Parse(Wav(1, 1, 8000, 8, new byte[] { 192 }));
            Assert.Equal(0.5f, pcm8.Samples[0], 4);

            var float32 = WavFile.Parse(Wav(3, 1, 22050, 32, BitConverter.GetBytes(0.25f), fmtSize: 40));
            Assert.Equal(0.25f, float32.Samples[0], 4); // WAVE_FORMAT_EXTENSIBLE, float sub-format

            Assert.Throws<System.IO.InvalidDataException>(() => WavFile.Parse(new byte[] { 1, 2, 3 }));
            Assert.Throws<System.IO.InvalidDataException>(() => WavFile.Parse(Wav(2, 1, 8000, 4, new byte[] { 0 }))); // ADPCM
        }

        [Fact]
        public void TheShippedCuesExistAndDecode()
        {
            string root = System.IO.Path.Combine(RepoRoot(), "assets", "audio");
            foreach (AudioCue cue in Enum.GetValues(typeof(AudioCue)))
            {
                string path = System.IO.Path.Combine(root, AudioCues.RelativePath(cue));
                Assert.True(System.IO.File.Exists(path), "no file for " + cue + " at " + path);
                Assert.True(WavFile.Parse(System.IO.File.ReadAllBytes(path)).Frames > 0, cue + " is empty");
            }
        }

        private static string RepoRoot()
        {
            var dir = new System.IO.DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !System.IO.File.Exists(System.IO.Path.Combine(dir.FullName, "MonsterTrainAccessibility.slnx"))) dir = dir.Parent;
            Assert.NotNull(dir);
            return dir.FullName;
        }
    }
}

using System;
using MonsterTrainAccessibility.Core;
using ShinyShoe.Audio;
using UnityEngine;

namespace MonsterTrainAccessibility.Audio
{
    internal static class BoundaryBeepPlayer
    {
        private const string EnemyCueName = "AccessibilityTeamBoundaryEnemyBeep";
        private const string AllyCueName = "AccessibilityTeamBoundaryAllyBeep";
        private const int SampleRate = 44100;
        private const float DurationSeconds = 0.08f;
        private const float EnemyFrequencyHz = 660f;
        private const float AllyFrequencyHz = 440f;
        private const float Volume = 0.22f;
        private const float EnvelopeSeconds = 0.006f;

        private static CoreSoundEffectData _soundData;
        private static bool _disabled;

        public static void Play(bool enteringAlly)
        {
            if (_disabled)
            {
                return;
            }

            try
            {
                SoundManager soundManager = AllGameManagers.Instance?.GetSoundManager();
                CoreSoundEffectData soundData = GetSoundData();
                if (soundManager == null || soundData == null)
                {
                    return;
                }

                soundManager.PlaySfx(enteringAlly ? AllyCueName : EnemyCueName, soundData);
            }
            catch (Exception ex)
            {
                _disabled = true;
                Log.Info("[AccessibilityMod] Boundary beep disabled after playback failure: " + ex);
            }
        }

        private static CoreSoundEffectData GetSoundData()
        {
            if (_soundData != null)
            {
                return _soundData;
            }

            try
            {
                AudioClip enemyClip = CreateSineClip(EnemyCueName, EnemyFrequencyHz);
                AudioClip allyClip = CreateSineClip(AllyCueName, AllyFrequencyHz);
                CoreSoundEffectData data = ScriptableObject.CreateInstance<CoreSoundEffectData>();
                data.hideFlags = HideFlags.HideAndDontSave;
                data.Sounds = new[]
                {
                    new CoreSoundEffectData.SoundCueDefinition
                    {
                        Name = EnemyCueName,
                        Clips = new[] { enemyClip },
                        VolumeMin = 1f,
                        VolumeMax = 1f,
                        PitchMin = 1f,
                        PitchMax = 1f,
                        Loop = false,
                        Tags = Array.Empty<string>()
                    },
                    new CoreSoundEffectData.SoundCueDefinition
                    {
                        Name = AllyCueName,
                        Clips = new[] { allyClip },
                        VolumeMin = 1f,
                        VolumeMax = 1f,
                        PitchMin = 1f,
                        PitchMax = 1f,
                        Loop = false,
                        Tags = Array.Empty<string>()
                    }
                };

                _soundData = data;
                return _soundData;
            }
            catch (Exception ex)
            {
                _disabled = true;
                Log.Info("[AccessibilityMod] Boundary beep disabled after clip creation failure: " + ex);
                return null;
            }
        }

        private static AudioClip CreateSineClip(string clipName, float frequencyHz)
        {
            int sampleCount = Mathf.Max(1, Mathf.RoundToInt(SampleRate * DurationSeconds));
            int envelopeSamples = Mathf.Max(1, Mathf.RoundToInt(SampleRate * EnvelopeSeconds));
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / SampleRate;
                float envelope = 1f;
                if (i < envelopeSamples)
                {
                    envelope = (float)i / envelopeSamples;
                }
                else
                {
                    int fromEnd = sampleCount - i - 1;
                    if (fromEnd < envelopeSamples)
                    {
                        envelope = (float)fromEnd / envelopeSamples;
                    }
                }

                samples[i] = TriangleWave(frequencyHz * t) * Volume * Mathf.Clamp01(envelope);
            }

            AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static float TriangleWave(float phase)
        {
            float normalized = phase - Mathf.Floor(phase);
            return 4f * Mathf.Abs(normalized - 0.5f) - 1f;
        }
    }
}

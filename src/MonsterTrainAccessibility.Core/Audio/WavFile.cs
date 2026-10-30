using System;
using System.IO;

namespace MonsterTrainAccessibility.Core.Audio
{
    /// <summary>
    /// A WAV file decoded to the interleaved float samples a Unity clip takes: integer PCM of 8, 16, 24
    /// or 32 bits and 32-bit float, plain or WAVE_FORMAT_EXTENSIBLE, any channel count and rate. The
    /// engine builds its clip from this (Unity reads no file at runtime without a coroutine), so a cue's
    /// file is whatever an editor exports, read synchronously on its first play.
    /// </summary>
    public sealed class WavFile
    {
        public int Channels { get; }
        public int SampleRate { get; }
        /// <summary>Interleaved samples in -1..1, <see cref="Channels"/> per frame.</summary>
        public float[] Samples { get; }
        public int Frames => Samples.Length / Channels;

        private WavFile(int channels, int sampleRate, float[] samples)
        {
            Channels = channels;
            SampleRate = sampleRate;
            Samples = samples;
        }

        /// <summary>Decode the bytes of a WAV file; throws <see cref="InvalidDataException"/> naming what
        /// is wrong with one it cannot read.</summary>
        public static WavFile Parse(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 12 || Tag(bytes, 0) != "RIFF" || Tag(bytes, 8) != "WAVE")
                throw new InvalidDataException("not a RIFF WAVE file");
            int format = 0, channels = 0, rate = 0, bits = 0;
            int pos = 12;
            while (pos + 8 <= bytes.Length)
            {
                string id = Tag(bytes, pos);
                int size = BitConverter.ToInt32(bytes, pos + 4);
                int body = pos + 8;
                if (size < 0 || body + size > bytes.Length) size = bytes.Length - body; // a truncated last chunk
                if (id == "fmt ")
                {
                    if (size < 16) throw new InvalidDataException("fmt chunk too short");
                    format = BitConverter.ToUInt16(bytes, body);
                    channels = BitConverter.ToUInt16(bytes, body + 2);
                    rate = BitConverter.ToInt32(bytes, body + 4);
                    bits = BitConverter.ToUInt16(bytes, body + 14);
                    // WAVE_FORMAT_EXTENSIBLE: the real format is the sub-format GUID's first two bytes.
                    if (format == 0xFFFE && size >= 26) format = BitConverter.ToUInt16(bytes, body + 24);
                }
                else if (id == "data")
                {
                    if (channels <= 0 || rate <= 0) throw new InvalidDataException("data before fmt");
                    return new WavFile(channels, rate, Decode(bytes, body, size, format, bits));
                }
                pos = body + size + (size & 1); // chunks are word-aligned
            }
            throw new InvalidDataException("no data chunk");
        }

        private static float[] Decode(byte[] b, int start, int size, int format, int bits)
        {
            int width = bits / 8;
            if (width <= 0) throw new InvalidDataException("bits per sample " + bits);
            int count = size / width;
            var samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                int at = start + i * width;
                if (format == 3 && bits == 32) samples[i] = BitConverter.ToSingle(b, at);
                else if (format != 1) throw new InvalidDataException("format " + format + " at " + bits + " bits");
                else if (bits == 8) samples[i] = (b[at] - 128) / 128f;
                else if (bits == 16) samples[i] = BitConverter.ToInt16(b, at) / 32768f;
                else if (bits == 24) samples[i] = ((b[at] << 8 | b[at + 1] << 16 | b[at + 2] << 24) >> 8) / 8388608f;
                else if (bits == 32) samples[i] = BitConverter.ToInt32(b, at) / 2147483648f;
                else throw new InvalidDataException("PCM at " + bits + " bits");
            }
            return samples;
        }

        private static string Tag(byte[] b, int at)
            => at + 4 <= b.Length ? new string(new[] { (char)b[at], (char)b[at + 1], (char)b[at + 2], (char)b[at + 3] }) : "";
    }
}

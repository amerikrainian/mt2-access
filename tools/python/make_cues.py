"""Generate the placeholder cue files under assets/audio: one short tone (or a few) per AudioCue,
distinct enough to tell apart, so the sounds exist until authored ones replace them 1:1 by name.

    uv run python tools/python/make_cues.py [--force]

Writes only the files that are missing (--force rewrites all), mono 16-bit 44.1 kHz WAV.
The names are AudioCues.FileName (the enum name in snake case) under the cue's group folder.
Guildrun's tool; the cues are this mod's.
"""
import argparse
import math
import os
import struct
import wave

RATE = 44100
ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "assets", "audio")
# Each note's fade in and out: main's boundary beeps had 6 ms edges.
EDGE = 0.006

# group/name: (notes in Hz, note length s, gap s, waveform, amplitude)
CUES = {
    # main's boundary beeps: a triangle at 440 Hz onto your units, 660 Hz onto the enemy's, 80 ms.
    "navigation/team_friendly": ([440], 0.08, 0.0, "tri", 0.22),
    "navigation/team_enemy": ([660], 0.08, 0.0, "tri", 0.22),
}


def sample(kind, f, t):
    if kind == "sine":
        return math.sin(2 * math.pi * f * t)
    if kind == "square":
        return 1.0 if math.sin(2 * math.pi * f * t) > 0 else -1.0
    if kind == "tri":
        return 2 * abs(2 * ((t * f) % 1) - 1) - 1
    return math.sin(2 * math.pi * f * t) * 0.6 + math.sin(2 * math.pi * f * 2 * t) * 0.4


def tone(path, notes, dur, gap, kind, amp):
    frames = []
    edge = max(1, int(RATE * EDGE))
    for f in notes:
        n = int(RATE * dur)
        for i in range(n):
            env = min(1.0, i / edge) * min(1.0, (n - 1 - i) / edge)
            frames.append(int(32767 * amp * env * sample(kind, f, i / RATE)))
        frames.extend([0] * int(RATE * gap))
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(b"".join(struct.pack("<h", s) for s in frames))


def main():
    parser = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    parser.add_argument("--force", action="store_true", help="rewrite files that exist")
    args = parser.parse_args()
    for name, spec in CUES.items():
        path = os.path.normpath(os.path.join(ROOT, name + ".wav"))
        if os.path.exists(path) and not args.force:
            print("kept   ", os.path.relpath(path))
            continue
        tone(path, *spec)
        print("written", os.path.relpath(path))


if __name__ == "__main__":
    main()

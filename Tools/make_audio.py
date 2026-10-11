"""Synthesizes Pixel Kingdom Rumble's placeholder sound effects and music loops (original, chiptune style).

Everything is generated from oscillators and noise here, so there are no third-party samples or licenses.
Run: python3 Tools/make_audio.py  ->  Assets/_Project/Resources/Audio/*.wav (22050 Hz, 16-bit mono)
"""
import math, os, wave
import numpy as np

SR = 22050
OUT = os.path.join(os.path.dirname(__file__), "..", "Assets", "_Project", "Resources", "Audio")
rng = np.random.default_rng(7)


def t(sec):
    return np.arange(int(SR * sec)) / SR


def env(n, a=0.005, d=0.1, s=0.0, r=0.05, length=None):
    """ADSR envelope over n samples (times in seconds; sustain level s held until release)."""
    a_n, d_n, r_n = int(a * SR), int(d * SR), int(r * SR)
    sus_n = max(0, n - a_n - d_n - r_n)
    e = np.concatenate([np.linspace(0, 1, max(1, a_n)), np.linspace(1, s, max(1, d_n)),
                        np.full(sus_n, s), np.linspace(s, 0, max(1, r_n))])
    return np.resize(e, n) if len(e) != n else e


def square(freq, sec, duty=0.5):
    ph = np.cumsum(np.full(int(SR * sec), freq) / SR) if np.isscalar(freq) else np.cumsum(freq / SR)
    return np.where((ph % 1.0) < duty, 1.0, -1.0)


def tri(freq, sec):
    ph = np.cumsum(np.full(int(SR * sec), freq) / SR) if np.isscalar(freq) else np.cumsum(freq / SR)
    return 2 * np.abs(2 * (ph % 1.0) - 1) - 1


def sweep(f0, f1, sec, curve=1.0):
    x = np.linspace(0, 1, int(SR * sec)) ** curve
    return f0 + (f1 - f0) * x


def noise(sec):
    return rng.uniform(-1, 1, int(SR * sec))


def save(name, x, gain=0.5):
    x = np.clip(x * gain, -1, 1)
    os.makedirs(OUT, exist_ok=True)
    with wave.open(os.path.join(OUT, name + ".wav"), "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR)
        w.writeframes((x * 32767).astype(np.int16).tobytes())


# ---- Sound effects ------------------------------------------------------------------------------
def sfx():
    d = 0.18; save("jump", square(sweep(330, 660, d, 0.6), d, 0.25) * env(int(SR * d), 0.003, 0.15, 0, 0.02), 0.35)
    d = 0.1; save("land", noise(d) * env(int(SR * d), 0.001, 0.08, 0, 0.01) * np.linspace(1, 0.2, int(SR * d)), 0.3)
    d = 0.12; save("hit", (noise(d) * 0.7 + square(sweep(220, 90, d), d) * 0.5) * env(int(SR * d), 0.001, 0.1, 0, 0.01), 0.5)
    d = 0.3; save("hit_heavy", (noise(d) * 0.6 + square(sweep(160, 40, d, 0.5), d) * 0.7) * env(int(SR * d), 0.001, 0.25, 0, 0.04), 0.6)
    d = 0.25; save("hurt", square(sweep(500, 150, d), d, 0.5) * env(int(SR * d), 0.002, 0.2, 0, 0.04), 0.35)
    d = 0.06; save("swing", noise(d) * env(int(SR * d), 0.01, 0.04, 0, 0.01) * 0.6, 0.35)
    # shard: two quick bright notes
    a = square(1318.5, 0.06, 0.25) * env(int(SR * 0.06), 0.001, 0.05, 0, 0.005)
    b = square(1975.5, 0.12, 0.25) * env(int(SR * 0.12), 0.001, 0.1, 0, 0.01)
    save("shard", np.concatenate([a, b]), 0.3)
    # relic: rising arpeggio with a shimmer
    notes = [659.3, 830.6, 987.8, 1318.5]
    save("relic", np.concatenate([tri(f, 0.07) * env(int(SR * 0.07), 0.001, 0.06, 0, 0.005) for f in notes] +
                                 [tri(1318.5, 0.25) * env(int(SR * 0.25), 0.001, 0.24, 0, 0.01)]), 0.5)
    d = 0.35; save("heal", tri(sweep(440, 880, d), d) * env(int(SR * d), 0.01, 0.3, 0, 0.04), 0.5)
    # checkpoint: three-note fanfare
    save("checkpoint", np.concatenate([square(f, 0.09, 0.5) * env(int(SR * 0.09), 0.002, 0.08, 0, 0.005)
                                       for f in (523.3, 659.3, 784.0)] +
                                      [square(1046.5, 0.25, 0.5) * env(int(SR * 0.25), 0.002, 0.22, 0, 0.02)]), 0.3)
    # spotted: alarm "!!" (two rising squares)
    one = square(sweep(600, 1200, 0.09), 0.09, 0.5) * env(int(SR * 0.09), 0.001, 0.08, 0, 0.005)
    save("spotted", np.concatenate([one, np.zeros(int(SR * 0.03)), one]), 0.35)
    d = 0.2; save("takedown", (noise(d) * 0.5 * np.linspace(1, 0, int(SR * d)) + tri(sweep(300, 80, d), d) * 0.6)
                  * env(int(SR * d), 0.001, 0.18, 0, 0.02), 0.6)
    d = 0.3; save("spring", square(sweep(200, 900, d, 0.4), d, 0.3) * env(int(SR * d), 0.002, 0.28, 0, 0.02)
                  * (1 + 0.3 * np.sin(2 * np.pi * 30 * t(d))), 0.3)
    d = 0.15; save("crate", (square(sweep(180, 120, d), d) * 0.6 + noise(d) * 0.3) * env(int(SR * d), 0.001, 0.13, 0, 0.01), 0.45)
    d = 0.05; save("ui_click", square(880, d, 0.5) * env(int(SR * d), 0.001, 0.04, 0, 0.005), 0.25)
    d = 0.5; save("secret", np.concatenate([tri(f, 0.1) * env(int(SR * 0.1), 0.002, 0.09, 0, 0.005)
                                            for f in (392.0, 523.3, 659.3, 784.0, 1046.5)]), 0.5)
    d = 0.6; save("ko", square(sweep(400, 60, d, 0.7), d, 0.5) * env(int(SR * d), 0.002, 0.55, 0, 0.05), 0.4)
    # level clear: a short victory jingle
    seq = [(523.3, .1), (659.3, .1), (784.0, .1), (1046.5, .2), (784.0, .1), (1046.5, .35)]
    save("victory", np.concatenate([square(f, d, 0.5) * env(int(SR * d), 0.003, d * .8, 0.3, 0.02) for f, d in seq]), 0.3)
    d = 0.25; save("purchase", np.concatenate([tri(f, 0.08) * env(int(SR * 0.08), 0.001, 0.07, 0, 0.005)
                                               for f in (784.0, 987.8, 1174.7, 1568.0)]), 0.5)


# ---- Music: small 4-channel trackers (lead, bass, arp, drums) ------------------------------------
NOTE = {n: i for i, n in enumerate(["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"])}


def hz(name):
    if name in (None, "-"): return 0.0
    pitch, octave = name[:-1], int(name[-1])
    return 440.0 * 2 ** ((NOTE[pitch] + 12 * (octave + 1) - 69) / 12)


def track(bpm, bars, lead, bass, chords, drums, lead_duty=0.25, swing=0.0):
    """Each pattern is a list of 16th-note steps. chords: list per bar of 3 note names for the arp."""
    step = 60.0 / bpm / 4
    n_steps = bars * 16
    total = np.zeros(int(SR * step * n_steps) + SR)
    def place(i, sig):
        start = int(SR * step * i)
        total[start:start + len(sig)] += sig
    for i in range(n_steps):
        dur = step * 0.95
        ln = lead[i % len(lead)]
        if ln not in (None, "-"):
            hold = 1
            while hold < 4 and lead[(i + hold) % len(lead)] == "-": hold += 1
            dd = step * hold * 0.95
            place(i, square(hz(ln), dd, lead_duty) * env(int(SR * dd), 0.004, 0.08, 0.55, 0.03) * 0.22)
        bn = bass[i % len(bass)]
        if bn not in (None, "-"):
            place(i, tri(hz(bn), step * 1.9) * env(int(SR * step * 1.9), 0.003, 0.1, 0.6, 0.03) * 0.35)
        ch = chords[(i // 16) % len(chords)]
        place(i, square(hz(ch[i % 3]), step * 0.5, 0.5) * env(int(SR * step * 0.5), 0.002, 0.05, 0, 0.01) * 0.06)
        dr = drums[i % len(drums)]
        if dr == "k": place(i, tri(sweep(150, 40, 0.12), 0.12) * env(int(SR * .12), 0.001, 0.11, 0, 0.005) * 0.5)
        elif dr == "s": place(i, noise(0.1) * env(int(SR * .1), 0.001, 0.09, 0, 0.005) * 0.22)
        elif dr == "h": place(i, noise(0.03) * env(int(SR * .03), 0.001, 0.025, 0, 0.003) * 0.08)
    out = total[:int(SR * step * n_steps)]
    return out / max(1e-6, np.max(np.abs(out))) * 0.9


def music():
    D = ["k", None, "h", None, "s", None, "h", None, "k", "k", "h", None, "s", None, "h", "h"]
    # Menu / Story: bright, hopeful, C major
    lead = ["E5", "-", "G5", "-", "C6", "-", "B5", "A5", "G5", "-", "E5", "-", "D5", "-", "E5", "-",
            "F5", "-", "A5", "-", "C6", "-", "A5", "-", "G5", "-", "-", "-", "E5", "-", "D5", "-",
            "E5", "-", "G5", "-", "C6", "-", "D6", "-", "E6", "-", "D6", "C6", "B5", "-", "G5", "-",
            "A5", "-", "F5", "-", "D5", "-", "B4", "-", "C5", "-", "-", "-", None, None, None, None]
    bass = ["C3", None, "C3", None, "G2", None, "G2", None, "A2", None, "A2", None, "E2", None, "E2", None,
            "F2", None, "F2", None, "C3", None, "C3", None, "G2", None, "G2", None, "G2", None, "B2", None]
    chords = [("C5", "E5", "G5"), ("F4", "A4", "C5"), ("C5", "E5", "G5"), ("G4", "B4", "D5")]
    save("music_story", track(132, 8, lead, bass, chords, D), 0.45)

    # Stealth / Contracts: minor, sparse, ticking
    Ds = ["k", None, None, "h", None, None, "h", None, "k", None, "h", None, None, "h", None, "h"]
    lead = ["A4", "-", "-", "-", "C5", "-", "B4", "-", "A4", "-", "-", "-", "E4", "-", "-", "-",
            "F4", "-", "-", "-", "E4", "-", "D4", "-", "E4", "-", "-", "-", None, None, None, None,
            "A4", "-", "-", "-", "C5", "-", "D5", "-", "E5", "-", "-", "-", "D5", "-", "C5", "-",
            "B4", "-", "-", "-", "G#4", "-", "-", "-", "A4", "-", "-", "-", None, None, None, None]
    bass = ["A2", None, None, None, "A2", None, None, None, "F2", None, None, None, "E2", None, None, None]
    chords = [("A4", "C5", "E5"), ("F4", "A4", "C5"), ("D4", "F4", "A4"), ("E4", "G#4", "B4")]
    save("music_stealth", track(100, 8, lead, bass, chords, Ds, lead_duty=0.5), 0.4)

    # Versus / Arena: fast, driving
    Dv = ["k", "h", "s", "h", "k", "k", "s", "h", "k", "h", "s", "h", "k", "k", "s", "s"]
    lead = ["D5", "-", "D5", "F5", "-", "D5", "G5", "-", "F5", "-", "D5", "-", "C5", "-", "A4", "-",
            "D5", "-", "D5", "F5", "-", "A5", "-", "G5", "F5", "-", "E5", "-", "D5", "-", "-", "-"]
    bass = ["D2", "D3", "D2", "D3", "D2", "D3", "D2", "D3", "A#1", "A#2", "A#1", "A#2", "C2", "C3", "C2", "C3"]
    chords = [("D5", "F5", "A5"), ("A#4", "D5", "F5"), ("C5", "E5", "G5"), ("A4", "C#5", "E5")]
    save("music_versus", track(156, 8, lead, bass, chords, Dv, lead_duty=0.125), 0.42)


if __name__ == "__main__":
    sfx()
    music()
    print("audio written to", os.path.abspath(OUT))

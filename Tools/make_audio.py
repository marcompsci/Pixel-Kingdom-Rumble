"""Synthesizes Pixel Kingdom Rumble's placeholder sound effects and music loops (original, chiptune style).

Everything is generated from oscillators and noise here, so there are no third-party samples or licenses.
Run: python3 Tools/make_audio.py  ->  Assets/_Project/Resources/Audio/*.wav (22050 Hz, 16-bit mono)
"""
import math, os, wave
import numpy as np
from scipy.signal import butter, lfilter

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
    x = np.tanh(x * gain * 1.15) / np.tanh(1.15)   # soft limiter: loud layers squash instead of clipping
    x = np.clip(x, -1, 1)
    os.makedirs(OUT, exist_ok=True)
    with wave.open(os.path.join(OUT, name + ".wav"), "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR)
        w.writeframes((x * 32767).astype(np.int16).tobytes())


# ---- filters and building blocks -----------------------------------------------------------------
def lp(x, hz_, order=2):
    b, a = butter(order, min(0.99, hz_ / (SR / 2)), 'low')
    return lfilter(b, a, x)


def hp(x, hz_, order=2):
    b, a = butter(order, min(0.99, hz_ / (SR / 2)), 'high')
    return lfilter(b, a, x)


def bp(x, lo, hi, order=2):
    b, a = butter(order, [lo / (SR / 2), min(0.99, hi / (SR / 2))], 'band')
    return lfilter(b, a, x)


def sine(freq, sec):
    ph = np.cumsum(np.full(int(SR * sec), freq) / SR) if np.isscalar(freq) else np.cumsum(freq / SR)
    return np.sin(2 * np.pi * ph)


def saw(freq, sec):
    ph = np.cumsum(np.full(int(SR * sec), freq) / SR) if np.isscalar(freq) else np.cumsum(freq / SR)
    return 2 * (ph % 1.0) - 1


def click(sec=0.006):
    return noise(sec) * np.linspace(1, 0, int(SR * sec))


def mixdown(*parts):
    n = max(len(p) for p in parts)
    out = np.zeros(n)
    for p in parts:
        out[:len(p)] += p
    return out


def fit(x, n):
    return np.concatenate([x, np.zeros(max(0, n - len(x)))])[:n]


# ---- Sound effects ------------------------------------------------------------------------------
def sfx():
    # jump: a springy pulse sweep with a puff of air
    d = 0.2
    j = square(sweep(300, 760, d, 0.55), d, 0.25) * env(int(SR * d), 0.002, 0.17, 0, 0.02)
    save("jump", mixdown(j, hp(noise(0.05), 3000) * env(int(SR * .05), 0.001, 0.04, 0, 0.005) * 0.25), 0.36)
    # land: soft thud
    d = 0.12
    save("land", mixdown(lp(noise(d), 900) * env(int(SR * d), 0.001, 0.1, 0, 0.01) * 1.4,
                         sine(sweep(110, 50, d), d) * env(int(SR * d), 0.001, 0.1, 0, 0.01) * 0.6), 0.45)
    # hit: crack + crunchy body + low punch
    d = 0.16
    body = square(sweep(260, 90, d, 0.5), d, 0.4) * env(int(SR * d), 0.001, 0.12, 0, 0.02)
    crunch = bp(noise(d), 900, 4000) * env(int(SR * d), 0.001, 0.07, 0, 0.01) * 1.6
    punch = sine(sweep(140, 45, d), d) * env(int(SR * d), 0.001, 0.14, 0, 0.01)
    save("hit", mixdown(click() * 1.2, body * 0.45, crunch, punch * 0.8), 0.55)
    # heavy hit: big low boom, longer crunch tail, a metallic ring
    d = 0.42
    boom = sine(sweep(120, 32, d, 0.6), d) * env(int(SR * d), 0.001, 0.38, 0, 0.03)
    crunch = lp(noise(d), 2600) * env(int(SR * d), 0.001, 0.2, 0, 0.03) * 1.3
    ring = square(sweep(420, 300, d), d, 0.5) * env(int(SR * d), 0.001, 0.3, 0, 0.03) * 0.18
    save("hit_heavy", mixdown(click(0.01) * 1.5, boom * 1.1, crunch, ring), 0.6)
    d = 0.28
    save("hurt", mixdown(square(sweep(560, 160, d), d, 0.5) * env(int(SR * d), 0.002, 0.22, 0, 0.04),
                         bp(noise(d), 500, 2000) * env(int(SR * d), 0.001, 0.08, 0, 0.01) * 0.6), 0.36)
    # swing: a filtered whoosh that sweeps up (attacks)
    d = 0.16
    n_ = noise(d)
    lo = bp(n_[:len(n_) // 2], 600, 2400)
    hi = bp(n_[len(n_) // 2:], 1600, 6000)
    save("swing", np.concatenate([lo, hi]) * env(int(SR * d), 0.03, 0.12, 0, 0.01) * 1.4, 0.4)
    # special: charge-up shimmer then a release zap
    d1, d2 = 0.16, 0.22
    charge = square(sweep(300, 1200, d1, 0.7), d1, 0.125) * np.linspace(0.2, 1, int(SR * d1)) * 0.5
    zap = mixdown(square(sweep(1400, 200, d2, 0.5), d2, 0.5) * env(int(SR * d2), 0.001, 0.2, 0, 0.02) * 0.6,
                  hp(noise(d2), 2500) * env(int(SR * d2), 0.001, 0.12, 0, 0.01) * 0.4)
    save("special", np.concatenate([charge, zap]), 0.42)
    # shard: two quick bright notes with a sparkle
    a = square(1318.5, 0.06, 0.25) * env(int(SR * 0.06), 0.001, 0.05, 0, 0.005)
    b = mixdown(square(1975.5, 0.14, 0.25) * env(int(SR * 0.14), 0.001, 0.12, 0, 0.01),
                tri(3951, 0.14) * env(int(SR * 0.14), 0.001, 0.1, 0, 0.01) * 0.25)
    save("shard", np.concatenate([a, b]), 0.3)
    notes = [659.3, 830.6, 987.8, 1318.5]
    save("relic", np.concatenate([tri(f, 0.07) * env(int(SR * 0.07), 0.001, 0.06, 0, 0.005) for f in notes] +
                                 [tri(1318.5, 0.3) * env(int(SR * 0.3), 0.001, 0.28, 0, 0.01) * (1 + 0.3 * sine(7, 0.3))]), 0.5)
    d = 0.4; save("heal", tri(sweep(440, 880, d), d) * env(int(SR * d), 0.01, 0.35, 0, 0.04) * (1 + 0.25 * sine(9, d)), 0.5)
    save("checkpoint", np.concatenate([square(f, 0.09, 0.5) * env(int(SR * 0.09), 0.002, 0.08, 0, 0.005)
                                       for f in (523.3, 659.3, 784.0)] +
                                      [square(1046.5, 0.3, 0.5) * env(int(SR * 0.3), 0.002, 0.26, 0, 0.02)]), 0.3)
    one = square(sweep(600, 1200, 0.09), 0.09, 0.5) * env(int(SR * 0.09), 0.001, 0.08, 0, 0.005)
    save("spotted", np.concatenate([one, np.zeros(int(SR * 0.03)), one]), 0.35)
    d = 0.22; save("takedown", mixdown(click() * 1.2, lp(noise(d), 1500) * env(int(SR * d), 0.001, 0.15, 0, 0.02),
                                      tri(sweep(300, 70, d), d) * env(int(SR * d), 0.001, 0.2, 0, 0.02) * 0.7), 0.6)
    d = 0.3; save("spring", square(sweep(200, 900, d, 0.4), d, 0.3) * env(int(SR * d), 0.002, 0.28, 0, 0.02)
                  * (1 + 0.3 * np.sin(2 * np.pi * 30 * t(d))), 0.3)
    d = 0.18; save("crate", mixdown(click() * 1.2, (square(sweep(180, 110, d), d) * 0.5 + lp(noise(d), 1800) * 0.7)
                                    * env(int(SR * d), 0.001, 0.15, 0, 0.01)), 0.5)
    d = 0.05; save("ui_click", square(880, d, 0.5) * env(int(SR * d), 0.001, 0.04, 0, 0.005), 0.25)
    save("secret", np.concatenate([tri(f, 0.1) * env(int(SR * 0.1), 0.002, 0.09, 0, 0.005)
                                   for f in (392.0, 523.3, 659.3, 784.0, 1046.5)]), 0.5)
    d = 0.7; save("ko", mixdown(square(sweep(400, 60, d, 0.7), d, 0.5) * env(int(SR * d), 0.002, 0.6, 0, 0.05),
                                sine(sweep(90, 30, d), d) * env(int(SR * d), 0.001, 0.5, 0, 0.05) * 0.8), 0.42)
    seq = [(523.3, .1), (659.3, .1), (784.0, .1), (1046.5, .2), (784.0, .1), (1046.5, .45)]
    save("victory", np.concatenate([mixdown(square(f, d, 0.5) * env(int(SR * d), 0.003, d * .8, 0.3, 0.02),
                                            tri(f / 2, d) * env(int(SR * d), 0.003, d * .8, 0.4, 0.02) * 0.6)
                                    for f, d in seq]), 0.32)
    save("purchase", np.concatenate([tri(f, 0.08) * env(int(SR * 0.08), 0.001, 0.07, 0, 0.005)
                                     for f in (784.0, 987.8, 1174.7, 1568.0)]), 0.5)
    # jetpack: a short roaring burst (filtered noise with a low rumble), played in quick repeats while flying
    d = 0.22
    roar = bp(noise(d), 300, 2500) * 1.3 + lp(noise(d), 200) * 1.5
    save("jetpack", roar * env(int(SR * d), 0.02, 0.1, 0.6, 0.06), 0.4)
    # double jump: a bright upward flip with a sparkle
    d = 0.16
    save("air_jump", mixdown(square(sweep(520, 1250, d, 0.5), d, 0.125) * env(int(SR * d), 0.002, 0.14, 0, 0.02),
                             tri(2637, d) * env(int(SR * d), 0.001, 0.08, 0, 0.01) * 0.3), 0.34)
    barks()


# ---- voice barks for specials (synthesized: a glottal buzz through vowel formants, no recordings) -------------
VOWELS = {'a': (750, 1250, 2600), 'e': (450, 1900, 2600), 'o': (500, 900, 2400), 'u': (350, 800, 2300)}


def vowel(f0_from, f0_to, sec, v, breath=0.0):
    f0 = sweep(f0_from, f0_to, sec, 0.8) * (1 + 0.02 * sine(6, sec))
    src = saw(f0, sec) + 0.4 * square(f0, sec, 0.3)
    out = np.zeros_like(src)
    for k, fr in enumerate(VOWELS[v]):
        out += bp(src, fr * 0.85, fr * 1.15) * (1.0, 0.6, 0.3)[k]
    if breath:
        out += bp(noise(sec), 800, 4000) * breath
    return out


def bark(spec):
    parts = []
    for (kind, a, b, sec, v) in spec:
        n = int(SR * sec)
        if kind == 'h':                       # breathy onset ("h")
            parts.append(bp(noise(sec), 700, 5000) * np.linspace(0.2, 0.7, n))
        else:
            parts.append(vowel(a, b, sec, v, breath=0.15) * env(n, 0.01, sec * 0.6, 0.5, sec * 0.3))
    x = np.concatenate(parts)
    return x / max(1e-6, np.max(np.abs(x)))


def barks():
    """Four short battle shouts, pitched per hero in game ("hah!", "hyah!", "hup!", "oraa")."""
    save("bark_0", bark([('h', 0, 0, 0.05, 'a'), ('v', 230, 170, 0.2, 'a')]), 0.5)
    save("bark_1", bark([('h', 0, 0, 0.04, 'e'), ('v', 260, 300, 0.07, 'e'), ('v', 300, 200, 0.18, 'a')]), 0.5)
    save("bark_2", bark([('h', 0, 0, 0.04, 'u'), ('v', 240, 200, 0.12, 'u')]), 0.5)
    save("bark_3", bark([('v', 200, 260, 0.1, 'o'), ('v', 260, 180, 0.24, 'a')]), 0.5)


# ---- Music: a small chiptune tracker -------------------------------------------------------------
NOTE = {n: i for i, n in enumerate(["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"])}


def hz(name):
    if name in (None, "-", "."): return 0.0
    pitch, octave = name[:-1], int(name[-1])
    return 440.0 * 2 ** ((NOTE[pitch] + 12 * (octave + 1) - 69) / 12)


def P(s):
    """Pattern string -> 16th steps. Notes like E5, '-' holds, '.' rests."""
    return [None if x == '.' else x for x in s.split()]


def drum(kind):
    if kind == 'k':
        d = 0.16
        return mixdown(sine(sweep(160, 42, d, 0.4), d) * env(int(SR * d), 0.001, 0.15, 0, 0.005) * 0.9, click(0.004) * 0.5)
    if kind == 's':
        d = 0.14
        return mixdown(bp(noise(d), 1200, 7000) * env(int(SR * d), 0.001, 0.12, 0, 0.005) * 0.55,
                       tri(sweep(220, 160, d), d) * env(int(SR * d), 0.001, 0.06, 0, 0.005) * 0.4)
    if kind == 'h':
        d = 0.03
        return hp(noise(d), 7000) * env(int(SR * d), 0.001, 0.025, 0, 0.003) * 0.35
    if kind == 'o':
        d = 0.14
        return hp(noise(d), 6000) * env(int(SR * d), 0.001, 0.12, 0, 0.01) * 0.3
    if kind == 'c':
        d = 0.8
        return hp(noise(d), 4000) * env(int(SR * d), 0.001, 0.75, 0, 0.03) * 0.3
    if kind == 't':
        d = 0.14
        return tri(sweep(240, 120, d), d) * env(int(SR * d), 0.001, 0.12, 0, 0.005) * 0.5
    return np.zeros(1)


DRUMS = {k: None for k in 'ksho ct'}


def section(bpm, bars, lead=None, bass=None, chords=None, drums=None, counter=None, lead_duty=0.25,
            lead_wave='square', bass_oct=True, echo=0.3, arp_vol=0.05, vib=0.012):
    step = 60.0 / bpm / 4
    n_steps = bars * 16
    total = np.zeros(int(SR * step * (n_steps + 8)))

    def place(i, sig, delay=0.0):
        start = int(SR * (step * i + delay))
        end = min(len(total), start + len(sig))
        total[start:end] += sig[:end - start]

    def voice(pat, i, wave, duty, vol, vib_amt):
        nn = pat[i % len(pat)]
        if nn in (None, "-"): return None
        hold = 1
        while hold < 8 and pat[(i + hold) % len(pat)] == "-": hold += 1
        dd = step * hold * 0.96
        f = hz(nn) * (1 + vib_amt * np.clip(t(dd) * 4 - 0.6, 0, 1) * sine(5.5, dd))
        osc = square(f, dd, duty) if wave == 'square' else (tri(f, dd) if wave == 'tri' else saw(f, dd) * 0.6)
        return osc * env(int(SR * dd), 0.004, 0.09, 0.6, 0.04) * vol

    for i in range(n_steps):
        if lead:
            v = voice(lead, i, lead_wave, lead_duty, 0.2, vib)
            if v is not None:
                place(i, v)
                if echo: place(i, v * echo, delay=step * 3)
        if counter:
            v = voice(counter, i, 'tri', 0.5, 0.16, vib * 0.5)
            if v is not None: place(i, v)
        if bass:
            bn = bass[i % len(bass)]
            if bn not in (None, "-"):
                dd = step * 1.8
                b = tri(hz(bn), dd) * env(int(SR * dd), 0.003, 0.1, 0.6, 0.03) * 0.36
                if bass_oct: b += square(hz(bn) * 2, dd, 0.5) * env(int(SR * dd), 0.002, 0.06, 0.0, 0.01) * 0.05
                place(i, b)
        if chords:
            ch = chords[(i // 16) % len(chords)]
            for k in range(2):  # 32nd-note arpeggio
                f = hz(ch[(i * 2 + k) % len(ch)])
                place(i + k * 0.5, square(f, step * 0.45, 0.5) * env(int(SR * step * 0.45), 0.002, 0.04, 0, 0.01) * arp_vol)
        if drums:
            dk = drums[i % len(drums)]
            for ch_ in (dk or ''):
                place(i, drum(ch_))
    return total[:int(SR * step * n_steps)]


def song(*sections):
    out = np.concatenate(sections)
    out = lp(out, 9000, 1)
    return out / max(1e-6, np.max(np.abs(out))) * 0.9


# Common beats
B_POP = "k . h . s . h . k k h . s . h h".split()
B_DRIVE = "k h s h k k s h k h s h k k s so".split()
B_HALF = "k . h . . . h . s . h . . . h .".split()
B_TICK = "k . . h . . h . k . h . . h . h".split()
B_FILL = "k h s h k k s h s s t s t t s c".split()


def music():
    def beat(b): return [None if x == '.' else x for x in b]

    # Title: fanfare + a heroic loop in G major
    ttl_a = P("G4 - B4 - D5 - G5 - - - F#5 - E5 - D5 - E5 - - - C5 - - - D5 - B4 - G4 - A4 - - - - - - - . . . . "
              "G4 - B4 - D5 - G5 - - - A5 - B5 - C6 - B5 - A5 - G5 - - - F#5 - G5 - A5 - - - - - D5 - . . ")
    ttl_bass = P("G2 . G3 . G2 . G3 . C3 . C3 . E3 . C3 . D3 . D3 . F#3 . D3 . G2 . G2 . D3 . B2 .")
    ttl_ch = [("G4", "B4", "D5"), ("C5", "E5", "G5"), ("D5", "F#5", "A5"), ("G4", "B4", "D5")]
    ttl_ctr = P("B3 - - - D4 - - - E4 - - - C4 - - - A3 - - - F#4 - - - G4 - - - - - - - ")
    save("music_title", song(section(120, 8, ttl_a, ttl_bass, ttl_ch, beat(B_POP), ttl_ctr, 0.25),
                             section(120, 4, ttl_a[:64], ttl_bass, ttl_ch, beat(B_FILL), None, 0.125)), 0.45)

    # World 1 story (Sunspire Meadows): bright C major, bouncy, with a B section
    st_a = P("E5 - G5 - C6 - B5 A5 G5 - E5 - D5 - E5 - F5 - A5 - C6 - A5 - G5 - - - E5 - D5 - "
             "E5 - G5 - C6 - D6 - E6 - D6 C6 B5 - G5 - A5 - F5 - D5 - B4 - C5 - - - . . . . ")
    st_b = P("A5 - - - G5 - E5 - F5 - - - E5 - C5 - D5 - - - C5 - A4 - B4 - - - G4 - - - "
             "A4 - C5 - F5 - A5 - G5 - E5 - C5 - E5 - D5 - - - - - - - G5 - F5 - E5 - D5 - ")
    st_bass = P("C3 . C3 . G2 . G2 . A2 . A2 . E2 . E2 . F2 . F2 . C3 . C3 . G2 . G2 . G2 . B2 .")
    st_ch = [("C5", "E5", "G5"), ("F4", "A4", "C5"), ("C5", "E5", "G5"), ("G4", "B4", "D5")]
    st_bass_b = P("F2 . F2 . E2 . E2 . D2 . D2 . G2 . G2 .")
    st_ch_b = [("F4", "A4", "C5"), ("E4", "G4", "C5"), ("D4", "F4", "A4"), ("G4", "B4", "D5")]
    save("music_story", song(section(132, 8, st_a, st_bass, st_ch, beat(B_POP)),
                             section(132, 8, st_b, st_bass_b, st_ch_b, beat(B_POP), P("C4 - - - - - - - G3 - - - - - - - "), 0.5),
                             section(132, 8, st_a, st_bass, st_ch, beat(B_DRIVE))), 0.45)

    # World 2 (Frostgear Peaks): E minor, airy triangle bells and a slow half-time beat
    fr_a = P("B5 - - - G5 - E5 - F#5 - - - D5 - B4 - C5 - - - E5 - G5 - F#5 - - - - - - - "
             "B5 - - - D6 - B5 - A5 - - - F#5 - D5 - E5 - - - G5 - B5 - E6 - - - - - - - ")
    fr_bass = P("E2 . . . E3 . . . C2 . . . C3 . . . D2 . . . D3 . . . B1 . . . B2 . . .")
    fr_ch = [("E4", "G4", "B4", "E5"), ("C4", "E4", "G4", "C5"), ("D4", "F#4", "A4", "D5"), ("B3", "D#4", "F#4", "B4")]
    save("music_frost", song(section(104, 8, fr_a, fr_bass, fr_ch, beat(B_HALF), None, 0.5, 'tri', echo=0.45, arp_vol=0.07),
                             section(104, 8, fr_a, fr_bass, fr_ch, beat(B_POP), P("E4 - - - - - - - G4 - - - F#4 - - - "), 0.25,
                                     echo=0.35)), 0.45)

    # World 3 (the Shattered Realms): driving D minor with a saw lead
    sr_a = P("D5 . D5 F5 . D5 A5 - G5 . F5 . E5 - C5 - D5 . D5 F5 . A5 C6 - A#5 - A5 - G5 - F5 - "
             "D5 . D5 F5 . D5 A5 - G5 . F5 . E5 - G5 - A5 - - - C#6 - - - D6 - - - - - - - ")
    sr_bass = P("D2 D3 D2 D3 D2 D3 D2 D3 A#1 A#2 A#1 A#2 C2 C3 C2 C3 G1 G2 G1 G2 A1 A2 A1 A2")
    sr_ch = [("D5", "F5", "A5"), ("A#4", "D5", "F5"), ("C5", "E5", "G5"), ("A4", "C#5", "E5")]
    save("music_realms", song(section(150, 8, sr_a, sr_bass, sr_ch, beat(B_DRIVE), None, 0.25, 'saw', echo=0.25),
                              section(150, 4, sr_a[:64], sr_bass, sr_ch, beat(B_FILL), None, 0.125)), 0.45)

    # Boss: tense, chromatic, pounding
    bs_a = P("A4 - A4 - C5 - A4 - D#5 - D5 - C5 - A4 - G#4 - A4 - - - E5 - D#5 - D5 - C5 - "
             "A4 - A4 - C5 - A4 - D#5 - D5 - C5 - A4 - F5 - E5 - D#5 - E5 - A5 - - - - - - - ")
    bs_bass = P("A1 A1 A2 A1 A1 A1 A2 A1 F1 F1 F2 F1 E1 E1 E2 E1")
    bs_ch = [("A4", "C5", "E5"), ("A4", "C5", "D#5"), ("F4", "A4", "C5"), ("E4", "G#4", "B4")]
    save("music_boss", song(section(164, 16, bs_a, bs_bass, bs_ch, beat("k h k h s h k h k h k h s h s s".split()),
                                    P("E4 - - - - - - - D#4 - - - - - - - "), 0.125, 'square', echo=0.2, arp_vol=0.06)), 0.45)

    # Stealth / Contracts: minor, sparse, ticking
    sl = P("A4 - - - C5 - B4 - A4 - - - E4 - - - F4 - - - E4 - D4 - E4 - - - . . . . "
           "A4 - - - C5 - D5 - E5 - - - D5 - C5 - B4 - - - G#4 - - - A4 - - - . . . . ")
    sl_bass = P("A2 . . . A2 . . . F2 . . . E2 . . .")
    sl_ch = [("A4", "C5", "E5"), ("F4", "A4", "C5"), ("D4", "F4", "A4"), ("E4", "G#4", "B4")]
    save("music_stealth", song(section(100, 8, sl, sl_bass, sl_ch, beat(B_TICK), None, 0.5, 'tri', echo=0.5, arp_vol=0.04)), 0.4)

    # Versus / Arena: fast and driving
    vs = P("D5 - D5 F5 - D5 G5 - F5 - D5 - C5 - A4 - D5 - D5 F5 - A5 - G5 F5 - E5 - D5 - - - ")
    vs_bass = P("D2 D3 D2 D3 D2 D3 D2 D3 A#1 A#2 A#1 A#2 C2 C3 C2 C3")
    vs_ch = [("D5", "F5", "A5"), ("A#4", "D5", "F5"), ("C5", "E5", "G5"), ("A4", "C#5", "E5")]
    save("music_versus", song(section(156, 8, vs, vs_bass, vs_ch, beat(B_DRIVE), None, 0.125, echo=0.2),
                              section(156, 2, vs, vs_bass, vs_ch, beat(B_FILL), None, 0.125)), 0.42)


# ---- Holiday + Halloween modes ------------------------------------------------------------------------------------
def sfx_events():
    # shoot: a tight pop with a bright snap (the zombie-mode guns)
    d = 0.11
    save("shoot", mixdown(click(0.004) * 1.4, square(sweep(900, 220, d, 0.4), d, 0.5) * env(int(SR * d), 0.001, 0.09, 0, 0.01) * 0.5,
                          bp(noise(d), 1500, 6000) * env(int(SR * d), 0.001, 0.05, 0, 0.005) * 0.9), 0.42)
    # reload: two mechanical clacks
    one = mixdown(click(0.006), bp(noise(0.05), 800, 3000) * env(int(SR * 0.05), 0.001, 0.04, 0, 0.005))
    save("reload", np.concatenate([one, np.zeros(int(SR * 0.12)), one * 1.2]), 0.4)
    # groan: a low wobbling voice-like drone (zombies), formant-ish band-pass on a buzz
    d = 0.9
    buzz = saw(sweep(95, 70, d) * (1 + 0.04 * sine(6, d)), d)
    save("groan", bp(buzz, 250, 1100) * env(int(SR * d), 0.08, 0.5, 0.4, 0.25) * 1.6, 0.45)
    # round start: a deep bell toll
    d = 2.2
    bell = mixdown(sine(196, d), sine(196 * 2.76, d) * 0.4, sine(196 * 5.4, d) * 0.2, sine(98, d) * 0.6)
    save("round_start", bell * env(int(SR * d), 0.002, 2.0, 0, 0.15), 0.5)
    # power-up: rising sparkle arpeggio
    save("powerup", np.concatenate([tri(f, 0.06) * env(int(SR * 0.06), 0.001, 0.05, 0, 0.005)
                                    for f in (523.3, 659.3, 784.0, 1046.5, 1318.5, 1568.0)]), 0.5)
    # board: hammering a board back up
    d = 0.12
    save("board", mixdown(click(0.008) * 1.5, lp(noise(d), 1200) * env(int(SR * d), 0.001, 0.1, 0, 0.01),
                          tri(sweep(220, 140, d), d) * env(int(SR * d), 0.001, 0.1, 0, 0.01) * 0.5), 0.5)
    # burst: a wet pop (bloater)
    d = 0.4
    save("burst", mixdown(lp(noise(d), 700) * env(int(SR * d), 0.001, 0.35, 0, 0.03) * 1.6,
                          sine(sweep(160, 40, d), d) * env(int(SR * d), 0.001, 0.3, 0, 0.03)), 0.55)


def music_events():
    def beat(b): return [None if x == '.' else x for x in b]
    # Holiday: bright, jingly F major with sleigh-bell hats
    hl = P("C5 - A4 - C5 - F5 - - - E5 - D5 - C5 - - - A4 - G4 - A4 - C5 - D5 - - - . . "
           "C5 - A4 - C5 - F5 - - - G5 - A5 - A#5 - - - A5 - G5 - F5 - - - - - - - . . . . ")
    hl_bass = P("F2 . C3 . F2 . C3 . A#1 . F2 . A#1 . F2 . C2 . G2 . C2 . G2 . F2 . C3 . F2 . C3 .")
    hl_ch = [("F4", "A4", "C5"), ("A#4", "D5", "F5"), ("C5", "E5", "G5"), ("F4", "A4", "C5")]
    jingle = "k h h h s h h h k h h h s h hh h".split()
    save("music_holiday", song(section(138, 8, hl, hl_bass, hl_ch, beat(jingle), P("F4 - - - - - - - A4 - - - G4 - - - "), 0.25,
                                       'tri', echo=0.3, arp_vol=0.06),
                               section(138, 8, hl, hl_bass, hl_ch, beat(B_POP), None, 0.25)), 0.45)
    # Haunted: E minor, spooky organ-like triangle lead with a tritone sting, slow and creeping
    hn = P("E4 - - - G4 - - - A#4 - - - B4 - - - C5 - B4 - A#4 - G4 - E4 - - - - - - - "
           "E5 - - - D5 - C5 - B4 - - - A#4 - - - A4 - G4 - F#4 - G4 - E4 - - - - - - - ")
    hn_bass = P("E2 . . . E2 . . . C2 . . . B1 . . .")
    hn_ch = [("E4", "G4", "B4"), ("E4", "G4", "A#4"), ("C4", "E4", "G4"), ("B3", "D#4", "F#4")]
    save("music_haunted", song(section(96, 8, hn, hn_bass, hn_ch, beat(B_TICK), None, 0.5, 'tri', echo=0.55, arp_vol=0.05, vib=0.03),
                               section(96, 8, hn, hn_bass, hn_ch, beat(B_HALF), P("B3 - - - - - - - A#3 - - - - - - - "), 0.5,
                                       'square', echo=0.45, arp_vol=0.05, vib=0.03)), 0.45)
    # Survival: pounding C minor with a saw lead, gets the heart going
    sv = P("C5 . C5 D#5 . C5 G5 - F5 . D#5 . D5 - A#4 - C5 . C5 D#5 . G5 A#5 - G#5 - G5 - F5 - D#5 - ")
    sv_bass = P("C2 C3 C2 C3 C2 C3 C2 C3 G#1 G#2 G#1 G#2 A#1 A#2 A#1 A#2")
    sv_ch = [("C5", "D#5", "G5"), ("G#4", "C5", "D#5"), ("A#4", "D5", "F5"), ("G4", "B4", "D5")]
    save("music_survival", song(section(150, 8, sv, sv_bass, sv_ch, beat(B_DRIVE), None, 0.25, 'saw', echo=0.25),
                                section(150, 4, sv, sv_bass, sv_ch, beat(B_FILL), P("G4 - - - - - - - F4 - - - D#4 - - - "), 0.125)), 0.45)


def music_world4():
    def beat(b): return [None if x == '.' else x for x in b]
    # Starfall Observatory: twinkly A major arpeggios over a floating half-time beat, a music-box lead
    st = P("E5 - A5 - C#6 - B5 - A5 - E5 - F#5 - - - E5 - C#5 - D5 - E5 - - - - - - - . . "
           "E5 - A5 - C#6 - E6 - D6 - C#6 - B5 - - - A5 - G#5 - A5 - B5 - - - - - - - . . . . ")
    st_bass = P("A2 . . . E3 . . . F#2 . . . C#3 . . . D2 . . . A2 . . . E2 . . . E3 . . .")
    st_ch = [("A4", "C#5", "E5", "A5"), ("F#4", "A4", "C#5", "F#5"), ("D4", "F#4", "A4", "D5"), ("E4", "G#4", "B4", "E5")]
    save("music_astral", song(section(112, 8, st, st_bass, st_ch, beat(B_HALF), None, 0.5, 'tri', echo=0.5, arp_vol=0.08),
                              section(112, 8, st, st_bass, st_ch, beat(B_POP), P("C#4 - - - - - - - B3 - - - A3 - - - "), 0.25,
                                      echo=0.4, arp_vol=0.07)), 0.45)


if __name__ == "__main__":
    import sys
    only = sys.argv[1:] or ['sfx', 'music']
    if 'sfx' in only: sfx()
    if 'music' in only: music()
    if 'events' in only: sfx_events(); music_events()
    if 'world4' in only: music_world4()
    print("audio written to", os.path.abspath(OUT))

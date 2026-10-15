"""
Level-select world maps (all original), one per Story world, 240x128 pixels (shown 4x): islands, sea or sky, and
landmarks in the world's colours. The route (dots and level nodes) is drawn on top at runtime.
"""
import math, random
from px import Canvas, Ramp, shift, mix
from tiles import C

MW, MH = 240, 128


def _noise_islands(c, r, col, n, rx=(14, 30), ry=(8, 16), y_range=(18, 110)):
    for _ in range(n):
        cx, cy = r.randrange(10, MW - 10), r.randrange(*y_range)
        a, b = r.uniform(*rx), r.uniform(*ry)
        c.fill(c.mask_ellipse(cx, cy, a, b), Ramp(col))
        c.fill(c.mask_ellipse(cx - a * 0.2, cy + b * 0.25, a * 0.6, b * 0.5), Ramp(shift(col, dl=0.08)))


def _waves(c, r, col, n=60):
    for _ in range(n):
        x, y = r.randrange(MW), r.randrange(MH)
        if c.get(x, y) is None and c.get(x + 2, y) is None:
            for k in range(3):
                c.put(x + k, y + (1 if k == 1 else 0), col)


def _base(col):
    c = Canvas(MW, MH)
    c.flat(c.mask_rect(0, 0, MW - 1, MH - 1), col)
    return c


def sunspire(seed=41):
    r = random.Random(seed)
    c = Canvas(MW, MH)
    sea = C(90, 170, 230)
    _noise_islands(c, r, C(110, 190, 90), 7)
    _noise_islands(c, r, C(150, 200, 110), 4, rx=(8, 14), ry=(5, 8))
    for (x, y, rr) in ((40, 90, 7), (180, 40, 9)):          # gear towers
        c.fill(c.mask_ellipse(x, y, rr, rr), Ramp(C(220, 180, 90)))
        c.fill(c.mask_ellipse(x, y, rr * 0.45, rr * 0.45), Ramp(C(150, 110, 60)))
    for (x, y) in ((70, 30), (200, 100)):                     # snowy peaks (Frostgear)
        c.fill(c.mask_poly([(x - 12, y - 8), (x + 12, y - 8), (x, y + 12)]), Ramp(C(200, 210, 230)))
        c.fill(c.mask_poly([(x - 4, y + 4), (x + 4, y + 4), (x, y + 12)]), Ramp(C(250, 250, 255)))
    out = _base(sea)
    _waves(out, r, C(150, 210, 245))
    for y in range(MH):
        for x in range(MW):
            if c.px[y][x] is not None:
                out.px[y][x] = c.px[y][x]
    return out


def realms(seed=42):
    r = random.Random(seed)
    out = Canvas(MW, MH)
    quads = ((0, 0, C(120, 40, 30)), (MW // 2, 0, C(120, 190, 240)), (0, MH // 2, C(40, 120, 130)), (MW // 2, MH // 2, C(220, 120, 170)))
    for (x0, y0, col) in quads:
        out.flat(out.mask_rect(x0, y0, x0 + MW // 2 - 1, y0 + MH // 2 - 1), col)
    for _ in range(30):                                        # lava cracks, clouds, bubbles, toy blocks
        x, y = r.randrange(0, MW // 2), r.randrange(0, MH // 2)
        out.put(x, y, C(255, 140, 50))
        out.put(x + 1, y, C(255, 200, 80))
    for _ in range(8):
        x, y = r.randrange(MW // 2 + 8, MW - 8), r.randrange(8, MH // 2 - 6)
        out.fill(out.mask_ellipse(x, y, 7, 3), Ramp(C(250, 250, 255)))
    for _ in range(25):
        x, y = r.randrange(4, MW // 2), r.randrange(MH // 2 + 2, MH - 2)
        out.put(x, y, C(150, 230, 220))
    for _ in range(10):
        x, y = r.randrange(MW // 2 + 4, MW - 8), r.randrange(MH // 2 + 4, MH - 8)
        out.fill(out.mask_rect(x, y, x + 4, y + 4), Ramp(r.choice([C(255, 230, 90), C(120, 200, 255), C(255, 255, 255)])))
    # the crossing in the middle
    out.fill(out.mask_ellipse(MW // 2, MH // 2, 16, 12), Ramp(C(110, 80, 160)))
    out.fill(out.mask_ellipse(MW // 2, MH // 2, 7, 6), Ramp(C(220, 160, 255)))
    return out


def starfall(seed=43):
    r = random.Random(seed)
    out = _base(C(22, 20, 56))
    for _ in range(160):
        out.put(r.randrange(MW), r.randrange(MH), r.choice([C(255, 255, 255), C(190, 200, 255), C(255, 230, 160)]))
    _noise_islands(out, r, C(80, 72, 150), 6, rx=(10, 22), ry=(5, 10))
    out.fill(out.mask_ellipse(200, 96, 14, 14), Ramp(C(250, 220, 170)))   # ringed planet
    for k in range(-24, 25):
        out.put(200 + k, 96 + int(k * 0.2), C(255, 236, 190))
    # the observatory at the top of the route
    out.fill(out.mask_rect(206, 14, 226, 28), Ramp(C(90, 84, 150)))
    out.fill(out.mask_ellipse(216, 28, 11, 8), Ramp(C(220, 180, 100)))
    out.fill(out.mask_capsule(218, 32, 228, 42, 2), Ramp(C(250, 200, 80)))
    return out


ALL = {'map_sunspire': sunspire, 'map_realms': realms, 'map_starfall': starfall}

"""Effects: soft glow, blob shadow, per-fighter projectile sprites (face right)."""
import math
from PIL import Image
from px import Canvas, Ramp, shift, mix
from tiles import C


def glow(size=64):
    """white radial falloff (bilinear-filtered in Unity, tinted per use)"""
    im = Image.new('RGBA', (size, size))
    p = im.load()
    c = (size - 1) / 2
    for y in range(size):
        for x in range(size):
            d = math.hypot(x - c, y - c) / c
            a = max(0.0, 1 - d) ** 2.2
            p[x, y] = (255, 255, 255, int(255 * a))
    return im


def shadow(w=48, h=12):
    im = Image.new('RGBA', (w, h))
    p = im.load()
    for y in range(h):
        for x in range(w):
            d = ((x - (w - 1) / 2) / (w / 2)) ** 2 + ((y - (h - 1) / 2) / (h / 2)) ** 2
            a = max(0.0, 1 - d) ** 1.4
            p[x, y] = (10, 6, 20, int(150 * a))
    return im


def _trail(c, x0, x1, y, col, th=2):
    for x in range(int(x0), int(x1)):
        t = (x - x0) / max(1, x1 - x0)
        for k in range(-th, th + 1):
            if abs(k) <= th * t + 0.5:
                c.put(x, y + k, mix(col, (255, 255, 255), t * 0.4) + (int(60 + 180 * t),), lock=True)


def shot(kind, col, acc=None):
    acc = acc or (255, 255, 255)
    if kind == 'comet':
        c = Canvas(40, 20)
        _trail(c, 0, 28, 10, col, 3)
        c.fill(c.mask_ellipse(30, 10, 6, 6), Ramp(col))
        star = []
        for i in range(10):
            a = i * math.pi / 5
            r = 7 if i % 2 == 0 else 3
            star.append((30 + math.cos(a) * r, 10 + math.sin(a) * r))
        c.fill(c.mask_poly(star), Ramp(shift(col, dl=0.1)))
        c.specular(28, 12)
        c.specular(29, 12)
    elif kind == 'crescent':
        c = Canvas(24, 32)
        for a in range(-80, 81, 3):
            for r in range(9, 14):
                x = 6 + math.cos(math.radians(a)) * r
                y = 16 + math.sin(math.radians(a)) * r * 1.1
                t = (r - 9) / 4
                c.put(x, y, mix(col, (255, 255, 255), t * 0.6) + (int(140 + 115 * (1 - abs(a) / 90)),), lock=True)
    elif kind == 'star':
        c = Canvas(24, 24)
        pts = []
        for i in range(8):
            a = i * math.pi / 4 + 0.3
            r = 10 if i % 2 == 0 else 3.5
            pts.append((12 + math.cos(a) * r, 12 + math.sin(a) * r))
        c.fill(c.mask_poly(pts), Ramp(col))
        c.fill(c.mask_ellipse(12, 12, 2, 2), Ramp(acc))
        c.outline(tint=0.3)
    elif kind == 'lance':
        c = Canvas(48, 16)
        _trail(c, 0, 20, 8, col, 2)
        c.fill(c.mask_poly([(14, 5), (40, 7), (47, 8), (40, 9), (14, 11)]), Ramp(col), light=(-0.2, 0.9))
        for x in range(16, 44):
            c.put(x, 8, (255, 255, 240), lock=True)
    elif kind == 'bolt':
        c = Canvas(32, 16)
        _trail(c, 0, 14, 8, col, 2)
        c.fill(c.mask_capsule(12, 8, 28, 8, 3.5), Ramp(col))
        for x in range(14, 28):
            c.put(x, 9, (255, 255, 255), lock=True)
    elif kind == 'spark':
        c = Canvas(32, 24)
        pts = [(2, 12), (10, 16), (14, 8), (20, 18), (24, 6), (30, 12)]
        for i in range(len(pts) - 1):
            for (x, y) in c.line_pts(*pts[i], *pts[i + 1]):
                c.put(x, y, (255, 255, 255), lock=True)
                c.put(x, y + 1, col, lock=True)
                c.put(x, y - 1, col, lock=True)
        c.fill(c.mask_ellipse(28, 12, 4, 4), Ramp(col))
    elif kind == 'shard':
        c = Canvas(32, 16)
        _trail(c, 0, 12, 8, col, 1)
        c.fill(c.mask_poly([(10, 8), (18, 13), (31, 8), (18, 3)]), Ramp(col), light=(-0.3, 0.9))
        c.outline(tint=0.4)
    elif kind == 'blob':
        c = Canvas(32, 24)
        for i, (x, y, r) in enumerate(((8, 12, 4), (15, 12, 6), (22, 12, 8))):
            c.fill(c.mask_ellipse(x, y, r, r * 0.85), Ramp(col))
        c.specular(20, 16)
        c.specular(21, 16)
        c.specular(20, 17)
    elif kind == 'ball':
        c = Canvas(24, 24)
        c.fill(c.mask_ellipse(14, 12, 8, 8), Ramp((250, 250, 250)))
        for (x, y) in ((14, 12), (15, 13), (13, 13), (14, 11), (10, 15), (18, 8)):
            c.put(x, y, (40, 40, 50))
        c.specular(11, 16)
        c.outline(tint=0.2)
        _trail(c, 0, 6, 12, col, 2)
    elif kind == 'puck':
        c = Canvas(24, 16)
        _trail(c, 0, 10, 8, col, 2)
        c.fill(c.mask_ellipse(16, 8, 6, 3.5), Ramp((40, 40, 50)))
        c.put(14, 10, (120, 120, 140), lock=True)
        c.outline(tint=0.2)
    elif kind == 'rock':
        c = Canvas(24, 24)
        c.fill(c.mask_poly([(6, 6), (16, 3), (21, 10), (18, 19), (8, 20), (4, 13)]), Ramp(col))
        c.put(10, 14, (96, 176, 80))
        c.outline(tint=0.3)
    else:  # orb
        c = Canvas(24, 24)
        c.fill(c.mask_ellipse(12, 12, 8, 8), Ramp(col))
        c.specular(9, 15)
    return c


SHOTS = {
    'nova': ('comet', C(255, 210, 90)), 'brick': ('rock', C(170, 160, 150)), 'luma': ('spark', C(140, 230, 255)),
    'rex_rollo': ('puck', C(255, 230, 120)), 'kai_tempest': ('crescent', C(200, 240, 255)),
    'mara_vex': ('star', C(200, 204, 224), C(200, 40, 70)), 'volt_ramirez': ('spark', C(255, 240, 90)),
    'sola_brightwing': ('lance', C(255, 220, 120)), 'grizz': ('rock', C(150, 104, 60)), 'pixel_pip': ('bolt', C(255, 110, 100)),
    'nyx_frost': ('shard', C(170, 230, 255)), 'blaze_torres': ('blob', C(90, 170, 255)), 'juno_strike': ('ball', C(255, 255, 255)),
    'ollie_kickflip': ('bolt', C(90, 220, 170)),
    'juni_bloom': ('crescent', C(150, 230, 140)), 'marco_brio': ('blob', C(90, 170, 255)),
    'tamsin_reed': ('star', C(230, 200, 255), C(150, 90, 200)), 'dex_ortega': ('bolt', C(255, 120, 200)),
    'pepper_quinn': ('ball', C(255, 170, 80)), 'bo_tidewell': ('blob', C(120, 210, 255)),
    'zuri_starling': ('star', C(255, 240, 160), C(160, 170, 230)), 'otto_brass': ('spark', C(255, 210, 120)),
}

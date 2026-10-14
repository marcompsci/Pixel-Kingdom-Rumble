"""
Props, pickups, hazards and enemies at 2x detail. Every function returns a Canvas whose size is exactly
twice the old placeholder (so world sizes stay the same at 32 px per unit).
"""
import math, random
from px import Canvas, Ramp, shift, mix, INK
from tiles import THEMES, make_top, make_stone, make_plank, C
import chars
from chars import draw_fighter, P, METAL, GOLD, WOOD

WHITE = C(255, 255, 255)


def finish(c, outline=True, rim=True):
    c.separate(0.45)
    if rim:
        c.rim(t=0.25)
    if outline:
        c.outline(tint=0.3)
    return c


def gem(c, cx, cy, rx, ry, col, facets=True):
    R = Ramp(col)
    pts = c.mask_poly([(cx, cy + ry), (cx + rx, cy), (cx, cy - ry), (cx - rx, cy)])
    p = c.fill(pts, R)
    if facets:
        for (x, y) in pts:
            if x < cx and y > cy:
                c.put(x, y, R.hi if (x + y) % 3 == 0 else R.light, p)
            elif x >= cx and y < cy:
                c.put(x, y, R.shadow, p)
        c.specular(int(cx - rx * 0.4), int(cy + ry * 0.35))
        c.specular(int(cx - rx * 0.4) + 1, int(cy + ry * 0.35) + 1)
    return p


# ---- terrain defaults (ph_ground / ph_stone / ph_oneway) ------------------------------------------------
def ground():
    return make_top(THEMES['meadow'], 3)


def stone():
    return make_stone(THEMES['meadow'], 9)


def oneway():
    return make_plank(THEMES['meadow'], 0)


# ---- hazards / gadgets -------------------------------------------------------------------------------------
def spikes():
    c = Canvas(32, 16)
    c.fill(c.mask_rect(0, 0, 31, 4), Ramp(C(70, 66, 84)))
    for i in range(4):
        x0 = i * 8
        pts = c.mask_poly([(x0 + 0.5, 4), (x0 + 7.5, 4), (x0 + 4, 15.5)])
        p = c.fill(pts, Ramp(C(200, 210, 228)), light=(-0.9, 0.3))
        for y in range(5, 15):
            c.put(x0 + 3, y, C(240, 246, 255), p) if y < 13 else None
        c.put(x0 + 4, 15, C(255, 120, 110), lock=True)
    for x in range(32):
        c.put(x, 0, C(40, 36, 52))
    return finish(c, outline=False)


def spring_pad():
    c = Canvas(32, 16)
    c.fill(c.mask_rect(4, 0, 27, 2, r=1), Ramp(C(90, 90, 110)))
    for k in range(4):
        y = 3 + k * 2
        c.fill(c.mask_rect(8 + (k % 2), y, 23 - (k % 2), y + 1, r=0), Ramp(GOLD))
    c.fill(c.mask_rect(3, 11, 28, 15, r=2), Ramp(C(236, 70, 70)))
    for x in range(6, 26, 3):
        c.put(x, 14, C(255, 180, 170), lock=True)
    return finish(c)


def platform_plate():
    c = Canvas(32, 16)
    R = Ramp(C(206, 160, 88))
    for y in range(16):
        for x in range(32):
            col = R.light if y >= 13 else (R.base if y >= 4 else R.shadow)
            if y == 15:
                col = R.hi
            if y == 0:
                col = R.deep
            if x % 16 == 15 and 1 <= y <= 14:
                col = R.deep
            c.put(x, y, col)
    for x in (4, 12, 20, 28):
        c.put(x, 9, R.hi, lock=True)
        c.put(x + 1, 8, R.deep)
    return c


def checkpoint():
    """grayscale banner (tinted in game) on a lit pole; 32x64"""
    c = Canvas(32, 64)
    c.fill(c.mask_rect(9, 0, 22, 3, r=1), Ramp(C(120, 110, 130)))
    c.fill(c.mask_rect(13, 2, 16, 60), Ramp(C(220, 220, 228)), light=(-1, 0.1))
    c.fill(c.mask_ellipse(14.5, 61, 3, 3), Ramp(C(250, 250, 250)))
    cloth = c.mask_poly([(16, 58), (30, 54), (28, 46), (31, 40), (16, 42)])
    c.fill(cloth, Ramp(C(220, 220, 220)), light=(-0.3, 0.9))
    for (x, y) in cloth:
        if (x + y) % 7 == 0:
            c.put(x, y, C(190, 190, 196))
    gem(c, 22, 50, 3, 3.5, C(245, 245, 245))
    return finish(c)


def goal_gate():
    """grand arch with a glowing portal and a gear keystone; 64x96"""
    c = Canvas(64, 96)
    stoneR = C(206, 188, 160)
    # pillars
    for x0 in (2, 50):
        c.fill(c.mask_rect(x0, 0, x0 + 11, 70, r=1), Ramp(stoneR), light=(-0.8, 0.3))
        for y in range(6, 70, 10):
            for x in range(x0 + 1, x0 + 11):
                c.put(x, y, shift(stoneR, dl=-0.18))
        c.fill(c.mask_rect(x0 - 1, 0, x0 + 12, 5, r=1), Ramp(shift(stoneR, dl=-0.08)))
        c.fill(c.mask_rect(x0 - 1, 66, x0 + 12, 72, r=1), Ramp(GOLD))
    # arch
    arch = [p for p in c.mask_ellipse(32, 70, 30, 24) if p[1] >= 70 and (p[0] - 32) ** 2 / 400 + (p[1] - 70) ** 2 / 240 > 1]
    c.fill(arch, Ramp(stoneR))
    trim = [p for p in c.mask_ellipse(32, 70, 21, 16) if p[1] >= 70 and (p[0] - 32) ** 2 / 361 + (p[1] - 70) ** 2 / 196 > 1]
    c.fill(trim, Ramp(GOLD), soft=True)
    # portal glow
    portal = c.mask_rect(13, 0, 50, 70)
    portal += [p for p in c.mask_ellipse(32, 70, 19, 14) if p[1] > 70]
    for (x, y) in portal:
        if c.get(x, y) is not None:
            continue
        t = y / 86
        swirl = math.sin(x * 0.4 + y * 0.18) * 0.5 + 0.5
        col = mix(C(120, 230, 255), C(255, 250, 220), t * 0.6 + swirl * 0.3)
        c.put(x, y, col + (int(150 + 80 * swirl),), lock=True)
    # keystone gear
    gear_at(c, 32, 86, 8, Ramp(GOLD))
    c.fill(c.mask_ellipse(32, 86, 3, 3), Ramp(C(120, 230, 255)))
    c.specular(31, 87)
    return finish(c, rim=False)


def gear_at(c, cx, cy, r, ramp, teeth=8, rot=0.0):
    pts = c.mask_ellipse(cx, cy, r * 0.78, r * 0.78)
    for i in range(teeth):
        a = i * math.tau / teeth + rot
        tx, ty = cx + math.cos(a) * r * 0.82, cy + math.sin(a) * r * 0.82
        pts += c.mask_rect(tx - r * 0.16, ty - r * 0.16, tx + r * 0.16, ty + r * 0.16)
    p = c.fill(pts, ramp)
    hole = c.mask_ellipse(cx, cy, r * 0.28, r * 0.28)
    for (x, y) in hole:
        c.put(x, y, ramp.deep, p)
    return p


def decor_gear():
    c = Canvas(64, 64)
    gear_at(c, 32, 32, 29, Ramp(C(196, 156, 90)), teeth=10)
    for i in range(5):
        a = i * math.tau / 5 + 0.3
        for k in range(8, 20):
            c.put(32 + math.cos(a) * k, 32 + math.sin(a) * k, shift(C(196, 156, 90), dl=-0.22))
    return finish(c, rim=False)


def shard_crate(empty=False):
    c = Canvas(32, 32)
    W_ = C(196, 128, 70)
    R = Ramp(W_)
    c.fill(c.mask_rect(1, 0, 30, 29 if not empty else 20, r=1), R, light=(-0.6, 0.6))
    for y in (1, 10, 19, 28) if not empty else (1, 10, 19):
        for x in range(2, 30):
            c.put(x, y, R.deep)
    for x in (2, 29):
        for y in range(1, 29 if not empty else 20):
            c.put(x, y, R.shadow)
    # metal corners
    for (x, y) in ((2, 1), (27, 1), (2, 25), (27, 25)) if not empty else ((2, 1), (27, 1)):
        c.fill(c.mask_rect(x, y, x + 2, y + 3), Ramp(METAL), soft=True)
    if not empty:
        star = []
        for i in range(10):
            a = math.pi / 2 + i * math.pi / 5
            rr = 7 if i % 2 == 0 else 3
            star.append((16 + math.cos(a) * rr, 15 + math.sin(a) * rr))
        c.fill(c.mask_poly(star), Ramp(GOLD))
        c.specular(14, 18)
    else:
        for x in range(4, 28):
            c.put(x, 20, R.deep)
    return finish(c)


def vine():
    c = Canvas(32, 32)
    r = random.Random(4)
    G = C(80, 170, 80)
    for x0, ph in ((12, 0.0), (19, 1.7)):
        for y in range(32):
            x = x0 + 2 * math.sin(y / 32 * math.tau + ph)
            c.fill(c.mask_rect(x - 1, y, x + 1, y), Ramp(C(60, 120, 56)), shade=False)
            c.put(x - 1, y, C(90, 160, 70))
    for i in range(7):
        y = 2 + i * 4.5
        x = 12 + 2 * math.sin(y / 32 * math.tau) + (5 if i % 2 else -5)
        pts = c.mask_ellipse(x, y, 3, 1.8)
        c.fill(pts, Ramp(G))
    for y in (8, 24):
        c.put(20, y, C(255, 140, 170), lock=True)
        c.put(21, y, C(255, 200, 220), lock=True)
    return finish(c, rim=False)


def hay_bale():
    c = Canvas(32, 32)
    Y = C(232, 196, 104)
    R = Ramp(Y)
    c.fill(c.mask_rect(0, 0, 31, 26, r=3), R, light=(-0.5, 0.7))
    r = random.Random(2)
    for i in range(70):
        x, y = r.randrange(1, 31), r.randrange(1, 26)
        if c.get(x, y):
            c.put(x, y, R.light if r.random() < 0.5 else R.shadow)
            c.put(x + 1, y + 1, R.shadow)
    for x in (8, 23):
        for y in range(0, 27):
            c.put(x, y, C(160, 90, 60))
    for x in range(0, 32, 3):
        c.put(x, 27, Y)
        c.put(x + 1, 28, R.light)
    return finish(c)


def water():
    c = Canvas(32, 32)
    for y in range(32):
        for x in range(32):
            t = y / 31
            col = mix(C(30, 90, 170), C(70, 170, 230), t)
            a = 200 if y < 28 else 230
            c.put(x, y, col + (a,))
    for x in range(32):
        wave = int(1.5 * math.sin(x / 32 * math.tau * 2))
        c.put(x, 30 + wave, C(170, 230, 255) + (240,))
        c.put(x, 29 + wave, C(110, 200, 245) + (230,))
    for (x, y) in ((6, 22), (7, 22), (20, 16), (21, 16), (22, 16), (12, 9), (13, 9)):
        c.put(x, y, C(150, 220, 255) + (220,))
    return c


# ---- pickups -------------------------------------------------------------------------------------------------
def star_shard():
    c = Canvas(24, 24)
    star = []
    for i in range(10):
        a = math.pi / 2 + i * math.pi / 5
        rr = 10.5 if i % 2 == 0 else 4.6
        star.append((12 + math.cos(a) * rr, 11.5 + math.sin(a) * rr))
    p = c.fill(c.mask_poly(star), Ramp(C(255, 206, 60)))
    for (x, y) in c.mask_poly(star):
        if x < 12 and y >= 12:
            c.put(x, y, C(255, 236, 140), p)
    c.specular(9, 15)
    c.specular(10, 15)
    c.specular(9, 16)
    return finish(c, rim=False)


def health_crystal():
    c = Canvas(24, 24)
    c.fill(c.mask_ellipse(12, 10, 8.5, 8), Ramp(C(236, 60, 90)))
    c.fill(c.mask_ellipse(7, 16, 4.6, 4.4), Ramp(C(236, 60, 90)))
    c.fill(c.mask_ellipse(17, 16, 4.6, 4.4), Ramp(C(236, 60, 90)))
    c.fill(c.mask_poly([(3.5, 15), (20.5, 15), (12, 2.5)]), Ramp(C(236, 60, 90)))
    c.specular(7, 17)
    c.specular(8, 18)
    c.specular(6, 16)
    return finish(c, rim=False)


def ledger():
    c = Canvas(24, 24)
    c.fill(c.mask_rect(3, 3, 20, 20, r=1), Ramp(C(120, 50, 60)))
    c.fill(c.mask_rect(4, 2, 21, 4), Ramp(C(246, 236, 210)), soft=True)
    for y in (6, 17):
        c.fill(c.mask_rect(3, y, 20, y + 1), Ramp(GOLD), soft=True)
    gear_at(c, 12, 12, 4.5, Ramp(GOLD), teeth=6)
    return finish(c)


def relic_idol():
    c = Canvas(24, 24)
    for i in range(8):
        a = i * math.tau / 8
        c.fill(c.mask_capsule(12, 14, 12 + math.cos(a) * 9, 14 + math.sin(a) * 9, 1.4), Ramp(C(255, 200, 70)))
    c.fill(c.mask_ellipse(12, 14, 6, 6), Ramp(C(255, 214, 90)))
    c.fill(c.mask_rect(9, 1, 15, 6, r=1), Ramp(C(200, 140, 60)))
    c.put(10, 15, INK, lock=True)
    c.put(14, 15, INK, lock=True)
    return finish(c)


def relic_mask():
    c = Canvas(24, 24)
    c.fill(c.mask_ellipse(12, 12, 9, 10.5), Ramp(C(70, 190, 130)))
    for ex in (8, 16):
        c.flat(c.mask_ellipse(ex, 14, 2.4, 1.6), C(20, 50, 40), lock=True)
    c.fill(c.mask_rect(9, 5, 15, 6), Ramp(C(40, 120, 90)), soft=True)
    c.fill(c.mask_poly([(12, 24), (9, 20), (15, 20)]), Ramp(GOLD))
    return finish(c)


def relic_amulet():
    c = Canvas(24, 24)
    for a in range(0, 180, 12):
        x = 12 + math.cos(math.radians(a)) * 8
        y = 12 + math.sin(math.radians(a)) * 9
        c.put(x, y, GOLD)
    c.fill(c.mask_ellipse(12, 9, 6.5, 6.5), Ramp(C(220, 230, 255)))
    c.fill(c.mask_ellipse(13.5, 10, 4, 4.5), Ramp(C(170, 180, 230)))
    c.specular(10, 11)
    return finish(c)


def cipher_scroll():
    c = Canvas(24, 24)
    c.fill(c.mask_rect(4, 4, 19, 19, r=2), Ramp(C(240, 222, 180)), light=(-0.7, 0.5))
    for x0 in (3, 18):
        c.fill(c.mask_rect(x0, 2, x0 + 3, 21, r=1), Ramp(C(200, 170, 120)))
    for y in (8, 11, 14):
        for x in range(7, 16, 2):
            c.put(x, y, C(150, 120, 90))
    c.fill(c.mask_ellipse(12, 6, 3, 3), Ramp(C(160, 80, 220)))
    return finish(c)


def contract_prize():
    c = Canvas(28, 28)
    c.fill(c.mask_rect(3, 2, 24, 15, r=1), Ramp(C(120, 60, 140)))
    c.fill(c.mask_rect(2, 14, 25, 19, r=2), Ramp(C(150, 80, 170)))
    for y in range(2, 20):
        c.put(13, y, GOLD)
        c.put(14, y, shift(GOLD, dl=-0.15))
    gem(c, 14, 22, 5, 5.5, C(120, 230, 255))
    return finish(c)


# ---- training dummy --------------------------------------------------------------------------------------------
def dummy():
    c = Canvas(32, 48)
    c.fill(c.mask_rect(8, 0, 23, 3, r=1), Ramp(WOOD))
    c.fill(c.mask_rect(14, 2, 17, 30), Ramp(C(140, 92, 54)))
    c.fill(c.mask_rect(6, 20, 25, 23, r=1), Ramp(C(140, 92, 54)))
    c.fill(c.mask_ellipse(16, 22, 8, 9), Ramp(C(232, 196, 112)))
    for r_, col in ((6, C(214, 64, 64)), (4, C(250, 240, 220)), (2, C(214, 64, 64))):
        c.fill(c.mask_ellipse(17, 23, r_, r_), Ramp(col), soft=True)
    c.fill(c.mask_ellipse(16, 38, 6, 6), Ramp(C(232, 196, 112)))
    c.put(18, 39, INK, lock=True)
    c.put(15, 39, INK, lock=True)
    c.put(17, 36, C(150, 100, 60), lock=True)
    return finish(c)


# ---- enemies (face right) -----------------------------------------------------------------------------------------
# Every enemy takes an optional animation state a = (kind, i, n): kind is 'idle', 'walk', 'air' or 'hurt', i the frame
# and n the frame count. a=None draws the classic single sprite (ph_*.png). Frames keep the same canvas and feet line.
def st(a):
    if a is None:
        return 'static', 0.0, 0
    kind, i, n = a
    return kind, i * math.tau / max(1, n), i


def bob_of(kind, ph):
    """Body lift in pixels: walking bounces twice per cycle, idle breathes."""
    if kind == 'walk':
        return int(round(abs(math.sin(ph))))
    if kind == 'idle':
        return 1 if math.sin(ph) > 0.5 else 0
    return 0


def eye(c, x, y, col=C(255, 255, 255), pupil=INK, big=False, shut=False):
    if shut:   # hurt: a squeezed ">" shape
        c.put(x, y + 1, INK, lock=True)
        c.put(x + 1, y, INK, lock=True)
        c.put(x + 1, y + 2, INK, lock=True)
        c.put(x - 1 if big else x, y + 1, INK, lock=True)
        return
    c.put(x, y, col, lock=True)
    c.put(x, y + 1, col, lock=True)
    c.put(x + 1, y, pupil, lock=True)
    c.put(x + 1, y + 1, pupil, lock=True)
    if big:
        c.put(x - 1, y, col, lock=True)
        c.put(x - 1, y + 1, col, lock=True)


def legs(c, xs, y, col, ph=None):
    """Thin legs from the ground to y. With a phase, alternate legs step forward and lift."""
    for k, x in enumerate(xs):
        dx, lift = 0, 0
        if ph is not None:
            q = ph + (k % 2) * math.pi
            dx = int(round(math.cos(q) * 1.2))
            lift = 1 if math.sin(q) > 0.4 else 0
        c.fill(c.mask_rect(x + dx, lift, x + dx + 1, y), Ramp(col), soft=True)


def cog_beetle(a=None):
    kind, ph, i = st(a)
    c = Canvas(32, 24)
    b = bob_of(kind, ph)
    if kind == 'hurt':
        b = -1
    legs(c, (7, 13, 19, 24), 6 + b, C(60, 50, 50), ph if kind == 'walk' else None)
    dome = [p for p in c.mask_ellipse(15, 6 + b, 13, 13) if p[1] >= 5 + b]
    c.fill(dome, Ramp(C(222, 172, 70)))
    spin = -ph / 7 if kind == 'walk' else 0.0
    for k in range(7):
        ang = math.pi * (k + 0.5) / 7 + spin
        if not (0.05 < ang % math.tau < math.pi - 0.05):
            continue
        tx, ty = 15 + math.cos(ang) * 13.5, 6 + b + math.sin(ang) * 13.5
        c.fill(c.mask_rect(tx - 1, ty - 1, tx + 1, ty + 1), Ramp(C(200, 150, 60)))
    hx = 26 + (1 if kind == 'walk' and math.cos(ph) > 0.5 else 0)
    c.fill(c.mask_ellipse(hx, 7 + b, 4.5, 4), Ramp(C(80, 70, 70)))
    eye(c, hx + 1, 7 + b, C(255, 240, 120) if not (kind == 'idle' and i == 1) else C(255, 200, 80), shut=kind == 'hurt')
    c.specular(10, 15 + b)
    c.specular(11, 16 + b)
    return finish(c)


def frost_mite(a=None):
    kind, ph, i = st(a)
    c = Canvas(32, 24)
    b = bob_of(kind, ph)
    legs(c, (6, 12, 18, 23), 5 + b, C(80, 100, 140), ph if kind == 'walk' else None)
    c.fill(c.mask_ellipse(15, 8 + b, 12, 6.5), Ramp(C(150, 200, 240)))
    wig = (lambda k: int(round(math.sin(ph + k) * 1))) if kind in ('walk', 'idle') else (lambda k: 0)
    for k, (x0, h) in enumerate(((7, 9), (12, 13), (17, 11), (22, 8))):
        hh = h - (2 if kind == 'hurt' else 0)
        c.fill(c.mask_poly([(x0 - 2, 11 + b), (x0 + 2, 11 + b), (x0 + wig(k), 11 + b + hh)]), Ramp(C(200, 240, 255)), light=(-0.9, 0.2))
    c.fill(c.mask_ellipse(26, 7 + b, 4, 3.6), Ramp(C(110, 150, 200)))
    eye(c, 27, 7 + b, C(255, 255, 255), shut=kind == 'hurt')
    if kind == 'idle' and i == 1:
        c.specular(14, 12 + b, C(255, 255, 255))
    return finish(c)


def cinder_crawler(a=None):
    kind, ph, i = st(a)
    c = Canvas(32, 20)
    s = math.sin(ph) if kind in ('walk', 'idle') else 0.0
    rx, ry = 14 + s * 1.0, 6.5 - s * 0.6
    if kind == 'hurt':
        rx, ry = 13, 7.2
    c.fill(c.mask_ellipse(15, 6, rx, ry), Ramp(C(70, 56, 60)))
    hot = (C(255, 140, 50), C(255, 220, 120)) if (i % 2 == 0 or kind == 'static') else (C(255, 110, 40), C(255, 190, 90))
    if kind == 'hurt':
        hot = (C(255, 200, 90), C(255, 255, 200))
    for (x, y) in c.line_pts(4, 9 + int(round(s * 0.4)), 26, 10):
        c.put(x, y, hot[0], lock=True)
        c.put(x, y + 1, hot[1], lock=True)
    for k, x0 in enumerate((8, 16, 23)):
        dx = int(round(math.cos(ph + k * 2.1))) if kind == 'walk' else 0
        c.fill(c.mask_ellipse(x0 + dx, 11, 2.6, 2.2), Ramp(C(90, 72, 76)))
    eye(c, 26 + int(round(s)), 5, C(255, 200, 80), shut=kind == 'hurt')
    return finish(c)


def puff_glider(a=None):
    kind, ph, i = st(a)
    c = Canvas(32, 24)
    puff = math.sin(ph) * 0.6 if kind != 'static' else 0.0
    dy = int(round(math.sin(ph))) if kind in ('idle', 'air') else 0
    for k, (x, y, r_) in enumerate(((12, 11, 7), (19, 13, 7), (24, 10, 5.5), (7, 9, 5))):
        rr = r_ + (puff if k % 2 == 0 else -puff)
        c.fill(c.mask_ellipse(x, y + dy, rr, rr * 0.9), Ramp(C(244, 246, 255)))
    c.fill(c.mask_rect(1, 9 + dy, 4, 11 + dy), Ramp(C(150, 150, 170)))
    # propeller: blades swap between long and foreshortened as it spins
    if i % 2 == 0 or kind == 'static':
        c.fill(c.mask_ellipse(2, 15 + dy, 1.6, 4.5), Ramp(C(200, 200, 220)), soft=True)
        c.fill(c.mask_ellipse(2, 5 + dy, 1.6, 4.5), Ramp(C(200, 200, 220)), soft=True)
    else:
        c.fill(c.mask_ellipse(2, 13 + dy, 1.4, 2.2), Ramp(C(220, 220, 236)), soft=True)
        c.fill(c.mask_ellipse(2, 7 + dy, 1.4, 2.2), Ramp(C(220, 220, 236)), soft=True)
    hurt = kind == 'hurt'
    eye(c, 21, 12 + dy, C(40, 50, 80), C(40, 50, 80), shut=hurt)
    eye(c, 26, 12 + dy, C(40, 50, 80), C(40, 50, 80), shut=hurt)
    c.put(23, 9 + dy, C(255, 170, 190), lock=True)
    c.put(28, 9 + dy, C(255, 170, 190), lock=True)
    return finish(c)


def rust_crab(a=None):
    kind, ph, i = st(a)
    c = Canvas(32, 24)
    b = bob_of(kind, ph)
    legs(c, (6, 10, 18, 22), 6 + b, C(130, 80, 50), ph if kind == 'walk' else None)
    c.fill(c.mask_ellipse(14, 9 + b, 11, 6.5), Ramp(C(200, 110, 60)))
    snap = (math.sin(ph * 2) > 0) if kind in ('walk', 'idle') else (kind == 'hurt')
    c.fill(c.mask_ellipse(26, 13 + b, 5.5, 4.6), Ramp(C(220, 140, 70)))
    if snap:
        c.fill(c.mask_poly([(25, 14 + b), (31, 19 + b), (30, 13 + b)]), Ramp(C(170, 90, 50)))
        c.fill(c.mask_poly([(25, 13 + b), (31, 11 + b), (29, 10 + b)]), Ramp(C(170, 90, 50)))
    else:
        c.fill(c.mask_poly([(25, 14 + b), (31, 17 + b), (29, 12 + b)]), Ramp(C(170, 90, 50)))
    for k, x in enumerate((11, 17)):
        sway = int(round(math.sin(ph + k))) if kind == 'walk' else 0
        c.fill(c.mask_rect(x + sway, 14 + b, x + 1 + sway, 18 + b), Ramp(C(160, 100, 60)), soft=True)
        eye(c, x + sway, 18 + b, C(255, 255, 255), shut=kind == 'hurt')
    for (x, y) in ((8, 10), (13, 7), (19, 11)):
        c.put(x, y + b, C(120, 70, 50))
    return finish(c)


def windup_soldier(a=None):
    kind, ph, i = st(a)
    c = Canvas(24, 32)
    b = bob_of(kind, ph)
    l0 = l1 = 0
    if kind == 'walk':   # stiff toy march: one leg up at a time
        l0 = 2 if math.sin(ph) > 0.4 else 0
        l1 = 2 if math.sin(ph) < -0.4 else 0
    c.fill(c.mask_rect(6, l0, 9, 6 + b), Ramp(C(50, 50, 70)))
    c.fill(c.mask_rect(13, l1, 16, 6 + b), Ramp(C(50, 50, 70)))
    lean = -1 if kind == 'hurt' else 0
    c.fill(c.mask_rect(4 + lean, 6 + b, 18 + lean, 20 + b, r=3), Ramp(C(210, 60, 60)))
    for y in (8, 13, 18):
        c.fill(c.mask_rect(4 + lean, y + b, 18 + lean, y + b), Ramp(GOLD), soft=True)
    c.fill(c.mask_ellipse(11 + lean, 24 + b, 6, 5.5), Ramp(C(250, 214, 180)))
    c.fill(c.mask_rect(5 + lean, 27 + b, 17 + lean, 31 if b == 0 else 31, r=1), Ramp(C(40, 50, 120)))
    eye(c, 13 + lean, 24 + b, shut=kind == 'hurt')
    # winding key turns: blades long, edge-on, long, edge-on
    c.fill(c.mask_rect(0, 12 + b, 4, 13 + b), Ramp(METAL), soft=True)
    if i % 2 == 0 or kind == 'static':
        c.fill(c.mask_ellipse(1, 16 + b, 1.6, 2.6), Ramp(METAL), soft=True)
        c.fill(c.mask_ellipse(1, 9 + b, 1.6, 2.6), Ramp(METAL), soft=True)
    else:
        c.fill(c.mask_rect(1, 10 + b, 1, 15 + b), Ramp(shift(METAL, dl=0.1)), soft=True)
    return finish(c)


def spring_tick(a=None):
    """Hopper: idle squashes the spring (coiling), air stretches it."""
    kind, ph, i = st(a)
    c = Canvas(28, 28)
    gap = 2.0
    if kind == 'idle':
        gap = 1.5 if i % 2 == 1 else 1.75
    elif kind == 'air':
        gap = 2.25 if i % 2 == 0 else 2.0
    elif kind == 'hurt':
        gap = 1.5
    base = int(round(gap * 4)) - 8      # body offset relative to the classic sprite
    for k in range(4):
        y = int(round(k * gap))
        c.fill(c.mask_rect(10 + (k % 2), y, 17 - (k % 2), y + 1), Ramp(METAL), soft=True)
    c.fill(c.mask_ellipse(14, 17 + base, 10, 9.5 - (0.6 if kind == 'idle' and i % 2 else 0)), Ramp(C(90, 200, 160)))
    gear_at(c, 9, 20 + base, 3.5, Ramp(GOLD), teeth=6, rot=i * 0.5)
    if kind == 'hurt':
        eye(c, 17, 18 + base, big=True, shut=True)
    else:
        eye(c, 17, 18 + base, C(255, 255, 255), big=True)
        brow = 15 if kind == 'idle' else 14
        c.put(18, brow + base, INK, lock=True)
        c.put(19, brow + base - (1 if kind == 'idle' else 0), INK, lock=True)
    return finish(c)


def gyro_moth(a=None):
    """Flyer: four-frame wing beat (wings fold toward the body and back)."""
    kind, ph, i = st(a)
    c = Canvas(32, 24)
    fold = 0.0
    if kind in ('air', 'idle'):
        fold = (1 - math.cos(ph)) / 2          # 0 open .. 1 folded
    if kind == 'hurt':
        fold = 0.6
    dy = int(round(math.sin(ph) * 0.8)) if kind in ('air', 'idle') else 0
    for (y, sgn) in ((18, 1), (6, -1)):
        wy = y - sgn * fold * 5 + dy
        c.fill(c.mask_ellipse(12, wy, 9 - fold * 2, 3.5 - fold * 1.8), Ramp(C(200, 214, 230)), light=(-0.2, 0.9))
        for x in range(5, 20, 4):
            c.put(x, round(wy), C(150, 166, 190))
    c.fill(c.mask_ellipse(17, 12 + dy, 7, 6.5), Ramp(C(220, 170, 80)))
    lens = C(255, 120, 120) if kind == 'hurt' else C(120, 220, 255)
    c.fill(c.mask_ellipse(22, 12 + dy, 3.4, 3.4), Ramp(lens))
    c.specular(21, 13 + dy)
    return finish(c)


def bolt_knight(a=None):
    kind, ph, i = st(a)
    c = Canvas(28, 40)
    b = bob_of(kind, ph)
    l0 = l1 = 0
    if kind == 'walk':
        l0 = 2 if math.sin(ph) > 0.4 else 0
        l1 = 2 if math.sin(ph) < -0.4 else 0
    c.fill(c.mask_rect(6, l0, 11, 9 + b, r=1), Ramp(C(110, 116, 140)))
    c.fill(c.mask_rect(15, l1, 20, 9 + b, r=1), Ramp(C(110, 116, 140)))
    lean = -1 if kind == 'hurt' else 0
    c.fill(c.mask_rect(3 + lean, 8 + b, 23 + lean, 26 + b, r=3), Ramp(C(160, 168, 190)))
    for (x, y) in ((6, 22), (20, 22), (6, 11), (20, 11)):
        c.specular(x + lean, y + b, C(240, 240, 255))
    top = min(39, 38 + b)
    c.fill(c.mask_rect(4 + lean, 25 + b, 22 + lean, top, r=4), Ramp(C(170, 178, 200)))
    c.fill(c.mask_rect(10 + lean, 30 + b, 24 + lean, 32 + b), Ramp(C(30, 30, 50)), soft=True)
    # visor: a bright scanning dot sweeps along the slit
    scan = 13 + 2 * (i % 5)
    for x in range(13, 23, 2):
        hot = kind != 'hurt' and kind != 'static' and x == scan
        col = C(255, 230, 160) if hot else (C(120, 120, 140) if kind == 'hurt' else C(255, 120, 60))
        c.put(x + lean, 31 + b, col, lock=True)
    if top >= 37:
        c.fill(c.mask_rect(12 + lean, min(37 + b, 38), 15 + lean, 39), Ramp(C(220, 60, 60)), soft=True)
    return finish(c)


def bolt_shield():
    c = Canvas(12, 32)
    c.fill(c.mask_rect(0, 0, 11, 31, r=2), Ramp(C(220, 180, 80)), light=(-0.8, 0.4))
    for y in (3, 15, 28):
        c.specular(3, y)
        c.specular(8, y)
    for y in range(2, 30):
        c.put(5, y, shift(C(220, 180, 80), dl=-0.15))
    return finish(c)


# ---- guards (the fighter rig, cropped) ---------------------------------------------------------------------------
def rig_crop(spec, pose, w, h):
    img = draw_fighter(spec, pose).image()
    x0 = (img.width - w) // 2 + 2
    out = img.crop((x0, img.height - h, x0 + w, img.height))
    c = Canvas(w, h)
    p = out.load()
    for y in range(h):
        for x in range(w):
            px_ = p[x, h - 1 - y]
            if px_[3] > 0:
                c.px[y][x] = px_
    return c


GUARD = dict(build='slim', head='hood', extra='collar', skin=C(214, 180, 150), hair=C(40, 40, 60), top=C(52, 60, 96),
             accent=C(250, 200, 90), pants=C(40, 44, 70), shoes=C(30, 26, 40),
             weapon=dict(type='lantern', color=C(250, 200, 90), accent=C(255, 240, 160)))
ELITE = dict(GUARD, head='helmet', helm=C(150, 160, 190), top=C(110, 120, 150), accent=C(70, 76, 100), eye=C(255, 80, 80),
             face='robot', weapon=dict(type='spear', color=C(180, 186, 210), accent=C(255, 90, 90)))
CAPTAIN = dict(GUARD, head='helmet', helm=C(40, 36, 60), plume=C(250, 200, 90), top=C(190, 40, 60), accent=C(250, 200, 90),
               weapon=dict(type='daggers', color=C(220, 224, 240), accent=GOLD))
GUARDS = {  # spec, idle hands, weapon angle, canvas height
    'sentry': (GUARD, ((-2, -7), (6, -5)), 0, 44),
    'elite': (ELITE, ((-2, -7), (4, -6)), 80, 44),
    'captain': (CAPTAIN, ((-2, -7), (4, -6)), 70, 48),
}


def guard(kind_name, a=None):
    spec, hands, wang, h = GUARDS[kind_name]
    kind, ph, i = st(a)
    if kind == 'walk':
        # a measured patrol: the run cycle with a shorter stride, the weapon hand kept steady
        legs_ = [((-4, 1), (4, 0)), ((-3, 2), (3, 0)), ((-1, 3), (1, 1)), ((4, 0), (-4, 1)), ((3, 0), (-3, 2)), ((1, 1), (-1, 3))][i % 6]
        sw = [(2, -6), (1, -7), (0, -7), (-2, -6), (-1, -7), (0, -7)][i % 6]
        pose = P(by=[0, -1, 0, 0, -1, 0][i % 6], legs=legs_, hands=(sw, hands[1]), wang=wang, flutter=i)
    elif kind == 'hurt':
        pose = P(bx=-2, lean=-2, legs=((-5, 0), (2, 1)), hands=((-6, 2), (-1, 3)), wang=wang + 60, eyes='hurt', flutter=5)
    elif kind == 'idle':
        pose = P(by=-(i % 2), hands=hands, wang=wang, flutter=i, eyes='blink' if i == 3 else 'open')
    else:
        pose = P(hands=hands, wang=wang)
    return rig_crop(spec, pose, 28, h)


def sentry(a=None):
    return guard('sentry', a)


def elite(a=None):
    return guard('elite', a)


def captain(a=None):
    return guard('captain', a)


# Animated enemy sets: art name -> (draw function, clips). Clips map to FighterFrames: idle, run (walk), jump and fall
# (the 'air' frames, used by flyers and hoppers in the air), hurt.
ENEMY_CLIPS = {
    'cog_beetle': (cog_beetle, dict(idle=2, walk=4, hurt=1)),
    'frost_mite': (frost_mite, dict(idle=2, walk=4, hurt=1)),
    'cinder_crawler': (cinder_crawler, dict(idle=2, walk=4, hurt=1)),
    'rust_crab': (rust_crab, dict(idle=2, walk=4, hurt=1)),
    'windup_soldier': (windup_soldier, dict(idle=2, walk=4, hurt=1)),
    'bolt_knight': (bolt_knight, dict(idle=2, walk=4, hurt=1)),
    'spring_tick': (spring_tick, dict(idle=2, air=2, hurt=1)),
    'puff_glider': (puff_glider, dict(idle=4, air=4, hurt=1)),
    'gyro_moth': (gyro_moth, dict(idle=4, air=4, hurt=1)),
    'gearwatch_sentry': (sentry, dict(idle=4, walk=6, hurt=1)),
    'gearwatch_elite': (elite, dict(idle=4, walk=6, hurt=1)),
    'gearwatch_captain': (captain, dict(idle=4, walk=6, hurt=1)),
}




# ---- boss body --------------------------------------------------------------------------------------------------------
WARDEN_STATES = ['idle_0', 'idle_1', 'angry', 'dazed', 'hurt']


def warden_body(state='idle_0'):
    """A huge clockwork face (neutral metal so the phase tints colour it). 64x64, drawn at 6x scale in game.
    States: idle_0/idle_1 (gear turns, grille breathes), angry (wind-up: brows down, hot grille), dazed (vulnerable:
    spiral eyes, open mouth) and hurt (squeezed eyes)."""
    c = Canvas(64, 64)
    rot = {'idle_1': math.tau / 24, 'angry': math.tau / 48}.get(state, 0.0)
    gear_at(c, 32, 32, 30, Ramp(C(210, 200, 186)), teeth=12, rot=rot)
    c.fill(c.mask_ellipse(32, 32, 20, 20), Ramp(C(236, 228, 214)))
    for i in range(12):
        a = i * math.tau / 12
        x, y = 32 + math.cos(a) * 16, 32 + math.sin(a) * 16
        c.fill(c.mask_rect(x - 1, y - 1, x + 1, y + 1), Ramp(C(80, 70, 70)), soft=True)
    ink = C(40, 34, 40)
    for ex in (24, 40):
        if state == 'dazed':
            c.fill(c.mask_ellipse(ex, 36, 4.5, 4.5), Ramp(C(250, 246, 236)))
            for k in range(14):
                r, t = 0.4 + k * 0.28, k * 0.9
                c.put(ex + math.cos(t) * r, 36 + math.sin(t) * r, ink, lock=True)
        elif state == 'hurt':
            for (x, y) in c.line_pts(ex - 4, 38, ex + 4, 35) + c.line_pts(ex - 4, 34, ex + 4, 35):
                c.put(x, y, ink, lock=True)
        else:
            ry = 2.4 if state == 'angry' else 3.5
            c.fill(c.mask_ellipse(ex, 36 - (0.6 if state == 'angry' else 0), 4.5, ry), Ramp(ink))
            glow = C(255, 70, 40) if state == 'angry' else C(255, 120, 60)
            c.flat(c.mask_ellipse(ex + 1, 36, 1.6, 1.6 if state != 'angry' else 1.1), glow, lock=True)
    brow = 3 if state == 'angry' else (0 if state != 'dazed' else -2)
    for (x, y) in c.line_pts(19, 42 - brow, 28, 39) + c.line_pts(36, 39, 45, 42 - brow):
        c.put(x, y, ink, lock=True)
    # grille mouth
    if state == 'dazed':
        c.fill(c.mask_ellipse(32, 22, 6, 5), Ramp(C(60, 54, 60)))
        c.flat(c.mask_ellipse(32, 21, 3, 2), C(30, 24, 30), lock=True)
    else:
        top = 27 if state == 'angry' else (26 if state != 'idle_1' else 25)
        c.fill(c.mask_rect(23, 20, 41, top, r=2), Ramp(C(60, 54, 60)))
        hot = {'angry': C(255, 80, 50), 'idle_1': C(255, 190, 110), 'hurt': C(200, 200, 210)}.get(state, C(255, 160, 80))
        for x in range(25, 40, 3):
            for y in range(21, top):
                c.put(x, y, hot, lock=True)
    return finish(c, rim=False)


def warden_frames():
    return [warden_body(st_) for st_ in WARDEN_STATES]


ALL = {
    'ph_ground': ground, 'ph_stone': stone, 'ph_oneway': oneway, 'ph_spikes': spikes, 'ph_spring_pad': spring_pad,
    'ph_platform_plate': platform_plate, 'ph_checkpoint': checkpoint, 'ph_goal_gate': goal_gate, 'ph_decor_gear': decor_gear,
    'ph_shard_crate': lambda: shard_crate(False), 'ph_shard_crate_empty': lambda: shard_crate(True), 'ph_vine': vine,
    'ph_hay_bale': hay_bale, 'ph_water': water, 'ph_star_shard': star_shard, 'ph_health_crystal': health_crystal,
    'ph_ledger': ledger, 'ph_relic_idol': relic_idol, 'ph_relic_mask': relic_mask, 'ph_relic_amulet': relic_amulet,
    'ph_cipher_scroll': cipher_scroll, 'ph_contract_prize': contract_prize, 'ph_dummy': dummy,
    'ph_cog_beetle': cog_beetle, 'ph_frost_mite': frost_mite, 'ph_cinder_crawler': cinder_crawler, 'ph_puff_glider': puff_glider,
    'ph_rust_crab': rust_crab, 'ph_windup_soldier': windup_soldier, 'ph_spring_tick': spring_tick, 'ph_gyro_moth': gyro_moth,
    'ph_bolt_knight': bolt_knight, 'ph_bolt_shield': bolt_shield,
    'ph_gearwatch_sentry': sentry, 'ph_gearwatch_elite': elite, 'ph_gearwatch_captain': captain,
}

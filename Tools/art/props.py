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


def gear_at(c, cx, cy, r, ramp, teeth=8):
    pts = c.mask_ellipse(cx, cy, r * 0.78, r * 0.78)
    for i in range(teeth):
        a = i * math.tau / teeth
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
def eye(c, x, y, col=C(255, 255, 255), pupil=INK, big=False):
    c.put(x, y, col, lock=True)
    c.put(x, y + 1, col, lock=True)
    c.put(x + 1, y, pupil, lock=True)
    c.put(x + 1, y + 1, pupil, lock=True)
    if big:
        c.put(x - 1, y, col, lock=True)
        c.put(x - 1, y + 1, col, lock=True)


def legs(c, xs, y, col, n=3):
    for x in xs:
        c.fill(c.mask_rect(x, 0, x + 1, y), Ramp(col), soft=True)


def cog_beetle():
    c = Canvas(32, 24)
    legs(c, (7, 13, 19, 24), 6, C(60, 50, 50))
    dome = [p for p in c.mask_ellipse(15, 6, 13, 13) if p[1] >= 5]
    c.fill(dome, Ramp(C(222, 172, 70)))
    for i in range(7):
        a = math.pi * (i + 0.5) / 7
        tx, ty = 15 + math.cos(a) * 13.5, 6 + math.sin(a) * 13.5
        c.fill(c.mask_rect(tx - 1, ty - 1, tx + 1, ty + 1), Ramp(C(200, 150, 60)))
    c.fill(c.mask_ellipse(26, 7, 4.5, 4), Ramp(C(80, 70, 70)))
    eye(c, 27, 7, C(255, 240, 120))
    c.specular(10, 15)
    c.specular(11, 16)
    return finish(c)


def frost_mite():
    c = Canvas(32, 24)
    legs(c, (6, 12, 18, 23), 5, C(80, 100, 140))
    c.fill(c.mask_ellipse(15, 8, 12, 6.5), Ramp(C(150, 200, 240)))
    for x0, h in ((7, 9), (12, 13), (17, 11), (22, 8)):
        c.fill(c.mask_poly([(x0 - 2, 11), (x0 + 2, 11), (x0, 11 + h)]), Ramp(C(200, 240, 255)), light=(-0.9, 0.2))
    c.fill(c.mask_ellipse(26, 7, 4, 3.6), Ramp(C(110, 150, 200)))
    eye(c, 27, 7, C(255, 255, 255))
    return finish(c)


def cinder_crawler():
    c = Canvas(32, 20)
    c.fill(c.mask_ellipse(15, 6, 14, 6.5), Ramp(C(70, 56, 60)))
    for (x, y) in c.line_pts(4, 9, 26, 10):
        c.put(x, y, C(255, 140, 50), lock=True)
        c.put(x, y + 1, C(255, 220, 120), lock=True)
    for x0 in (8, 16, 23):
        c.fill(c.mask_ellipse(x0, 11, 2.6, 2.2), Ramp(C(90, 72, 76)))
    eye(c, 26, 5, C(255, 200, 80))
    return finish(c)


def puff_glider():
    c = Canvas(32, 24)
    for (x, y, r_) in ((12, 11, 7), (19, 13, 7), (24, 10, 5.5), (7, 9, 5)):
        c.fill(c.mask_ellipse(x, y, r_, r_ * 0.9), Ramp(C(244, 246, 255)))
    c.fill(c.mask_rect(1, 9, 4, 11), Ramp(C(150, 150, 170)))
    c.fill(c.mask_ellipse(2, 15, 1.6, 4.5), Ramp(C(200, 200, 220)), soft=True)
    c.fill(c.mask_ellipse(2, 5, 1.6, 4.5), Ramp(C(200, 200, 220)), soft=True)
    eye(c, 21, 12, C(40, 50, 80), C(40, 50, 80))
    eye(c, 26, 12, C(40, 50, 80), C(40, 50, 80))
    c.put(23, 9, C(255, 170, 190), lock=True)
    c.put(28, 9, C(255, 170, 190), lock=True)
    return finish(c)


def rust_crab():
    c = Canvas(32, 24)
    legs(c, (6, 10, 18, 22), 6, C(130, 80, 50))
    c.fill(c.mask_ellipse(14, 9, 11, 6.5), Ramp(C(200, 110, 60)))
    c.fill(c.mask_ellipse(26, 13, 5.5, 4.6), Ramp(C(220, 140, 70)))
    c.fill(c.mask_poly([(25, 14), (31, 17), (29, 12)]), Ramp(C(170, 90, 50)))
    for x in (11, 17):
        c.fill(c.mask_rect(x, 14, x + 1, 18), Ramp(C(160, 100, 60)), soft=True)
        eye(c, x, 18, C(255, 255, 255))
    for (x, y) in ((8, 10), (13, 7), (19, 11)):
        c.put(x, y, C(120, 70, 50))
    return finish(c)


def windup_soldier():
    c = Canvas(24, 32)
    c.fill(c.mask_rect(6, 0, 9, 6), Ramp(C(50, 50, 70)))
    c.fill(c.mask_rect(13, 0, 16, 6), Ramp(C(50, 50, 70)))
    c.fill(c.mask_rect(4, 6, 18, 20, r=3), Ramp(C(210, 60, 60)))
    for y in (8, 13, 18):
        c.fill(c.mask_rect(4, y, 18, y), Ramp(GOLD), soft=True)
    c.fill(c.mask_ellipse(11, 24, 6, 5.5), Ramp(C(250, 214, 180)))
    c.fill(c.mask_rect(5, 27, 17, 31, r=1), Ramp(C(40, 50, 120)))
    eye(c, 13, 24)
    # winding key
    c.fill(c.mask_rect(0, 12, 4, 13), Ramp(METAL), soft=True)
    c.fill(c.mask_ellipse(1, 16, 1.6, 2.6), Ramp(METAL), soft=True)
    c.fill(c.mask_ellipse(1, 9, 1.6, 2.6), Ramp(METAL), soft=True)
    return finish(c)


def spring_tick():
    c = Canvas(28, 28)
    for k in range(4):
        c.fill(c.mask_rect(10 + (k % 2), k * 2, 17 - (k % 2), k * 2 + 1), Ramp(METAL), soft=True)
    c.fill(c.mask_ellipse(14, 17, 10, 9.5), Ramp(C(90, 200, 160)))
    gear_at(c, 9, 20, 3.5, Ramp(GOLD), teeth=6)
    eye(c, 17, 18, C(255, 255, 255), big=True)
    c.put(18, 14, INK, lock=True)
    c.put(19, 14, INK, lock=True)
    return finish(c)


def gyro_moth():
    c = Canvas(32, 24)
    for (y, ph) in ((18, 1), (6, -1)):
        c.fill(c.mask_ellipse(12, y, 9, 3.5), Ramp(C(200, 214, 230)), light=(-0.2, 0.9))
        for x in range(5, 20, 4):
            c.put(x, y, C(150, 166, 190))
    c.fill(c.mask_ellipse(17, 12, 7, 6.5), Ramp(C(220, 170, 80)))
    c.fill(c.mask_ellipse(22, 12, 3.4, 3.4), Ramp(C(120, 220, 255)))
    c.specular(21, 13)
    return finish(c)


def bolt_knight():
    c = Canvas(28, 40)
    c.fill(c.mask_rect(6, 0, 11, 9, r=1), Ramp(C(110, 116, 140)))
    c.fill(c.mask_rect(15, 0, 20, 9, r=1), Ramp(C(110, 116, 140)))
    c.fill(c.mask_rect(3, 8, 23, 26, r=3), Ramp(C(160, 168, 190)))
    for (x, y) in ((6, 22), (20, 22), (6, 11), (20, 11)):
        c.specular(x, y, C(240, 240, 255))
    c.fill(c.mask_rect(4, 25, 22, 38, r=4), Ramp(C(170, 178, 200)))
    c.fill(c.mask_rect(10, 30, 24, 32), Ramp(C(30, 30, 50)), soft=True)
    for x in range(13, 23, 2):
        c.put(x, 31, C(255, 120, 60), lock=True)
    c.fill(c.mask_rect(12, 37, 15, 39), Ramp(C(220, 60, 60)), soft=True)
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


def sentry():
    return rig_crop(GUARD, P(hands=((-2, -7), (6, -5)), wang=0), 28, 44)


def elite():
    s = dict(GUARD, head='helmet', helm=C(150, 160, 190), top=C(110, 120, 150), accent=C(70, 76, 100), eye=C(255, 80, 80),
             face='robot', weapon=dict(type='spear', color=C(180, 186, 210), accent=C(255, 90, 90)))
    return rig_crop(s, P(hands=((-2, -7), (4, -6)), wang=80), 28, 44)


def captain():
    s = dict(GUARD, head='helmet', helm=C(40, 36, 60), plume=C(250, 200, 90), top=C(190, 40, 60), accent=C(250, 200, 90),
             weapon=dict(type='daggers', color=C(220, 224, 240), accent=GOLD))
    return rig_crop(s, P(hands=((-2, -7), (4, -6)), wang=70), 28, 48)


# ---- boss body --------------------------------------------------------------------------------------------------------
def warden_body():
    """A huge clockwork face (neutral metal so the phase tints colour it). 64x64, drawn at 6x scale in game."""
    c = Canvas(64, 64)
    gear_at(c, 32, 32, 30, Ramp(C(210, 200, 186)), teeth=12)
    c.fill(c.mask_ellipse(32, 32, 20, 20), Ramp(C(236, 228, 214)))
    for i in range(12):
        a = i * math.tau / 12
        x, y = 32 + math.cos(a) * 16, 32 + math.sin(a) * 16
        c.fill(c.mask_rect(x - 1, y - 1, x + 1, y + 1), Ramp(C(80, 70, 70)), soft=True)
    # angry eyes
    for ex in (24, 40):
        c.fill(c.mask_ellipse(ex, 36, 4.5, 3.5), Ramp(C(40, 34, 40)))
        c.flat(c.mask_ellipse(ex + 1, 36, 1.6, 1.6), C(255, 120, 60), lock=True)
    for (x, y) in c.line_pts(19, 42, 28, 39) + c.line_pts(36, 39, 45, 42):
        c.put(x, y, C(40, 34, 40), lock=True)
    # grille mouth
    c.fill(c.mask_rect(23, 20, 41, 26, r=2), Ramp(C(60, 54, 60)))
    for x in range(25, 40, 3):
        for y in range(21, 26):
            c.put(x, y, C(255, 160, 80), lock=True)
    return finish(c, rim=False)


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

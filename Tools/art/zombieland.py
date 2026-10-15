"""
Zombie Land top-down art (all original): 16 px ground tiles, building roofs (9-slice) and wall faces, landmark
roofs, props, vehicles (facing right), pickups, rot nests, blood hounds and the First Turned, and the minimap.
PPU 16 for the city (one tile = one world unit). Deterministic (seeded RNG, no hash()).
"""
import math, random
from PIL import Image, ImageDraw

T = 16
INK = (18, 16, 24, 255)


def img(w, h):
    return Image.new('RGBA', (w, h), (0, 0, 0, 0))


def jitter(c, r, k):
    d = r.randint(-k, k)
    return (max(0, min(255, c[0] + d)), max(0, min(255, c[1] + d)), max(0, min(255, c[2] + d)), 255)


def noise_fill(im, box, base, seed, k=8, speck=None, speck_p=0.0):
    r = random.Random(seed)
    p = im.load()
    x0, y0, x1, y1 = box
    for y in range(y0, y1):
        for x in range(x0, x1):
            c = jitter(base, r, k)
            if speck and r.random() < speck_p:
                c = speck + (255,) if len(speck) == 3 else speck
            p[x, y] = c


def shade(c, f):
    return (max(0, min(255, int(c[0] * f))), max(0, min(255, int(c[1] * f))), max(0, min(255, int(c[2] * f))), 255)


# ---- ground tiles ---------------------------------------------------------------------------------

ASPHALT = (62, 62, 70)
SIDEWALK = (150, 148, 142)
GRASS = (74, 128, 62)
YARD = (92, 146, 70)
PATH = (176, 146, 98)
COBBLE = (134, 112, 98)
CONCRETE = (120, 120, 114)
PLANK = (132, 92, 58)
WATER = (44, 92, 150)
LOT = (84, 84, 90)


def asphalt(v):
    im = img(T, T)
    noise_fill(im, (0, 0, T, T), ASPHALT, 10 + v, 6, speck=(78, 78, 86), speck_p=0.06)
    d = ImageDraw.Draw(im)
    if v == 1:
        d.line([(2, 11), (6, 9), (9, 12)], fill=(44, 44, 50, 255))     # crack
    if v == 2:
        d.point([(11, 4), (12, 4), (11, 5)], fill=(90, 30, 30, 255))   # old stain
    return im


def lane(vertical):
    im = asphalt(0)
    d = ImageDraw.Draw(im)
    if vertical:
        d.rectangle([0, 2, 1, 13], fill=(232, 196, 70, 255))
    else:
        d.rectangle([2, 0, 13, 1], fill=(232, 196, 70, 255))
    return im


def crosswalk(vertical):
    im = asphalt(0)
    d = ImageDraw.Draw(im)
    for k in range(0, T, 4):
        if vertical:
            d.rectangle([k, 2, k + 1, 13], fill=(226, 226, 220, 255))
        else:
            d.rectangle([2, k, 13, k + 1], fill=(226, 226, 220, 255))
    return im


def sidewalk(v):
    im = img(T, T)
    noise_fill(im, (0, 0, T, T), SIDEWALK, 30 + v, 5)
    d = ImageDraw.Draw(im)
    d.line([(0, 0), (T - 1, 0)], fill=shade(SIDEWALK + (255,), 0.82))
    d.line([(0, 0), (0, T - 1)], fill=shade(SIDEWALK + (255,), 0.82))
    if v == 1:
        d.line([(8, 0), (8, T - 1)], fill=shade(SIDEWALK + (255,), 0.85))
    return im


def grass(base, v):
    im = img(T, T)
    noise_fill(im, (0, 0, T, T), base, 50 + v, 9, speck=shade(base + (255,), 1.25)[:3], speck_p=0.08)
    r = random.Random(60 + v)
    d = ImageDraw.Draw(im)
    for _ in range(2 + v):
        x, y = r.randint(1, 14), r.randint(1, 14)
        d.point([(x, y), (x, y - 1)], fill=shade(base + (255,), 0.75))
    return im


def cobble(v):
    im = img(T, T)
    noise_fill(im, (0, 0, T, T), shade(COBBLE + (255,), 0.7)[:3], 70 + v, 4)
    d = ImageDraw.Draw(im)
    r = random.Random(71 + v)
    for yy in range(0, T, 4):
        off = 2 if (yy // 4) % 2 else 0
        for xx in range(-2 + off, T, 5):
            c = jitter(COBBLE + (255,), r, 12)
            d.rectangle([xx, yy, xx + 3, yy + 2], fill=c)
    return im


def concrete(v):
    im = img(T, T)
    noise_fill(im, (0, 0, T, T), CONCRETE, 80 + v, 5, speck=(100, 100, 96), speck_p=0.05)
    d = ImageDraw.Draw(im)
    d.line([(0, T - 1), (T - 1, T - 1)], fill=shade(CONCRETE + (255,), 0.85))
    if v == 1:
        d.line([(3, 4), (7, 6), (12, 5)], fill=shade(CONCRETE + (255,), 0.75))
    return im


def planks():
    im = img(T, T)
    r = random.Random(90)
    p = im.load()
    for y in range(T):
        for x in range(T):
            c = jitter(PLANK + (255,), r, 8)
            if y % 4 == 3:
                c = shade(c, 0.6)
            p[x, y] = c
    return im


def water(v):
    im = img(T, T)
    noise_fill(im, (0, 0, T, T), WATER, 100 + v, 4)
    d = ImageDraw.Draw(im)
    r = random.Random(101 + v)
    for _ in range(3):
        x, y = r.randint(0, 11), r.randint(1, 14)
        d.line([(x, y), (x + 3, y)], fill=(110, 160, 210, 255))
    return im


def bridge():
    im = img(T, T)
    noise_fill(im, (0, 0, T, T), (96, 82, 70), 110, 5)
    d = ImageDraw.Draw(im)
    d.line([(0, 0), (T - 1, 0)], fill=(70, 58, 50, 255))
    return im


def fence():
    im = img(T, T)
    noise_fill(im, (0, 0, T, T), (58, 62, 52), 120, 4)
    d = ImageDraw.Draw(im)
    for k in range(0, T, 4):
        d.line([(k, 0), (k + 3, T - 1)], fill=(150, 150, 150, 255))
        d.line([(k + 3, 0), (k, T - 1)], fill=(120, 120, 120, 255))
    d.rectangle([0, 6, T - 1, 8], fill=(170, 40, 40, 255))   # quarantine tape
    return im


def lot(v):
    im = img(T, T)
    noise_fill(im, (0, 0, T, T), LOT, 130 + v, 5)
    d = ImageDraw.Draw(im)
    if v == 1:
        d.line([(0, 0), (0, T - 1)], fill=(220, 220, 220, 255))
    return im


def rubble():
    im = img(T, T)
    noise_fill(im, (0, 0, T, T), (104, 88, 72), 140, 14, speck=(70, 60, 52), speck_p=0.2)
    return im


TILES = {
    'r0': lambda: asphalt(0), 'r1': lambda: asphalt(1), 'r2': lambda: asphalt(2),
    'lane_h': lambda: lane(False), 'lane_v': lambda: lane(True),
    'cross_h': lambda: crosswalk(False), 'cross_v': lambda: crosswalk(True),
    's0': lambda: sidewalk(0), 's1': lambda: sidewalk(1),
    'g0': lambda: grass(GRASS, 0), 'g1': lambda: grass(GRASS, 1), 'g2': lambda: grass(GRASS, 2),
    'y0': lambda: grass(YARD, 0), 'y1': lambda: grass(YARD, 1),
    'p0': lambda: grass(PATH, 0),
    'o0': lambda: cobble(0), 'o1': lambda: cobble(1),
    'i0': lambda: concrete(0), 'i1': lambda: concrete(1),
    'd0': planks, 'w0': lambda: water(0), 'w1': lambda: water(1), 'bridge': bridge, 'f0': fence,
    'l0': lambda: lot(0), 'l1': lambda: lot(1), 'x0': rubble,
    # the footprint under a roof (never seen; buildings draw on top). Names are case-safe (macOS ignores case).
    'bldg': lambda: concrete(0),
}


# ---- roofs ----------------------------------------------------------------------------------------

ROOF_STYLES = {
    'house_red': ((178, 70, 58), 'shingle'), 'house_blue': ((70, 98, 160), 'shingle'),
    'house_green': ((74, 128, 88), 'shingle'), 'house_gray': ((110, 110, 118), 'shingle'),
    'tower_gray': ((132, 134, 140), 'flat'), 'tower_blue': ((96, 118, 150), 'flat'), 'tower_tan': ((168, 150, 120), 'flat'),
    'tower_glass': ((100, 150, 170), 'glass'),
    'warehouse_gray': ((128, 130, 128), 'corrugated'), 'warehouse_rust': ((150, 92, 62), 'corrugated'),
    'warehouse_green': ((88, 120, 96), 'corrugated'),
    'old_terracotta': ((176, 98, 64), 'tile'), 'old_slate': ((88, 92, 110), 'tile'), 'old_brick': ((146, 76, 66), 'tile'),
    'campus_brick': ((150, 82, 66), 'flat'), 'campus_stone': ((170, 164, 150), 'flat'),
    'container_red': ((170, 58, 50), 'container'), 'container_blue': ((58, 94, 160), 'container'),
    'container_green': ((62, 130, 82), 'container'), 'container_orange': ((210, 120, 50), 'container'),
}


def roof(style):
    """48x48 9-slice roof (border 12 px): dark outline, lit north rim, texture by kind."""
    base, kind = ROOF_STYLES[style]
    S = 48
    im = img(S, S)
    r = random.Random(sum(ord(c) for c in style))
    p = im.load()
    for y in range(S):
        for x in range(S):
            c = jitter(base + (255,), r, 6)
            if kind == 'shingle' and y % 4 == 0:
                c = shade(c, 0.8)
            if kind == 'shingle' and (x + (y // 4) * 3) % 6 == 0:
                c = shade(c, 0.88)
            if kind == 'corrugated' and x % 3 == 0:
                c = shade(c, 0.82)
            if kind == 'tile' and (y % 3 == 0 or (x + (y // 3) * 2) % 5 == 0):
                c = shade(c, 0.8)
            if kind == 'glass' and (x % 8 == 0 or y % 8 == 0):
                c = shade(c, 0.75)
            if kind == 'glass' and (x + y) % 11 == 0:
                c = shade(c, 1.3)
            if kind == 'container' and x % 2 == 0:
                c = shade(c, 0.85)
            if kind == 'flat' and r.random() < 0.05:
                c = shade(c, 0.85)
            p[x, y] = c
    d = ImageDraw.Draw(im)
    rim = shade(base + (255,), 1.25)
    d.rectangle([0, 0, S - 1, S - 1], outline=INK)
    d.line([(1, 1), (S - 2, 1)], fill=rim)
    d.line([(1, 1), (1, S - 2)], fill=rim)
    d.line([(1, S - 2), (S - 2, S - 2)], fill=shade(base + (255,), 0.65))
    d.line([(S - 2, 1), (S - 2, S - 2)], fill=shade(base + (255,), 0.65))
    if kind == 'shingle':
        d.line([(2, S // 2), (S - 3, S // 2)], fill=shade(base + (255,), 0.6))   # ridge
    if kind == 'flat':
        d.rectangle([3, 3, S - 4, S - 4], outline=shade(base + (255,), 0.75))      # parapet
    return im


def wall_face(style):
    """16x16 south wall (tiled along the building's bottom edge) with windows."""
    base, kind = ROOF_STYLES[style]
    wall = shade(base + (255,), 0.55)
    im = img(16, 16)
    noise_fill(im, (0, 0, 16, 16), wall[:3], 7, 4)
    d = ImageDraw.Draw(im)
    if kind != 'container':
        d.rectangle([3, 5, 6, 10], fill=(40, 46, 60, 255))
        d.rectangle([10, 5, 13, 10], fill=(40, 46, 60, 255))
        d.point([(4, 6), (11, 6)], fill=(130, 150, 180, 255))
    d.line([(0, 15), (15, 15)], fill=INK)
    return im


def roof_props(kind):
    """Rooftop clutter: AC unit, vent, water tank, skylight."""
    im = img(16, 16)
    d = ImageDraw.Draw(im)
    if kind == 'ac':
        d.rectangle([2, 3, 13, 12], fill=(170, 172, 176, 255), outline=INK)
        d.ellipse([5, 5, 10, 10], outline=(90, 92, 96, 255))
    elif kind == 'vent':
        d.ellipse([4, 4, 11, 11], fill=(110, 110, 116, 255), outline=INK)
        d.ellipse([6, 6, 9, 9], fill=(50, 50, 56, 255))
    elif kind == 'tank':
        d.ellipse([1, 1, 14, 14], fill=(130, 96, 64, 255), outline=INK)
        d.ellipse([4, 4, 11, 11], outline=(100, 72, 48, 255))
    elif kind == 'skylight':
        d.rectangle([2, 2, 13, 13], fill=(90, 140, 170, 255), outline=INK)
        d.line([(2, 13), (13, 2)], fill=(160, 210, 230, 255))
    return im


# ---- landmarks (drawn at full footprint size) -------------------------------------------------------

def landmark(style, w, h):
    W_, H_ = w * T, h * T
    im = img(W_, H_)
    d = ImageDraw.Draw(im)
    r = random.Random(sum(ord(c) for c in style))

    def base(col, kind='flat'):
        noise_fill(im, (0, 0, W_, H_), col, r.randint(0, 999), 6)
        d.rectangle([0, 0, W_ - 1, H_ - 1], outline=INK)
        d.rectangle([2, 2, W_ - 3, H_ - 3], outline=shade(col + (255,), 0.75))

    def sign(text_col, y=None):
        y = H_ - 14 if y is None else y
        d.rectangle([W_ // 2 - 20, y, W_ // 2 + 20, y + 8], fill=(30, 30, 36, 255), outline=INK)
        d.line([(W_ // 2 - 16, y + 4), (W_ // 2 + 16, y + 4)], fill=text_col)

    if style == 'pharmacy':
        base((200, 210, 210))
        d.rectangle([W_ // 2 - 10, H_ // 2 - 3, W_ // 2 + 10, H_ // 2 + 3], fill=(60, 170, 90, 255))
        d.rectangle([W_ // 2 - 3, H_ // 2 - 10, W_ // 2 + 3, H_ // 2 + 10], fill=(60, 170, 90, 255))
    elif style == 'gunshop':
        base((96, 84, 70))
        for k in range(3):
            d.rectangle([8 + k * 40, 8, 30 + k * 40, 30], fill=(130, 120, 100, 255), outline=INK)
        sign((240, 80, 60, 255))
    elif style == 'mast':
        base((90, 90, 96))
        cx, cy = W_ // 2, H_ // 2
        for k in range(4):
            a = k * math.pi / 2 + math.pi / 4
            d.line([(cx, cy), (cx + math.cos(a) * W_ * 0.45, cy + math.sin(a) * H_ * 0.45)], fill=(200, 60, 50, 255), width=2)
        d.ellipse([cx - 7, cy - 7, cx + 7, cy + 7], fill=(230, 230, 230, 255), outline=INK)
        d.ellipse([cx - 2, cy - 2, cx + 2, cy + 2], fill=(255, 60, 40, 255))
    elif style == 'subway':
        base((120, 116, 108))
        d.rectangle([12, 12, W_ - 13, H_ - 13], fill=(70, 92, 70, 255), outline=INK)   # glass roof
        for x in range(12, W_ - 12, 12):
            d.line([(x, 12), (x, H_ - 13)], fill=(40, 60, 40, 255))
        d.ellipse([W_ // 2 - 10, H_ - 30, W_ // 2 + 10, H_ - 10], fill=(40, 90, 170, 255), outline=INK)
        d.text((W_ // 2 - 3, H_ - 26), "U", fill=(255, 255, 255, 255))
    elif style == 'firestation':
        base((170, 60, 50))
        for k in range(3):
            d.rectangle([10 + k * 58, H_ - 26, 56 + k * 58, H_ - 4], fill=(220, 220, 220, 255), outline=INK)
        d.ellipse([W_ - 30, 8, W_ - 8, 30], fill=(230, 190, 60, 255), outline=INK)
    elif style == 'precinct':
        base((70, 80, 110))
        d.polygon([(W_ // 2, 14), (W_ // 2 + 14, 30), (W_ // 2, 46), (W_ // 2 - 14, 30)], fill=(230, 200, 80, 255), outline=INK)
        for x in range(8, W_ - 8, 20):
            d.rectangle([x, H_ - 18, x + 8, H_ - 8], fill=(200, 210, 230, 255))
    elif style == 'waterworks':
        base((118, 118, 112))
        for (cx, cy, rr) in ((W_ * 0.25, H_ * 0.3, 40), (W_ * 0.7, H_ * 0.3, 40), (W_ * 0.25, H_ * 0.72, 34), (W_ * 0.72, H_ * 0.72, 34)):
            d.ellipse([cx - rr, cy - rr, cx + rr, cy + rr], fill=(90, 96, 92, 255), outline=INK)
            d.ellipse([cx - rr + 6, cy - rr + 6, cx + rr - 6, cy + rr - 6], fill=(70, 110, 120, 255))
            d.line([(cx - rr + 8, cy), (cx + rr - 8, cy)], fill=(160, 160, 150, 255), width=2)
    elif style == 'raildepot':
        base((110, 96, 82))
        for y in range(16, H_ - 10, 28):
            d.rectangle([4, y, W_ - 5, y + 14], fill=(90, 76, 64, 255))
            for x in range(4, W_ - 4, 8):
                d.line([(x, y), (x, y + 14)], fill=(70, 56, 46, 255))
            d.line([(4, y + 3), (W_ - 5, y + 3)], fill=(170, 170, 176, 255))
            d.line([(4, y + 11), (W_ - 5, y + 11)], fill=(170, 170, 176, 255))
    elif style == 'iceplant':
        base((190, 214, 226))
        for x in range(10, W_ - 10, 30):
            d.rectangle([x, 10, x + 22, H_ - 10], fill=(160, 196, 214, 255), outline=(110, 140, 160, 255))
        d.ellipse([W_ // 2 - 12, H_ // 2 - 12, W_ // 2 + 12, H_ // 2 + 12], fill=(230, 245, 255, 255), outline=INK)
    elif style == 'cathedral':
        base((150, 144, 132))
        # cross-shaped nave roof
        d.rectangle([W_ // 2 - 24, 6, W_ // 2 + 24, H_ - 6], fill=(90, 96, 120, 255), outline=INK)
        d.rectangle([10, H_ // 2 - 20, W_ - 11, H_ // 2 + 20], fill=(90, 96, 120, 255), outline=INK)
        d.line([(W_ // 2, 8), (W_ // 2, H_ - 8)], fill=(60, 64, 84, 255), width=2)
        d.ellipse([W_ // 2 - 18, H_ - 50, W_ // 2 + 18, H_ - 14], fill=(170, 150, 90, 255), outline=INK)   # bell tower
        d.ellipse([W_ // 2 - 8, H_ - 40, W_ // 2 + 8, H_ - 24], fill=(220, 190, 90, 255))
    elif style == 'bus':
        base((230, 190, 60))
        for x in range(6, W_ - 6, 10):
            d.rectangle([x, 6, x + 6, H_ - 7], fill=(120, 150, 170, 255))
        d.text((6, 2), "19", fill=(30, 30, 30, 255))
    elif style == 'forge':
        base((96, 70, 60))
        d.ellipse([W_ // 2 - 14, H_ // 2 - 14, W_ // 2 + 14, H_ // 2 + 14], fill=(60, 50, 46, 255), outline=INK)
        d.ellipse([W_ // 2 - 6, H_ // 2 - 6, W_ // 2 + 6, H_ // 2 + 6], fill=(255, 140, 50, 255))
    elif style == 'labs':
        base((210, 212, 216))
        for x in range(10, W_ - 10, 26):
            for y in range(10, H_ - 20, 26):
                d.rectangle([x, y, x + 16, y + 16], fill=(120, 170, 200, 255), outline=INK)
        # scorched roof (it burned on day one)
        for _ in range(40):
            x, y = r.randint(0, W_ - 1), r.randint(0, H_ - 1)
            d.ellipse([x - 6, y - 4, x + 6, y + 4], fill=(40, 34, 30, 200))
        sign((120, 220, 120, 255))
    elif style == 'stadium':
        base((170, 166, 160))
        d.ellipse([12, 12, W_ - 13, H_ - 13], fill=(130, 126, 120, 255), outline=INK)
        for k in range(6):
            d.ellipse([12 + k * 6, 12 + k * 5, W_ - 13 - k * 6, H_ - 13 - k * 5], outline=(110, 106, 100, 255))
        d.ellipse([60, 52, W_ - 61, H_ - 53], fill=(70, 140, 60, 255), outline=INK)
        d.line([(W_ // 2, 54), (W_ // 2, H_ - 55)], fill=(230, 230, 230, 255))
        d.ellipse([W_ // 2 - 12, H_ // 2 - 12, W_ // 2 + 12, H_ // 2 + 12], outline=(230, 230, 230, 255))
        for (x, y) in ((10, 10), (W_ - 20, 10), (10, H_ - 20), (W_ - 20, H_ - 20)):
            d.rectangle([x, y, x + 10, y + 10], fill=(250, 250, 200, 255), outline=INK)
    elif style == 'substation':
        base((110, 112, 104))
        for x in range(10, W_ - 10, 22):
            for y in range(10, H_ - 10, 22):
                d.rectangle([x, y, x + 14, y + 14], fill=(150, 150, 160, 255), outline=INK)
                d.line([(x + 2, y + 7), (x + 12, y + 7)], fill=(255, 220, 80, 255))
    elif style == 'lighthouse':
        base((180, 170, 150))
        d.ellipse([8, 8, W_ - 9, H_ - 9], fill=(230, 230, 226, 255), outline=INK)
        d.ellipse([20, 20, W_ - 21, H_ - 21], fill=(200, 60, 50, 255), outline=INK)
        d.ellipse([W_ // 2 - 6, H_ // 2 - 6, W_ // 2 + 6, H_ // 2 + 6], fill=(255, 240, 160, 255))
    elif style == 'freighter':
        noise_fill(im, (0, 0, W_, H_), WATER, 5, 3)
        d.polygon([(20, 4), (W_ - 20, 4), (W_ - 10, H_ // 2), (W_ - 20, H_ - 4), (20, H_ - 4), (10, H_ // 2)], fill=(150, 60, 50, 255), outline=INK)
        d.rectangle([28, 16, W_ - 29, H_ - 17], fill=(90, 92, 96, 255), outline=INK)
        for y in range(24, H_ - 24, 26):
            d.rectangle([34, y, W_ - 35, y + 18], fill=(60, 96, 140, 255), outline=INK)
        d.rectangle([W_ // 2 - 18, H_ - 46, W_ // 2 + 18, H_ - 20], fill=(230, 230, 220, 255), outline=INK)
    else:
        base((140, 140, 140))
    # south wall face (depth)
    face = shade((40, 40, 46, 255), 1.0)
    if style != 'freighter':
        d.rectangle([0, H_ - 4, W_ - 1, H_ - 1], fill=face)
    return im


LANDMARKS = ['pharmacy', 'gunshop', 'mast', 'subway', 'firestation', 'precinct', 'waterworks', 'raildepot', 'iceplant',
             'cathedral', 'bus', 'forge', 'labs', 'stadium', 'substation', 'lighthouse', 'freighter']


# ---- props (top-down) --------------------------------------------------------------------------------

def car_body(w, h, col, kind='car', burnt=False):
    """Facing right. w x h px."""
    im = img(w, h)
    d = ImageDraw.Draw(im)
    body = col + (255,)
    if burnt:
        body = (60, 50, 46, 255)
    dark = shade(body, 0.6)
    # tires
    for tx in (4, w - 10):
        d.rectangle([tx, 0, tx + 5, 1], fill=INK)
        d.rectangle([tx, h - 2, tx + 5, h - 1], fill=INK)
    d.rounded_rectangle([1, 1, w - 2, h - 2], radius=3, fill=body, outline=INK)
    if kind == 'truck':
        d.rectangle([w // 2 + 1, 3, w - 4, h - 4], fill=shade(body, 0.85), outline=dark)    # cab
        d.rectangle([w // 2 + 5, 3, w // 2 + 8, h - 4], fill=(120, 160, 190, 255))           # windshield
        d.rectangle([4, 3, w // 2 - 2, h - 4], fill=shade(body, 0.7), outline=dark)           # bed
    else:
        d.rectangle([w // 2 - 6, 3, w // 2 + 6, h - 4], fill=shade(body, 0.85), outline=dark)  # roof
        d.rectangle([w // 2 + 6, 3, w // 2 + 9, h - 4], fill=(120, 160, 190, 255))             # windshield
        d.rectangle([w // 2 - 9, 3, w // 2 - 7, h - 4], fill=(100, 140, 170, 255))             # rear glass
    if burnt:
        r = random.Random(w * 7 + h)
        for _ in range(12):
            x, y = r.randint(2, w - 3), r.randint(2, h - 3)
            d.point((x, y), fill=(220, 120, 40, 255) if r.random() < 0.2 else (30, 26, 24, 255))
    else:
        d.point([(w - 2, 3), (w - 2, h - 4)], fill=(255, 250, 200, 255))  # headlights
        d.point([(1, 3), (1, h - 4)], fill=(220, 40, 40, 255))
    return im


def two_wheeler(motor, col):
    w, h = (22, 9) if motor else (18, 7)
    im = img(w, h)
    d = ImageDraw.Draw(im)
    d.ellipse([0, h // 2 - 2, 5, h // 2 + 2], fill=INK)
    d.ellipse([w - 6, h // 2 - 2, w - 1, h // 2 + 2], fill=INK)
    d.rectangle([3, h // 2 - 1, w - 4, h // 2 + 1], fill=col + (255,))
    if motor:
        d.rectangle([6, 1, 13, h - 2], fill=col + (255,), outline=INK)
        d.rectangle([13, 2, 15, h - 3], fill=(60, 60, 66, 255))
    d.line([(w - 6, 0), (w - 6, h - 1)], fill=(200, 200, 200, 255))  # handlebar
    return im


def tree(kind):
    if kind == 'pine':
        im = img(24, 24)
        d = ImageDraw.Draw(im)
        d.ellipse([3, 6, 22, 23], fill=(0, 0, 0, 70))
        for k, rr in enumerate((10, 7, 4)):
            d.polygon([(12 + rr, 12), (12, 12 - rr), (12 - rr, 12), (12, 12 + rr)], fill=shade((50, 104, 70, 255), 0.85 + k * 0.15), outline=INK if k == 0 else None)
        return im
    size = 24 if kind == 'tree' else 14
    im = img(size, size)
    d = ImageDraw.Draw(im)
    d.ellipse([3, 4, size - 1, size - 1], fill=(0, 0, 0, 70))
    base = (64, 120, 58) if kind == 'tree' else (78, 132, 64)
    d.ellipse([1, 1, size - 3, size - 3], fill=base + (255,), outline=INK)
    r = random.Random(size)
    for _ in range(size):
        x, y = r.randint(3, size - 6), r.randint(3, size - 6)
        d.point((x, y), fill=shade(base + (255,), 1.25))
    d.ellipse([4, 3, size // 2, size // 2], fill=shade(base + (255,), 1.15))
    return im


def small_prop(kind):
    im = img(16, 16)
    d = ImageDraw.Draw(im)
    r = random.Random(sum(ord(c) for c in kind))
    if kind == 'blood':
        for _ in range(7):
            x, y = r.randint(3, 12), r.randint(3, 12)
            rr = r.randint(1, 3)
            d.ellipse([x - rr, y - rr, x + rr, y + rr], fill=(120, 16, 20, 200))
    elif kind == 'rot':
        for _ in range(9):
            x, y = r.randint(2, 13), r.randint(2, 13)
            rr = r.randint(1, 4)
            d.ellipse([x - rr, y - rr, x + rr, y + rr], fill=(96, 50, 110, 170))
        d.point([(6, 6), (10, 9)], fill=(190, 120, 220, 255))
    elif kind == 'trash':
        d.rectangle([4, 5, 11, 11], fill=(70, 80, 70, 255), outline=INK)
        d.point([(5, 4), (9, 4)], fill=(200, 200, 190, 255))
    elif kind == 'paper':
        d.polygon([(4, 6), (10, 4), (12, 9), (6, 11)], fill=(230, 226, 210, 255))
    elif kind == 'tire':
        d.ellipse([4, 4, 11, 11], fill=INK)
        d.ellipse([6, 6, 9, 9], fill=(60, 60, 60, 255))
    elif kind == 'cone':
        d.polygon([(8, 3), (12, 12), (4, 12)], fill=(240, 120, 40, 255), outline=INK)
        d.line([(6, 9), (10, 9)], fill=(250, 250, 250, 255))
    elif kind == 'lamp':
        d.ellipse([5, 5, 10, 10], fill=(80, 84, 90, 255), outline=INK)
        d.ellipse([7, 7, 8, 8], fill=(255, 240, 180, 255))
    elif kind == 'barricade':
        d.rectangle([1, 4, 14, 11], fill=(200, 200, 196, 255), outline=INK)
        for x in range(2, 14, 4):
            d.line([(x, 5), (x + 2, 10)], fill=(220, 60, 40, 255), width=2)
    return im


def crane():
    im = img(32, 32)
    d = ImageDraw.Draw(im)
    d.rectangle([2, 2, 29, 29], outline=(230, 170, 40, 255), width=3)
    d.line([(2, 2), (29, 29)], fill=(230, 170, 40, 255), width=2)
    d.line([(29, 2), (2, 29)], fill=(230, 170, 40, 255), width=2)
    d.rectangle([12, 12, 19, 19], fill=(60, 60, 66, 255), outline=INK)
    return im


# ---- pickups, interactables -----------------------------------------------------------------------

def pickup(kind):
    im = img(16, 16)
    d = ImageDraw.Draw(im)
    if kind == 'ammo':
        d.rectangle([2, 4, 13, 12], fill=(86, 104, 60, 255), outline=INK)
        d.rectangle([2, 4, 13, 6], fill=(110, 130, 76, 255))
        for x in (5, 8, 11):
            d.line([(x, 8), (x, 10)], fill=(230, 190, 70, 255))
    elif kind == 'medkit':
        d.rectangle([2, 3, 13, 12], fill=(236, 236, 230, 255), outline=INK)
        d.rectangle([6, 5, 9, 10], fill=(210, 40, 40, 255))
        d.rectangle([5, 6, 10, 9], fill=(210, 40, 40, 255))
    elif kind == 'armor':
        d.polygon([(3, 3), (12, 3), (13, 12), (8, 14), (2, 12)], fill=(50, 70, 110, 255), outline=INK)
        d.line([(5, 6), (10, 6)], fill=(140, 170, 220, 255))
    elif kind == 'weapon':
        d.rectangle([1, 4, 14, 11], fill=(60, 60, 64, 255), outline=INK)
        d.rectangle([3, 6, 12, 9], fill=(150, 150, 156, 255))
        d.line([(9, 9), (9, 11)], fill=(150, 150, 156, 255))
    elif kind == 'cooler':
        d.rectangle([3, 4, 12, 12], fill=(60, 140, 210, 255), outline=INK)
        d.rectangle([3, 4, 12, 6], fill=(240, 240, 240, 255))
    elif kind == 'folder':
        d.rectangle([3, 4, 12, 12], fill=(230, 196, 90, 255), outline=INK)
        d.line([(5, 7), (10, 7)], fill=(120, 90, 40, 255))
        d.line([(5, 9), (9, 9)], fill=(120, 90, 40, 255))
    elif kind == 'generator':
        d.rectangle([1, 3, 14, 13], fill=(200, 160, 40, 255), outline=INK)
        d.rectangle([3, 5, 7, 11], fill=(60, 60, 60, 255))
        d.ellipse([9, 6, 12, 9], fill=(230, 60, 40, 255))
    elif kind == 'generator_on':
        d.rectangle([1, 3, 14, 13], fill=(200, 160, 40, 255), outline=INK)
        d.rectangle([3, 5, 7, 11], fill=(60, 60, 60, 255))
        d.ellipse([9, 6, 12, 9], fill=(90, 240, 90, 255))
    elif kind == 'switch':
        d.rectangle([3, 2, 12, 13], fill=(100, 100, 110, 255), outline=INK)
        d.rectangle([6, 5, 9, 10], fill=(220, 60, 50, 255))
    elif kind == 'safehouse':
        d.ellipse([1, 1, 14, 14], fill=(40, 120, 60, 255), outline=INK)
        d.rectangle([5, 7, 10, 12], fill=(230, 230, 220, 255))
        d.polygon([(4, 7), (8, 3), (11, 7)], fill=(230, 230, 220, 255))
    elif kind == 'safehouse_lit':
        d.ellipse([1, 1, 14, 14], fill=(90, 220, 110, 255), outline=INK)
        d.rectangle([5, 7, 10, 12], fill=(255, 255, 240, 255))
        d.polygon([(4, 7), (8, 3), (11, 7)], fill=(255, 255, 240, 255))
    elif kind == 'relic':
        d.rectangle([1, 3, 14, 12], fill=(70, 50, 90, 255), outline=INK)
        d.rectangle([3, 5, 12, 10], fill=(150, 110, 210, 255))
        d.point([(6, 7), (9, 7), (7, 8)], fill=(255, 240, 200, 255))
    elif kind == 'shard':
        d.polygon([(8, 1), (12, 7), (8, 15), (4, 7)], fill=(150, 150, 160, 255), outline=INK)
        d.line([(8, 3), (8, 12)], fill=(220, 220, 240, 255))
    elif kind == 'marker':
        d.ellipse([1, 1, 14, 14], outline=(255, 220, 80, 255), width=2)
        d.ellipse([6, 6, 9, 9], fill=(255, 220, 80, 255))
    elif kind == 'arrow':
        d.polygon([(8, 0), (15, 13), (8, 10), (1, 13)], fill=(255, 220, 80, 255), outline=INK)
    elif kind == 'dot':
        d.ellipse([3, 3, 12, 12], fill=(255, 255, 255, 255), outline=INK)
    elif kind == 'lair':
        d.ellipse([1, 1, 14, 14], fill=(70, 20, 30, 255), outline=INK)
        d.line([(4, 4), (11, 11)], fill=(220, 60, 60, 255), width=2)
        d.line([(11, 4), (4, 11)], fill=(220, 60, 60, 255), width=2)
    return im


def nest(frame):
    im = img(40, 40)
    d = ImageDraw.Draw(im)
    pulse = 2 if frame else 0
    d.ellipse([2, 6, 37, 37], fill=(0, 0, 0, 80))
    d.ellipse([4 - pulse, 4 - pulse, 35 + pulse, 33 + pulse], fill=(96, 44, 92, 255), outline=INK)
    r = random.Random(77)
    for _ in range(14):
        x, y = r.randint(8, 30), r.randint(8, 28)
        rr = r.randint(2, 4)
        d.ellipse([x - rr, y - rr, x + rr, y + rr], fill=(140, 70, 140, 255))
    for (x, y) in ((14, 14), (24, 18), (18, 24)):
        d.ellipse([x - 2 - pulse // 2, y - 2 - pulse // 2, x + 2 + pulse // 2, y + 2 + pulse // 2], fill=(220, 160, 90, 255))
    return im


def hound(frame):
    """Side view (faces right), lean and red-eyed. 32x20."""
    im = img(32, 20)
    d = ImageDraw.Draw(im)
    body = (84, 40, 36, 255)
    legs = [(8, 12), (11, 12), (21, 12), (24, 12)]
    sw = [3, -2, -3, 2][frame % 4]
    for i, (x, y) in enumerate(legs):
        dx = sw if i % 2 == 0 else -sw
        d.line([(x, y), (x + dx, 18)], fill=shade(body, 0.7), width=2)
    d.ellipse([4, 6, 27, 14], fill=body, outline=INK)
    for x in range(8, 24, 3):
        d.point((x, 7), fill=(150, 60, 50, 255))   # raised hackles
    d.polygon([(24, 4), (31, 7), (30, 11), (24, 11)], fill=body, outline=INK)
    d.polygon([(25, 4), (26, 1), (27, 4)], fill=body)
    d.point((28, 7), fill=(255, 60, 40, 255))
    d.line([(29, 10), (31, 10)], fill=(240, 230, 220, 255))
    d.line([(4, 9), (0, 6)], fill=body, width=2)
    return im


def first_turned(frame, kind):
    """The master: a towering gaunt figure in a torn night-guard uniform, graystone growths on his shoulders. 48x56."""
    im = img(48, 56)
    d = ImageDraw.Draw(im)
    skin = (150, 160, 150, 255)
    coat = (40, 50, 70, 255)
    step = [0, 2, 0, -2][frame % 4] if kind == 'walk' else 0
    lift = -3 if kind == 'attack' and frame == 1 else 0
    d.ellipse([10, 48, 38, 55], fill=(0, 0, 0, 90))
    d.line([(19, 36), (17 + step, 52)], fill=coat, width=5)
    d.line([(29, 36), (31 - step, 52)], fill=coat, width=5)
    d.polygon([(12, 16), (36, 16), (38, 40), (10, 40)], fill=coat, outline=INK)
    d.line([(24, 18), (24, 38)], fill=(30, 36, 52, 255))
    d.rectangle([20, 22, 24, 25], fill=(210, 180, 60, 255))   # the old guard badge
    # arms (long, reaching)
    ay = 30 + lift
    d.line([(12, 20), (4, ay)], fill=coat, width=5)
    d.line([(36, 20), (44, ay)], fill=coat, width=5)
    d.ellipse([1, ay - 2, 7, ay + 4], fill=skin, outline=INK)
    d.ellipse([41, ay - 2, 47, ay + 4], fill=skin, outline=INK)
    # head
    d.ellipse([16, 2, 32, 18], fill=skin, outline=INK)
    d.point([(21, 9), (27, 9)], fill=(255, 230, 120, 255))
    d.line([(21, 14), (27, 14)], fill=(60, 30, 30, 255))
    # graystone growths
    for (x, y, rr) in ((12, 14, 5), (36, 14, 5), (30, 4, 3), (14, 30, 3)):
        d.polygon([(x, y - rr), (x + rr, y), (x, y + rr), (x - rr, y)], fill=(160, 160, 176, 255), outline=INK)
        d.point((x, y), fill=(230, 220, 255, 255))
    return im


def herald_crown():
    im = img(16, 8)
    d = ImageDraw.Draw(im)
    d.polygon([(1, 7), (3, 1), (6, 5), (8, 0), (10, 5), (13, 1), (15, 7)], fill=(160, 160, 176, 255), outline=INK)
    return im


def minimap(city, building_color):
    """One pixel per tile (y flipped so north is up in the image)."""
    W_, H_ = city['w'], city['h']
    col = {'r': (58, 58, 64), 'h': (58, 58, 64), 'v': (58, 58, 64), 'c': (88, 88, 92), 'C': (88, 88, 92), 's': (128, 126, 120),
           'g': (64, 112, 56), 'y': (80, 128, 62), 'p': (150, 126, 86), 'o': (118, 98, 86), 'i': (104, 104, 100), 'd': (120, 84, 52),
           'w': (40, 84, 140), 'b': (96, 82, 70), 'f': (30, 30, 30), 'x': (96, 80, 64), 'B': (40, 40, 44), 'l': (78, 78, 84)}
    im = Image.new('RGBA', (W_, H_))
    p = im.load()
    for y, row in enumerate(city['rows']):
        for x, c in enumerate(row):
            p[x, H_ - 1 - y] = col.get(c, (255, 0, 255)) + (255,)
    d = ImageDraw.Draw(im)
    for b in city['buildings']:
        c = building_color(b['style'])
        d.rectangle([b['x'], H_ - b['y'] - b['h'], b['x'] + b['w'] - 1, H_ - b['y'] - 1], fill=c + (255,))
    return im


def building_color(style):
    if style in ROOF_STYLES:
        c = ROOF_STYLES[style][0]
        return tuple(int(v * 0.85) for v in c)
    return (200, 180, 120)

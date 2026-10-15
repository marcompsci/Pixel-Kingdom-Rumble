"""
Biome tile sets (32 px = 1 world unit). Each biome gets:
  top   - surface tile (grass / snow / brass roof / crust) that also tiles horizontally,
  fill  - body tile that tiles in both directions,
  stone - a block tile (bricks / plates / crystal),
  plank - one-way platform.
Everything is drawn procedurally with seeded noise so tiles are seamless.
"""
import math, random
from px import Canvas, Ramp, shift, mix, INK, hex2rgb

T = 32


def C(r, g, b):
    return (r, g, b)


def rnd(seed):
    return random.Random(seed)


def vnoise(w, h, cell, seed):
    """tileable value noise in 0..1"""
    r = rnd(seed)
    gw, gh = max(1, w // cell), max(1, h // cell)
    g = [[r.random() for _ in range(gw)] for _ in range(gh)]
    out = [[0.0] * w for _ in range(h)]
    for y in range(h):
        for x in range(w):
            fx, fy = x / cell, y / cell
            x0, y0 = int(fx) % gw, int(fy) % gh
            x1, y1 = (x0 + 1) % gw, (y0 + 1) % gh
            tx, ty = fx - int(fx), fy - int(fy)
            tx = tx * tx * (3 - 2 * tx)
            ty = ty * ty * (3 - 2 * ty)
            a = g[y0][x0] * (1 - tx) + g[y0][x1] * tx
            b = g[y1][x0] * (1 - tx) + g[y1][x1] * tx
            out[y][x] = a * (1 - ty) + b * ty
    return out


def fbm(w, h, seed, cells=(16, 8, 4)):
    layers = [vnoise(w, h, c, seed + i * 17) for i, c in enumerate(cells)]
    wts = [0.55, 0.3, 0.15][:len(cells)]
    return [[sum(l[y][x] * wt for l, wt in zip(layers, wts)) for x in range(w)] for y in range(h)]


THEMES = {
    'meadow':   dict(surf=C(110, 200, 72), surf2=C(64, 150, 58), fill=C(150, 98, 60), fill2=C(116, 72, 46), rock=C(176, 150, 120),
                     stone=C(150, 156, 180), plank=C(206, 146, 82), trim=C(246, 196, 72), flowers=[C(255, 230, 90), C(255, 120, 140), C(250, 250, 250)]),
    'garden':   dict(surf=C(140, 220, 110), surf2=C(84, 170, 84), fill=C(168, 128, 96), fill2=C(132, 96, 74), rock=C(200, 180, 150),
                     stone=C(220, 214, 196), plank=C(226, 186, 120), trim=C(255, 214, 120), flowers=[C(255, 160, 200), C(200, 150, 255), C(255, 240, 140)]),
    'city':     dict(surf=C(206, 120, 72), surf2=C(150, 80, 56), fill=C(96, 70, 92), fill2=C(70, 50, 72), rock=C(130, 100, 120),
                     stone=C(120, 96, 128), plank=C(176, 120, 72), trim=C(250, 200, 90), roof=True, flowers=[]),
    'ember':    dict(surf=C(84, 60, 64), surf2=C(56, 40, 46), fill=C(70, 50, 54), fill2=C(48, 34, 40), rock=C(110, 80, 80),
                     stone=C(92, 70, 74), plank=C(120, 84, 66), trim=C(255, 140, 60), glow=C(255, 120, 40), flowers=[]),
    'frost':    dict(surf=C(236, 246, 255), surf2=C(178, 206, 236), fill=C(110, 140, 190), fill2=C(84, 108, 160), rock=C(160, 200, 236),
                     stone=C(150, 186, 226), plank=C(170, 140, 120), trim=C(170, 230, 255), snow=True, flowers=[]),
    'spire':    dict(surf=C(126, 92, 176), surf2=C(84, 60, 130), fill=C(60, 46, 90), fill2=C(42, 32, 66), rock=C(110, 80, 150),
                     stone=C(90, 70, 130), plank=C(110, 90, 140), trim=C(230, 140, 255), glow=C(220, 120, 255), crystal=True, flowers=[]),
    'shattered': dict(surf=C(150, 120, 210), surf2=C(100, 80, 160), fill=C(76, 60, 110), fill2=C(54, 42, 84), rock=C(140, 110, 190),
                      stone=C(110, 92, 150), plank=C(150, 120, 170), trim=C(255, 210, 120), glow=C(140, 220, 255), crystal=True, flowers=[]),
    'foundry':  dict(surf=C(206, 160, 90), surf2=C(150, 108, 60), fill=C(110, 96, 100), fill2=C(84, 72, 80), rock=C(150, 130, 120),
                     stone=C(146, 130, 126), plank=C(200, 150, 80), trim=C(255, 200, 90), metal=True, flowers=[]),
    'sunken':   dict(surf=C(80, 180, 150), surf2=C(50, 130, 120), fill=C(60, 96, 110), fill2=C(42, 72, 88), rock=C(100, 150, 150),
                     stone=C(90, 140, 140), plank=C(150, 130, 100), trim=C(230, 200, 120), flowers=[C(255, 140, 160), C(255, 210, 120)]),
    'toy':      dict(surf=C(255, 150, 190), surf2=C(220, 100, 150), fill=C(130, 200, 240), fill2=C(100, 160, 220), rock=C(255, 240, 160),
                     stone=C(255, 200, 90), plank=C(250, 230, 200), trim=C(255, 255, 255), toy=True, flowers=[C(255, 255, 255), C(255, 240, 120)]),
    'cave':     dict(surf=C(96, 120, 110), surf2=C(64, 88, 84), fill=C(70, 64, 82), fill2=C(52, 46, 62), rock=C(110, 104, 120),
                     stone=C(100, 96, 116), plank=C(140, 104, 72), trim=C(120, 230, 200), glow=C(120, 240, 210), flowers=[C(120, 240, 210)]),
}

BIOME_THEME = {
    'Sunspire Meadows': 'meadow', 'Sky Gardens': 'garden', 'Brasslight Quarter': 'city', 'Ember Wastes': 'ember',
    'Frost Citadel': 'frost', 'Frostgear Peaks': 'frost', 'Shadow Spire': 'spire', "Sovereign's Core": 'spire',
    'Shattered Crossing': 'shattered', 'Sky Foundry': 'foundry', 'Tickworks Engine Hall': 'foundry',
    'Sunken Clockworks': 'sunken', 'Toy Fortress': 'toy', 'Under the Isles': 'cave',
    'Winter Village': 'winter', 'Sweetheart Gardens': 'valentine', 'Harvest Hollow': 'harvest', 'Midnight City': 'newyear',
    'Haunted Hollow': 'haunted', 'Moonlit Manor': 'haunted', 'Starfall Observatory': 'astral',
}


# ---- body / fill -----------------------------------------------------------------------------------
def draw_fill(c, th, seed, y0=0, y1=T):
    """earthy body with pebbles; tileable"""
    n = fbm(T, T, seed, (16, 8))
    f1, f2 = th['fill'], th['fill2']
    R = Ramp(f1)
    for y in range(y0, y1):
        for x in range(T):
            v = n[y][x]
            if th.get('metal'):
                col = f1 if v > 0.45 else f2
            else:
                col = R.light if v > 0.72 else (f1 if v > 0.36 else f2)
            c.put(x, y, col)
    r = rnd(seed + 5)
    # pebbles / ore, wrapped
    for i in range(5):
        px_, py_ = r.randrange(T), r.randrange(y0, y1)
        rr = r.choice([1.2, 1.6, 2.2])
        rock = th['rock']
        rr_ = Ramp(rock)
        for dy in range(-3, 4):
            for dx in range(-3, 4):
                if (dx * dx) / (rr * rr) + (dy * dy) / ((rr * 0.8) ** 2) <= 1:
                    x, y = (px_ + dx) % T, py_ + dy
                    if y0 <= y < y1:
                        c.put(x, y, rr_.light if dy > 0 and dx < 1 else (rr_.shadow if dy < 0 else rock))
        x, y = (px_ - 1) % T, py_ + 1
        if y0 <= y < y1:
            c.put(x, y, rr_.hi)
    if th.get('glow'):
        # glowing veins
        for i in range(2):
            x = r.randrange(T)
            y = r.randrange(y0 + 2, y1 - 2)
            for k in range(r.randrange(5, 10)):
                c.put(x % T, y, th['glow'], lock=True)
                x += 1
                y += r.choice([-1, 0, 0, 1])
                y = max(y0, min(y1 - 1, y))
    if th.get('crystal'):
        for i in range(2):
            x, y = r.randrange(T), r.randrange(y0 + 3, max(y0 + 4, y1 - 3))
            cr = Ramp(th['glow'])
            for k in range(4):
                for j in range(-k // 2, k // 2 + 1):
                    c.put((x + j) % T, y + 3 - k, cr.light if j < 0 else cr.base, lock=True)


def make_fill(th, seed):
    c = Canvas(T, T)
    draw_fill(c, th, seed)
    return c


# ---- top surface --------------------------------------------------------------------------------------
def make_top(th, seed):
    c = Canvas(T, T)
    draw_fill(c, th, seed + 1, 0, T - 6)
    r = rnd(seed)
    surf, surf2 = th['surf'], th['surf2']
    S = Ramp(surf)
    edge = 6  # surface band thickness
    if th.get('roof'):
        # brass-rimmed clay roof tiles
        for y in range(T - 9, T - 1):
            for x in range(T):
                row = (T - 1 - y) // 4
                xx = (x + (row % 2) * 4) % 8
                col = S.light if xx < 2 else (surf if xx < 6 else S.shadow)
                if (T - 1 - y) % 4 == 3:
                    col = S.deep
                c.put(x, y, col)
        for x in range(T):
            c.put(x, T - 1, th['trim'])
            c.put(x, T - 2, shift(th['trim'], dl=-0.18))
        return c
    if th.get('metal'):
        for y in range(T - 8, T):
            for x in range(T):
                col = S.light if y >= T - 2 else (surf if y >= T - 6 else S.shadow)
                c.put(x, y, col)
        for x in (3, 13, 19, 29):
            c.put(x, T - 4, S.hi, lock=True)
            c.put(x, T - 5, S.deep)
        for x in range(T):
            if x % 8 == 0:
                for y in range(T - 8, T - 1):
                    c.put(x, y, S.deep)
        return c
    if th.get('toy'):
        for y in range(T - 8, T):
            for x in range(T):
                stripe = ((x + (T - y)) // 4) % 2
                col = surf if stripe else (255, 250, 250)
                if y >= T - 2:
                    col = shift(col, dl=0.05)
                c.put(x, y, col)
        for x in range(T):
            c.put(x, T - 9, S.deep)
        # drips of icing
        for x0 in (4, 15, 24):
            for k in range(r.randrange(2, 5)):
                c.put(x0, T - 9 - k, surf)
                c.put(x0 + 1, T - 9 - k, S.shadow)
        return c
    # organic surface: wavy lower edge, tufts above
    for x in range(T):
        depth = edge + int(2 * math.sin((x / T) * math.tau * 2 + seed) + r.random() * 1.5)
        for k in range(depth):
            y = T - 1 - k
            if k == depth - 1:
                col = S.deep
            elif k == depth - 2:
                col = surf2
            elif k <= 1:
                col = S.light if (x + k) % 5 else S.hi
            else:
                col = surf if (x * 3 + k) % 7 else S.light
            c.put(x, y, col)
        # hanging drips of grass / snow over the soil
        if r.random() < 0.18:
            c.put(x, T - 1 - depth, surf2)
    if th.get('snow'):
        for x in range(0, T, 1):
            if r.random() < 0.12:
                c.put(x, T - 3, (255, 255, 255), lock=True)
        # icicles
        for x0 in (5, 18, 27):
            L = r.randrange(2, 5)
            for k in range(L):
                c.put(x0, T - edge - 2 - k, th['trim'])
        return c
    # little flowers / sparkles on top
    fl = th.get('flowers') or []
    for i in range(3 if fl else 0):
        x = r.randrange(2, T - 2)
        col = fl[i % len(fl)]
        c.put(x, T - 2, col, lock=True)
        c.put(x, T - 3, shift(col, dl=-0.2), lock=True)
    return c


# ---- stone block ----------------------------------------------------------------------------------------
def make_stone(th, seed):
    c = Canvas(T, T)
    base = th['stone']
    R = Ramp(base)
    r = rnd(seed)
    if th.get('metal'):
        for y in range(T):
            for x in range(T):
                col = base
                if x in (0,) or y == T - 1:
                    col = R.light
                if x == T - 1 or y == 0:
                    col = R.deep
                c.put(x, y, col)
        for (x, y) in ((3, 3), (T - 4, 3), (3, T - 4), (T - 4, T - 4)):
            c.put(x, y, R.hi, lock=True)
            c.put(x + 1, y - 1, R.deep)
        for x in range(4, T - 4):
            c.put(x, T // 2, R.shadow)
            c.put(x, T // 2 + 1, R.light)
        return c
    if th.get('crystal'):
        for y in range(T):
            for x in range(T):
                c.put(x, y, base)
        facets = [(0, 0, 16, 18), (16, 0, 32, 12), (16, 12, 32, 32), (0, 18, 16, 32)]
        for i, (a, b_, cc, d) in enumerate(facets):
            for y in range(b_, d):
                for x in range(a, cc):
                    t = ((x - a) / max(1, cc - a) + (y - b_) / max(1, d - b_)) / 2
                    c.put(x, y, R.light if t > 0.7 else (R.shadow if t < 0.25 else base))
            for x in range(a, cc):
                c.put(x, b_, R.deep)
            for y in range(b_, d):
                c.put(a, y, R.deep)
        c.put(20, 24, th['glow'], lock=True)
        c.put(21, 25, (255, 255, 255), lock=True)
        return c
    # bricks with bevels, 2 rows of 16px
    rows = 2
    bh = T // rows
    for row in range(rows):
        off = 0 if row % 2 == 0 else 8
        y0 = row * bh
        for b_ in range(-1, 3):
            x0 = b_ * 16 + off
            tone = r.choice([0, 0, 1, -1])
            col = R.light if tone == 1 else (R.shadow if tone == -1 else base)
            RR = Ramp(col)
            for y in range(y0, y0 + bh):
                for xx in range(x0, x0 + 16):
                    x = xx % T
                    ly, lx = y - y0, xx - x0
                    if ly == 0 or lx == 15:
                        cc_ = R.deep
                    elif ly == bh - 1 or lx == 0:
                        cc_ = RR.light
                    elif ly == 1 or lx == 14:
                        cc_ = RR.shadow
                    else:
                        cc_ = col
                    c.put(x, y, cc_)
            # crack / moss
            if r.random() < 0.35:
                cx = (x0 + r.randrange(4, 12)) % T
                cy = y0 + r.randrange(4, bh - 4)
                c.put(cx, cy, R.shadow)
                c.put((cx + 1) % T, cy, RR.light)
    if th.get('flowers') and th is THEMES.get('meadow') or th.get('surf') == THEMES['sunken']['surf']:
        for x in range(T):
            if r.random() < 0.3:
                c.put(x, T - 1, th['surf2'])
    if th.get('snow'):
        for x in range(T):
            c.put(x, T - 1, (240, 248, 255))
            if r.random() < 0.5:
                c.put(x, T - 2, (220, 236, 252))
    return c


def make_plank(th, seed):
    """one-way platform: plank deck with a trim edge, transparent below"""
    c = Canvas(T, T)
    p = th['plank']
    R = Ramp(p)
    for y in range(T - 10, T):
        for x in range(T):
            ly = T - 1 - y
            if ly == 0:
                col = th['trim']
            elif ly == 1:
                col = shift(th['trim'], dl=-0.2)
            elif ly == 9:
                col = R.deep
            else:
                col = R.light if ly in (2, 3) else (p if ly < 7 else R.shadow)
            if x % 16 == 15 and 2 <= ly <= 8:
                col = R.deep
            c.put(x, y, col)
    for x in (3, 19):
        c.put(x, T - 5, R.hi, lock=True)
        c.put(x + 9, T - 5, R.hi, lock=True)
    # support brackets
    for x0 in (6, 22):
        for k in range(4):
            c.put(x0 + k, T - 11 - k, R.shadow)
            c.put(x0 + k + 1, T - 11 - k, R.deep)
    return c

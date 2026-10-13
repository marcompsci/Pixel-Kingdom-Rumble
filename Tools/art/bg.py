"""
Parallax background layers for every world (all original). Each theme has three horizontally-tiling layers:
  far  (640x256)  mountains / skylines / giant gears, moves slowly
  mid  (640x224)  hills, trees, rooftops, crystals
  near (640x160)  bushes, fences, rocks right behind the play area
plus an optional `clouds` layer (640x192, transparent). The sky itself is a runtime gradient from each level's
sky colour, so a sunset level and a midday level in the same world both look right.
"""
import math, random
from px import Canvas, Ramp, shift, mix, INK
from tiles import C

LW = 640


class WrapCanvas(Canvas):
    def put(self, x, y, c, part=None, lock=False, a=255):
        x = int(x) % self.w
        y = int(y)
        if 0 <= y < self.h and c is not None:
            if len(c) == 4:
                a = c[3]
            self.px[y][x] = (c[0], c[1], c[2], a)
            if part is not None:
                self.part[y][x] = part
            self.lock[y][x] = lock

    def get(self, x, y):
        y = int(y)
        if 0 <= y < self.h:
            return self.px[y][int(x) % self.w]
        return None


def ridge(w, base, amp, rough, seed, octaves=6):
    """tileable 1D midpoint-displacement-ish ridge via summed sines"""
    r = random.Random(seed)
    phases = [(r.random() * math.tau, r.randint(1, 3) * (2 ** i)) for i in range(octaves)]
    out = []
    for x in range(w):
        v = 0
        a = amp
        for i, (ph, f) in enumerate(phases):
            v += math.sin(x / w * math.tau * f + ph) * a
            a *= rough
        out.append(base + v)
    return out


def shade_silhouette(c, heights, col, light_col=None, rim=True, y0=0, tex_seed=0, tex=0.0):
    """fill below a height profile with a lit top edge (light from the left)"""
    R = Ramp(col)
    lc = light_col or R.light
    r = random.Random(tex_seed)
    for x in range(c.w):
        h = int(heights[x])
        slope = heights[(x + 1) % c.w] - heights[x - 1]
        for y in range(y0, min(c.h, h + 1)):
            colr = col
            depth = h - y
            if depth <= 1 and rim:
                colr = lc if slope < 0.6 else R.base
            elif depth <= 3 and slope < -0.4:
                colr = mix(col, lc, 0.45)
            elif slope > 1.2 and depth < 6:
                colr = R.shadow
            if tex and r.random() < tex:
                colr = R.shadow if r.random() < 0.6 else R.light
            c.put(x, y, colr)


def tree_round(c, x, y, r_, leaf, trunk=C(110, 72, 50), seed=0):
    R = Ramp(leaf)
    c.fill(c.mask_rect(x - 1, y - r_ * 1.4, x + 1, y), Ramp(trunk), soft=True)
    rr = random.Random(seed)
    blobs = [(0, 0, r_)] + [(rr.uniform(-r_ * 0.7, r_ * 0.7), rr.uniform(-r_ * 0.3, r_ * 0.5), r_ * rr.uniform(0.55, 0.75)) for _ in range(4)]
    for (dx, dy, rad) in blobs:
        pts = c.mask_ellipse(x + dx, y + dy, rad, rad * 0.9)
        c.fill(pts, R, light=(-0.6, 0.8))


def pine(c, x, y, h, col, snow=False):
    R = Ramp(col)
    c.fill(c.mask_rect(x - 1, y - 3, x + 1, y), Ramp(C(90, 64, 54)), soft=True)
    for k in range(3):
        w = h * (0.5 - k * 0.12)
        yb = y + k * h * 0.26
        pts = c.mask_poly([(x - w, yb), (x + w, yb), (x, yb + h * 0.5)])
        c.fill(pts, R, light=(-0.7, 0.4))
        if snow:
            for (px_, py_) in pts:
                if py_ > yb + h * 0.5 - (py_ - yb) * 0.0 - 3 and abs(px_ - x) < 3:
                    c.put(px_, py_, C(240, 248, 255))
            for xx in range(int(x - w), int(x + w) + 1):
                if (xx + k) % 3 == 0:
                    c.put(xx, int(yb), C(236, 246, 255))


def building(c, x, w, h, col, win, roof=None, seed=0, lit=0.5, y0=0):
    R = Ramp(col)
    c.fill(c.mask_rect(x, y0, x + w - 1, h), R, light=(-0.9, 0.2))
    rr = random.Random(seed)
    if roof == 'peak':
        c.fill(c.mask_poly([(x - 2, h), (x + w + 1, h), (x + w / 2, h + w * 0.55)]), Ramp(shift(col, dl=-0.08)), light=(-0.8, 0.5))
    elif roof == 'dome':
        c.fill(c.mask_ellipse(x + w / 2, h, w / 2, w / 3), Ramp(shift(col, dl=-0.05)))
    elif roof == 'clock':
        c.fill(c.mask_rect(x + 2, h, x + w - 3, h + w * 0.8), R, light=(-0.9, 0.2))
        c.fill(c.mask_ellipse(x + w / 2, h + w * 0.45, w * 0.28, w * 0.28), Ramp(C(250, 230, 170)))
        c.fill(c.mask_poly([(x, h + w * 0.8), (x + w, h + w * 0.8), (x + w / 2, h + w * 1.4)]), Ramp(shift(col, dl=-0.1)))
    for wy in range(int(y0) + 4, int(h) - 3, 7):
        for wx in range(x + 3, x + w - 3, 5):
            if rr.random() < lit:
                c.put(wx, wy, win, lock=True)
                c.put(wx + 1, wy, win, lock=True)
                c.put(wx, wy + 1, win, lock=True)
                c.put(wx + 1, wy + 1, shift(win, dl=-0.15), lock=True)
            else:
                c.put(wx, wy, R.shadow)
                c.put(wx + 1, wy, R.shadow)


def gear(c, cx, cy, r, col, teeth=10):
    R = Ramp(col)
    pts = c.mask_ellipse(cx, cy, r * 0.8, r * 0.8)
    for i in range(teeth):
        a = i * math.tau / teeth
        tx, ty = cx + math.cos(a) * r * 0.85, cy + math.sin(a) * r * 0.85
        pts += c.mask_rect(tx - r * 0.14, ty - r * 0.14, tx + r * 0.14, ty + r * 0.14)
    c.fill(pts, R)
    for (x, y) in c.mask_ellipse(cx, cy, r * 0.5, r * 0.5):
        c.put(x, y, R.shadow)
    for (x, y) in c.mask_ellipse(cx, cy, r * 0.2, r * 0.2):
        c.put(x, y, R.light)


def crystal(c, x, y, h, w, col, ang=0):
    R = Ramp(col)
    d = math.radians(ang)
    tip = (x + math.sin(d) * h, y + math.cos(d) * h)
    pts = [(x - w, y), (x + w, y), (tip[0] + w * 0.4, tip[1] - w), tip, (tip[0] - w * 0.6, tip[1] - w)]
    p = c.fill(c.mask_poly(pts), R, light=(-0.9, 0.2))
    for (px_, py_) in c.line_pts(x - w * 0.2, y + 1, tip[0] - 1, tip[1] - 2):
        c.put(px_, py_, R.hi, p)


def clouds_layer(seed, col=C(255, 255, 255), n=7, h=192):
    c = WrapCanvas(LW, h)
    r = random.Random(seed)
    R = Ramp(col)
    for i in range(n):
        x = r.uniform(0, LW)
        y = r.uniform(h * 0.35, h * 0.85)
        s = r.uniform(10, 20)
        puffs = [(0, 0, s)] + [(r.uniform(-s * 1.6, s * 1.6), r.uniform(-s * 0.2, s * 0.5), s * r.uniform(0.5, 0.85)) for _ in range(5)]
        for (dx, dy, rad) in puffs:
            pts = [p for p in c.mask_ellipse(x + dx, y + dy, rad, rad * 0.7) if p[1] >= y - s * 0.45]
            for (px_, py_) in pts:
                ny = (py_ - (y + dy)) / (rad * 0.7)
                colr = R.hi if ny > 0.45 else (col if ny > -0.2 else R.shadow)
                c.put(px_, py_, colr + (235,))
    return c


# ---- themes --------------------------------------------------------------------------------------------------
def meadow(seed=1, flowers=True, garden=False):
    far = WrapCanvas(LW, 256)
    mtn = ridge(LW, 120, 34, 0.55, seed)
    shade_silhouette(far, mtn, C(150, 176, 226) if not garden else C(176, 200, 240), C(214, 228, 250))
    # floating isle with a castle (original)
    r = random.Random(seed)
    for i in range(2):
        ix = 160 + i * 320
        iy = 190 - i * 20
        far.fill(far.mask_poly([(ix - 34, iy), (ix + 34, iy), (ix + 12, iy - 26), (ix - 8, iy - 30)]), Ramp(C(160, 150, 190)), light=(-0.8, 0.3))
        far.fill(far.mask_rect(ix - 34, iy, ix + 34, iy + 4), Ramp(C(140, 210, 140)), soft=True)
        if i == 0:
            for (bx, bw, bh) in ((-18, 9, 22), (-6, 12, 32), (8, 9, 24)):
                building(far, ix + bx, bw, iy + bh, C(220, 214, 236), C(255, 240, 180), roof='peak', seed=i, lit=0.3, y0=iy + 3)
                for yy in range(int(iy + 4), int(iy + bh)):
                    pass
        else:
            tree_round(far, ix - 10, iy + 10, 9, C(140, 200, 150), seed=7)
            tree_round(far, ix + 12, iy + 8, 7, C(140, 200, 150), seed=8)
            if garden:
                for k in range(40):
                    far.put(ix + 30, iy - k, C(220, 240, 255) + (200,))
                    far.put(ix + 31, iy - k, C(190, 220, 255) + (200,))
    mid = WrapCanvas(LW, 224)
    hills = ridge(LW, 70, 22, 0.5, seed + 3, octaves=4)
    leaf = C(96, 178, 96) if not garden else C(120, 196, 120)
    shade_silhouette(mid, hills, C(84, 160, 92) if not garden else C(110, 186, 112), C(140, 210, 120), tex_seed=2, tex=0.03)
    r = random.Random(seed + 5)
    for i in range(9):
        x = i * 71 + r.randint(0, 30)
        y = hills[x % LW] - 4
        if garden and i % 2:
            tree_round(mid, x, y + 18, r.randint(11, 15), C(255, 170, 200), seed=i)
        else:
            tree_round(mid, x, y + 16, r.randint(10, 15), leaf, seed=i)
    near = WrapCanvas(LW, 160)
    bush = ridge(LW, 30, 9, 0.6, seed + 9, octaves=5)
    shade_silhouette(near, bush, C(60, 140, 76), C(110, 196, 96), tex_seed=4, tex=0.06)
    # fence
    for x in range(0, LW, 24):
        near.fill(near.mask_rect(x, 0, x + 2, 44), Ramp(C(200, 150, 100)), soft=True)
        near.put(x + 1, 44, C(230, 190, 140))
    for y in (24, 36):
        for x in range(LW):
            near.put(x, y, C(190, 140, 92))
            near.put(x, y + 1, C(150, 104, 70))
    if flowers:
        for i in range(60):
            x = r.randrange(LW)
            y = int(bush[x]) - r.randrange(0, 10)
            col = r.choice([C(255, 230, 90), C(255, 130, 150), C(250, 250, 250), C(200, 150, 255)])
            near.put(x, y, col, lock=True)
            near.put(x, y - 1, shift(col, dl=-0.25))
    return dict(far=far, mid=mid, near=near, clouds=clouds_layer(seed))


def city(seed=2):
    far = WrapCanvas(LW, 256)
    # moon + stars
    far.fill(far.mask_ellipse(470, 210, 20, 20), Ramp(C(255, 246, 214)))
    for (x, y, r_) in ((462, 214, 4), (478, 202, 3), (470, 222, 2)):
        for (px_, py_) in far.mask_ellipse(x, y, r_, r_):
            far.put(px_, py_, C(236, 222, 190))
    r = random.Random(seed)
    for i in range(70):
        x, y = r.randrange(LW), r.randrange(130, 256)
        far.put(x, y, C(255, 255, 255) if r.random() < 0.3 else C(200, 200, 255))
    x = 0
    while x < LW:
        w = r.randint(18, 40)
        h = r.randint(50, 120)
        building(far, x, w, h, C(70, 60, 110), C(255, 210, 120), roof=r.choice([None, 'peak', 'dome']), seed=x, lit=0.25)
        x += w + r.randint(0, 6)
    building(far, 300, 30, 150, C(80, 66, 120), C(255, 220, 140), roof='clock', seed=99, lit=0.2)
    mid = WrapCanvas(LW, 224)
    x = 0
    while x < LW:
        w = r.randint(34, 60)
        h = r.randint(40, 90)
        building(mid, x, w, h, C(110, 80, 120), C(255, 200, 110), roof='peak', seed=x + 3, lit=0.45)
        # chimney
        if r.random() < 0.5:
            mid.fill(mid.mask_rect(x + w - 10, h, x + w - 6, h + 14), Ramp(C(150, 90, 80)), soft=True)
        x += w + r.randint(2, 10)
    near = WrapCanvas(LW, 160)
    # lantern string + railings
    for x in range(LW):
        y = int(110 + 8 * math.cos((x % 160) / 160 * math.tau))
        near.put(x, y, C(60, 50, 60))
    for x in range(20, LW, 40):
        y = int(110 + 8 * math.cos((x % 160) / 160 * math.tau)) - 3
        near.fill(near.mask_rect(x - 2, y - 4, x + 2, y + 1, r=1), Ramp(C(255, 190, 90)))
        near.put(x, y - 2, C(255, 250, 210), lock=True)
    for x in range(0, LW, 10):
        near.fill(near.mask_rect(x, 0, x + 1, 26), Ramp(C(70, 56, 80)), soft=True)
    for x in range(LW):
        near.put(x, 26, C(110, 90, 120))
        near.put(x, 27, C(160, 130, 150))
    return dict(far=far, mid=mid, near=near, clouds=clouds_layer(seed, C(140, 120, 190), n=4))


def ember(seed=3):
    far = WrapCanvas(LW, 256)
    r = random.Random(seed)
    m = ridge(LW, 110, 30, 0.5, seed)
    shade_silhouette(far, m, C(100, 50, 56), C(170, 80, 70))
    for vx in (120, 420):
        far.fill(far.mask_poly([(vx - 70, 60), (vx + 70, 60), (vx + 14, 190), (vx - 14, 190)]), Ramp(C(80, 40, 46)), light=(-0.8, 0.3))
        for k in range(30):
            far.put(vx + r.randint(-8, 8), 190 + r.randint(0, 6), C(255, 160, 60))
        for y in range(150, 190):
            far.put(vx + int(math.sin(y * 0.3) * 3), y, C(255, 120, 40), lock=True)
    mid = WrapCanvas(LW, 224)
    m2 = ridge(LW, 60, 18, 0.55, seed + 2)
    shade_silhouette(mid, m2, C(60, 36, 44), C(140, 60, 50))
    for i in range(7):
        x = i * 90 + r.randint(0, 40)
        crystal(mid, x, m2[x % LW] - 4, r.randint(40, 80), 7, C(70, 44, 52), ang=r.uniform(-8, 8))
    near = WrapCanvas(LW, 160)
    m3 = ridge(LW, 26, 10, 0.6, seed + 5)
    shade_silhouette(near, m3, C(44, 28, 34), C(120, 50, 40), tex_seed=1, tex=0.04)
    for i in range(50):
        x = r.randrange(LW)
        near.put(x, int(m3[x]) + r.randint(4, 60), C(255, 160, 70) + (200,), lock=True)
    return dict(far=far, mid=mid, near=near)


def frost(seed=4):
    far = WrapCanvas(LW, 256)
    m = ridge(LW, 130, 46, 0.5, seed)
    shade_silhouette(far, m, C(150, 180, 230), C(240, 248, 255))
    for x in range(LW):  # snow caps
        h = int(m[x])
        if h > 140:
            for k in range(min(10, h - 140)):
                far.put(x, h - k, C(236, 244, 255) if k < 6 else C(200, 220, 246))
    mid = WrapCanvas(LW, 224)
    m2 = ridge(LW, 50, 14, 0.5, seed + 2, octaves=4)
    shade_silhouette(mid, m2, C(200, 220, 246), C(250, 252, 255))
    r = random.Random(seed)
    for i in range(16):
        x = i * 40 + r.randint(0, 20)
        pine(mid, x, m2[x % LW] - 2, r.randint(40, 64), C(60, 110, 120), snow=True)
    near = WrapCanvas(LW, 160)
    m3 = ridge(LW, 22, 8, 0.6, seed + 5)
    shade_silhouette(near, m3, C(220, 236, 252), C(255, 255, 255))
    for i in range(8):
        x = r.randrange(LW)
        crystal(near, x, m3[x] - 2, r.randint(14, 26), 4, C(150, 210, 250), ang=r.uniform(-15, 15))
    return dict(far=far, mid=mid, near=near, clouds=clouds_layer(seed, C(240, 246, 255), n=5))


def spire(seed=5, shattered=False):
    far = WrapCanvas(LW, 256)
    r = random.Random(seed)
    base = C(90, 60, 140) if not shattered else C(110, 90, 170)
    for i in range(10):
        x = i * 64 + r.randint(0, 30)
        h = r.randint(90, 200)
        w = r.randint(8, 16)
        far.fill(far.mask_poly([(x - w, 0), (x + w, 0), (x + 2, h), (x - 2, h)]), Ramp(base), light=(-0.8, 0.3))
        far.put(x, h - 2, C(255, 200, 255), lock=True)
    if shattered:
        for i in range(9):
            x, y = r.randrange(LW), r.randint(120, 230)
            s = r.randint(6, 14)
            far.fill(far.mask_poly([(x - s, y), (x, y + s * 0.6), (x + s, y), (x + 2, y - s * 1.2)]), Ramp(C(150, 130, 210)))
    mid = WrapCanvas(LW, 224)
    m2 = ridge(LW, 46, 14, 0.55, seed + 1)
    shade_silhouette(mid, m2, C(50, 34, 80), C(120, 80, 170))
    for i in range(12):
        x = i * 54 + r.randint(0, 20)
        crystal(mid, x, m2[x % LW] - 3, r.randint(26, 70), r.randint(5, 9),
                C(170, 110, 230) if not shattered else C(120, 200, 240), ang=r.uniform(-14, 14))
    near = WrapCanvas(LW, 160)
    m3 = ridge(LW, 24, 9, 0.6, seed + 4)
    shade_silhouette(near, m3, C(34, 22, 54), C(90, 60, 130))
    for i in range(30):
        x = r.randrange(LW)
        y = int(m3[x])
        for k in range(r.randint(4, 12)):
            near.put(x + (k % 3 == 0), y + k, C(40, 26, 60))
    return dict(far=far, mid=mid, near=near)


def foundry(seed=6):
    far = WrapCanvas(LW, 256)
    r = random.Random(seed)
    for (x, y, rad) in ((90, 150, 60), (330, 190, 44), (520, 130, 70), (230, 90, 34)):
        gear(far, x, y, rad, C(150, 110, 90))
    for x in (40, 200, 460, 600):
        far.fill(far.mask_rect(x, 0, x + 14, 170 + (x % 50)), Ramp(C(120, 90, 90)), light=(-0.9, 0.2))
        for k in range(6):
            far.fill(far.mask_ellipse(x + 7 + k * 3, 180 + (x % 50) + k * 10, 8 + k * 2, 6 + k), Ramp(C(170, 150, 150)), soft=True)
    mid = WrapCanvas(LW, 224)
    x = 0
    while x < LW:
        w = r.randint(40, 80)
        h = r.randint(40, 100)
        building(mid, x, w, h, C(110, 84, 80), C(255, 170, 80), seed=x, lit=0.35)
        x += w + r.randint(4, 14)
    for y in (60, 110):
        for x in range(LW):
            mid.put(x, y, C(200, 150, 80))
            mid.put(x, y + 1, C(150, 104, 60))
            mid.put(x, y + 2, C(110, 76, 50))
    near = WrapCanvas(LW, 160)
    for x in range(0, LW, 80):
        near.fill(near.mask_rect(x, 0, x + 6, 70), Ramp(C(170, 120, 70)), soft=True)
        near.fill(near.mask_rect(x - 2, 66, x + 8, 72), Ramp(C(210, 160, 80)), soft=True)
    for x in range(LW):
        near.put(x, 54, C(180, 130, 70))
        near.put(x, 55, C(130, 90, 56))
    for i in range(10):
        cx = r.randrange(LW)
        near.fill(near.mask_rect(cx, 0, cx + 20, 16, r=1), Ramp(C(170, 110, 64)))
    return dict(far=far, mid=mid, near=near)


def sunken(seed=7):
    far = WrapCanvas(LW, 256)
    r = random.Random(seed)
    for x in range(0, LW, 90):  # light rays
        for y in range(256):
            for k in range(14):
                xx = x + k + (256 - y) * 0.35
                if far.get(xx, y) is None:
                    far.put(xx, y, C(170, 240, 255) + (40,))
    for (x, w, h) in ((60, 40, 110), (240, 60, 80), (420, 36, 140), (560, 50, 90)):
        building(far, x, w, h, C(50, 110, 130), C(140, 230, 220), roof='dome', seed=x, lit=0.15)
    mid = WrapCanvas(LW, 224)
    m2 = ridge(LW, 30, 10, 0.5, seed + 1)
    shade_silhouette(mid, m2, C(36, 80, 96), C(70, 150, 150))
    for i in range(22):
        x = r.randrange(LW)
        h = r.randint(30, 90)
        for y in range(int(m2[x]), int(m2[x]) + h):
            xx = x + int(3 * math.sin(y * 0.15 + i))
            mid.put(xx, y, C(60, 150, 100))
            mid.put(xx + 1, y, C(40, 110, 80))
    near = WrapCanvas(LW, 160)
    m3 = ridge(LW, 20, 8, 0.6, seed + 3)
    shade_silhouette(near, m3, C(30, 64, 80), C(70, 140, 150))
    for i in range(14):
        x = r.randrange(LW)
        near.fill(near.mask_ellipse(x, m3[x] + 4, 5, 6), Ramp(r.choice([C(255, 130, 150), C(255, 180, 100), C(200, 120, 255)])))
    for i in range(40):
        x, y = r.randrange(LW), r.randrange(40, 160)
        near.put(x, y, C(200, 250, 255) + (180,), lock=True)
    return dict(far=far, mid=mid, near=near)


def toy(seed=8):
    far = WrapCanvas(LW, 256)
    r = random.Random(seed)
    cols = [C(255, 150, 190), C(130, 200, 240), C(255, 220, 120), C(170, 230, 160)]
    x = 0
    while x < LW:
        w = r.randint(24, 44)
        h = r.randint(60, 150)
        building(far, x, w, h, r.choice(cols), C(255, 255, 255), roof=r.choice(['peak', 'dome']), seed=x, lit=0.2)
        x += w + r.randint(10, 30)
    mid = WrapCanvas(LW, 224)
    for i in range(14):
        bx = r.randrange(LW)
        s = r.randint(18, 34)
        mid.fill(mid.mask_rect(bx, 0, bx + s, s * r.randint(1, 3)), Ramp(r.choice(cols)), light=(-0.8, 0.5))
        mid.fill(mid.mask_ellipse(bx + s / 2, s * 0.5, s * 0.2, s * 0.2), Ramp(C(255, 255, 255)))
    near = WrapCanvas(LW, 160)
    for i in range(18):
        x = r.randrange(LW)
        near.fill(near.mask_rect(x, 0, x + 3, 50), Ramp(C(250, 250, 250)), soft=True)
        near.fill(near.mask_ellipse(x + 1.5, 56, 10, 10), Ramp(r.choice(cols)))
        for a in range(0, 360, 30):
            near.put(x + 1.5 + math.cos(math.radians(a)) * 6, 56 + math.sin(math.radians(a)) * 6, C(255, 255, 255))
    return dict(far=far, mid=mid, near=near, clouds=clouds_layer(seed, C(255, 230, 245), n=5))


def cave(seed=9):
    far = WrapCanvas(LW, 256)
    r = random.Random(seed)
    m = ridge(LW, 70, 24, 0.55, seed)
    shade_silhouette(far, m, C(60, 54, 80), C(100, 90, 130))
    top = ridge(LW, 200, 26, 0.6, seed + 1)
    for x in range(LW):
        for y in range(int(top[x]), 256):
            far.put(x, y, C(48, 42, 64))
    for i in range(18):  # stalactites
        x = r.randrange(LW)
        L = r.randint(14, 44)
        far.fill(far.mask_poly([(x - 5, top[x % LW] + 1), (x + 5, top[x % LW] + 1), (x, top[x % LW] - L)]), Ramp(C(56, 50, 74)), light=(-0.8, 0.1))
    for i in range(20):
        x, y = r.randrange(LW), r.randint(20, 150)
        far.put(x, y, C(120, 240, 210), lock=True)
    mid = WrapCanvas(LW, 224)
    for i in range(8):
        x = i * 80 + r.randint(0, 30)
        w = r.randint(14, 26)
        mid.fill(mid.mask_rect(x, 0, x + w, 224), Ramp(C(64, 58, 86)), light=(-0.9, 0.1))
    m2 = ridge(LW, 40, 12, 0.5, seed + 2)
    shade_silhouette(mid, m2, C(52, 46, 70), C(90, 84, 116))
    for i in range(10):
        x = r.randrange(LW)
        crystal(mid, x, m2[x] - 2, r.randint(14, 30), 4, C(100, 230, 200), ang=r.uniform(-20, 20))
    near = WrapCanvas(LW, 160)
    m3 = ridge(LW, 20, 8, 0.6, seed + 4)
    shade_silhouette(near, m3, C(40, 34, 56), C(80, 70, 100))
    for i in range(12):  # glowing mushrooms
        x = r.randrange(LW)
        y = int(m3[x])
        near.fill(near.mask_rect(x - 1, y, x + 1, y + 8), Ramp(C(220, 210, 200)), soft=True)
        cap = [p for p in near.mask_ellipse(x, y + 9, 6, 4) if p[1] >= y + 8]
        near.fill(cap, Ramp(C(120, 240, 210)))
    return dict(far=far, mid=mid, near=near)


def dojo(seed=10):
    """Versus stage: sunset peaks, pagoda-like temple roofs (original), lanterns."""
    d = meadow(seed, flowers=False)
    far = WrapCanvas(LW, 256)
    m = ridge(LW, 140, 50, 0.5, seed)
    shade_silhouette(far, m, C(220, 140, 130), C(255, 210, 170))
    far.fill(far.mask_ellipse(320, 170, 30, 30), Ramp(C(255, 230, 160)))
    mid = WrapCanvas(LW, 224)
    hills = ridge(LW, 50, 12, 0.5, seed + 2)
    shade_silhouette(mid, hills, C(150, 90, 100), C(220, 150, 130))
    for x0 in (80, 400):
        for k, (w, y) in enumerate(((70, 50), (54, 80), (38, 106))):
            mid.fill(mid.mask_rect(x0 - w * 0.35, y - 26, x0 + w * 0.35, y), Ramp(C(200, 70, 60)), light=(-0.9, 0.2))
            mid.fill(mid.mask_poly([(x0 - w / 2 - 6, y), (x0 + w / 2 + 6, y), (x0 + w / 2 - 6, y + 12), (x0 - w / 2 + 6, y + 12)]),
                     Ramp(C(70, 60, 90)), light=(-0.6, 0.8))
            for xx in range(int(x0 - w * 0.3), int(x0 + w * 0.3), 6):
                mid.put(xx, y - 12, C(255, 210, 130), lock=True)
    near = WrapCanvas(LW, 160)
    for x in range(30, LW, 110):
        near.fill(near.mask_rect(x, 0, x + 3, 60), Ramp(C(90, 60, 60)), soft=True)
        near.fill(near.mask_rect(x - 5, 60, x + 8, 72, r=3), Ramp(C(255, 120, 80)))
        near.put(x + 1, 66, C(255, 240, 200), lock=True)
    bush = ridge(LW, 20, 7, 0.6, seed + 9)
    shade_silhouette(near, bush, C(120, 70, 80), C(200, 120, 110))
    return dict(far=far, mid=mid, near=near, clouds=clouds_layer(seed, C(255, 220, 210), n=5))


THEMES = {
    'meadow': lambda: meadow(1), 'garden': lambda: meadow(11, garden=True), 'city': city, 'ember': ember, 'frost': frost,
    'spire': spire, 'shattered': lambda: spire(12, shattered=True), 'foundry': foundry, 'sunken': sunken, 'toy': toy,
    'cave': cave, 'dojo': dojo,
}

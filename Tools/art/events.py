"""
Holiday Mode and Halloween Mode art (all original): five extra world themes (tiles + parallax backdrops),
holiday critters, the zombie cast (drawn with the fighter rig so they animate like the heroes), collectibles,
zombie-mode props (mystery crate, perk shrines, upgrade forge, barricades, wall-weapon outlines, power-up icons)
and the secret jack-o'-lanterns.
"""
import math, random
from px import Canvas, Ramp, shift, mix, INK
from tiles import C, THEMES as TILE_THEMES
import bg
from bg import WrapCanvas, LW, ridge, shade_silhouette, tree_round, pine, building, clouds_layer
from props import finish, gem, eye, legs, gear_at, rig_crop, st, bob_of
from chars import P, METAL, GOLD, WOOD

WHITE = C(255, 255, 255)

# ---------------------------------------------------------------------------------------------------------------
# Tile themes (merged into tiles.THEMES so build_all's tile step makes them)
TILE_THEMES.update({
    'winter':    dict(surf=C(246, 250, 255), surf2=C(196, 216, 240), fill=C(150, 96, 80), fill2=C(116, 72, 62), rock=C(200, 210, 230),
                      stone=C(220, 80, 80), plank=C(196, 150, 110), trim=C(110, 200, 120), snow=True, flowers=[C(255, 80, 80), C(80, 200, 110)]),
    'valentine': dict(surf=C(255, 170, 200), surf2=C(230, 110, 160), fill=C(190, 90, 130), fill2=C(150, 66, 104), rock=C(255, 210, 225),
                      stone=C(255, 230, 240), plank=C(250, 214, 220), trim=C(255, 255, 255), toy=True, flowers=[C(255, 60, 100), C(255, 255, 255)]),
    'harvest':   dict(surf=C(214, 150, 60), surf2=C(170, 100, 44), fill=C(130, 84, 52), fill2=C(100, 62, 40), rock=C(190, 150, 100),
                      stone=C(180, 120, 80), plank=C(214, 170, 100), trim=C(255, 200, 80), flowers=[C(255, 120, 40), C(200, 60, 40), C(255, 220, 90)]),
    'newyear':   dict(surf=C(90, 100, 160), surf2=C(60, 66, 120), fill=C(50, 50, 84), fill2=C(36, 36, 64), rock=C(110, 110, 160),
                      stone=C(80, 84, 130), plank=C(150, 130, 180), trim=C(255, 214, 90), roof=True, flowers=[]),
    'haunted':   dict(surf=C(96, 120, 80), surf2=C(64, 84, 60), fill=C(74, 60, 72), fill2=C(54, 42, 54), rock=C(110, 100, 112),
                      stone=C(110, 106, 120), plank=C(120, 96, 80), trim=C(255, 150, 50), glow=C(255, 150, 50), flowers=[C(255, 140, 40)]),
})


# ---------------------------------------------------------------------------------------------------------------
# Backdrops
def _stars(c, r, n, y0=120):
    for _ in range(n):
        c.put(r.randrange(LW), r.randrange(y0, c.h), WHITE if r.random() < 0.3 else C(200, 210, 255))


def winter(seed=21):
    r = random.Random(seed)
    far = WrapCanvas(LW, 256)
    m = ridge(LW, 120, 36, 0.5, seed)
    shade_silhouette(far, m, C(180, 200, 236), C(250, 252, 255))
    mid = WrapCanvas(LW, 224)
    m2 = ridge(LW, 46, 10, 0.5, seed + 2, octaves=4)
    shade_silhouette(mid, m2, C(230, 238, 252), C(255, 255, 255))
    x = 10
    while x < LW:
        w = r.randint(30, 44)
        h = int(m2[x % LW]) + r.randint(26, 40)
        building(mid, x, w, h, r.choice([C(200, 80, 70), C(120, 90, 150), C(80, 130, 110)]), C(255, 220, 130), roof='peak',
                 seed=x, lit=0.6, y0=int(m2[x % LW]) - 2)
        for xx in range(x - 2, x + w + 2):            # snowy roof line and fairy lights
            mid.put(xx, h + 1, C(250, 252, 255))
            if xx % 4 == 0:
                mid.put(xx, h - 1, r.choice([C(255, 80, 80), C(90, 220, 120), C(255, 220, 90), C(110, 170, 255)]), lock=True)
        x += w + r.randint(40, 70)
    near = WrapCanvas(LW, 160)
    m3 = ridge(LW, 20, 6, 0.6, seed + 5)
    shade_silhouette(near, m3, C(236, 244, 255), WHITE)
    for i in range(10):
        px_ = r.randrange(LW)
        pine(near, px_, m3[px_] - 2, r.randint(34, 52), C(50, 120, 90), snow=True)
        for k in range(6):                            # ornaments
            near.put(px_ + r.randint(-8, 8), m3[px_] + r.randint(6, 30), r.choice([C(255, 70, 70), C(255, 220, 80), C(120, 180, 255)]), lock=True)
    return dict(far=far, mid=mid, near=near, clouds=clouds_layer(seed, C(250, 250, 255), n=6))


def _heart(c, cx, cy, s, col):
    pts = c.mask_ellipse(cx - s * 0.5, cy + s * 0.3, s * 0.55, s * 0.55) + c.mask_ellipse(cx + s * 0.5, cy + s * 0.3, s * 0.55, s * 0.55)
    pts += c.mask_poly([(cx - s * 1.05, cy + s * 0.25), (cx + s * 1.05, cy + s * 0.25), (cx, cy - s * 1.0)])
    c.fill(pts, Ramp(col))


def valentine(seed=22):
    r = random.Random(seed)
    far = WrapCanvas(LW, 256)
    m = ridge(LW, 100, 26, 0.5, seed)
    shade_silhouette(far, m, C(240, 170, 200), C(255, 220, 235))
    for i in range(8):
        _heart(far, r.randrange(LW), r.randint(150, 230), r.randint(5, 10), C(255, 120, 160))
    mid = WrapCanvas(LW, 224)
    m2 = ridge(LW, 50, 12, 0.55, seed + 2)
    shade_silhouette(mid, m2, C(230, 140, 180), C(255, 200, 220))
    for i in range(12):
        x = i * 54 + r.randint(0, 20)
        tree_round(mid, x, m2[x % LW] + 26, r.randint(12, 18), C(255, 150, 190), seed=x)
    near = WrapCanvas(LW, 160)
    m3 = ridge(LW, 22, 8, 0.6, seed + 5)
    shade_silhouette(near, m3, C(200, 90, 140), C(255, 160, 200))
    for i in range(30):                               # roses
        x = r.randrange(LW)
        near.fill(near.mask_ellipse(x, m3[x] + 2, 2.4, 2.4), Ramp(C(230, 40, 80)))
    return dict(far=far, mid=mid, near=near, clouds=clouds_layer(seed, C(255, 230, 240), n=6))


def harvest(seed=23):
    r = random.Random(seed)
    far = WrapCanvas(LW, 256)
    m = ridge(LW, 90, 24, 0.5, seed)
    shade_silhouette(far, m, C(200, 120, 80), C(255, 190, 120))
    far.fill(far.mask_ellipse(160, 200, 24, 24), Ramp(C(255, 220, 140)))
    mid = WrapCanvas(LW, 224)
    m2 = ridge(LW, 44, 10, 0.55, seed + 2)
    shade_silhouette(mid, m2, C(190, 130, 60), C(240, 190, 90))
    for x in (60, 330):                               # barns
        base = int(m2[x])
        mid.fill(mid.mask_rect(x, base - 2, x + 44, base + 36), Ramp(C(190, 50, 40)), light=(-0.9, 0.2))
        mid.fill(mid.mask_poly([(x - 4, base + 36), (x + 48, base + 36), (x + 22, base + 58)]), Ramp(C(120, 40, 40)))
        mid.fill(mid.mask_rect(x + 16, base - 2, x + 28, base + 18), Ramp(C(240, 230, 210)))
    for i in range(18):
        x = r.randrange(LW)
        tree_round(mid, x, m2[x] + 22, r.randint(9, 14), r.choice([C(230, 120, 40), C(200, 60, 40), C(240, 190, 60)]), seed=i)
    near = WrapCanvas(LW, 160)
    m3 = ridge(LW, 20, 6, 0.6, seed + 5)
    shade_silhouette(near, m3, C(150, 100, 50), C(210, 160, 80))
    for i in range(24):                               # corn stalks and hay
        x = r.randrange(LW)
        for k in range(30):
            near.put(x + int(math.sin(k * 0.3)), m3[x] + k, C(200, 170, 70))
        near.fill(near.mask_ellipse(x + 1, m3[x] + 22, 2, 5), Ramp(C(250, 210, 80)))
    return dict(far=far, mid=mid, near=near)


def newyear(seed=24):
    r = random.Random(seed)
    far = WrapCanvas(LW, 256)
    _stars(far, r, 90, 100)
    for i in range(7):                                # firework bursts
        cx, cy = r.randrange(LW), r.randint(150, 235)
        col = r.choice([C(255, 90, 120), C(120, 220, 255), C(255, 220, 90), C(170, 255, 140), C(220, 140, 255)])
        for a in range(0, 360, 15):
            for k in range(4, 18, 3):
                far.put(cx + math.cos(math.radians(a)) * k, cy + math.sin(math.radians(a)) * k, col if k < 15 else WHITE, lock=True)
    x = 0
    while x < LW:
        w = r.randint(16, 34)
        h = r.randint(60, 140)
        building(far, x, w, h, C(40, 40, 76), C(255, 220, 140), roof=r.choice([None, 'dome', None]), seed=x, lit=0.35)
        x += w + r.randint(0, 4)
    mid = WrapCanvas(LW, 224)
    x = 0
    while x < LW:
        w = r.randint(30, 56)
        h = r.randint(36, 80)
        building(mid, x, w, h, C(64, 60, 104), C(255, 230, 160), roof='peak', seed=x + 3, lit=0.5)
        x += w + r.randint(4, 12)
    near = WrapCanvas(LW, 160)
    for x in range(LW):                               # bunting
        y = int(118 + 7 * math.cos((x % 80) / 80 * math.tau))
        near.put(x, y, C(220, 220, 240))
    for x in range(10, LW, 20):
        y = int(118 + 7 * math.cos((x % 80) / 80 * math.tau))
        near.fill(near.mask_poly([(x - 4, y), (x + 4, y), (x, y - 8)]), Ramp(r.choice([C(255, 90, 120), C(255, 214, 90), C(120, 200, 255)])))
    for x in range(0, LW, 10):
        near.fill(near.mask_rect(x, 0, x + 1, 26), Ramp(C(70, 66, 100)), soft=True)
    for x in range(LW):
        near.put(x, 26, C(140, 130, 170))
    return dict(far=far, mid=mid, near=near)


def _dead_tree(c, x, y, h, col):
    c.fill(c.mask_rect(x - 2, y, x + 2, y + h), Ramp(col), soft=True)
    r = random.Random(x)
    for k in range(5):
        by = y + h * (0.45 + k * 0.1)
        d = -1 if k % 2 else 1
        for (px_, py_) in c.line_pts(x, by, x + d * r.randint(8, 16), by + r.randint(6, 12)):
            c.put(px_, py_, col)
            c.put(px_, py_ + 1, col)


def haunted(seed=25):
    r = random.Random(seed)
    far = WrapCanvas(LW, 256)
    _stars(far, r, 60, 140)
    far.fill(far.mask_ellipse(420, 190, 34, 34), Ramp(C(255, 246, 200)))
    for (x, y, rr) in ((408, 198, 6), (430, 180, 4), (424, 204, 3)):
        for p in far.mask_ellipse(x, y, rr, rr):
            far.put(*p, C(236, 224, 180))
    m = ridge(LW, 80, 24, 0.5, seed)
    shade_silhouette(far, m, C(60, 50, 90), C(110, 90, 140))
    # haunted manor on the hill
    base = int(m[180])
    building(far, 150, 70, base + 60, C(50, 44, 70), C(255, 200, 90), roof='peak', seed=7, lit=0.3, y0=base - 2)
    building(far, 170, 18, base + 90, C(46, 40, 66), C(255, 200, 90), roof='peak', seed=8, lit=0.4, y0=base + 50)
    mid = WrapCanvas(LW, 224)
    m2 = ridge(LW, 40, 10, 0.55, seed + 2)
    shade_silhouette(mid, m2, C(54, 48, 70), C(90, 80, 110))
    for i in range(10):
        x = i * 64 + r.randint(0, 30)
        _dead_tree(mid, x, int(m2[x % LW]) - 2, r.randint(40, 70), C(40, 34, 48))
    near = WrapCanvas(LW, 160)
    m3 = ridge(LW, 20, 6, 0.6, seed + 5)
    shade_silhouette(near, m3, C(50, 60, 50), C(80, 100, 70))
    for i in range(18):                               # tombstones and pumpkins
        x = r.randrange(LW)
        y = int(m3[x])
        if r.random() < 0.6:
            near.fill(near.mask_rect(x - 5, y - 2, x + 5, y + 12, r=4), Ramp(C(120, 120, 136)))
            near.put(x, y + 8, C(80, 80, 96))
            near.put(x - 1, y + 7, C(80, 80, 96))
            near.put(x + 1, y + 7, C(80, 80, 96))
        else:
            near.fill(near.mask_ellipse(x, y + 4, 6, 5), Ramp(C(240, 130, 40)))
            near.put(x - 2, y + 5, C(255, 230, 120), lock=True)
            near.put(x + 2, y + 5, C(255, 230, 120), lock=True)
    return dict(far=far, mid=mid, near=near, clouds=clouds_layer(seed, C(120, 110, 150), n=4))


bg.THEMES.update({'winter': winter, 'valentine': valentine, 'harvest': harvest, 'newyear': newyear, 'haunted': haunted})


# ---------------------------------------------------------------------------------------------------------------
# Holiday critters (same frame rules as the other enemies: a = (kind, i, n))
def snowman(a=None):
    """Walker: three snowballs, a scarf and a carrot nose; waddles."""
    kind, ph, i = st(a)
    c = Canvas(24, 32)
    b = bob_of(kind, ph)
    tilt = int(round(math.sin(ph))) if kind == 'walk' else 0
    c.fill(c.mask_ellipse(12, 7, 9, 7), Ramp(C(240, 246, 255)))
    c.fill(c.mask_ellipse(12 + tilt, 17 + b, 7, 5.5), Ramp(C(240, 246, 255)))
    c.fill(c.mask_ellipse(12 + tilt, 25 + b, 5.5, 5), Ramp(C(244, 248, 255)))
    c.fill(c.mask_rect(6 + tilt, 21 + b, 18 + tilt, 22 + b), Ramp(C(220, 50, 60)))
    c.fill(c.mask_rect(15 + tilt, 15 + b, 17 + tilt, 21 + b), Ramp(C(220, 50, 60)))
    c.fill(c.mask_rect(8 + tilt, 29 + b, 16 + tilt, 30 + b), Ramp(C(40, 40, 50)))
    c.fill(c.mask_rect(9 + tilt, 30 + b, 15 + tilt, 31), Ramp(C(40, 40, 50)))
    eye(c, 13 + tilt, 25 + b, C(30, 30, 40), C(30, 30, 40), shut=kind == 'hurt')
    for k in range(4):
        c.put(17 + tilt + k, 24 + b, C(255, 140, 40), lock=True)
    for y in (6, 10, 16):
        c.put(12 + tilt, y + (b if y > 12 else 0), C(40, 40, 50), lock=True)
    return finish(c)


def cupid_bot(a=None):
    """Flyer: a little heart-shaped drone with flapping wings."""
    kind, ph, i = st(a)
    c = Canvas(32, 24)
    fold = (1 - math.cos(ph)) / 2 if kind in ('air', 'idle') else 0.4
    dy = int(round(math.sin(ph) * 0.8)) if kind in ('air', 'idle') else 0
    for sgn in (1, -1):
        wy = 14 + dy + sgn * (3 - fold * 3)
        c.fill(c.mask_ellipse(9, wy, 7 - fold * 2, 3 - fold), Ramp(WHITE), light=(-0.2, 0.9))
    _heart(c, 18, 12 + dy, 6, C(240, 70, 110))
    eye(c, 18, 12 + dy, WHITE, shut=kind == 'hurt')
    eye(c, 22, 12 + dy, WHITE, shut=kind == 'hurt')
    return finish(c)


def turkey(a=None):
    """Hopper: a plump bird with a fan of tail feathers; crouches, then springs."""
    kind, ph, i = st(a)
    c = Canvas(28, 28)
    squat = 2 if kind == 'idle' and i % 2 else 0
    air = kind == 'air'
    for k, col in enumerate((C(200, 60, 40), C(240, 150, 40), C(250, 210, 80), C(150, 90, 50), C(200, 60, 40))):
        ang = math.radians(100 + k * 22)
        c.fill(c.mask_capsule(10, 12 - squat, 10 + math.cos(ang) * 12, 12 - squat + math.sin(ang) * 12, 2.6), Ramp(col))
    c.fill(c.mask_ellipse(13, 10 - squat, 8, 7), Ramp(C(140, 86, 50)))
    c.fill(c.mask_ellipse(20, 16 - squat, 4, 4.4), Ramp(C(170, 110, 70)))
    c.fill(c.mask_poly([(23, 16 - squat), (27, 15 - squat), (23, 14 - squat)]), Ramp(C(250, 190, 60)))
    c.fill(c.mask_rect(23, 11 - squat, 24, 14 - squat), Ramp(C(230, 60, 60)))
    eye(c, 20, 17 - squat, WHITE, shut=kind == 'hurt')
    if not air:
        legs(c, (11, 15), 4 - squat, C(240, 170, 60))
    return finish(c)


def firecracker(a=None):
    """Walker: a fizzing paper firecracker on stubby legs; the fuse sparks."""
    kind, ph, i = st(a)
    c = Canvas(20, 32)
    b = bob_of(kind, ph)
    legs(c, (6, 12), 4 + b, C(60, 60, 80), ph if kind == 'walk' else None)
    c.fill(c.mask_rect(3, 4 + b, 16, 24 + b, r=2), Ramp(C(220, 40, 50)))
    for y in (8, 20):
        c.fill(c.mask_rect(3, y + b, 16, y + 1 + b), Ramp(GOLD), soft=True)
    eye(c, 9, 15 + b, WHITE, shut=kind == 'hurt')
    eye(c, 13, 15 + b, WHITE, shut=kind == 'hurt')
    c.fill(c.mask_rect(9, 25 + b, 10, 28 + b), Ramp(C(120, 100, 80)), soft=True)
    spark = C(255, 240, 120) if i % 2 == 0 else C(255, 160, 60)
    for (dx, dy) in ((0, 0), (1, 1), (-1, 1), (0, 2)):
        c.put(10 + dx, 29 + b + dy, spark, lock=True)
    return finish(c)


# ---------------------------------------------------------------------------------------------------------------
# Zombies: drawn with the fighter rig (arms reaching forward), so they share the heroes' animation quality.
def _zspec(skin, top, pants, build='normal', head='bald', hair=C(60, 70, 50), eye_=C(255, 230, 90), **kw):
    s = dict(build=build, head=head, extra=None, skin=skin, hair=hair, top=top, accent=shift(top, dl=-0.1), pants=pants,
             shoes=C(60, 50, 50), eye=eye_, belt=C(70, 60, 50),
             weapon=dict(type='fists', color=skin, accent=shift(skin, dl=-0.15), fx=C(200, 255, 160), reach=14))
    s.update(kw)
    return s


ZOMBIES = {
    'zombie_shambler': (_zspec(C(150, 190, 120), C(120, 110, 150), C(70, 70, 100)), 44),
    'zombie_runner': (_zspec(C(170, 200, 130), C(200, 70, 60), C(60, 60, 80), build='slim', head='spiky', hair=C(50, 40, 40)), 44),
    'zombie_bloater': (_zspec(C(140, 200, 110), C(150, 170, 80), C(90, 80, 70), build='bulky', eye_=C(255, 150, 60)), 44),
    'zombie_spitter': (_zspec(C(170, 150, 210), C(80, 160, 90), C(60, 60, 70), build='slim', head='hood', hair=C(60, 90, 60),
                              eye_=C(120, 255, 120)), 44),
    'zombie_brute': (_zspec(C(130, 160, 110), C(80, 80, 90), C(60, 50, 60), build='bulky', head='band', hair=C(160, 40, 40),
                            eye_=C(255, 60, 60)), 48),
}


def zombie(name, a=None):
    spec, h = ZOMBIES[name]
    kind, ph, i = st(a)
    reach = ((6, -3), (9, -2))                       # both arms out front
    if kind == 'walk':
        legs_ = [((-4, 1), (4, 0)), ((-3, 2), (3, 0)), ((-1, 3), (1, 1)), ((4, 0), (-4, 1)), ((3, 0), (-3, 2)), ((1, 1), (-1, 3))][i % 6]
        sway = [0, 1, 0, 0, 1, 0][i % 6]
        pose = P(by=-sway, lean=1, legs=legs_, hands=((reach[0][0], reach[0][1] + sway), (reach[1][0], reach[1][1] - sway)),
                 wang=0, flutter=i, eyes='angry')
    elif kind == 'hurt':
        pose = P(bx=-2, lean=-2, legs=((-5, 0), (2, 1)), hands=((-6, 2), (-1, 3)), wang=0, eyes='hurt', flutter=5)
    elif kind == 'attack':
        pose = P(bx=2, lean=2, legs=((-6, 0), (7, 0)), hands=((9, -1), (11, 0)), wang=0, eyes='angry', smear='swing')
    else:
        pose = P(by=-(i % 2), lean=1, hands=reach, wang=0, eyes='angry', flutter=i)
    c = rig_crop(spec, pose, 32, h)
    # torn clothes and stitches
    r = random.Random(sum(map(ord, name)))  # stable across runs (str hash is randomized)
    for _ in range(5):
        x, y = r.randrange(8, 24), r.randrange(10, 26)
        if c.px[y][x] is not None:
            c.put(x, y, shift(spec['top'], dl=-0.25))
    return c


def bat(a=None):
    kind, ph, i = st(a)
    c = Canvas(32, 20)
    fold = (1 - math.cos(ph)) / 2 if kind in ('air', 'idle') else 0.5
    dy = int(round(math.sin(ph))) if kind in ('air', 'idle') else 0
    for sgn in (-1, 1):
        tip = (16 + sgn * 15, 12 + dy - fold * 7)
        c.fill(c.mask_poly([(16, 11 + dy), (16 + sgn * 5, 14 + dy), tip, (16 + sgn * 10, 7 + dy), (16 + sgn * 4, 8 + dy)]),
               Ramp(C(70, 50, 90)), light=(-0.3, 0.9))
    c.fill(c.mask_ellipse(16, 10 + dy, 4.5, 4.5), Ramp(C(90, 66, 110)))
    c.put(14, 11 + dy, C(255, 80, 80), lock=True)
    c.put(18, 11 + dy, C(255, 80, 80), lock=True)
    c.put(14, 14 + dy, WHITE, lock=True)
    c.put(18, 14 + dy, WHITE, lock=True)
    return finish(c)


def pumpkin_king(a=None):
    """Boss: a bulky zombie with a carved, glowing pumpkin head and a crown of twigs."""
    kind, ph, i = st(a)
    base = zombie('zombie_brute', a)
    c = Canvas(40, 56)
    for y in range(min(base.h, 48)):
        for x in range(32):
            if base.px[y][x] is not None:
                c.px[y][x + 4] = base.px[y][x]
    bobv = 1 if kind == 'walk' and i % 2 else 0
    for y in range(19, 56):                           # the pumpkin replaces the (chibi) head
        for x in range(40):
            c.px[y][x] = None
    cy = 28 + bobv
    c.fill(c.mask_ellipse(20, cy, 11, 9), Ramp(C(240, 130, 40)))
    for dx in (-5, 0, 5):
        for y in range(cy - 7, cy + 8):
            c.put(20 + dx, y, shift(C(240, 130, 40), dl=-0.12))
    glow = C(255, 240, 120) if kind != 'hurt' else C(255, 120, 80)
    for ex in (15, 25):
        c.fill(c.mask_poly([(ex - 3, cy), (ex + 3, cy), (ex, cy + 4)]), Ramp(glow))
    c.fill(c.mask_poly([(12, cy - 4), (28, cy - 4), (20, cy - 1)]), Ramp(glow))
    for k in range(5):
        x = 13 + k * 3.5
        c.fill(c.mask_rect(x, cy + 8, x + 1, cy + 11 + (k % 2) * 2), Ramp(C(110, 80, 50)), soft=True)
    return finish(c)


def jack_lantern():
    c = Canvas(32, 28)
    c.fill(c.mask_ellipse(16, 12, 13, 11), Ramp(C(240, 130, 40)))
    for dx in (-6, 0, 6):
        for y in range(3, 22):
            c.put(16 + dx, y, shift(C(240, 130, 40), dl=-0.12))
    c.fill(c.mask_rect(15, 22, 17, 26), Ramp(C(90, 120, 60)))
    for ex in (10, 22):
        c.fill(c.mask_poly([(ex - 3, 13), (ex + 3, 13), (ex, 18)]), Ramp(C(255, 240, 120)))
    c.fill(c.mask_poly([(8, 8), (24, 8), (20, 4), (16, 7), (12, 4)]), Ramp(C(255, 240, 120)))
    return finish(c)


EVENT_ENEMIES = {
    'snowman': (snowman, dict(idle=2, walk=4, hurt=1)),
    'cupid_bot': (cupid_bot, dict(idle=4, air=4, hurt=1)),
    'turkey': (turkey, dict(idle=2, air=2, hurt=1)),
    'firecracker': (firecracker, dict(idle=2, walk=4, hurt=1)),
    'zombie_shambler': (lambda a=None: zombie('zombie_shambler', a), dict(idle=2, walk=6, hurt=1)),
    'zombie_runner': (lambda a=None: zombie('zombie_runner', a), dict(idle=2, walk=6, hurt=1)),
    'zombie_bloater': (lambda a=None: zombie('zombie_bloater', a), dict(idle=2, walk=6, hurt=1)),
    'zombie_spitter': (lambda a=None: zombie('zombie_spitter', a), dict(idle=2, walk=6, hurt=1)),
    'zombie_brute': (lambda a=None: zombie('zombie_brute', a), dict(idle=2, walk=6, hurt=1)),
    'zombie_bat': (bat, dict(idle=4, air=4, hurt=1)),
    'pumpkin_king': (pumpkin_king, dict(idle=2, walk=6, hurt=1)),
}


# ---------------------------------------------------------------------------------------------------------------
# Collectibles (same 24x24 size as a Star Shard)
def gift():
    c = Canvas(24, 24)
    c.fill(c.mask_rect(3, 1, 20, 15, r=1), Ramp(C(220, 50, 60)))
    c.fill(c.mask_rect(10, 1, 13, 15), Ramp(GOLD))
    c.fill(c.mask_rect(2, 14, 21, 17, r=1), Ramp(C(200, 40, 50)))
    c.fill(c.mask_ellipse(8, 19, 4, 2.5), Ramp(GOLD))
    c.fill(c.mask_ellipse(15, 19, 4, 2.5), Ramp(GOLD))
    return finish(c)


def heart():
    c = Canvas(24, 24)
    _heart(c, 12, 12, 7, C(240, 60, 100))
    c.specular(8, 15)
    return finish(c)


def pie():
    c = Canvas(24, 24)
    c.fill(c.mask_ellipse(12, 9, 10, 6), Ramp(C(210, 150, 80)))
    c.fill(c.mask_ellipse(12, 11, 8, 4), Ramp(C(220, 110, 50)))
    for x in range(5, 20, 3):
        c.put(x, 13, C(250, 210, 150), lock=True)
    return finish(c)


def firework_star():
    c = Canvas(24, 24)
    pts = []
    for k in range(10):
        r_ = 10 if k % 2 == 0 else 4
        a = -math.pi / 2 + k * math.pi / 5
        pts.append((12 + math.cos(a) * r_, 12 - math.sin(a) * r_))
    c.fill(c.mask_poly(pts), Ramp(C(120, 220, 255)))
    c.specular(10, 15)
    return finish(c)


def candy():
    c = Canvas(24, 24)
    c.fill(c.mask_ellipse(12, 12, 6, 5), Ramp(C(255, 140, 40)))
    for sgn in (-1, 1):
        c.fill(c.mask_poly([(12 + sgn * 5, 12), (12 + sgn * 11, 17), (12 + sgn * 11, 7)]), Ramp(C(250, 230, 120)))
    for x in range(8, 17, 3):
        c.put(x, 12, C(255, 255, 255), lock=True)
    return finish(c)


COLLECTIBLES = {'gift': gift, 'heart': heart, 'pie': pie, 'firework_star': firework_star, 'candy': candy}


# ---------------------------------------------------------------------------------------------------------------
# Zombie mode props
def mystery_crate():
    c = Canvas(64, 40)
    c.fill(c.mask_rect(2, 0, 61, 30, r=2), Ramp(C(110, 70, 120)))
    for x in (4, 58):
        c.fill(c.mask_rect(x, 0, x + 2, 30), Ramp(GOLD))
    c.fill(c.mask_rect(2, 28, 61, 33, r=2), Ramp(C(80, 50, 90)))
    for k, x in enumerate((20, 32, 44)):
        c.fill(c.mask_rect(x - 4, 8, x + 4, 20, r=1), Ramp(C(255, 220, 120)))
        c.flat(c.mask_rect(x - 1, 11, x + 1, 13), C(80, 50, 90), lock=True)
        c.put(x, 17, C(80, 50, 90), lock=True)
    return finish(c)


PERKS = {
    'iron_heart': C(220, 60, 70), 'quick_draw': C(90, 200, 255), 'second_wind': C(120, 230, 120), 'fleet_feet': C(255, 200, 70),
    'deadeye': C(240, 240, 250), 'scavenger': C(200, 140, 60),
}


def perk_shrine(col):
    c = Canvas(32, 64)
    c.fill(c.mask_rect(3, 0, 28, 50, r=3), Ramp(shift(col, dl=-0.2)))
    c.fill(c.mask_rect(7, 18, 24, 44, r=2), Ramp(C(40, 40, 60)))
    c.fill(c.mask_ellipse(15.5, 31, 6, 9), Ramp(col))          # glowing bottle
    c.fill(c.mask_rect(13, 39, 18, 44), Ramp(col))
    c.specular(13, 34)
    c.fill(c.mask_rect(1, 50, 30, 56, r=2), Ramp(GOLD))
    c.fill(c.mask_rect(3, 2, 28, 10, r=1), Ramp(C(60, 56, 80)))
    return finish(c)


def forge():
    c = Canvas(64, 64)
    c.fill(c.mask_rect(4, 0, 59, 40, r=3), Ramp(C(80, 76, 96)))
    c.fill(c.mask_ellipse(32, 20, 16, 14), Ramp(C(40, 30, 50)))
    c.fill(c.mask_ellipse(32, 17, 11, 8), Ramp(C(170, 120, 255)))
    c.fill(c.mask_ellipse(32, 15, 6, 4), Ramp(C(230, 210, 255)))
    gear_at(c, 32, 52, 12, Ramp(GOLD), teeth=10)
    c.fill(c.mask_ellipse(32, 52, 5, 5), Ramp(C(170, 120, 255)))
    return finish(c)


def window_frame():
    c = Canvas(32, 64)
    c.fill(c.mask_rect(0, 0, 31, 63, r=1), Ramp(C(70, 60, 70)))
    c.fill(c.mask_rect(4, 4, 27, 59), Ramp(C(24, 20, 34)))
    for k in range(6):
        c.put(10 + k * 2, 50 - k * 3, C(110, 220, 120), lock=True)   # eyes glinting in the dark
    return finish(c, rim=False)


def board():
    c = Canvas(32, 8)
    c.fill(c.mask_rect(0, 0, 31, 6, r=1), Ramp(WOOD))
    for x in (3, 28):
        c.put(x, 3, C(200, 200, 210), lock=True)
    return finish(c, rim=False)


def paid_door():
    c = Canvas(32, 32)
    c.fill(c.mask_rect(1, 0, 30, 31), Ramp(C(90, 70, 80)))
    for (a, b) in ((0, 31), (31, 0)):
        for (x, y) in c.line_pts(a, 2, b, 29):
            c.put(x, y, C(160, 160, 180))
            c.put(x, y + 1, C(110, 110, 130))
    return finish(c, rim=False)


WEAPON_OUTLINES = {  # chalk outline on the wall for a wall weapon
    'pea_popper': [(6, 10, 30, 18), (10, 4, 16, 10)],
    'candy_repeater': [(4, 10, 44, 18), (12, 2, 18, 10), (30, 4, 34, 10)],
    'hex_scattergun': [(2, 11, 48, 19), (8, 3, 16, 11)],
    'ecto_sprayer': [(4, 9, 40, 19), (14, 1, 20, 9), (40, 12, 52, 16)],
}


def outline(name):
    c = Canvas(56, 24)
    for (x0, y0, x1, y1) in WEAPON_OUTLINES[name]:
        for x in range(x0, x1 + 1):
            c.put(x, y0, (240, 240, 230), a=200)
            c.put(x, y1, (240, 240, 230), a=200)
        for y in range(y0, y1 + 1):
            c.put(x0, y, (240, 240, 230), a=200)
            c.put(x1, y, (240, 240, 230), a=200)
    return c


POWERUPS = {
    'ammo': C(120, 200, 255), 'instakill': C(240, 70, 70), 'double': C(255, 210, 70), 'nuke': C(200, 140, 255), 'boardup': C(170, 120, 70),
}


def powerup(kind):
    col = POWERUPS[kind]
    c = Canvas(32, 32)
    c.fill(c.mask_ellipse(16, 16, 14, 14), Ramp(shift(col, dl=-0.1)))
    c.fill(c.mask_ellipse(16, 16, 11, 11), Ramp(col))
    sym = C(255, 255, 255)
    if kind == 'ammo':
        for x in (10, 15, 20):
            c.fill(c.mask_rect(x, 8, x + 2, 20), Ramp(sym))
            c.fill(c.mask_ellipse(x + 1, 21, 1.5, 2), Ramp(sym))
    elif kind == 'instakill':
        c.fill(c.mask_ellipse(16, 18, 7, 6), Ramp(sym))
        c.fill(c.mask_rect(12, 9, 20, 13), Ramp(sym))
        c.flat(c.mask_ellipse(13, 18, 1.6, 1.8), col, lock=True)
        c.flat(c.mask_ellipse(19, 18, 1.6, 1.8), col, lock=True)
    elif kind == 'double':
        for x in (11, 21):
            c.fill(c.mask_ellipse(x, 16, 4, 6), Ramp(sym))
    elif kind == 'nuke':
        for k in range(8):
            a = k * math.tau / 8
            c.fill(c.mask_capsule(16, 16, 16 + math.cos(a) * 9, 16 + math.sin(a) * 9, 1.4), Ramp(sym))
        c.fill(c.mask_ellipse(16, 16, 4, 4), Ramp(sym))
    else:
        for y in (11, 16, 21):
            c.fill(c.mask_rect(8, y - 1, 24, y + 1), Ramp(sym))
    return finish(c)


ZOMBIE_PROPS = dict(
    {'mystery_crate': mystery_crate, 'forge': forge, 'window_frame': window_frame, 'board': board, 'paid_door': paid_door,
     'jack_lantern': jack_lantern},
    **{f'perk_{k}': (lambda col=v: perk_shrine(col)) for k, v in PERKS.items()},
    **{f'wall_{k}': (lambda n=k: outline(n)) for k in WEAPON_OUTLINES},
    **{f'power_{k}': (lambda n=k: powerup(n)) for k in POWERUPS},
)

"""
Story World 4, the Starfall Observatory (all original): the astral tile set and night-sky backdrop with brass
telescope domes and orbiting rings, plus three foes: the Comet Wisp (flyer), the Lens Golem (walker) and the Star
Hopper (hopper).
"""
import math, random
from px import Canvas, Ramp, shift, mix, INK
from tiles import C, THEMES as TILE_THEMES
import bg
from bg import WrapCanvas, LW, ridge, shade_silhouette, clouds_layer
from props import finish, eye, legs, st, bob_of
from chars import GOLD, METAL

WHITE = C(255, 255, 255)

TILE_THEMES['astral'] = dict(surf=C(120, 110, 220), surf2=C(80, 70, 170), fill=C(44, 40, 90), fill2=C(32, 28, 70),
                             rock=C(100, 96, 170), stone=C(70, 66, 130), plank=C(210, 170, 90), trim=C(255, 220, 120),
                             glow=C(170, 200, 255), crystal=True, flowers=[C(255, 240, 160), C(170, 220, 255)])


def astral(seed=31):
    r = random.Random(seed)
    far = WrapCanvas(LW, 256)
    for _ in range(140):                                  # dense star field with a few bright ones
        x, y = r.randrange(LW), r.randrange(60, 256)
        far.put(x, y, WHITE if r.random() < 0.25 else C(190, 200, 255))
    for (cx, cy, rr, col) in ((120, 200, 46, C(120, 60, 160)), (430, 180, 60, C(50, 90, 170)), (560, 220, 30, C(160, 70, 130))):
        for p in far.mask_ellipse(cx, cy, rr, rr * 0.55):   # soft nebula clouds, thinning toward the edge
            d = math.hypot((p[0] - cx) / rr, (p[1] - cy) / (rr * 0.55))
            if far.get(*p) is None and r.random() < 0.7 * max(0.0, 1 - d) ** 1.5:
                far.put(*p, mix(col, C(20, 18, 50), d * 0.6))
    far.fill(far.mask_ellipse(300, 210, 18, 18), Ramp(C(250, 220, 170)))   # a ringed planet
    for k in range(-30, 31):
        x = 300 + k
        y = 210 + int(k * 0.18)
        if abs(k) > 17 or y < 206:
            far.put(x, y, C(255, 236, 190), lock=True)
            far.put(x, y - 1, C(220, 200, 160), lock=True)
    mid = WrapCanvas(LW, 224)
    m2 = ridge(LW, 60, 16, 0.5, seed + 2)
    shade_silhouette(mid, m2, C(54, 50, 110), C(90, 84, 160))
    for x in (90, 300, 520):                              # observatory domes with telescopes
        base = int(min(m2[(x + k) % LW] for k in range(-18, 19))) - 3
        mid.fill(mid.mask_rect(x - 18, base - 2, x + 18, base + 22), Ramp(C(70, 66, 120)), light=(-0.9, 0.2))
        mid.fill(mid.mask_ellipse(x, base + 22, 20, 15), Ramp(C(200, 170, 100)))
        mid.fill(mid.mask_rect(x - 2, base + 22, x + 2, base + 37), Ramp(C(40, 36, 70)))
        mid.fill(mid.mask_capsule(x + 2, base + 30, x + 18, base + 46, 3), Ramp(GOLD))
        for wx in (x - 10, x, x + 10):
            mid.put(wx, base + 10, C(255, 230, 140), lock=True)
    near = WrapCanvas(LW, 160)
    m3 = ridge(LW, 22, 6, 0.6, seed + 5)
    shade_silhouette(near, m3, C(40, 36, 80), C(70, 64, 130))
    for i in range(16):                                   # glowing crystals
        x = r.randrange(LW)
        y = int(m3[x])
        h = r.randint(6, 14)
        near.fill(near.mask_poly([(x - 3, y - 1), (x + 3, y - 1), (x, y + h)]), Ramp(C(150, 190, 255)))
    return dict(far=far, mid=mid, near=near, clouds=clouds_layer(seed, C(140, 120, 200), n=4))


bg.THEMES['astral'] = astral


def comet_wisp(a=None):
    """Flyer: a little comet with a sparkling tail and two big eyes."""
    kind, ph, i = st(a)
    c = Canvas(32, 24)
    dy = int(round(math.sin(ph) * 1.2)) if kind in ('air', 'idle') else 0
    for k in range(10):                                   # tail streams behind (left)
        t = k / 10
        c.fill(c.mask_ellipse(14 - k * 1.3, 12 + dy + math.sin(ph + k * 0.6) * (1 + t), 3.6 * (1 - t) + 0.6, 3 * (1 - t) + 0.5),
               Ramp(mix(C(120, 200, 255), C(200, 140, 255), t)), soft=True)
    c.fill(c.mask_ellipse(19, 12 + dy, 6, 6), Ramp(C(250, 240, 200)))
    eye(c, 20, 12 + dy, C(40, 40, 80), shut=kind == 'hurt')
    eye(c, 23, 12 + dy, C(40, 40, 80), shut=kind == 'hurt')
    c.specular(17, 15 + dy)
    return finish(c)


def lens_golem(a=None):
    """Walker: a stubby brass telescope on legs with one big glass eye."""
    kind, ph, i = st(a)
    c = Canvas(28, 30)
    b = bob_of(kind, ph)
    legs(c, (9, 18), 6 + b, C(90, 80, 70), ph if kind == 'walk' else None)
    c.fill(c.mask_rect(4, 6 + b, 23, 22 + b, r=4), Ramp(C(200, 160, 80)))
    for y in (10, 18):
        c.fill(c.mask_rect(4, y + b, 23, y + 1 + b), Ramp(C(150, 110, 50)), soft=True)
    c.fill(c.mask_ellipse(18, 15 + b, 5, 5), Ramp(C(80, 70, 120)))
    lens = C(140, 220, 255) if kind != 'hurt' else C(255, 120, 120)
    c.fill(c.mask_ellipse(18.5, 15.5 + b, 3.4, 3.4), Ramp(lens))
    c.specular(17, 17 + b)
    c.fill(c.mask_rect(10, 22 + b, 14, 26 + b), Ramp(METAL))
    c.fill(c.mask_ellipse(12, 27 + b, 2.2, 2.2), Ramp(C(255, 200, 90)))
    return finish(c)


def star_hopper(a=None):
    """Hopper: a five-pointed star with springy legs; squashes before it jumps."""
    kind, ph, i = st(a)
    c = Canvas(26, 28)
    squat = 2 if kind == 'idle' and i % 2 else 0
    cy = 14 - squat + (2 if kind == 'air' else 0)
    pts = []
    for k in range(10):
        r_ = 9 if k % 2 == 0 else 4
        ang = math.pi / 2 + k * math.pi / 5
        pts.append((13 + math.cos(ang) * r_, cy + math.sin(ang) * r_ * (0.85 if squat else 1)))
    c.fill(c.mask_poly(pts), Ramp(C(255, 220, 90)))
    eye(c, 11, cy, INK, shut=kind == 'hurt')
    eye(c, 15, cy, INK, shut=kind == 'hurt')
    if kind != 'air':
        legs(c, (10, 16), cy - 6, C(200, 160, 70))
    return finish(c)


WORLD4_ENEMIES = {
    'comet_wisp': (comet_wisp, dict(idle=4, air=4, hurt=1)),
    'lens_golem': (lens_golem, dict(idle=2, walk=4, hurt=1)),
    'star_hopper': (star_hopper, dict(idle=2, air=2, hurt=1)),
}

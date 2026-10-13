"""
Original fighters for Pixel Kingdom Rumble: a small 2D rig (IK limbs, chibi proportions) drawn with
hue-shifted ramps, dark separation lines and a tinted outline. Every fighter holds a signature weapon.

Frames are 64x48 (feet at bottom-centre, faces right) so weapons can reach past the body;
the menu/idle sprite is a tight 32x48 crop of idle frame 0.
"""
import math
from px import Canvas, Ramp, shift, mix, INK, hex2rgb

W, H = 80, 48
OX = 40  # feet centre x


def C(r, g, b):
    return (r, g, b)


# ---- builds (y-up, relative to feet) -------------------------------------------------------------
BUILDS = {
    'normal': dict(hip=12, top=24, tw=5.0, sh=5.0, hcy=32.5, hrx=7.6, hry=7.0, leg=2.3, arm=1.8, hand=2.0, foot=3),
    'slim':   dict(hip=12, top=24, tw=4.2, sh=4.4, hcy=32.5, hrx=7.4, hry=7.0, leg=2.0, arm=1.6, hand=1.8, foot=3),
    'bulky':  dict(hip=12, top=26, tw=7.2, sh=7.0, hcy=34.0, hrx=7.6, hry=6.8, leg=2.9, arm=2.6, hand=2.6, foot=4),
    'small':  dict(hip=8, top=18, tw=4.6, sh=4.6, hcy=26.0, hrx=7.4, hry=6.6, leg=1.9, arm=1.6, hand=1.8, foot=3),
}


# ---- poses ---------------------------------------------------------------------------------------
# Each pose: body offset (bx,by), lean (px), legs [(fx,fy) back, front] relative to feet centre,
# hands [(hx,hy) back, front] relative to the shoulder, weapon angle (deg, 0=forward/right, 90=up),
# eyes, flutter phase, glow (special), smear (attack arc)
def P(bx=0, by=0, lean=0, legs=((-3, 0), (3, 0)), hands=((-2, -7), (3, -7)), wang=60, eyes='open',
      flutter=0, glow=0, smear=None, knee=1.0, hold='front'):
    return dict(bx=bx, by=by, lean=lean, legs=legs, hands=hands, wang=wang, eyes=eyes, flutter=flutter,
                glow=glow, smear=smear, knee=knee, hold=hold)


def anim_poses(build):
    small = build == 'small'
    k = 0.75 if small else 1.0
    def L(x, y):
        return (round(x * k), round(y * k))
    poses = {}
    poses['idle'] = [
        P(by=0, hands=((-2, -7), (3, -7)), wang=62),
        P(by=0, hands=((-2, -7), (3, -7)), wang=62, flutter=1),
        P(by=-1, hands=((-2, -7), (3, -8)), wang=60, flutter=2),
        P(by=-1, hands=((-2, -7), (3, -8)), wang=60, eyes='blink', flutter=3),
    ]
    # 6-frame run: contact, down, pass, up ... mirrored
    run_legs = [((-6, 1), (6, 0)), ((-4, 3), (4, 0)), ((-1, 5), (1, 1)), ((6, 0), (-6, 1)), ((4, 0), (-4, 3)), ((1, 1), (-1, 5))]
    run_hands = [((4, -5), (-3, -6)), ((2, -6), (-1, -7)), ((0, -7), (1, -7)), ((-3, -6), (4, -5)), ((-1, -7), (2, -6)), ((1, -7), (0, -7))]
    run_by = [0, -1, 1, 0, -1, 1]
    poses['run'] = [P(lean=1, by=run_by[i], legs=tuple(L(*f) for f in run_legs[i]), hands=run_hands[i], wang=30 + (i % 3) * 6,
                      flutter=i) for i in range(6)]
    poses['jump'] = [P(by=2, legs=(L(-4, 4), L(3, 6)), hands=((-4, -2), (4, 0)), wang=75, flutter=2, knee=1.3)]
    poses['fall'] = [P(by=1, legs=(L(-4, 1), L(4, 2)), hands=((-5, 1), (5, 2)), wang=100, flutter=4)]
    poses['attack'] = [
        P(bx=-1, lean=-1, legs=((-4, 0), (4, 0)), hands=((-3, -6), (-5, -2)), wang=135, eyes='angry'),
        P(bx=2, lean=2, legs=((-6, 0), (7, 0)), hands=((-4, -7), (8, -2)), wang=0, eyes='angry', smear='swing', flutter=3),
        P(bx=1, lean=1, legs=((-5, 0), (6, 0)), hands=((-3, -7), (6, -6)), wang=-30, eyes='angry', flutter=4),
    ]
    poses['special'] = [
        P(by=1, legs=((-4, 0), (4, 0)), hands=((-4, -3), (2, 4)), wang=95, eyes='angry', glow=1, flutter=1),
        P(bx=1, lean=1, legs=((-5, 0), (6, 0)), hands=((-3, -5), (8, 0)), wang=10, eyes='angry', glow=2, smear='burst', flutter=3),
    ]
    poses['hurt'] = [P(bx=-2, lean=-2, legs=((-5, 0), (2, 1)), hands=((-6, 2), (-1, 3)), wang=140, eyes='hurt', flutter=5)]
    return poses


# ---- helpers -------------------------------------------------------------------------------------
def ik(a, b, l1, l2, bend):
    """two-bone IK: returns joint between a and b; bend=+1 bends toward +x"""
    dx, dy = b[0] - a[0], b[1] - a[1]
    d = max(0.01, math.hypot(dx, dy))
    d = min(d, l1 + l2 - 0.01)
    x = (l1 * l1 - l2 * l2 + d * d) / (2 * d)
    h = math.sqrt(max(0.0, l1 * l1 - x * x))
    ux, uy = dx / d, dy / d
    px, py = -uy, ux
    if px * bend < 0:
        px, py = -px, -py
    return (a[0] + ux * x + px * h, a[1] + uy * x + py * h)


def rot(v, deg):
    r = math.radians(deg)
    return (v[0] * math.cos(r) - v[1] * math.sin(r), v[0] * math.sin(r) + v[1] * math.cos(r))


# ---- weapons -------------------------------------------------------------------------------------
METAL = C(196, 206, 224)
GOLD = C(246, 196, 72)
WOOD = C(160, 102, 58)
DARKWOOD = C(110, 70, 44)
LEATHER = C(120, 72, 48)


def draw_weapon(c, grip, ang, w, glow, scale=1.0):
    """Draws the weapon from the grip point along angle `ang`. Returns tip point."""
    t = w['type']
    d = (math.cos(math.radians(ang)), math.sin(math.radians(ang)))
    def at(s):
        return (grip[0] + d[0] * s * scale, grip[1] + d[1] * s * scale)
    col = w.get('color', METAL)
    acc = w.get('accent', GOLD)
    gcol = w.get('glow', (255, 240, 160))
    tip = at(10)
    if t == 'baton':   # Nova: comet baton, short rod with a star tip
        a, b = at(-3), at(10)
        c.fill(c.mask_capsule(*a, *b, 1.1), Ramp(col))
        s = at(12)
        c.fill(c.mask_ellipse(s[0], s[1], 2.6 + glow * 0.6, 2.6 + glow * 0.6), Ramp(acc))
        c.specular(int(s[0]) - 1, int(s[1]) + 1)
        tip = s
    elif t == 'spear':
        a, b = at(-8), at(17)
        c.fill(c.mask_capsule(*a, *b, 1.0), Ramp(col))
        h1, h2 = at(17), at(24)
        n = rot(d, 90)
        pts = [(h1[0] + n[0] * 2.6, h1[1] + n[1] * 2.6), (h2[0], h2[1]), (h1[0] - n[0] * 2.6, h1[1] - n[1] * 2.6)]
        c.fill(c.mask_poly(pts), Ramp(acc))
        g = at(16)
        c.fill(c.mask_ellipse(g[0], g[1], 1.6, 1.6), Ramp(C(255, 120, 60)))
        tip = h2
    elif t in ('hammer', 'mallet'):
        a, b = at(-3), at(12)
        c.fill(c.mask_capsule(*a, *b, 1.1), Ramp(w.get('handle', WOOD)))
        h = at(13)
        n = rot(d, 90)
        hw, hl = (4.2, 3.6) if t == 'hammer' else (4.8, 4.2)
        pts = [(h[0] + n[0] * hw + d[0] * hl, h[1] + n[1] * hw + d[1] * hl),
               (h[0] - n[0] * hw + d[0] * hl, h[1] - n[1] * hw + d[1] * hl),
               (h[0] - n[0] * hw - d[0] * hl * 0.6, h[1] - n[1] * hw - d[1] * hl * 0.6),
               (h[0] + n[0] * hw - d[0] * hl * 0.6, h[1] + n[1] * hw - d[1] * hl * 0.6)]
        c.fill(c.mask_poly(pts), Ramp(col))
        band = at(13)
        c.flat(c.mask_ellipse(band[0], band[1], 1.0, 1.0), acc)
        tip = at(16)
    elif t == 'axe':
        a, b = at(-4), at(15)
        c.fill(c.mask_capsule(*a, *b, 1.2), Ramp(w.get('handle', C(200, 40, 40))))
        h = at(13)
        n = rot(d, -90)
        pts = [(h[0] - d[0] * 2.5, h[1] - d[1] * 2.5),
               (h[0] + n[0] * 5 - d[0] * 4.5, h[1] + n[1] * 5 - d[1] * 4.5),
               (h[0] + n[0] * 8 - d[0] * 3.5, h[1] + n[1] * 8 - d[1] * 3.5),
               (h[0] + n[0] * 8.5 + d[0] * 2.5, h[1] + n[1] * 8.5 + d[1] * 2.5),
               (h[0] + n[0] * 5 + d[0] * 4.5, h[1] + n[1] * 5 + d[1] * 4.5),
               (h[0] + d[0] * 2.5, h[1] + d[1] * 2.5)]
        c.fill(c.mask_poly(pts), Ramp(col))
        # bright cutting edge
        e0 = (h[0] + n[0] * 8 - d[0] * 3, h[1] + n[1] * 8 - d[1] * 3)
        e1 = (h[0] + n[0] * 8.3 + d[0] * 2, h[1] + n[1] * 8.3 + d[1] * 2)
        for (x, y) in c.line_pts(*e0, *e1):
            c.put(x, y, C(255, 255, 255), lock=True)
        # spike on the back
        c.fill(c.mask_poly([(h[0] - n[0] * 1, h[1] - n[1] * 1), (h[0] - n[0] * 5, h[1] - n[1] * 5), (h[0] - n[0] * 1 + d[0] * 2, h[1] - n[1] * 1 + d[1] * 2)]), Ramp(col))
        tip = (h[0] + n[0] * 8, h[1] + n[1] * 8)
    elif t == 'wrench':
        a, b = at(-3), at(11)
        c.fill(c.mask_capsule(*a, *b, 1.3), Ramp(col))
        h = at(13)
        c.fill(c.mask_ellipse(h[0], h[1], 3.4, 3.4), Ramp(col))
        j = at(15)
        c.flat(c.mask_ellipse(j[0], j[1], 1.4, 1.6), (0, 0, 0, 0))
        for (x, y) in c.mask_ellipse(j[0], j[1], 1.4, 1.6):
            if c.inside(x, y):
                c.px[y][x] = None
        g = at(4)
        c.fill(c.mask_capsule(*at(1), *g, 1.6), Ramp(acc))
        tip = h
    elif t == 'daggers':
        a, b = at(1), at(11)
        n = rot(d, 90)
        pts = [(a[0] + n[0] * 1.8, a[1] + n[1] * 1.8), (b[0], b[1]), (a[0] - n[0] * 1.8, a[1] - n[1] * 1.8)]
        c.fill(c.mask_poly(pts), Ramp(col))
        gpts = c.mask_capsule(grip[0] - n[0] * 2, grip[1] - n[1] * 2, grip[0] + n[0] * 2, grip[1] + n[1] * 2, 0.7)
        c.flat(gpts, acc)
        tip = b
    elif t == 'fan':
        n = rot(d, 90)
        pts = [grip]
        for i in range(9):
            a_ = ang - 70 + i * 17.5
            v = (math.cos(math.radians(a_)), math.sin(math.radians(a_)))
            pts.append((grip[0] + v[0] * 8 * scale, grip[1] + v[1] * 8 * scale))
        part = c.fill(c.mask_poly(pts), Ramp(col), soft=True)
        for i in range(0, 9, 2):
            a_ = ang - 70 + i * 17.5
            v = (math.cos(math.radians(a_)), math.sin(math.radians(a_)))
            for (x, y) in c.line_pts(grip[0], grip[1], grip[0] + v[0] * 7 * scale, grip[1] + v[1] * 7 * scale):
                c.put(x, y, shift(col, dl=-0.2), part)
        edge = []
        for i in range(9):
            a_ = ang - 70 + i * 17.5
            v = (math.cos(math.radians(a_)), math.sin(math.radians(a_)))
            edge.append((grip[0] + v[0] * 7.6 * scale, grip[1] + v[1] * 7.6 * scale))
        for i in range(len(edge) - 1):
            for (x, y) in c.line_pts(*edge[i], *edge[i + 1]):
                c.put(x, y, acc, part)
        tip = at(8)
    elif t == 'staff':
        a, b = at(-9), at(14)
        c.fill(c.mask_capsule(*a, *b, 1.0), Ramp(w.get('handle', C(90, 100, 170))))
        h = at(17)
        n = rot(d, 90)
        pts = [(h[0] - d[0] * 4, h[1] - d[1] * 4), (h[0] + n[0] * 2.8, h[1] + n[1] * 2.8), (h[0] + d[0] * 5, h[1] + d[1] * 5),
               (h[0] - n[0] * 2.8, h[1] - n[1] * 2.8)]
        c.fill(c.mask_poly(pts), Ramp(col))
        c.specular(int(h[0]), int(h[1] + 1))
        tip = h
    elif t == 'blaster':
        a, b = at(-2), at(9)
        c.fill(c.mask_capsule(*a, *b, 2.6), Ramp(col))
        m = at(10)
        c.fill(c.mask_ellipse(m[0], m[1], 2.0, 2.0), Ramp(acc))
        c.specular(int(m[0]), int(m[1]))
        tip = m
    elif t == 'ball':
        tip = grip
    elif t == 'lantern':
        for (x, y) in c.line_pts(grip[0], grip[1], grip[0] + 1, grip[1] - 3):
            c.put(x, y, C(60, 50, 50))
        lx, ly = grip[0] + 1, grip[1] - 6
        c.fill(c.mask_rect(lx - 2.5, ly - 3, lx + 2.5, ly + 3, r=1), Ramp(acc))
        c.flat(c.mask_rect(lx - 1.5, ly - 2, lx + 1.5, ly + 2), C(255, 250, 200), lock=True)
        c.fill(c.mask_rect(lx - 2.5, ly + 2, lx + 2.5, ly + 3), Ramp(col), soft=True)
        tip = (lx, ly)
    elif t == 'board':
        n = rot(d, 90)
        a, b = at(-6), at(8)
        c.fill(c.mask_capsule(*a, *b, 1.3), Ramp(col))
        for s in (-4, 6):
            q = at(s)
            wp = (q[0] - n[0] * 2, q[1] - n[1] * 2)
            c.fill(c.mask_ellipse(wp[0], wp[1], 1.3, 1.3), Ramp(acc))
        tip = b
    elif t == 'stick':
        a, b = at(-4), at(13)
        c.fill(c.mask_capsule(*a, *b, 0.9), Ramp(col))
        n = rot(d, -90)
        h = at(13)
        e = (h[0] + n[0] * 4 + d[0] * 1, h[1] + n[1] * 4 + d[1] * 1)
        c.fill(c.mask_capsule(h[0], h[1], e[0], e[1], 1.3), Ramp(acc))
        tip = e
    return tip


def draw_smear(c, origin, kind, color, reach=22):
    """Attack arc / special burst drawn as a bright crescent with a soft trailing edge."""
    ox, oy = origin
    if kind == 'swing':
        # crescent: bright leading edge, fading tail toward the body
        for a in range(-55, 71, 2):
            fa = 1 - abs(a - 8) / 72.0
            th = max(1, int(round(4 * fa)))
            for k in range(th):
                r = reach - k
                x = ox + math.cos(math.radians(a)) * r * 0.9
                y = oy + math.sin(math.radians(a)) * r * 0.66
                if not c.inside(int(x), int(y)) or c.get(int(x), int(y)) is not None:
                    continue
                t = 1 - k / 4.0
                col = mix(color, (255, 255, 255), 0.45 * t)
                alpha = int(185 + 70 * t * fa)
                c.put(x, y, col + (alpha,), part=c.new_part(), lock=True)
    elif kind == 'burst':
        for r in range(2, 9):
            for a in range(0, 360, 12):
                if (a // 12 + r) % 3 == 0:
                    continue
                x = ox + math.cos(math.radians(a)) * r
                y = oy + math.sin(math.radians(a)) * r
                if c.inside(int(x), int(y)) and c.get(int(x), int(y)) is None:
                    t = 1 - r / 9
                    c.put(x, y, mix(color, (255, 255, 255), t * 0.8) + (int(120 + 135 * t),), lock=True)


# ---- heads ---------------------------------------------------------------------------------------
def draw_face(c, cx, cy, spec, eyes, rx, ry):
    skin = Ramp(spec['skin'])
    eye = spec.get('eye') or INK
    ex = int(round(cx + rx * 0.38))
    ey = int(round(cy - ry * 0.05))
    ex2 = int(round(cx - rx * 0.12))
    white = C(250, 250, 255)
    kind = spec.get('face', 'human')
    if kind == 'robot':
        # visor band
        pts = c.mask_rect(cx - rx * 0.55, ey - 1, cx + rx * 0.95, ey + 2, r=1)
        c.flat(pts, C(30, 40, 60), lock=True)
        for x in range(int(cx - rx * 0.4), int(cx + rx * 0.85)):
            if eyes in ('blink', 'hurt'):
                if x % 2 == 0:
                    c.put(x, ey, eye, lock=True)
            elif x in (ex, ex + 1, ex2, ex2 + 1):
                c.put(x, ey, eye, lock=True)
                c.put(x, ey + 1, eye, lock=True)
        return
    if eyes == 'blink':
        for x in (ex - 1, ex, ex2):
            c.put(x, ey, shift(spec['skin'], dl=-0.35), lock=True)
    elif eyes == 'hurt':
        for (x, y) in ((ex - 1, ey + 1), (ex, ey), (ex - 1, ey - 1)):
            c.put(x, y, INK, lock=True)
        for (x, y) in ((ex2 + 1, ey + 1), (ex2, ey), (ex2 + 1, ey - 1)):
            c.put(x, y, INK, lock=True)
    else:
        # big readable eyes: 2x3 with a glint; far eye slimmer (3/4 view)
        for y in range(ey - 1, ey + 2):
            c.put(ex, y, eye, lock=True)
            c.put(ex - 1, y, eye if y < ey + 1 else shift(eye, dl=0.12), lock=True)
            c.put(ex2, y, eye, lock=True)
        c.put(ex, ey + 1, white, lock=True)
        c.put(ex2, ey + 1, white, lock=True)
        if eyes == 'angry':
            c.put(ex - 1, ey + 2, INK, lock=True)
            c.put(ex, ey + 2, INK, lock=True)
            c.put(ex + 1, ey + 3, INK, lock=True)
            c.put(ex2, ey + 2, INK, lock=True)
            c.put(ex2 - 1, ey + 3, INK, lock=True)
    # blush + mouth
    if kind == 'human':
        c.put(ex + 1, ey - 2, mix(spec['skin'], C(255, 110, 110), 0.45), lock=True)
        mouth_y = ey - 3
        c.put(ex - 1, mouth_y, shift(spec['skin'], dl=-0.3), lock=True)
        if eyes == 'angry':
            c.put(ex - 2, mouth_y, shift(spec['skin'], dl=-0.3), lock=True)


def draw_head(c, cx, cy, spec, eyes, b, flutter):
    rx, ry = b['hrx'], b['hry']
    hair = spec['hair']
    kind = spec['head']
    face = spec.get('face', 'human')
    # behind-head pieces
    if kind == 'hood':
        c.fill(c.mask_ellipse(cx - 1, cy + 0.5, rx + 1.5, ry + 1.5), Ramp(spec['top']))
    if kind == 'ears':
        for ex in (cx - rx * 0.6, cx + rx * 0.35):
            c.fill(c.mask_ellipse(ex, cy + ry * 0.85, 2.6, 2.6), Ramp(hair))
            c.flat(c.mask_ellipse(ex, cy + ry * 0.85, 1.2, 1.2), shift(hair, dl=0.15))
    if kind == 'hair' and spec.get('long'):
        sway = [0, 1, 1, 0, -1, 0][flutter % 6]
        c.fill(c.mask_poly([(cx - rx * 0.2, cy + ry * 0.8), (cx - rx - 2 + sway, cy - ry - 3), (cx - rx * 0.9, cy - ry * 0.2),
                            (cx + rx * 0.3, cy + ry * 0.2)]), Ramp(hair))
    if face == 'lizard':
        # snout
        c.fill(c.mask_ellipse(cx + rx * 0.55, cy - ry * 0.25, rx * 0.7, ry * 0.55), Ramp(spec['skin']))
    head_part = c.fill(c.mask_ellipse(cx, cy, rx, ry), Ramp(spec['skin']))
    if face == 'bear':
        c.fill(c.mask_ellipse(cx + rx * 0.55, cy - ry * 0.35, rx * 0.45, ry * 0.38), Ramp(spec['accent']))
        c.put(int(cx + rx * 0.85), int(cy - ry * 0.15), INK, lock=True)
        c.put(int(cx + rx * 0.85) - 1, int(cy - ry * 0.15), INK, lock=True)
    if face == 'golem':
        for (x, y) in ((cx - 3, cy + 2), (cx + 1, cy - 4), (cx - 5, cy - 2)):
            c.put(x, y, shift(spec['skin'], dl=-0.2))
            c.put(x + 1, y, shift(spec['skin'], dl=-0.2))
    draw_face(c, cx, cy, spec, eyes, rx, ry)
    if face == 'lizard':
        c.put(int(cx + rx * 1.1), int(cy - ry * 0.1), shift(spec['skin'], dl=-0.35), lock=True)
    # top-of-head pieces
    top = cy + ry
    if kind == 'hair':
        pts = c.mask_ellipse(cx - 0.8, cy + ry * 0.45, rx + 0.6, ry * 0.75)
        pts = [p for p in pts if p[1] >= cy + ry * 0.05 or p[0] < cx - rx * 0.45]
        c.fill(pts, Ramp(hair))
        # fringe spikes over the forehead
        for i, fx in enumerate((cx + 1, cx + 3.5, cx + 6)):
            c.fill(c.mask_poly([(fx - 2, cy + ry * 0.6), (fx + 1.5, cy + ry * 0.6), (fx + 0.5, cy + ry * 0.05 - (i % 2))]), Ramp(hair))
    elif kind == 'spiky':
        base = c.mask_ellipse(cx - 0.5, cy + ry * 0.45, rx + 0.5, ry * 0.7)
        c.fill([p for p in base if p[1] >= cy + ry * 0.1], Ramp(hair))
        for i in range(5):
            bx = cx - rx + 1 + i * 3.4
            c.fill(c.mask_poly([(bx - 2, top - 1), (bx + 2, top - 1), (bx - 1 + (i % 2), top + 3 + (i % 2))]), Ramp(hair))
    elif kind == 'mohawk':
        for i in range(4):
            bx = cx - 4 + i * 2.6
            c.fill(c.mask_poly([(bx - 1.6, top - 1.5), (bx + 1.6, top - 1.5), (bx - 0.8, top + 4 - abs(i - 1.5))]), Ramp(hair))
        side = c.mask_ellipse(cx - 1, cy + ry * 0.3, rx * 0.85, ry * 0.5)
        for (x, y) in side:
            if c.get(x, y) and y > cy + ry * 0.2 and (x + y) % 3 == 0:
                c.put(x, y, shift(spec['skin'], dl=-0.12))
    elif kind == 'band':
        pts = c.mask_ellipse(cx - 0.6, cy + ry * 0.45, rx + 0.5, ry * 0.7)
        c.fill([p for p in pts if p[1] >= cy + ry * 0.15], Ramp(hair))
        band = [p for p in c.mask_ellipse(cx, cy, rx + 0.6, ry + 0.6) if int(cy + ry * 0.2) <= p[1] <= int(cy + ry * 0.2) + 1]
        c.fill(band, Ramp(spec['accent']), soft=True)
        sway = [0, 1, 2, 1, 0, -1][flutter % 6]
        tail = c.mask_poly([(cx - rx, cy + ry * 0.3), (cx - rx - 5, cy + ry * 0.2 - sway), (cx - rx - 4, cy - 1 - sway), (cx - rx + 0.5, cy + ry * 0.1)])
        c.fill(tail, Ramp(spec['accent']), soft=True)
    elif kind == 'hood':
        pts = c.mask_ellipse(cx - 0.5, cy + 1, rx + 1.2, ry + 1.2)
        pts = [p for p in pts if p[1] >= cy + ry * 0.35 or p[0] < cx - rx * 0.35]
        c.fill(pts, Ramp(spec['top']))
        c.fill(c.mask_poly([(cx - rx - 1, cy + 2), (cx - rx - 4, cy - 3), (cx - rx + 1, cy - 2)]), Ramp(spec['top']))
    elif kind == 'helmet':
        pts = c.mask_ellipse(cx - 0.3, cy + ry * 0.35, rx + 1.2, ry * 0.85)
        pts = [p for p in pts if p[1] >= cy + ry * 0.05 or p[0] < cx - rx * 0.5]
        c.fill(pts, Ramp(spec.get('helm', METAL)))
        rimpts = [p for p in pts if int(cy + ry * 0.05) <= p[1] <= int(cy + ry * 0.05) + 1]
        c.fill(rimpts, Ramp(spec['accent']), soft=True)
        if spec.get('plume'):
            sway = [0, 1, 1, 0, -1, -1][flutter % 6]
            c.fill(c.mask_poly([(cx - 1, top + 0.5), (cx + 2, top + 1), (cx - 6 + sway, top + 5), (cx - 7 + sway, top + 2)]), Ramp(spec['plume']))
    elif kind == 'cap':
        pts = c.mask_ellipse(cx, cy + ry * 0.4, rx + 0.8, ry * 0.7)
        c.fill([p for p in pts if p[1] >= cy + ry * 0.2], Ramp(spec['accent2'] if spec.get('accent2') else spec['top']))
        c.fill(c.mask_rect(cx + rx * 0.3, cy + ry * 0.15, cx + rx + 4, cy + ry * 0.15 + 1), Ramp(spec['accent2'] if spec.get('accent2') else spec['top']), soft=True)
        c.fill(c.mask_poly([(cx - rx, cy + 1), (cx - rx - 2, cy - 2), (cx - rx + 1, cy - 1)]), Ramp(hair))
    elif kind == 'ears':
        tuft = c.mask_ellipse(cx - 0.5, cy + ry * 0.7, rx * 0.7, ry * 0.35)
        c.fill(tuft, Ramp(hair))
    elif kind == 'bald':
        if spec.get('antenna'):
            ax = cx - 1
            for y in range(int(top - 1), int(top + 4)):
                c.put(ax, y, METAL)
            c.fill(c.mask_ellipse(ax, top + 4.5, 1.6, 1.6), Ramp(spec['accent']))
            c.specular(int(ax), int(top + 5))
    elif kind == 'goggles':
        pts = c.mask_ellipse(cx - 0.6, cy + ry * 0.45, rx + 0.6, ry * 0.72)
        c.fill([p for p in pts if p[1] >= cy + ry * 0.1], Ramp(hair))
        for i, gx in enumerate((cx - 1.5, cx + 3.5)):
            c.fill(c.mask_ellipse(gx, cy + ry * 0.65, 2.2, 1.9), Ramp(C(120, 90, 60)))
            c.flat(c.mask_ellipse(gx + 0.3, cy + ry * 0.65, 1.2, 1.0), C(140, 230, 255), lock=True)
    elif kind == 'crown':  # moss crown for the golem
        for i in range(4):
            bx = cx - 5 + i * 3.2
            c.fill(c.mask_ellipse(bx, top - 0.5, 2.2, 1.8), Ramp(hair))
    elif kind == 'courier':  # Nova: swept hair + aviator cap with comet pin
        pts = c.mask_ellipse(cx - 0.4, cy + ry * 0.45, rx + 0.8, ry * 0.75)
        c.fill([p for p in pts if p[1] >= cy + ry * 0.2], Ramp(spec['accent2']))
        c.fill(c.mask_poly([(cx - rx - 0.5, cy + ry * 0.4), (cx - rx - 4, cy - 2), (cx - rx + 1, cy - 1)]), Ramp(hair))
        c.fill(c.mask_poly([(cx + rx * 0.2, cy + ry * 0.35), (cx + rx + 1.5, cy + ry * 0.1), (cx + rx * 0.6, cy + ry * 0.5)]), Ramp(hair))
        c.fill(c.mask_ellipse(cx + 2, cy + ry * 0.75, 1.3, 1.3), Ramp(GOLD))
        c.specular(int(cx + 2), int(cy + ry * 0.75) + 1)


# ---- body ----------------------------------------------------------------------------------------
def draw_fighter(spec, pose, w=W, h=H, ox=OX):
    b = BUILDS[spec['build']]
    c = Canvas(w, h)
    bx, by, lean = pose['bx'], pose['by'] + 1, pose['lean']
    hipY = b['hip'] + by
    topY = b['top'] + by
    cxB = ox + bx
    sh = b['sh']
    shoulderY = topY - 2
    S_back = (cxB + lean - sh * 0.55, shoulderY)
    S_front = (cxB + lean + sh * 0.55, shoulderY)
    flutter = pose['flutter']
    weapon = spec['weapon']
    glow = pose['glow']

    leg_len = b['hip'] - 1
    l1 = l2 = leg_len / 2 + 0.6
    arm_l = (topY - hipY) * 0.48 + 1.2

    pants = spec['pants']
    shoes = spec['shoes']
    top = spec['top']

    # -- back pieces: cape / tail / wings
    extra = spec.get('extra')
    if extra == 'cape':
        sway = [0, 1, 2, 1, 0, -1][flutter % 6]
        pts = [(S_back[0] - 1, shoulderY + 1), (S_front[0] - 1, shoulderY + 1), (cxB - 3 - sway, hipY - 7),
               (cxB - 9 - sway * 1.5, hipY - 5)]
        c.fill(c.mask_poly(pts), Ramp(spec['accent']), light=(-0.8, 0.4))
    if extra == 'tail':
        sway = [0, 1, 1, 0, -1, -1][flutter % 6]
        tx = cxB - b['tw'] - 1
        pts = c.mask_capsule(tx + 2, hipY + 1, tx - 5, hipY + 3 + sway, 2.6 if spec['build'] == 'bulky' else 1.8)
        c.fill(pts, Ramp(spec.get('tail', spec['hair'])))
    if extra == 'lizardtail':
        sway = [0, 1, 1, 0, -1, -1][flutter % 6]
        tx = cxB - b['tw']
        c.fill(c.mask_capsule(tx + 2, hipY + 1, tx - 4, hipY - 1 + sway, 2.4), Ramp(spec['skin']))
        c.fill(c.mask_capsule(tx - 4, hipY - 1 + sway, tx - 10, hipY - 4 + sway * 2, 1.4), Ramp(spec['skin']))
    if extra == 'pack':  # firefighter water tank
        c.fill(c.mask_rect(cxB - b['tw'] - 4, hipY + 2, cxB - b['tw'] + 1, topY - 1, r=2), Ramp(C(200, 60, 50)))
    if extra == 'satchel':
        c.fill(c.mask_rect(cxB - b['tw'] - 3, hipY - 1, cxB - b['tw'] + 2, hipY + 4, r=1), Ramp(LEATHER))

    # -- back arm (behind the body)
    hb = pose['hands'][0]
    H_back = (S_back[0] + hb[0], S_back[1] + hb[1])
    E_back = ik(S_back, H_back, arm_l * 0.55, arm_l * 0.55, -1)
    sleeve = Ramp(shift(spec.get('sleeve', top), dl=-0.08))
    c.fill(c.mask_capsule(*S_back, *E_back, b['arm']), sleeve)
    c.fill(c.mask_capsule(*E_back, *H_back, b['arm'] * 0.9), Ramp(shift(spec.get('forearm', spec.get('sleeve', top)), dl=-0.08)))
    hand_col = spec.get('glove', spec['skin'])
    hr = b['hand'] * (1.45 if weapon['type'] == 'gloves' else 1.0) * (1.7 if weapon['type'] == 'fists' else 1.0)
    c.fill(c.mask_ellipse(H_back[0], H_back[1], hr, hr), Ramp(shift(hand_col, dl=-0.08)))
    if weapon['type'] == 'daggers' or weapon.get('twin'):
        draw_weapon(c, H_back, pose['wang'] - 25, weapon, glow, 0.85)

    # -- legs (back then front)
    for i, (fx, fy) in enumerate(pose['legs']):
        hip = (cxB + (-1.6 if i == 0 else 1.6), hipY)
        foot = (ox + fx + bx * 0.5, fy + 3)
        knee = ik(hip, foot, l1, l2, 1)
        knee = (knee[0] + (pose['knee'] - 1) * 2, knee[1])
        ramp = Ramp(shift(pants, dl=-0.1) if i == 0 else pants)
        c.fill(c.mask_capsule(*hip, *knee, b['leg']), ramp)
        c.fill(c.mask_capsule(*knee, foot[0], foot[1] + 0.5, b['leg'] * 0.9), ramp)
        sh_ramp = Ramp(shift(shoes, dl=-0.1) if i == 0 else shoes)
        f0 = foot[0] - 1.5
        fl = b['foot'] + 1
        if spec.get('skates'):
            c.fill(c.mask_rect(f0 - 1, foot[1] - 1, f0 + fl + 1, foot[1] + 2, r=1), sh_ramp)
            for wx in (f0, f0 + fl):
                c.fill(c.mask_ellipse(wx, foot[1] - 2, 1.3, 1.3), Ramp(C(250, 200, 60)))
        else:
            c.fill(c.mask_rect(f0 - 1, foot[1] - 2, f0 + fl, foot[1] + 1, r=1), sh_ramp)
            c.flat([(x, int(foot[1] - 2)) for x in range(int(f0 - 1), int(f0 + fl) + 1)], shift(shoes, dl=-0.25))

    # -- torso
    tw = b['tw']
    lean_top = lean
    pts = [(cxB - tw + 0.5, hipY - 1), (cxB + tw + 0.5, hipY - 1), (cxB + tw + lean_top + 0.6, topY), (cxB - tw + lean_top + 0.3, topY)]
    torso = c.mask_poly(pts)
    # round the shoulders
    torso += c.mask_ellipse(cxB + lean_top + 0.5, topY - 1, tw + 0.3, 2.4)
    c.fill(torso, Ramp(top))
    # belt + chest details
    belt_y = int(hipY + 1)
    c.fill([p for p in torso if p[1] in (belt_y, belt_y + 1)], Ramp(spec.get('belt', shift(top, dl=-0.2))), soft=True)
    c.put(int(cxB + 1), belt_y + 1, GOLD, lock=True)
    if spec.get('overalls'):
        straps = [p for p in torso if p[1] > belt_y + 1 and (abs(p[0] - (cxB - 2)) < 1 or abs(p[0] - (cxB + 3)) < 1)]
        c.fill(straps, Ramp(spec['pants']), soft=True)
        bib = [p for p in torso if belt_y + 1 < p[1] < belt_y + 6 and cxB - 2 <= p[0] <= cxB + 3]
        c.fill(bib, Ramp(spec['pants']), soft=True)
    if spec.get('emblem'):
        ex, ey = int(cxB + 1 + lean * 0.5), int((hipY + topY) / 2 + 1)
        c.fill(c.mask_ellipse(ex, ey, 1.6, 1.6), Ramp(spec['emblem']))
    if extra == 'scarf':
        sway = [0, 1, 2, 1, 0, -1][flutter % 6]
        col = spec.get('scarf', spec['accent'])
        c.fill(c.mask_ellipse(cxB + lean + 0.5, topY + 0.5, tw + 0.8, 1.9), Ramp(col), soft=True)
        tail = c.mask_poly([(cxB - tw + 1, topY + 1), (cxB - tw - 6, topY - 1 - sway), (cxB - tw - 5, topY - 4 - sway),
                            (cxB - tw + 1, topY - 1)])
        c.fill(tail, Ramp(col), soft=True)
    if extra == 'collar':
        c.fill(c.mask_ellipse(cxB + lean + 0.5, topY + 0.5, tw + 0.5, 1.6), Ramp(spec['accent']), soft=True)

    # -- head
    hx = cxB + lean + 0.5 + (1 if spec['build'] != 'bulky' else 1.5)
    hy = b['hcy'] + by
    draw_head(c, hx, hy, spec, pose['eyes'], b, flutter)

    # -- front arm + weapon
    hf = pose['hands'][1]
    H_front = (S_front[0] + hf[0], S_front[1] + hf[1])
    E_front = ik(S_front, H_front, arm_l * 0.55, arm_l * 0.55, -1)
    if weapon['type'] not in ('board',) and pose['hold'] == 'front':
        pass
    wt = weapon['type']
    behind = wt in ('spear', 'staff')  # long weapons pass behind the front hand
    if wt not in ('gloves', 'fists', 'ball'):
        tip = draw_weapon(c, H_front, pose['wang'], weapon, glow)
    else:
        tip = H_front
    c.fill(c.mask_capsule(*S_front, *E_front, b['arm']), Ramp(spec.get('sleeve', top)))
    c.fill(c.mask_capsule(*E_front, *H_front, b['arm'] * 0.9), Ramp(spec.get('forearm', spec.get('sleeve', top))))
    if wt == 'gloves':
        gp = c.fill(c.mask_ellipse(H_front[0] + 0.5, H_front[1], hr, hr), Ramp(weapon['color']))
        c.specular(int(H_front[0]) - 1, int(H_front[1] + hr * 0.5))
        c.fill(c.mask_ellipse(H_back[0], H_back[1], hr * 0.9, hr * 0.9), Ramp(shift(weapon['color'], dl=-0.1)))
        tip = (H_front[0] + hr, H_front[1])
    elif wt == 'fists':
        c.fill(c.mask_ellipse(H_back[0], H_back[1], hr * 0.9, hr * 0.85), Ramp(shift(weapon['color'], dl=-0.1)))
        c.fill(c.mask_ellipse(H_front[0] + 0.5, H_front[1], hr, hr * 0.9), Ramp(weapon['color']))
        for k in range(-1, 2):
            c.put(int(H_front[0] + hr * 0.6), int(H_front[1] + k * 1.6), shift(weapon['color'], dl=-0.28))
        c.fill(c.mask_ellipse(H_front[0] - 0.5, H_front[1] + hr * 0.6, 1.8, 1.0), Ramp(weapon.get('accent', C(96, 170, 80))), soft=True)
        tip = (H_front[0] + hr, H_front[1])
    else:
        c.fill(c.mask_ellipse(H_front[0], H_front[1], b['hand'], b['hand']), Ramp(hand_col))
    if wt == 'ball':
        bp = (H_front[0] + 3, H_front[1] + 1) if pose['smear'] is None else (H_front[0] + 6, H_front[1] + 2)
        c.fill(c.mask_ellipse(bp[0], bp[1], 2.8, 2.8), Ramp(C(250, 250, 250)))
        for (x, y) in ((0, 0), (1, 1), (-1, 1), (0, -1)):
            c.put(int(bp[0]) + x, int(bp[1]) + y, C(40, 40, 50))
        c.specular(int(bp[0] - 1), int(bp[1] + 1))
        tip = bp

    # -- passes
    c.separate(0.5)
    c.rim(t=0.28)
    c.outline(tint=0.28)
    # weapon trails / special glow drawn outside the outline
    fx_col = weapon.get('fx', C(255, 240, 170))
    if pose['smear'] == 'swing':
        draw_smear(c, (cxB + 2, hipY + 6), 'swing', fx_col, reach=weapon.get('reach', 18))
    if pose['smear'] == 'burst' or glow:
        draw_smear(c, tip, 'burst', fx_col)
    return c


def crop_center(img, cw):
    x0 = (img.width - cw) // 2
    return img.crop((x0, 0, x0 + cw, img.height))

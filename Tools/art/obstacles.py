"""
Obstacle and puzzle art (all original), 32 px per world unit: crusher, laser emitter + beam, saw blade, key,
locked door, red / blue switch blocks and the floor switch.
"""
import math
from px import Canvas, Ramp, shift, INK
from tiles import C
from props import finish, gem, gear_at
from chars import METAL, GOLD, WOOD

IRON = C(120, 126, 150)


def crusher():
    """2x2 cells: a clockwork press-hammer head, hazard-striped base plate and a gear housing (no face)."""
    c = Canvas(64, 64)
    c.fill(c.mask_rect(26, 52, 37, 63), Ramp(METAL))                       # piston rod
    c.fill(c.mask_rect(4, 22, 59, 53, r=3), Ramp(IRON))                     # housing
    gear_at(c, 32, 38, 11, Ramp(GOLD), teeth=8)
    for x in (9, 54):
        for y in (27, 48):
            c.fill(c.mask_ellipse(x, y, 2.0, 2.0), Ramp(METAL))
    c.fill(c.mask_rect(1, 6, 62, 21, r=2), Ramp(C(70, 72, 88)))             # heavy plate
    for i in range(-2, 10):                                                  # hazard stripes
        x0 = i * 8
        c.fill([p for p in c.mask_poly([(x0, 8), (x0 + 4, 8), (x0 + 10, 19), (x0 + 6, 19)]) if 2 <= p[0] <= 61],
               Ramp(C(250, 200, 60)), light=(-0.6, 0.6))
    c.fill(c.mask_rect(2, 0, 61, 5), Ramp(C(200, 206, 222)), light=(-0.8, 0.3))   # steel striking edge
    return finish(c)


def laser_emitter():
    c = Canvas(32, 16)
    c.fill(c.mask_rect(3, 0, 28, 11, r=2), Ramp(C(80, 86, 110)))
    c.fill(c.mask_rect(10, 9, 21, 15, r=2), Ramp(C(110, 116, 140)))
    c.fill(c.mask_ellipse(16, 5, 4, 3.4), Ramp(C(255, 120, 120)))
    c.specular(14, 6)
    return finish(c)


def laser_beam():
    """A white tileable beam strip (tinted red at runtime): bright core, soft edges."""
    c = Canvas(12, 32)
    for y in range(32):
        for x in range(12):
            d = abs(x - 5.5)
            if d < 1.6:
                c.put(x, y, (255, 255, 255), a=255)
            elif d < 3.6:
                c.put(x, y, (255, 255, 255), a=170)
            elif d < 5.6:
                c.put(x, y, (255, 255, 255), a=70)
    return c


def saw():
    c = Canvas(48, 48)
    pts = c.mask_ellipse(24, 24, 18, 18)
    for i in range(14):
        a = i * math.tau / 14
        tip = (24 + math.cos(a) * 23, 24 + math.sin(a) * 23)
        l = (24 + math.cos(a - 0.22) * 17, 24 + math.sin(a - 0.22) * 17)
        r = (24 + math.cos(a + 0.12) * 17, 24 + math.sin(a + 0.12) * 17)
        pts += c.mask_poly([l, tip, r])
    c.fill(pts, Ramp(C(206, 212, 228)))
    c.fill(c.mask_ellipse(24, 24, 11, 11), Ramp(C(150, 156, 176)))
    gear_at(c, 24, 24, 7, Ramp(C(230, 80, 70)), teeth=6)
    for i in range(4):
        a = i * math.tau / 4 + 0.4
        c.put(24 + math.cos(a) * 14, 24 + math.sin(a) * 14, (255, 255, 255), lock=True)
    return finish(c)


def key():
    c = Canvas(24, 32)
    c.fill(c.mask_ellipse(12, 23, 7.5, 7.5), Ramp(GOLD))
    c.flat(c.mask_ellipse(12, 23, 3.2, 3.2), (0, 0, 0, 0))
    for (x, y) in c.mask_ellipse(12, 23, 3.2, 3.2):
        c.px[y][x] = None
    c.fill(c.mask_rect(10, 3, 14, 16), Ramp(GOLD))
    c.fill(c.mask_rect(14, 4, 18, 6), Ramp(GOLD))
    c.fill(c.mask_rect(14, 9, 17, 11), Ramp(GOLD))
    c.specular(8, 26)
    return finish(c)


def door_tile():
    """One cell of a locked door (tiled up the doorway): planks with iron bands."""
    c = Canvas(32, 32)
    c.fill(c.mask_rect(1, 0, 30, 31), Ramp(WOOD))
    for x in (8, 16, 24):
        for y in range(32):
            c.put(x, y, shift(WOOD, dl=-0.18))
    for y in (5, 25):
        c.fill(c.mask_rect(1, y, 30, y + 2), Ramp(C(90, 92, 110)))
        for x in (4, 27):
            c.specular(x, y + 1, C(230, 230, 240))
    return finish(c, rim=False)


def door_lock():
    c = Canvas(32, 32)
    c.fill(c.mask_rect(8, 4, 23, 20, r=2), Ramp(GOLD))
    c.fill(c.mask_ellipse(16, 21, 6, 6), Ramp(GOLD))
    for (x, y) in c.mask_ellipse(16, 21, 3, 3.5):
        c.px[y][x] = None
    c.fill(c.mask_ellipse(16, 13, 2.2, 2.2), Ramp(C(50, 40, 40)))
    c.fill(c.mask_rect(15, 7, 17, 12), Ramp(C(50, 40, 40)))
    return finish(c)


def switch_block(red=True):
    col = C(230, 70, 80) if red else C(70, 130, 240)
    c = Canvas(32, 32)
    c.fill(c.mask_rect(0, 0, 31, 31, r=2), Ramp(col))
    c.fill(c.mask_rect(4, 4, 27, 27, r=1), Ramp(shift(col, dl=0.07)))
    # symbol: a sun for red, a moon for blue
    if red:
        c.fill(c.mask_ellipse(16, 16, 6, 6), Ramp(C(255, 230, 200)))
        for i in range(8):
            a = i * math.tau / 8
            c.put(16 + math.cos(a) * 9, 16 + math.sin(a) * 9, C(255, 230, 200), lock=True)
    else:
        moon = [p for p in c.mask_ellipse(16, 16, 7, 7) if (p[0] - 19) ** 2 + (p[1] - 18) ** 2 > 30]
        c.fill(moon, Ramp(C(220, 235, 255)))
    return finish(c, rim=False)


def switch_base():
    c = Canvas(32, 16)
    c.fill(c.mask_rect(2, 0, 29, 7, r=1), Ramp(C(90, 96, 120)))
    return finish(c, rim=False)


def switch_cap():
    """White cap (tinted red/blue at runtime)."""
    c = Canvas(24, 10)
    c.fill(c.mask_rect(1, 0, 22, 8, r=3), Ramp(C(236, 236, 240)))
    return finish(c, rim=False)


ALL = {
    'crusher': crusher, 'laser_emitter': laser_emitter, 'laser_beam': laser_beam, 'saw': saw, 'key': key,
    'door_tile': door_tile, 'door_lock': door_lock, 'block_red': lambda: switch_block(True),
    'block_blue': lambda: switch_block(False), 'switch_base': switch_base, 'switch_cap': switch_cap,
}

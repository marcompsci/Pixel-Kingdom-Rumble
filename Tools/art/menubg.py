"""
Menu backdrop (all original): the Sunspire meadow layers over a sunset sky, baked into one 640x360 strip that
tiles sideways (the menus scroll it slowly), plus a separate cloud strip that drifts faster.
"""
from PIL import Image
import bg

W, H = 640, 360
TOP, MID, BOTTOM = (34, 38, 92), (120, 96, 168), (250, 172, 140)


def _sky():
    img = Image.new('RGBA', (W, H))
    p = img.load()
    for y in range(H):
        t = y / (H - 1)
        if t < 0.6:
            k = t / 0.6
            c = tuple(int(TOP[i] + (MID[i] - TOP[i]) * k) for i in range(3))
        else:
            k = (t - 0.6) / 0.4
            c = tuple(int(MID[i] + (BOTTOM[i] - MID[i]) * k) for i in range(3))
        for x in range(W):
            p[x, y] = c + (255,)
    return img


def _tint(img, mul):
    r, g, b, a = img.split()
    r = r.point(lambda v: int(v * mul[0])); g = g.point(lambda v: int(v * mul[1])); b = b.point(lambda v: int(v * mul[2]))
    return Image.merge('RGBA', (r, g, b, a))


def build():
    L = bg.THEMES['meadow']()
    sky = _sky()
    # Bottom-aligned layers, each a little darker and warmer toward the front (evening light).
    for key, mul, lift in (('far', (0.55, 0.5, 0.78), 70), ('mid', (0.4, 0.4, 0.55), 30), ('near', (0.28, 0.28, 0.4), 0)):
        im = _tint(L[key].image(), mul)
        if key == 'near':
            p = sky.load()
            for y in range(H - 40, H):             # solid ground behind the front hedge and fence
                for x in range(W):
                    p[x, y] = (26, 30, 44, 255)
        sky.alpha_composite(im, (0, H - im.height - lift))
    clouds = L.get('clouds')
    cl = _tint(clouds.image(), (1.0, 0.85, 0.9)) if clouds is not None else Image.new('RGBA', (W, 192))
    return sky, cl

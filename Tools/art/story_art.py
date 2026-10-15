"""
Story cutscene pictures (all original): a world backdrop under its own sky, cropped to the comic frame's shape
(640x206), with a few pixel props for the beats that need them. The hero is drawn on top at runtime.
"""
import math, random
from PIL import Image, ImageDraw
import bg

W, H = 640, 206


def _sky(top, bottom):
    img = Image.new('RGBA', (W, H))
    d = ImageDraw.Draw(img)
    for y in range(H):
        t = y / (H - 1)
        d.line([(0, y), (W, y)], fill=tuple(int(top[i] + (bottom[i] - top[i]) * t) for i in range(3)) + (255,))
    return img


def _tint(img, mul):
    if mul == (1, 1, 1):
        return img
    r, g, b, a = img.split()
    return Image.merge('RGBA', (r.point(lambda v: min(255, int(v * mul[0]))), g.point(lambda v: min(255, int(v * mul[1]))),
                                b.point(lambda v: min(255, int(v * mul[2]))), a))


def scene(theme, top, bottom, mul=(1, 1, 1), lifts=(40, 14, -10), clouds=True):
    L = bg.THEMES[theme]()
    img = _sky(top, bottom)
    if clouds and 'clouds' in L:
        img.alpha_composite(_tint(L['clouds'].image(), mul), (0, -40))
    for key, lift in zip(('far', 'mid', 'near'), lifts):
        layer = _tint(L[key].image(), mul)
        img.alpha_composite(layer, (0, H - layer.height - lift))
    return img


def _gear(d, cx, cy, r, col, hole):
    teeth = 10
    pts = []
    for k in range(teeth * 2):
        a = k * math.pi / teeth
        rr = r if k % 2 == 0 else r * 0.82
        pts.append((cx + math.cos(a) * rr, cy + math.sin(a) * rr))
    d.polygon(pts, fill=col)
    d.ellipse([cx - r * 0.35, cy - r * 0.35, cx + r * 0.35, cy + r * 0.35], fill=hole)


def _stars(img, n, seed):
    r = random.Random(seed)
    p = img.load()
    for _ in range(n):
        x, y = r.randrange(W), r.randrange(H // 2)
        p[x, y] = (255, 255, 255, 255) if r.random() < 0.3 else (200, 210, 255, 255)


def isles():
    return scene('meadow', (110, 170, 240), (200, 230, 255))


def dawn():
    return scene('meadow', (250, 170, 120), (255, 226, 170), mul=(1.0, 0.92, 0.85))


def hero():
    return scene('garden', (120, 190, 250), (210, 240, 255))


def peaks():
    return scene('frost', (150, 196, 240), (230, 244, 255))


def realms():
    img = scene('shattered', (40, 26, 70), (120, 70, 140))
    _stars(img, 60, 7)
    return img


def warden():
    img = scene('foundry', (60, 40, 34), (140, 90, 60), lifts=(30, 6, -16))
    d = ImageDraw.Draw(img)
    _gear(d, 320, 96, 58, (200, 150, 70, 255), (50, 36, 30, 255))
    d.ellipse([306, 82, 334, 110], fill=(255, 214, 90, 255))
    return img


def mainspring():
    img = scene('foundry', (80, 50, 40), (180, 120, 70), lifts=(30, 6, -16))
    d = ImageDraw.Draw(img)
    for i, (x, r) in enumerate(((200, 40), (320, 64), (450, 36))):
        _gear(d, x, 100 - i % 2 * 10, r, (220, 176, 90, 255), (70, 50, 40, 255))
    for k in range(6):                                  # the coiled spring at the heart
        d.ellipse([320 - 10 - k * 4, 100 - 10 - k * 4, 320 + 10 + k * 4, 100 + 10 + k * 4], outline=(255, 230, 140, 255))
    return img


def sovereign():
    img = scene('shattered', (20, 12, 32), (70, 30, 80), mul=(0.8, 0.7, 0.9))
    _stars(img, 80, 11)
    d = ImageDraw.Draw(img)
    for i, col in enumerate(((232, 96, 48), (120, 196, 240), (48, 170, 160), (214, 84, 150))):
        a = i * math.pi / 2 + math.pi / 4
        x, y = 320 + math.cos(a) * 80, 90 + math.sin(a) * 50
        d.polygon([(x, y - 16), (x + 11, y), (x, y + 16), (x - 11, y)], fill=col + (255,))
    d.ellipse([300, 70, 340, 110], fill=(255, 220, 90, 255))
    return img


def ledger():
    img = scene('cave', (40, 30, 50), (90, 70, 90), mul=(0.9, 0.85, 0.9))
    d = ImageDraw.Draw(img)
    d.rectangle([230, 40, 410, 170], fill=(236, 220, 180, 255))
    d.rectangle([230, 40, 410, 48], fill=(180, 140, 90, 255))
    d.rectangle([230, 162, 410, 170], fill=(180, 140, 90, 255))
    for i in range(6):
        d.line([(250, 62 + i * 16), (390 - (i % 3) * 18, 62 + i * 16)], fill=(120, 100, 90, 255), width=3)
    return img


def starfall():
    img = scene('astral', (16, 14, 44), (60, 50, 120))
    _stars(img, 120, 23)
    d = ImageDraw.Draw(img)
    for k in range(5):                                  # falling stars
        x, y = 120 + k * 110, 20 + (k % 3) * 18
        d.line([(x, y), (x - 34, y - 14)], fill=(255, 240, 180, 255), width=2)
        d.ellipse([x - 3, y - 3, x + 3, y + 3], fill=(255, 255, 220, 255))
    return img


def orrery():
    img = scene('astral', (22, 18, 56), (80, 60, 140))
    _stars(img, 90, 29)
    d = ImageDraw.Draw(img)
    for r, col in ((70, (210, 170, 90)), (48, (170, 140, 220)), (26, (255, 220, 120))):
        d.ellipse([320 - r, 96 - r * 0.55, 320 + r, 96 + r * 0.55], outline=col + (255,), width=3)
    d.ellipse([308, 84, 332, 108], fill=(255, 230, 140, 255))
    return img


def zl_city():
    img = scene('city', (40, 18, 30), (150, 50, 40), mul=(0.75, 0.62, 0.62))
    _stars(img, 30, 41)
    d = ImageDraw.Draw(img)
    d.ellipse([520, 18, 556, 54], fill=(230, 210, 170, 255))             # a sick moon
    for x in range(0, W, 9):                                            # the quarantine fence
        d.line([(x, H - 10), (x, H - 34)], fill=(70, 70, 76, 255), width=2)
    d.line([(0, H - 30), (W, H - 30)], fill=(150, 40, 40, 255), width=2)
    return img


def zl_boss():
    img = scene('haunted', (20, 10, 26), (70, 30, 60), mul=(0.7, 0.6, 0.75))
    d = ImageDraw.Draw(img)
    d.ellipse([290, 40, 350, 100], fill=(150, 150, 170, 255), outline=(30, 26, 40, 255))   # the Graystone
    d.ellipse([310, 60, 330, 80], fill=(230, 220, 255, 255))
    for k in range(10):
        a = k * math.pi / 5
        d.line([(320 + math.cos(a) * 34, 70 + math.sin(a) * 34), (320 + math.cos(a) * 60, 70 + math.sin(a) * 60)], fill=(170, 160, 210, 255), width=2)
    return img


ALL = {'isles': isles, 'dawn': dawn, 'hero': hero, 'peaks': peaks, 'realms': realms, 'warden': warden,
       'mainspring': mainspring, 'sovereign': sovereign, 'ledger': ledger, 'starfall': starfall, 'orrery': orrery,
       'zl_city': zl_city, 'zl_boss': zl_boss}

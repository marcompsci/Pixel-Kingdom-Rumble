"""
On-screen touch controls (original art): dark rounded buttons with a soft bevel and bold white icons, a round
D-pad base with a raised cross, a dotted thumb knob and square HUD buttons. Smooth vector-style shapes (drawn at 4x
and downsampled) so they read cleanly over the pixel art. Icons are white so the game can tint them per theme.
"""
import math
from PIL import Image, ImageDraw, ImageFilter

SS = 4  # supersampling


def _canvas(size):
    return Image.new('RGBA', (size * SS, size * SS), (0, 0, 0, 0))


def _done(img, size):
    return img.resize((size, size), Image.LANCZOS)


def _circle(d, cx, cy, r, fill):
    d.ellipse((cx - r, cy - r, cx + r, cy + r), fill=fill)


BASE = (52, 52, 56, 255)
RIM = (74, 74, 80, 255)
DARK = (34, 34, 38, 255)
EDGE = (24, 24, 28, 255)
HI = (255, 255, 255, 34)


def round_button(size=192, pressed=False):
    """Dark disc: outer edge, lighter rim ring, inner face with a top highlight (pressed: sunk, no highlight)."""
    img = _canvas(size)
    d = ImageDraw.Draw(img)
    s = size * SS
    c = s / 2
    _circle(d, c, c + s * 0.015, s * 0.49, (0, 0, 0, 90))            # drop shadow
    _circle(d, c, c, s * 0.48, EDGE)
    _circle(d, c, c, s * 0.455, RIM if not pressed else BASE)
    face = DARK if pressed else BASE
    _circle(d, c, c + (s * 0.01 if pressed else 0), s * 0.385, face)
    if not pressed:
        hl = Image.new('RGBA', img.size, (0, 0, 0, 0))
        hd = ImageDraw.Draw(hl)
        hd.ellipse((c - s * 0.32, c - s * 0.37, c + s * 0.32, c - s * 0.02), fill=HI)
        hl = hl.filter(ImageFilter.GaussianBlur(s * 0.03))
        img = Image.alpha_composite(img, hl)
    return _done(img, size)


def square_button(size=128):
    img = _canvas(size)
    d = ImageDraw.Draw(img)
    s = size * SS
    r = s * 0.16
    d.rounded_rectangle((s * 0.03, s * 0.05, s * 0.97, s * 0.99), r, fill=(0, 0, 0, 90))
    d.rounded_rectangle((s * 0.03, s * 0.03, s * 0.97, s * 0.95), r, fill=EDGE)
    d.rounded_rectangle((s * 0.08, s * 0.07, s * 0.92, s * 0.88), r * 0.8, fill=RIM)
    d.rounded_rectangle((s * 0.12, s * 0.11, s * 0.88, s * 0.84), r * 0.6, fill=BASE)
    return _done(img, size)


def _tri(d, cx, cy, r, ang, fill):
    pts = [(cx + math.cos(ang + k * math.tau / 3) * r, cy + math.sin(ang + k * math.tau / 3) * r) for k in range(3)]
    d.polygon(pts, fill=fill)


def pad_base(size=320):
    """Round base with a raised cross and a small arrow on each arm (the stick/D-pad background)."""
    img = _canvas(size)
    d = ImageDraw.Draw(img)
    s = size * SS
    c = s / 2
    _circle(d, c, c + s * 0.012, s * 0.49, (0, 0, 0, 90))
    _circle(d, c, c, s * 0.48, EDGE)
    _circle(d, c, c, s * 0.46, RIM)
    _circle(d, c, c, s * 0.43, DARK)
    arm, w = s * 0.36, s * 0.13
    for (x0, y0, x1, y1) in ((c - w, c - arm, c + w, c + arm), (c - arm, c - w, c + arm, c + w)):
        d.rounded_rectangle((x0, y0 + s * 0.012, x1, y1 + s * 0.012), s * 0.03, fill=EDGE)
        d.rounded_rectangle((x0, y0, x1, y1), s * 0.03, fill=BASE)
    _circle(d, c, c, s * 0.05, DARK)
    arrow = (150, 150, 158, 255)
    for k, ang in enumerate((-math.pi / 2, 0, math.pi / 2, math.pi)):
        ax, ay = c + math.cos(ang) * arm * 0.74, c + math.sin(ang) * arm * 0.74
        _tri(d, ax, ay, s * 0.045, ang, arrow)
    # the four diagonals as faint dots (8-way hint)
    for ang in (math.pi / 4, 3 * math.pi / 4, 5 * math.pi / 4, 7 * math.pi / 4):
        _circle(d, c + math.cos(ang) * s * 0.33, c + math.sin(ang) * s * 0.33, s * 0.014, (110, 110, 118, 255))
    return _done(img, size)


def pad_knob(size=140):
    img = round_button(size)
    d = ImageDraw.Draw(img)
    c = size / 2
    for ang in (0, math.pi / 2, math.pi, 3 * math.pi / 2):
        x, y = c + math.cos(ang) * size * 0.2, c + math.sin(ang) * size * 0.2
        d.ellipse((x - size * 0.03, y - size * 0.03, x + size * 0.03, y + size * 0.03), fill=(150, 150, 158, 255))
    return img


def pad_glow(size=320):
    """Soft wedge shown under the knob in the pressed direction (rotated at runtime)."""
    img = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    c = size / 2
    d.pieslice((c - size * 0.47, c - size * 0.47, c + size * 0.47, c + size * 0.47), -25, 25, fill=(255, 255, 255, 120))
    _circle(d, c, c, size * 0.2, (0, 0, 0, 0))
    return img.filter(ImageFilter.GaussianBlur(size * 0.02))


# ---- icons: white on transparent, 128 px ---------------------------------------------------------------------------
W = (255, 255, 255, 255)


def _icon():
    return _canvas(128)


def _fin(img):
    return _done(img, 128)


def icon_jump():
    """Curved arrow up and over (jump)."""
    img = _icon()
    d = ImageDraw.Draw(img)
    s = 128 * SS
    d.arc((s * 0.22, s * 0.36, s * 0.78, s * 0.98), 180, 285, fill=W, width=int(s * 0.12))
    tip = (s * 0.80, s * 0.36)
    d.polygon([tip, (s * 0.54, s * 0.20), (s * 0.60, s * 0.56)], fill=W)
    return _fin(img)


def icon_star():
    img = _icon()
    d = ImageDraw.Draw(img)
    s = 128 * SS
    c = s / 2
    pts = []
    for k in range(10):
        r = s * (0.36 if k % 2 == 0 else 0.15)
        a = -math.pi / 2 + k * math.pi / 5
        pts.append((c + math.cos(a) * r, c + s * 0.03 + math.sin(a) * r))
    d.polygon(pts, fill=W)
    return _fin(img)


def icon_target():
    img = _icon()
    d = ImageDraw.Draw(img)
    s = 128 * SS
    c = s / 2
    _circle(d, c, c, s * 0.33, W)
    _circle(d, c, c, s * 0.22, (0, 0, 0, 0))
    _circle(d, c, c, s * 0.11, W)
    return _fin(img)


def icon_dash():
    """Arrow with speed fins (dodge)."""
    img = _icon()
    d = ImageDraw.Draw(img)
    s = 128 * SS
    d.polygon([(s * 0.86, s * 0.5), (s * 0.48, s * 0.22), (s * 0.54, s * 0.42), (s * 0.30, s * 0.42),
               (s * 0.30, s * 0.58), (s * 0.54, s * 0.58), (s * 0.48, s * 0.78)], fill=W)
    for k, y in enumerate((0.34, 0.5, 0.66)):
        x0 = s * (0.14 if k == 1 else 0.2)
        d.rounded_rectangle((x0, s * (y - 0.035), s * 0.26, s * (y + 0.035)), s * 0.03, fill=W)
    return _fin(img)


def icon_pause():
    img = _icon()
    d = ImageDraw.Draw(img)
    s = 128 * SS
    for x in (0.30, 0.56):
        d.rounded_rectangle((s * x, s * 0.25, s * (x + 0.14), s * 0.75), s * 0.03, fill=W)
    return _fin(img)


def _speaker(d, s):
    d.rectangle((s * 0.18, s * 0.40, s * 0.32, s * 0.60), fill=W)
    d.polygon([(s * 0.30, s * 0.40), (s * 0.50, s * 0.22), (s * 0.50, s * 0.78), (s * 0.30, s * 0.60)], fill=W)


def icon_sound():
    img = _icon()
    d = ImageDraw.Draw(img)
    s = 128 * SS
    _speaker(d, s)
    for r in (0.16, 0.28):
        d.arc((s * (0.5 - r), s * (0.5 - r), s * (0.5 + r), s * (0.5 + r)), -50, 50, fill=W, width=int(s * 0.06))
    return _fin(img)


def icon_mute():
    img = _icon()
    d = ImageDraw.Draw(img)
    s = 128 * SS
    _speaker(d, s)
    w = int(s * 0.065)
    d.line((s * 0.60, s * 0.36, s * 0.84, s * 0.64), fill=W, width=w)
    d.line((s * 0.60, s * 0.64, s * 0.84, s * 0.36), fill=W, width=w)
    return _fin(img)


def icon_menu():
    img = _icon()
    d = ImageDraw.Draw(img)
    s = 128 * SS
    for y in (0.30, 0.47, 0.64):
        d.rounded_rectangle((s * 0.24, s * y, s * 0.76, s * (y + 0.09)), s * 0.03, fill=W)
    return _fin(img)


ALL = {
    'ctl_button': lambda: round_button(192),
    'ctl_button_pressed': lambda: round_button(192, pressed=True),
    'ctl_square': lambda: square_button(128),
    'ctl_pad_base': lambda: pad_base(320),
    'ctl_pad_knob': lambda: pad_knob(140),
    'ctl_pad_glow': lambda: pad_glow(320),
    'icon_jump': icon_jump, 'icon_attack': icon_star, 'icon_special': icon_target, 'icon_dodge': icon_dash,
    'icon_pause': icon_pause, 'icon_sound': icon_sound, 'icon_mute': icon_mute, 'icon_menu': icon_menu,
}

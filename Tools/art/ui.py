"""
Menu UI art (all original): chunky pixel 9-slice panels and buttons (grey-scale so the game tints them per theme)
and "PKR Pixel", a blocky 5x7 display font built as a TrueType file (fontTools) for titles and buttons.
"""
from PIL import Image

INK = (16, 14, 24, 255)


def _img(w, h):
    return Image.new('RGBA', (w, h), (0, 0, 0, 0))


def _corner_cut(x, y, w, h, k):
    """True for pixels removed to make stepped (pixel-rounded) corners of size k."""
    dx = min(x, w - 1 - x)
    dy = min(y, h - 1 - y)
    return dx + dy < k


def panel(size=48, border=16):
    """Outline, a lit top-left rim, a shaded bottom-right rim and a flat light face (tinted at runtime)."""
    im = _img(size, size)
    p = im.load()
    for y in range(size):
        for x in range(size):
            if _corner_cut(x, y, size, size, 4):
                continue
            edge = min(x, y, size - 1 - x, size - 1 - y)
            if edge < 2 or _corner_cut(x, y, size, size, 6):
                p[x, y] = INK
            elif edge < 4:
                lit = (x < size / 2 and y < size - 6) or y < 6
                p[x, y] = (255, 255, 255, 255) if lit and y < size - 4 else (150, 150, 160, 255)
            elif y >= size - 7:
                p[x, y] = (196, 196, 204, 255)
            else:
                p[x, y] = (232, 232, 238, 255)
    return im


def button(size=32, pressed=False):
    """Chunky 3D key: bright top lip, face, and a thick darker base (pressed: base gone, face lower)."""
    im = _img(size, size)
    p = im.load()
    base = 0 if pressed else 5
    for y in range(size):
        for x in range(size):
            if _corner_cut(x, y, size, size, 3):
                continue
            edge = min(x, y, size - 1 - x, size - 1 - y)
            if edge < 2 or _corner_cut(x, y, size, size, 5):
                p[x, y] = INK
            elif y >= size - 2 - base:
                p[x, y] = (150, 150, 158, 255)               # side / base of the key
            elif pressed and y < 5:
                p[x, y] = (196, 196, 204, 255)               # sunk: top in shadow
            elif not pressed and y < 4:
                p[x, y] = (255, 255, 255, 255)               # top lip catches the light
            else:
                p[x, y] = (236, 236, 240, 255)
    if not pressed:
        for x in range(4, 9):                                 # a little glint
            p[x, 5] = (255, 255, 255, 255)
    return im


HEART = [
    "..##...##..",
    ".####.####.",
    "###########",
    "###########",
    "###########",
    ".#########.",
    "..#######..",
    "...#####...",
    "....###....",
    ".....#.....",
]


def heart(full=True):
    """HUD health heart (13x12): outlined, red with a glint when full, a dark socket when empty."""
    w, h = 13, 12
    im = _img(w, h)
    p = im.load()
    fill = (232, 52, 72, 255) if full else (60, 40, 60, 255)
    shade = (170, 28, 60, 255) if full else (44, 30, 48, 255)
    for y, row in enumerate(HEART):
        for x, ch in enumerate(row):
            if ch != '#':
                continue
            px, py = x + 1, y + 1
            p[px, py] = shade if (y >= 5 or x >= 8) else fill
    # outline
    solid = {(x + 1, y + 1) for y, row in enumerate(HEART) for x, ch in enumerate(row) if ch == '#'}
    for y in range(h):
        for x in range(w):
            if (x, y) in solid:
                continue
            if any((x + dx, y + dy) in solid for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                p[x, y] = INK
    if full:
        p[3, 2] = p[4, 2] = p[3, 3] = (255, 220, 225, 255)
    return im


# ---- 5x7 pixel font ---------------------------------------------------------------------------------------------
G = {
    'A': ".###.|#...#|#...#|#####|#...#|#...#|#...#",
    'B': "####.|#...#|#...#|####.|#...#|#...#|####.",
    'C': ".###.|#...#|#....|#....|#....|#...#|.###.",
    'D': "####.|#...#|#...#|#...#|#...#|#...#|####.",
    'E': "#####|#....|#....|####.|#....|#....|#####",
    'F': "#####|#....|#....|####.|#....|#....|#....",
    'G': ".###.|#...#|#....|#.###|#...#|#...#|.####",
    'H': "#...#|#...#|#...#|#####|#...#|#...#|#...#",
    'I': "#####|..#..|..#..|..#..|..#..|..#..|#####",
    'J': "..###|...#.|...#.|...#.|#..#.|#..#.|.##..",
    'K': "#...#|#..#.|#.#..|##...|#.#..|#..#.|#...#",
    'L': "#....|#....|#....|#....|#....|#....|#####",
    'M': "#...#|##.##|#.#.#|#.#.#|#...#|#...#|#...#",
    'N': "#...#|##..#|#.#.#|#..##|#...#|#...#|#...#",
    'O': ".###.|#...#|#...#|#...#|#...#|#...#|.###.",
    'P': "####.|#...#|#...#|####.|#....|#....|#....",
    'Q': ".###.|#...#|#...#|#...#|#.#.#|#..#.|.##.#",
    'R': "####.|#...#|#...#|####.|#.#..|#..#.|#...#",
    'S': ".####|#....|#....|.###.|....#|....#|####.",
    'T': "#####|..#..|..#..|..#..|..#..|..#..|..#..",
    'U': "#...#|#...#|#...#|#...#|#...#|#...#|.###.",
    'V': "#...#|#...#|#...#|#...#|#...#|.#.#.|..#..",
    'W': "#...#|#...#|#...#|#.#.#|#.#.#|##.##|#...#",
    'X': "#...#|#...#|.#.#.|..#..|.#.#.|#...#|#...#",
    'Y': "#...#|#...#|.#.#.|..#..|..#..|..#..|..#..",
    'Z': "#####|....#|...#.|..#..|.#...|#....|#####",
    '0': ".###.|#...#|#..##|#.#.#|##..#|#...#|.###.",
    '1': "..#..|.##..|..#..|..#..|..#..|..#..|.###.",
    '2': ".###.|#...#|....#|...#.|..#..|.#...|#####",
    '3': "####.|....#|....#|.###.|....#|....#|####.",
    '4': "...#.|..##.|.#.#.|#..#.|#####|...#.|...#.",
    '5': "#####|#....|####.|....#|....#|#...#|.###.",
    '6': ".###.|#....|#....|####.|#...#|#...#|.###.",
    '7': "#####|....#|...#.|..#..|.#...|.#...|.#...",
    '8': ".###.|#...#|#...#|.###.|#...#|#...#|.###.",
    '9': ".###.|#...#|#...#|.####|....#|....#|.###.",
    '.': ".....|.....|.....|.....|.....|.##..|.##..",
    ',': ".....|.....|.....|.....|.##..|..#..|.#...",
    ':': ".....|.##..|.##..|.....|.##..|.##..|.....",
    ';': ".....|.##..|.##..|.....|.##..|..#..|.#...",
    '!': "..#..|..#..|..#..|..#..|..#..|.....|..#..",
    '?': ".###.|#...#|....#|...#.|..#..|.....|..#..",
    "'": "..#..|..#..|.#...|.....|.....|.....|.....",
    '"': ".#.#.|.#.#.|.....|.....|.....|.....|.....",
    '-': ".....|.....|.....|.###.|.....|.....|.....",
    '+': ".....|..#..|..#..|#####|..#..|..#..|.....",
    '=': ".....|.....|#####|.....|#####|.....|.....",
    '/': "....#|....#|...#.|..#..|.#...|#....|#....",
    '(': "...#.|..#..|.#...|.#...|.#...|..#..|...#.",
    ')': ".#...|..#..|...#.|...#.|...#.|..#..|.#...",
    '%': "##..#|##..#|...#.|..#..|.#...|#..##|#..##",
    '&': ".##..|#..#.|#.#..|.#...|#.#.#|#..#.|.##.#",
    '#': ".#.#.|#####|.#.#.|.#.#.|.#.#.|#####|.#.#.",
    '*': ".....|#.#.#|.###.|#####|.###.|#.#.#|.....",
    '<': "...#.|..#..|.#...|#....|.#...|..#..|...#.",
    '>': ".#...|..#..|...#.|....#|...#.|..#..|.#...",
    '_': ".....|.....|.....|.....|.....|.....|#####",
    '·': ".....|.....|.....|.##..|.##..|.....|.....",
    '★': "..#..|..#..|#####|.###.|.###.|##.##|#...#",
}


def build_font(path):
    from fontTools.fontBuilder import FontBuilder
    from fontTools.pens.ttGlyphPen import TTGlyphPen
    UNIT = 100           # one pixel = 100 font units
    upm = 1000
    adv = 6 * UNIT       # 5 px glyph + 1 px gap
    order = ['.notdef', 'space']
    cmap = {32: 'space'}
    glyphs = {}
    metrics = {}

    def draw(rows):
        pen = TTGlyphPen(None)
        for r, row in enumerate(rows):
            for c, ch in enumerate(row):
                if ch != '#':
                    continue
                x0, y0 = c * UNIT, (6 - r) * UNIT
                pen.moveTo((x0, y0))
                pen.lineTo((x0, y0 + UNIT))
                pen.lineTo((x0 + UNIT, y0 + UNIT))
                pen.lineTo((x0 + UNIT, y0))
                pen.closePath()
        return pen.glyph()

    glyphs['.notdef'] = draw("#####|#...#|#...#|#...#|#...#|#...#|#####".split('|'))
    metrics['.notdef'] = (adv, 0)
    glyphs['space'] = TTGlyphPen(None).glyph()
    metrics['space'] = (adv - 2 * UNIT, 0)
    for ch, spec in G.items():
        name = 'g%04X' % ord(ch)
        order.append(name)
        glyphs[name] = draw(spec.split('|'))
        metrics[name] = (adv, 0)
        cmap[ord(ch)] = name
        if ch.isalpha() and ch.isupper():
            cmap[ord(ch.lower())] = name          # lower case shows as capitals (an all-caps display face)
    fb = FontBuilder(upm, isTTF=True)
    fb.setupGlyphOrder(order)
    fb.setupCharacterMap(cmap)
    fb.setupGlyf(glyphs)
    fb.setupHorizontalMetrics(metrics)
    fb.setupHorizontalHeader(ascent=800, descent=-200)
    fb.setupNameTable({'familyName': 'PKR Pixel', 'styleName': 'Regular'})
    fb.setupOS2(sTypoAscender=800, sTypoDescender=-200, usWinAscent=900, usWinDescent=200)
    fb.setupPost()
    fb.save(path)


ALL = {
    'ui_panel': lambda: panel(),
    'ui_button': lambda: button(),
    'ui_button_pressed': lambda: button(pressed=True),
    'ui_heart': lambda: heart(True),
    'ui_heart_empty': lambda: heart(False),
}
BORDERS = {'ui_panel': 16, 'ui_button': 10, 'ui_button_pressed': 10, 'ui_heart': 0, 'ui_heart_empty': 0}

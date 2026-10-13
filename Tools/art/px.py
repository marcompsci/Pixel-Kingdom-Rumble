"""
Pixel-art core for Pixel Kingdom Rumble's generated art (all original).

A Canvas holds RGBA pixels plus a "part id" per pixel, so we can:
  * shade shapes with a directional light (top-left) using hue-shifted ramps,
  * draw dark separation lines where one part overlaps another,
  * add a 1px outer outline that takes a little colour from what it wraps ("sel-out").
Coordinates are y-UP (0 = bottom row) to match Unity; save() flips for PNG.
"""
import colorsys, math
from PIL import Image

INK = (26, 18, 34)


def clamp(v, a=0, b=255):
    return max(a, min(b, int(round(v))))


def hex2rgb(h):
    h = h.lstrip('#')
    return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4))


def shift(c, dl=0.0, ds=0.0, dh=0.0):
    """Shift a colour in HLS space. dh in degrees."""
    r, g, b = [x / 255 for x in c[:3]]
    h, l, s = colorsys.rgb_to_hls(r, g, b)
    h = (h + dh / 360.0) % 1.0
    l = max(0.0, min(1.0, l + dl))
    s = max(0.0, min(1.0, s + ds))
    r, g, b = colorsys.hls_to_rgb(h, l, s)
    return (clamp(r * 255), clamp(g * 255), clamp(b * 255))


def mix(a, b, t):
    return tuple(clamp(a[i] + (b[i] - a[i]) * t) for i in range(3))


def hue_toward(c, target_deg, amount):
    """Rotate hue part of the way toward target hue (shadows -> blue/violet, lights -> yellow)."""
    r, g, b = [x / 255 for x in c[:3]]
    h, l, s = colorsys.rgb_to_hls(r, g, b)
    hd = h * 360
    d = ((target_deg - hd + 540) % 360) - 180
    return shift(c, dh=d * amount)


class Ramp:
    """Five-step hue-shifted colour ramp: deep shadow, shadow, base, light, highlight."""

    def __init__(self, base, contrast=1.0):
        base = base[:3]
        lum = sum(base) / 765
        k = contrast
        self.base = base
        self.shadow = shift(hue_toward(base, 250, 0.10 * k), dl=-0.13 * k, ds=0.05)
        self.deep = shift(hue_toward(base, 255, 0.18 * k), dl=-0.25 * k, ds=0.02)
        self.light = shift(hue_toward(base, 55, 0.08 * k), dl=0.09 * k * (1.0 if lum < 0.8 else 0.4), ds=0.03)
        self.hi = shift(hue_toward(base, 55, 0.14 * k), dl=0.18 * k * (1.0 if lum < 0.75 else 0.35), ds=0.0)

    def at(self, v):
        """v in -2..2 : -2 deep, -1 shadow, 0 base, 1 light, 2 highlight"""
        return [self.deep, self.shadow, self.base, self.light, self.hi][max(0, min(4, v + 2))]


class Canvas:
    def __init__(self, w, h):
        self.w, self.h = w, h
        self.px = [[None] * w for _ in range(h)]      # (r,g,b,a) or None
        self.part = [[0] * w for _ in range(h)]
        self.lock = [[False] * w for _ in range(h)]   # pixels that keep their colour (eyes, glints)
        self._next = 1

    # ---- basics -----------------------------------------------------------------------------
    def new_part(self):
        self._next += 1
        return self._next

    def inside(self, x, y):
        return 0 <= x < self.w and 0 <= y < self.h

    def get(self, x, y):
        return self.px[y][x] if self.inside(x, y) else None

    def put(self, x, y, c, part=None, lock=False, a=255):
        x, y = int(x), int(y)
        if not self.inside(x, y) or c is None:
            return
        if len(c) == 4:
            a = c[3]
        self.px[y][x] = (c[0], c[1], c[2], a)
        if part is not None:
            self.part[y][x] = part
        self.lock[y][x] = lock

    def blend(self, x, y, c, t):
        x, y = int(x), int(y)
        if not self.inside(x, y):
            return
        cur = self.px[y][x]
        if cur is None:
            return
        m = mix(cur, c, t)
        self.px[y][x] = (m[0], m[1], m[2], cur[3])

    # ---- shapes: each returns the set of covered pixels ------------------------------------------
    def mask_ellipse(self, cx, cy, rx, ry):
        pts = []
        for y in range(int(math.floor(cy - ry)) - 1, int(math.ceil(cy + ry)) + 2):
            for x in range(int(math.floor(cx - rx)) - 1, int(math.ceil(cx + rx)) + 2):
                dx = (x + 0.5 - cx) / max(rx, 0.01)
                dy = (y + 0.5 - cy) / max(ry, 0.01)
                if dx * dx + dy * dy <= 1.0:
                    pts.append((x, y))
        return pts

    def mask_rect(self, x0, y0, x1, y1, r=0):
        """inclusive pixel rect with optional corner radius"""
        pts = []
        for y in range(int(y0), int(y1) + 1):
            for x in range(int(x0), int(x1) + 1):
                if r > 0:
                    cx = min(max(x, x0 + r), x1 - r)
                    cy = min(max(y, y0 + r), y1 - r)
                    if (x - cx) ** 2 + (y - cy) ** 2 > r * r + 0.5:
                        continue
                pts.append((x, y))
        return pts

    def mask_poly(self, pts_in):
        xs = [p[0] for p in pts_in]
        ys = [p[1] for p in pts_in]
        out = []
        n = len(pts_in)
        for y in range(int(math.floor(min(ys))), int(math.ceil(max(ys))) + 1):
            for x in range(int(math.floor(min(xs))), int(math.ceil(max(xs))) + 1):
                px, py = x + 0.5, y + 0.5
                inside = False
                j = n - 1
                for i in range(n):
                    xi, yi = pts_in[i]
                    xj, yj = pts_in[j]
                    if (yi > py) != (yj > py) and px < (xj - xi) * (py - yi) / (yj - yi + 1e-9) + xi:
                        inside = not inside
                    j = i
                if inside:
                    out.append((x, y))
        return out

    def mask_capsule(self, x0, y0, x1, y1, r):
        """thick line with round ends (limbs)"""
        pts = []
        minx, maxx = int(min(x0, x1) - r - 1), int(max(x0, x1) + r + 1)
        miny, maxy = int(min(y0, y1) - r - 1), int(max(y0, y1) + r + 1)
        dx, dy = x1 - x0, y1 - y0
        L2 = dx * dx + dy * dy + 1e-9
        for y in range(miny, maxy + 1):
            for x in range(minx, maxx + 1):
                px, py = x + 0.5, y + 0.5
                t = max(0, min(1, ((px - x0) * dx + (py - y0) * dy) / L2))
                qx, qy = x0 + dx * t, y0 + dy * t
                if (px - qx) ** 2 + (py - qy) ** 2 <= r * r:
                    pts.append((x, y))
        return pts

    def line_pts(self, x0, y0, x1, y1):
        x0, y0, x1, y1 = int(round(x0)), int(round(y0)), int(round(x1)), int(round(y1))
        pts = []
        dx, dy = abs(x1 - x0), -abs(y1 - y0)
        sx = 1 if x0 < x1 else -1
        sy = 1 if y0 < y1 else -1
        err = dx + dy
        while True:
            pts.append((x0, y0))
            if x0 == x1 and y0 == y1:
                break
            e2 = 2 * err
            if e2 >= dy:
                err += dy
                x0 += sx
            if e2 <= dx:
                err += dx
                y0 += sy
        return pts

    # ---- painting ----------------------------------------------------------------------------
    def fill(self, pts, ramp, light=(-0.6, 0.8), shade=True, part=None, bias=0.0, soft=False, lock=False):
        """Fill a mask with a ramp, lit from `light` (dx,dy in mask-normalised space, y up)."""
        if not pts:
            return part
        if part is None:
            part = self.new_part()
        if isinstance(ramp, tuple):
            ramp = Ramp(ramp)
        xs = [p[0] for p in pts]
        ys = [p[1] for p in pts]
        cx = (min(xs) + max(xs) + 1) / 2
        cy = (min(ys) + max(ys) + 1) / 2
        rx = max(1.0, (max(xs) - min(xs) + 1) / 2)
        ry = max(1.0, (max(ys) - min(ys) + 1) / 2)
        S = set(pts)
        lx, ly = light
        for (x, y) in pts:
            if not shade:
                c = ramp.base
            else:
                nx = (x + 0.5 - cx) / rx
                ny = (y + 0.5 - cy) / ry
                v = nx * lx + ny * ly + bias
                # edge darkening on the far side from the light
                edge_far = ((x - 1, y) not in S and lx > 0) or ((x + 1, y) not in S and lx < 0) or ((x, y - 1) not in S and ly > 0)
                edge_near = ((x, y + 1) not in S and ly > 0) or ((x - 1, y) not in S and lx < 0)
                if v > 0.62:
                    k = 1
                elif v > -0.25:
                    k = 0
                elif v > -0.85:
                    k = -1
                else:
                    k = -2
                if edge_far and k > -2:
                    k -= 1
                if edge_near and k < 1 and v > -0.2:
                    k += 1
                if soft:
                    k = max(-1, min(1, k))
                c = ramp.at(k)
            self.put(x, y, c, part, lock)
        return part

    def flat(self, pts, color, part=None, lock=False):
        if part is None:
            part = self.new_part()
        for (x, y) in pts:
            self.put(x, y, color, part, lock)
        return part

    def specular(self, x, y, color=(255, 255, 240)):
        self.put(x, y, color, lock=True)

    # ---- passes ------------------------------------------------------------------------------
    def separate(self, strength=0.55):
        """Darken pixels of a part where it borders a *lower* part id drawn earlier (overlap lines)."""
        out = []
        for y in range(self.h):
            for x in range(self.w):
                c = self.px[y][x]
                if c is None or self.lock[y][x]:
                    continue
                p = self.part[y][x]
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    nx, ny = x + dx, y + dy
                    if not self.inside(nx, ny) or self.px[ny][nx] is None:
                        continue
                    q = self.part[ny][nx]
                    if q != p and q < p and q > 1:
                        out.append((x, y))
                        break
        for (x, y) in out:
            c = self.px[y][x]
            d = mix(c, INK, strength)
            self.px[y][x] = (d[0], d[1], d[2], c[3])

    def outline(self, ink=INK, tint=0.22, diag=False):
        """1px outer outline around the silhouette, slightly tinted by the neighbour's colour."""
        add = []
        for y in range(self.h):
            for x in range(self.w):
                if self.px[y][x] is not None:
                    continue
                nb = None
                dirs = [(1, 0), (-1, 0), (0, 1), (0, -1)]
                if diag:
                    dirs += [(1, 1), (-1, 1), (1, -1), (-1, -1)]
                for dx, dy in dirs:
                    c = self.get(x + dx, y + dy)
                    if c is not None and c[3] > 200:
                        nb = c
                        break
                if nb is not None:
                    add.append((x, y, mix(ink, shift(nb, dl=-0.35), tint)))
        for x, y, c in add:
            self.px[y][x] = (c[0], c[1], c[2], 255)
            self.part[y][x] = 1

    def rim(self, color=(255, 246, 214), t=0.35, side=(-1, 1)):
        """Light rim on the lit edge of the silhouette (top-left)."""
        sx, sy = side
        ch = []
        for y in range(self.h):
            for x in range(self.w):
                c = self.px[y][x]
                if c is None or self.part[y][x] == 1 or self.lock[y][x]:
                    continue
                n1 = self.get(x + sx, y)
                n2 = self.get(x, y + sy)
                if (n1 is None or (self.part[y + 0][x + sx] == 1 if self.inside(x + sx, y) else True)) and \
                   (n2 is None or (self.part[y + sy][x] == 1 if self.inside(x, y + sy) else True)):
                    ch.append((x, y))
        for x, y in ch:
            self.blend(x, y, color, t)

    def shadow_drop(self, color=(0, 0, 0), alpha=70, dx=0, dy=-1):
        pass

    # ---- output ------------------------------------------------------------------------------
    def image(self):
        img = Image.new('RGBA', (self.w, self.h), (0, 0, 0, 0))
        p = img.load()
        for y in range(self.h):
            for x in range(self.w):
                c = self.px[y][x]
                if c is not None:
                    p[x, self.h - 1 - y] = c
        return img

    def save(self, path):
        self.image().save(path)


def upscale(img, k):
    return img.resize((img.width * k, img.height * k), Image.NEAREST)

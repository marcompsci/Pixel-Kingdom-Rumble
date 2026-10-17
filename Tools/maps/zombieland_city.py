#!/usr/bin/env python3
"""
Zombie Land: the quarantined city of New Hollow (all original), seen from above. Generates the city layout as
JSON for the game (Resources/PKR/ZombieLand/city.json): ground tiles, buildings, landmarks, safehouses, mission
sites, vehicles, pickups, rot nests, relic cases and the six possible lairs. Deterministic (fixed seed).

Grid: W x H tiles, x to the right, y UP (Unity). One tile = one world unit.
Ground chars:
  r asphalt   h asphalt with a horizontal lane line   v asphalt with a vertical lane line
  c crosswalk (stripes run across a horizontal road)   C crosswalk (vertical road)
  s sidewalk   g grass   y yard   p park path   o cobblestone   i concrete   d dock planks
  w water (blocked)   b bridge deck   f fence (blocked)   x rubble   B building (blocked)   l parking lot
"""
import json, os, random, sys

W, H = 240, 192
ISLE_X = 200             # chapter 2: Blackwater Isle lies east of here, across the causeway
RIVER = (92, 98)          # y range of the river (inclusive)
SEA_X = 177               # sea from here east

DISTRICTS = [  # id, x0, y0, x1, y1 (inclusive), drawn in this order (later wins)
    ('suburbs', 2, 2, 63, 91),
    ('downtown', 64, 2, 127, 91),
    ('industrial', 128, 2, 176, 91),
    ('oldtown', 2, 99, 63, 189),
    ('park', 64, 99, 111, 189),
    ('university', 112, 99, 150, 189),
    ('harbor', 151, 99, 176, 189),
    ('isle', 204, 50, 237, 181),
]

V_AVENUES = [30, 64, 96, 128, 151]     # x of 4-wide north-south avenues (left edge)
H_AVENUES = [26, 58, 86, 102, 134, 166] # y of 4-wide east-west avenues (bottom edge)


class City:
    def __init__(self):
        self.g = [['g'] * W for _ in range(H)]
        self.buildings, self.props, self.sites = [], [], {}
        self.safehouses, self.vehicles, self.pickups = [], [], []
        self.nests, self.relics, self.lairs, self.missions = [], [], [], []
        self.doors = []
        self.rng = random.Random(1337)

    # -- grid helpers ----------------------------------------------------------------------------
    def inside(self, x, y):
        return 0 <= x < W and 0 <= y < H

    def get(self, x, y):
        return self.g[y][x] if self.inside(x, y) else 'f'

    def set(self, x, y, c):
        if self.inside(x, y):
            self.g[y][x] = c

    def fill(self, x, y, w, h, c, only=None):
        for yy in range(y, y + h):
            for xx in range(x, x + w):
                if self.inside(xx, yy) and (only is None or self.g[yy][xx] in only):
                    self.g[yy][xx] = c

    def area_is(self, x, y, w, h, allowed):
        for yy in range(y, y + h):
            for xx in range(x, x + w):
                if not self.inside(xx, yy) or self.g[yy][xx] not in allowed:
                    return False
        return True

    def district_of(self, x, y):
        if x >= ISLE_X:
            return 'isle'
        if x >= SEA_X:
            return 'harbor'
        best = 'suburbs'
        for d, x0, y0, x1, y1 in DISTRICTS:
            if x0 <= x <= x1 and y0 <= y <= y1:
                best = d
        if RIVER[0] <= y <= RIVER[1]:
            return 'downtown' if 64 <= x < 128 else best
        return best

    # -- placement ------------------------------------------------------------------------------
    def building(self, x, y, w, h, style, name=None, ground=('g', 'y', 's', 'i', 'o', 'l', 'p', 'x', 'd')):
        if not self.area_is(x, y, w, h, ground):
            return None
        self.fill(x, y, w, h, 'B')
        b = dict(x=x, y=y, w=w, h=h, style=style)
        if name:
            b['name'] = name
        self.buildings.append(b)
        return b

    def prop(self, kind, x, y, solid=False, rot=0, w=1, h=1):
        self.props.append(dict(k=kind, x=round(x, 2), y=round(y, 2), solid=1 if solid else 0, r=rot, w=w, h=h))

    def site(self, key, x, y):
        self.sites[key] = [round(x, 2), round(y, 2)]

    def walkable(self, x, y):
        return self.get(x, y) not in ('B', 'w', 'f')

    def near_free(self, x, y, allowed=('s', 'g', 'y', 'p', 'o', 'i', 'l', 'd', 'x', 'r', 'h', 'v', 'b', 'c', 'C')):
        """The closest walkable tile to (x, y) (spiral search)."""
        x, y = int(x), int(y)
        for rad in range(0, 12):
            for dy in range(-rad, rad + 1):
                for dx in range(-rad, rad + 1):
                    if max(abs(dx), abs(dy)) != rad:
                        continue
                    if self.get(x + dx, y + dy) in allowed and not self.occupied(x + dx, y + dy):
                        return x + dx + 0.5, y + dy + 0.5
        return x + 0.5, y + 0.5

    def occupied(self, x, y):
        for p in self.props:
            if p['solid'] and abs(p['x'] - (x + 0.5)) < 1.2 and abs(p['y'] - (y + 0.5)) < 1.2:
                return True
        return False

    # -- build ----------------------------------------------------------------------------------
    def build(self):
        self.ground()
        self.roads()
        self.river_and_sea()
        self.blocks()
        self.landmarks()
        self.park()
        self.harbor()
        self.dress()
        self.place_missions()
        self.place_safehouses()
        self.place_vehicles()
        self.place_pickups()
        self.place_nests()
        self.isle()
        # chapter 3: the freight lift down to the Larkin sublevel, beside Larkin Labs
        lx, ly = self.near_free(131, 138, allowed=('s', 'g', 'y', 'p', 'i', 'l'))
        self.site('lab_lift', lx, ly)
        self.border()
        return self

    def ground(self):
        base = {'suburbs': 'g', 'downtown': 's', 'industrial': 'i', 'oldtown': 'o', 'park': 'g', 'university': 'g', 'harbor': 'i', 'isle': 'w'}
        for y in range(H):
            for x in range(W):
                self.g[y][x] = base[self.district_of(x, y)]

    def road(self, x, y, w, h, vertical):
        for yy in range(y, y + h):
            for xx in range(x, x + w):
                if not self.inside(xx, yy):
                    continue
                c = self.g[yy][xx]
                if c in ('r', 'h', 'v', 'c', 'C'):
                    self.g[yy][xx] = 'r'  # intersections stay plain
                    continue
                if vertical:
                    self.g[yy][xx] = 'v' if w >= 4 and xx - x == w // 2 and yy % 4 < 2 else 'r'
                else:
                    self.g[yy][xx] = 'h' if w >= 4 and yy - y == h // 2 and xx % 4 < 2 else 'r'

    def roads(self):
        for x in V_AVENUES:
            self.road(x, 2, 4, H - 4, True)
        for y in H_AVENUES:
            self.road(2, y, SEA_X - 2, 4, False)
        # Side streets (2 wide) inside districts
        for x in (12, 46, 80, 112, 140):
            for y0, y1 in ((2, RIVER[0] - 1), (RIVER[1] + 1, H - 3)):
                if x == 112 and y0 > 90:
                    continue
                self.road(x, y0, 2, y1 - y0 + 1, True)
        for y in (10, 42, 72, 118, 150, 180):
            self.road(2, y, SEA_X - 2, 2, False)
        # crosswalk stripes beside avenue intersections
        for x in V_AVENUES:
            for y in H_AVENUES:
                for dx in range(4):
                    for yy in (y - 1, y + 4):
                        if self.get(x + dx, yy) == 'r' or self.get(x + dx, yy) == 'v':
                            self.set(x + dx, yy, 'C')
                for dy in range(4):
                    for xx in (x - 1, x + 4):
                        if self.get(xx, y + dy) in ('r', 'h'):
                            self.set(xx, y + dy, 'c')

    def river_and_sea(self):
        for y in range(RIVER[0], RIVER[1] + 1):
            for x in range(2, SEA_X):
                c = self.g[y][x]
                self.g[y][x] = 'b' if c in ('r', 'h', 'v', 'c', 'C') and self.is_bridge_col(x) else 'w'
        for y in range(H):
            for x in range(SEA_X, W):
                self.g[y][x] = 'w'

    def is_bridge_col(self, x):
        for ax in V_AVENUES:
            if ax <= x < ax + 4:
                return True
        return False

    def road_tile(self, x, y):
        return self.get(x, y) in ('r', 'h', 'v', 'c', 'C', 'b')

    def blocks(self):
        """Find each block (maximal non-road, non-water region rectangles) and fill it by district style."""
        seen = [[False] * W for _ in range(H)]
        for y in range(2, H - 2):
            for x in range(2, W - 2):
                if seen[y][x] or self.road_tile(x, y) or self.get(x, y) in ('w', 'f'):
                    continue
                # grow a rectangle right then up
                x1 = x
                while x1 + 1 < W - 2 and not self.road_tile(x1 + 1, y) and self.get(x1 + 1, y) not in ('w', 'f'):
                    x1 += 1
                y1 = y
                while y1 + 1 < H - 2 and all(not self.road_tile(xx, y1 + 1) and self.get(xx, y1 + 1) not in ('w', 'f') for xx in range(x, x1 + 1)):
                    y1 += 1
                for yy in range(y, y1 + 1):
                    for xx in range(x, x1 + 1):
                        seen[yy][xx] = True
                self.block(x, y, x1 - x + 1, y1 - y + 1)

    def block(self, x, y, w, h):
        d = self.district_of(x + w // 2, y + h // 2)
        rng = self.rng
        # sidewalk ring
        ring = {'suburbs': 's', 'downtown': 's', 'industrial': 's', 'oldtown': 's', 'park': 'p', 'university': 's', 'harbor': 'i'}[d]
        for xx in range(x, x + w):
            self.set(xx, y, ring); self.set(xx, y + h - 1, ring)
        for yy in range(y, y + h):
            self.set(x, yy, ring); self.set(x + w - 1, yy, ring)
        ix, iy, iw, ih = x + 1, y + 1, w - 2, h - 2
        if iw < 3 or ih < 3:
            return
        if d == 'suburbs':
            self.fill(ix, iy, iw, ih, 'y')
            # houses in a row along the bottom and top of the block
            for row_y in range(iy + 1, iy + ih - 5, 7):
                xx = ix + 1
                while xx + 6 <= ix + iw - 1:
                    hw, hh = rng.choice([(5, 5), (6, 5), (5, 6), (6, 6)])
                    if row_y + hh > iy + ih - 1 or row_y < iy:
                        break
                    self.building(xx, row_y, hw, hh, rng.choice(['house_red', 'house_blue', 'house_green', 'house_gray']))
                    if rng.random() < 0.5:
                        self.prop('tree', xx + hw + 0.5, row_y + 1.5, solid=True)
                    xx += hw + rng.randint(2, 3)
        elif d == 'downtown':
            self.fill(ix, iy, iw, ih, 's')
            if rng.random() < 0.18 and iw > 8 and ih > 8:     # a plaza
                self.fill(ix + 1, iy + 1, iw - 2, ih - 2, 'l')
                return
            self.split_fill(ix, iy, iw, ih, ['tower_gray', 'tower_blue', 'tower_tan', 'tower_glass'], 7, 14, gap=2)
        elif d == 'industrial':
            self.fill(ix, iy, iw, ih, 'i')
            self.split_fill(ix, iy, iw, ih, ['warehouse_gray', 'warehouse_rust', 'warehouse_green'], 9, 16, gap=3)
        elif d == 'oldtown':
            self.fill(ix, iy, iw, ih, 'o')
            self.split_fill(ix, iy, iw, ih, ['old_terracotta', 'old_slate', 'old_brick'], 5, 9, gap=1)
        elif d == 'university':
            self.fill(ix, iy, iw, ih, 'g')
            self.split_fill(ix + 1, iy + 1, iw - 2, ih - 2, ['campus_brick', 'campus_stone'], 8, 13, gap=4)
        elif d == 'harbor':
            self.fill(ix, iy, iw, ih, 'i')
            # container stacks
            for yy in range(iy + 1, iy + ih - 2, 4):
                for xx in range(ix + 1, ix + iw - 5, 7):
                    if rng.random() < 0.7:
                        self.building(xx, yy, 5, 2, rng.choice(['container_red', 'container_blue', 'container_green', 'container_orange']), ground=('i',))
        elif d == 'park':
            self.fill(ix, iy, iw, ih, 'g')

    def split_fill(self, x, y, w, h, styles, lo, hi, gap):
        """Pack buildings into a block in rows, leaving alleys."""
        rng = self.rng
        yy = y
        while yy + lo <= y + h:
            bh = min(rng.randint(lo, hi), y + h - yy)
            if bh < lo:
                break
            xx = x
            while xx + lo <= x + w:
                bw = min(rng.randint(lo, hi), x + w - xx)
                if bw < lo:
                    break
                self.building(xx, yy, bw, bh, rng.choice(styles))
                xx += bw + gap
            yy += bh + gap

    # -- landmarks ------------------------------------------------------------------------------
    def clear(self, x, y, w, h, ground):
        """Knock down whatever was generated here and lay ground (landmarks need room)."""
        keep, gone = [], []
        for b in self.buildings:
            if b['x'] + b['w'] <= x or b['x'] >= x + w or b['y'] + b['h'] <= y or b['y'] >= y + h:
                keep.append(b)
            else:
                gone.append(b)
        self.buildings = keep
        for b in gone:  # the whole footprint of anything knocked down goes back to open ground
            for yy in range(b['y'], b['y'] + b['h']):
                for xx in range(b['x'], b['x'] + b['w']):
                    if self.get(xx, yy) == 'B':
                        self.set(xx, yy, ground)
        for yy in range(y, y + h):
            for xx in range(x, x + w):
                if self.get(xx, yy) not in ('w', 'f') and not self.road_tile(xx, yy):
                    self.set(xx, yy, ground)

    def landmark(self, key, x, y, w, h, style, name, ground, door):
        self.clear(x - 2, y - 2, w + 4, h + 4, ground)
        self.fill(x, y, w, h, ground)          # landmarks may sit over a side street
        b = self.building(x, y, w, h, style, name)
        dx, dy = door
        self.site(key, x + dx, y + dy)
        return b

    def landmarks(self):
        # Maple Row
        self.landmark('pharmacy', 16, 30, 10, 7, 'pharmacy', 'MAPLE PHARMACY', 'l', (5, -1.5))
        self.landmark('gunshop', 36, 62, 9, 7, 'gunshop', "HANK'S GUNS", 'l', (4.5, -1.5))
        # Downtown
        self.landmark('mast', 104, 44, 6, 6, 'mast', 'KNHW RADIO', 's', (3, -1.5))
        self.landmark('lair_subway', 70, 64, 14, 10, 'subway', 'UNION SUBWAY DEPOT', 's', (7, -1.5))
        self.landmark('firestation', 100, 4, 12, 9, 'firestation', 'ENGINE 7', 's', (6, 10.5))
        self.landmark('precinct', 70, 30, 12, 9, 'precinct', '4TH PRECINCT', 's', (6, -1.5))
        # Ironside
        self.landmark('lair_waterworks', 154, 30, 18, 14, 'waterworks', 'IRONSIDE WATERWORKS', 'i', (9, -1.5))
        self.landmark('raildepot', 134, 62, 14, 10, 'raildepot', 'RAIL DEPOT', 'i', (7, -1.5))
        self.landmark('iceplant', 156, 6, 12, 10, 'iceplant', 'POLAR ICE PLANT', 'i', (6, 11.5))
        # Old Quarter
        self.landmark('lair_cathedral', 16, 138, 14, 16, 'cathedral', "ST. BRIGID'S CATHEDRAL", 'o', (7, -1.5))
        self.landmark('busstop', 36, 108, 6, 3, 'bus', 'BUS 19', 'o', (3, -1.5))
        self.landmark('forge', 48, 170, 8, 7, 'forge', 'OLD FORGE', 'o', (4, -1.5))
        # College Hill
        self.landmark('lair_labs', 116, 140, 14, 10, 'labs', 'LARKIN LABS', 'g', (7, -1.5))
        self.landmark('lair_stadium', 116, 106, 26, 20, 'stadium', 'HOLLOW FIELD', 'g', (13, -1.5))
        self.landmark('substation', 136, 172, 8, 7, 'substation', 'SUBSTATION 9', 'g', (4, -1.5))
        # Saltmouth Harbor
        self.landmark('lighthouse', 168, 182, 5, 5, 'lighthouse', 'LIGHTHOUSE', 'd', (2.5, -1.5))

    def park(self):
        rng = self.rng
        x0, y0, x1, y1 = 64, 99, 111, 189
        # pond
        for y in range(146, 160):
            for x in range(70, 88):
                if ((x - 79) / 9.0) ** 2 + ((y - 153) / 7.0) ** 2 < 1 and not self.road_tile(x, y):
                    self.set(x, y, 'w')
        # winding paths
        for y in range(y0, y1):
            x = int(88 + 6 * __import__('math').sin(y / 9.0))
            for dx in range(2):
                if not self.road_tile(x + dx, y) and self.get(x + dx, y) == 'g':
                    self.set(x + dx, y, 'p')
        # rose garden (the nest mission)
        for y in range(172, 180):
            for x in range(68, 80):
                if self.get(x, y) == 'g':
                    self.set(x, y, 'p' if (x + y) % 3 else 'g')
        self.site('roses', 74, 176)
        for _ in range(140):
            x, y = rng.randint(x0 + 1, x1 - 1), rng.randint(y0 + 1, y1 - 1)
            if self.get(x, y) == 'g' and not self.occupied(x, y):
                self.prop(rng.choice(['tree', 'tree', 'pine', 'bush']), x + 0.5, y + 0.5, solid=True)

    def harbor(self):
        # piers out into the sea
        for py in (110, 130, 150, 170):
            for x in range(SEA_X - 1, SEA_X + 9):
                for dy in range(3):
                    self.set(x, py + dy, 'd')
        # the freighter moored at the middle pier
        self.fill(SEA_X + 1, 136, 10, 12, 'w')
        b = dict(x=SEA_X + 1, y=135, w=10, h=13, style='freighter', name='THE MARIGOLD')
        self.fill(b['x'], b['y'], b['w'], b['h'], 'B')
        self.buildings.append(b)
        for x in range(SEA_X - 1, SEA_X + 1):
            self.set(x, 140, 'd'); self.set(x, 141, 'd')
        self.site('lair_freighter', SEA_X - 1.5, 140.5)
        self.site('docks', 165, 152)
        self.site('lighthouse_road', 166, 176)
        # cranes along the quay (solid props)
        for y in range(104, 186, 14):
            self.prop('crane', SEA_X - 3, y + 0.5, solid=True, w=2, h=2)

    def dress(self):
        rng = self.rng
        # wrecked cars, barricades, trash, blood and lamps along streets
        for _ in range(240):
            x, y = rng.randint(4, SEA_X - 4), rng.randint(4, H - 5)
            c = self.get(x, y)
            if c in ('r', 'h', 'v') and not self.occupied(x, y) and self.get(x + 1, y) in ('r', 'h', 'v'):
                vertical = self.get(x, y + 2) in ('r', 'v', 'h') and self.get(x, y - 2) in ('r', 'v', 'h') and self.get(x + 3, y) not in ('r', 'h')
                self.prop(rng.choice(['wreck_red', 'wreck_blue', 'wreck_white', 'wreck_burnt']), x + 0.5, y + 0.5, solid=True,
                          rot=90 if vertical else 0, w=2, h=1)
        for _ in range(500):
            x, y = rng.randint(4, SEA_X - 4), rng.randint(4, H - 5)
            if self.get(x, y) in ('r', 'h', 'v', 's', 'i', 'o', 'l'):
                self.prop(rng.choice(['blood', 'blood', 'trash', 'paper', 'tire', 'cone']), x + rng.random(), y + rng.random())
        for _ in range(120):
            x, y = rng.randint(4, SEA_X - 4), rng.randint(4, H - 5)
            if self.get(x, y) == 's' and not self.occupied(x, y):
                self.prop('lamp', x + 0.5, y + 0.5, solid=True)
        # army barricades at the city edge gates
        for x in V_AVENUES:
            for y in (3, H - 4):
                for dx in range(4):
                    self.prop('barricade', x + dx + 0.5, y + 0.5, solid=True)

    # -- missions, safehouses, loot -------------------------------------------------------------
    def nearby_points(self, cx, cy, n, rmin, rmax, seed):
        rng = random.Random(seed)
        pts = []
        tries = 0
        while len(pts) < n and tries < 2000:
            tries += 1
            import math
            a = rng.random() * math.tau
            d = rmin + rng.random() * (rmax - rmin)
            x, y = int(cx + math.cos(a) * d), int(cy + math.sin(a) * d)
            if self.walkable(x, y) and self.get(x, y) not in ('w',) and not self.occupied(x, y) \
                    and all(abs(px - x) + abs(py - y) > 6 for px, py in pts):
                pts.append((x + 0.5, y + 0.5))
        return [dict(x=round(a, 2), y=round(b, 2)) for a, b in pts]

    def mission(self, mid, key, extra=None, dest=None):
        x, y = self.sites[key]
        x, y = self.near_free(x, y)
        m = dict(id=mid, x=x, y=y)
        if extra:
            m['points'] = self.nearby_points(x, y, extra[0], extra[1], extra[2], hash_s(mid))
        if dest:
            dx, dy = self.near_free(*self.sites[dest])
            m['dest'] = dict(x=dx, y=dy)
            m['hasDest'] = 1
        self.missions.append(m)

    def place_missions(self):
        self.site('rosegate', 74, 176)
        self.mission('m_medicine', 'pharmacy', extra=(3, 4, 14))
        self.mission('b_gunshop', 'gunshop')
        self.mission('m_bus', 'busstop')
        self.mission('m_bells', 'lair_cathedral')
        self.mission('m_roses', 'roses')
        self.mission('m_broadcast', 'mast')
        self.mission('m_engine7', 'firestation', dest='docks')
        self.mission('b_butcher', 'precinct')
        self.mission('m_dockmaster', 'docks')
        self.mission('b_joyride', 'docks', dest='lighthouse_road')
        self.mission('m_power', 'raildepot', extra=(3, 8, 20))
        self.mission('b_quartermaster', 'raildepot')
        self.mission('m_notes', 'lair_labs', extra=(3, 5, 15))
        self.mission('m_stadium', 'lair_stadium')
        # the lairs (door points)
        for key in ('lair_cathedral', 'lair_subway', 'lair_freighter', 'lair_stadium', 'lair_waterworks', 'lair_labs'):
            x, y = self.near_free(*self.sites[key])
            self.lairs.append(dict(id=key, x=x, y=y))
        # relic cases
        for relic, key in (('Sunflare', 'lighthouse'), ('Frostbite', 'iceplant'), ('Arc', 'substation'), ('Cinder', 'forge')):
            x, y = self.near_free(*self.sites[key])
            self.relics.append(dict(relic=relic, x=x, y=y))

    def place_safehouses(self):
        spots = [('safe_maple', 'MAPLE ROW SAFEHOUSE', 20, 18), ('safe_downtown', 'CITY HALL SAFEHOUSE', 90, 50),
                 ('safe_ironside', 'IRONSIDE SAFEHOUSE', 140, 20), ('safe_oldtown', 'OLD QUARTER SAFEHOUSE', 40, 128),
                 ('safe_park', 'PARK RANGER STATION', 96, 125), ('safe_college', 'COLLEGE HILL SAFEHOUSE', 140, 155),
                 ('safe_harbor', 'HARBOR SAFEHOUSE', 162, 120)]
        for sid, name, x, y in spots:
            fx, fy = self.near_free(x, y, allowed=('s', 'i', 'o', 'l', 'p', 'g', 'y'))
            self.safehouses.append(dict(id=sid, name=name, x=fx, y=fy, district=self.district_of(int(fx), int(fy))))
        # start: in front of the Maple Row safehouse
        s = self.safehouses[0]
        self.start = [s['x'] + 1.5, s['y']]

    def place_vehicles(self):
        rng = self.rng
        kinds = ['Car'] * 5 + ['Truck'] * 2 + ['Bike'] * 2 + ['Motorcycle'] * 2
        placed = []
        tries = 0
        while len(placed) < 44 and tries < 5000:
            tries += 1
            x, y = rng.randint(4, SEA_X - 4), rng.randint(4, H - 5)
            if self.get(x, y) not in ('r', 'h', 'v', 'l'):
                continue
            if self.occupied(x, y) or any(abs(px - x) + abs(py - y) < 10 for px, py in placed):
                continue
            vertical = self.get(x, y + 3) in ('r', 'v') and self.get(x, y - 3) in ('r', 'v') and self.get(x + 3, y) not in ('r', 'h', 'v')
            ang = rng.choice([90, 270]) if vertical else rng.choice([0, 180])
            self.vehicles.append(dict(k=rng.choice(kinds), x=x + 0.5, y=y + 0.5, a=ang))
            placed.append((x, y))
        # a guaranteed car and bike by the start, and the mission vehicles
        sx, sy = self.start
        cx, cy = self.near_free(sx + 4, sy, allowed=('r', 'h', 'v', 'l', 's'))
        self.vehicles.append(dict(k='Car', x=cx, y=cy, a=0))
        bx, by = self.near_free(sx - 3, sy, allowed=('r', 'h', 'v', 'l', 's'))
        self.vehicles.append(dict(k='Bike', x=bx, y=by, a=90))

    def place_pickups(self):
        rng = self.rng
        n = 0
        def drop(kind, count, where=None):
            nonlocal n
            got, tries = 0, 0
            while got < count and tries < 4000:
                tries += 1
                if where:
                    x0, y0, x1, y1 = where
                    x, y = rng.randint(x0, x1), rng.randint(y0, y1)
                else:
                    x, y = rng.randint(4, SEA_X - 4), rng.randint(4, H - 5)
                if self.get(x, y) not in ('s', 'i', 'o', 'l', 'y', 'p', 'd') or self.occupied(x, y):
                    continue
                # next to a building wall: loot sits by doors
                if not any(self.get(x + dx, y + dy) == 'B' for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                    continue
                self.pickups.append(dict(id=f'p{n}', k=kind, x=x + 0.5, y=y + 0.5))
                n += 1
                got += 1
        drop('ammo', 70)
        drop('medkit', 22)
        drop('armor', 12)
        drop('street_smg', 1, (64, 2, 127, 91))
        drop('street_smg', 1, (2, 99, 63, 189))
        drop('riot_shotgun', 1, (128, 2, 176, 91))
        drop('ranger_rifle', 1, (112, 99, 150, 189))
        drop('patrol_lmg', 1, (151, 99, 176, 189))
        drop('thumper', 1, (128, 2, 176, 91))
        drop('assault_rifle', 1, (64, 2, 127, 91))

    def place_nests(self):
        spots = [(50, 80, 'suburbs'), (80, 14, 'downtown'), (118, 76, 'downtown'), (140, 44, 'industrial'), (168, 70, 'industrial'),
                 (24, 120, 'oldtown'), (52, 150, 'oldtown'), (100, 170, 'park'), (130, 182, 'university'), (160, 160, 'harbor')]
        for i, (x, y, d) in enumerate(spots):
            fx, fy = self.near_free(x, y, allowed=('g', 'i', 'o', 's', 'l', 'y', 'p'))
            self.nests.append(dict(id=f'nest_{i}', x=fx, y=fy, district=d))
            for k in range(10):
                self.prop('rot', fx + self.rng.uniform(-3, 3), fy + self.rng.uniform(-3, 3))

    # -- chapter 2: Blackwater Isle ------------------------------------------------------------------
    def isle(self):
        """Blackwater Isle (east, across a causeway sealed until the First Turned falls). Drawn last with its own
        random generator so the mainland stays exactly as it was."""
        rng = random.Random(4040)
        X0, X1, Y0, Y1 = 204, 237, 52, 179
        # the island: grass and salt marsh, a shore of planks in places
        for y in range(Y0, Y1 + 1):
            for x in range(X0, X1 + 1):
                edge = min(x - X0, X1 - x, y - Y0, Y1 - y)
                wobble = (hash_s(f'{x},{y}') % 3)
                if edge + wobble < 2:
                    continue  # ragged coast: water
                self.g[y][x] = 'y' if (x * 7 + y * 3) % 11 == 0 else 'g'
        # the causeway (bridge deck) from the harbor quay, with the army's quarantine line (D) at the mainland end
        self.fill(SEA_X, 117, X0 - SEA_X + 1, 4, 'b')
        self.fill(SEA_X + 2, 117, 2, 4, 'D')
        self.doors.append(dict(id='isle_bridge', name='QUARANTINE LINE', x=SEA_X + 2, y=117, w=2, h=4, cost=0, opens='isle'))
        # roads: the island road north-south and the causeway road east-west
        self.road(220, Y0 + 3, 4, Y1 - Y0 - 6, True)
        self.road(X0, 117, X1 - X0 - 1, 4, False)
        # fishing village (east, cobbles) with the ferry dock
        self.fill(225, 98, 11, 38, 'o')
        for (hx, hy, st) in ((226, 124, 'house_blue'), (231, 124, 'house_gray'), (226, 106, 'house_green'), (231, 106, 'house_red'),
                             (226, 130, 'house_red'), (231, 130, 'house_blue')):
            self.building(hx, hy, 4, 4, st)
        self.fill(228, 100, 10, 3, 'd')   # ferry dock out to the water
        self.building(229, 112, 6, 4, 'old_slate', 'NET SHED')
        self.site('netshed', 231.5, 110.5)
        # the cannery (east, south)
        self.fill(224, 60, 12, 34, 'i')
        self.building(225, 72, 11, 9, 'warehouse_rust', 'BLACKWATER CANNERY')
        self.site('cannery', 230.5, 70.5)
        self.prop('crane', 233.5, 64.5, solid=True, w=2, h=2)
        # the penitentiary (west, north) and the prison yard
        self.fill(205, 128, 14, 44, 'i')
        self.building(206, 146, 12, 18, 'campus_stone', 'BLACKWATER PENITENTIARY')
        self.site('prison', 212.0, 143.5)
        self.site('tide_gate', 211.5, 166.5)
        for x in range(206, 218, 2):
            self.prop('barricade', x + 0.5, 131.5, solid=True)   # the old yard fence
        self.fill(210, 131, 4, 1, 'i')                          # its gate
        # the lighthouse (north tip) and its lot
        self.fill(222, 160, 10, 16, 'i')
        self.landmark('isle_lighthouse', 225, 168, 5, 5, 'lighthouse', 'NORTH BEACON', 'i', (2.5, -1.5))
        self.site('beacon', 227.5, 166.5)
        self.site('lighthouse_lot', 226.0, 162.0)
        # marsh (west, south): trees, the smuggler's stash
        for _ in range(60):
            x, y = rng.randint(X0 + 2, 218), rng.randint(Y0 + 3, 114)
            if self.get(x, y) in ('g', 'y') and not self.occupied(x, y):
                self.prop(rng.choice(['tree', 'pine', 'bush', 'bush']), x + 0.5, y + 0.5, solid=True)
        self.fill(208, 92, 6, 5, 'l')
        self.site('stash', 210.5, 94.5)
        # chapter 2 missions and the tide gate
        self.mission('c2_ferry', 'netshed')
        self.mission('c2_cannery', 'cannery')
        self.mission('c2_beacon', 'beacon')
        self.mission('c2_cellblock', 'prison', extra=(3, 4, 12))
        self.mission('c2_smuggler', 'stash', dest='lighthouse_lot')
        tx, ty = self.near_free(*self.sites['tide_gate'])
        self.lairs.append(dict(id='tide_gate', x=tx, y=ty))
        # a safehouse at the end of the causeway
        fx, fy = self.near_free(209, 123, allowed=('g', 'y', 'i', 'o', 's'))
        self.safehouses.append(dict(id='safe_isle', name='BLACKWATER SAFEHOUSE', x=fx, y=fy, district='isle'))
        # nests, vehicles, loot
        for i, (x, y) in enumerate(((214, 80), (232, 140), (209, 158))):
            nx, ny = self.near_free(x, y, allowed=('g', 'i', 'o', 'y', 'l'))
            self.nests.append(dict(id=f'nest_isle_{i}', x=nx, y=ny, district='isle'))
            for k in range(8):
                self.prop('rot', nx + rng.uniform(-3, 3), ny + rng.uniform(-3, 3))
        for k, x, y, a in (('Car', 216.5, 125.5, 0), ('Motorcycle', 221.5, 140.5, 90), ('Truck', 226.5, 88.5, 0)):
            self.vehicles.append(dict(k=k, x=x, y=y, a=a))
        n = 0
        for kind, count in (('ammo', 12), ('medkit', 5), ('armor', 3)):
            got, tries = 0, 0
            while got < count and tries < 4000:
                tries += 1
                x, y = rng.randint(X0 + 2, X1 - 2), rng.randint(Y0 + 2, Y1 - 2)
                if self.get(x, y) not in ('i', 'o', 'l', 'y', 'g') or self.occupied(x, y):
                    continue
                if not any(self.get(x + dx, y + dy) == 'B' for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                    continue
                self.pickups.append(dict(id=f'pi{n}', k=kind, x=x + 0.5, y=y + 0.5))
                n += 1
                got += 1
        # lamps along the island road
        for y in range(Y0 + 6, Y1 - 6, 9):
            if self.get(219, y) in ('g', 'y', 'i', 'o'):
                self.prop('lamp', 219.5, y + 0.5, solid=True)

    def border(self):
        for y in range(H):
            for x in range(W):
                if x < 2 or y < 2 or y >= H - 2 or (x >= W - 2):
                    if self.g[y][x] != 'w':
                        self.g[y][x] = 'f'

    # -- output ---------------------------------------------------------------------------------
    def to_json(self):
        rows = [''.join(self.g[y]) for y in range(H)]
        return dict(
            w=W, h=H, rows=rows, start=dict(x=self.start[0], y=self.start[1]),
            districts=[dict(id=d, x0=x0, y0=y0, x1=x1, y1=y1) for d, x0, y0, x1, y1 in DISTRICTS],
            buildings=self.buildings, props=self.props, safehouses=self.safehouses, vehicles=self.vehicles,
            pickups=self.pickups, nests=self.nests, relics=self.relics, lairs=self.lairs, missions=self.missions, doors=self.doors,
            sites=[dict(key=k, x=v[0], y=v[1]) for k, v in self.sites.items()])


def hash_s(s):
    """Deterministic string hash (Python's hash() is salted per run)."""
    h = 0
    for ch in s:
        h = (h * 131 + ord(ch)) & 0x7fffffff
    return h


def build():
    return City().build()


if __name__ == '__main__':
    c = build()
    out = sys.argv[1] if len(sys.argv) > 1 else None
    data = c.to_json()
    if out:
        with open(out, 'w') as f:
            json.dump(data, f, separators=(',', ':'))
    print(len(c.buildings), 'buildings,', len(c.props), 'props,', len(c.vehicles), 'vehicles,', len(c.pickups), 'pickups,',
          len(c.missions), 'missions,', len(c.safehouses), 'safehouses')

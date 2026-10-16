#!/usr/bin/env python3
"""
LAST STAND: HOLLOW BLOCK (all original) — one New Hollow city block for round-based survival, seen from above.
Writes Resources/PKR/ZombieLand/block.json in the same format as city.json plus the Last Stand pieces:
zones, doors (debris you pay to clear), stations (wall guns, perk machines, the forge), box spots, spawns per zone
and the three hidden graystone fragments (the Easter egg).

Grid: W x H tiles, x right, y UP. Same ground chars as the city, plus:
  D debris barricade (blocked until bought; the ground under it is road)

Layout (y up):
  y 1-14   FORECOURT (start): the street, the Last Chance gas station and its forecourt
  y 15-17  a row of buildings with two debris gaps: to the LOT (west) and to MAPLE STREET (east)
  y 18-44  the LOT (west, parking) · the YARD (centre, a little park) · MAPLE STREET (east, the diner)
           walls between them with debris gaps from the Lot and from Maple Street into the Yard
"""
import json, random, sys

W, H = 60, 46


class Block:
    def __init__(self):
        self.g = [['B'] * W for _ in range(H)]
        self.buildings, self.props = [], []
        self.zones, self.doors, self.stations, self.spawns = [], [], [], []
        self.box_spots, self.eggs = [], []
        self.rng = random.Random(4242)

    def inside(self, x, y):
        return 0 <= x < W and 0 <= y < H

    def get(self, x, y):
        return self.g[y][x] if self.inside(x, y) else 'f'

    def fill(self, x0, y0, x1, y1, c):
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                if self.inside(x, y):
                    self.g[y][x] = c

    def building(self, x0, y0, x1, y1, style, name=None):
        self.fill(x0, y0, x1, y1, 'B')
        b = dict(x=x0, y=y0, w=x1 - x0 + 1, h=y1 - y0 + 1, style=style)
        if name:
            b['name'] = name
        self.buildings.append(b)

    def prop(self, kind, x, y, solid=False, rot=0, w=1, h=1):
        self.props.append(dict(k=kind, x=round(x, 2), y=round(y, 2), solid=1 if solid else 0, r=rot, w=w, h=h))

    def door(self, did, name, x0, y0, x1, y1, cost, opens):
        self.fill(x0, y0, x1, y1, 'D')
        self.doors.append(dict(id=did, name=name, x=x0, y=y0, w=x1 - x0 + 1, h=y1 - y0 + 1, cost=cost, opens=opens))

    def station(self, k, sid, x, y, cost=0, zone=''):
        self.stations.append(dict(k=k, id=sid, x=x, y=y, cost=cost, zone=zone))

    def build(self):
        # border fence
        self.fill(0, 0, W - 1, 0, 'f'); self.fill(0, H - 1, W - 1, H - 1, 'f')
        self.fill(0, 0, 0, H - 1, 'f'); self.fill(W - 1, 0, W - 1, H - 1, 'f')

        # ---- FORECOURT (start) ----
        self.zones.append(dict(id='forecourt', name='THE FORECOURT'))
        self.fill(1, 1, W - 2, 4, 'r')
        for x in range(1, W - 1):
            if x % 4 < 2:
                self.g[2][x] = 'h'
        self.fill(1, 5, W - 2, 14, 's')
        self.fill(14, 5, 45, 14, 'i')
        self.building(24, 11, 35, 14, 'tower_tan', 'LAST CHANCE GAS')
        for x in (18.5, 24.5, 30.5, 36.5, 41.5):
            self.prop('barricade', x, 7.5, solid=True)       # pump islands
        self.prop('wreck_red', 8.5, 3.0, solid=True, rot=0, w=2, h=1)
        self.prop('wreck_blue', 51.5, 3.0, solid=True, rot=0, w=2, h=1)
        for x, y in ((4.5, 8.5), (55.5, 9.5), (12.5, 12.5), (47.5, 6.5)):
            self.prop('trash', x, y)
        for x, y in ((2.5, 6.5), (57.5, 6.5)):
            self.prop('lamp', x, y, solid=True)

        # ---- the row between the forecourt and the back zones ----
        self.building(1, 15, 7, 17, 'old_brick')
        self.building(12, 15, 47, 17, 'warehouse_gray', 'HOLLOW BLOCK')
        self.building(52, 15, W - 2, 17, 'old_slate')
        self.door('lot_gate', 'THE LOT', 8, 15, 11, 17, 750, 'lot')
        self.door('maple_gate', 'MAPLE STREET', 48, 15, 51, 17, 1000, 'maple')

        # ---- the LOT (west) ----
        self.zones.append(dict(id='lot', name='THE LOT'))
        self.fill(1, 18, 19, H - 2, 'l')
        for (x, y) in ((5, 24), (11, 24), (5, 32), (15, 32), (9, 40)):
            self.prop(self.rng.choice(['wreck_red', 'wreck_blue', 'wreck_white']), x + 0.5, y + 0.5, solid=True, rot=90, w=2, h=1)
        self.prop('wreck_burnt', 15.5, 38.5, solid=True, rot=0, w=2, h=1)
        for x, y in ((3.5, 20.5), (17.5, 27.5), (7.5, 36.5)):
            self.prop('tire', x, y)

        # ---- the YARD (centre) ----
        self.zones.append(dict(id='yard', name='THE YARD'))
        self.fill(24, 18, 35, H - 2, 'g')
        self.fill(29, 18, 30, H - 2, 'p')
        for x, y in ((25.5, 22.5), (34.5, 26.5), (25.5, 34.5), (34.5, 38.5), (27.5, 43.5)):
            self.prop('tree', x, y, solid=True)
        for x, y in ((26.5, 28.5), (33.5, 31.5)):
            self.prop('bush', x, y)

        # ---- MAPLE STREET (east) ----
        self.zones.append(dict(id='maple', name='MAPLE STREET'))
        self.fill(40, 18, W - 2, H - 2, 's')
        self.fill(44, 18, 47, H - 2, 'r')
        for y in range(18, H - 1):
            if y % 4 < 2:
                self.g[y][45] = 'v'
        self.building(50, 34, W - 2, 42, 'old_terracotta', "ROSA'S DINER")
        self.prop('wreck_white', 45.5, 28.5, solid=True, rot=90, w=2, h=1)
        for x, y in ((42.5, 21.5), (56.5, 22.5)):
            self.prop('lamp', x, y, solid=True)
        for x, y in ((41.5, 33.5), (53.5, 30.5)):
            self.prop('trash', x, y)

        # ---- walls between the back zones, with debris gaps into the Yard ----
        self.building(20, 18, 23, 29, 'old_brick')
        self.building(20, 34, 23, H - 2, 'old_brick')
        self.door('yard_west', 'THE YARD', 20, 30, 23, 33, 1250, 'yard')
        self.building(36, 18, 39, 29, 'old_slate')
        self.building(36, 34, 39, H - 2, 'old_slate')
        self.door('yard_east', 'THE YARD', 36, 30, 39, 33, 1250, 'yard')

        # ---- stations ----
        # wall guns (on building faces), perk machines, the forge
        self.station('gun', 'street_smg', 16.5, 14.3, 1000, 'forecourt')
        self.station('gun', 'riot_shotgun', 43.5, 14.3, 1500, 'forecourt')
        self.station('perk', 'FleetFoot', 54.5, 13.5, 0, 'forecourt')
        self.station('gun', 'ranger_rifle', 1.8, 30.5, 1200, 'lot')
        self.station('perk', 'ThickSkin', 17.5, 43.5, 0, 'lot')
        self.station('gun', 'assault_rifle', 40.4, 24.5, 1800, 'maple')
        self.station('perk', 'QuickHands', 57.5, 20.5, 0, 'maple')
        self.station('perk', 'SecondChance', 41.5, 43.5, 0, 'maple')
        self.station('perk', 'SteadyAim', 25.5, 42.0, 0, 'yard')
        self.station('forge', 'forge', 30.0, 40.5, 0, 'yard')

        # the mystery box: starts on the forecourt, moves around
        self.box_spots = [dict(x=38.5, y=10.0, zone='forecourt'), dict(x=10.5, y=42.5, zone='lot'),
                          dict(x=53.5, y=26.5, zone='maple'), dict(x=31.5, y=21.0, zone='yard')]

        # spawns at the block's edges (the dead climb the fences), by zone
        for x, y in ((2.5, 2.5), (57.5, 2.5), (2.5, 12.5), (57.5, 12.5), (30.5, 1.5)):
            self.spawns.append(dict(x=x, y=y, zone='forecourt'))
        for x, y in ((2.5, 43.5), (10.5, 43.5), (2.5, 22.5)):
            self.spawns.append(dict(x=x, y=y, zone='lot'))
        for x, y in ((26.5, 43.5), (33.5, 43.5)):
            self.spawns.append(dict(x=x, y=y, zone='yard'))
        for x, y in ((57.5, 43.5), (45.5, 43.5), (57.5, 30.5)):
            self.spawns.append(dict(x=x, y=y, zone='maple'))

        # the Easter egg: three graystone fragments, one in each back zone, tucked into corners
        self.eggs = [dict(x=18.5, y=19.5), dict(x=34.5, y=19.0), dict(x=57.5, y=33.0)]
        self.start = (30.5, 8.5)
        return self

    def to_json(self):
        rows = [''.join(self.g[y]) for y in range(H)]
        return dict(
            w=W, h=H, rows=rows, start=dict(x=self.start[0], y=self.start[1]),
            districts=[dict(id='downtown', x0=0, y0=0, x1=W - 1, y1=H - 1)],
            buildings=self.buildings, props=self.props, safehouses=[], vehicles=[], pickups=[], nests=[], relics=[],
            lairs=[], missions=[], sites=[],
            zones=self.zones, doors=self.doors, stations=self.stations, spawns=self.spawns,
            boxSpots=self.box_spots, eggs=self.eggs)


def build():
    return Block().build()


def check(b):
    """Sanity checks: every station/spawn/egg/box spot stands on open ground, doors are 'D'."""
    def open_at(x, y):
        return b.get(int(x), int(y)) not in ('B', 'f', 'w', 'D')
    problems = []
    for s in b.stations:
        if not open_at(s['x'], s['y']):
            problems.append(('station', s))
    for s in b.spawns + b.eggs + b.box_spots:
        if not open_at(s['x'], s['y']):
            problems.append(('spot', s))
    if not open_at(*b.start):
        problems.append(('start', b.start))
    return problems


if __name__ == '__main__':
    blk = build()
    probs = check(blk)
    for p in probs:
        print('PROBLEM', p)
    data = blk.to_json()
    if len(sys.argv) > 1:
        with open(sys.argv[1], 'w') as f:
            json.dump(data, f, separators=(',', ':'))
    print(len(blk.buildings), 'buildings,', len(blk.props), 'props,', len(blk.doors), 'doors,', len(blk.stations), 'stations,',
          len(blk.spawns), 'spawns')

#!/usr/bin/env python3
"""
LAST STAND: SALT PIER (all original) — the third round-based map: New Hollow's old fishing pier, seen from above.
Writes Resources/PKR/ZombieLand/pier.json in the same format as block.json.

The dead come up out of the water here, so most spawns are on the plank edges.

Layout (y up), W x H tiles:
  y 1-13   THE DOCKS (start): planks over the water, the bait shop, moored boats (wrecks)
  y 14-16  a row of sheds with two debris gaps: to the CONTAINER YARD (west) and to the FISH MARKET (east)
  y 17-35  CONTAINER YARD (west: lanes between stacked containers, a crane)
           COLD STORAGE (centre: one big solid warehouse)
           FISH MARKET (east: cobbles, stalls, the market hall)
  y 36-50  THE LIGHTHOUSE PIER (north): a long plank pier out over the water with the lighthouse at its end, reached
           through debris from the Yard and from the Market
"""
import json, os, random, sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import zombieland_block as zb

W, H = 60, 52
zb.W, zb.H = W, H   # the Block helpers read these


class Pier(zb.Block):
    def __init__(self):
        super().__init__()
        self.g = [['w'] * W for _ in range(H)]
        self.rng = random.Random(1313)

    def build(self):
        # water all round (solid); a fence row at the very top and bottom keeps the grid closed
        self.fill(0, 0, W - 1, 0, 'f'); self.fill(0, H - 1, W - 1, H - 1, 'f')

        # ---- THE DOCKS (start) ----
        self.zones.append(dict(id='docks', name='THE DOCKS'))
        self.fill(4, 1, W - 5, 13, 'd')
        self.fill(8, 1, 12, 3, 'w')        # boat slips cut into the planks
        self.fill(47, 1, 51, 3, 'w')
        self.building(24, 9, 35, 13, 'old_slate', 'BAIT & TACKLE')
        self.prop('wreck_white', 10.5, 5.0, solid=True, rot=0, w=2, h=1)   # boats tied up
        self.prop('wreck_blue', 49.5, 5.0, solid=True, rot=0, w=2, h=1)
        for x, y in ((16.5, 6.5), (43.5, 6.5), (20.5, 11.5), (39.5, 11.5)):
            self.prop('barricade', x, y, solid=True)                        # crates and nets
        for x, y in ((5.5, 12.5), (54.5, 12.5), (30.5, 2.5)):
            self.prop('lamp', x, y, solid=True)
        for x, y in ((14.5, 2.5), (45.5, 9.5), (26.5, 6.5)):
            self.prop('trash', x, y)

        # ---- sheds between the docks and the back ----
        self.building(4, 14, 9, 16, 'warehouse_rust')
        self.building(14, 14, 45, 16, 'warehouse_gray', 'PIER 13')
        self.building(50, 14, W - 5, 16, 'warehouse_green')
        self.door('yard_gate', 'CONTAINER YARD', 10, 14, 13, 16, 750, 'yard')
        self.door('market_gate', 'FISH MARKET', 46, 14, 49, 16, 1000, 'market')

        # ---- CONTAINER YARD (west) ----
        self.zones.append(dict(id='yard', name='CONTAINER YARD'))
        self.fill(4, 17, 21, 35, 'i')
        cols = ['container_red', 'container_blue', 'container_green', 'container_orange']
        for i, (x0, y0) in enumerate(((6, 19), (6, 25), (6, 31), (13, 21), (13, 28), (17, 33))):
            self.building(x0, y0, x0 + 3, y0 + 1, cols[i % 4])
        self.prop('crane', 18.5, 24.5, solid=True)
        for x, y in ((11.5, 18.5), (19.5, 30.5)):
            self.prop('tire', x, y)
        self.prop('cone', 12.0, 34.0)

        # ---- COLD STORAGE (centre, solid) ----
        self.building(22, 17, 37, 35, 'warehouse_gray', 'COLD STORAGE')

        # ---- FISH MARKET (east) ----
        self.zones.append(dict(id='market', name='FISH MARKET'))
        self.fill(38, 17, W - 5, 35, 'o')
        self.building(46, 28, W - 5, 34, 'old_terracotta', 'FISH MARKET')
        for x0, y0 in ((40, 20), (46, 20), (52, 20)):
            self.building(x0, y0, x0 + 2, y0 + 1, 'old_brick')               # stalls
        for x, y in ((41.5, 26.5), (53.5, 25.5)):
            self.prop('trash', x, y)
        for x, y in ((39.5, 34.0), (54.5, 18.0)):
            self.prop('lamp', x, y, solid=True)

        # ---- THE LIGHTHOUSE PIER (north) ----
        self.zones.append(dict(id='lighthouse', name='THE LIGHTHOUSE PIER'))
        self.fill(16, 36, 43, 40, 'd')       # the landing
        self.fill(26, 41, 33, H - 2, 'd')    # the long pier
        self.building(28, 45, 31, 48, 'campus_stone', 'LIGHTHOUSE')
        for x, y in ((17.5, 39.5), (42.5, 39.5), (26.5, 44.5), (33.5, 44.5)):
            self.prop('lamp', x, y, solid=True)
        self.prop('barricade', 30.0, 38.0, solid=True)
        # walls of the back zones onto the landing, with debris gaps
        self.door('pier_west', 'THE LIGHTHOUSE PIER', 16, 36, 19, 36, 1250, 'lighthouse')
        self.door('pier_east', 'THE LIGHTHOUSE PIER', 40, 36, 43, 36, 1250, 'lighthouse')
        self.fill(4, 36, 15, 36, 'w'); self.fill(20, 36, 39, 36, 'B'); self.fill(44, 36, W - 5, 36, 'w')
        self.buildings.append(dict(x=20, y=36, w=20, h=1, style='warehouse_rust'))
        # the yard and market reach the landing from below
        self.fill(16, 35, 19, 35, 'i'); self.fill(40, 35, 43, 35, 'o')

        # ---- stations ----
        self.station('gun', 'street_smg', 15.5, 13.6, 1000, 'docks')
        self.station('gun', 'riot_shotgun', 44.5, 13.6, 1500, 'docks')
        self.station('perk', 'QuickHands', 6.5, 8.5, 0, 'docks')
        self.station('gun', 'ranger_rifle', 4.6, 28.5, 1200, 'yard')
        self.station('perk', 'ThickSkin', 11.5, 34.0, 0, 'yard')
        self.station('gun', 'assault_rifle', 55.4, 24.5, 1800, 'market')
        self.station('perk', 'FleetFoot', 43.5, 18.0, 0, 'market')
        self.station('perk', 'SecondChance', 41.5, 31.5, 0, 'market')
        self.station('perk', 'SteadyAim', 27.0, 42.5, 0, 'lighthouse')
        self.station('forge', 'forge', 32.5, 50.0 - 1.0, 0, 'lighthouse')

        self.box_spots = [dict(x=38.0, y=4.5, zone='docks'), dict(x=10.5, y=23.0, zone='yard'),
                          dict(x=50.5, y=26.0, zone='market'), dict(x=22.0, y=38.5, zone='lighthouse')]

        # spawns: the dead climb out of the water onto the plank edges (and over the market fence)
        for x, y in ((4.5, 1.5), (55.5, 1.5), (4.5, 10.5), (55.5, 10.5), (20.5, 1.5), (39.5, 1.5)):
            self.spawns.append(dict(x=x, y=y, zone='docks'))
        for x, y in ((4.5, 18.5), (4.5, 34.5), (20.5, 18.5)):
            self.spawns.append(dict(x=x, y=y, zone='yard'))
        for x, y in ((55.5, 18.5), (55.5, 35.5), (39.5, 23.5)):
            self.spawns.append(dict(x=x, y=y, zone='market'))
        for x, y in ((16.5, 40.5), (43.5, 40.5), (26.5, 49.5), (33.5, 49.5)):
            self.spawns.append(dict(x=x, y=y, zone='lighthouse'))

        # the Easter egg: three brass bells
        self.eggs = [dict(x=20.5, y=31.5), dict(x=44.5, y=33.5), dict(x=32.5, y=43.0)]
        self.start = (30.0, 6.5)
        return self

    def to_json(self):
        d = super().to_json()
        d['w'], d['h'] = W, H
        d['rows'] = [''.join(self.g[y]) for y in range(H)]
        d['districts'] = [dict(id='harbor', x0=0, y0=0, x1=W - 1, y1=H - 1)]
        return d


def build():
    return Pier().build()


def reachable(p):
    """Every zone's spots must be reachable from the start once every door is open."""
    from collections import deque
    def walk(x, y):
        return p.get(x, y) not in ('B', 'f', 'w')
    solid_props = {(int(pr['x']), int(pr['y'])) for pr in p.props if pr['solid']}
    sx, sy = int(p.start[0]), int(p.start[1])
    seen = {(sx, sy)}
    q = deque([(sx, sy)])
    while q:
        x, y = q.popleft()
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            n = (x + dx, y + dy)
            if n not in seen and walk(*n) and n not in solid_props:
                seen.add(n); q.append(n)
    bad = []
    for s in p.stations + p.spawns + p.eggs + p.box_spots:
        if (int(s['x']), int(s['y'])) not in seen:
            bad.append(s)
    return bad


if __name__ == '__main__':
    p = build()
    for pr in zb.check(p):
        print('PROBLEM', pr)
    for s in reachable(p):
        print('UNREACHABLE', s)
    data = p.to_json()
    if len(sys.argv) > 1:
        with open(sys.argv[1], 'w') as f:
            json.dump(data, f, separators=(',', ':'))
    print(len(p.buildings), 'buildings,', len(p.props), 'props,', len(p.doors), 'doors,', len(p.stations), 'stations,',
          len(p.spawns), 'spawns')
    for y in range(H - 1, -1, -1):
        print(''.join(p.g[y]))

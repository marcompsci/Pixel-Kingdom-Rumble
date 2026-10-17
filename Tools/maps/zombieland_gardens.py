#!/usr/bin/env python3
"""
LAST STAND: GRAVEMOOR PARK (all original) — the fourth round-based map: an old city park and its hillside cemetery,
seen from above. Writes Resources/PKR/ZombieLand/gardens.json in the same format as block.json.

Layout (y up), W x H tiles:
  y 1-12   THE GATEHOUSE (start): a cobbled plaza in front of the Gravemoor gatehouse
  y 13     an iron park fence with two debris gaps: to the ROSE GARDEN (west) and the BOATHOUSE (east)
  y 14-35  THE ROSE GARDEN (west: hedges, paths and the glasshouse) · a solid mausoleum in the middle ·
           THE BOATHOUSE (east: a pond with a plank bridge and the boathouse)
  y 36     a second fence with debris gaps from the rose garden and from the boathouse
  y 37-50  CHAPEL HILL (north): rows of graves around the hill chapel
Three mourning lanterns hidden in the park wake THE BRIAR QUEEN.
"""
import json, os, random, sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import zombieland_block as zb

W, H = 64, 52


class Gardens(zb.Block):
    def __init__(self):
        zb.W, zb.H = W, H
        super().__init__()
        self.g = [['B'] * W for _ in range(H)]
        self.rng = random.Random(5151)

    def path(self, x0, y0, x1, y1):
        self.fill(x0, y0, x1, y1, 'p')

    def build(self):
        self.fill(0, 0, W - 1, 0, 'f'); self.fill(0, H - 1, W - 1, H - 1, 'f')
        self.fill(0, 0, 0, H - 1, 'f'); self.fill(W - 1, 0, W - 1, H - 1, 'f')

        # ---- THE GATEHOUSE (start) ----
        self.zones.append(dict(id='gatehouse', name='THE GATEHOUSE'))
        self.fill(1, 1, W - 2, 12, 'o')
        self.fill(1, 1, W - 2, 2, 's')
        self.building(26, 8, 37, 12, 'old_slate', 'GRAVEMOOR GATE')
        for x in (8.5, 18.5, 45.5, 55.5):
            self.prop('lamp', x, 6.5, solid=True)
        for x, y in ((13.5, 9.5), (50.5, 9.5)):
            self.prop('barricade', x, y, solid=True)   # park benches
        for x, y in ((5.5, 4.5), (59.5, 5.5), (31.5, 4.0)):
            self.prop('paper', x, y)

        # ---- the first fence ----
        self.fill(1, 13, W - 2, 13, 'f')
        self.door('rose_gate', 'THE ROSE GARDEN', 9, 13, 12, 13, 750, 'rose')
        self.door('boat_gate', 'THE BOATHOUSE', 51, 13, 54, 13, 1000, 'boathouse')

        # ---- THE ROSE GARDEN (west) ----
        self.zones.append(dict(id='rose', name='THE ROSE GARDEN'))
        self.fill(1, 14, 25, 35, 'y')
        self.path(1, 19, 25, 20)
        self.path(13, 14, 14, 35)
        self.path(1, 31, 12, 32)
        self.building(3, 24, 10, 29, 'tower_glass', 'GLASSHOUSE')
        for x in (3.5, 6.5, 9.5, 17.5, 20.5, 23.5):
            self.prop('bush', x, 16.5, solid=True)       # rose hedges
        for x in (17.5, 20.5, 23.5):
            self.prop('bush', x, 25.5, solid=True)
            self.prop('bush', x, 29.5, solid=True)
        self.prop('tree', 21.0, 34.0, solid=True)

        # ---- the mausoleum (middle, solid) ----
        self.building(26, 15, 37, 34, 'campus_stone', 'MAUSOLEUM')

        # ---- THE BOATHOUSE (east) ----
        self.zones.append(dict(id='boathouse', name='THE BOATHOUSE'))
        self.fill(38, 14, W - 2, 35, 'g')
        self.path(38, 15, W - 2, 16)
        for y in range(19, 31):          # the pond, crossed by a plank bridge
            for x in range(41, 59):
                if ((x - 49.5) / 7.5) ** 2 + ((y - 24.5) / 5.2) ** 2 < 1:
                    self.g[y][x] = 'w'
        self.fill(41, 24, 58, 25, 'd')
        self.building(55, 31, W - 2, 35, 'warehouse_green', 'BOATHOUSE')
        self.prop('wreck_white', 45.5, 32.5, solid=True, rot=0, w=2, h=1)   # a rowing boat pulled up on the bank
        for x, y in ((39.5, 21.5), (60.5, 20.5)):
            self.prop('tree', x, y, solid=True)
        self.prop('lamp', 40.5, 33.5, solid=True)

        # ---- the second fence ----
        self.fill(1, 36, W - 2, 36, 'f')
        self.door('hill_west', 'CHAPEL HILL', 10, 36, 13, 36, 1250, 'hill')
        self.door('hill_east', 'CHAPEL HILL', 50, 36, 53, 36, 1250, 'hill')

        # ---- CHAPEL HILL (north) ----
        self.zones.append(dict(id='hill', name='CHAPEL HILL'))
        self.fill(1, 37, W - 2, H - 2, 'g')
        self.path(1, 39, W - 2, 40)
        self.path(30, 37, 33, H - 2)
        self.building(27, 44, 36, 48, 'old_slate', 'HILL CHAPEL')
        for row in (42, 46, 49):        # the graves: short rows with gaps the dead (and you) walk through
            for x in range(3, 26, 3):
                if self.rng.random() < 0.8:
                    self.prop('grave', x + 0.5, row + 0.5, solid=True)
            for x in range(39, 62, 3):
                if self.rng.random() < 0.8:
                    self.prop('grave', x + 0.5, row + 0.5, solid=True)
        for x, y in ((2.5, 37.5), (61.5, 37.5), (22.0, 44.0), (42.0, 44.0)):
            self.prop(self.rng.choice(['pine', 'tree']), x, y, solid=True)

        # ---- stations ----
        self.station('gun', 'street_smg', 22.5, 12.4, 1000, 'gatehouse')
        self.station('gun', 'riot_shotgun', 41.5, 12.4, 1500, 'gatehouse')
        self.station('perk', 'QuickHands', 4.5, 10.5, 0, 'gatehouse')
        self.station('gun', 'ranger_rifle', 1.6, 22.0, 1200, 'rose')
        self.station('perk', 'ThickSkin', 19.5, 33.5, 0, 'rose')
        self.station('gun', 'assault_rifle', 62.4, 18.0, 1800, 'boathouse')
        self.station('perk', 'FleetFoot', 38.6, 17.5, 0, 'boathouse')
        self.station('perk', 'SecondChance', 39.5, 30.5, 0, 'boathouse')
        self.station('perk', 'SteadyAim', 24.5, 47.5, 0, 'hill')
        self.station('forge', 'forge', 39.5, 47.5, 0, 'hill')

        self.box_spots = [dict(x=47.5, y=4.5, zone='gatehouse'), dict(x=16.5, y=22.5, zone='rose'),
                          dict(x=49.5, y=33.5, zone='boathouse'), dict(x=31.5, y=42.0, zone='hill')]

        for x, y in ((1.5, 1.5), (62.5, 1.5), (1.5, 11.5), (62.5, 11.5), (31.5, 1.5)):
            self.spawns.append(dict(x=x, y=y, zone='gatehouse'))
        for x, y in ((1.5, 14.5), (1.5, 35.5), (24.5, 14.5)):
            self.spawns.append(dict(x=x, y=y, zone='rose'))
        for x, y in ((62.5, 14.5), (38.5, 35.5), (62.5, 28.5)):
            self.spawns.append(dict(x=x, y=y, zone='boathouse'))
        for x, y in ((1.5, 50.5), (62.5, 50.5), (31.5, 50.5), (16.5, 44.5)):
            self.spawns.append(dict(x=x, y=y, zone='hill'))

        # the Easter egg: three mourning lanterns
        self.eggs = [dict(x=6.5, y=33.5), dict(x=49.5, y=24.5), dict(x=58.5, y=47.5)]
        self.start = (31.5, 6.0)
        return self

    def to_json(self):
        d = super().to_json()
        d['w'], d['h'] = W, H
        d['rows'] = [''.join(self.g[y]) for y in range(H)]
        d['districts'] = [dict(id='downtown', x0=0, y0=0, x1=W - 1, y1=H - 1)]
        return d


def build():
    return Gardens().build()


if __name__ == '__main__':
    import zombieland_pier as zp
    r = build()
    for pr in zb.check(r):
        print('PROBLEM', pr)
    for s in zp.reachable(r):
        print('UNREACHABLE', s)
    data = r.to_json()
    if len(sys.argv) > 1:
        with open(sys.argv[1], 'w') as f:
            json.dump(data, f, separators=(',', ':'))
    print(len(r.buildings), 'buildings,', len(r.props), 'props,', len(r.doors), 'doors,', len(r.stations), 'stations,',
          len(r.spawns), 'spawns')
    for y in range(H - 1, -1, -1):
        print(''.join(r.g[y]))

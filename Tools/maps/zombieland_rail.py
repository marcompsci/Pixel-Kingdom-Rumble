#!/usr/bin/env python3
"""
LAST STAND: RAIL YARD (all original) — the fourth round-based map: the Hollow Junction freight yard, seen from above.
Writes Resources/PKR/ZombieLand/rail.json in the same format as block.json.

Layout (y up), W x H tiles:
  y 1-12   THE PLATFORM (start): the station platform and Hollow Junction station house
  y 13-15  a fence of parked freight cars with two debris gaps: to the SWITCHYARD (west) and the ROUNDHOUSE (east)
  y 16-34  THE SWITCHYARD (west: tracks between long freight cars) · THE ROUNDHOUSE (east: the engine shed
           around a turntable) · a solid coal store in the middle
  y 35-46  THE SIGNAL TOWER (north): reached through debris from the switchyard and from the roundhouse
Three signal levers hidden in the yard wake THE IRON WIDOW.
"""
import json, os, random, sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import zombieland_block as zb

W, H = 64, 48


class Rail(zb.Block):
    def __init__(self):
        zb.W, zb.H = W, H
        super().__init__()
        self.g = [['B'] * W for _ in range(H)]
        self.rng = random.Random(7070)

    def track(self, x0, x1, y):
        """A horizontal track: sleepers (planks) on gravel."""
        for x in range(x0, x1 + 1):
            self.g[y][x] = 'd' if x % 2 == 0 else 'x'

    def build(self):
        self.fill(0, 0, W - 1, 0, 'f'); self.fill(0, H - 1, W - 1, H - 1, 'f')
        self.fill(0, 0, 0, H - 1, 'f'); self.fill(W - 1, 0, W - 1, H - 1, 'f')

        # ---- THE PLATFORM (start) ----
        self.zones.append(dict(id='platform', name='THE PLATFORM'))
        self.fill(1, 1, W - 2, 12, 'i')
        self.track(1, W - 2, 2)
        self.track(1, W - 2, 4)
        self.fill(1, 5, W - 2, 12, 's')
        self.building(24, 9, 39, 12, 'old_brick', 'HOLLOW JUNCTION')
        for x in (10.5, 18.5, 45.5, 53.5):
            self.prop('lamp', x, 6.5, solid=True)
        for x, y in ((14.5, 9.5), (49.5, 9.5)):
            self.prop('barricade', x, y, solid=True)   # benches
        for x, y in ((6.5, 7.5), (58.5, 8.5), (32.0, 6.5)):
            self.prop('trash', x, y)

        # ---- the fence of freight cars ----
        self.building(1, 13, 9, 15, 'container_red')
        self.building(14, 13, 49, 15, 'container_blue', 'FREIGHT LINE')
        self.building(54, 13, W - 2, 15, 'container_green')
        self.door('switch_gate', 'THE SWITCHYARD', 10, 13, 13, 15, 750, 'switchyard')
        self.door('round_gate', 'THE ROUNDHOUSE', 50, 13, 53, 15, 1000, 'roundhouse')

        # ---- THE SWITCHYARD (west) ----
        self.zones.append(dict(id='switchyard', name='THE SWITCHYARD'))
        self.fill(1, 16, 24, 34, 'x')
        for y in (18, 23, 28, 33):
            self.track(1, 24, y)
        self.building(3, 20, 12, 21, 'container_orange')
        self.building(14, 25, 23, 26, 'container_red')
        self.building(3, 30, 12, 31, 'container_blue')
        self.prop('crane', 20.5, 20.5, solid=True)
        self.prop('tire', 9.5, 27.5)

        # ---- the coal store (middle, solid) ----
        self.building(25, 16, 38, 34, 'warehouse_rust', 'COAL STORE')

        # ---- THE ROUNDHOUSE (east) ----
        self.zones.append(dict(id='roundhouse', name='THE ROUNDHOUSE'))
        self.fill(39, 16, W - 2, 34, 'i')
        self.building(52, 26, W - 2, 34, 'warehouse_gray', 'ENGINE SHED')
        for y in range(19, 25):        # the turntable pit and its bridge
            for x in range(42, 50):
                if ((x - 45.5) / 4.0) ** 2 + ((y - 21.5) / 3.2) ** 2 < 1:
                    self.g[y][x] = 'o'
        self.track(42, 49, 21)
        for x, y in ((41.5, 31.5), (60.5, 18.5)):
            self.prop('lamp', x, y, solid=True)
        self.prop('wreck_burnt', 56.5, 21.0, solid=True, rot=0, w=2, h=1)

        # ---- THE SIGNAL TOWER (north) ----
        self.zones.append(dict(id='tower', name='THE SIGNAL TOWER'))
        self.fill(1, 36, W - 2, H - 2, 'g')
        self.track(1, W - 2, 44)
        self.building(28, 39, 35, 42, 'campus_stone', 'SIGNAL TOWER')
        self.building(1, 35, 9, 35, 'container_red')
        self.building(14, 35, 49, 35, 'container_orange')
        self.building(54, 35, W - 2, 35, 'container_green')
        self.door('tower_west', 'THE SIGNAL TOWER', 10, 35, 13, 35, 1250, 'tower')
        self.door('tower_east', 'THE SIGNAL TOWER', 50, 35, 53, 35, 1250, 'tower')
        for x, y in ((8.5, 40.5), (55.5, 40.5), (20.5, 39.0), (44.5, 39.0)):
            self.prop(self.rng.choice(['tree', 'bush']), x, y, solid=True)

        # ---- stations ----
        self.station('gun', 'street_smg', 22.5, 12.6, 1000, 'platform')
        self.station('gun', 'riot_shotgun', 41.5, 12.6, 1500, 'platform')
        self.station('perk', 'QuickHands', 4.5, 10.5, 0, 'platform')
        self.station('gun', 'ranger_rifle', 1.6, 26.0, 1200, 'switchyard')
        self.station('perk', 'ThickSkin', 22.0, 32.0, 0, 'switchyard')
        self.station('gun', 'assault_rifle', 62.4, 24.0, 1800, 'roundhouse')
        self.station('perk', 'FleetFoot', 40.5, 17.5, 0, 'roundhouse')
        self.station('perk', 'SecondChance', 50.5, 31.0, 0, 'roundhouse')
        self.station('perk', 'SteadyAim', 26.0, 37.5, 0, 'tower')
        self.station('forge', 'forge', 37.5, 38.5, 0, 'tower')

        self.box_spots = [dict(x=46.5, y=7.0, zone='platform'), dict(x=7.5, y=25.5, zone='switchyard'),
                          dict(x=45.5, y=29.5, zone='roundhouse'), dict(x=31.5, y=44.5, zone='tower')]

        for x, y in ((1.5, 1.5), (62.5, 1.5), (1.5, 11.5), (62.5, 11.5), (31.5, 1.5)):
            self.spawns.append(dict(x=x, y=y, zone='platform'))
        for x, y in ((1.5, 17.5), (1.5, 33.5), (23.5, 17.5)):
            self.spawns.append(dict(x=x, y=y, zone='switchyard'))
        for x, y in ((62.5, 17.5), (40.5, 33.5), (62.5, 25.5)):
            self.spawns.append(dict(x=x, y=y, zone='roundhouse'))
        for x, y in ((1.5, 45.5), (62.5, 45.5), (31.5, 45.5), (16.5, 41.5)):
            self.spawns.append(dict(x=x, y=y, zone='tower'))

        # the Easter egg: three signal levers
        self.eggs = [dict(x=13.5, y=29.0), dict(x=40.5, y=26.0), dict(x=47.5, y=42.5)]
        self.start = (31.5, 7.0)
        return self

    def to_json(self):
        d = super().to_json()
        d['w'], d['h'] = W, H
        d['rows'] = [''.join(self.g[y]) for y in range(H)]
        d['districts'] = [dict(id='industrial', x0=0, y0=0, x1=W - 1, y1=H - 1)]
        return d


def build():
    return Rail().build()


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

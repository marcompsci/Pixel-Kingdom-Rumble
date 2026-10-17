#!/usr/bin/env python3
"""
ZOMBIE LAND CHAPTER 4: COLD HARBOR AIRFIELD (all original) — the army airfield north of the rail depot, reached by
the old service road once the Curator is dead. Writes Resources/PKR/ZombieLand/airfield.json in the city format
(safehouse, missions, nests, pickups, a lair door for the boss), seen from above.

Layout (y up): the gate and service road (south, start and safehouse) · the crew barracks (south-west) · the fuel depot
(south-east) · the runway across the middle · the control tower (west of the runway) · a spare hangar (east) ·
HANGAR 3 (north), where the Marshal waits.
"""
import json, math, os, random, sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import zombieland_block as zb
import zombieland_labs as zlabs

W, H = 76, 56


class Airfield(zlabs.Labs):
    OPEN = ('i', 'r', 'g', 'h', 'o')

    def __init__(self):
        zlabs.W, zlabs.H = W, H
        zb.W, zb.H = W, H
        super().__init__()
        self.g = [['g'] * W for _ in range(H)]
        self.rng = random.Random(4141)

    def mission(self, mid, key, extra=None, dest=None):
        x, y = self.sites[key]
        m = dict(id=mid, x=x, y=y)
        if extra:
            n, rmin, rmax = extra
            pts, tries = [], 0
            while len(pts) < n and tries < 4000:
                tries += 1
                a = self.rng.random() * math.tau
                d = rmin + self.rng.random() * (rmax - rmin)
                px, py = int(x + math.cos(a) * d), int(y + math.sin(a) * d)
                if self.get(px, py) in self.OPEN and not self.blocked_prop(px, py) and all(abs(qx - px) + abs(qy - py) > 4 for qx, qy in pts):
                    pts.append((px, py))
            m['points'] = [dict(x=px + 0.5, y=py + 0.5) for px, py in pts]
        self.missions.append(m)

    def build(self):
        self.fill(0, 0, W - 1, 0, 'f'); self.fill(0, H - 1, W - 1, H - 1, 'f')
        self.fill(0, 0, 0, H - 1, 'f'); self.fill(W - 1, 0, W - 1, H - 1, 'f')

        # the gate, the service road and the apron in front of the gate
        self.fill(1, 2, 27, 5, 'r')
        self.fill(28, 1, 48, 7, 'i')
        self.building(30, 5, 33, 7, 'tower_gray', 'GATEHOUSE')
        # the taxiways
        self.fill(36, 8, 39, 23, 'r')
        self.fill(36, 30, 39, 44, 'r')
        # the runway (centre line in lane paint)
        self.fill(3, 24, W - 4, 29, 'r')
        for x in range(4, W - 4, 2):
            self.g[26][x] = 'h'
            self.g[27][x] = 'h'
        # crew barracks (south-west)
        self.fill(5, 8, 21, 18, 'i')
        self.building(8, 12, 18, 16, 'house_gray', 'CREW BARRACKS')
        # fuel depot (south-east)
        self.fill(53, 8, 71, 19, 'i')
        self.building(62, 13, 70, 17, 'warehouse_rust', 'FUEL DEPOT')
        for x, y in ((55.5, 15.5), (58.5, 15.5)):
            self.prop('barricade', x, y, solid=True)              # fuel bowsers
        # control tower (west of the runway)
        self.fill(5, 31, 21, 42, 'i')
        self.building(9, 35, 14, 40, 'tower_gray', 'CONTROL TOWER')
        # a spare hangar (east)
        self.fill(52, 31, 72, 44, 'i')
        self.building(56, 37, 69, 43, 'warehouse_green', 'HANGAR 2')
        # HANGAR 3 and its apron (north)
        self.fill(24, 45, 52, 50, 'i')
        self.building(28, 51, 48, 54, 'warehouse_gray', 'HANGAR 3')
        # details
        for x, y in ((45.5, 34.5), (24.5, 12.5), (66.5, 28.5)):
            self.prop(self.rng.choice(['wreck_white', 'wreck_burnt']), x, y, solid=True, rot=0, w=2, h=1)
        for x, y in ((29.5, 2.5), (47.5, 2.5), (35.0, 8.5), (40.0, 8.5), (23.5, 46.5), (52.5, 46.5)):
            self.prop('lamp', x, y, solid=True)
        for x, y in ((20.5, 22.5), (52.5, 22.5), (6.5, 22.5), (70.5, 31.5)):
            self.prop('cone', x, y)
        for x, y in ((3.5, 47.5), (8.5, 51.5), (16.5, 49.5), (60.5, 50.5), (69.5, 48.5), (73.5, 12.5), (2.5, 38.5)):
            self.prop(self.rng.choice(['pine', 'tree', 'bush']), x, y, solid=True)

        # sites
        self.site('road', 4.5, 3.5)
        self.site('lights', 38.0, 27.0)
        self.site('fuel', 60.5, 10.5)
        self.site('tower', 12.5, 32.5)
        self.site('pilot', 13.5, 10.0)
        self.site('hangar', 38.5, 48.5)
        self.start = (6.5, 3.5)
        self.safehouses.append(dict(id='safe_air', name='GATE SAFEHOUSE', x=44.5, y=4.5, district='airfield'))
        # missions (chapter 4)
        self.mission('c4_lights', 'lights', extra=(3, 8, 26))
        self.mission('c4_fuel', 'fuel', extra=(3, 4, 11))
        self.mission('c4_tower', 'tower')
        self.mission('c4_pilot', 'pilot')
        self.lairs.append(dict(id='marshal_hangar', x=38.5, y=48.5))
        # nests and loot
        for i, (x, y) in enumerate(((6.5, 47.5), (70.5, 50.5), (72.5, 21.5))):
            self.nests.append(dict(id=f'nest_air_{i}', x=x, y=y, district='airfield'))
        n = 0
        for kind, count in (('ammo', 12), ('medkit', 5), ('armor', 3)):
            got, tries = 0, 0
            while got < count and tries < 4000:
                tries += 1
                x, y = self.rng.randint(3, W - 4), self.rng.randint(3, H - 4)
                if self.get(x, y) != 'i' or self.blocked_prop(x, y) or not any(self.get(x + dx, y + dy) == 'B' for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                    continue
                self.pickups.append(dict(id=f'pa{n}', k=kind, x=x + 0.5, y=y + 0.5))
                n += 1
                got += 1
        return self

    def to_json(self):
        d = super().to_json()
        d['districts'] = [dict(id='airfield', x0=0, y0=0, x1=W - 1, y1=H - 1)]
        return d


def build():
    return Airfield().build()


if __name__ == '__main__':
    import zombieland_pier as zp
    a = build()
    data = a.to_json()
    a.spawns = [dict(x=m['x'], y=m['y']) for m in data['missions']] + [p for m in data['missions'] for p in m.get('points', [])]
    a.eggs = [dict(x=s['x'], y=s['y']) for s in data['pickups']] + [dict(x=n['x'], y=n['y']) for n in data['nests']]
    a.box_spots = [dict(x=38.5, y=48.5), dict(x=44.5, y=4.5)]
    for s in zp.reachable(a):
        print('UNREACHABLE', s)
    for m in data['missions']:
        if 'points' in m:
            print(m['id'], len(m['points']), 'points')
    if len(sys.argv) > 1:
        with open(sys.argv[1], 'w') as f:
            json.dump(data, f, separators=(',', ':'))
    print(len(data['missions']), 'missions,', len(data['pickups']), 'pickups,', len(data['nests']), 'nests')
    for y in range(H - 1, -1, -1):
        print(''.join(a.g[y]))

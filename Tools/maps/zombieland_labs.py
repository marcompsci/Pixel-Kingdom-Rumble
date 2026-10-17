#!/usr/bin/env python3
"""
ZOMBIE LAND CHAPTER 3: THE LARKIN SUBLEVEL (all original) — the sealed research floors under Larkin Labs, reached by
the freight lift on College Hill once Blackwater is clear. Writes Resources/PKR/ZombieLand/labs.json in the city
format (safehouse, missions, nests, pickups, a lair door for the boss), seen from above.

Layout (y up): the lift hall (south, start and safehouse) · a north-south spine and an east-west corridor ·
POWER PLANT (south-west) · SPECIMEN STORAGE (north-west) · ARMORY (south-east) · QUARANTINE WING (north-east) ·
the CURATOR'S VAULT (north, behind a sealed door).
"""
import json, os, random, sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import zombieland_block as zb

W, H = 72, 52


class Labs(zb.Block):
    def __init__(self):
        zb.W, zb.H = W, H
        super().__init__()
        self.rng = random.Random(3131)
        self.sites, self.missions, self.safehouses, self.pickups, self.nests, self.lairs = {}, [], [], [], [], []

    def blocked_prop(self, x, y):
        return any(int(p['x']) == x and int(p['y']) == y for p in self.props if p['solid'])

    def room(self, x0, y0, x1, y1, floor='i'):
        self.fill(x0, y0, x1, y1, floor)

    def site(self, key, x, y):
        self.sites[key] = (x, y)

    def mission(self, mid, key, extra=None, dest=None):
        x, y = self.sites[key]
        m = dict(id=mid, x=x, y=y)
        if extra:
            n, rmin, rmax = extra
            pts, tries = [], 0
            while len(pts) < n and tries < 3000:
                tries += 1
                import math
                a = self.rng.random() * math.tau
                d = rmin + self.rng.random() * (rmax - rmin)
                px, py = int(x + math.cos(a) * d), int(y + math.sin(a) * d)
                if self.get(px, py) in ('i', 'o', 'p') and not self.blocked_prop(px, py) and all(abs(qx - px) + abs(qy - py) > 4 for qx, qy in pts):
                    pts.append((px, py))
            m['points'] = [dict(x=px + 0.5, y=py + 0.5) for px, py in pts]
        if dest:
            dx, dy = self.sites[dest]
            m['dest'] = dict(x=dx, y=dy)
            m['hasDest'] = 1
        self.missions.append(m)

    def wall_roofs(self):
        """Every run of rock gets a roof record so it draws as a wall, not as floor (greedy rectangles)."""
        seen = [[False] * W for _ in range(H)]
        for y in range(H):
            for x in range(W):
                if seen[y][x] or self.g[y][x] != 'B':
                    continue
                x1 = x
                while x1 + 1 < W and self.g[y][x1 + 1] == 'B' and not seen[y][x1 + 1]:
                    x1 += 1
                y1 = y
                while y1 + 1 < H and all(self.g[y1 + 1][xx] == 'B' and not seen[y1 + 1][xx] for xx in range(x, x1 + 1)):
                    y1 += 1
                for yy in range(y, y1 + 1):
                    for xx in range(x, x1 + 1):
                        seen[yy][xx] = True
                self.buildings.append(dict(x=x, y=y, w=x1 - x + 1, h=y1 - y + 1, style='tower_gray'))

    def build(self):
        # solid rock everywhere, then carve
        self.fill(0, 0, W - 1, H - 1, 'B')
        self.room(30, 2, 41, 10)                 # lift hall
        self.room(34, 11, 37, 43)                # spine
        self.room(3, 24, 68, 27)                 # east-west corridor
        self.room(3, 11, 28, 22)                 # power plant
        self.room(3, 29, 28, 48)                 # specimen storage
        self.room(43, 11, 68, 22)                # armory
        self.room(43, 29, 68, 40)                # quarantine wing
        self.room(30, 44, 41, 49, 'o')           # the vault (behind the sealed door)
        # door gaps from the corridor into the rooms
        self.room(14, 23, 17, 23); self.room(14, 28, 17, 28)
        self.room(54, 23, 57, 23); self.room(54, 28, 57, 28)
        # floor details
        for y in range(12, 22, 3):
            for x in range(5, 27, 6):
                self.prop('barricade', x + 0.5, y + 0.5, solid=True)       # turbines / consoles
        for x, y in ((6.5, 32.5), (12.5, 36.5), (20.5, 44.5), (25.5, 32.5), (8.5, 45.5)):
            self.prop('trash', x, y)                                     # broken specimen jars
        for x, y in ((46.5, 14.5), (52.5, 19.5), (63.5, 13.5)):
            self.prop('wreck_burnt', x, y, solid=True, rot=0, w=2, h=1)   # burnt-out carts
        for x, y in ((31.5, 3.5), (40.5, 3.5), (35.5, 26.0), (5.5, 25.5), (66.5, 25.5)):
            self.prop('lamp', x, y, solid=True)
        self.wall_roofs()

        # sites
        self.site('lift', 35.5, 4.5)
        self.site('power', 15.5, 17.5)
        self.site('samples', 15.5, 38.5)
        self.site('armory', 55.5, 16.5)
        self.site('seal', 55.5, 34.5)
        self.site('vault', 35.5, 42.5)
        self.start = (35.5, 6.5)
        self.safehouses.append(dict(id='safe_labs', name='LIFT HALL SAFEHOUSE', x=32.5, y=8.0, district='labs'))
        # missions (chapter 3)
        self.mission('c3_power', 'power', extra=(3, 4, 9))
        self.mission('c3_samples', 'samples', extra=(3, 5, 11))
        self.mission('c3_seal', 'seal')
        self.mission('c3_armory', 'armory')
        self.lairs.append(dict(id='curator_vault', x=35.5, y=42.5))
        # nests and loot
        for i, (x, y) in enumerate(((8.5, 41.5), (63.5, 37.5), (22.5, 13.5))):
            self.nests.append(dict(id=f'nest_labs_{i}', x=x, y=y, district='labs'))
        n = 0
        for kind, count in (('ammo', 10), ('medkit', 4), ('armor', 2)):
            got, tries = 0, 0
            while got < count and tries < 4000:
                tries += 1
                x, y = self.rng.randint(3, 68), self.rng.randint(3, 48)
                if self.get(x, y) != 'i' or self.blocked_prop(x, y) or not any(self.get(x + dx, y + dy) == 'B' for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                    continue
                self.pickups.append(dict(id=f'pl{n}', k=kind, x=x + 0.5, y=y + 0.5))
                n += 1
                got += 1
        return self

    def to_json(self):
        rows = [''.join(self.g[y]) for y in range(H)]
        return dict(
            w=W, h=H, rows=rows, start=dict(x=self.start[0], y=self.start[1]),
            districts=[dict(id='labs', x0=0, y0=0, x1=W - 1, y1=H - 1)],
            buildings=self.buildings, props=self.props, safehouses=self.safehouses, vehicles=[], pickups=self.pickups,
            nests=self.nests, relics=[], lairs=self.lairs, missions=self.missions,
            sites=[dict(key=k, x=v[0], y=v[1]) for k, v in self.sites.items()],
            zones=[], doors=[], stations=[], spawns=[], boxSpots=[], eggs=[])


def build():
    return Labs().build()


if __name__ == '__main__':
    import zombieland_pier as zp
    l = build()
    data = l.to_json()
    # reachability of every mission point / site from the start
    l.spawns = [dict(x=m['x'], y=m['y']) for m in data['missions']] + [p for m in data['missions'] for p in m.get('points', [])]
    l.eggs = [dict(x=s['x'], y=s['y']) for s in data['pickups']] + [dict(x=n['x'], y=n['y']) for n in data['nests']]
    l.box_spots = [dict(x=35.5, y=42.5)]
    for s in zp.reachable(l):
        print('UNREACHABLE', s)
    if len(sys.argv) > 1:
        with open(sys.argv[1], 'w') as f:
            json.dump(data, f, separators=(',', ':'))
    print(len(data['missions']), 'missions,', len(data['pickups']), 'pickups,', len(data['nests']), 'nests')
    for y in range(H - 1, -1, -1):
        print(''.join(l.g[y]))

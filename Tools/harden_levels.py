#!/usr/bin/env python3
"""
Adds the quick-thinking obstacles (saws 'o', laser gates 'l', crushers 'v') to the Story maps in
Assets/_Project/Scripts/Core/Level/StoryLayouts.cs, more of them in later worlds. Placement is deterministic and
conservative: only on flat floor, away from the start, goal, checkpoints, pickups and enemies, with head room so
every obstacle can be timed or jumped. Safe to re-run: maps that already contain obstacles are left alone.
  python3 Tools/harden_levels.py
"""
import re, os

PATH = os.path.join(os.path.dirname(__file__), '..', 'Assets', '_Project', 'Scripts', 'Core', 'Level', 'StoryLayouts.cs')
PLAN = {  # level: (saws, lasers, crushers)
    'SunspireHeights': (2, 0, 0),
    'GearfallCaverns': (2, 0, 1),
    'FrostgearAscent': (2, 2, 1),
    'ClockworkGlacier': (3, 2, 2),
    'EmberWastes': (3, 3, 2),
    'SkyGardens': (3, 3, 2),
    'SunkenClockworks': (3, 3, 2),
    'ToyFortress': (3, 3, 2),
    'ShatteredCrossing': (4, 4, 3),
}
SOLID = set('#X')
KEEP_AWAY = set('PGC')
BUSY = set('PGC?JOmuBTMKFgEVRZfYQDWHL~cs')  # enemies, movers, springs, crates, vines, hiding spots


def load():
    src = open(PATH).read()
    maps = {}
    for m in re.finditer(r'public static readonly string\[\] (\w+) =\s*\{(.*?)\};', src, re.S):
        rows = re.findall(r'"([^"]*)"', m.group(2))
        maps[m.group(1)] = (m.start(2), m.end(2), rows)
    return src, maps


class Map:
    def __init__(self, rows):
        self.h = len(rows)
        self.w = max(len(r) for r in rows)
        self.g = [list(rows[self.h - 1 - y].ljust(self.w, '.')) for y in range(self.h)]  # y-up

    def at(self, x, y):
        return self.g[y][x] if 0 <= x < self.w and 0 <= y < self.h else 'X'

    def rows(self):
        return [''.join(self.g[y]) for y in range(self.h - 1, -1, -1)]

    def near(self, x, y, chars, r):
        for yy in range(y - r, y + r + 1):
            for xx in range(x - r, x + r + 1):
                if self.at(xx, yy) in chars:
                    return True
        return False

    def empty(self, x, y):
        return self.at(x, y) == '.'

    def floor(self, x, y):
        return self.at(x, y - 1) in SOLID


def candidates(m, kind):
    out = []
    for y in range(2, m.h - 2):
        for x in range(3, m.w - 3):
            if kind == 'o':
                ok = all(m.at(xx, y) in '.*' and m.floor(xx, y) for xx in range(x - 1, x + 2)) and \
                     m.empty(x, y) and all(m.at(x, y + k) in '.*' for k in range(1, 4))
                r = 6
            elif kind == 'l':
                ok = m.floor(x, y) and all(m.empty(x, y + k) for k in range(0, 3)) and \
                     all(m.at(x, y + k) in '.*' for k in range(3, 5)) and \
                     m.at(x - 1, y) in '.*' and m.at(x + 1, y) in '.*' and m.floor(x - 1, y) and m.floor(x + 1, y)
                r = 6
            else:  # crusher: body at (x..x+1, y+3..y+4), clear drop to the floor at y
                ok = all(m.floor(xx, y) for xx in (x, x + 1)) and \
                     all(m.at(xx, yy) in '.*' for xx in (x, x + 1) for yy in range(y, y + 3)) and \
                     all(m.empty(xx, yy) for xx in (x, x + 1) for yy in range(y + 3, y + 5))
                r = 7
            if ok and not m.near(x, y, KEEP_AWAY, r) and not m.near(x, y, BUSY - KEEP_AWAY, 2) and not m.near(x, y, 'olv', 5):
                out.append((x, y))
    return out


def place(m, kind, count):
    placed = 0
    for i in range(count):
        cands = candidates(m, kind)
        if not cands:
            break
        # Spread across the level: aim for evenly spaced x positions.
        target = m.w * (i + 1) / (count + 1)
        x, y = min(cands, key=lambda c: (abs(c[0] - target), c[1]))
        if kind == 'o':
            m.g[y][x] = 'o'
        elif kind == 'l':
            for k in range(3):
                m.g[y + k][x] = 'l'
        else:
            m.g[y + 4][x] = 'v'
        placed += 1
    return placed


def main():
    src, maps = load()
    edits = []
    for name, (saws, lasers, crushers) in PLAN.items():
        if name not in maps:
            continue
        start, end, rows = maps[name]
        if any(ch in ''.join(rows) for ch in 'olv'):
            print(name, 'already has obstacles, skipped')
            continue
        m = Map(rows)
        got = (place(m, 'v', crushers), place(m, 'l', lasers), place(m, 'o', saws))
        print(f'{name}: crushers {got[0]}/{crushers}, lasers {got[1]}/{lasers}, saws {got[2]}/{saws}')
        indent = '            '
        body = '\n' + '\n'.join(f'{indent}"{r}",' for r in m.rows()) + '\n        '
        edits.append((start, end, body))
    for start, end, body in sorted(edits, reverse=True):
        src = src[:start] + body + src[end:]
    open(PATH, 'w').write(src)


if __name__ == '__main__':
    main()

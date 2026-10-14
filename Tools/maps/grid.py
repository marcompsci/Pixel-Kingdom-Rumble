class G:
    def __init__(s, w, h):
        s.w, s.h = w, h
        s.g = [['.'] * w for _ in range(h)]
        s.rect(0, 0, w, 1, 'X'); s.rect(0, h - 1, w, 1, 'X'); s.rect(0, 0, 1, h, 'X'); s.rect(w - 1, 0, 1, h, 'X')
    def set(s, x, y, c): s.g[y][x] = c
    def rect(s, x, y, w, h, c):
        for yy in range(y, y + h):
            for xx in range(x, x + w): s.g[yy][xx] = c
    def row(s, x, y, text):
        for i, ch in enumerate(text):
            if ch != ' ': s.g[y][x + i] = ch
    def rows(s): return [''.join(s.g[y]) for y in range(s.h - 1, -1, -1)]
    def cs(s, name):
        out = [f'        public static readonly string[] {name} =', '        {']
        out += [f'            "{r}",' for r in s.rows()]
        out += ['        };']
        return '\n'.join(out)

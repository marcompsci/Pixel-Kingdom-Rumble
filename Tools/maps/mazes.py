from grid import G

def maze1():
    """World 1 bonus: Gearwork Maze. Keys, two locked doors, a switch bridge, lasers and a crusher."""
    m = G(76, 16)
    m.rect(1, 1, 74, 1, '#')                        # floor (top at y=2)
    m.set(3, 2, 'P')
    m.row(6, 2, '*.*.*')
    m.set(10, 2, 'o')                               # saw rolling on the floor
    # staircase of planks up to key 1
    m.row(5, 5, '===')
    m.row(9, 7, '===')
    m.row(13, 9, '====')
    m.set(15, 10, 'k')
    m.set(14, 10, '*')
    # wall with door 1
    m.rect(18, 2, 2, 12, 'X')
    m.rect(18, 2, 1, 3, 'n')
    m.rect(19, 2, 1, 3, '.')
    # spike pit bridged by red then blue blocks, a switch on a pillar between
    m.rect(21, 1, 16, 1, '#')
    m.set(20, 2, '!')                               # reset switch before the pit (after a fall)
    m.row(22, 2, '^^^^^^^^^^^^^^^')
    m.rect(22, 2, 5, 1, '^')
    m.rect(22, 3, 5, 1, '1')
    m.rect(27, 2, 2, 2, 'X')
    m.set(27, 4, '!')
    m.rect(29, 2, 7, 1, '^')
    m.rect(29, 3, 7, 1, '2')
    m.row(23, 6, '*.*.*')
    m.set(37, 2, 'C')
    # laser alley with a crusher
    m.rect(41, 2, 1, 4, 'l')
    m.rect(46, 2, 1, 4, 'l')
    m.set(50, 6, 'v')                               # body y5..6, drops to the floor
    m.row(42, 2, '*.*')
    m.row(53, 5, '====')
    m.set(55, 6, 'k')                               # key 2 above the alley exit
    m.rect(58, 2, 2, 12, 'X')
    m.rect(58, 2, 1, 3, 'n')
    m.rect(59, 2, 1, 3, '.')
    m.row(62, 2, '*.*.+')
    m.set(70, 2, 'G')
    return m

def maze2():
    """World 2 bonus: Frozen Vault. Switch-block climb, saws, lasers in rhythm, three keys."""
    m = G(84, 18)
    m.rect(1, 1, 82, 1, '#')
    m.set(3, 2, 'P')
    # switch tower: red ledge, solid ledge with a switch, blue ledge, solid ledge with a switch, red ledge + key
    m.set(8, 2, '!')                                # floor switch resets the colours after a fall
    m.rect(11, 4, 3, 1, '1')
    m.rect(14, 6, 3, 1, 'X')
    m.set(15, 7, '!')
    m.rect(11, 8, 3, 1, '2')
    m.rect(14, 10, 3, 1, 'X')
    m.set(16, 11, '!')
    m.rect(11, 12, 3, 1, '1')
    m.set(12, 13, 'k')
    m.set(12, 9, '*')
    m.set(14, 11, '*')
    m.rect(22, 2, 2, 14, 'X')
    m.rect(22, 2, 1, 3, 'n')
    m.rect(23, 2, 1, 3, '.')
    # saw corridor with a low ceiling (no jumping over the top)
    m.rect(25, 6, 22, 1, 'X')
    m.set(28, 2, 'o')
    m.set(36, 2, 'o')
    m.set(44, 2, 'o')
    m.row(31, 3, '=')
    m.row(39, 3, '=')
    m.row(26, 2, '*')
    m.set(48, 2, 'C')
    # laser rhythm hall with key 2 on a high plank, crusher row
    for x in (51, 54, 57, 60):
        m.rect(x, 2, 1, 5, 'l')
    m.set(55, 2, 'k')                               # key 2 waits between the beams
    m.set(63, 7, 'v')
    m.set(66, 7, 'v')
    m.rect(69, 2, 2, 14, 'X')
    m.rect(69, 2, 1, 4, 'n')
    m.rect(70, 2, 1, 4, '.')
    m.rect(69, 6, 2, 1, 'X')
    m.row(72, 2, '*.*.+')
    m.set(79, 2, 'G')
    return m

def maze3():
    """World 3 bonus: Shattered Labyrinth. Everything at once: switch flips mid-jump, saws, lasers, crushers."""
    m = G(96, 18)
    m.rect(1, 1, 94, 1, '#')
    m.set(3, 2, 'P')
    m.rect(10, 2, 1, 4, 'l')
    m.set(13, 2, 'o')
    m.set(14, 6, 'v')
    m.set(18, 2, '!')
    # lava pit bridged by red, blue, red with switches on pillars
    m.rect(21, 2, 6, 1, 'a')
    m.rect(21, 3, 6, 1, '1')
    m.rect(27, 2, 2, 2, 'X')
    m.set(27, 4, '!')
    m.rect(29, 2, 6, 1, 'a')
    m.rect(29, 3, 6, 1, '2')
    m.rect(35, 2, 2, 2, 'X')
    m.set(35, 4, '!')
    m.rect(37, 2, 6, 1, 'a')
    m.rect(37, 3, 6, 1, '1')
    m.set(44, 2, 'C')
    # key 1 above a crusher pair
    m.row(47, 8, '======')                          # ride a crusher up, then hop to the key
    m.set(49, 9, 'k')
    m.set(47, 6, 'v')
    m.set(51, 6, 'v')
    m.rect(55, 2, 2, 14, 'X')
    m.rect(55, 2, 1, 3, 'n')
    m.rect(56, 2, 1, 3, '.')
    # saw + laser gauntlet
    m.set(59, 2, 'o')
    m.rect(63, 2, 1, 5, 'l')
    m.set(66, 2, 'o')
    m.rect(70, 2, 1, 5, 'l')
    m.row(60, 5, '==')
    m.row(67, 5, '==')
    m.set(68, 6, 'k')
    m.set(73, 2, 'C')
    m.set(77, 6, 'v')
    m.rect(81, 2, 2, 14, 'X')
    m.rect(81, 2, 1, 3, 'n')
    m.rect(82, 2, 1, 3, '.')
    m.row(84, 2, '*.*.+')
    m.set(91, 2, 'G')
    for (x, y) in ((6, 2), (8, 2), (24, 6), (32, 6), (40, 6), (50, 9), (61, 6), (75, 2)):
        m.set(x, y, '*')
    return m

if __name__ == '__main__':
    for f, n in ((maze1, 'GearworkMaze'), (maze2, 'FrozenVault'), (maze3, 'ShatteredLabyrinth')):
        m = f()
        print(n); print('\n'.join(m.rows())); print()

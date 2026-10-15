// Edit the text directly; PKR > Build All Scenes rebuilds the scenes from it.
using System.Collections.Generic;

namespace PKR.Core
{
    /// <summary>
    /// Maze levels (one per Story world, see AsciiLevel for the legend): keys and locked doors, red/blue switch blocks,
    /// crushers, laser gates and saw blades. Shorter than story levels and built around quick thinking rather than shards.
    /// </summary>
    public static class MazeLayouts
    {
        /// <summary>World 1 maze: two locked doors, a sun/moon bridge over spikes, a laser gallery and a crusher.</summary>
        public static readonly string[] GearworkMaze =
        {
            "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
            "X..........................................................................X",
            "X.................XX......................................XX...............X",
            "X.................XX......................................XX...............X",
            "X.................XX......................................XX...............X",
            "X.............*k..XX......................................XX...............X",
            "X............====.XX......................................XX...............X",
            "X.................XX......................................XX...............X",
            "X........===......XX......................................XX...............X",
            "X.................XX...*.*.*......................v....k..XX...............X",
            "X....===..........XX.....................l....l......====.XX...............X",
            "X.................n........!.............l....l...........n................X",
            "X.................n...11111XX2222222.....l....l...........n................X",
            "X..P..*.*.o.......n.!.^^^^^XX^^^^^^^^C...l*.*.l...........n...*.*.+...G....X",
            "X##########################################################################X",
            "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
        };

        /// <summary>World 2 maze: the switch tower, a saw corridor, four lasers and twin crushers.</summary>
        public static readonly string[] FrozenVault =
        {
            "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
            "X..................................................................................X",
            "X.....................XX.............................................XX............X",
            "X.....................XX.............................................XX............X",
            "X...........k.........XX.............................................XX............X",
            "X..........111........XX.............................................XX............X",
            "X.............*.!.....XX.............................................XX............X",
            "X.............XXX.....XX.............................................XX............X",
            "X...........*.........XX.............................................XX............X",
            "X..........222........XX.............................................XX............X",
            "X..............!......XX.......................................v..v..XX............X",
            "X.............XXX.....XX.XXXXXXXXXXXXXXXXXXXXXX....l..l..l..l........XX............X",
            "X.....................XX...........................l..l..l..l........n.............X",
            "X..........111........n............................l..l..l..l........n.............X",
            "X.....................n........=.......=...........l..l..l..l........n.............X",
            "X..P....!.............n...*.o.......o.......o...C..l..lk.l..l........n..*.*.+..G...X",
            "X##################################################################################X",
            "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
        };

        /// <summary>World 3 maze: lava bridges in three colours, a crusher ride up to the key, a saw and laser gauntlet.</summary>
        public static readonly string[] ShatteredLabyrinth =
        {
            "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
            "X..............................................................................................X",
            "X......................................................XX........................XX............X",
            "X......................................................XX........................XX............X",
            "X......................................................XX........................XX............X",
            "X......................................................XX........................XX............X",
            "X......................................................XX........................XX............X",
            "X......................................................XX........................XX............X",
            "X................................................k*....XX........................XX............X",
            "X..............................................======..XX........................XX............X",
            "X......................................................XX........................XX............X",
            "X.............v.........*.......*.......*......v...v...XX....*.l....k.l......v...XX............X",
            "X.........l............................................XX...==.l...==.l..........XX............X",
            "X.........l................!.......!...................n.......l......l..........n.............X",
            "X.........l..........111111XX222222XX111111............n.......l......l..........n.............X",
            "X..P..*.*.l..o....!..aaaaaaXXaaaaaaXXaaaaaa.C..........n...o...l..o...l..C.*.....n..*.*.+..G...X",
            "X##############################################################################################X",
            "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
        };

        /// <summary>World 4 maze: ride the updraft shaft to the first key, sun/moon bridges over spikes, a laser-and-crusher hall.</summary>
        public static readonly string[] CelestialMaze =
        {
            "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
            "X........X.............X............................................X......................X",
            "X........X.............X............................................X......................X",
            "X........X........*.k..X............................................X......................X",
            "X........X%%%%%.======.X......................................i..k..X......................X",
            "X........X%%%%%X.......X..................................========..X......................X",
            "X........X%%%%%X.......X....................................%%%%....X......................X",
            "X........X%%%%%X.......X....................................%%%%....X......................X",
            "X........X%%%%%X.......X...*.*.*.*.*.*......................%%%%....X......................X",
            "X........X%%%%%X.......X....................................%%%%....X......................X",
            "X........X%%%%%X.......X............1111......X......X......%%%%....X......................X",
            "X........X%%%%%X.......X.........!............l..v...l......%%%%....X......................X",
            "X........X%%%%%X.......n.......2222...........l......l......%%%%....n.=====................X",
            "X.........%%%%%X.......n......................l......l......%%%%....n......................X",
            "X.........%%%%%X.......n..1111................l......l......%%%%....n......................X",
            "X..P..*.*.%%%%%X...!...n...................C..l*.....*..t...%%%%....n..o.....C..*.*.+...G..X",
            "X#########^^^^^###########^^^^^^^^^^^^^^^^#################^^^^^^##########################X",
            "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
        };

        public static IEnumerable<KeyValuePair<string, string[]>> All()
        {
            yield return new KeyValuePair<string, string[]>("GearworkMaze", GearworkMaze);
            yield return new KeyValuePair<string, string[]>("FrozenVault", FrozenVault);
            yield return new KeyValuePair<string, string[]>("ShatteredLabyrinth", ShatteredLabyrinth);
            yield return new KeyValuePair<string, string[]>("CelestialMaze", CelestialMaze);
        }
    }
}

using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Project physics layers. Created by PKR > Setup Layers (run automatically by the scene builders).
    /// Indices are fixed so scenes and prefabs stay consistent across machines.
    /// </summary>
    public static class PKRLayers
    {
        public const int Ground = 8;
        public const int Player = 9;
        public const int Enemy = 10;
        public const int Hitbox = 11;
        public const int Pickup = 12;
        public const int Hazard = 13;

        public static readonly (int index, string name)[] All =
        {
            (Ground, "Ground"), (Player, "Player"), (Enemy, "Enemy"),
            (Hitbox, "Hitbox"), (Pickup, "Pickup"), (Hazard, "Hazard"),
        };

        public static LayerMask GroundMask => 1 << Ground;
    }
}

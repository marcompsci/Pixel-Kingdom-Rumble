using PKR.Core;
using UnityEngine;

namespace PKR
{
    public enum StationKind { WallWeapon, MysteryCrate, Perk, Forge, Door, Lantern }

    /// <summary>
    /// Something in the Zombie Survival arena the hero can use: a wall weapon (chalk outline), the mystery crate, a perk
    /// shrine, the upgrade forge, a paid door, or one of the secret jack-o'-lanterns (lit with a melee hit, not USE).
    /// ZombieSurvival owns the rules; a station just knows what it is and where.
    /// </summary>
    public class ZombieStation : MonoBehaviour
    {
        public StationKind kind;
        [Tooltip("Wall weapon: index into ZombieArmory.Wall. Perk: (int)PerkKind.")]
        public int index;
        public float reach = 1.4f;
        public SpriteRenderer art;
        [Tooltip("Door: the solid blocker that opens.")]
        public Collider2D blocker;

        public bool Used { get; set; }
        /// <summary>Busy (the crate spinning, the forge working).</summary>
        public float BusyUntil { get; set; }

        public ZombieWeapon Weapon => ZombieArmory.Wall[Mathf.Clamp(index, 0, ZombieArmory.Wall.Length - 1)];
        public PerkKind Perk => (PerkKind)Mathf.Clamp(index, 0, 3);

        public bool InReach(Vector2 hero)
        {
            Vector2 c = blocker != null ? (Vector2)blocker.bounds.center : (Vector2)transform.position + Vector2.up * 0.6f;
            Vector2 d = hero - c;
            float rx = blocker != null ? blocker.bounds.extents.x + reach * 0.8f : reach;
            float ry = blocker != null ? blocker.bounds.extents.y + 0.6f : 1.5f;
            return Mathf.Abs(d.x) <= rx && Mathf.Abs(d.y) <= ry;
        }

        /// <summary>The paid door swings open (fades out, stops blocking).</summary>
        public void OpenDoor()
        {
            Used = true;
            if (blocker != null) blocker.enabled = false;
            foreach (var sr in GetComponentsInChildren<SpriteRenderer>()) sr.color = new Color(1f, 1f, 1f, 0.15f);
        }

        /// <summary>A secret lantern lights up.</summary>
        public void Light()
        {
            Used = true;
            if (art != null) art.color = new Color(1f, 1f, 0.75f, 1f);
            var glow = transform.Find("Glow");
            if (glow != null) glow.gameObject.SetActive(true);
        }

        void Update()
        {
            // Wall outlines and shrines breathe a little so they read as interactive.
            if (art == null || kind == StationKind.Door || kind == StationKind.Lantern) return;
            float k = 0.85f + Mathf.Sin(Time.time * 2.4f + transform.position.x) * 0.15f;
            var c = art.color;
            art.color = new Color(c.r, c.g, c.b, kind == StationKind.WallWeapon ? k : 1f);
        }
    }
}

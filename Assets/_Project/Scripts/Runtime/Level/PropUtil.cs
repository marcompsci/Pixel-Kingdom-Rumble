using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>Shared helpers for props that react to the player without physics callbacks.</summary>
    static class PropUtil
    {
        /// <summary>The player's motor and body bounds, or false if there is no live player.</summary>
        public static bool TryPlayer(out PlatformerMotor2D motor, out Bounds bounds)
        {
            motor = null;
            bounds = default;
            var p = PlayerMarker.Current;
            if (p == null || !p.TryGetComponent(out motor) || motor.Body == null) return false;
            if (p.TryGetComponent(out Damageable d) && d.IsDead) return false;
            var col = p.GetComponent<Collider2D>();
            bounds = col != null ? col.bounds : new Bounds(p.transform.position, new Vector3(0.7f, 1.4f, 0f));
            return true;
        }
    }

}

using UnityEngine;

namespace PKR
{
    /// <summary>Where a pooled pickup is placed when the level starts. Collected pickups don't come back on respawn.</summary>
    public class PickupSpot : MonoBehaviour
    {
        public PickupKind kind = PickupKind.StarShard;
        [Min(1)] public int value = 1;

        void OnDrawGizmos()
        {
            Gizmos.color = kind == PickupKind.StarShard ? new Color(1f, 0.85f, 0.3f, 0.9f) : new Color(1f, 0.4f, 0.5f, 0.9f);
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.4f);
        }
    }
}

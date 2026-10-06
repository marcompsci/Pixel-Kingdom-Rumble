using UnityEngine;

namespace PKR
{
    /// <summary>Where a pooled enemy appears when the level starts (and after the player respawns).</summary>
    public class EnemySpawnPoint : MonoBehaviour
    {
        public EnemyDefinition enemy;
        [Tooltip("+1 = start walking right, -1 = left.")]
        public int startDirection = -1;

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, 0.4f);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.right * startDirection * 0.8f);
        }
    }
}

using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Respawn flag. Touching it activates it (only forward progress counts). Respawn happens at RespawnPoint.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Checkpoint : MonoBehaviour
    {
        [Tooltip("Order along the level, starting at 0.")]
        public int index;
        public SpriteRenderer flag;
        public Color inactiveColor = new Color(0.55f, 0.55f, 0.6f, 1f);
        public Color activeColor = new Color32(46, 220, 190, 255);
        public Vector2 respawnOffset = new Vector2(0f, 0.8f);

        public bool IsActive { get; private set; }
        public Vector2 RespawnPoint => (Vector2)transform.position + respawnOffset;

        void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
            SetVisual(false);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (IsActive || !TriggerUtil.IsPlayer(other, out _)) return;
            var flow = LevelFlowController.Current;
            if (flow != null) flow.ReachCheckpoint(this);
        }

        public void Activate()
        {
            IsActive = true;
            SetVisual(true);
        }

        void SetVisual(bool on)
        {
            if (flag != null) flag.color = on ? activeColor : inactiveColor;
        }
    }
}

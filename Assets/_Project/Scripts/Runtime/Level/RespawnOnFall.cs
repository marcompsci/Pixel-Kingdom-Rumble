using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Sandbox safety net: if the fighter falls below killY, put it back at the respawn point.
    /// Story levels replace this with the checkpoint system (increment 4).
    /// </summary>
    [RequireComponent(typeof(PlatformerMotor2D))]
    public class RespawnOnFall : MonoBehaviour
    {
        public float killY = -12f;
        public Vector2 respawnPoint;
        public float respawnInvulnerability = 1.5f;

        PlatformerMotor2D _motor;

        void Awake()
        {
            _motor = GetComponent<PlatformerMotor2D>();
            if (respawnPoint == Vector2.zero) respawnPoint = transform.position;
        }

        void FixedUpdate()
        {
            if (transform.position.y >= killY) return;
            // TryGetComponent, not GetComponent()?.: Unity's fake-null objects break the ?. operator.
            if (TryGetComponent(out NovaAbilities nova)) nova.Cancel();
            _motor.Teleport(respawnPoint);
            if (TryGetComponent(out Invulnerability inv)) inv.Grant(respawnInvulnerability, blink: true);
            if (CameraFollow2D.Main != null) CameraFollow2D.Main.SnapToTarget();
        }
    }
}

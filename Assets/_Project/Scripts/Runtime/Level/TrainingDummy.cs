using System.Collections;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Sandbox target: takes hits, gets knocked around, and resets to its post (full HP / pips)
    /// after being defeated or knocked off the course.
    /// </summary>
    [RequireComponent(typeof(Damageable), typeof(PlatformerMotor2D))]
    public class TrainingDummy : MonoBehaviour
    {
        public float respawnDelay = 1f;
        public float killY = -12f;
        [Tooltip("Face this way while idle (+1 right, -1 left).")]
        public int idleFacing = -1;

        Damageable _d;
        PlatformerMotor2D _motor;
        Vector2 _home;
        bool _respawning;

        void Awake()
        {
            _d = GetComponent<Damageable>();
            _motor = GetComponent<PlatformerMotor2D>();
            _home = transform.position;
        }

        void Start() => _motor.SetFacing(idleFacing);

        void OnEnable() => _d.Died += OnDied;
        void OnDisable()
        {
            _d.Died -= OnDied;
            _respawning = false; // coroutines stop when disabled; don't stay stuck
        }

        void OnDied()
        {
            if (!_respawning) StartCoroutine(Respawn());
        }

        void FixedUpdate()
        {
            if (!_respawning && transform.position.y < killY) StartCoroutine(Respawn());
        }

        IEnumerator Respawn()
        {
            _respawning = true;
            yield return new WaitForSeconds(respawnDelay);
            _motor.Teleport(_home);
            _motor.SetFacing(idleFacing);
            _d.ResetState();
            _respawning = false;
        }
    }
}

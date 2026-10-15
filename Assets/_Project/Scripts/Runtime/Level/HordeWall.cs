using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Haunted Escape: the zombie horde rolling in from the left. It starts a few seconds after the level begins,
    /// shambles right at a steady pace and never falls too far behind (it rubber-bands to keep the pressure on).
    /// Touching it costs a heart and flings the hero forward. After a respawn it backs off behind the checkpoint.
    /// The look: a dark fog bank with a row of shambling zombies (frames swapped here).
    /// </summary>
    public class HordeWall : MonoBehaviour
    {
        public float speed = 2.3f;
        public float startDelay = 3.5f;
        [Tooltip("The horde never trails the hero by more than this (world units).")]
        public float maxGap = 15f;
        [Tooltip("After a respawn it restarts this far behind the hero.")]
        public float respawnGap = 12f;
        public SpriteRenderer[] walkers;
        public Sprite[] walkFrames;
        public Transform fog;

        float _x, _clock, _hurtCooldown, _groanCooldown;
        Damageable _hero;

        void OnEnable() => EventBus<PlayerRespawned>.Subscribe(OnRespawned);
        void OnDisable() => EventBus<PlayerRespawned>.Unsubscribe(OnRespawned);

        void Start()
        {
            _x = transform.position.x;
            _groanCooldown = 2f;
        }

        void OnRespawned(PlayerRespawned e)
        {
            var hero = PlayerMarker.Current;
            if (hero != null) _x = Mathf.Min(_x, hero.transform.position.x - respawnGap);
            _clock = Mathf.Min(_clock, startDelay - 1.5f); // a short breather
        }

        void Update()
        {
            var flow = LevelFlowController.Current;
            if (flow != null && flow.IsComplete) return;
            if (Services.State != null && Services.State.State != GameState.Playing) return;
            var hero = PlayerMarker.Current;
            float dt = Time.deltaTime;
            _clock += dt;
            if (hero != null && _clock > startDelay)
            {
                float hx = hero.transform.position.x;
                _x += speed * dt;
                if (hx - _x > maxGap) _x = hx - maxGap;
            }
            transform.position = new Vector3(_x, transform.position.y, transform.position.z);

            // Shamble: each walker bobs and steps through the frames, out of step with its neighbours.
            if (walkers != null && walkFrames != null && walkFrames.Length > 0)
                for (int i = 0; i < walkers.Length; i++)
                {
                    if (walkers[i] == null) continue;
                    float t = Time.time * 7f + i * 1.7f;
                    walkers[i].sprite = walkFrames[(int)t % walkFrames.Length];
                }
            if (fog != null) fog.localScale = new Vector3(1f + Mathf.Sin(Time.time * 1.3f) * 0.05f, 1f, 1f);

            _hurtCooldown -= dt;
            _groanCooldown -= dt;
            if (hero == null) return;
            float gap = hero.transform.position.x - _x;
            if (gap < 6f && _groanCooldown <= 0f)
            {
                _groanCooldown = Random.Range(2.5f, 4f);
                Sfx.Play(Sfx.Groan, 0.55f, 0.15f);
            }
            if (gap < 0.6f && _hurtCooldown <= 0f)
            {
                _hurtCooldown = 1.1f;
                if (_hero == null || _hero.gameObject != hero.gameObject) _hero = hero.GetComponent<Damageable>();
                if (_hero == null) return;
                var hit = new HitData
                {
                    damage = 1, pipDamage = 1, baseKnockback = 15f, exposedMultiplier = 1f, angleDegrees = 50f,
                    baseHitstunFrames = 16, hitstopFrames = 4, isHeavy = true
                };
                _hero.TakeHit(hit, 1, TeamIds.Enemy, out _, _x - 1f);
                EventBus<GameHint>.Raise(new GameHint { id = "horde", text = "THE HORDE GOT YOU! KEEP RUNNING!" });
            }
        }
    }
}

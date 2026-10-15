using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// The First Turned. Only the true relic weapon hurts him (anything else: "IMMUNE"). Phase 1: stalks and lunges,
    /// raising the dead around him. Phase 2 (below 60%): howls for his blood hounds and lobs rot. Phase 3 (below 25%):
    /// faster, slams the ground in a ring. When he falls, every zombie in Zombie Land falls with him.
    /// </summary>
    public class ZLBoss : MonoBehaviour, IZLHittable
    {
        public int hp, maxHp;
        ZLGame _game;
        Rigidbody2D _body;
        SpriteRenderer _sr;
        Sprite[] _idle, _walk, _attack;
        float _t, _act, _hurt, _summon, _immuneNote, _lunge, _windup;
        Vector2 _lungeDir;
        bool _dead;
        int _phase = 1;

        public bool Hostile => true;
        public bool Alive => !_dead;
        public int Phase => _phase;
        public float Health01 => maxHp > 0 ? hp / (float)maxHp : 0f;

        public void Init(ZLGame g, Sprite[] idle, Sprite[] walk, Sprite[] attack)
        {
            _game = g; _idle = idle; _walk = walk; _attack = attack;
            maxHp = hp = ZLRules.Health(ZLFoe.FirstTurned, 5);
            _body = gameObject.AddComponent<Rigidbody2D>();
            _body.gravityScale = 0f;
            _body.freezeRotation = true;
            _body.mass = 20f;
            _body.linearDamping = 3f;
            var c = gameObject.AddComponent<CircleCollider2D>();
            c.radius = 0.8f;
            var art = new GameObject("Art");
            art.transform.SetParent(transform, false);
            art.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            art.transform.localScale = Vector3.one * 1.3f;
            _sr = art.AddComponent<SpriteRenderer>();
            if (g.material != null) _sr.sharedMaterial = g.material;
            _act = 2f;
            _summon = 3f;
        }

        public void Hit(int damage, Vector2 dir, ZLGun gun)
        {
            if (_dead) return;
            int real = ZLRules.BossDamage(gun, damage, _game.Puzzle.TrueRelic);
            if (real <= 0)
            {
                if (_immuneNote <= 0f)
                {
                    _immuneNote = 0.8f;
                    ZLFloater.Show(gun != null && gun.isRelic ? "IT DOESN'T HURT HIM" : "IMMUNE", (Vector2)transform.position + Vector2.up * 2f, new Color(0.8f, 0.8f, 1f), 1.3f);
                }
                return;
            }
            hp -= real;
            _hurt = 0.1f;
            ZLFloater.Show(real.ToString(), (Vector2)transform.position + Vector2.up * 2f + Random.insideUnitCircle * 0.4f, new Color(1f, 0.9f, 0.4f));
            int phase = ZLRules.BossPhase(Health01);
            if (phase != _phase)
            {
                _phase = phase;
                ZLFloater.Show(phase == 2 ? "HE HOWLS FOR HIS HOUNDS" : "HE'S ENRAGED", (Vector2)transform.position + Vector2.up * 2.6f, new Color(1f, 0.4f, 0.4f), 1.5f);
                if (phase == 2) _game.SpawnHoundPack(4, transform.position);
                if (Services.Haptics != null) Services.Haptics.Play(HapticStrength.Heavy);
            }
            if (hp <= 0)
            {
                _dead = true;
                _game.BossDefeated(this);
            }
        }

        void FixedUpdate()
        {
            if (_dead || _game == null || _game.Paused) { if (_body != null) _body.linearVelocity = Vector2.zero; return; }
            float dt = Time.fixedDeltaTime;
            _t += dt; _act -= dt; _hurt -= dt; _summon -= dt; _immuneNote -= dt;
            Vector2 pos = _body.position;
            Vector2 target = _game.TargetPos;
            Vector2 to = target - pos;
            float speedMul = _phase == 3 ? 1.5f : 1f;

            if (_windup > 0f)
            {
                _windup -= dt;
                _body.linearVelocity = Vector2.zero;
                if (_windup <= 0f) { _lunge = 0.45f; Sfx.Play(Sfx.HitHeavy, 0.9f); }
                return;
            }
            if (_lunge > 0f)
            {
                _lunge -= dt;
                _body.linearVelocity = _lungeDir * 13f * speedMul;
                if (Vector2.Distance(pos, target) < 1.4f) { _game.Bite(null, ZLRules.Bite(ZLFoe.FirstTurned)); _lunge = 0f; }
                return;
            }

            // Walk at the hero (flow field around obstacles)
            var dir = _game.City.Sees(pos, target) ? to.normalized : _game.Flow.Direction(pos);
            if (dir == Vector2.zero) dir = to.normalized;
            _body.linearVelocity = dir * ZLRules.Speed(ZLFoe.FirstTurned, 5) * speedMul;
            if (to.magnitude < 1.3f && _act <= 0.6f) { _game.Bite(null, ZLRules.Bite(ZLFoe.FirstTurned)); _act = 1.2f; }

            if (_act <= 0f)
            {
                _act = _phase == 3 ? 2.2f : 3.2f;
                int pick = Random.Range(0, _phase >= 2 ? 3 : 2);
                if (pick == 0 && to.magnitude < 9f)
                {
                    _windup = 0.55f;
                    _lungeDir = to.normalized;
                    ZLFloater.Show("!", (Vector2)transform.position + Vector2.up * 2.6f, Color.red, 1.6f);
                }
                else if (pick == 1 || _phase == 3)
                {
                    // ground slam ring
                    ZLFx.Spawn(GlowHalo.Sprite, pos, new Color(0.6f, 0.3f, 0.8f, 0.8f), 0.6f, 2f, 9f, _game.material);
                    if (to.magnitude < 3.5f) _game.Bite(null, 18);
                    Sfx.Play(Sfx.Burst, 0.9f);
                }
                else
                {
                    // lob rot at the hero
                    for (int k = -1; k <= 1; k++)
                    {
                        var aim = Quaternion.Euler(0f, 0f, k * 14f) * to.normalized;
                        ZLBullet.Fire(pos + (Vector2)aim, aim, null, 14, true, _game.ShotSprite("blob"), new Color(0.6f, 0.3f, 0.8f), _game.material, null, 9f, 12f);
                    }
                }
            }
            if (_summon <= 0f)
            {
                _summon = _phase == 1 ? 7f : 10f;
                _game.SummonAround(pos, _phase == 1 ? 3 : 2);
            }
        }

        void LateUpdate()
        {
            if (_sr == null) return;
            var clip = _windup > 0f || _lunge > 0f ? _attack : _body.linearVelocity.sqrMagnitude > 0.1f ? _walk : _idle;
            if (clip != null && clip.Length > 0) _sr.sprite = clip[(int)(_t * 6f) % clip.Length];
            if (Mathf.Abs(_body.linearVelocity.x) > 0.1f) _sr.flipX = _body.linearVelocity.x < 0f;
            _sr.sortingOrder = ZLSort.ByY(transform.position.y);
            _sr.color = _hurt > 0f ? new Color(1f, 0.6f, 0.6f) : _phase == 3 ? new Color(1f, 0.85f, 0.85f) : Color.white;
        }
    }
}

using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// One of the dead (top-down). Wanders until it sees or hears the hero, then hunts: straight at you when close and
    /// in sight, otherwise along the city flow field around buildings. Kinds: shamblers, runners, screamers (call a
    /// horde), bloaters (burst into rot), spitters (lob acid), brutes, blood hounds and the Heralds.
    /// </summary>
    public class ZLZombie : MonoBehaviour, IZLHittable
    {
        public ZLFoe kind;
        public int threat = 1;
        public int hp, maxHp;
        public float speed;
        public bool hunting;
        /// <summary>Set by missions: the Herald or a mission spawn (not despawned when far away).</summary>
        public bool pinned;
        public string missionTag;

        ZLGame _game;
        Rigidbody2D _body;
        SpriteRenderer _sr;
        CircleCollider2D _col;
        readonly ZLAnim _anim = new ZLAnim();
        Vector2 _wander, _investigate;
        bool _investigating;
        float _think, _bite, _spit, _hurt, _animT, _screamed, _lunge, _lungeCool, _stuck;
        Vector2 _lastPos;
        bool _dead;

        public bool Hostile => true;
        public bool Alive => !_dead;
        public Rigidbody2D Body => _body;

        public void Init(ZLGame g, ZLFoe k, int districtThreat, Sprite[] idle, Sprite[] move, Sprite[] hurt, Color tint, float scale)
        {
            _game = g; kind = k; threat = districtThreat;
            maxHp = hp = ZLRules.Health(k, threat);
            speed = ZLRules.Speed(k, threat) * Random.Range(0.85f, 1.15f);
            _body = gameObject.AddComponent<Rigidbody2D>();
            _body.gravityScale = 0f;
            _body.freezeRotation = true;
            _body.mass = k == ZLFoe.Brute || k == ZLFoe.Herald ? 4f : 1f;
            _body.linearDamping = 4f;
            _col = gameObject.AddComponent<CircleCollider2D>();
            _col.radius = k == ZLFoe.Brute || k == ZLFoe.Herald ? 0.55f : k == ZLFoe.Hound ? 0.3f : 0.32f;
            var art = new GameObject("Art");
            art.transform.SetParent(transform, false);
            art.transform.localPosition = new Vector3(0f, 0.3f * scale, 0f);
            art.transform.localScale = Vector3.one * scale;
            _sr = art.AddComponent<SpriteRenderer>();
            _sr.color = tint;
            if (g.material != null) _sr.sharedMaterial = g.material;
            _anim.idle = idle; _anim.move = move; _anim.hurt = hurt;
            _anim.fps = k == ZLFoe.Hound ? 12f : 6f;
            _wander = Random.insideUnitCircle.normalized;
            _think = Random.value;
            _lastPos = transform.position;
        }

        public void Hit(int damage, Vector2 dir, ZLGun gun)
        {
            if (_dead) return;
            if (_game.InstaKills(kind)) damage = Mathf.Max(damage, hp);
            hp -= damage;
            _hurt = 0.12f;
            if (_game.IsLastStand && _game.Stand != null) _game.Stand.ZombieHit(this, hp <= 0);
            _body.linearVelocity += dir * (kind == ZLFoe.Brute || kind == ZLFoe.Herald ? 0.8f : 3f);
            if (!hunting) StartHunt();
            ZLFx.Spawn(_game.Sprite("Props/blood"), (Vector2)transform.position + Random.insideUnitCircle * 0.2f, Color.white, 0.6f, 0.5f, 0.6f, _game.material);
            if (hp <= 0) Die();
        }

        public void StartHunt()
        {
            if (hunting) return;
            hunting = true;
            if (kind == ZLFoe.Screamer && _screamed <= 0f)
            {
                _screamed = 1f;
                ZLFloater.Show("SCREAM!", transform.position, new Color(0.9f, 0.6f, 1f), 1.3f);
                Sfx.Play(Sfx.Spotted, 0.9f);
                _game.CallHorde(transform.position);
            }
        }

        public void Investigate(Vector2 where)
        {
            if (hunting) return;
            _investigating = true;
            _investigate = where;
        }

        void Die()
        {
            if (_dead) return;
            _dead = true;
            _game.ZombieDied(this);
            if (kind == ZLFoe.Bloater)
            {
                ZLFx.Spawn(GlowHalo.Sprite, transform.position, new Color(0.55f, 0.85f, 0.35f, 0.7f), 1.2f, 2.5f, 2f, _game.material);
                _game.Explode(transform.position, 2.2f, 12, false);
            }
            ZLFx.Spawn(_game.Sprite("Props/blood"), transform.position, Color.white, 6f, 1.1f, 0f, _game.material, ZLSort.Decal + 1);
            Destroy(gameObject);
        }

        /// <summary>Removed quietly (too far away, or the First Turned fell).</summary>
        public void Vanish(bool withFx)
        {
            if (_dead) return;
            _dead = true;
            if (withFx) ZLFx.Spawn(GlowHalo.Sprite, transform.position, new Color(0.7f, 0.6f, 0.9f, 0.8f), 0.8f, 1f, 2f, _game.material);
            Destroy(gameObject);
        }

        void FixedUpdate()
        {
            if (_dead || _game == null) return;
            if (_game.Paused) { _body.linearVelocity = Vector2.zero; return; }
            float dt = Time.fixedDeltaTime;
            _bite -= dt; _spit -= dt; _hurt -= dt; _lungeCool -= dt; _think -= dt;
            Vector2 pos = _body.position;
            Vector2 target = _game.TargetPos;
            float dist = Vector2.Distance(pos, target);

            // Senses
            if (!hunting && _think <= 0f)
            {
                _think = 0.35f;
                float sight = kind == ZLFoe.Runner || kind == ZLFoe.Hound || kind == ZLFoe.Screamer ? 11f : 7f + threat * 0.5f;
                if (_game.PlayerHidden) sight *= 0.4f;
                if (dist < sight && _game.City.Sees(pos, target)) StartHunt();
                if (kind == ZLFoe.Hound || pinned && kind == ZLFoe.Herald && dist < 9f) StartHunt();
            }
            if (hunting && dist > 26f && kind != ZLFoe.Hound && !pinned) hunting = false;

            Vector2 dir;
            float sp = speed;
            if (hunting)
            {
                bool close = dist < 3.5f && _game.City.Sees(pos, target);
                dir = close ? (target - pos).normalized : _game.Flow.Direction(pos);
                if (dir == Vector2.zero) dir = (target - pos).normalized;
                // Hounds lunge, spitters keep their distance and spit
                if (kind == ZLFoe.Hound && dist < 3.2f && _lungeCool <= 0f && close)
                {
                    _lunge = 0.25f; _lungeCool = 1.6f;
                }
                if (_lunge > 0f) { _lunge -= dt; sp *= 2.2f; }
                if (kind == ZLFoe.Spitter && dist < 7f && close || kind == ZLFoe.Spitter && dist < 7f && _game.City.Sees(pos, target))
                {
                    if (dist < 4f) dir = -dir * 0.6f; else dir = Vector2.zero;
                    if (_spit <= 0f)
                    {
                        _spit = 2.4f;
                        var aim = (target - pos).normalized;
                        ZLBullet.Fire(pos + aim * 0.5f, aim, null, 8, true, _game.ShotSprite("blob"), new Color(0.6f, 0.9f, 0.3f), _game.material, _col, 7f, 8f);
                    }
                }
                // Bite
                float reach = _col.radius + 0.55f;
                if (dist < reach + (_game.PlayerDriving ? 0.6f : 0f) && _bite <= 0f)
                {
                    _bite = kind == ZLFoe.Hound ? 0.7f : 1.1f;
                    _game.Bite(this, ZLRules.Bite(kind));
                }
            }
            else if (_investigating)
            {
                dir = (_investigate - pos);
                if (dir.magnitude < 1.5f) _investigating = false;
                dir = _game.City.Sees(pos, _investigate) ? dir.normalized : (_game.Flow.Reaches(pos) ? _game.Flow.Direction(pos) : dir.normalized);
                sp *= 0.8f;
            }
            else
            {
                if (Random.value < dt * 0.3f) _wander = Random.insideUnitCircle.normalized;
                dir = _wander;
                sp *= 0.35f;
                if (!_game.City.Walkable(pos + _wander * 0.8f)) _wander = -_wander;
            }

            // Unstick: if pressing against something, slide sideways for a moment
            if (Vector2.Distance(pos, _lastPos) < 0.01f && dir.sqrMagnitude > 0.01f) _stuck += dt; else _stuck = 0f;
            _lastPos = pos;
            if (_stuck > 0.5f) { dir = Vector2.Perpendicular(dir) * (Random.value < 0.5f ? 1f : -1f); _stuck = 0f; }

            var want = dir * sp;
            _body.linearVelocity = Vector2.MoveTowards(_body.linearVelocity, want, 30f * dt);
        }

        void LateUpdate()
        {
            if (_sr == null) return;
            _animT += Time.deltaTime;
            bool moving = _body != null && _body.linearVelocity.sqrMagnitude > 0.05f;
            var s = _anim.Pick(moving, _hurt > 0f, _animT);
            if (s != null) _sr.sprite = s;
            if (_body != null && Mathf.Abs(_body.linearVelocity.x) > 0.05f) _sr.flipX = _body.linearVelocity.x < 0f;
            _sr.sortingOrder = ZLSort.ByY(transform.position.y);
            if (_hurt > 0f) _sr.color = Color.Lerp(_sr.color, Color.white, 0.5f);
        }
    }
}

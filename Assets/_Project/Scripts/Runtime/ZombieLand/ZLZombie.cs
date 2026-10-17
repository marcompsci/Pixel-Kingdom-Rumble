using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// One of the dead (top-down). Wanders until it sees or hears the hero, then hunts: straight at you when close and
    /// in sight, otherwise along the city flow field around buildings. Kinds: shamblers, runners, screamers (call a
    /// horde), bloaters (burst into rot), spitters (lob acid), brutes, blood hounds and the Heralds; riot zombies (a shield
    /// that stops bullets from the front) and leapers (crouch, then spring at you); splitters (fall into two halves),
    /// burrowers (tunnel under the street as an untouchable mound, show "!", then burst up). The Rime Cannon freezes
    /// them; the Kindler turns one to fight the rest for a while.
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
        /// <summary>Online guest: only mirrors the host (no thinking, no biting); NetId matches the host's zombie.</summary>
        public bool Puppet { get; private set; }
        public int NetId { get; set; }
        /// <summary>The hero who hit this zombie last (Last Stand points go to them).</summary>
        public ZLPlayer LastHitBy { get; private set; }
        Vector2 _netTarget;
        readonly NetInterp _interp = new NetInterp();

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
        Color _tint = Color.white;
        float _sidestep;
        Vector2 _sideDir;
        Vector2 _facing = Vector2.right;
        float _frozen, _leapWind, _leapT, _leapCool;
        Vector2 _leapDir;
        SpriteRenderer _shield;
        Vector3? _artBase;
        /// <summary>Frozen by the Rime Cannon (crawls, takes extra damage).</summary>
        public bool Frozen => _frozen > 0f;
        public bool Leaping => _leapT > 0f;
        public bool Crouching => _leapWind > 0f;
        public Vector2 Facing => _facing;

        // burrowers
        bool _under;
        float _underT, _burrowCool = 0.8f, _surfaceTell;
        SpriteRenderer _mound;
        /// <summary>Under the street (or about to burst up): bullets pass over it and nothing can hurt it.</summary>
        public bool Burrowed => _under || _surfaceTell > 0f;

        // kindled (the Kindler)
        float _kindled, _allyBite, _allySeek;
        ZLPlayer _kindledBy;
        ZLZombie _prey;
        /// <summary>Kindled: fights the other dead for a while, then burns out.</summary>
        public bool Kindled => _kindled > 0f;
        public ZLPlayer KindledBy => _kindledBy;

        /// <summary>Something heroes shoot at (not while kindled or under the street).</summary>
        public bool Hostile => !Kindled && !Burrowed;
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
            _sr.color = _tint = tint;
            if (g.material != null) _sr.sharedMaterial = g.material;
            _anim.idle = idle; _anim.move = move; _anim.hurt = hurt;
            _anim.fps = k == ZLFoe.Hound ? 12f : 6f;
            _wander = Random.insideUnitCircle.normalized;
            _think = Random.value;
            _lastPos = transform.position;
        }

        public void Hit(int damage, Vector2 dir, ZLGun gun)
        {
            if (_dead || Puppet || Burrowed) return;
            var by = ZLBullet.LastOwner != null ? ZLBullet.LastOwner : _game.MeleeAttacker;
            if (by != null) LastHitBy = by;
            if (gun != null && gun.kindle > 0f && !Kindled && ZLCatalog.Kindleable(kind))
            {
                Kindle(gun.kindle, by);
                return;
            }
            if (kind == ZLFoe.Riot && ZLBullet.Striking && !_game.InstaKills(kind))
            {
                int through = ZLRules.RiotDamage(damage, _facing.x, _facing.y, dir.x, dir.y);
                if (through < damage)
                {
                    // CLANK: the shield takes it
                    ZLFx.Spawn(GlowHalo.Sprite, (Vector2)transform.position + _facing * 0.45f + Vector2.up * 0.3f, new Color(0.8f, 0.9f, 1f, 0.9f), 0.15f, 0.3f, 1.2f, _game.material, ZLSort.Canopy - 2, true);
                    if (by != null && by == _game.Player && Random.value < 0.35f) ZLFloater.Show("SHIELD", transform.position + Vector3.up * 0.9f, new Color(0.7f, 0.85f, 1f), 0.6f);
                    damage = through;
                }
            }
            if (Frozen) damage = ZLRules.FrozenDamage(damage);
            if (gun != null && gun.freeze > 0f) _game.FreezeAround(transform.position, gun.freeze);
            if (_game.InstaKills(kind)) damage = Mathf.Max(damage, hp);
            hp -= damage;
            _hurt = 0.12f;
            // Hit markers: a soft tick for your hits, a heavier one for the killing blow.
            if (by != null && by == _game.Player) Sfx.Play(hp <= 0 ? Sfx.HitHeavy : Sfx.Hit, hp <= 0 ? 0.4f : 0.22f);
            if (_game.IsLastStand && _game.Stand != null) _game.Stand.ZombieHit(this, hp <= 0);
            _body.linearVelocity += dir * (kind == ZLFoe.Brute || kind == ZLFoe.Herald ? 0.8f : 3f);
            if (!hunting) StartHunt();
            ZLFx.HitSpray(_game.Sprite("Props/blood"), (Vector2)transform.position + Vector2.up * 0.3f, dir, _game.material, hp <= 0 ? 5 : 3);
            if (hp <= 0) Die();
        }

        /// <summary>The Kindler: this zombie hunts the other dead for a while (points for its kills go to the shooter).</summary>
        public void Kindle(float seconds, ZLPlayer by)
        {
            if (_dead || Kindled) return;
            _kindled = seconds;
            _kindledBy = by != null ? by : _game.Player;
            LastHitBy = _kindledBy;
            hunting = true;
            _leapT = _leapWind = 0f;
            _prey = null;
            ZLFloater.Show("KINDLED", transform.position + Vector3.up * 0.8f, new Color(1f, 0.75f, 0.35f), 0.9f);
            Sfx.Play(Sfx.Kindle, 0.8f);
            ZLFx.Spawn(GlowHalo.Sprite, transform.position, new Color(1f, 0.65f, 0.25f, 0.85f), 0.5f, 1f, 4f, _game.material);
        }

        /// <summary>The Rime Cannon: frozen for this long (crawls, takes extra damage; a leap in progress stops).</summary>
        public void Freeze(float seconds)
        {
            if (_dead) return;
            if (_under || _surfaceTell > 0f) Surface(false);   // the frost drives a burrower up
            _frozen = Mathf.Max(_frozen, kind == ZLFoe.Herald || kind == ZLFoe.Brute ? seconds * 0.5f : seconds);
            _leapT = 0f; _leapWind = 0f;
        }

        /// <summary>Riot zombies carry a shield (a child sprite in front of them).</summary>
        public void AddShield(Sprite s)
        {
            var go = new GameObject("Shield");
            go.transform.SetParent(transform, false);
            _shield = go.AddComponent<SpriteRenderer>();
            _shield.sprite = s;
            if (_game.material != null) _shield.sharedMaterial = _game.material;
        }

        /// <summary>Recolours this zombie (bosses); the hurt flash returns to this colour.</summary>
        public void SetTint(Color c)
        {
            _tint = c;
            if (_sr != null) _sr.color = c;
        }

        public void StartHunt()
        {
            if (hunting) return;
            hunting = true;
            if (kind == ZLFoe.Hound)
            {
                _game.SoundCue(transform.position, "HOUND");
                if (Random.value < 0.35f) Sfx.Play(Sfx.Howl, 0.35f, 0.1f);
            }
            if (kind == ZLFoe.Screamer && _screamed <= 0f)
            {
                _screamed = 1f;
                ZLFloater.Show("SCREAM!", transform.position, new Color(0.9f, 0.6f, 1f), 1.3f);
                Sfx.Play(Sfx.Spotted, 0.9f);
                _game.SoundCue(transform.position, "SCREAM");
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
            if (kind == ZLFoe.Splitter && !_game.IsGuest) _game.SplitApart(this);
            if (kind == ZLFoe.Bloater)
            {
                ZLFx.Spawn(GlowHalo.Sprite, transform.position, new Color(0.55f, 0.85f, 0.35f, 0.7f), 1.2f, 2.5f, 2f, _game.material);
                _game.Explode(transform.position, 2.2f, 12, false);
            }
            ZLFx.Decal(_game.Sprite("Props/blood"), transform.position, _game.material);
            if (_sr != null && _sr.sprite != null && kind != ZLFoe.Bloater && kind != ZLFoe.Splitter)
                ZLFx.Corpse(_sr.sprite, transform.position, _sr.flipX, _tint, _sr.transform.localScale.x, _game.material);
            if ((kind == ZLFoe.Brute || kind == ZLFoe.Herald) && CameraFollowTopDown.Main != null) CameraFollowTopDown.Main.Shake(0.15f, 0.25f);
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

        /// <summary>Guest's phone: this zombie only shows where the host's zombie is.</summary>
        public void MakePuppet(int netId)
        {
            Puppet = true;
            NetId = netId;
            hunting = true;
            _netTarget = transform.position;
            if (_body != null) { _body.bodyType = RigidbodyType2D.Kinematic; _body.linearVelocity = Vector2.zero; }
        }

        public void SetNet(Vector2 pos, float health01)
        {
            if ((pos - (Vector2)transform.position).sqrMagnitude > 0.0004f && _sr != null && Mathf.Abs(pos.x - transform.position.x) > 0.01f)
                _sr.flipX = pos.x < transform.position.x;
            _netTarget = pos;
            _interp.Push(Time.unscaledTimeAsDouble, pos.x, pos.y);
            int newHp = Mathf.Max(1, Mathf.RoundToInt(maxHp * health01));
            if (newHp < hp) _hurt = 0.12f;
            hp = newHp;
        }

        /// <summary>Guest: the host says this zombie is gone (blood where it fell).</summary>
        public void PuppetDie()
        {
            if (_dead) return;
            _dead = true;
            ZLFx.Decal(_game.Sprite("Props/blood"), transform.position, _game.material);
            Destroy(gameObject);
        }

        void Update()
        {
            if (!Puppet || _dead) return;
            var next = _interp.Sample(Time.unscaledTimeAsDouble, out float x, out float y) ? new Vector2(x, y) : _netTarget;
            if (_body != null) _body.MovePosition(next); else transform.position = next;
            _hurt -= Time.deltaTime;
        }

        void FixedUpdate()
        {
            if (_dead || _game == null || Puppet) return;
            if (_game.Paused) { _body.linearVelocity = Vector2.zero; return; }
            float dt = Time.fixedDeltaTime;
            _bite -= dt; _spit -= dt; _hurt -= dt; _lungeCool -= dt; _think -= dt;
            Vector2 pos = _body.position;
            if (Kindled) { KindledThink(pos, dt); return; }
            Vector2 target = _game.TargetFor(pos);
            float dist = Vector2.Distance(pos, target);
            if (kind == ZLFoe.Burrower && _frozen <= 0f && BurrowThink(pos, target, dist, dt)) return;

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
            if (_frozen > 0f) { _frozen -= dt; sp *= ZLCatalog.FrozenSpeedScale; }
            _leapCool -= dt;
            // Leapers: crouch (a tell), then spring along a straight line at the hero
            if (kind == ZLFoe.Leaper && _frozen <= 0f)
            {
                if (_leapT > 0f)
                {
                    _leapT -= dt;
                    _body.linearVelocity = _leapDir * ZLRules.LeapSpeed;
                    if (dist < _col.radius + 0.6f && _bite <= 0f) { _bite = 0.9f; _game.Bite(this, ZLRules.Bite(kind)); _leapT = 0f; }
                    if (!_game.City.Walkable(pos + _leapDir * 0.5f)) _leapT = 0f;
                    return;
                }
                if (_leapWind > 0f)
                {
                    _leapWind -= dt;
                    _body.linearVelocity = Vector2.zero;
                    if (_leapWind <= 0f) { _leapT = ZLRules.LeapTime; _leapCool = ZLRules.LeapCooldown; _leapDir = (target - pos).normalized; }
                    return;
                }
                if (hunting && _leapCool <= 0f && dist > ZLRules.LeapMin && dist < ZLRules.LeapMax && _game.City.Sees(pos, target))
                {
                    _leapWind = ZLRules.LeapWindup;
                    _facing = (target - pos).normalized;
                    // The tell is a shape as well as a colour (readable for colour-blind players): a "!" overhead.
                    ZLFloater.Show("!", pos + Vector2.up * 1.1f, new Color(1f, 0.95f, 0.4f), 1.5f, true);
                    _game.SoundCue(pos, "LEAPER");
                    return;
                }
            }
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

            // Unstick: pressing against something for half a second -> step sideways for a while (one frame did nothing).
            if (_sidestep > 0f) { _sidestep -= dt; dir = _sideDir; }
            else
            {
                if (Vector2.Distance(pos, _lastPos) < 0.01f && dir.sqrMagnitude > 0.01f) _stuck += dt; else _stuck = 0f;
                if (_stuck > 0.5f)
                {
                    _sideDir = Vector2.Perpendicular(dir) * (Random.value < 0.5f ? 1f : -1f);
                    if (!_game.City.Walkable(pos + _sideDir * 0.8f)) _sideDir = -_sideDir;
                    _sidestep = 0.6f;
                    _stuck = 0f;
                    dir = _sideDir;
                }
            }
            _lastPos = pos;

            var want = dir * sp;
            _body.linearVelocity = Vector2.MoveTowards(_body.linearVelocity, want, 30f * dt);
            // Which way it faces (the riot shield points this way): at the hero when close, else where it walks.
            Vector2 look = hunting && dist < 7f ? (target - pos).normalized : want.sqrMagnitude > 0.05f ? want.normalized : _facing;
            if (look.sqrMagnitude > 0.5f && Vector2.Dot(_facing, look) < -0.98f) _facing = Vector2.Perpendicular(_facing);   // turn round (no axis when exactly opposite)
            if (look.sqrMagnitude > 0.5f) _facing = ((Vector2)Vector3.RotateTowards(_facing, look, 7f * dt, 0f)).normalized;
        }

        // ---- burrowers ----------------------------------------------------------------------------------

        bool BurrowThink(Vector2 pos, Vector2 target, float dist, float dt)
        {
            _burrowCool -= dt;
            if (_surfaceTell > 0f)
            {
                _surfaceTell -= dt;
                _body.linearVelocity = Vector2.zero;
                if (_surfaceTell <= 0f) Surface(true);
                return true;
            }
            if (_under)
            {
                _underT += dt;
                if (ZLRules.BurrowerSurfaces(dist, _underT))
                {
                    _surfaceTell = ZLRules.BurrowTell;
                    // the tell: a "!" (a shape, not only a colour) and a sound cue
                    ZLFloater.Show("!", pos + Vector2.up * 0.9f, new Color(1f, 0.95f, 0.4f), 1.5f, true);
                    _game.SoundCue(pos, "BURROWER");
                    return true;
                }
                bool sees = dist < 5f && _game.City.Sees(pos, target);
                Vector2 d = sees ? (target - pos).normalized : _game.Flow.Direction(pos);
                if (d == Vector2.zero) d = (target - pos).normalized;
                _body.linearVelocity = d * ZLRules.BurrowSpeed;
                if (Random.value < dt * 6f)
                    ZLFx.Spawn(GlowHalo.Sprite, pos + Random.insideUnitCircle * 0.25f, new Color(0.55f, 0.45f, 0.3f, 0.7f), 0.5f, 0.35f, 0.6f, _game.material, ZLSort.Decal + 2, true);
                return true;
            }
            if (ZLRules.BurrowerDives(hunting, dist, _burrowCool)) { Dive(); return true; }
            return false;
        }

        void Dive()
        {
            _under = true;
            _underT = 0f;
            if (_sr != null) _sr.enabled = false;
            if (_mound == null)
            {
                var go = new GameObject("Mound");
                go.transform.SetParent(transform, false);
                go.transform.localScale = new Vector3(0.9f, 0.5f, 1f);
                _mound = go.AddComponent<SpriteRenderer>();
                _mound.sprite = GlowHalo.Sprite;
                _mound.color = new Color(0.45f, 0.36f, 0.24f, 0.95f);
                if (_game.material != null) _mound.sharedMaterial = _game.material;
            }
            _mound.enabled = true;
            ZLFx.Spawn(GlowHalo.Sprite, transform.position, new Color(0.55f, 0.45f, 0.3f, 0.8f), 0.4f, 0.8f, 2f, _game.material);
            if (!Puppet) Sfx.Play(Sfx.Dig, 0.5f);
        }

        /// <summary>Bursts up out of the street (biting if the hero is right there).</summary>
        void Surface(bool attack)
        {
            _under = false;
            _surfaceTell = 0f;
            _burrowCool = ZLRules.BurrowCooldown;
            if (_sr != null) _sr.enabled = true;
            if (_mound != null) _mound.enabled = false;
            ZLFx.Spawn(GlowHalo.Sprite, transform.position, new Color(0.6f, 0.5f, 0.35f, 0.85f), 0.45f, 1f, 3f, _game.material);
            Sfx.Play(Sfx.Emerge, 0.7f);
            if (CameraFollowTopDown.Main != null && Vector2.Distance(transform.position, _game.TargetPos) < 6f) CameraFollowTopDown.Main.Shake(0.12f, 0.15f);
            if (!attack) return;
            Vector2 pos = transform.position;
            if (Vector2.Distance(pos, _game.TargetFor(pos)) < _col.radius + 0.9f && _bite <= 0f)
            {
                _bite = 1.1f;
                _game.Bite(this, ZLRules.Bite(kind));
            }
        }

        /// <summary>Guest: how the host's zombie looks right now (under the street, kindled).</summary>
        public void SetNetFlags(bool burrowed, bool kindled)
        {
            if (burrowed != _under)
            {
                if (burrowed) Dive(); else { _under = false; if (_sr != null) _sr.enabled = true; if (_mound != null) _mound.enabled = false; }
            }
            _kindled = kindled ? 1f : 0f;
        }

        // ---- kindled ------------------------------------------------------------------------------------

        void KindledThink(Vector2 pos, float dt)
        {
            _kindled -= dt;
            _allyBite -= dt;
            _allySeek -= dt;
            if (_kindled <= 0f)
            {
                // burns out: counts as the shooter's kill
                ZLFx.Spawn(GlowHalo.Sprite, pos, new Color(1f, 0.6f, 0.2f, 0.9f), 0.6f, 1.2f, 5f, _game.material);
                Die();
                return;
            }
            if (_allySeek <= 0f || _prey == null || !_prey.Alive || !_prey.Hostile)
            {
                _allySeek = 0.3f;
                _prey = _game.NearestHostileZombie(pos, 14f, this);
            }
            Vector2 goal;
            if (_prey != null) goal = _prey.transform.position;
            else goal = _kindledBy != null ? (Vector2)_kindledBy.transform.position : _game.TargetPos;
            Vector2 to = goal - pos;
            float d = to.magnitude;
            Vector2 dir = d > 0.01f ? to / d : Vector2.zero;
            if (_prey == null && d < 2.5f) dir = Vector2.zero;   // no prey: stay near the one who kindled it
            if (_prey != null && d > 3f && !_game.City.Sees(pos, goal)) dir = Vector2.zero;   // lost it behind a wall
            _body.linearVelocity = Vector2.MoveTowards(_body.linearVelocity, dir * speed * ZLCatalog.KindledSpeedScale, 30f * dt);
            if (dir.sqrMagnitude > 0.01f) _facing = dir;
            if (_prey != null && d < _col.radius + 0.7f && _allyBite <= 0f)
            {
                _allyBite = ZLCatalog.KindledBiteEvery;
                _game.HitAs(_prey, _game.KindledBiteDamage(_prey.threat), dir, _kindledBy);
            }
        }

        void LateUpdate()
        {
            if (_sr == null) return;
            _animT += Time.deltaTime;
            bool moving = Puppet ? ((Vector2)transform.position - _netTarget).sqrMagnitude > 0.0004f : _body != null && _body.linearVelocity.sqrMagnitude > 0.05f;
            var s = _anim.Pick(moving, _hurt > 0f, _animT);
            if (s != null) _sr.sprite = s;
            if (_body != null && Mathf.Abs(_body.linearVelocity.x) > 0.05f) _sr.flipX = _body.linearVelocity.x < 0f;
            _sr.sortingOrder = ZLSort.ByY(transform.position.y);
            // Hurt flash: a quick white pop that always fades back to the zombie's own colour.
            var baseCol = _frozen > 0f ? Color.Lerp(_tint, new Color(0.55f, 0.85f, 1f), 0.7f) : _tint;
            if (Kindled) baseCol = Color.Lerp(_tint, new Color(1f, 0.7f, 0.3f), 0.65f);
            if (_mound != null && _mound.enabled)
            {
                float k = _surfaceTell > 0f ? 1f + 0.25f * Mathf.Sin(_animT * 30f) : 1f;
                _mound.transform.localScale = new Vector3(0.9f * k, 0.5f * k, 1f);
                _mound.sortingOrder = ZLSort.Decal + 2;
            }
            if (_leapWind > 0f) baseCol = Color.Lerp(baseCol, new Color(1f, 0.4f, 0.3f), 0.5f + 0.5f * Mathf.Sin(_animT * 40f));   // the tell
            _sr.color = _hurt > 0f ? Color.Lerp(baseCol, Color.white, 0.65f) : baseCol;
            if (_shield != null)
            {
                _shield.transform.localPosition = new Vector3(_facing.x * 0.38f, 0.3f + _facing.y * 0.25f, 0f);
                _shield.flipX = _facing.x < 0f;
                _shield.sortingOrder = _sr.sortingOrder + (_facing.y < -0.3f ? 1 : _facing.y > 0.3f ? -1 : 1);
            }
            if (_leapWind > 0f && _sr != null) _sr.transform.localScale = new Vector3(_sr.transform.localScale.x, Mathf.Abs(_sr.transform.localScale.x) * 0.8f, 1f);
            else if (_sr != null) _sr.transform.localScale = new Vector3(_sr.transform.localScale.x, Mathf.Abs(_sr.transform.localScale.x) * (_hurt > 0f ? 0.88f : 1f), 1f);
            // a shuffling walk: a small bob and a lean the way it's going (none with Reduced motion)
            if (_sr != null && _artBase.HasValue)
            {
                bool calm = SettingsService.ReducedMotion;
                float bob = moving && !calm ? Mathf.Abs(Mathf.Sin(_animT * (kind == ZLFoe.Hound || kind == ZLFoe.Runner || kind == ZLFoe.SplitterHalf ? 14f : 8f))) * 0.05f : 0f;
                float lean = moving && !calm && _body != null ? Mathf.Clamp(-_body.linearVelocity.x * 2.5f, -8f, 8f) : 0f;
                _sr.transform.localPosition = _artBase.Value + new Vector3(0f, bob, 0f);
                _sr.transform.localRotation = Quaternion.Euler(0f, 0f, lean);
            }
            else if (_sr != null) _artBase = _sr.transform.localPosition;
        }
    }
}

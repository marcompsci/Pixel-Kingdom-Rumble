using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// A drivable car, truck, bicycle or motorcycle. Point-to-drive for touch: push the stick the way you want to go
    /// and the vehicle turns toward it and accelerates; pull back against your heading to brake and reverse. Ramming
    /// the dead hurts them (trucks plow through crowds); they claw at the bodywork, and a wrecked vehicle throws you out.
    /// Engines are loud and draw the dead; bicycles are silent.
    /// </summary>
    public class ZLVehicle : MonoBehaviour, IZLUse
    {
        public ZLVehicleSpec spec;
        public int health;
        public bool wrecked;
        /// <summary>Set by Delivery missions.</summary>
        public string missionTag;

        ZLGame _game;
        Rigidbody2D _body;
        SpriteRenderer _sr;
        BoxCollider2D _box;
        float _speed, _noiseT, _hurt;
        ZLPlayer _driver;
        Sprite _wreck;

        public bool Occupied => _driver != null;
        public float Speed => _speed;
        public Vector2 UsePos => transform.position;
        public float UseRange => spec != null && (spec.kind == ZLVehicleKind.Bike || spec.kind == ZLVehicleKind.Motorcycle) ? 1.4f : 1.9f;
        public bool CanUse => !wrecked && !Occupied;
        public string Prompt => "DRIVE " + (spec != null ? spec.name : "");
        public float Health01 => spec != null ? health / (float)spec.health : 0f;

        public void Init(ZLGame g, ZLVehicleKind kind, Sprite sprite, Sprite wreck, float angle)
        {
            _game = g;
            spec = ZLCatalog.Vehicle(kind);
            health = spec.health;
            _wreck = wreck;
            _body = gameObject.AddComponent<Rigidbody2D>();
            _body.gravityScale = 0f;
            _body.mass = spec.mass;
            _body.linearDamping = 2.5f;
            _body.angularDamping = 8f;
            _body.interpolation = RigidbodyInterpolation2D.Interpolate;
            _body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _sr = gameObject.AddComponent<SpriteRenderer>();
            _sr.sprite = sprite;
            if (g.material != null) _sr.sharedMaterial = g.material;
            _box = gameObject.AddComponent<BoxCollider2D>();
            if (sprite != null) _box.size = sprite.bounds.size * 0.92f;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        public void Use(ZLPlayer p)
        {
            if (!CanUse) return;
            _driver = p;
            p.EnterVehicle(this);
            _game.VehicleEntered(this);
        }

        /// <summary>Called by the driver every frame with the stick.</summary>
        public void Drive(Vector2 stick)
        {
            if (wrecked || _game.Paused) return;
            float dt = Time.deltaTime;
            Vector2 fwd = transform.right;
            _speed = Vector2.Dot(_body.linearVelocity, fwd);
            float mag = Mathf.Clamp01(stick.magnitude);
            if (mag > 0.2f)
            {
                float want = Mathf.Atan2(stick.y, stick.x) * Mathf.Rad2Deg;
                float cur = transform.eulerAngles.z;
                float diff = Mathf.DeltaAngle(cur, want);
                bool reverse = Mathf.Abs(diff) > 120f && _speed < 2f;
                if (reverse)
                {
                    _speed = Mathf.MoveTowards(_speed, -spec.topSpeed * 0.4f * mag, spec.accel * dt);
                    float back = Mathf.DeltaAngle(cur, want + 180f);
                    Turn(-back, dt);
                }
                else if (Mathf.Abs(diff) > 120f)
                {
                    _speed = Mathf.MoveTowards(_speed, 0f, spec.accel * 2f * dt);   // brake
                }
                else
                {
                    Turn(diff, dt);
                    float target = spec.topSpeed * mag * (1f - Mathf.Abs(diff) / 260f);
                    _speed = Mathf.MoveTowards(_speed, target, (target > _speed ? spec.accel : spec.accel * 1.5f) * dt);
                }
            }
            else _speed = Mathf.MoveTowards(_speed, 0f, spec.accel * 0.8f * dt);

            // grip: kill sideways slide
            Vector2 side = Vector2.Perpendicular(fwd);
            float lateral = Vector2.Dot(_body.linearVelocity, side);
            lateral = Mathf.MoveTowards(lateral, 0f, spec.grip * 4f * dt);
            _body.linearVelocity = fwd * _speed + side * lateral;

            _noiseT -= dt;
            if (_noiseT <= 0f && spec.noise > 0f && Mathf.Abs(_speed) > 1f)
            {
                _noiseT = 1f;
                _game.MakeNoise(transform.position, spec.noise);
            }
        }

        void Turn(float diff, float dt)
        {
            float speed01 = Mathf.Clamp01(Mathf.Abs(_speed) / 3f + 0.25f);
            float step = Mathf.Clamp(diff, -spec.turn * dt * speed01, spec.turn * dt * speed01);
            _body.MoveRotation(_body.rotation + step);
        }

        void FixedUpdate()
        {
            if (_body == null) return;
            if (_driver == null || wrecked) _body.linearVelocity = Vector2.MoveTowards(_body.linearVelocity, Vector2.zero, 12f * Time.fixedDeltaTime);
        }

        void Update()
        {
            _hurt -= Time.deltaTime;
            if (_sr != null)
            {
                _sr.sortingOrder = ZLSort.ByY(transform.position.y);
                _sr.color = wrecked ? new Color(0.5f, 0.45f, 0.45f) : _hurt > 0f ? new Color(1f, 0.6f, 0.6f) : Color.white;
            }
        }

        void OnCollisionEnter2D(Collision2D c)
        {
            if (wrecked) return;
            float rel = c.relativeVelocity.magnitude;
            var z = c.collider.GetComponentInParent<ZLZombie>();
            if (z != null && z.Alive)
            {
                if (rel > 2f)
                {
                    int dmg = Mathf.RoundToInt(rel * spec.ram * 2f);
                    z.Hit(dmg, (c.transform.position - transform.position).normalized, null);
                    if (z.Body != null) z.Body.linearVelocity += (Vector2)(c.transform.position - transform.position).normalized * rel * 0.6f;
                    Damage(spec.kind == ZLVehicleKind.Truck ? 1 : 3);
                    Sfx.Play(Sfx.Hit, 0.7f);
                }
                return;
            }
            var boss = c.collider.GetComponentInParent<ZLBoss>();
            if (boss != null) { Damage(25); return; }
            if (rel > 7f && _driver != null)
            {
                Damage(Mathf.RoundToInt((rel - 6f) * 3f));
                if (Services.Haptics != null) Services.Haptics.Play(HapticStrength.Medium);
                if (CameraFollowTopDown.Main != null) CameraFollowTopDown.Main.Shake(0.15f, 0.15f);
            }
        }

        public void Damage(int dmg)
        {
            if (wrecked) return;
            health -= dmg;
            _hurt = 0.1f;
            if (health <= 0) Wreck();
        }

        void Wreck()
        {
            wrecked = true;
            health = 0;
            if (_wreck != null) _sr.sprite = _wreck;
            ZLFx.Spawn(GlowHalo.Sprite, transform.position, new Color(1f, 0.55f, 0.2f, 0.9f), 0.8f, 2f, 5f, _game.material);
            Sfx.Play(Sfx.Burst, 1f);
            _game.Explode(transform.position, 3f, 30, true);
            if (_driver != null)
            {
                var d = _driver;
                Eject(true);
                d.Hurt(15, transform.position);
            }
            _game.VehicleWrecked(this);
        }

        /// <summary>The driver gets out (next to the vehicle, on the first free side).</summary>
        public void Eject(bool placeDriver)
        {
            if (_driver == null) return;
            var d = _driver;
            _driver = null;
            _speed = 0f;
            if (placeDriver)
            {
                Vector2 side = Vector2.Perpendicular(transform.right);
                Vector2 at = transform.position;
                foreach (var cand in new[] { side * 1.3f, -side * 1.3f, (Vector2)transform.right * -1.8f, (Vector2)transform.right * 1.8f })
                {
                    var p = (Vector2)transform.position + cand;
                    if (_game.City.Walkable(p)) { at = p; break; }
                }
                d.ExitVehicle(at);
            }
            _game.VehicleExited(this);
        }
    }
}

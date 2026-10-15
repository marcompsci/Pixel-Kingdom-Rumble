using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>A rot nest: feeds the district with fresh dead while the hero is near. Destroy it to thin the district.</summary>
    public class ZLNest : MonoBehaviour, IZLHittable
    {
        public string id, district, missionId;
        public int hp, maxHp;
        ZLGame _game;
        SpriteRenderer _sr;
        Sprite[] _frames;
        float _spawn, _t, _hurt;
        int _alive;
        bool _dead;

        public bool Hostile => true;
        public bool Alive => !_dead;

        public void Init(ZLGame g, string nestId, string districtId, int health, Sprite[] frames, string mission = null)
        {
            _game = g; id = nestId; district = districtId; maxHp = hp = health; _frames = frames; missionId = mission;
            _sr = gameObject.AddComponent<SpriteRenderer>();
            _sr.sprite = frames[0];
            _sr.sortingOrder = ZLSort.ByY(transform.position.y);
            if (g.material != null) _sr.sharedMaterial = g.material;
            var c = gameObject.AddComponent<CircleCollider2D>();
            c.radius = 1.1f;
            _spawn = 2f;
        }

        public void Hit(int damage, Vector2 dir, ZLGun gun)
        {
            if (_dead) return;
            hp -= damage;
            _hurt = 0.1f;
            if (hp <= 0)
            {
                _dead = true;
                _game.NestDestroyed(this);
                ZLFx.Spawn(GlowHalo.Sprite, transform.position, new Color(0.8f, 0.4f, 0.9f, 0.9f), 1.2f, 3f, 3f, _game.material);
                Sfx.Play(Sfx.Burst, 0.9f);
                Destroy(gameObject);
            }
        }

        void Update()
        {
            if (_dead || _game == null || _game.Paused) return;
            _t += Time.deltaTime;
            _hurt -= Time.deltaTime;
            _sr.sprite = _frames[(int)(_t * 2f) % _frames.Length];
            _sr.color = _hurt > 0f ? new Color(1f, 0.6f, 0.6f) : Color.white;
            float d = Vector2.Distance(transform.position, _game.TargetPos);
            if (d > 20f) return;
            _spawn -= Time.deltaTime;
            if (_spawn <= 0f)
            {
                _spawn = missionId != null ? 3.5f : 6f;
                if (_game.NestSpawnCount(this) < (missionId != null ? 8 : 5))
                    _game.SpawnFromNest(this);
            }
        }
    }
}

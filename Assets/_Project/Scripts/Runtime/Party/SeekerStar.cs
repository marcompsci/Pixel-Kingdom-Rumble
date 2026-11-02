using UnityEngine;

namespace PKR
{
    /// <summary>Seeker Star: flies at the nearest rival for a few seconds and knocks them up and back.</summary>
    public class SeekerStar : MonoBehaviour
    {
        IPartyHost _host;
        GameObject _owner, _target;
        Vector2 _vel;
        float _life = 3.5f;
        const float Speed = 11f;

        public static void Launch(IPartyHost host, GameObject owner)
        {
            GameObject best = null;
            float bestD = float.MaxValue;
            foreach (var r in host.Rivals(owner))
            {
                float d = ((Vector2)r.transform.position - (Vector2)owner.transform.position).sqrMagnitude;
                if (d < bestD) { bestD = d; best = r; }
            }
            if (best == null) return;
            var go = new GameObject("SeekerStar");
            go.transform.position = owner.transform.position + new Vector3(0f, 0.8f, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SeasonArt.Star();
            sr.sortingOrder = 35;
            if (host.SpriteMaterial != null) sr.sharedMaterial = host.SpriteMaterial;
            go.transform.localScale = Vector3.one * 1.6f;
            GlowHalo.Attach(go.transform, new Color(1f, 0.9f, 0.4f, 0.6f), 1.2f, 35, host.SpriteMaterial);
            var s = go.AddComponent<SeekerStar>();
            s._host = host; s._owner = owner; s._target = best;
            s._vel = new Vector2(owner.TryGetComponent(out PlatformerMotor2D m) ? m.Facing * Speed : Speed, 4f);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            _life -= dt;
            if (_life <= 0f || _target == null || !_target.activeInHierarchy) { Destroy(gameObject); return; }
            Vector2 pos = transform.position;
            Vector2 to = (Vector2)_target.transform.position + new Vector2(0f, 0.3f) - pos;
            _vel = Vector2.Lerp(_vel, to.normalized * Speed, 6f * dt);
            transform.position = pos + _vel * dt;
            transform.Rotate(0f, 0f, 540f * dt);
            if (to.sqrMagnitude < 0.5f)
            {
                float dir = Mathf.Sign(_vel.x == 0f ? 1f : _vel.x);
                if (ItemBuffs.Knock(_target, new Vector2(dir * 5f, 9f), 0.9f)) _host.OnItemHit(_target, _owner);
                PuffEffect.Play(transform.position, new Color32(255, 230, 90, 255), _host.SpriteMaterial, 0.3f, 6f, 0.5f);
                Destroy(gameObject);
            }
        }
    }
}

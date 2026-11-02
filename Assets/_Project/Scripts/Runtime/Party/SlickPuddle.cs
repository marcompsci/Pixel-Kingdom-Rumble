using UnityEngine;

namespace PKR
{
    /// <summary>Slick Puddle: left on the ground behind you; the first rival to run through spins out.</summary>
    public class SlickPuddle : MonoBehaviour
    {
        IPartyHost _host;
        GameObject _owner;
        float _age;
        const float Life = 10f, OwnerGrace = 1.2f;

        public static void Drop(IPartyHost host, GameObject owner)
        {
            int facing = owner.TryGetComponent(out PlatformerMotor2D m) ? m.Facing : 1;
            Vector2 at = (Vector2)owner.transform.position + new Vector2(-facing * 1.2f, 0f);
            var hit = Physics2D.Raycast(at + Vector2.up * 0.5f, Vector2.down, 6f, PKRLayers.GroundMask);
            if (hit.collider != null) at = hit.point;
            var go = new GameObject("SlickPuddle");
            go.transform.position = at;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PlazaArt.Puddle();
            sr.sortingOrder = 4;
            if (host.SpriteMaterial != null) sr.sharedMaterial = host.SpriteMaterial;
            var p = go.AddComponent<SlickPuddle>();
            p._host = host; p._owner = owner;
        }

        void Update()
        {
            _age += Time.deltaTime;
            if (_age > Life) { Destroy(gameObject); return; }
            Vector2 c = (Vector2)transform.position + new Vector2(0f, 0.5f);
            foreach (var col in Physics2D.OverlapBoxAll(c, new Vector2(1.1f, 1f), 0f))
            {
                var rb = col.attachedRigidbody;
                if (rb == null) continue;
                var who = rb.gameObject;
                if (!_host.IsRacer(who) || (who == _owner && _age < OwnerGrace)) continue;
                float dir = who.TryGetComponent(out PlatformerMotor2D m) ? -m.Facing : -1f;
                if (ItemBuffs.Knock(who, new Vector2(dir * 3f, 7f), 0.8f)) _host.OnItemHit(who, _owner);
                Destroy(gameObject);
                return;
            }
        }
    }
}

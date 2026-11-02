using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// An item box: touch it for a random item (better ones the further behind you are), used at once. Comes back
    /// a few seconds later.
    /// </summary>
    public class ItemBox : MonoBehaviour
    {
        IPartyHost _host;
        SpriteRenderer _sr;
        float _hiddenUntil, _age;
        System.Random _rng;
        const float Respawn = 5f;

        public bool Ready => Time.time >= _hiddenUntil;

        public static ItemBox Spawn(IPartyHost host, Vector2 at, int seed)
        {
            var go = new GameObject("ItemBox");
            go.transform.position = at;
            var art = new GameObject("Art");
            art.transform.SetParent(go.transform, false);
            var sr = art.AddComponent<SpriteRenderer>();
            sr.sprite = PlazaArt.ItemBox();
            sr.sortingOrder = 14;
            if (host.SpriteMaterial != null) sr.sharedMaterial = host.SpriteMaterial;
            GlowHalo.Attach(art.transform, new Color(0.6f, 0.9f, 1f, 0.45f), 1.4f, 14, host.SpriteMaterial);
            var box = go.AddComponent<ItemBox>();
            box._host = host; box._sr = sr; box._rng = new System.Random(seed); box._age = seed % 7;
            return box;
        }

        void Update()
        {
            _age += Time.deltaTime;
            bool ready = Ready;
            if (_sr != null)
            {
                _sr.enabled = ready;
                var glow = _sr.GetComponentInChildren<GlowHalo>(true);
                if (glow != null) glow.gameObject.SetActive(ready);
                float bob = SettingsService.ReducedMotion ? 0f : Mathf.Sin(_age * 2.4f) * 0.1f;
                _sr.transform.localPosition = new Vector3(0f, bob, 0f);
                _sr.transform.localRotation = Quaternion.Euler(0f, 0f, SettingsService.ReducedMotion ? 0f : Mathf.Sin(_age * 1.3f) * 8f);
            }
            if (!ready) return;
            foreach (var col in Physics2D.OverlapCircleAll(transform.position, 0.6f))
            {
                var rb = col.attachedRigidbody;
                if (rb == null || !_host.IsRacer(rb.gameObject)) continue;
                Take(rb.gameObject);
                return;
            }
        }

        void Take(GameObject racer)
        {
            _hiddenUntil = Time.time + Respawn;
            var item = PartyItems.Roll(_rng, _host.PlaceOf(racer), _host.RacerCount);
            PartyItemFx.Use(_host, racer, item);
            PuffEffect.Play(transform.position, new Color32(150, 220, 255, 255), _host.SpriteMaterial, 0.25f, 5f, 0.4f);
        }
    }
}

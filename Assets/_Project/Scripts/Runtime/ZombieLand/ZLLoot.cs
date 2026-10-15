using System.Collections.Generic;
using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>Ammo, medkits, armor and guns lying around the city. Guns are taken for good; the rest come back.</summary>
    public class ZLLoot : ZLMarkerBase
    {
        public string id, kind;
        public bool temporary;
        float _respawn, _life = 30f;
        public bool Available => _respawn <= 0f;

        public void Init(ZLGame g, string lootId, string lootKind, Sprite s)
        {
            id = lootId; kind = lootKind;
            var gun = ZLCatalog.Gun(lootKind);
            Setup(g, s, gun != null ? gun.name : null, new Color(1f, 0.9f, 0.6f), 1f);
        }

        public void Take()
        {
            if (ZLCatalog.Gun(kind) != null) { Destroy(gameObject); return; }
            _respawn = 240f;
            sr.enabled = false;
        }

        public void Tick(float dt)
        {
            if (temporary) { _life -= dt; if (_life <= 0f) { Destroy(gameObject); return; } }
            if (_respawn > 0f)
            {
                _respawn -= dt;
                if (_respawn <= 0f) sr.enabled = true;
            }
        }

        protected override void Update()
        {
            base.Update();
            if (sr != null && sr.enabled) sr.transform.localPosition = new Vector3(0f, Mathf.Sin(t * 3f) * 0.05f, 0f);
        }
    }
}

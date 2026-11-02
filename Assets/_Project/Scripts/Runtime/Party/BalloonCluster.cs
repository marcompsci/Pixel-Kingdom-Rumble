using UnityEngine;

namespace PKR
{
    /// <summary>Balloon Battle: a fighter's remaining balloons (their stocks) bob above their head in their colour.</summary>
    public class BalloonCluster : MonoBehaviour
    {
        ArenaMatchController _arena;
        int _slot;
        readonly SpriteRenderer[] _balloons = new SpriteRenderer[3];
        float _t;

        public static void Attach(GameObject fighter, int slot, Color color, ArenaMatchController arena, Material mat)
        {
            var old = fighter.GetComponentInChildren<BalloonCluster>(true);
            if (old != null) Destroy(old.gameObject);
            var go = new GameObject("Balloons");
            go.transform.SetParent(fighter.transform, false);
            go.transform.localPosition = new Vector3(0f, 1.15f, 0f);
            var c = go.AddComponent<BalloonCluster>();
            c._arena = arena; c._slot = slot; c._t = slot;
            for (int i = 0; i < 3; i++)
            {
                var sr = new GameObject("Balloon" + i).AddComponent<SpriteRenderer>();
                sr.transform.SetParent(go.transform, false);
                sr.transform.localPosition = new Vector3((i - 1) * 0.36f, i == 1 ? 0.12f : 0f, 0f);
                sr.sprite = PlazaArt.Balloon();
                sr.color = color;
                sr.sortingOrder = 12;
                if (mat != null) sr.sharedMaterial = mat;
                c._balloons[i] = sr;
            }
        }

        void LateUpdate()
        {
            // stays upright even when the fighter's body flips
            transform.rotation = Quaternion.identity;
            var s = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
            transform.localScale = new Vector3(s.x < 0f ? -1f : 1f, 1f, 1f);
            if (_arena == null || _arena.Match == null || _slot >= _arena.Match.Fighters.Count) return;
            int left = _arena.Match.Get(_slot).stocks;
            _t += Time.deltaTime;
            for (int i = 0; i < _balloons.Length; i++)
            {
                _balloons[i].enabled = i < left;
                float bob = SettingsService.ReducedMotion ? 0f : Mathf.Sin(_t * 2f + i) * 0.05f;
                var p = _balloons[i].transform.localPosition;
                _balloons[i].transform.localPosition = new Vector3(p.x, (i == 1 ? 0.12f : 0f) + bob, 0f);
            }
        }
    }
}

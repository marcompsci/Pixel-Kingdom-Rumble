using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// A solid crate with a Star Shard inside: bump it from below (head-first) to pop the shard into your
    /// pocket. Each crate counts toward the level's shard total.
    /// </summary>
    public class ShardCrate : MonoBehaviour
    {
        public SpriteRenderer art;
        public Sprite emptySprite;
        public Sprite shardSprite;
        public bool Used { get; private set; }

        void FixedUpdate()
        {
            if (Used || !PropUtil.TryPlayer(out var motor, out var b)) return;
            Vector2 c = transform.position;
            float bottom = c.y - 0.5f;
            bool underneath = Mathf.Abs(b.center.x - c.x) <= 0.7f && b.max.y >= bottom - 0.12f && b.center.y < bottom;
            if (underneath) Pop();
        }

        public void Pop()
        {
            if (Used) return;
            Used = true;
            if (art != null && emptySprite != null) art.sprite = emptySprite;
            var flow = LevelFlowController.Current;
            if (flow != null) flow.OnPickupCollected(PickupKind.StarShard, 1);
            Vector2 top = (Vector2)transform.position + Vector2.up * 0.9f;
            PuffEffect.Play(top, new Color32(255, 220, 90, 255), art != null ? art.sharedMaterial : null, 0.18f, 3f, 0.4f);
            if (Services.Haptics != null) Services.Haptics.Play(HapticStrength.Light);
            if (shardSprite != null) StartCoroutine(RiseShard(top));
        }

        System.Collections.IEnumerator RiseShard(Vector2 from)
        {
            var go = new GameObject("CrateShard");
            go.transform.position = from;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = shardSprite;
            if (art != null) sr.sharedMaterial = art.sharedMaterial;
            sr.sortingOrder = 16;
            for (float t = 0f; t < 0.45f; t += Time.deltaTime)
            {
                go.transform.position = from + Vector2.up * (t * 3f);
                var col = sr.color; col.a = 1f - t / 0.45f; sr.color = col;
                yield return null;
            }
            Destroy(go);
        }
    }
}

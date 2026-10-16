using System.Collections.Generic;
using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>Shared look for world markers: a pulsing sprite and an optional floating label.</summary>
    public abstract class ZLMarkerBase : MonoBehaviour
    {
        protected ZLGame game;
        protected SpriteRenderer sr;
        protected TextMesh label;
        protected float t;

        protected void Setup(ZLGame g, Sprite s, string text, Color labelColor, float scale = 1.2f)
        {
            game = g;
            sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            sr.sortingOrder = ZLSort.ByY(transform.position.y);
            if (g.material != null) sr.sharedMaterial = g.material;
            transform.localScale = Vector3.one * scale;
            if (text != null)
            {
                var go = new GameObject("Label");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(0f, 0.9f, 0f);
                label = go.AddComponent<TextMesh>();
                label.font = UISprites.DisplayFont;
                label.fontSize = 40;
                label.characterSize = 0.07f / scale;
                label.anchor = TextAnchor.MiddleCenter;
                label.color = labelColor;
                label.text = text;
                var mr = go.GetComponent<MeshRenderer>();
                mr.sharedMaterial = UISprites.DisplayFont.material;
                mr.sortingOrder = ZLSort.Overhead;
            }
        }

        protected virtual void Update()
        {
            t += Time.deltaTime;
        }
    }
}

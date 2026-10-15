using System.Collections.Generic;
using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>A simple mission prop (cooler, folder, generator, survivor, destination flag).</summary>
    public class ZLMissionProp : ZLMarkerBase
    {
        public float progress;
        public bool done;
        public void Init(ZLGame g, Sprite s, string text, Color c, float scale) => Setup(g, s, text, c, scale);
        public void SetSprite(Sprite s) { if (sr != null) sr.sprite = s; }
        public void SetColor(Color c, int order) { if (sr != null) { sr.color = c; sr.sortingOrder = order; } _fixedOrder = true; }
        bool _fixedOrder;
        public void SetLabel(string text) { if (label != null) label.text = text; }
        public void Show(bool on) { if (sr != null) sr.enabled = on; if (label != null) label.gameObject.SetActive(on); }
        protected override void Update()
        {
            base.Update();
            if (sr != null && !_fixedOrder) sr.sortingOrder = ZLSort.ByY(transform.position.y);
        }
    }
}

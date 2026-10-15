using System.Collections.Generic;
using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>A mission giver / start point. Unknown missions show "?" until you find the lead.</summary>
    public class ZLMissionMarker : ZLMarkerBase, IZLUse
    {
        public ZLMissionSpec Spec { get; private set; }
        public ZLCityData.Mission Site { get; private set; }
        public bool Done => game.Progress.Done(Spec.id);

        public Vector2 UsePos => transform.position;
        public float UseRange => 2.2f;
        public bool CanUse => !Done && game.Active == null;
        public string Prompt => "START " + Spec.name;

        public void Init(ZLGame g, ZLMissionSpec spec, ZLCityData.Mission site)
        {
            Spec = spec; Site = site;
            Setup(g, g.Sprite("Props/pk_marker"), spec.name, spec.required ? new Color(1f, 0.85f, 0.4f) : new Color(0.6f, 0.9f, 1f), 1.8f);
            Refresh();
        }

        public void Refresh()
        {
            bool known = game.Progress.leads.Contains(Spec.id);
            if (label != null) label.text = Done ? "" : known ? Spec.name : "?";
            if (sr != null) sr.enabled = !Done;
        }

        public void Use(ZLPlayer p) => game.StartMission(this);

        protected override void Update()
        {
            base.Update();
            if (Done) return;
            float k = 1f + Mathf.Sin(t * 4f) * 0.08f;
            sr.transform.localScale = Vector3.one * k;
            if (label != null && Time.frameCount % 30 == 0) Refresh();
            if (game.Active != null && game.Active.Spec == Spec) sr.color = new Color(1f, 1f, 1f, 0.35f);
            else sr.color = Color.white;
        }
    }
}

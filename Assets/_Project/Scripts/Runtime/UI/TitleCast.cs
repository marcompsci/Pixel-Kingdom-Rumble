using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PKR
{
    /// <summary>
    /// The title screen's cast: a row of heroes from the roster playing their idle loop, each now and then
    /// showing off an attack or special, over a soft glow. Uses the generated hero frames (FighterFrames).
    /// </summary>
    public class TitleCast : MonoBehaviour
    {
        class Actor
        {
            public Image image;
            public FighterFrames frames;
            public float clock, nextMove, moveT = -1f;
            public Sprite[] move;
        }

        readonly List<Actor> _actors = new List<Actor>();
        const float Fps = 7f;

        /// <summary>Fills parent (a RectTransform row) with up to count heroes that have frames, spaced evenly.</summary>
        public static TitleCast Build(RectTransform parent, CharacterRoster roster, int count)
        {
            var cast = parent.gameObject.AddComponent<TitleCast>();
            if (roster == null) return cast;
            var pool = new List<CharacterDefinition>();
            foreach (var h in roster.heroes)
                if (h != null && h.frames != null && h.frames.HasAny && roster.IsSelectable(h)) pool.Add(h);
            // A different line-up each visit.
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }
            int n = Mathf.Min(count, pool.Count);
            for (int i = 0; i < n; i++)
            {
                var rt = UIFactory.Rect("Hero_" + pool[i].id, parent);
                float x = (i + 0.5f) / n;
                rt.anchorMin = rt.anchorMax = new Vector2(x, 0f);
                rt.pivot = new Vector2(0.5f, 0f);
                // Frames are 80x48 with the hero in the middle: draw them wide so the heroes stand shoulder to shoulder.
                rt.sizeDelta = new Vector2(300f, 180f);
                rt.anchoredPosition = new Vector2(0f, 6f);
                var glow = UIFactory.Rect("Glow", rt);
                glow.anchorMin = glow.anchorMax = new Vector2(0.5f, 0f);
                glow.sizeDelta = new Vector2(150f, 40f);
                glow.anchoredPosition = new Vector2(0f, 6f);
                var gimg = glow.gameObject.AddComponent<Image>();
                gimg.sprite = GlowHalo.Sprite;
                gimg.color = new Color(0f, 0f, 0f, 0.45f);
                gimg.raycastTarget = false;
                var img = rt.gameObject.AddComponent<Image>();
                img.raycastTarget = false;
                img.preserveAspect = true;
                if (i >= n / 2) rt.localScale = new Vector3(-1f, 1f, 1f); // the right half faces the left: a face-off
                var a = new Actor { image = img, frames = pool[i].frames, clock = Random.value * 3f, nextMove = 1.5f + Random.value * 4f };
                img.sprite = FighterFrames.Pick(a.frames.idle, 0);
                cast._actors.Add(a);
            }
            return cast;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            foreach (var a in _actors)
            {
                a.clock += dt;
                if (a.moveT >= 0f)
                {
                    a.moveT += dt;
                    int f = Mathf.FloorToInt(a.moveT * 8f);
                    if (a.move != null && f < a.move.Length + 2) { a.image.sprite = a.move[Mathf.Min(f, a.move.Length - 1)]; continue; }
                    a.moveT = -1f;
                    a.nextMove = a.clock + 3f + Random.value * 5f;
                }
                if (a.clock >= a.nextMove)
                {
                    bool special = Random.value < 0.4f && a.frames.special != null && a.frames.special.Length > 0;
                    a.move = special ? a.frames.special : a.frames.attack;
                    if (a.move != null && a.move.Length > 0) { a.moveT = 0f; continue; }
                    a.nextMove = a.clock + 4f;
                }
                var s = FighterFrames.Pick(a.frames.idle, Mathf.FloorToInt(a.clock * Fps));
                if (s != null) a.image.sprite = s;
            }
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using PKR.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PKR
{
    /// <summary>
    /// Story/Contract game feel, added to every level by LevelFlowController (no scene changes needed):
    /// jump, landing and running dust; a slow-motion beat and flash when the hero goes down; the level title card;
    /// and ambient life for hazards (lava bubbles, current bubbles, geyser embers), culled to what the camera sees.
    /// </summary>
    public class JuiceDirector : MonoBehaviour
    {
        static readonly Color Dust = new Color(0.92f, 0.86f, 0.74f, 0.9f);
        static readonly Color Ember = new Color(1f, 0.55f, 0.15f, 1f);
        static readonly Color Bubble = new Color(0.7f, 1f, 0.95f, 0.8f);

        Material _mat;
        PlatformerMotor2D _motor;
        Damageable _health;
        float _runDust, _ambient;
        readonly List<Transform> _lava = new List<Transform>();
        readonly List<Vector2> _lavaSize = new List<Vector2>();
        readonly List<ForceZone> _currents = new List<ForceZone>();
        readonly List<FireGeyser> _geysers = new List<FireGeyser>();
        Image _flash;
        RectTransform _card;

        void Awake() => BuildOverlay();

        void Start()
        {
            var marker = PlayerMarker.Current;
            if (marker != null)
            {
                _motor = marker.GetComponent<PlatformerMotor2D>();
                _health = marker.GetComponent<Damageable>();
                var sr = marker.GetComponentInChildren<SpriteRenderer>();
                if (sr != null) _mat = sr.sharedMaterial;
            }
            if (_motor != null)
            {
                _motor.Landed += OnLanded;
                _motor.Jumped += OnJumped;
            }
            if (_health != null) _health.Died += OnDied;

            foreach (var cd in FindObjectsByType<ContactDamage>())
                if (cd != null && cd.name == "Lava") { _lava.Add(cd.transform); _lavaSize.Add(cd.size); }
            foreach (var z in FindObjectsByType<ForceZone>()) if (z.kind == ForceKind.Current) _currents.Add(z);
            _geysers.AddRange(FindObjectsByType<FireGeyser>());
        }

        void OnDestroy()
        {
            if (_motor != null) { _motor.Landed -= OnLanded; _motor.Jumped -= OnJumped; }
            if (_health != null) _health.Died -= OnDied;
        }

        // ---- Hero dust ----------------------------------------------------------------------------------

        Vector2 Feet => _motor != null ? (Vector2)_motor.transform.position + Vector2.down * 0.05f : Vector2.zero;

        void OnLanded(float speed)
        {
            if (speed < 5f) return;
            int n = speed > 12f ? 4 : 2;
            for (int i = 0; i < n; i++)
                PuffEffect.Play(Feet + new Vector2((i - (n - 1) * 0.5f) * 0.3f, 0.05f), Dust, _mat, 0.12f + speed * 0.006f, 2.5f, 0.3f);
        }

        void OnJumped() => PuffEffect.Play(Feet, Dust, _mat, 0.12f, 2f, 0.25f);

        void OnDied()
        {
            StartCoroutine(DeathBeat());
        }

        IEnumerator DeathBeat()
        {
            if (CameraFollow2D.Main != null) CameraFollow2D.Main.Shake(0.35f, 0.35f);
            if (_flash != null) _flash.color = new Color(1f, 0.25f, 0.2f, 0.45f);
            bool slowed = false;
            if (Time.timeScale == 1f) { Time.timeScale = 0.3f; slowed = true; }
            float t = 0f;
            while (t < 0.4f)
            {
                t += Time.unscaledDeltaTime;
                if (_flash != null) _flash.color = new Color(1f, 0.25f, 0.2f, Mathf.Lerp(0.45f, 0f, t / 0.4f));
                yield return null;
            }
            if (slowed && Mathf.Approximately(Time.timeScale, 0.3f)) Time.timeScale = 1f;
            if (_flash != null) _flash.color = Color.clear;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            if (_motor != null && _motor.IsGrounded && Mathf.Abs(_motor.Velocity.x) > 5f)
            {
                _runDust -= dt;
                if (_runDust <= 0f)
                {
                    _runDust = 0.16f;
                    PuffEffect.Play(Feet + new Vector2(-Mathf.Sign(_motor.Velocity.x) * 0.25f, 0.05f), Dust, _mat, 0.09f, 1.5f, 0.22f);
                }
            }

            _ambient -= dt;
            if (_ambient > 0f) return;
            _ambient = 0.12f;
            var cam = Camera.main;
            if (cam == null) return;
            Vector2 cc = cam.transform.position;
            float halfW = cam.orthographicSize * cam.aspect + 1f, halfH = cam.orthographicSize + 1f;
            bool Visible(Vector2 p) => Mathf.Abs(p.x - cc.x) < halfW && Mathf.Abs(p.y - cc.y) < halfH;

            for (int i = 0; i < _lava.Count; i++)
            {
                var t = _lava[i];
                if (t == null || Random.value > 0.35f) continue;
                Vector2 p = (Vector2)t.position + new Vector2(Random.Range(-0.5f, 0.5f) * _lavaSize[i].x, 0.2f);
                if (Visible(p)) PuffEffect.Play(p, Ember, _mat, 0.1f, 2f, 0.5f);
            }
            foreach (var z in _currents)
            {
                if (z == null || Random.value > 0.4f) continue;
                Vector2 p = (Vector2)z.transform.position + new Vector2(Random.Range(-0.5f, 0.5f) * z.size.x, Random.Range(-0.5f, 0.5f) * z.size.y);
                if (Visible(p)) PuffEffect.Play(p, Bubble, _mat, 0.07f, 1.2f, 0.6f);
            }
            foreach (var g in _geysers)
            {
                if (g == null || g.Phase == BridgePhase.Solid) continue;
                Vector2 p = (Vector2)g.transform.position + new Vector2(Random.Range(-0.3f, 0.3f), g.Phase == BridgePhase.Open ? Random.Range(0.5f, g.height) : 0.2f);
                if (Visible(p)) PuffEffect.Play(p, Ember, _mat, 0.12f, 3f, 0.4f);
            }
        }

        // ---- Overlay: death flash + level title card -----------------------------------------------------

        void BuildOverlay()
        {
            var safe = UIFactory.CreateCanvas(transform, "JuiceCanvas", 150, landscape: true, out _);
            var flashRt = UIFactory.Rect("Flash", safe);
            UIFactory.Stretch(flashRt);
            flashRt.offsetMin = new Vector2(-400f, -400f);
            flashRt.offsetMax = new Vector2(400f, 400f);
            _flash = flashRt.gameObject.AddComponent<Image>();
            _flash.color = Color.clear;
            _flash.raycastTarget = false;
        }

        /// <summary>Slides in "BIOME / LEVEL NAME / PAR" at the top for a couple of seconds.</summary>
        public void ShowIntro(LevelDefinition level)
        {
            if (level == null || _flash == null || MissionTracker.Current != null) return; // contracts have their own banner
            var safe = (RectTransform)_flash.transform.parent;
            if (_card != null) Destroy(_card.gameObject);
            _card = UIFactory.Rect("TitleCard", safe);
            _card.anchorMin = _card.anchorMax = new Vector2(0.5f, 1f);
            _card.pivot = new Vector2(0.5f, 1f);
            _card.sizeDelta = new Vector2(1100f, 210f);
            var bg = _card.gameObject.AddComponent<Image>();
            bg.sprite = UISprites.RoundedRect; bg.type = Image.Type.Sliced;
            bg.color = new Color(0.08f, 0.06f, 0.12f, 0.88f);
            bg.raycastTarget = false;
            var v = _card.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(30, 30, 18, 18);
            v.childControlWidth = true; v.childControlHeight = true;
            v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            UIFactory.Label(_card, (level.biomeName ?? "").ToUpperInvariant(), 32, TextAnchor.MiddleCenter, 44f).color = UITheme.Current.subtle;
            UIFactory.Label(_card, (level.displayName ?? "").ToUpperInvariant(), 64, TextAnchor.MiddleCenter, 80f, title: true);
            if (level.parTimeSeconds > 0f)
                UIFactory.Label(_card, $"PAR {ResultsMath.FormatTime(level.parTimeSeconds)}", 30, TextAnchor.MiddleCenter, 40f);
            StartCoroutine(Slide(_card));
        }

        IEnumerator Slide(RectTransform card)
        {
            float t = 0f;
            const float inT = 0.35f, hold = 2f, outT = 0.35f;
            while (card != null && t < inT + hold + outT)
            {
                t += Time.unscaledDeltaTime;
                float y = t < inT ? Mathf.Lerp(240f, -40f, Ease(t / inT))
                        : t < inT + hold ? -40f
                        : Mathf.Lerp(-40f, 240f, Ease((t - inT - hold) / outT));
                card.anchoredPosition = new Vector2(0f, y);
                yield return null;
            }
            if (card != null) Destroy(card.gameObject);
        }

        static float Ease(float x) { x = Mathf.Clamp01(x); return 1f - (1f - x) * (1f - x); }
    }
}

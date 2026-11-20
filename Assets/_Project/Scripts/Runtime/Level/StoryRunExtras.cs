using PKR.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PKR
{
    /// <summary>
    /// Story Quest extras on top of the normal level flow (added by LevelFlowController in Story world levels):
    /// - FLAWLESS: clear without losing a heart (one-off Star Shards per level).
    /// - TIME-TRIAL GHOST: in levels you've cleared, a see-through copy of your best clear runs beside you
    ///   (Settings > STORY GHOST); a faster clear saves a new ghost.
    /// - SHARD COMPASS: replaying a cleared level with Star Shards still missing, an arrow at the screen edge points
    ///   to the nearest one you haven't picked up.
    /// </summary>
    public class StoryRunExtras : MonoBehaviour
    {
        public struct Result
        {
            public bool flawless;
            public int flawlessShards;
            public bool newGhost;
            public float ghostDelta;
            public bool brave;
            public int braveShards;
        }

        LevelFlowController _flow;
        Damageable _health;
        int _lastHealth = -1;
        GhostTrack _run, _best;
        Transform _ghost;
        float _ghostSeconds;
        readonly System.Collections.Generic.List<GameObject> _marks = new System.Collections.Generic.List<GameObject>();
        float _recorded;
        bool _compassOn;
        RectTransform _compassRoot, _arrow;
        Text _compassLabel;
        float _nextScan;
        Vector2? _target;

        public bool TookHit { get; private set; }
        readonly System.Collections.Generic.List<float> _splits = new System.Collections.Generic.List<float>();
        public System.Collections.Generic.IReadOnlyList<float> SplitTimes => _splits;

        void OnEnable() => EventBus<CheckpointActivated>.Subscribe(OnCheckpoint);
        void OnDisable() => EventBus<CheckpointActivated>.Unsubscribe(OnCheckpoint);

        /// <summary>CHECKPOINT SPLITS: your time here against your best clear (ahead in green, behind in red).</summary>
        void OnCheckpoint(CheckpointActivated e)
        {
            if (_flow == null || _flow.Run == null || _flow.Run.IsComplete) return;
            float t = _flow.Run.ElapsedSeconds;
            int i = _splits.Count;
            _splits.Add(t);
            var save = Services.Save != null ? Services.Save.Data : null;
            var d = Splits.Delta(save, _flow.Run.LevelId, i, t);
            if (d.HasValue)
                EventBus<GameHint>.Raise(new GameHint { id = "split", text = $"CHECKPOINT {i + 1}   {Splits.Format(d.Value)}" + (d.Value <= 0f ? "  AHEAD!" : "") });
        }
        public bool GhostShown => _ghost != null;
        public bool CompassOn => _compassOn;
        public Vector2? CompassTarget => _target;

        void Start()
        {
            _flow = GetComponent<LevelFlowController>();
            if (_flow == null) { enabled = false; return; }
            _health = _flow.PlayerHealth;
            _run = new GhostTrack();
            var save = Services.Save != null ? Services.Save.Data : null;
            string id = _flow.Run != null ? _flow.Run.LevelId : null;
            var rec = save != null ? save.GetLevel(id) : null;
            bool ghostOn = Services.Settings == null || Services.Settings.Data.storyGhost;
            var saved = ghostOn ? StoryGhosts.Find(save, id) : null;
            if (saved != null && _flow.Player != null)
            {
                _best = GhostTrack.Decode(saved.data);
                _ghostSeconds = saved.seconds;
                if (_best.Count > 1) _ghost = GhostRacer.Spawn(_flow.Player, _flow.SpriteMaterial).transform;
            }
            ShowFallMarks(save, id);
            // On replays the compass points at missed shards and, once every shard is in hand, at missed secrets.
            _compassOn = rec != null && rec.completed &&
                         ((_flow.TotalShards > 0 && rec.mostShardsCollected < _flow.TotalShards) || (_flow.TotalSecrets > 0 && rec.mostSecretsFound < _flow.TotalSecrets));
            if (_compassOn) BuildCompass();
        }

        void Update()
        {
            if (_flow == null || _flow.Run == null) return;
            // FLAWLESS: any lost heart (a hit, a hazard, a fall) breaks it.
            if (_health != null)
            {
                int h = _health.Health;
                if (_lastHealth >= 0 && h < _lastHealth) TookHit = true;
                if (_health.IsDead) TookHit = true;
                _lastHealth = h;
            }
            if (_flow.Run.Deaths > 0) TookHit = true;

            // Ghost: record by level time (not frame time) so playback lines up with the clock.
            float t = _flow.Run.ElapsedSeconds;
            var hero = _flow.Player;
            if (hero != null && !_flow.Run.IsComplete && t > _recorded)
            {
                _run.Record(t - _recorded, hero.transform.position.x, hero.transform.position.y);
                _recorded = t;
            }
            if (_ghost != null && _best != null)
            {
                var g = _best.At(t);
                _ghost.position = new Vector3(g.x, g.y, 0f);
            }

            if (_compassOn) UpdateCompass();
        }

        /// <summary>Called by LevelFlowController when the level is cleared (before the results screen).</summary>
        public Result OnCleared(SaveData data, string levelId, float seconds, bool assistSpeed)
        {
            bool invincible = Services.Settings != null && Services.Settings.Data.storyInvincible;
            var r = new Result { flawless = !TookHit && !invincible };
            if (_ghost != null && _ghostSeconds > 0f && !assistSpeed) r.ghostDelta = _ghostSeconds - seconds;
            r.brave = _flow.BraveRun && !assistSpeed;
            if (data == null) return r;
            var rec = data.GetLevel(levelId);
            if (rec == null)
            {
                data.RecordLevelResult(levelId, 0f, 0, 0); // Run.Complete normally made it already
                rec = data.GetLevel(levelId);
            }
            r.flawlessShards = invincible ? 0 : StoryFlawless.Record(rec, TookHit);
            r.braveShards = StoryBrave.Record(rec, r.brave, assistSpeed);
            if (r.braveShards > 0) Economy.Grant(data, r.braveShards);
            if (!assistSpeed) Splits.Offer(data, levelId, _splits, seconds);
            if (r.flawlessShards > 0) Economy.Grant(data, r.flawlessShards);
            if (!assistSpeed) r.newGhost = StoryGhosts.Offer(data, levelId, seconds, _run);
            if (_compassRoot != null) _compassRoot.gameObject.SetActive(false);
            _compassOn = false;
            return r;
        }

        // ---- shard compass --------------------------------------------------------------------------------------

        void BuildCompass()
        {
            var safe = UIFactory.CreateCanvas(transform, "ShardCompass", 35, landscape: true, out var canvas);
            var ray = canvas.GetComponent<GraphicRaycaster>();
            if (ray != null) ray.enabled = false;
            _compassRoot = UIFactory.Rect("Compass", safe);
            _compassRoot.anchorMin = _compassRoot.anchorMax = new Vector2(0.5f, 0.5f);
            _compassRoot.sizeDelta = new Vector2(120f, 120f);
            _arrow = UIFactory.Rect("Arrow", _compassRoot);
            _arrow.sizeDelta = new Vector2(64f, 64f);
            var img = _arrow.gameObject.AddComponent<Image>();
            img.sprite = Resources.Load<Sprite>("PKR/ZombieLand/Props/pk_arrow");
            img.color = new Color32(255, 214, 90, 230);
            img.raycastTarget = false;
            var lab = UIFactory.Rect("Label", _compassRoot);
            lab.anchoredPosition = new Vector2(0f, -56f);
            lab.sizeDelta = new Vector2(220f, 40f);
            _compassLabel = lab.gameObject.AddComponent<Text>();
            _compassLabel.font = UISprites.HasDisplayFont ? UISprites.DisplayFont : UISprites.Font;
            _compassLabel.fontSize = 26;
            _compassLabel.alignment = TextAnchor.MiddleCenter;
            _compassLabel.color = new Color32(255, 214, 90, 255);
            _compassLabel.text = "SHARD";
            _compassLabel.raycastTarget = false;
            _compassRoot.gameObject.SetActive(false);
        }

        void UpdateCompass()
        {
            if (Time.unscaledTime >= _nextScan)
            {
                _nextScan = Time.unscaledTime + 0.5f;
                _target = NearestShard();
            }
            var cam = Camera.main;
            if (!_target.HasValue || cam == null || _compassRoot == null)
            {
                if (_compassRoot != null) _compassRoot.gameObject.SetActive(false);
                return;
            }
            Vector2 vp = cam.WorldToViewportPoint(_target.Value);
            bool onScreen = vp.x > 0.06f && vp.x < 0.94f && vp.y > 0.1f && vp.y < 0.9f;
            _compassRoot.gameObject.SetActive(!onScreen);
            if (onScreen) return;
            Vector2 dir = (vp - new Vector2(0.5f, 0.5f)).normalized;
            var size = ((RectTransform)_compassRoot.parent).rect.size;
            _compassRoot.anchoredPosition = new Vector2(dir.x * size.x * 0.42f, dir.y * size.y * 0.36f);
            _arrow.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f);
        }

        Vector2? NearestShard()
        {
            var hero = _flow.Player;
            if (hero == null) return null;
            Vector2 from = hero.transform.position;
            Vector2? best = null;
            float bd = float.MaxValue;
            foreach (var p in FindObjectsByType<Pickup>())
            {
                if (p == null || !p.isActiveAndEnabled || p.Kind != PickupKind.StarShard) continue;
                float d = ((Vector2)p.transform.position - from).sqrMagnitude;
                if (d < bd) { bd = d; best = p.transform.position; }
            }
            foreach (var c in FindObjectsByType<ShardCrate>())
            {
                if (c == null || c.Used) continue;
                float d = ((Vector2)c.transform.position - from).sqrMagnitude;
                if (d < bd) { bd = d; best = c.transform.position; }
            }
            if (best.HasValue) { if (_compassLabel != null) _compassLabel.text = "SHARD"; return best; }
            // SECRET SENSE: every shard found; now point at a secret you haven't found this run.
            foreach (var s in FindObjectsByType<SecretArea>())
            {
                if (s == null || _flow.Run.HasSecret(s.secretId)) continue;
                float d = ((Vector2)s.transform.position - from).sqrMagnitude;
                if (d < bd) { bd = d; best = s.transform.position; }
            }
            if (best.HasValue && _compassLabel != null) _compassLabel.text = "SECRET";
            return best;
        }

        /// <summary>Faint markers where you fell on earlier tries (FallMemory), so the tricky spots are easy to spot.</summary>
        void ShowFallMarks(SaveData save, string levelId)
        {
            var f = FallMemory.Get(save, levelId);
            if (f == null) return;
            for (int i = 0; i < f.xs.Count; i++)
            {
                var go = new GameObject("FallMark");
                go.transform.position = new Vector3(f.xs[i], f.ys[i] + 0.4f, 0f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = Resources.Load<Sprite>("PKR/ZombieLand/Props/pk_arrow");
                sr.color = new Color(1f, 0.35f, 0.35f, 0.35f);
                sr.flipY = true; // points down at the spot
                sr.sortingOrder = -5;
                if (_flow.SpriteMaterial != null) sr.sharedMaterial = _flow.SpriteMaterial;
                _marks.Add(go);
            }
        }

        public int FallMarkCount => _marks.Count;

        void OnDestroy()
        {
            foreach (var m in _marks) if (m != null) Destroy(m);
            if (_ghost != null) Destroy(_ghost.gameObject);
        }
    }
}

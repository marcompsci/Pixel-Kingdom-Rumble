using System.Collections.Generic;
using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Arena Clash > TRAINING: a combo counter over the training dummies (hits that land while the target is still
    /// reeling chain into a combo), the best combo this session, Guard Pips broken and launches. What the dummies do
    /// (stand, walk, hop, fight back) is picked on the setup screen.
    /// </summary>
    public class TrainingLab : MonoBehaviour
    {
        readonly ComboCounter _combo = new ComboCounter();
        readonly List<System.Action> _unsubscribe = new List<System.Action>();
        readonly Dictionary<int, int> _pips = new Dictionary<int, int>();
        ArenaMatchController _arena;
        int _lastFlash;
        readonly Dictionary<int, Vector2> _starts = new Dictionary<int, Vector2>();
        GameObject _ui;

        public ComboCounter Combo => _combo;
        public bool Running { get; private set; }

        public void Begin(ArenaMatchController arena)
        {
            Clear();
            _arena = arena;
            _combo.Reset();
            Running = true;
            _starts.Clear();
            foreach (var f in arena.Fighters) if (f.go != null) _starts[f.slot] = f.go.transform.position;
            BuildResetButton();
            foreach (var f in arena.Fighters)
            {
                if (f.human || f.health == null) continue;
                var fighter = f;
                _pips[f.slot] = f.health.Pips != null ? f.health.Pips.Current : 0;
                System.Action<HitResult> onHit = r => OnHit(fighter, r);
                f.health.Hit += onHit;
                _unsubscribe.Add(() => { if (fighter.health != null) fighter.health.Hit -= onHit; });
            }
            PartyHUD.Show("TRAINING LAB  ·  CPU " + TrainingActs.Name(arena.Config.trainingAct), Line, belowMatchClock: true);
        }

        public void Clear()
        {
            if (!Running && _unsubscribe.Count == 0) return;
            Running = false;
            foreach (var u in _unsubscribe) u();
            _unsubscribe.Clear();
            _pips.Clear();
            if (_ui != null) Destroy(_ui);
            _ui = null;
            PartyHUD.Hide();
        }

        /// <summary>RESET: everyone back to their start spot, the dummies' Guard Pips refilled, the counter cleared.</summary>
        public void ResetLab()
        {
            if (_arena == null) return;
            foreach (var f in _arena.Fighters)
            {
                if (f.go == null) continue;
                if (f.go.TryGetComponent(out HeroAbilities a)) a.Cancel();
                if (f.go.TryGetComponent(out AttackRunner atk)) atk.Cancel();
                f.motor.Intent.ClearAll();
                if (_starts.TryGetValue(f.slot, out var at)) f.motor.Teleport(at);
                if (!f.human && f.health != null)
                {
                    f.health.ResetState();
                    _pips[f.slot] = f.health.Pips != null ? f.health.Pips.Current : 0;
                }
            }
            _combo.Reset();
            _lastFlash = 0;
            PartyHUD.Flash("RESET");
        }

        void BuildResetButton()
        {
            if (_ui != null) Destroy(_ui);
            _ui = new GameObject("TrainingLabUI");
            _ui.transform.SetParent(transform, false);
            var safe = UIFactory.CreateCanvas(_ui.transform, "TrainingLabCanvas", 41, landscape: true, out _);
            var holder = UIFactory.Rect("Reset", safe);
            holder.anchorMin = holder.anchorMax = new Vector2(0f, 1f);
            holder.pivot = new Vector2(0f, 1f);
            holder.anchoredPosition = new Vector2(40f, -150f);
            holder.sizeDelta = new Vector2(220f, 90f);
            var b = UIFactory.Button(holder, "RESET", ResetLab, 90f);
            var rt = (RectTransform)b.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        void OnHit(ArenaMatchController.Fighter f, HitResult r)
        {
            if (!Running || r.blocked) return;
            _pips.TryGetValue(f.slot, out int before);
            int broken = Mathf.Max(0, before - r.pipsRemaining);
            _pips[f.slot] = r.pipsRemaining;
            int n = _combo.Register(Time.time, r.hitstunFrames / 60f, broken, r.isLaunch);
            if (n >= 3 && n > _lastFlash)
            {
                _lastFlash = n;
                PartyHUD.Flash($"{n} HIT COMBO!");
            }
            if (n == 1) _lastFlash = 0;
            if (n >= 2 && Services.Save != null && LabRecord.Offer(Services.Save.Data, n))
            {
                Services.Save.MarkDirty();
                if (n >= 5) PartyHUD.Flash($"NEW LAB RECORD: {n} HITS!");
            }
        }

        string Line()
        {
            int now = _combo.Current(Time.time);
            int rec = Services.Save != null ? Services.Save.Data.lifetime.labBestCombo : 0;
            return $"COMBO {now}   ·   BEST {_combo.Best} (RECORD {rec})   ·   HITS {_combo.TotalHits}   ·   PIPS BROKEN {_combo.PipsBroken}   ·   LAUNCHES {_combo.Launches}";
        }

        void OnDestroy() => Clear();
    }
}

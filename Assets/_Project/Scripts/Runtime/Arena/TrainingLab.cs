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

        public ComboCounter Combo => _combo;
        public bool Running { get; private set; }

        public void Begin(ArenaMatchController arena)
        {
            Clear();
            _arena = arena;
            _combo.Reset();
            Running = true;
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
            PartyHUD.Hide();
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
        }

        string Line()
        {
            int now = _combo.Current(Time.time);
            return $"COMBO {now}   ·   BEST {_combo.Best}   ·   HITS {_combo.TotalHits}   ·   PIPS BROKEN {_combo.PipsBroken}   ·   LAUNCHES {_combo.Launches}";
        }

        void OnDestroy() => Clear();
    }
}

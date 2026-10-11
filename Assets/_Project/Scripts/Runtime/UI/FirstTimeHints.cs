using System.Collections.Generic;
using UnityEngine;

namespace PKR
{
    /// <summary>A one-time tip for the HUD ("PUSH UP ON A VINE TO CLIMB").</summary>
    public struct GameHint { public string id; public string text; }

    /// <summary>
    /// First-time experience: watches what the player is doing and shows each tip once per save, the first time it
    /// matters (first level, first enemy nearby, first vine, first guard, first hit, first death). Added by the
    /// StoryHUD, so every story level and contract gets it without any scene changes.
    /// </summary>
    public class FirstTimeHints : MonoBehaviour
    {
        public const string Move = "move", Attack = "attack", Dodge = "dodge", Climb = "climb", Stealth = "stealth",
            Takedown = "takedown", Relics = "relics", Checkpoint = "checkpoint";

        public static readonly Dictionary<string, string> Texts = new Dictionary<string, string>
        {
            { Move, "DRAG THE LEFT STICK TO MOVE\nTAP JUMP  ·  HOLD IT TO JUMP HIGHER" },
            { Attack, "TAP ATK TO STRIKE  ·  SPL FOR YOUR SPECIAL MOVE" },
            { Dodge, "TAP DODGE TO ROLL THROUGH ATTACKS" },
            { Climb, "PUSH UP ON A VINE TO CLIMB IT" },
            { Stealth, "STAY OUT OF THE GUARDS' LIGHT\nDUCK INTO HAY BALES TO HIDE" },
            { Takedown, "STRIKE A GUARD FROM BEHIND\nFOR A SILENT TAKEDOWN" },
            { Relics, "ANCIENT RELICS ARE HIDDEN HERE\nFIND THEM ALL FOR BONUS POINTS" },
            { Checkpoint, "YOU RESTART AT THE LAST\nCHECKPOINT FLAG YOU TOUCHED" },
        };

        float _nextScan;
        Damageable _hero;
        bool _started;

        void OnEnable() => EventBus<PlayerRespawned>.Subscribe(OnRespawned);

        void OnDisable()
        {
            EventBus<PlayerRespawned>.Unsubscribe(OnRespawned);
            if (_hero != null) _hero.Hit -= OnHeroHit;
        }

        void OnRespawned(PlayerRespawned e) { if (e.afterDeath) Show(Checkpoint); }

        void OnHeroHit(HitResult r) => Show(Dodge);

        /// <summary>Shows a tip if this save hasn't seen it yet. Returns true when it was shown.</summary>
        public static bool Show(string id)
        {
            var save = Services.Save;
            if (save == null || !Texts.TryGetValue(id, out var text)) return false;
            if (!save.Data.MarkHintSeen(id)) return false;
            save.SaveNow();
            EventBus<GameHint>.Raise(new GameHint { id = id, text = text });
            return true;
        }

        void Update()
        {
            var flow = LevelFlowController.Current;
            if (flow == null || flow.Run == null || flow.IsComplete) return;
            if (!_started)
            {
                _started = true;
                Show(Move);
                if (StealthTracker.Current != null) Show(Stealth);
                if (MissionTracker.Current != null && MissionTracker.Current.TotalRelics > 0) Show(Relics);
            }
            if (_hero == null && flow.PlayerHealth != null) { _hero = flow.PlayerHealth; _hero.Hit += OnHeroHit; }

            if (Time.unscaledTime < _nextScan) return;
            _nextScan = Time.unscaledTime + 0.4f;
            var player = PlayerMarker.Current;
            if (player == null) return;
            Vector2 p = player.transform.position;

            foreach (var e in FindObjectsByType<EnemyAI>())
            {
                if (e == null || !e.isActiveAndEnabled) continue;
                float d = Vector2.Distance(p, e.transform.position);
                if (e.GetComponent<GuardVision>() != null) { if (d < 6f) Show(Takedown); }
                else if (d < 7f) Show(Attack);
            }
            foreach (var c in FindObjectsByType<ClimbZone>())
            {
                if (c == null) continue;
                Vector2 cp = c.transform.position;
                if (Mathf.Abs(p.x - cp.x) < 2.5f && Mathf.Abs(p.y - cp.y) < c.size.y * 0.5f + 1f) { Show(Climb); break; }
            }
        }
    }
}

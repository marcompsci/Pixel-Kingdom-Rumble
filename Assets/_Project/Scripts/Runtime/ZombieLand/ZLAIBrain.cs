using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// An AI teammate in Last Stand. Sticks near the squad leader (the person playing), keeps the dead at arm's length,
    /// shoots whatever is closest, reloads, and runs to revive anyone who goes down when it's safe enough. AI
    /// teammates don't spend points (they get better guns as the rounds climb instead).
    /// </summary>
    public class ZLAIBrain : IZLBrain
    {
        readonly ZLGame _g;
        readonly float _angle;
        ZLFlowField _toLeader;
        float _flowT, _wobbleT, _dodgeCool;
        Vector2 _wobble;
        bool _trigger;

        public ZLAIBrain(ZLGame g, int slot)
        {
            _g = g;
            _angle = slot * 2.1f;
        }

        public ZLInputFrame Think(ZLPlayer self, float dt)
        {
            var f = new ZLInputFrame();
            Vector2 me = self.transform.position;
            _dodgeCool -= dt;

            // threats
            ZLZombie nearest = null;
            float nd = float.MaxValue;
            int close = 0;
            foreach (var z in _g.Zombies)
            {
                if (z == null || !z.Alive) continue;
                float d = Vector2.Distance(z.transform.position, me);
                if (d < 3f) close++;
                if (d < nd && _g.City.Sees(me, z.transform.position)) { nd = d; nearest = z; }
            }

            // revive a downed teammate when nothing is right on top of us
            ZLPlayer downed = null;
            float dd = float.MaxValue;
            foreach (var h in _g.Squad)
            {
                if (h == null || h == self || !h.Downed) continue;
                float d = Vector2.Distance(h.transform.position, me);
                if (d < dd) { dd = d; downed = h; }
            }
            if (downed != null && close <= 1)
            {
                if (dd < SquadRules.ReviveRange * 0.8f) { f.useHeld = true; return f; }
                f.move = Steer(me, downed.transform.position);
                _trigger = !_trigger;
                f.fire = nearest != null && nd < 6f && (self.Gun == null || self.Gun.automatic || _trigger);
                if (nearest != null) f.face = ((Vector2)nearest.transform.position - me).normalized;
                return f;
            }

            // where to be: a spot around the leader
            var leader = _g.Player != null && _g.Player.Standing ? _g.Player : _g.NearestHero(me);
            Vector2 home = leader != null ? (Vector2)leader.transform.position : me;
            _wobbleT -= dt;
            if (_wobbleT <= 0f) { _wobbleT = Random.Range(1.5f, 3f); _wobble = Random.insideUnitCircle * 0.8f; }
            Vector2 spot = home + new Vector2(Mathf.Cos(_angle), Mathf.Sin(_angle)) * 2.6f + _wobble;
            if (!_g.City.Walkable(spot)) spot = home;
            Vector2 move = Vector2.Distance(me, spot) > 1.2f ? Steer(me, spot) : Vector2.zero;

            // keep the dead at arm's length
            if (nearest != null && nd < 2.4f)
            {
                Vector2 away = (me - (Vector2)nearest.transform.position).normalized;
                if (_g.City.Walkable(me + away * 1.2f)) move = Vector2.Lerp(move, away, 0.7f).normalized;
                if (nd < 1.2f && _dodgeCool <= 0f) { f.dodge = true; _dodgeCool = 2.5f; }
            }
            f.move = Vector2.ClampMagnitude(move, 1f) * 0.9f;

            // shoot what's closest
            var gun = self.Gun;
            if (nearest != null && gun != null && nd < gun.range * 0.95f)
            {
                f.face = ((Vector2)nearest.transform.position - me).normalized;
                // Semi-automatic guns fire on a press: let go of the trigger every other frame.
                _trigger = !_trigger;
                f.fire = gun.automatic || _trigger;
            }
            return f;
        }

        Vector2 Steer(Vector2 me, Vector2 to)
        {
            if (_g.City.Sees(me, to) && Vector2.Distance(me, to) < 6f) return (to - me).normalized;
            _flowT -= Time.deltaTime;
            if (_toLeader == null) _toLeader = _g.IsLastStand ? new ZLFlowField(_g.City, _g.City.w + _g.City.h) : new ZLFlowField(_g.City);
            if (_flowT <= 0f) { _flowT = 0.5f; _toLeader.Build(to); }
            var dir = _toLeader.Direction(me);
            return dir == Vector2.zero ? (to - me).normalized : dir;
        }
    }
}

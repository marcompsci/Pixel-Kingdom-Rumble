using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Host side of an online player: their controls arrive over the network. They move on their own phone, so the hero
    /// here follows the position they report; shooting, using things and reviving happen here, for real.
    /// </summary>
    public class ZLRemoteBrain : IZLBrain
    {
        NetInput _in;
        int _seenUse, _seenMelee, _seenSwap;
        bool _have, _pendingUse, _pendingMelee, _pendingSwap;
        public float LastHeard { get; private set; }

        public int Stale { get; private set; }

        public void Feed(NetInput i)
        {
            // unreliable packets can arrive late: an older one would undo the newer position and presses
            if (_have && !ZLNet.SeqNewer(i.seq, _in.seq)) { Stale++; return; }
            if (_have)
            {
                if (ZLNet.Presses(_seenUse, i.useCount) > 0) _pendingUse = true;
                if (ZLNet.Presses(_seenMelee, i.meleeCount) > 0) _pendingMelee = true;
                if (ZLNet.Presses(_seenSwap, i.swapCount) > 0) _pendingSwap = true;
            }
            _seenUse = i.useCount; _seenMelee = i.meleeCount; _seenSwap = i.swapCount;
            _in = i;
            _have = true;
            LastHeard = Time.unscaledTime;
        }

        public ZLInputFrame Think(ZLPlayer self, float dt)
        {
            if (!_have) return default;
            self.SetNetPosition(new Vector2(_in.posX, _in.posY), new Vector2(_in.aimX, _in.aimY));
            var f = new ZLInputFrame
            {
                face = new Vector2(_in.aimX, _in.aimY),
                fire = _in.fire,
                useHeld = _in.useHeld,
                usePressed = _pendingUse,
                melee = _pendingMelee,
                swap = _pendingSwap,
            };
            _pendingUse = _pendingMelee = _pendingSwap = false;
            return f;
        }
    }
}

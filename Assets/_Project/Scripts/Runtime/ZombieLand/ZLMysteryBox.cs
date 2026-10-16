using System.Collections;
using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Last Stand: the mystery box. A spin rolls any gun but the pistol (the Storm Coil is rarer, and certain after
    /// a dry spell); every few spins the box gets up and moves somewhere else on the block.
    /// </summary>
    public class ZLMysteryBox : ZLMarkerBase, IZLUse
    {
        ZLLastStand _stand;
        int _spot;
        bool _busy;
        Sprite _closed, _open;

        public Vector2 UsePos => transform.position;
        public float UseRange => 1.9f;
        public bool CanUse => !_busy && _stand != null && !_stand.Over && _stand.HasZone(Zone);
        public string Prompt => _stand.Ls.FreeSpinLeft ? "MYSTERY BOX  FREE!" : $"MYSTERY BOX  {LastStandRules.BoxCost}";
        public string Zone => game.City.boxSpots[_spot].zone;
        public bool Busy => _busy;

        public void Init(ZLGame g, ZLLastStand stand, int spot)
        {
            _stand = stand;
            _closed = g.Sprite("Props/box_closed");
            _open = g.Sprite("Props/box_open");
            Setup(g, _closed, "?", new Color(1f, 0.85f, 0.4f), 1.4f);
            MoveTo(spot);
        }

        void MoveTo(int spot)
        {
            _spot = spot;
            var s = game.City.boxSpots[spot];
            transform.position = new Vector2(s.x, s.y);
            if (sr != null) sr.sortingOrder = ZLSort.ByY(s.y);
        }

        public void Use(ZLPlayer p)
        {
            if (!CanUse) return;
            if (!_stand.Ls.PayForSpin()) { _stand.Pay(LastStandRules.BoxCost); return; }
            Sfx.Play(Sfx.Purchase, 0.8f, 0f);
            StartCoroutine(Spin());
        }

        IEnumerator Spin()
        {
            _busy = true;
            sr.sprite = _open;
            var pool = ZLShop.BoxPool();
            float t = 0f;
            int i = 0;
            while (t < 2f)
            {
                var show = pool[i++ % pool.Count].gun;
                if (label != null) label.text = show.name;
                Sfx.Play(Sfx.Click, 0.4f, 0.2f);
                float wait = Mathf.Lerp(0.07f, 0.3f, t / 2f);
                t += wait;
                yield return new WaitForSeconds(wait);
            }
            var run = _stand.Run;
            var prize = ZLShop.RollBox(Random.value, game.Progress.equipped, run.SpinsSinceWonder);
            run.CountSpin(prize.wonder);
            _stand.GiveGun(prize);
            if (label != null) label.text = prize.name;
            game.ShowToast((prize.wonder ? "THE WONDER WEAPON!  " : "") + prize.name + "!", 2.5f);
            Sfx.Play(prize.wonder ? Sfx.Relic : Sfx.Purchase, 0.9f, 0f);
            yield return new WaitForSeconds(1.2f);
            sr.sprite = _closed;
            if (label != null) label.text = "?";
            if (_stand.Ls.BoxShouldMove && game.City.boxSpots.Count > 1)
            {
                game.ShowToast("THE BOX GOT UP AND LEFT! FIND IT AGAIN.", 3f);
                ZLFx.Spawn(GlowHalo.Sprite, transform.position, new Color(0.7f, 0.5f, 1f, 0.8f), 0.6f, 1f, 6f, game.material);
                int next = (_spot + 1 + Random.Range(0, game.City.boxSpots.Count - 1)) % game.City.boxSpots.Count;
                MoveTo(next);
            }
            _busy = false;
        }
    }
}

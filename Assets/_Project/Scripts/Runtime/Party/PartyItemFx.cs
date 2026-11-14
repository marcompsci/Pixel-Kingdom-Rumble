using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>What each item does the moment it's picked up.</summary>
    public static class PartyItemFx
    {
        public const float BoostSeconds = 3f, ShieldSeconds = 6f, GustRange = 4.5f;

        public static void Use(IPartyHost host, GameObject racer, PartyItem item)
        {
            bool isHero = racer.GetComponent<PlayerMarker>() != null;
            if (isHero)
            {
                PartyHUD.Flash(PartyItems.Name(item));
                Sfx.Play(Sfx.PowerUp, 0.8f, 0f);
                if (Services.Haptics != null) Services.Haptics.Play(HapticStrength.Light);
            }
            switch (item)
            {
                case PartyItem.DashFizz:
                    ItemBuffs.On(racer).Boost(BoostSeconds);
                    break;
                case PartyItem.BubbleShield:
                    ItemBuffs.On(racer).Shield(ShieldSeconds, host.SpriteMaterial);
                    break;
                case PartyItem.SeekerStar:
                    SeekerStar.Launch(host, racer);
                    break;
                case PartyItem.SlickPuddle:
                    SlickPuddle.Drop(host, racer);
                    break;
                case PartyItem.ThunderBell:
                    foreach (var r in host.Rivals(racer))
                    {
                        if (ItemBuffs.Knock(r, new Vector2(0f, 6f), 1.2f)) host.OnItemHit(r, racer);
                        PuffEffect.Play(r.transform.position + Vector3.up * 0.6f, new Color32(255, 240, 120, 255), host.SpriteMaterial, 0.3f, 5f, 0.5f);
                    }
                    if (CameraFollow2D.Main != null) CameraFollow2D.Main.Shake(0.2f, 0.25f);
                    Sfx.Play(Sfx.Overcharge, 0.7f, 0f);
                    break;
                case PartyItem.GustFan:
                    // A blast of wind: every rival within GustRange is shoved away from you (a shield blocks it).
                    Vector2 me = racer.transform.position;
                    foreach (var r in host.Rivals(racer))
                    {
                        Vector2 d = (Vector2)r.transform.position - me;
                        if (d.sqrMagnitude > GustRange * GustRange) continue;
                        float dir = d.x >= 0f ? 1f : -1f;
                        if (ItemBuffs.Knock(r, new Vector2(dir * 9f, 4f), 0.6f)) host.OnItemHit(r, racer);
                    }
                    PuffEffect.Play(me + Vector2.up * 0.5f, new Color32(190, 240, 255, 255), host.SpriteMaterial, 0.45f, 7f, 0.5f);
                    Sfx.Play(Sfx.Swing, 0.9f, 0f);
                    break;
            }
        }
    }
}

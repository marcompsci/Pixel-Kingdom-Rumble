using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>What each item does the moment it's picked up.</summary>
    public static class PartyItemFx
    {
        public const float BoostSeconds = 3f, ShieldSeconds = 6f;

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
            }
        }
    }
}

using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Turns an existing fighter into another hero: movement stats, sprite, ability kit, moveset, HP / Guard Pips.
    /// Used when a scene was built with one hero but the player picked another (see SelectedHeroLoader).
    /// </summary>
    public static class HeroLoadout
    {
        public static void Apply(GameObject fighter, CharacterDefinition def)
        {
            if (fighter == null || def == null) return;

            var motor = fighter.GetComponent<PlatformerMotor2D>();
            if (motor != null)
            {
                motor.SetDefinition(def);
                if (motor.Body != null) motor.Teleport(motor.Body.position); // resets air jumps and transient state
            }

            var body = fighter.transform.Find("Body");
            var sr = body != null ? body.GetComponent<SpriteRenderer>() : null;
            if (sr != null && def.bodySprite != null) sr.sprite = def.bodySprite;

            if (fighter.TryGetComponent(out HeroAbilities abilities)) abilities.SetKit(def.kit as HeroKitDefinition);
            if (fighter.TryGetComponent(out AttackRunner attacks))
            {
                attacks.Cancel();
                attacks.Moveset = def.moveset;
            }
            if (fighter.TryGetComponent(out Damageable health)) health.ResetState(); // re-reads HP and Guard Pips from def

            fighter.name = def.displayName;
        }

        /// <summary>Tint the fighter's body sprite (cosmetic palette), keeping hit flashes in step.</summary>
        public static void ApplyTint(GameObject fighter, Color tint)
        {
            if (fighter == null) return;
            if (fighter.TryGetComponent(out HitFlash flash)) flash.SetBaseColor(tint);
            var body = fighter.transform.Find("Body");
            var sr = body != null ? body.GetComponent<SpriteRenderer>() : null;
            if (sr != null) sr.color = tint;
        }
    }
}

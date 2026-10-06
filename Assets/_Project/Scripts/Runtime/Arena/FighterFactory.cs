using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Builds a playable hero at runtime (used by Arena Clash, where the line-up is chosen on the setup screen).
    /// Mirrors the editor-built hero: rigidbody, capsule, motor, Damageable, hurtbox, hero kit, attacks, visuals.
    /// Body sprite is child 0 so FighterVisual finds it.
    /// </summary>
    public static class FighterFactory
    {
        public static GameObject CreateHero(CharacterDefinition def, Material material, Vector2 position,
                                            int team, DamageModel model, Color tint, string name)
        {
            var go = new GameObject(name) { layer = PKRLayers.Player };
            // Build inactive so every component's Awake runs once all components exist
            // (HeroAbilities and Damageable look for AttackRunner in Awake).
            go.SetActive(false);
            go.transform.position = position;

            var body = new GameObject("Body");
            body.transform.SetParent(go.transform, false);
            body.transform.localPosition = new Vector3(0f, -0.7f, 0f);
            var sr = body.AddComponent<SpriteRenderer>();
            sr.sprite = def.bodySprite;
            sr.color = tint;
            if (material != null) sr.sharedMaterial = material;
            sr.sortingOrder = 10 + Mathf.Max(0, team);

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var col = go.AddComponent<CapsuleCollider2D>();
            col.direction = CapsuleDirection2D.Vertical;
            col.size = new Vector2(0.7f, 1.4f);

            go.AddComponent<Invulnerability>();
            var motor = go.AddComponent<PlatformerMotor2D>();
            motor.SetDefinition(def);
            motor.SetGroundMask(PKRLayers.GroundMask);

            var dmg = go.AddComponent<Damageable>();
            dmg.Configure(model, team, def.maxHealth, def.weight, def.armorPercent, model == DamageModel.StoryHealth ? 1f : 0f);

            var hb = new GameObject("Hurtbox") { layer = PKRLayers.Hurtbox };
            hb.transform.SetParent(go.transform, false);
            var hbCol = hb.AddComponent<CapsuleCollider2D>();
            hbCol.isTrigger = true;
            hbCol.size = new Vector2(0.75f, 1.35f);
            hb.AddComponent<Hurtbox>();

            go.AddComponent<HeroAbilities>(); // kit comes from def.kit (NovaKit, BrickKit, ...)
            var attacks = go.AddComponent<AttackRunner>();
            attacks.Moveset = def.moveset;
            attacks.Team = team;
            attacks.debugDrawHitboxes = false;

            go.AddComponent<FighterVisual>();
            go.AddComponent<HitFlash>();
            go.AddComponent<StatusPips>();
            go.SetActive(true);
            return go;
        }
    }
}

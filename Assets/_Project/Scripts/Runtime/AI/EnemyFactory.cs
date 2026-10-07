using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Builds enemies from EnemyDefinition at runtime and recycles them through PoolService.
    /// Structure: root (Rigidbody2D, capsule, motor, Damageable, AI, ContactDamage) with children
    /// Body (sprite, first child for FighterVisual) and Hurtbox (trigger).
    /// </summary>
    public static class EnemyFactory
    {
        public static string PoolKey(EnemyDefinition def) => "pkr.enemy." + def.id;

        public static EnemyAI Spawn(EnemyDefinition def, Vector2 position, int startDir, Material material)
        {
            var go = PoolService.Get(PoolKey(def), () => Build(def, material), maxSize: 32);
            var motor = go.GetComponent<PlatformerMotor2D>();
            motor.Teleport(position);
            var health = go.GetComponent<Damageable>();
            health.Configure(DamageModel.StoryHealth, TeamIds.Enemy, def.maxHealth, def.weight, 0f, 0f);
            go.GetComponent<Invulnerability>().Clear();
            var ai = go.GetComponent<EnemyAI>();
            ai.Init(def, startDir);
            return ai;
        }

        public static void Release(EnemyAI enemy)
        {
            if (enemy == null || enemy.Definition == null) return;
            PoolService.Release(PoolKey(enemy.Definition), enemy.gameObject);
        }

        static GameObject Build(EnemyDefinition def, Material material)
        {
            var go = new GameObject("Enemy_" + def.displayName) { layer = PKRLayers.Enemy };

            // Visual child FIRST so FighterVisual finds it as child 0.
            var body = new GameObject("Body");
            body.transform.SetParent(go.transform, false);
            body.transform.localPosition = new Vector3(0f, -def.bodySize.y * 0.5f, 0f);
            var sr = body.AddComponent<SpriteRenderer>();
            sr.sprite = def.sprite;
            if (material != null) sr.sharedMaterial = material;
            sr.sortingOrder = 9;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var col = go.AddComponent<CapsuleCollider2D>();
            col.direction = def.bodySize.x >= def.bodySize.y ? CapsuleDirection2D.Horizontal : CapsuleDirection2D.Vertical;
            col.size = def.bodySize;

            go.AddComponent<Invulnerability>();
            var motor = go.AddComponent<PlatformerMotor2D>();
            motor.SetGroundMask(PKRLayers.GroundMask);
            go.AddComponent<Damageable>();

            var hb = new GameObject("Hurtbox") { layer = PKRLayers.Hurtbox };
            hb.transform.SetParent(go.transform, false);
            var hbCol = hb.AddComponent<BoxCollider2D>();
            hbCol.isTrigger = true;
            hbCol.size = def.bodySize * 1.05f;
            hb.AddComponent<Hurtbox>();

            var contact = go.AddComponent<ContactDamage>();
            contact.hit = def.contactHit;
            contact.team = TeamIds.Enemy;
            contact.size = def.bodySize * 0.9f;

            if (def.behavior == EnemyBehavior.ShieldWalker)
            {
                // Shield child in front of the body (ShieldGuard keeps it on the facing side).
                var shield = new GameObject("Shield");
                shield.transform.SetParent(go.transform, false);
                shield.transform.localPosition = new Vector3(def.bodySize.x * 0.55f, 0f, 0f);
                var ssr = shield.AddComponent<SpriteRenderer>();
                ssr.sprite = def.shieldSprite;
                if (material != null) ssr.sharedMaterial = material;
                ssr.sortingOrder = 10;
                go.AddComponent<ShieldGuard>().Configure(def.shieldBreakDuration, ssr);
            }

            go.AddComponent<FighterVisual>();
            go.AddComponent<HitFlash>();
            go.AddComponent<EnemyAI>();
            return go;
        }
    }
}

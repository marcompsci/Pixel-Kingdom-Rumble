using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Front shield for a ShieldWalker (Bolt Knight). Light hits into the shield are blocked; a heavy hit breaks it
    /// (the hit lands, the knight is stunned and the shield stays down for a while). Hits from behind always land.
    /// The shield sprite is a child placed in front of the body.
    /// </summary>
    [RequireComponent(typeof(PlatformerMotor2D))]
    public class ShieldGuard : MonoBehaviour, IHitFilter
    {
        public ShieldState Shield { get; private set; } = new ShieldState();
        public SpriteRenderer ShieldSprite { get; set; }

        PlatformerMotor2D _motor;
        Vector3 _shieldLocal;

        void Awake() => _motor = GetComponent<PlatformerMotor2D>();

        public void Configure(float breakDuration, SpriteRenderer sprite)
        {
            Shield = new ShieldState(Mathf.Max(0.1f, breakDuration));
            ShieldSprite = sprite;
            if (sprite != null) _shieldLocal = sprite.transform.localPosition;
        }

        public bool TryBlock(in HitData hit, int attackerFacing, float sourceX)
        {
            var outcome = float.IsNaN(sourceX)
                ? Shield.Resolve(_motor.Facing, attackerFacing, hit.isHeavy)
                : Shield.ResolveFromSource(_motor.Facing, _motor.Body.position.x, sourceX, hit.isHeavy);
            if (outcome == ShieldOutcome.Blocked)
            {
                if (ShieldSprite != null) PuffEffect.Play(ShieldSprite.transform.position, Color.white, ShieldSprite.sharedMaterial, 0.15f, 4f, 0.2f);
                return true;
            }
            if (outcome == ShieldOutcome.Broken)
            {
                _motor.LockControl(Shield.BreakDuration * 0.6f); // stagger: a free window to keep hitting
                if (ShieldSprite != null) PuffEffect.Play(ShieldSprite.transform.position, new Color(1f, 0.8f, 0.3f), ShieldSprite.sharedMaterial, 0.3f, 6f, 0.4f);
            }
            return false;
        }

        void FixedUpdate() => Shield.Tick(Time.fixedDeltaTime);

        void LateUpdate()
        {
            if (ShieldSprite == null) return;
            ShieldSprite.enabled = !Shield.IsBroken;
            // Keep the shield on the facing side (FighterVisual mirrors only the body).
            ShieldSprite.transform.localPosition = new Vector3(Mathf.Abs(_shieldLocal.x) * _motor.Facing, _shieldLocal.y, _shieldLocal.z);
            ShieldSprite.flipX = _motor.Facing < 0;
        }
    }
}

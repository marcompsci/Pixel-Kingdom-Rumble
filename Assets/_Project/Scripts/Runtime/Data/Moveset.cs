using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Which move comes out for each input context. Leave a slot empty if the hero has no move there
    /// (e.g. Nova's air special is the Meteor Drop ability, not a MoveDefinition).
    /// </summary>
    [CreateAssetMenu(menuName = "PKR/Moveset", fileName = "NewMoveset")]
    public class Moveset : ScriptableObject
    {
        [Header("Attack button")]
        public MoveDefinition groundAttack;
        [Tooltip("Attack while holding up on the ground.")]
        public MoveDefinition groundUpAttack;
        public MoveDefinition airAttack;

        [Header("Special button")]
        public MoveDefinition groundSpecial;
        public MoveDefinition airSpecial;

        [Header("Ability hits")]
        [Tooltip("Hit used by an ability's impact (Nova: Meteor Drop landing shockwave).")]
        public MoveDefinition abilityImpact;

        /// <summary>Stick Y above this counts as "up".</summary>
        public const float UpThreshold = 0.6f;

        public MoveDefinition ForAttack(bool grounded, Vector2 stick)
        {
            if (!grounded) return airAttack;
            if (stick.y > UpThreshold && groundUpAttack != null) return groundUpAttack;
            return groundAttack;
        }

        public MoveDefinition ForSpecial(bool grounded) => grounded ? groundSpecial : airSpecial;
    }
}

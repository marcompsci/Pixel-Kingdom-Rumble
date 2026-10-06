using UnityEngine;

namespace PKR
{
    /// <summary>
    /// A trigger collider on the Hurtbox layer that attacks query to find a Damageable.
    /// Put it on a child of the fighter so it can be shaped independently of the physics body.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Hurtbox : MonoBehaviour
    {
        [SerializeField] Damageable owner;

        public Damageable Owner => owner;

        void Awake()
        {
            if (owner == null) owner = GetComponentInParent<Damageable>();
            GetComponent<Collider2D>().isTrigger = true;
            gameObject.layer = PKRLayers.Hurtbox;
        }
    }
}

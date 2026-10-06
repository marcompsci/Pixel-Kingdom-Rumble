namespace PKR
{
    /// <summary>Marks the player-controlled fighter so pickups, triggers and AI can find it.</summary>
    public class PlayerMarker : UnityEngine.MonoBehaviour
    {
        public static PlayerMarker Current { get; private set; }

        void OnEnable() => Current = this;

        void OnDisable()
        {
            if (Current == this) Current = null;
        }
    }
}

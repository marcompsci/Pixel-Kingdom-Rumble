namespace PKR
{
    internal static class TriggerUtil
    {
        /// <summary>True if the collider belongs to the player-controlled fighter.</summary>
        public static bool IsPlayer(UnityEngine.Collider2D other, out PlayerMarker player)
        {
            player = null;
            var rb = other.attachedRigidbody;
            return rb != null && rb.TryGetComponent(out player);
        }
    }
}

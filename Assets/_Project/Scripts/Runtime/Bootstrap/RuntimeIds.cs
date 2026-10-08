namespace PKR
{
    /// <summary>
    /// Unique ids for objects in this play session, used where the code needs an int key per object (hit logs, trap
    /// owners, random seeds). Replaces Object.GetInstanceID, which Unity 6.6 made obsolete-as-error. Never 0.
    /// </summary>
    public static class RuntimeIds
    {
        static int s_last;

        public static int Next()
        {
            unchecked { s_last++; }
            if (s_last == 0) s_last = 1;
            return s_last;
        }
    }
}

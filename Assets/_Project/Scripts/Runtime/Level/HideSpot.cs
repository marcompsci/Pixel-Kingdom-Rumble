using System.Collections.Generic;
using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>A hay bale / awning the hero can hide in. Guards don't see a hidden hero (unless it attacks).</summary>
    public class HideSpot : MonoBehaviour
    {
        public Vector2 size = new Vector2(2f, 1.6f);
        static readonly List<HideSpot> Active = new List<HideSpot>();

        void OnEnable() => Active.Add(this);
        void OnDisable() => Active.Remove(this);

        /// <summary>Is this point (the hero's center) inside any hiding spot?</summary>
        public static bool Contains(Vector2 point)
        {
            foreach (var h in Active)
            {
                Vector2 c = h.transform.position;
                if (Mathf.Abs(point.x - c.x) <= h.size.x * 0.5f && Mathf.Abs(point.y - c.y) <= h.size.y * 0.5f) return true;
            }
            return false;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.9f, 0.8f, 0.3f, 0.6f);
            Gizmos.DrawWireCube(transform.position, size);
        }
    }
}

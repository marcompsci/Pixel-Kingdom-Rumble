using System.Collections.Generic;
using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>Anything a bullet can hit in Zombie Land.</summary>
    public interface IZLHittable
    {
        /// <summary>True for the dead (zombies, nests, the boss); false for the hero's side.</summary>
        bool Hostile { get; }
        bool Alive { get; }
        void Hit(int damage, Vector2 dir, ZLGun gun);
    }

    /// <summary>Something the hero can USE (vehicles, cases, missions, lairs...).</summary>
    public interface IZLUse
    {
        Vector2 UsePos { get; }
        float UseRange { get; }
        bool CanUse { get; }
        string Prompt { get; }
        void Use(ZLPlayer p);
    }

    /// <summary>Simple frame animation for a SpriteRenderer.</summary>
    public class ZLAnim
    {
        public Sprite[] idle = new Sprite[0], move = new Sprite[0], hurt = new Sprite[0];
        public float fps = 8f;

        public Sprite Pick(bool moving, bool hurtNow, float t)
        {
            var clip = hurtNow && hurt.Length > 0 ? hurt : moving && move.Length > 0 ? move : idle.Length > 0 ? idle : move;
            if (clip == null || clip.Length == 0) return null;
            return clip[(int)(t * fps) % clip.Length];
        }
    }
}

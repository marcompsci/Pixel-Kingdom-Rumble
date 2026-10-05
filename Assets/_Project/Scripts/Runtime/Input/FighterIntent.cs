using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// What a fighter wants to do this frame. Written by a human input router or an AI brain,
    /// read by the motor and abilities. Same interface for players and bots.
    /// </summary>
    public class FighterIntent
    {
        public Vector2 Move;
        public readonly ActionBuffer Jump = new ActionBuffer(0.12f);
        public readonly ActionBuffer Attack = new ActionBuffer(0.12f);
        public readonly ActionBuffer Special = new ActionBuffer(0.12f);
        public readonly ActionBuffer Dodge = new ActionBuffer(0.1f);

        public void ClearAll()
        {
            Move = Vector2.zero;
            Jump.Clear(); Attack.Clear(); Special.Clear(); Dodge.Clear();
        }
    }
}

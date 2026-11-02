using System.Collections.Generic;
using UnityEngine;

namespace PKR
{
    /// <summary>What item boxes and items need from the quest they're in (the Item Race or a party arena).</summary>
    public interface IPartyHost
    {
        /// <summary>True for a racer / fighter taking part (the hero or a CPU).</summary>
        bool IsRacer(GameObject go);
        /// <summary>1-based standing right now (items favour whoever is behind).</summary>
        int PlaceOf(GameObject racer);
        int RacerCount { get; }
        /// <summary>Everyone else still in it.</summary>
        IEnumerable<GameObject> Rivals(GameObject racer);
        Material SpriteMaterial { get; }
        /// <summary>An item knocked a racer about (balloon modes pop a balloon, Coin Rush drops coins).</summary>
        void OnItemHit(GameObject victim, GameObject attacker);
    }
}

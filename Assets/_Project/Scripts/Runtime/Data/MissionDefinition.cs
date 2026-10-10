using System.Collections.Generic;
using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>One Shadow Contract: its board text, scene (via the LevelDefinition) and how it unlocks.</summary>
    [CreateAssetMenu(menuName = "PKR/Mission (Shadow Contract)", fileName = "NewContract")]
    public class MissionDefinition : ScriptableObject
    {
        [Tooltip("Stable ID used in saves. Never change after release.")]
        public string id = "ms_new";
        public string displayName = "New Contract";
        [TextArea(2, 4)] public string briefing = "";
        public ContractKind kind = ContractKind.Main;
        [Tooltip("Playable without the Shadow Contracts unlock (the free teaser).")]
        public bool free;
        [Tooltip("Secret contracts: the cipher scroll id that reveals this one on the board.")]
        public string revealedByClue = "";
        [Tooltip("Where to look for the clue, shown on the board while this secret is still hidden.")]
        public string clueHint = "";
        [Tooltip("The cipher scroll id found inside this contract's map (empty if none).")]
        public string containsClue = "";
        public LevelDefinition level;
        public int totalRelics;

        public ContractInfo ToInfo() => new ContractInfo { id = id, kind = kind, free = free, revealedByClue = revealedByClue };
    }

}

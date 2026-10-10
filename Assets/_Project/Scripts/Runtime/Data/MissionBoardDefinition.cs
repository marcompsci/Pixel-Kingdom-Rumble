using System.Collections.Generic;
using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>The Shadow Contracts board: contracts in display order.</summary>
    [CreateAssetMenu(menuName = "PKR/Mission Board", fileName = "MissionBoard")]
    public class MissionBoardDefinition : ScriptableObject
    {
        public string title = "SHADOW CONTRACTS";
        [TextArea(2, 4)] public string description = "";
        public List<MissionDefinition> contracts = new List<MissionDefinition>();

        public ContractInfo[] Infos()
        {
            var list = new ContractInfo[contracts.Count];
            for (int i = 0; i < contracts.Count; i++) list[i] = contracts[i] != null ? contracts[i].ToInfo() : default;
            return list;
        }

        public MissionDefinition FindByLevel(string levelId)
        {
            foreach (var c in contracts) if (c != null && c.level != null && c.level.id == levelId) return c;
            return null;
        }
    }
}

using System.Collections.Generic;
using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>One world of the Shadow Contracts board: its name, door text and colors.</summary>
    [System.Serializable]
    public class ContractWorld
    {
        public string name = "World";
        [TextArea(1, 3)] public string tagline = "";
        public Color color = new Color32(124, 104, 214, 255);
    }

    /// <summary>The Shadow Contracts board: contracts in display order, grouped into worlds.</summary>
    [CreateAssetMenu(menuName = "PKR/Mission Board", fileName = "MissionBoard")]
    public class MissionBoardDefinition : ScriptableObject
    {
        public string title = "SHADOW CONTRACTS";
        [TextArea(2, 4)] public string description = "";
        public List<MissionDefinition> contracts = new List<MissionDefinition>();
        public List<ContractWorld> worlds = new List<ContractWorld>();

        public ContractWorld World(int index) =>
            index >= 0 && index < worlds.Count && worlds[index] != null ? worlds[index] : new ContractWorld { name = "WORLD " + (index + 1) };

        public int IndexOf(string contractId)
        {
            for (int i = 0; i < contracts.Count; i++) if (contracts[i] != null && contracts[i].id == contractId) return i;
            return -1;
        }

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

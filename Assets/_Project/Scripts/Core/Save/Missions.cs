using System;
using System.Collections.Generic;

namespace PKR.Core
{
    /// <summary>Shadow Contracts: main contracts are listed from the start; secret ones appear once their clue is found.</summary>
    public enum ContractKind { Main, Secret }

    /// <summary>What the board can show for one contract.</summary>
    public enum ContractState
    {
        /// <summary>Playable now.</summary>
        Open,
        /// <summary>Listed, but needs the one-time Shadow Contracts unlock.</summary>
        NeedsUnlock,
        /// <summary>A secret contract whose cipher scroll hasn't been found yet (shown as "???").</summary>
        Hidden,
        /// <summary>Its world's door is still sealed: finish every main contract in the world before it.</summary>
        WorldSealed
    }

    /// <summary>The board's view of a contract (engine-free mirror of MissionDefinition).</summary>
    public struct ContractInfo
    {
        public string id;
        public ContractKind kind;
        /// <summary>The free teaser contract(s): playable without the unlock.</summary>
        public bool free;
        /// <summary>Secret contracts: the clue (cipher scroll) that reveals this one.</summary>
        public string revealedByClue;
        /// <summary>Which world (0 = the first) the contract belongs to. Worlds open in order, like doors.</summary>
        public int world;
    }

    public struct ContractSlot
    {
        public string id;
        public ContractState state;
        public bool completed;
        public int bestScore;
        public int bestRelics;
        public bool Playable => state == ContractState.Open;
    }

    [Serializable]
    public class MissionRecord
    {
        public string contractId = "";
        public bool completed;
        public int bestScore;
        public int bestRelics;
    }

    /// <summary>Pure rules for the Shadow Contracts board (unit-tested).</summary>
    public static class MissionBoard
    {
        public static ContractSlot[] Build(IList<ContractInfo> contracts, SaveData save)
        {
            if (contracts == null) return Array.Empty<ContractSlot>();
            bool owned = save != null && save.contractsUnlocked;
            var slots = new ContractSlot[contracts.Count];
            for (int i = 0; i < contracts.Count; i++)
            {
                var c = contracts[i];
                var rec = save != null ? save.GetMission(c.id) : null;
                ContractState state;
                if (c.kind == ContractKind.Secret && !(save != null && save.HasClue(c.revealedByClue)) && !(rec != null && rec.completed))
                    state = ContractState.Hidden;
                else if (!WorldOpen(contracts, save, c.world))
                    state = ContractState.WorldSealed;
                else
                    state = c.free || owned ? ContractState.Open : ContractState.NeedsUnlock;
                slots[i] = new ContractSlot
                {
                    id = c.id, state = state,
                    completed = rec != null && rec.completed,
                    bestScore = rec != null ? rec.bestScore : 0,
                    bestRelics = rec != null ? rec.bestRelics : 0
                };
            }
            return slots;
        }

        /// <summary>Number of worlds on the board (highest world index + 1).</summary>
        public static int WorldCount(IList<ContractInfo> contracts)
        {
            int n = 0;
            if (contracts != null) foreach (var c in contracts) n = Math.Max(n, c.world + 1);
            return n;
        }

        /// <summary>
        /// A world's door is open when every MAIN contract of the world before it is completed (secret contracts never
        /// block a door). World 0 is always open.
        /// </summary>
        public static bool WorldOpen(IList<ContractInfo> contracts, SaveData save, int world)
        {
            if (world <= 0) return true;
            if (!WorldOpen(contracts, save, world - 1)) return false;
            bool any = false;
            foreach (var c in contracts)
            {
                if (c.world != world - 1 || c.kind != ContractKind.Main) continue;
                any = true;
                var rec = save != null ? save.GetMission(c.id) : null;
                if (rec == null || !rec.completed) return false;
            }
            return any;
        }

        /// <summary>Main contracts of a world that are completed / in total (for the board's world header).</summary>
        public static (int done, int total) WorldProgress(IList<ContractInfo> contracts, SaveData save, int world)
        {
            int done = 0, total = 0;
            foreach (var c in contracts)
            {
                if (c.world != world || c.kind != ContractKind.Main) continue;
                total++;
                var rec = save != null ? save.GetMission(c.id) : null;
                if (rec != null && rec.completed) done++;
            }
            return (done, total);
        }

        /// <summary>
        /// The contract behind the exit door of <paramref name="currentId"/>: the next main contract in board order. It
        /// may be in the next world (then <paramref name="crossesWorld"/> is true). Returns -1 when there is none or it
        /// can't be played yet (world still sealed, or the unlock isn't owned).
        /// </summary>
        public static int NextThroughDoor(IList<ContractInfo> contracts, SaveData save, string currentId, out bool crossesWorld)
        {
            crossesWorld = false;
            if (contracts == null) return -1;
            int cur = -1;
            for (int i = 0; i < contracts.Count; i++) if (contracts[i].id == currentId) { cur = i; break; }
            if (cur < 0) return -1;
            var slots = Build(contracts, save);
            for (int i = cur + 1; i < contracts.Count; i++)
            {
                if (contracts[i].kind != ContractKind.Main) continue;
                if (!slots[i].Playable) return -1;
                crossesWorld = contracts[i].world != contracts[cur].world;
                return i;
            }
            return -1;
        }

        /// <summary>Sum of best scores: the "Shadow Rank" total on the board.</summary>
        public static int TotalScore(SaveData save)
        {
            if (save == null || save.missions == null) return 0;
            int total = 0;
            foreach (var m in save.missions) if (m != null) total += Math.Max(0, m.bestScore);
            return total;
        }

        /// <summary>Title for a total score, shown under the board heading.</summary>
        public static string ShadowTitle(int totalScore)
        {
            if (totalScore >= 30000) return "MASTER OF SHADOWS";
            if (totalScore >= 15000) return "SHADOW BLADE";
            if (totalScore >= 6000) return "NIGHT RUNNER";
            if (totalScore > 0) return "INITIATE";
            return "RECRUIT";
        }
    }

    /// <summary>How a finished contract is scored.</summary>
    public static class MissionScore
    {
        public const int ObjectivePoints = 1000;
        public const int RelicPoints = 250;
        public const int TakedownPoints = 150;
        public const int UnseenBonus = 1500;
        public const int SpottedPenalty = 200;
        public const int TimeBonusPerSecond = 10;

        public static int Compute(int objectives, int relics, int takedowns, int timesSpotted, float seconds, float parSeconds)
        {
            int score = Math.Max(0, objectives) * ObjectivePoints + Math.Max(0, relics) * RelicPoints +
                        Math.Max(0, takedowns) * TakedownPoints;
            if (timesSpotted <= 0) score += UnseenBonus;
            else score -= Math.Min(score / 2, timesSpotted * SpottedPenalty);
            if (parSeconds > 0f && seconds < parSeconds) score += (int)((parSeconds - seconds) * TimeBonusPerSecond);
            return Math.Max(0, score);
        }
    }
}

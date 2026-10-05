using System;
using System.Collections.Generic;
using System.Linq;

namespace PKR.Core
{
    public enum MatchMode { Stock, Timed, Training }

    [Serializable]
    public class FighterRecord
    {
        public int slot;
        public string characterId = "";
        public bool isBot;
        public int stocks;
        public int score;
        public int knockouts;
        public int falls;
        /// <summary>Order of elimination in Stock mode (1 = first out). 0 = still alive.</summary>
        public int eliminatedOrder;
        public bool IsEliminated => eliminatedOrder > 0;
    }

    /// <summary>
    /// Rules-only model of an Arena Clash match. The Unity ArenaMatchController drives it
    /// (reports falls, ticks the clock) and reads it for HUD and results.
    /// Timed scoring: +1 for knocking someone out, -1 for falling.
    /// </summary>
    public class MatchState
    {
        public MatchMode Mode { get; }
        public int StartingStocks { get; }
        public float DurationSeconds { get; }
        public float TimeRemaining { get; private set; }
        public bool IsOver { get; private set; }
        public IReadOnlyList<FighterRecord> Fighters => _fighters;

        readonly List<FighterRecord> _fighters = new List<FighterRecord>();
        int _eliminations;

        public MatchState(MatchMode mode, int fighterCount, int startingStocks = 3, float durationSeconds = 120f)
        {
            if (fighterCount < 1 || fighterCount > 4) throw new ArgumentOutOfRangeException(nameof(fighterCount));
            if (mode == MatchMode.Stock && startingStocks < 1) throw new ArgumentOutOfRangeException(nameof(startingStocks));
            if (mode == MatchMode.Timed && durationSeconds <= 0f) throw new ArgumentOutOfRangeException(nameof(durationSeconds));
            Mode = mode;
            StartingStocks = startingStocks;
            DurationSeconds = durationSeconds;
            TimeRemaining = durationSeconds;
            for (int i = 0; i < fighterCount; i++)
                _fighters.Add(new FighterRecord { slot = i, stocks = mode == MatchMode.Stock ? startingStocks : 0 });
        }

        public FighterRecord Get(int slot) => _fighters[slot];

        /// <summary>
        /// A fighter crossed a blast zone. lastAttackerSlot = -1 for self-destructs.
        /// Returns true if the fighter should respawn.
        /// </summary>
        public bool ReportFall(int victimSlot, int lastAttackerSlot)
        {
            if (IsOver) return false;
            var victim = _fighters[victimSlot];
            if (victim.IsEliminated) return false;

            victim.falls++;
            bool creditAttacker = lastAttackerSlot >= 0 && lastAttackerSlot < _fighters.Count && lastAttackerSlot != victimSlot;
            if (creditAttacker) _fighters[lastAttackerSlot].knockouts++;

            switch (Mode)
            {
                case MatchMode.Training:
                    return true;
                case MatchMode.Timed:
                    victim.score -= 1;
                    if (creditAttacker) _fighters[lastAttackerSlot].score += 1;
                    return true;
                default: // Stock
                    victim.stocks = Math.Max(0, victim.stocks - 1);
                    if (victim.stocks > 0) return true;
                    victim.eliminatedOrder = ++_eliminations;
                    if (_fighters.Count(f => !f.IsEliminated) <= 1) IsOver = true;
                    return false;
            }
        }

        public void Tick(float deltaTime)
        {
            if (IsOver || Mode != MatchMode.Timed || deltaTime <= 0f) return;
            TimeRemaining = Math.Max(0f, TimeRemaining - deltaTime);
            if (TimeRemaining <= 0f) IsOver = true;
        }

        /// <summary>Ends Training (or any mode) manually.</summary>
        public void End() => IsOver = true;

        /// <summary>
        /// 1-based placement per slot. Ties share a placement (e.g. two fighters tied for first both get 1).
        /// Stock: alive fighters first (more stocks better), then reverse elimination order.
        /// Timed: by score, then fewer falls.
        /// </summary>
        public int[] Placements()
        {
            var keyed = _fighters.Select(f => (f.slot, key: RankKey(f))).OrderByDescending(t => t.key).ToList();
            var result = new int[_fighters.Count];
            for (int i = 0; i < keyed.Count; i++)
            {
                if (i > 0 && keyed[i].key == keyed[i - 1].key) result[keyed[i].slot] = result[keyed[i - 1].slot];
                else result[keyed[i].slot] = i + 1;
            }
            return result;
        }

        long RankKey(FighterRecord f)
        {
            if (Mode == MatchMode.Stock)
            {
                // Alive: 1_000_000 + stocks. Eliminated: later elimination = higher key.
                return f.IsEliminated ? f.eliminatedOrder : 1_000_000L + f.stocks;
            }
            // Timed/Training: score dominates, falls break ties.
            return (long)f.score * 1000L - f.falls;
        }
    }
}

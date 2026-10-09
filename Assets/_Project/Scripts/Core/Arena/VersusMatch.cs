using System;

namespace PKR.Core
{
    public enum VersusPhase { Waiting, Fighting, RoundOver, MatchOver }

    /// <summary>
    /// Rules for a 1-on-1 Versus match (pure, unit-tested): rounds with a clock, first to RoundsToWin round wins.
    /// A round ends by K.O. (health 0) or time up (higher health share wins; equal = draw, nobody scores).
    /// After MaxRounds the leader wins; if still level the match is a draw.
    /// Slot 0 is player 1, slot 1 the opponent.
    /// </summary>
    public class VersusMatch
    {
        public const int DefaultRoundsToWin = 2;
        public const float DefaultRoundSeconds = 60f;
        public const int MaxRounds = 5;

        public int RoundsToWin { get; }
        public float RoundSeconds { get; }
        public int Round { get; private set; }
        public float TimeLeft { get; private set; }
        public VersusPhase Phase { get; private set; } = VersusPhase.Waiting;
        /// <summary>Winner of the last finished round: 0, 1, or -1 for a draw.</summary>
        public int LastRoundWinner { get; private set; } = -1;
        public bool LastRoundWasTimeUp { get; private set; }
        /// <summary>Match winner once Phase is MatchOver: 0, 1, or -1 for a draw.</summary>
        public int Winner { get; private set; } = -1;

        readonly int[] _wins = new int[2];

        public VersusMatch(int roundsToWin = DefaultRoundsToWin, float roundSeconds = DefaultRoundSeconds)
        {
            RoundsToWin = Math.Max(1, roundsToWin);
            RoundSeconds = roundSeconds > 0f ? roundSeconds : DefaultRoundSeconds;
        }

        public int Wins(int slot) => slot == 0 || slot == 1 ? _wins[slot] : 0;

        /// <summary>Begin the next round (fighters are reset by the caller).</summary>
        public void StartRound()
        {
            if (Phase == VersusPhase.MatchOver || Phase == VersusPhase.Fighting) return;
            Round++;
            TimeLeft = RoundSeconds;
            Phase = VersusPhase.Fighting;
        }

        /// <summary>Count down the clock. Returns true if time ran out this tick (call EndRoundOnTime next).</summary>
        public bool Tick(float dt)
        {
            if (Phase != VersusPhase.Fighting || dt <= 0f) return false;
            TimeLeft = Math.Max(0f, TimeLeft - dt);
            return TimeLeft <= 0f;
        }

        /// <summary>The fighter in loserSlot was knocked out.</summary>
        public void ReportKO(int loserSlot)
        {
            if (Phase != VersusPhase.Fighting || (loserSlot != 0 && loserSlot != 1)) return;
            FinishRound(1 - loserSlot, timeUp: false);
        }

        /// <summary>Both fighters K.O.'d on the same frame: a draw round.</summary>
        public void ReportDoubleKO()
        {
            if (Phase != VersusPhase.Fighting) return;
            FinishRound(-1, timeUp: false);
        }

        /// <summary>Time up: decide by remaining health share (0..1 each).</summary>
        public void EndRoundOnTime(float health0, float health1)
        {
            if (Phase != VersusPhase.Fighting) return;
            FinishRound(TimeUpWinner(health0, health1), timeUp: true);
        }

        public static int TimeUpWinner(float health0, float health1)
        {
            const float eps = 0.0001f;
            if (health0 > health1 + eps) return 0;
            if (health1 > health0 + eps) return 1;
            return -1;
        }

        void FinishRound(int winner, bool timeUp)
        {
            LastRoundWinner = winner;
            LastRoundWasTimeUp = timeUp;
            if (winner >= 0) _wins[winner]++;

            if (winner >= 0 && _wins[winner] >= RoundsToWin)
            {
                Winner = winner;
                Phase = VersusPhase.MatchOver;
            }
            else if (Round >= MaxRounds)
            {
                Winner = _wins[0] > _wins[1] ? 0 : _wins[1] > _wins[0] ? 1 : -1;
                Phase = VersusPhase.MatchOver;
            }
            else Phase = VersusPhase.RoundOver;
        }

        /// <summary>Whole seconds shown on the clock (rounded up so 0.3 s shows 1).</summary>
        public int ClockSeconds => (int)Math.Ceiling(Math.Max(0f, TimeLeft));
    }

    /// <summary>Health for Versus: the same for every fighter, nudged by weight so heavies last a little longer.</summary>
    public static class VersusRules
    {
        public const int BaseHealth = 24;

        public static int HealthFor(float weight)
        {
            int bonus = (int)Math.Round((weight - 1f) * 8f);
            return Math.Max(16, Math.Min(34, BaseHealth + bonus));
        }

        /// <summary>Star Shards for finishing a Versus match.</summary>
        public static int Reward(bool won, bool perfectRound) => won ? (perfectRound ? 45 : 35) : 10;
    }
}

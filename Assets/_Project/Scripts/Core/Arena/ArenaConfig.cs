using System;

namespace PKR.Core
{
    /// <summary>Options chosen on the arena setup screen.</summary>
    [Serializable]
    public class ArenaMatchConfig
    {
        public const int MinBots = 1, MaxBots = 3;
        public const int MinStocks = 1, MaxStocks = 5;
        public const int MinMinutes = 1, MaxMinutes = 5;

        public MatchMode mode = MatchMode.Stock;
        public int botCount = 3;
        public BotLevel botLevel = BotLevel.Normal;
        public int stocks = 3;
        public int minutes = 2;

        public int FighterCount => 1 + botCount;
        public float DurationSeconds => minutes * 60f;

        public void Clamp()
        {
            botCount = Math.Max(MinBots, Math.Min(MaxBots, botCount));
            stocks = Math.Max(MinStocks, Math.Min(MaxStocks, stocks));
            minutes = Math.Max(MinMinutes, Math.Min(MaxMinutes, minutes));
        }

        public MatchState CreateMatch()
        {
            Clamp();
            return new MatchState(mode, FighterCount, stocks, DurationSeconds);
        }

        /// <summary>Cycle helper for setup buttons: next value in [min,max], wrapping.</summary>
        public static int Cycle(int value, int min, int max) => value >= max ? min : value + 1;

        public static MatchMode Next(MatchMode m) =>
            m == MatchMode.Stock ? MatchMode.Timed : (m == MatchMode.Timed ? MatchMode.Training : MatchMode.Stock);

        public static BotLevel Next(BotLevel l) =>
            l == BotLevel.Easy ? BotLevel.Normal : (l == BotLevel.Normal ? BotLevel.Hard : BotLevel.Easy);
    }

    public enum BridgePhase { Solid, Warning, Open }

    /// <summary>
    /// Skyforge Arena's center bridge: solid, then a warning flash, then it retracts and leaves a gap, repeating.
    /// </summary>
    public struct BridgeCycle
    {
        public float solidSeconds;
        public float warningSeconds;
        public float openSeconds;

        public BridgeCycle(float solid, float warning, float open)
        {
            solidSeconds = Math.Max(0.1f, solid);
            warningSeconds = Math.Max(0f, warning);
            openSeconds = Math.Max(0.1f, open);
        }

        public float Period => solidSeconds + warningSeconds + openSeconds;

        public BridgePhase PhaseAt(float t)
        {
            float p = Period;
            float x = t % p;
            if (x < 0f) x += p;
            if (x < solidSeconds) return BridgePhase.Solid;
            if (x < solidSeconds + warningSeconds) return BridgePhase.Warning;
            return BridgePhase.Open;
        }
    }
}

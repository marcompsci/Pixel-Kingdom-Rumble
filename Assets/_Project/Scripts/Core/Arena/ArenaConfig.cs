using System;

namespace PKR.Core
{
    /// <summary>Options chosen on the arena setup screen.</summary>
    [Serializable]
    public class ArenaMatchConfig
    {
        public const int MinBots = 1, MaxBots = 3;
        public const int MaxFighters = 4;
        public const int MinStocks = 1, MaxStocks = 5;
        public const int MinMinutes = 1, MaxMinutes = 5;

        public MatchMode mode = MatchMode.Stock;
        public int botCount = 3;
        public BotLevel botLevel = BotLevel.Normal;
        public int stocks = 3;
        public int minutes = 2;
        /// <summary>CPU level follows your Rumble skill (AdaptiveDifficulty) instead of botLevel.</summary>
        public bool autoLevel = true;
        /// <summary>1, or 2 for couch play (P2 on a controller or the right half of the screen).</summary>
        public int humans = 1;
        /// <summary>Couch play: P2's fighter (null/empty = random).</summary>
        public string p2CharacterId;
        public Mutator mutators = Mutator.None;
        /// <summary>This match is today's Daily Rumble (set by the setup screen, not by the player).</summary>
        public bool daily;

        public int FighterCount => humans + botCount;
        /// <summary>Two players can fight each other with no CPUs.</summary>
        public int MinBotsNow => humans >= 2 ? 0 : MinBots;
        public int MaxBotsNow => MaxFighters - humans;
        /// <summary>Only solo matches against the CPU count for Rumble Rating.</summary>
        public bool Rated => humans == 1 && mode != MatchMode.Training;
        public float DurationSeconds => minutes * 60f;

        public void Clamp()
        {
            humans = Math.Max(1, Math.Min(2, humans));
            if (mode == MatchMode.Training) humans = 1;
            botCount = Math.Max(MinBotsNow, Math.Min(MaxBotsNow, botCount));
            if ((mutators & Mutator.SuddenDeath) != 0) stocks = 1;
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

        /// <summary>Setup button cycle: Auto, Easy, Normal, Hard, Auto...</summary>
        public void NextLevel()
        {
            if (autoLevel) { autoLevel = false; botLevel = BotLevel.Easy; }
            else if (botLevel == BotLevel.Hard) autoLevel = true;
            else botLevel = Next(botLevel);
        }

        public ArenaMatchConfig Copy() => (ArenaMatchConfig)MemberwiseClone();

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

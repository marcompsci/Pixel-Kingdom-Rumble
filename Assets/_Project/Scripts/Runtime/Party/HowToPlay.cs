using PKR.Core;

namespace PKR
{
    /// <summary>
    /// First time in each party quest (per save): a short how-to-play card. Seen ids live in the save's seenHints, so
    /// resetting the save shows them again.
    /// </summary>
    public static class HowToPlay
    {
        public const float Seconds = 7f;

        public static string Text(PartyMode m) => m switch
        {
            PartyMode.ItemRace => "RUN RIGHT TO THE FLAG  ·  JUMP THE GAPS\nGRAB ? BOXES FOR ITEMS  ·  THE BACK OF THE PACK GETS THE BEST ONES",
            PartyMode.BalloonBattle => "THREE BALLOONS EACH  ·  A LAUNCH OR THREE HITS POPS ONE\nLAST HERO FLOATING WINS",
            PartyMode.RingOut => "THREE LIVES  ·  KNOCK RIVALS OUT OF THE RING\nAFTER A MINUTE THE RING CLOSES IN",
            PartyMode.CoinRush => "GRAB THE MOST COINS IN 90 SECONDS\nGET HIT AND YOU DROP 2  ·  FALL AND YOU DROP 5",
            PartyMode.KingOfTheHill => "STAND ALONE IN THE GOLD ZONE TO SCORE\nIT MOVES EVERY 15 SECONDS  ·  KNOCK RIVALS OUT OF IT",
            _ => "",
        };

        public static string HintId(PartyMode m) => "party_" + (int)m;

        /// <summary>Shows the card if this save hasn't seen it. Returns true when shown.</summary>
        public static bool ShowOnce(PartyMode m)
        {
            var save = Services.Save;
            string text = Text(m);
            if (save == null || text.Length == 0 || !save.Data.MarkHintSeen(HintId(m))) return false;
            save.SaveNow();
            PartyHUD.Tip(text, Seconds);
            return true;
        }
    }
}

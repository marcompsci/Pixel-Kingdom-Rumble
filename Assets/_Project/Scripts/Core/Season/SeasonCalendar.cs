using System;

namespace PKR.Core
{
    /// <summary>The live seasons Story Quest follows, by the device's own date.</summary>
    public enum SeasonEvent { None = 0, Autumn = 1, Harvest = 2, Winterfest = 3, Frost = 4 }

    /// <summary>What a season looks like in a level: autumn colour and leaves, or winter snow.</summary>
    public enum SeasonLook { None = 0, Autumn = 1, Winter = 2 }

    /// <summary>The peak days inside a season (the holiday itself), when everything is turned up.</summary>
    public enum SeasonPeak { None = 0, HalloweenWeek = 1, ThanksgivingWeek = 2, Christmas = 3, NewYear = 4 }

    /// <summary>The season on one day: which event, its first and last day, and whether it is at its peak.</summary>
    public readonly struct SeasonState
    {
        public readonly SeasonEvent Event;
        public readonly DateTime Start, End; // whole days, both inclusive
        public readonly SeasonPeak Peak;

        public SeasonState(SeasonEvent ev, DateTime start, DateTime end, SeasonPeak peak)
        {
            Event = ev; Start = start.Date; End = end.Date; Peak = peak;
        }

        public bool Active => Event != SeasonEvent.None;
        public bool AtPeak => Peak != SeasonPeak.None;
        public SeasonLook Look => SeasonCalendar.LookOf(Event);
        /// <summary>Save key for this year's run of the season ("harvest_2026"). Winterfest keeps the December year.</summary>
        public string Key => Active ? SeasonCalendar.KeyPrefix(Event) + "_" + Start.Year : "";
        /// <summary>How strong the dressing is: 1 at the peak, 0.65 the rest of the season, 0 out of season.</summary>
        public float Intensity => !Active ? 0f : AtPeak ? 1f : 0.65f;
        /// <summary>Days left including today (1 on the last day).</summary>
        public int DaysLeft(DateTime today) => Active ? Math.Max(0, (End - today.Date).Days + 1) : 0;
    }

    /// <summary>
    /// Real-world calendar for the live seasons. Story Quest switches its look by itself:
    ///   Autumn      22 Sep - 31 Oct   (peak: Halloween week, 25-31 Oct)
    ///   Harvest      1 Nov - 30 Nov   (peak: Thanksgiving week, Monday to Sunday around the 4th Thursday)
    ///   Winterfest   1 Dec -  1 Jan   (peaks: Christmas 20-26 Dec, New Year 31 Dec - 1 Jan)
    ///   Frost        2 Jan - end Feb
    /// Spring and summer keep each world's normal look.
    /// </summary>
    public static class SeasonCalendar
    {
        /// <summary>Settings: 0 = follow the calendar (default), 1 = off.</summary>
        public const int ModeLive = 0, ModeOff = 1;

        /// <summary>Tests and screenshots only: pretend today is this date.</summary>
        public static DateTime? OverrideToday;

        public static DateTime Today => (OverrideToday ?? DateTime.Now).Date;

        public static SeasonState Current(int settingsMode) => settingsMode == ModeOff ? default : At(Today);

        public static SeasonState At(DateTime day)
        {
            day = day.Date;
            int y = day.Year, m = day.Month, d = day.Day;
            if ((m == 9 && d >= 22) || m == 10)
            {
                var peak = m == 10 && d >= 25 ? SeasonPeak.HalloweenWeek : SeasonPeak.None;
                return new SeasonState(SeasonEvent.Autumn, new DateTime(y, 9, 22), new DateTime(y, 10, 31), peak);
            }
            if (m == 11)
            {
                var tg = Thanksgiving(y);
                var peak = day >= tg.AddDays(-3) && day <= tg.AddDays(3) ? SeasonPeak.ThanksgivingWeek : SeasonPeak.None;
                return new SeasonState(SeasonEvent.Harvest, new DateTime(y, 11, 1), new DateTime(y, 11, 30), peak);
            }
            if (m == 12 || (m == 1 && d == 1))
            {
                int startYear = m == 12 ? y : y - 1;
                var peak = SeasonPeak.None;
                if (m == 12 && d >= 20 && d <= 26) peak = SeasonPeak.Christmas;
                else if ((m == 12 && d == 31) || m == 1) peak = SeasonPeak.NewYear;
                return new SeasonState(SeasonEvent.Winterfest, new DateTime(startYear, 12, 1), new DateTime(startYear + 1, 1, 1), peak);
            }
            if (m == 1 || m == 2)
                return new SeasonState(SeasonEvent.Frost, new DateTime(y, 1, 2), new DateTime(y, 2, DateTime.DaysInMonth(y, 2)), SeasonPeak.None);
            return default;
        }

        /// <summary>The next season to start after <paramref name="day"/>, and its first day.</summary>
        public static (SeasonEvent ev, DateTime start) Next(DateTime day)
        {
            day = day.Date;
            var cur = At(day);
            var probe = cur.Active ? cur.End.AddDays(1) : day.AddDays(1);
            for (int i = 0; i < 400; i++, probe = probe.AddDays(1))
            {
                var s = At(probe);
                if (s.Active && s.Event != cur.Event) return (s.Event, s.Start);
            }
            return (SeasonEvent.None, day);
        }

        /// <summary>
        /// The holiday peak running today (with its first day, which may be earlier), or else the next one to start.
        /// </summary>
        public static (SeasonPeak peak, DateTime start) NextPeak(DateTime day)
        {
            day = day.Date;
            var now = At(day);
            if (now.AtPeak)
            {
                var first = day;
                while (At(first.AddDays(-1)).Peak == now.Peak) first = first.AddDays(-1);
                return (now.Peak, first);
            }
            var probe = day.AddDays(1);
            for (int i = 0; i < 400; i++, probe = probe.AddDays(1))
            {
                var s = At(probe);
                if (s.AtPeak) return (s.Peak, probe);
            }
            return (SeasonPeak.None, day);
        }

        /// <summary>The fourth Thursday of November.</summary>
        public static DateTime Thanksgiving(int year)
        {
            var d = new DateTime(year, 11, 1);
            int offset = ((int)DayOfWeek.Thursday - (int)d.DayOfWeek + 7) % 7;
            return d.AddDays(offset + 21);
        }

        public static SeasonLook LookOf(SeasonEvent ev) =>
            ev == SeasonEvent.Autumn || ev == SeasonEvent.Harvest ? SeasonLook.Autumn
            : ev == SeasonEvent.Winterfest || ev == SeasonEvent.Frost ? SeasonLook.Winter : SeasonLook.None;

        public static string KeyPrefix(SeasonEvent ev) => ev switch
        {
            SeasonEvent.Autumn => "autumn",
            SeasonEvent.Harvest => "harvest",
            SeasonEvent.Winterfest => "winterfest",
            SeasonEvent.Frost => "frost",
            _ => "",
        };

        public static string Name(SeasonEvent ev) => ev switch
        {
            SeasonEvent.Autumn => "Autumn Leaves",
            SeasonEvent.Harvest => "Harvest Season",
            SeasonEvent.Winterfest => "Winterfest",
            SeasonEvent.Frost => "Frost Season",
            _ => "",
        };

        public static string PeakName(SeasonPeak p) => p switch
        {
            SeasonPeak.HalloweenWeek => "Halloween Week",
            SeasonPeak.ThanksgivingWeek => "Thanksgiving Week",
            SeasonPeak.Christmas => "Christmas",
            SeasonPeak.NewYear => "New Year",
            _ => "",
        };

        /// <summary>One line for the menu banner: what is happening in the kingdom this season.</summary>
        public static string Tagline(SeasonState s) => s.Event switch
        {
            SeasonEvent.Autumn => s.AtPeak ? "Jack-o'-lanterns glow on every ledge" : "Red leaves drift over every world",
            SeasonEvent.Harvest => s.AtPeak ? "Feast tables are set at every checkpoint" : "Pumpkins, hay and falling leaves",
            SeasonEvent.Winterfest => s.Peak == SeasonPeak.Christmas ? "Heavy snow, lit trees and gifts at every goal"
                                    : s.Peak == SeasonPeak.NewYear ? "Fireworks over the snowy kingdom"
                                    : "Snow and string lights across the kingdom",
            SeasonEvent.Frost => "Quiet snow over every world",
            _ => "",
        };

        /// <summary>"in 3 days", "tomorrow", "today".</summary>
        public static string InDays(int days) => days <= 0 ? "today" : days == 1 ? "tomorrow" : $"in {days} days";
    }
}

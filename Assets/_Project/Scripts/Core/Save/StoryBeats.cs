using System.Collections.Generic;

namespace PKR.Core
{
    public enum BeatTiming { BeforeLevel, AfterClear }

    /// <summary>One comic panel: who speaks (or "" for narration), what they say, and the art to show.</summary>
    public struct BeatPanel
    {
        public string speaker;
        public string text;
        /// <summary>hero, warden, sovereign, ledger, mainspring, realms, isles, peaks, dawn.</summary>
        public string art;
        public BeatPanel(string speaker, string text, string art) { this.speaker = speaker; this.text = text; this.art = art; }
    }

    public struct StoryBeat
    {
        public string id;
        public string levelId;
        public BeatTiming timing;
        public BeatPanel[] panels;
    }

    /// <summary>
    /// The Story Quest's short comic-panel scenes: the prologue, the trail between worlds, and the boss entrances and
    /// defeats. Each plays once (SaveData.seenCutscenes); the level select can replay them later. Pure data + rules.
    /// </summary>
    public static class StoryBeats
    {
        public static readonly StoryBeat[] All =
        {
            new StoryBeat { id = "beat_prologue", levelId = "sq_sunspire_test", timing = BeatTiming.BeforeLevel, panels = new[]
            {
                new BeatPanel("", "The Sunspire Isles float on the Tickworks: an engine older than anyone's grandmother's grandmother.", "isles"),
                new BeatPanel("", "This morning, every gear on the isles started turning backward.", "isles"),
                new BeatPanel("NOVA", "Somebody has to go down to the engine hall and ask it nicely to stop. Guess that's me.", "hero"),
            }},
            new StoryBeat { id = "beat_warden_intro", levelId = "sq_clockwork_warden", timing = BeatTiming.BeforeLevel, panels = new[]
            {
                new BeatPanel("", "The engine hall. Something huge unfolds from the wall, piston first.", "warden"),
                new BeatPanel("CLOCKWORK WARDEN", "INTRUDER. THE ENGINE IS RUNNING TO SCHEDULE. THE SCHEDULE IS BACKWARD.", "warden"),
                new BeatPanel("NOVA", "Watch the piston. When it gets stuck, hit the core.", "hero"),
            }},
            new StoryBeat { id = "beat_warden_down", levelId = "sq_clockwork_warden", timing = BeatTiming.AfterClear, panels = new[]
            {
                new BeatPanel("", "The Warden winds down with a long, tired whistle.", "warden"),
                new BeatPanel("NOVA", "It wasn't broken. Someone reset it. There's a gearwright's mark on the casing.", "hero"),
            }},
            new StoryBeat { id = "beat_ledger", levelId = "sq_rooftop_run", timing = BeatTiming.BeforeLevel, panels = new[]
            {
                new BeatPanel("", "The mark leads to the Brasslight Quarter, where the Gearwright kept a ledger of every change to the engine.", "ledger"),
                new BeatPanel("NOVA", "The Gearwatch guards the rooftops at night. Quiet feet, then.", "hero"),
            }},
            new StoryBeat { id = "beat_north", levelId = "sq_frostgear_ascent", timing = BeatTiming.BeforeLevel, panels = new[]
            {
                new BeatPanel("", "The ledger's last page: \"Mainspring moved north, under the glacier, for safekeeping.\"", "ledger"),
                new BeatPanel("NOVA", "Safekeeping. Sure. Pack a scarf.", "peaks"),
            }},
            new StoryBeat { id = "beat_realms", levelId = "sq_ember_wastes", timing = BeatTiming.BeforeLevel, panels = new[]
            {
                new BeatPanel("", "Under the glacier, the mainspring. Nova frees it, and the whole engine takes a breath.", "mainspring"),
                new BeatPanel("", "Then the heart of the Tickworks cracks into four sealed realms, each running on its own broken rule.", "realms"),
                new BeatPanel("NOVA", "Fire, wind, water and toys. Somewhere in the middle, whatever split it. One realm at a time.", "hero"),
            }},
            new StoryBeat { id = "beat_sovereign_intro", levelId = "sq_fourfold_sovereign", timing = BeatTiming.BeforeLevel, panels = new[]
            {
                new BeatPanel("", "Where the four realms meet, the engine's own mind waits, wearing the Warden's old shell.", "sovereign"),
                new BeatPanel("FOURFOLD SOVEREIGN", "I SPLIT MYSELF TO KEEP RUNNING. FOUR RULES. NO STOPPING.", "sovereign"),
                new BeatPanel("NOVA", "Same tricks as the Warden, just faster. Stay off the cogs and wait for the slam.", "hero"),
            }},
            new StoryBeat { id = "beat_sovereign_down", levelId = "sq_fourfold_sovereign", timing = BeatTiming.AfterClear, panels = new[]
            {
                new BeatPanel("", "The four realms fold back into one heart. Every gear on the isles stops, then turns forward again.", "dawn"),
                new BeatPanel("NOVA", "Morning. The right way round, this time.", "hero"),
                new BeatPanel("", "THE END ... of this chapter. Thanks for playing Pixel Kingdom Rumble.", "dawn"),
            }},
        };

        public static StoryBeat? For(string levelId, BeatTiming timing)
        {
            foreach (var b in All) if (b.levelId == levelId && b.timing == timing) return b;
            return null;
        }

        /// <summary>The scene to play now, or null if there is none or it was already seen.</summary>
        public static StoryBeat? Pending(SaveData save, string levelId, BeatTiming timing)
        {
            var b = For(levelId, timing);
            if (!b.HasValue) return null;
            if (save != null && save.seenCutscenes != null && save.seenCutscenes.Contains(b.Value.id)) return null;
            return b;
        }

        public static bool MarkSeen(SaveData save, string id)
        {
            if (save == null || string.IsNullOrEmpty(id)) return false;
            if (save.seenCutscenes == null) save.seenCutscenes = new List<string>();
            if (save.seenCutscenes.Contains(id)) return false;
            save.seenCutscenes.Add(id);
            return true;
        }
    }
}

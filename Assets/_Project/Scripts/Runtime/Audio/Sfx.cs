using System.Collections.Generic;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Names of the synthesized clips in Resources/Audio (made by Tools/make_audio.py) and a one-line way to play them.
    /// Clips load lazily and are cached; a missing clip is silently skipped, so gameplay never depends on audio.
    /// </summary>
    public static class Sfx
    {
        public const string Jump = "jump", Land = "land", Hit = "hit", HitHeavy = "hit_heavy", Hurt = "hurt", Swing = "swing",
            Shard = "shard", Relic = "relic", Heal = "heal", Checkpoint = "checkpoint", Spotted = "spotted",
            Takedown = "takedown", Spring = "spring", Crate = "crate", Click = "ui_click", Secret = "secret", KO = "ko",
            Victory = "victory", Purchase = "purchase", Special = "special", Jetpack = "jetpack", AirJump = "air_jump",
            Shoot = "shoot", Reload = "reload", Groan = "groan", RoundStart = "round_start", PowerUp = "powerup", Board = "board",
            Burst = "burst", Split = "split", Dig = "dig", Emerge = "emerge", Kindle = "kindle", Howl = "howl",
            Overcharge = "overcharge", Prestige = "prestige";
        /// <summary>The dead's groans: picked at random so a crowd doesn't repeat one voice.</summary>
        public static readonly string[] Groans = { "groan", "groan_1", "groan_2" };
        public static string AnyGroan => Groans[UnityEngine.Random.Range(0, Groans.Length)];
        public const string MusicStory = "music_story", MusicStealth = "music_stealth", MusicVersus = "music_versus",
            MusicTitle = "music_title", MusicFrost = "music_frost", MusicRealms = "music_realms", MusicBoss = "music_boss",
            MusicHoliday = "music_holiday", MusicHaunted = "music_haunted", MusicSurvival = "music_survival",
            MusicAstral = "music_astral", MusicZLCity = "music_zl_city", MusicPier = "music_ls_pier", MusicRail = "music_ls_rail",
            MusicGardens = "music_ls_gardens", MusicAirfield = "music_airfield", MusicZombieBoss = "music_zombie_boss";

        /// <summary>Each Last Stand map's own loop (Hollow Block keeps the survival theme).</summary>
        public static string LastStandMusic(string mapFile) => mapFile == "pier" ? MusicPier : mapFile == "rail" ? MusicRail
                                                             : mapFile == "gardens" ? MusicGardens : MusicSurvival;
        public const int BarkCount = 4;

        /// <summary>Each hero's battle shout: one of the four bark clips and a pitch, fixed per hero id.</summary>
        public static (string clip, float pitch) BarkFor(string heroId)
        {
            if (string.IsNullOrEmpty(heroId)) return ("bark_0", 1f);
            int h = 17;
            foreach (char c in heroId) h = unchecked(h * 31 + c);
            h &= 0x7fffffff;
            return ("bark_" + (h % BarkCount), 0.85f + (h / BarkCount % 7) * 0.07f);
        }

        /// <summary>Plays a clip at an exact pitch (no jitter); used for the hero barks.</summary>
        public static void PlayAt(string name, float volume, float pitch)
        {
            if (Services.Audio == null) return;
            var clip = Clip(name);
            if (clip != null) Services.Audio.PlaySfx(clip, volume, pitch);
        }

        static readonly Dictionary<string, AudioClip> Cache = new Dictionary<string, AudioClip>();
        static float _lastTime;
        static string _lastName;

        public static AudioClip Clip(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (!Cache.TryGetValue(name, out var clip))
            {
                clip = Resources.Load<AudioClip>("Audio/" + name);
                Cache[name] = clip;
            }
            return clip;
        }

        /// <summary>Plays a sound effect with a little random pitch so repeats don't sound robotic.</summary>
        public static void Play(string name, float volume = 1f, float pitchJitter = 0.06f)
        {
            if (Services.Audio == null) return;
            // The same clip twice in one frame (e.g. a hit that also spawns a pickup) only plays once.
            if (name == _lastName && Time.unscaledTime - _lastTime < 0.02f) return;
            var clip = Clip(name);
            if (clip == null) return;
            _lastName = name;
            _lastTime = Time.unscaledTime;
            Services.Audio.PlaySfx(clip, volume, 1f + Random.Range(-pitchJitter, pitchJitter));
        }
    }
}

using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// The season's hidden collectible in a Story level (a Golden Acorn in Harvest, a Gift in Winterfest...). One per
    /// level per season. Found already this season: it shows as a faint outline and can't be taken again.
    /// </summary>
    public class SeasonToken : MonoBehaviour
    {
        SeasonState _state;
        string _levelId;
        bool _taken;
        float _age;
        Transform _art;

        public static SeasonToken Spawn(Transform parent, Vector2 at, SeasonState state, string levelId, bool alreadyFound, Material mat)
        {
            var go = new GameObject("SeasonToken") { layer = PKRLayers.Pickup };
            go.transform.SetParent(parent, false);
            go.transform.position = at;
            var t = go.AddComponent<SeasonToken>();
            t._state = state;
            t._levelId = levelId;
            t._taken = alreadyFound;
            var art = new GameObject("Art");
            art.transform.SetParent(go.transform, false);
            var sr = art.AddComponent<SpriteRenderer>();
            sr.sprite = SeasonArt.Token(state.Event);
            sr.sortingOrder = 16;
            if (mat != null) sr.sharedMaterial = mat;
            t._art = art.transform;
            if (alreadyFound)
            {
                sr.color = new Color(1f, 1f, 1f, 0.3f);
                return t;
            }
            GlowHalo.Attach(art.transform, new Color(1f, 0.85f, 0.45f, 0.6f), 1.9f, 16, mat);
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.5f;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            return t;
        }

        void Update()
        {
            if (_art == null) return;
            _age += Time.deltaTime;
            if (SettingsService.ReducedMotion) { _art.localPosition = Vector3.zero; return; }
            _art.localPosition = new Vector3(0f, Mathf.Sin(_age * 2.2f) * 0.12f, 0f);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (_taken || !TriggerUtil.IsPlayer(other, out _)) return;
            _taken = true;
            var save = Services.Save;
            int levels = SeasonDirector.StoryLevelCount;
            bool keepsake = false;
            if (save != null)
            {
                Seasons.AddToken(save.Data, _state, _levelId, levels, out keepsake);
                save.SaveNow();
            }
            int have = save != null ? Seasons.Tokens(save.Data, _state) : 1;
            int goal = Seasons.KeepsakeGoal(levels);
            string name = Seasons.TokenName(_state.Event).ToUpperInvariant();
            string detail = have < goal
                ? $"{have} of {goal} for the {Seasons.KeepsakeFor(_state.Event)?.name}  ·  one in every Story level"
                : $"{Plural.Of(have, Seasons.TokenName(_state.Event))} this {SeasonCalendar.Name(_state.Event)}";
            EventBus<ProgressToast>.Raise(new ProgressToast { title = name + " FOUND!", detail = detail, color = new Color32(255, 214, 90, 255) });
            if (keepsake)
            {
                var k = Seasons.KeepsakeFor(_state.Event);
                EventBus<ProgressToast>.Raise(new ProgressToast
                {
                    title = "KEEPSAKE: " + (k.HasValue ? k.Value.name.ToUpperInvariant() : "EARNED"),
                    detail = "Wear it from the season banner on the main menu",
                    color = new Color32(140, 230, 170, 255),
                });
            }
            Sfx.Play(SeasonAudio.ChimeFor(_state), 0.95f, 0f);
            if (Services.Haptics != null) Services.Haptics.Play(HapticStrength.Medium);
            var sr = _art != null ? _art.GetComponent<SpriteRenderer>() : null;
            PuffEffect.Play(transform.position, new Color32(255, 220, 120, 255), sr != null ? sr.sharedMaterial : null, 0.2f, 3.5f, 0.45f);
            gameObject.SetActive(false);
        }
    }

    /// <summary>Seasonal sounds and music (all synthesized by Tools/make_audio.py).</summary>
    public static class SeasonAudio
    {
        public const string Chime = "season_chime", Bells = "season_bells";
        public const string MusicHarvest = "music_harvest", MusicWinterfest = "music_winterfest";

        public static string ChimeFor(SeasonState s) => s.Look == SeasonLook.Winter ? Bells : Chime;

        /// <summary>The menus play the season's own tune while a season is live.</summary>
        public static string MenuMusic(SeasonState s) =>
            !s.Active ? null : s.Look == SeasonLook.Winter ? MusicWinterfest : MusicHarvest;
    }
}

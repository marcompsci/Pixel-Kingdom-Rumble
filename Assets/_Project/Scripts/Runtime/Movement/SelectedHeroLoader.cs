using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Put on a scene's baked player: at Start, swaps in the hero chosen in Character Select
    /// (if it differs from the baked one and is selectable), and puts on the hero's equipped palette.
    /// Runs before the level flow and HUD start.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class SelectedHeroLoader : MonoBehaviour
    {
        [SerializeField] CharacterRoster roster;

        public CharacterRoster Roster { get => roster; set => roster = value; }

        void Start()
        {
            if (roster == null) return;
            var motor = GetComponent<PlatformerMotor2D>();
            var def = roster.Find(GameSession.SelectedCharacterId);
            if (def != null && roster.IsSelectable(def) && (motor == null || motor.Definition != def))
                HeroLoadout.Apply(gameObject, def);
            var worn = motor != null ? motor.Definition : def;
            if (worn != null) HeroLoadout.ApplyTint(gameObject, roster.TintFor(worn.id));
        }
    }
}

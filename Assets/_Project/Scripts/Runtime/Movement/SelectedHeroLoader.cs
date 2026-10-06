using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Put on a scene's baked player: at Start, swaps in the hero chosen in Character Select
    /// (if it differs from the baked one and is selectable). Runs before the level flow and HUD start.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class SelectedHeroLoader : MonoBehaviour
    {
        [SerializeField] CharacterRoster roster;

        public CharacterRoster Roster { get => roster; set => roster = value; }

        void Start()
        {
            if (roster == null) return;
            var def = roster.Find(GameSession.SelectedCharacterId);
            if (def == null || !roster.IsSelectable(def)) return;
            var motor = GetComponent<PlatformerMotor2D>();
            if (motor != null && motor.Definition == def) return;
            HeroLoadout.Apply(gameObject, def);
        }
    }
}

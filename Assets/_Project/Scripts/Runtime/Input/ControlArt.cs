using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Generated on-screen control art (Tools/art/controls.py -> Resources/PKR/UI): dark bevelled buttons, a round
    /// D-pad base, a dotted knob, square HUD buttons and white icons. Every getter can return null (fallback: the old
    /// plain circles), so the controls still work if the art is missing.
    /// </summary>
    public static class ControlArt
    {
        static Sprite Load(string name) => Resources.Load<Sprite>("PKR/UI/" + name);

        public static Sprite Button => Load("ctl_button");
        public static Sprite ButtonPressed => Load("ctl_button_pressed");
        public static Sprite Square => Load("ctl_square");
        public static Sprite PadBase => Load("ctl_pad_base");
        public static Sprite PadKnob => Load("ctl_pad_knob");
        public static Sprite PadGlow => Load("ctl_pad_glow");

        /// <summary>Icon for an action control id (jump, attack, special, dodge) or a HUD icon (pause, sound, mute, menu).</summary>
        public static Sprite Icon(string id) => Load("icon_" + id);

        public static bool Available => Button != null;
    }
}

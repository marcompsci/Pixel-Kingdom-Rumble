using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Developer overlay for tuning movement (IMGUI, top-left). Shows motor/ability state and offers
    /// quick toggles for touch-layout edit mode, high contrast, haptics and layout reset until the
    /// real Settings screen lands (increment 6). Not included in Story/Arena scenes.
    /// </summary>
    public class SandboxDebugPanel : MonoBehaviour
    {
        public PlatformerMotor2D motor;
        public bool visible = true;

        HeroAbilities _abilities;
        AttackRunner _attacks;
        TouchControlsUI _touch;
        GUIStyle _box, _btn;
        float _fps, _fpsTimer;
        int _frames;

        void Start()
        {
            if (motor != null) _abilities = motor.GetComponent<HeroAbilities>();
            if (motor != null) _attacks = motor.GetComponent<AttackRunner>();
            _touch = Object.FindAnyObjectByType<TouchControlsUI>();
        }

        void Update()
        {
            _frames++;
            _fpsTimer += Time.unscaledDeltaTime;
            if (_fpsTimer >= 0.5f) { _fps = _frames / _fpsTimer; _frames = 0; _fpsTimer = 0f; }
        }

        void OnGUI()
        {
            float s = Mathf.Max(1f, Screen.height / 720f);
            if (_box == null)
            {
                _box = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft };
                _btn = new GUIStyle(GUI.skin.button);
            }
            _box.fontSize = Mathf.RoundToInt(13 * s);
            _btn.fontSize = Mathf.RoundToInt(13 * s);

            var safe = Screen.safeArea;
            float x = safe.x + 8 * s;
            float y = Screen.height - safe.yMax + 8 * s; // IMGUI y is top-down
            float w = 250 * s, h = 26 * s;

            if (GUI.Button(new Rect(x, y, 90 * s, h), visible ? "Hide dbg" : "Debug", _btn)) visible = !visible;
            if (!visible || motor == null) return;
            y += h + 4 * s;

            var v = motor.Velocity;
            string text =
                $"FPS {_fps:0}\n" +
                $"Vel ({v.x:0.0}, {v.y:0.0})\n" +
                $"Grounded {motor.IsGrounded}  Facing {motor.Facing}\n" +
                $"Ability {(_abilities != null ? _abilities.Current.ToString() : "-")}\n" +
                $"Locked {motor.IsControlLocked}\n" +
                $"Attack {(_attacks != null && _attacks.Current != null ? _attacks.Current.displayName + " f" + _attacks.Frame : "-")}";
            GUI.Box(new Rect(x, y, w, 128 * s), text, _box);
            y += 132 * s;

            var settings = Services.Settings;
            if (_touch != null && GUI.Button(new Rect(x, y, w, h), _touch.EditMode ? "Done editing controls" : "Edit touch layout", _btn))
                _touch.SetEditMode(!_touch.EditMode);
            y += h + 2 * s;
            if (settings == null) return;
            if (GUI.Button(new Rect(x, y, w, h), $"High contrast: {(settings.Data.highContrastUI ? "ON" : "off")}", _btn))
                settings.SetHighContrast(!settings.Data.highContrastUI);
            y += h + 2 * s;
            if (GUI.Button(new Rect(x, y, w, h), $"Haptics: {(settings.Data.hapticsEnabled ? "ON" : "off")}", _btn))
                settings.SetHaptics(!settings.Data.hapticsEnabled);
            y += h + 2 * s;
            if (GUI.Button(new Rect(x, y, w, h), $"Floating stick: {(settings.Data.floatingJoystick ? "ON" : "off")}", _btn))
            {
                settings.Data.floatingJoystick = !settings.Data.floatingJoystick;
                settings.Commit();
            }
            y += h + 2 * s;
            if (GUI.Button(new Rect(x, y, w, h), "Reset touch layout", _btn))
                settings.ResetControlLayout();
        }
    }
}

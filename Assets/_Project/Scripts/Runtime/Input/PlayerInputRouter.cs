using PKR.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PKR
{
    /// <summary>
    /// Merges keyboard, gamepad and on-screen touch controls into the fighter's FighterIntent.
    /// Actions are defined in code (no .inputactions asset) so bindings can't break from missing references.
    ///
    /// Editor / desktop testing:
    ///   Move: WASD or arrows   Jump: Space / K      Attack: J      Special: L / I      Dodge: Left Shift / U
    ///   Pause: Escape / P      Gamepad: left stick, South=Jump, West=Attack, North=Special, East/RB=Dodge, Start=Pause
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class PlayerInputRouter : MonoBehaviour
    {
        [SerializeField] PlatformerMotor2D motor;
        [Tooltip("Optional. Found in the scene automatically if empty.")]
        [SerializeField] TouchControlsUI touchControls;

        InputAction _move, _jump, _attack, _special, _dodge, _pause;

        public FighterIntent Intent => motor != null ? motor.Intent : null;

        void Awake()
        {
            if (motor == null) motor = GetComponent<PlatformerMotor2D>();
            if (touchControls == null) touchControls = Object.FindAnyObjectByType<TouchControlsUI>();

            _move = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
            _move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            _move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            _move.AddBinding("<Gamepad>/leftStick");
            _move.AddBinding("<Gamepad>/dpad");

            _jump = Button("Jump", "<Keyboard>/space", "<Keyboard>/k", "<Gamepad>/buttonSouth");
            _attack = Button("Attack", "<Keyboard>/j", "<Gamepad>/buttonWest");
            _special = Button("Special", "<Keyboard>/l", "<Keyboard>/i", "<Gamepad>/buttonNorth");
            _dodge = Button("Dodge", "<Keyboard>/leftShift", "<Keyboard>/u", "<Gamepad>/buttonEast", "<Gamepad>/rightShoulder");
            _pause = Button("Pause", "<Keyboard>/escape", "<Keyboard>/p", "<Gamepad>/start");
        }

        static InputAction Button(string name, params string[] bindings)
        {
            var a = new InputAction(name, InputActionType.Button);
            foreach (var b in bindings) a.AddBinding(b);
            return a;
        }

        void OnEnable()
        {
            _move.Enable(); _jump.Enable(); _attack.Enable(); _special.Enable(); _dodge.Enable(); _pause.Enable();
        }

        void OnDisable()
        {
            _move.Disable(); _jump.Disable(); _attack.Disable(); _special.Disable(); _dodge.Disable(); _pause.Disable();
            if (motor != null) motor.Intent.ClearAll();
        }

        void OnDestroy()
        {
            _move?.Dispose(); _jump?.Dispose(); _attack?.Dispose(); _special?.Dispose(); _dodge?.Dispose(); _pause?.Dispose();
        }

        void Update()
        {
            var state = Services.State;
            if (_pause.WasPressedThisFrame() && state != null) state.TogglePause();
            if (motor == null) return;

            var intent = motor.Intent;
            if (state != null && state.IsPaused)
            {
                intent.ClearAll();
                return;
            }

            bool touch = touchControls != null && touchControls.Visible && !touchControls.EditMode;
            Vector2 keys = _move.ReadValue<Vector2>();
            Vector2 stick = touch ? touchControls.StickValue : Vector2.zero;
            intent.Move = Vector2.ClampMagnitude(stick.sqrMagnitude > keys.sqrMagnitude ? stick : keys, 1f);

            float now = Time.time;
            Route(intent.Jump, _jump, ControlIds.Jump, touch, now);
            Route(intent.Attack, _attack, ControlIds.Attack, touch, now);
            Route(intent.Special, _special, ControlIds.Special, touch, now);
            Route(intent.Dodge, _dodge, ControlIds.Dodge, touch, now);
        }

        void Route(ActionBuffer buffer, InputAction action, string touchId, bool touch, float now)
        {
            bool pressed = action.WasPressedThisFrame();
            bool held = action.IsPressed();
            if (touch)
            {
                pressed |= touchControls.ConsumePressed(touchId);
                held |= touchControls.IsHeld(touchId);
            }
            if (pressed) buffer.Press(now);
            buffer.SetHeld(held);
        }
    }
}

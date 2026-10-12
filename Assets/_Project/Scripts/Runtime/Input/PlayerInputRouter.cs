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

        InputActionMap _map;
        InputAction _move, _jump, _attack, _special, _dodge, _pause;
        bool _devicesOff;

        public FighterIntent Intent => motor != null ? motor.Intent : null;
        /// <summary>The touch overlay this fighter reads (the scene's one unless Bind gave another).</summary>
        public TouchControlsUI Touch => touchControls;
        /// <summary>Ignore move/jump/attack input (round intros in Versus) while pause still works.</summary>
        public bool GameplayLocked { get; set; }
        /// <summary>Couch play: "P1" / "P2" (null in single player).</summary>
        public string PlayerTag { get; private set; }

        void Awake()
        {
            if (motor == null) motor = GetComponent<PlatformerMotor2D>();
            if (touchControls == null) touchControls = Object.FindAnyObjectByType<TouchControlsUI>();

            _map = new InputActionMap("Player");
            _move = _map.AddAction("Move", InputActionType.Value, expectedControlLayout: "Vector2");
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

        InputAction Button(string name, params string[] bindings)
        {
            var a = _map.AddAction(name, InputActionType.Button);
            foreach (var b in bindings) a.AddBinding(b);
            return a;
        }

        /// <summary>
        /// Couch play: limit this fighter to some devices and (optionally) its own touch controls.
        /// devices: null = every device (single player); an empty array = touch only.
        /// </summary>
        public void Bind(InputDevice[] devices, TouchControlsUI touch, string playerTag)
        {
            bool wasEnabled = _map.enabled;
            _map.Disable();
            _devicesOff = devices != null && devices.Length == 0;
            if (devices == null || devices.Length == 0) _map.devices = null;
            else _map.devices = devices;
            touchControls = touch;
            PlayerTag = playerTag;
            if (wasEnabled && !_devicesOff) _map.Enable();
        }

        void OnEnable()
        {
            if (!_devicesOff) _map.Enable();
        }

        void OnDisable()
        {
            _map.Disable();
            if (motor != null) motor.Intent.ClearAll();
        }

        void OnDestroy() => _map?.Dispose();

        void Update()
        {
            var state = Services.State;
            if (!_devicesOff && _pause.WasPressedThisFrame() && state != null) state.TogglePause();
            if (motor == null) return;

            var intent = motor.Intent;
            if ((state != null && state.IsPaused) || GameplayLocked)
            {
                intent.ClearAll();
                return;
            }

            bool touch = touchControls != null && touchControls.Visible && !touchControls.EditMode;
            Vector2 keys = _devicesOff ? Vector2.zero : _move.ReadValue<Vector2>();
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
            bool pressed = !_devicesOff && action.WasPressedThisFrame();
            bool held = !_devicesOff && action.IsPressed();
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

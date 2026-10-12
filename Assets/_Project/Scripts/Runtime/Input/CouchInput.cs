using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace PKR
{
    /// <summary>
    /// Who controls what in couch (2-player) matches on one device:
    ///   two controllers  -> P1 controller 1 (+ keyboard and the normal touch controls), P2 controller 2
    ///   one controller   -> P1 touch (+ keyboard), P2 the controller
    ///   no controllers   -> split screen touch: P1 left half, P2 right half
    /// </summary>
    public static class CouchInput
    {
        public struct Plan
        {
            public InputDevice[] p1Devices;  // null = all devices, empty = touch only
            public InputDevice[] p2Devices;
            public bool splitTouch;
            public string description;
        }

        public static int ControllerCount => Gamepad.all.Count;

        public static Plan Make()
        {
            var pads = Gamepad.all;
            var kb = Keyboard.current;
            var plan = new Plan();
            if (pads.Count >= 2)
            {
                plan.p1Devices = With(kb, pads[0]);
                plan.p2Devices = new InputDevice[] { pads[1] };
                plan.description = "P1 and P2 each use a controller.";
            }
            else if (pads.Count == 1)
            {
                plan.p1Devices = With(kb, null);
                plan.p2Devices = new InputDevice[] { pads[0] };
                plan.description = "P1 uses touch, P2 uses the controller.";
            }
            else
            {
                plan.p1Devices = With(kb, null);
                plan.p2Devices = new InputDevice[0];
                plan.splitTouch = true;
                plan.description = "Split screen: P1 plays on the left half, P2 on the right. Connect a controller for more room.";
            }
            return plan;
        }

        static InputDevice[] With(InputDevice a, InputDevice b)
        {
            var list = new List<InputDevice>();
            if (a != null) list.Add(a);
            if (b != null) list.Add(b);
            return list.ToArray();
        }
    }
}

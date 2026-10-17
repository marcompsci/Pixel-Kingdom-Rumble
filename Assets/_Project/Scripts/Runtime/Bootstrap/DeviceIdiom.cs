using System.Runtime.InteropServices;
using UnityEngine;

namespace PKR
{
    /// <summary>Device facts: iPad idiom, thermal state, Low Power Mode (all false/nominal in the Editor).</summary>
    public static class DeviceIdiom
    {
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] static extern bool PKR_IsPadIdiom();
        [DllImport("__Internal")] static extern int PKR_ThermalState();
        [DllImport("__Internal")] static extern bool PKR_LowPowerMode();
#endif
        static bool? _pad;

        public static bool IsPad
        {
            get
            {
                if (_pad.HasValue) return _pad.Value;
#if UNITY_IOS && !UNITY_EDITOR
                _pad = PKR_IsPadIdiom();
#else
                _pad = false;
#endif
                return _pad.Value;
            }
        }

        /// <summary>How hot the phone is (always nominal off-device; tests can set <see cref="FakeThermal"/>).</summary>
        public static PKR.Core.ThermalState Thermal
        {
            get
            {
                if (FakeThermal.HasValue) return FakeThermal.Value;
#if UNITY_IOS && !UNITY_EDITOR
                return (PKR.Core.ThermalState)Mathf.Clamp(PKR_ThermalState(), 0, 3);
#else
                return PKR.Core.ThermalState.Nominal;
#endif
            }
        }

        public static PKR.Core.ThermalState? FakeThermal;

        public static bool LowPowerMode
        {
            get
            {
#if UNITY_IOS && !UNITY_EDITOR
                return PKR_LowPowerMode();
#else
                return false;
#endif
            }
        }
    }
}

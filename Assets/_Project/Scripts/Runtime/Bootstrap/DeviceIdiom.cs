using System.Runtime.InteropServices;
using UnityEngine;

namespace PKR
{
    /// <summary>Is this running as an iPad app (UIUserInterfaceIdiomPad)? False everywhere else, and in the Editor.</summary>
    public static class DeviceIdiom
    {
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] static extern bool PKR_IsPadIdiom();
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
    }
}

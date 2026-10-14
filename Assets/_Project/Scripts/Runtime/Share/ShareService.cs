using System.Runtime.InteropServices;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Opens the iOS share sheet with text and/or an image file (Assets/Plugins/iOS/PKRShare.mm). In the Editor and on
    /// other platforms the text goes to the clipboard instead, so the flow can be tested anywhere.
    /// </summary>
    public static class ShareService
    {
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void PKR_Share(string text, string imagePath);
#endif

        /// <summary>The last text handed to Share (tests and the Editor log read it).</summary>
        public static string LastText { get; private set; }
        public static string LastImagePath { get; private set; }

        public static void Share(string text, string imagePath = null)
        {
            LastText = text;
            LastImagePath = imagePath;
#if UNITY_IOS && !UNITY_EDITOR
            PKR_Share(text ?? "", imagePath ?? "");
#else
            if (!string.IsNullOrEmpty(text)) GUIUtility.systemCopyBuffer = text;
            Debug.Log($"[PKR Share] {(string.IsNullOrEmpty(imagePath) ? "" : imagePath + "\n")}{text}");
#endif
        }
    }
}

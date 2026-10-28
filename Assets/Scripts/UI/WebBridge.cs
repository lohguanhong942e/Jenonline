using System.Runtime.InteropServices;
using UnityEngine;

namespace Vesperhold
{
    // Browser features Unity does not expose: vibration, "is this a phone browser", fullscreen.
    // The JavaScript lives in Assets/Plugins/WebGL/Vesperhold.jslib. Everywhere else these are harmless no-ops.
    public static class WebBridge
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void WebVibrate(int ms);
        [DllImport("__Internal")] static extern int WebIsMobileBrowser();
        [DllImport("__Internal")] static extern void WebToggleFullscreen();
        static int mobile = -1;
        public static bool IsMobileBrowser { get { if (mobile < 0) mobile = WebIsMobileBrowser(); return mobile == 1; } }
        public static void Vibrate(int ms) { WebVibrate(ms); }
        public static void ToggleFullscreen() { WebToggleFullscreen(); }
#else
        public static bool IsMobileBrowser { get { return false; } }
        public static void Vibrate(int ms) { }
        public static void ToggleFullscreen() { }
#endif
    }
}

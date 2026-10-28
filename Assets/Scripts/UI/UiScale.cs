using UnityEngine;

namespace Vesperhold
{
    // Phones have tiny pixels, so every IMGUI screen is drawn in a virtual resolution (about 600 units tall on
    // a phone, 1000 on a desktop monitor) and scaled up. Safe-area insets keep the HUD clear of notches and
    // rounded corners. Use UiScale.W/H instead of Screen.width/height when laying out UI.
    public static class UiScale
    {
        public static bool Mobile { get { return Application.isMobilePlatform || WebBridge.IsMobileBrowser || TouchInput.Active; } }
        public static float Scale { get { float h = Screen.height; return Mobile ? Mathf.Max(1f, h / 600f) : Mathf.Max(1f, h / 1000f); } }
        public static float W { get { return Screen.width / Scale; } }
        public static float H { get { return Screen.height / Scale; } }
        // Safe-area insets in virtual units (left, right, top, bottom).
        public static float L { get { return Screen.safeArea.x / Scale; } }
        public static float R { get { return (Screen.width - Screen.safeArea.xMax) / Scale; } }
        public static float B { get { return Screen.safeArea.y / Scale; } }
        public static float T { get { return (Screen.height - Screen.safeArea.yMax) / Scale; } }
        public static bool Portrait { get { return Screen.height > Screen.width; } }

        public static void Begin() { float s = Scale; GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f)); }
        public static void End() { GUI.matrix = Matrix4x4.identity; }

        // Finger-drag scrolling for IMGUI scroll views (the thin scrollbars are hard to grab on a phone).
        public static void DragScroll(Rect view, ref Vector2 scroll)
        {
            var e = Event.current;
            if (e.type == EventType.MouseDrag && view.Contains(e.mousePosition)) { scroll.y -= e.delta.y; scroll.x -= e.delta.x; }
        }
    }

    // Short vibration on a hit or a boss roar (phones only), limited so it never buzzes continuously.
    public static class Haptics
    {
        public static bool On = true;
        static float last;
        public static void Tap()
        {
            if (!On || !UiScale.Mobile || Time.unscaledTime - last < 0.6f) return;
            last = Time.unscaledTime;
#if UNITY_WEBGL
            WebBridge.Vibrate(45);               // iPhone Safari ignores this; Android browsers buzz
#elif UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }
    }

    // Quality knobs that scale down on phones and in battery-saver mode.
    public static class Perf
    {
        static bool batterySaver;
        public static float ParticleScale { get { return UiScale.Mobile ? 0.55f : 1f; } }
        public static bool BatterySaver
        {
            get { return batterySaver; }
            set { batterySaver = value; Application.targetFrameRate = value ? 30 : 60; }
        }
    }
}

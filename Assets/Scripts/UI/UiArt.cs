using System.Collections.Generic;
using UnityEngine;

namespace Vesperhold
{
    // Painted UI art from Resources (Art/UI, Art/Portraits, Art/Props). Every lookup returns null when the file is
    // missing, and every caller falls back to the old flat drawing, so the game still works without the art.
    public static class UiArt
    {
        static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();
        static GUIStyle button;

        public static Texture2D Tex(string name) { return Any("Art/UI/" + name); }
        public static Texture2D Portrait(string name) { return Any("Art/Portraits/" + name); }
        public static Texture2D Any(string path)
        {
            Texture2D t;
            if (!cache.TryGetValue(path, out t))
            {
                t = Resources.Load<Texture2D>(path);
                if (t != null) t.filterMode = FilterMode.Bilinear;
                cache[path] = t;
            }
            return t;
        }

        // Draw keeping the picture's aspect ratio inside r. Returns false if the art is missing.
        public static bool Draw(Rect r, Texture2D t, float alpha = 1f)
        {
            if (t == null) return false;
            var c = GUI.color; GUI.color = new Color(c.r, c.g, c.b, c.a * alpha);
            GUI.DrawTexture(r, t, ScaleMode.ScaleToFit);
            GUI.color = c; return true;
        }

        // Stretches a frame texture over r (used for windows and the boss bar).
        public static bool Stretch(Rect r, Texture2D t)
        {
            if (t == null) return false;
            GUI.DrawTexture(r, t, ScaleMode.StretchToFill);
            return true;
        }

        // Status bar: ornate frame behind, coloured fill inside its dark track, optional label on top.
        public static void Bar(Rect r, float f, Color fill, string label)
        {
            var frame = Tex("bar_frame");
            if (frame != null)
            {
                float fw = r.width / 0.85f, fh = r.height / 0.4f;
                GUI.DrawTexture(new Rect(r.x - 0.075f * fw, r.y - 0.3f * fh, fw, fh), frame, ScaleMode.StretchToFill);
            }
            else { GUI.color = Color.black; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = Color.white; }
            GUI.color = fill; GUI.DrawTexture(new Rect(r.x + 1, r.y + 1, (r.width - 2) * Mathf.Clamp01(f), r.height - 2), Texture2D.whiteTexture);
            GUI.color = new Color(1, 1, 1, 0.18f); GUI.DrawTexture(new Rect(r.x + 1, r.y + 1, (r.width - 2) * Mathf.Clamp01(f), (r.height - 2) * 0.4f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            if (!string.IsNullOrEmpty(label)) GUI.Label(r, label, new GUIStyle(GUI.skin.label) { fontSize = 12, alignment = TextAnchor.MiddleCenter });
        }

        // Tab button: gold glowing plate when selected, dull plate otherwise.
        public static GUIStyle Tab(bool active, int fontSize = 15)
        {
            var t = Tex(active ? "tab_active" : "tab_inactive");
            var st = new GUIStyle(GUI.skin.button) { fontSize = fontSize, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            if (t != null)
            {
                st.normal.background = t; st.hover.background = t; st.active.background = t; st.focused.background = t;
                st.border = new RectOffset(70, 70, 30, 30);
                st.normal.textColor = active ? new Color(1f, 0.92f, 0.65f) : new Color(0.7f, 0.68f, 0.78f); st.hover.textColor = Color.white; st.active.textColor = Color.white;
            }
            return st;
        }

        // Checkbox with the painted box. Returns the new value.
        public static bool Check(Rect r, bool value, string label)
        {
            var box = Tex(value ? "check_on" : "check_off");
            float s = Mathf.Min(28f, r.height);
            if (box != null) GUI.DrawTexture(new Rect(r.x, r.y, s, s), box, ScaleMode.ScaleToFit);
            else { GUI.Box(new Rect(r.x, r.y, s, s), value ? "x" : ""); }
            GUI.Label(new Rect(r.x + s + 8, r.y - 2, r.width - s - 8, r.height + 4), label);
            if (GUI.Button(r, GUIContent.none, GUIStyle.none)) { value = !value; Sfx.Play("click", 0.4f); }
            return value;
        }

        // Slider with a painted track and knob (the invisible IMGUI slider on top handles the input).
        public static float Slider(Rect r, float value, float min, float max)
        {
            float v = GUI.HorizontalSlider(r, value, min, max, GUIStyle.none, GUIStyle.none);
            var track = Tex("slider_track"); var knob = Tex("slider_knob");
            float t = Mathf.InverseLerp(min, max, v), ks = 30f;
            if (track != null) GUI.DrawTexture(new Rect(r.x, r.y + r.height / 2f - 9f, r.width, 18f), track, ScaleMode.StretchToFill);
            else { GUI.color = new Color(0.2f, 0.2f, 0.3f); GUI.DrawTexture(new Rect(r.x, r.y + r.height / 2f - 3f, r.width, 6f), Texture2D.whiteTexture); GUI.color = Color.white; }
            if (knob != null) GUI.DrawTexture(new Rect(r.x + t * (r.width - ks), r.y + r.height / 2f - ks / 2f, ks, ks), knob, ScaleMode.ScaleToFit);
            return v;
        }

        // Round painted icon button (close, plus). Returns true when clicked.
        public static bool IconButton(Rect r, string tex, string fallback)
        {
            var t = Tex(tex);
            if (t != null) { GUI.DrawTexture(r, t, ScaleMode.ScaleToFit); return GUI.Button(r, GUIContent.none, GUIStyle.none); }
            return GUI.Button(r, fallback);
        }

        // Ornate button plate as a nine-slice IMGUI style, or a plain button if the art is missing.
        public static GUIStyle Button(int fontSize)
        {
            if (button == null || button.fontSize != fontSize)
            {
                button = new GUIStyle(GUI.skin.button) { fontSize = fontSize, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                var plate = Tex("button_plate");
                if (plate != null)
                {
                    button.normal.background = plate; button.hover.background = plate; button.active.background = plate; button.focused.background = plate;
                    button.border = new RectOffset(70, 70, 40, 40);
                    button.padding = new RectOffset(16, 16, 8, 8);
                    button.normal.textColor = new Color(0.95f, 0.82f, 0.5f); button.hover.textColor = Color.white; button.active.textColor = new Color(1f, 0.7f, 0.3f);
                }
            }
            return button;
        }
    }
}

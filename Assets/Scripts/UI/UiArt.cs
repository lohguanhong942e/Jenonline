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

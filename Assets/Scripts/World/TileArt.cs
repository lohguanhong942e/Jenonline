using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Vesperhold
{
    // Procedural 16x16 pixel tiles and room backdrops so every theme looks distinct without painted art.
    // A painted PNG in Art/Backdrops/<theme> still wins over the generated backdrop.
    public static class TileArt
    {
        static readonly Dictionary<string, Tile[]> tileCache = new Dictionary<string, Tile[]>();
        static readonly Dictionary<string, Texture2D> backCache = new Dictionary<string, Texture2D>();

        static uint Hash(int x, int y, int s)
        {
            uint h = (uint)(x * 374761393 + y * 668265263 + s * 144665);
            h = (h ^ (h >> 13)) * 1274126177u; return h ^ (h >> 16);
        }
        static float H01(int x, int y, int s) { return (Hash(x, y, s) & 1023) / 1023f; }

        static Tile MakeTile(Func<int, int, Color> px, bool grid)
        {
            var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var cols = new Color32[256];
            for (int y = 0; y < 16; y++) for (int x = 0; x < 16; x++) cols[y * 16 + x] = px(x, 15 - y);
            tex.SetPixels32(cols); tex.Apply();
            var t = ScriptableObject.CreateInstance<Tile>();
            t.sprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16f);
            t.colliderType = Tile.ColliderType.Grid;
            return t;
        }

        static Color Shade(Color c, float k) { return new Color(Mathf.Clamp01(c.r * k), Mathf.Clamp01(c.g * k), Mathf.Clamp01(c.b * k), 1f); }

        // Four brick variants, each with a normal and a "lit top edge" version: index = variant * 2 + (top ? 1 : 0).
        public static Tile Solid(string theme, int variant, bool top)
        {
            string key = "solid_" + theme; Tile[] set;
            if (!tileCache.TryGetValue(key, out set))
            {
                var pal = Palette.For(theme); set = new Tile[8];
                for (int v = 0; v < 4; v++) for (int tp = 0; tp < 2; tp++)
                {
                    int vv = v; bool isTop = tp == 1;
                    set[v * 2 + tp] = MakeTile((x, y) =>
                    {
                        int row = y / 8, off = row % 2 == 0 ? 0 : 4; int bx = (x + off) % 16, bi = (x + off) / 8;
                        bool mortar = y % 8 == 7 || bx % 8 == 7;
                        float n = 0.88f + H01(bi, row, vv + 3) * 0.24f + (H01(x, y, vv) - 0.5f) * 0.07f;
                        float edge = (y % 8 == 0 ? 1.1f : 1f) * (bx % 8 == 0 ? 1.06f : 1f);
                        var c = Shade(pal.Solid, mortar ? 0.62f : n * edge);
                        if (isTop && y < 3) c = Color.Lerp(c, pal.Platform, y == 0 ? 0.85f : y == 1 ? 0.5f : 0.2f);
                        return c;
                    }, true);
                }
                tileCache[key] = set;
            }
            return set[Mathf.Clamp(variant, 0, 3) * 2 + (top ? 1 : 0)];
        }

        public static Tile Platform(string theme)
        {
            string key = "plat_" + theme; Tile[] set;
            if (!tileCache.TryGetValue(key, out set))
            {
                var pal = Palette.For(theme);
                set = new[] { MakeTile((x, y) =>
                {
                    if (y > 5) return new Color(0, 0, 0, 0);
                    bool gap = x % 8 == 0; float n = 0.9f + H01(x / 8, y / 3, 9) * 0.2f;
                    var c = Shade(pal.Platform, gap ? 0.6f : n * (y == 0 ? 1.25f : y == 5 ? 0.7f : 1f)); return c;
                }, true) };
                tileCache[key] = set;
            }
            return set[0];
        }

        public static Tile Spike(string theme)
        {
            string key = "spike_" + theme; Tile[] set;
            if (!tileCache.TryGetValue(key, out set))
            {
                var pal = Palette.For(theme);
                set = new[] { MakeTile((x, y) =>
                {
                    int px = x % 8; float half = (8 - y) * 0.5f;          // triangles 8 wide, 8 tall at the bottom of the cell
                    bool inside = y >= 8 && Mathf.Abs(px - 3.5f) <= (y - 7) * 0.5f;
                    if (!inside) return new Color(0, 0, 0, 0);
                    return Shade(pal.Spike, 0.75f + (px < 4 ? 0.3f : 0f) + (15 - y) * 0.02f);
                }, true) };
                tileCache[key] = set;
            }
            return set[0];
        }

        // ---- backdrops ----
        public static Texture2D Backdrop(string theme)
        {
            Texture2D t;
            if (backCache.TryGetValue(theme, out t)) return t;
            const int W = 384, Hh = 192;
            var pal = Palette.For(theme); var px = new Color[W * Hh];
            var rnd = new System.Random(theme.GetHashCode() & 0xffff);
            Color top = Color.Lerp(pal.Back, pal.Platform, 0.22f), bottom = Shade(pal.Back, 0.55f);
            for (int y = 0; y < Hh; y++) for (int x = 0; x < W; x++) px[y * W + x] = Color.Lerp(bottom, top, y / (float)Hh);

            Action<int, int, int, int, Color> rect = (x0, y0, w, h, c) =>
            {
                for (int y = Mathf.Max(0, y0); y < Mathf.Min(Hh, y0 + h); y++) for (int x = Mathf.Max(0, x0); x < Mathf.Min(W, x0 + w); x++) px[y * W + x] = c;
            };
            Action<int, int, int, Color> disc = (cx, cy, r, c) =>
            {
                for (int y = cy - r; y <= cy + r; y++) for (int x = cx - r; x <= cx + r; x++)
                    if (x >= 0 && y >= 0 && x < W && y < Hh && (x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r) px[y * W + x] = c;
            };
            Color dark = Shade(pal.Back, 0.38f), mid = Shade(pal.Back, 0.62f), glow = Color.Lerp(pal.Spike, pal.Platform, 0.4f);

            if (theme == "gate" || theme == "tower")
            {
                for (int i = 0; i < 90; i++) rect(rnd.Next(W), 80 + rnd.Next(Hh - 80), 1, 1, Color.Lerp(top, Color.white, 0.6f));
                if (theme == "gate") disc(300, 150, 22, Color.Lerp(Color.white, glow, 0.25f));
                for (int i = 0; i < 9; i++)
                {
                    int bx = i * 46 + rnd.Next(10), bw = 22 + rnd.Next(18), bh = 50 + rnd.Next(60);
                    rect(bx, 0, bw, bh, i % 2 == 0 ? dark : mid); rect(bx - 2, bh, bw + 4, 4, i % 2 == 0 ? dark : mid);
                    for (int m = 0; m < bw; m += 6) rect(bx + m, bh + 4, 3, 5, i % 2 == 0 ? dark : mid);
                    rect(bx + bw / 2 - 1, bh - 22, 3, 9, Color.Lerp(glow, Color.white, 0.2f));
                }
                if (theme == "tower") for (int g = 0; g < 4; g++) { int cx = 40 + g * 100, cy = 70 + rnd.Next(70); disc(cx, cy, 26, mid); disc(cx, cy, 20, dark); disc(cx, cy, 5, glow); }
            }
            else if (theme == "library")
            {
                for (int s = 0; s < 4; s++) for (int x = 0; x < W; x += 5)
                {
                    int sh = 30 + rnd.Next(14); var c = Color.HSVToRGB((float)rnd.NextDouble(), 0.35f, 0.38f);
                    rect(x, s * 46 + 6, 4, sh, Color.Lerp(c, pal.Back, 0.35f));
                }
                for (int s = 0; s < 4; s++) rect(0, s * 46 + 4, W, 3, Shade(pal.Platform, 0.7f));
            }
            else if (theme == "under")
            {
                for (int i = 0; i < 6; i++) { int cx = 30 + i * 66; rect(cx, 0, 16, Hh, mid); rect(cx - 3, 0, 22, 8, dark); rect(cx - 3, Hh - 14, 22, 14, dark); }
                for (int y = 0; y < 50; y += 5) rect(0, y, W, 2, Color.Lerp(pal.Platform, pal.Back, 0.5f));
            }
            else
            {
                // arched hall: columns with windows glowing in the theme colour (hall, chapel, catacomb, armory, throne)
                int cols = 6, cw = W / cols;
                for (int i = 0; i < cols; i++)
                {
                    int x0 = i * cw; rect(x0 + 2, 0, 12, Hh, mid); rect(x0, 0, 16, 10, dark); rect(x0, Hh - 16, 16, 16, dark);
                    int wx = x0 + cw / 2 - 14; bool glowing = theme != "catacomb";
                    rect(wx, 70, 28, 70, dark); disc(wx + 14, 140, 14, dark);
                    if (glowing) { rect(wx + 4, 74, 20, 64, Color.Lerp(glow, pal.Back, 0.3f)); disc(wx + 14, 140, 10, Color.Lerp(glow, pal.Back, 0.3f)); rect(wx + 13, 74, 2, 76, dark); }
                    if (theme == "throne" || theme == "armory") { rect(x0 + cw / 2 - 5, 100, 10, 60, Color.Lerp(pal.Platform, dark, 0.45f)); }
                }
            }
            // fade the bottom so the floor reads as a distinct layer
            for (int y = 0; y < 30; y++) for (int x = 0; x < W; x++) px[y * W + x] = Color.Lerp(px[y * W + x], Shade(pal.Back, 0.3f), 1f - y / 30f);
            t = new Texture2D(W, Hh, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            t.SetPixels(px); t.Apply();
            backCache[theme] = t; return t;
        }
    }
}

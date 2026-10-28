using System.Collections.Generic;
using UnityEngine;

namespace Vesperhold
{
    // Skill effect toolkit. Every effect is a short-lived additive sprite built from the painted
    // Canva sheets in Assets/Resources/Art/VFX (one 192 px sprite each, named like the web Skill Lab).
    public static class Vfx
    {
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
        static Material additive;

        public static Material Additive
        {
            get
            {
                if (additive == null)
                {
                    var sh = Resources.Load<Shader>("Shaders/VfxAdditive");
                    if (sh == null) sh = Shader.Find("Sprites/Default");
                    additive = new Material(sh);
                }
                return additive;
            }
        }

        // 1 world unit wide at scale 1, so sizes below are in tiles.
        public static Sprite Get(string name)
        {
            Sprite s;
            if (cache.TryGetValue(name, out s)) return s;
            var tex = Resources.Load<Texture2D>("Art/VFX/" + name);
            if (tex != null)
            {
                tex.filterMode = FilterMode.Bilinear;
                s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
            }
            cache[name] = s;
            return s;
        }

        // Colour keys used by recipes: o orange, b blue, g green, p purple, r red, y gold, w white, k shadow violet.
        public static Color C(string key)
        {
            switch (key)
            {
                case "o": return new Color(1f, 0.62f, 0.27f);
                case "b": return new Color(0.38f, 0.72f, 1f);
                case "g": return new Color(0.42f, 0.92f, 0.5f);
                case "p": return new Color(0.78f, 0.52f, 1f);
                case "r": return new Color(1f, 0.28f, 0.32f);
                case "y": return new Color(1f, 0.86f, 0.42f);
                case "k": return new Color(0.55f, 0.32f, 0.78f);
                default: return Color.white;
            }
        }

        // Pick the colour variant of the four-colour sheets (crescent_o, streak_b, ...).
        public static string Key(string key) { return key == "o" || key == "b" || key == "g" || key == "p" ? key : key == "r" || key == "y" ? "o" : key == "k" ? "p" : "o"; }

        // Fading, growing, spinning sprite. Returns the effect so callers can tweak it.
        public static VfxSprite PopWH(string spr, Vector2 pos, float w, float h, float life, float grow = 1.3f, float rot = 0, float spin = 0, string tint = null, int order = 30, float alpha = 1f)
        {
            var s = Get(spr);
            if (s == null) return null;
            var go = new GameObject("Vfx_" + spr);
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s; sr.sharedMaterial = Additive; sr.sortingOrder = order;
            sr.color = tint == null ? Color.white : C(tint);
            var v = go.AddComponent<VfxSprite>();
            v.Setup(sr, w, h, life, grow, rot, spin, alpha);
            return v;
        }

        public static VfxSprite Pop(string spr, Vector2 pos, float size, float life, float grow = 1.3f, float rot = 0, float spin = 0, string tint = null, int order = 30, float alpha = 1f)
        {
            return PopWH(spr, pos, size, size, life, grow, rot, spin, tint, order, alpha);
        }

        public static void Burst(Vector2 pos, float size, string ck = "o", float life = 0.35f)
        {
            Pop("burst_" + Key(ck), pos, size, life, 1.35f, Random.Range(0f, 360f), 0, null, 32, 0.8f);
        }

        public static void Ring(Vector2 pos, float radius, string ck = "o", float life = 0.45f, float flat = 0.32f)
        {
            var v = PopWH("shock_" + Key(ck), pos, radius * 2f, radius * 2f * flat, life, 2.0f, 0, 0, null, 28, 0.7f);
            if (v != null) v.StartScale = 0.15f;
        }

        public static void Slash(Vector2 pos, int face, float size, string ck = "o", float rot = 0, float life = 0.28f)
        {
            var v = PopWH("crescent_" + Key(ck), pos, size * face, size, life, 1.25f, rot, 0, null, 34, 0.95f);
            if (v != null) v.StartScale = 0.55f;
            PopWH("streak_" + Key(ck), pos + new Vector2(face * size * 0.15f, 0), size * face, size, life * 0.8f, 1.2f, rot, 0, null, 33, 0.8f);
        }

        // A tall painted light pillar standing on the ground.
        public static void Pillar(Vector2 feet, float height, string spr = "pillar_gold", float life = 0.5f, float width = 1.6f)
        {
            var v = PopWH(spr, feet + Vector2.up * height * 0.5f, width, height, life, 1f, 0, 0, null, 30, 0.9f);
            if (v != null) v.WidthGrow = 0.6f;
        }

        // Sprite stretched between two points (chains, lightning, hook lines).
        public static void Beam(Vector2 a, Vector2 b, string spr = "bolt_blue", float width = 0.9f, float life = 0.18f, string tint = null)
        {
            var d = b - a; float len = d.magnitude; if (len < 0.05f) return;
            var v = PopWH(spr, (a + b) * 0.5f, len, width, life, 1f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg, 0, tint, 31, 0.95f);
            if (v != null) v.WidthGrow = 0.5f;
        }

        public static void Sparks(Vector2 pos, int n, float speed, string spr = "burst_o", string tint = null, float size = 0.4f, float life = 0.5f, float gravity = 0f)
        {
            for (int i = 0; i < n; i++)
            {
                float a = Random.value * Mathf.PI * 2f, sp = speed * (0.4f + Random.value * 0.8f);
                var v = Pop(spr, pos, size * (0.6f + Random.value * 0.8f), life * (0.6f + Random.value * 0.7f), 0.3f, Random.value * 360f, 0, tint, 31, 0.9f);
                if (v != null) { v.Vel = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * sp; v.Gravity = gravity; }
            }
        }

        // Translucent copy of the hero sprite (clones, afterimages, mirror images).
        public static void Ghost(Vector2 feet, int face, string tint, float life = 0.45f, float alpha = 0.7f)
        {
            var p = PlayerController.Instance; if (p == null || p.Body == null) return;
            var go = new GameObject("Ghost");
            go.transform.position = feet + (Vector2)p.Body.transform.localPosition;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = p.Body.sprite; sr.sharedMaterial = Additive; sr.sortingOrder = 9;
            sr.color = C(tint);
            var v = go.AddComponent<VfxSprite>();
            var ls = p.Body.transform.localScale;
            v.Setup(sr, Mathf.Abs(ls.x) * face, ls.y, life, 1f, 0, 0, alpha);
            v.Raw = true;
        }

        // Sprite that follows the hero for a while (shields, wings, auras).
        public static VfxSprite Follow(string spr, Transform target, Vector2 offset, float size, float life, string tint = null, float spin = 0, int order = 29)
        {
            var v = Pop(spr, (Vector2)target.position + offset, size, life, 1f, 0, spin, tint, order, 0.9f);
            if (v != null) { v.Target = target; v.Offset = offset; v.Pulse = true; v.FadeTail = 0.35f; v.FadeHead = 0.15f; }
            return v;
        }
    }

    public class VfxSprite : MonoBehaviour
    {
        public Vector2 Vel; public float Gravity, StartScale = 1f, WidthGrow = 1f, FadeTail = 1f, FadeHead = 0f;
        public Transform Target; public Vector2 Offset; public bool Pulse, Raw;
        SpriteRenderer sr; float w, h, life, grow, rot, spin, alpha, t; Color baseColor;

        public void Setup(SpriteRenderer r, float w, float h, float life, float grow, float rot, float spin, float alpha)
        {
            sr = r; this.w = w; this.h = h; this.life = Mathf.Max(0.05f, life); this.grow = grow; this.rot = rot; this.spin = spin; this.alpha = alpha;
            baseColor = r.color;
            Apply(0);
        }

        void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.Paused) return;
            t += Time.deltaTime;
            if (t >= life) { Destroy(gameObject); return; }
            Vel.y -= Gravity * Time.deltaTime;
            if (Target != null) transform.position = (Vector2)Target.position + Offset;
            else transform.position += (Vector3)(Vel * Time.deltaTime);
            Apply(t / life);
        }

        void Apply(float k)
        {
            float s = Raw ? 1f : Mathf.Lerp(StartScale, grow, Mathf.Sqrt(k));
            if (Pulse) s *= 1f + 0.04f * Mathf.Sin(t * 14f);
            float wm = Mathf.Lerp(1f, WidthGrow, k);
            transform.localScale = new Vector3(w * s, h * s * (WidthGrow != 1f ? wm : 1f), 1f);
            transform.rotation = Quaternion.Euler(0, 0, rot + spin * t);
            float fade = FadeTail >= 1f ? 1f - k : Mathf.Clamp01((1f - k) / FadeTail);
            if (FadeHead > 0f) fade *= Mathf.Clamp01(k / FadeHead);
            var c = baseColor; c.a = alpha * Mathf.Clamp01(fade); sr.color = c;
        }
    }
}

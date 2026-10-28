using System.Collections.Generic;
using UnityEngine;

namespace Vesperhold
{
    // One recipe per skill slot (12 per class) plus two follow-up recipes per slot, matching skills.json.
    // They are the Unity version of the Skill Lab effects: same names, timings and colours, built from the
    // painted sprites in Assets/Resources/Art/VFX. Tuning knobs are the numbers inside each recipe.
    public static partial class SkillRecipes
    {
        public static Recipe[] Skills(string id) { return id == "mage" ? MageSk : id == "rogue" ? RogueSk : WarriorSk; }
        public static Recipe[][] Upgrades(string id) { return id == "mage" ? MageUp : id == "rogue" ? RogueUp : WarriorUp; }

        // ---- helpers ----
        static ProjSpec Mk(SkillCtx c, string spr, Vector2 pos, Vector2 vel, float size, float mult)
        {
            return new ProjSpec { Spr = spr, Pos = pos, Vel = vel, Size = size, Dmg = c.D(mult) };
        }

        static Vector2 Fw(SkillCtx c, float x, float y = 0f) { return new Vector2(c.F * x, y); }

        // Ground spots under the nearest enemies, padded with spots in front of the hero.
        static List<Vector2> Spots(SkillCtx c, int n, float range, float step)
        {
            var list = new List<Vector2>();
            foreach (var e in c.Near(range, n)) list.Add(e.transform.position);
            for (int i = list.Count; i < n; i++) { float x = c.Feet.x + c.F * (2f + i * step); list.Add(new Vector2(x, c.GroundY(x, c.Feet.y + 1.5f))); }
            return list;
        }

        // A projectile that falls from above onto a spot and explodes.
        static void Drop(SkillCtx c, string spr, Vector2 at, float size, float mult, float boom, string key, float height = 9f, bool sword = false, float delay = 0f, Vector2? vel = null)
        {
            c.After(delay, () =>
            {
                float top = Mathf.Max(at.y + 2.5f, Mathf.Min(at.y + height, c.CeilY(at, height)));
                var v = vel ?? new Vector2(0, -20f);
                var s = Mk(c, spr, new Vector2(at.x - v.x * (top - at.y) / Mathf.Max(1f, -v.y), top), v, size, mult);
                s.Boom = boom; s.BoomKey = key; s.Radius = 0.6f; s.Life = 1.6f;
                if (sword) { s.Align = true; s.BaseDeg = 90f; }
                c.Fire(s);
            });
        }

        // A tall sprite bursts out of the ground (spikes, vines, hands, rock) and hurts what it touches.
        static void Erupt(SkillCtx c, string spr, Vector2 g, float w, float h, float mult, float delay, float stun, string key)
        {
            c.After(delay, () =>
            {
                Vfx.PopWH(spr, g + Vector2.up * h * 0.5f, w, h, 0.7f, 1f, 0, 0, null, 30, 0.95f);
                Vfx.Burst(g + Vector2.up * 0.3f, w * 1.6f, key, 0.3f);
                foreach (var e in Combat.Near(g + Vector2.up, w * 0.9f + 0.5f, 8)) { e.TakeDamage(c.D(mult), c.F, 0); if (stun > 0) e.Stun(stun, key); }
                c.Shake(0.05f);
            });
        }

        // A flying sprite that pierces everything on its path (spirits, serpents, wolves, phoenix).
        static void Spirit(SkillCtx c, string spr, Vector2 from, float speed, float size, float mult, float life, string tint = null, float baseDeg = 0f, float turn = 0f)
        {
            var s = Mk(c, spr, from, Fw(c, speed), size, mult);
            s.Pierce = true; s.Life = life; s.Radius = size * 0.45f; s.Tint = tint; s.Trail = "burst_o"; s.BoomKey = tint != null ? Vfx.Key(tint) : "o";
            s.ExplodeOnWall = false; s.Home = turn > 0f; s.Turn = turn;
            c.Fire(s);
        }

        static void Banner(SkillCtx c, string s, string col) { FloatingText.Spawn(c.Pl.transform.position + Vector3.up * 2.6f, s, Vfx.C(col)); }

        // The same shape on every enemy in range: a mark that detonates.
        static void MarkAll(SkillCtx c, string mark, float range, int max, float size, float delay, float mult, string key, string boomSpr = "burst_o")
        {
            foreach (var e in c.Near(range, max))
            {
                var tgt = e;
                Vfx.Pop(mark, tgt.Mid + Vector2.up * 0.6f, size, delay, 1.15f, 0, 40f, null, 33, 0.9f);
                c.After(delay, () =>
                {
                    if (tgt == null) return;
                    Vfx.Pillar(tgt.transform.position, 5f, key == "b" ? "pillar_ice" : key == "p" ? "pillar_shadow" : "pillar_fire", 0.5f, 1.8f);
                    Vfx.Burst(tgt.Mid, size * 1.6f, key, 0.35f);
                    tgt.TakeDamage(c.D(mult), c.F, 0);
                });
            }
        }
    }
}

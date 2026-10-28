using System.Collections.Generic;
using UnityEngine;

namespace Vesperhold
{
    public static partial class SkillRecipes
    {
        static void Star(SkillCtx c, float deg, float speed, float mult)
        {
            float a = deg * Mathf.Deg2Rad;
            var s = Mk(c, "shuriken", c.P + Fw(c, 0.7f), new Vector2(c.F * Mathf.Cos(a), Mathf.Sin(a)) * speed, 0.9f, mult);
            s.Spin = 720f; s.Life = 0.9f; s.Radius = 0.55f; s.Trail = "burst_g"; s.BoomKey = "g"; c.Fire(s);
        }

        static void Mine(SkillCtx c, float dx, float delay, float r, float mult)
        {
            float x = c.Feet.x + c.F * dx; var g = new Vector2(x, c.GroundY(x, c.Feet.y + 1f));
            Vfx.Pop("mine", g + Vector2.up * 0.4f, 1f, delay + 0.05f, 1f, 0, 0, null, 24, 0.95f);
            c.After(delay, () => {
                Vfx.Burst(g + Vector2.up * 0.6f, r * 2.4f, "o", 0.45f); Vfx.Ring(g, r * 1.2f, "o", 0.5f); Vfx.Sparks(g, 12, 6f, "burst_o", null, 0.4f, 0.6f, 8f);
                c.Circle(g + Vector2.up * 0.6f, r, mult); c.Shake(0.2f);
            });
        }

        static readonly Recipe[] RogueSk = new Recipe[]
        {
            // 1 Double Throw: three spinning stars
            c => { Star(c, -8f, 16f, 0.9f); Star(c, 0f, 17f, 0.9f); Star(c, 8f, 16f, 0.9f); },
            // 2 Dash Slash: blink through them all
            c => {
                Vector2 a = c.P; Vfx.Ghost(c.Feet, c.F, "g", 0.45f); c.Pl.Blink(c.F * 6f, 0.4f); c.Refresh(); Vector2 b = c.P;
                Vfx.Beam(a, b, "bolt_green", 1.1f, 0.3f); Vfx.Slash((a + b) * 0.5f, c.F, 4f, "g"); c.Box((a + b) * 0.5f, Mathf.Abs(b.x - a.x) + 1.2f, 2f, 2f);
            },
            // 3 Haste: speed lines and afterimages
            c => {
                c.Caster.Buff("haste", 1.5f, 6f); Vfx.Ring(c.Feet, 2.5f, "g", 0.4f);
                for (int i = 0; i < 20; i++) c.After(i * 0.3f, () => { c.Refresh(); Vfx.Ghost(c.Feet, c.Pl.Facing, "g", 0.4f, 0.5f); Vfx.Pop("streak_g", c.P - Fw(c, 1.2f), 3f, 0.25f, 1.2f); });
            },
            // 4 Shadow Storm: a vortex of blades
            c => { c.Caster.Zone(c, c.P, 2.6f, 2.2f, 0.25f, 0.7f, "tornado", 4f, "p", 0f, 0f, 0f, true, 0f); c.Caster.Orbit(c, "dagger_red", 5, 2f, 2.2f, 0.4f, 0.6f, null, 1.1f, 3f); c.Pl.GrantInvul(0.4f); },
            // 5 Venom Cloud: throw a vial that leaves a toxic cloud
            c => {
                var s = Mk(c, "orb_poison", c.P + Fw(c, 0.8f), new Vector2(c.F * 8f, 4f), 0.9f, 0.5f);
                s.Grav = 16f; s.Life = 1.3f; s.Radius = 0.5f; s.BoomKey = "g";
                s.OnEnd = pos => { Vfx.Burst(pos, 3f, "g", 0.4f); c.Caster.Zone(c, pos + Vector2.up * 0.6f, 2.6f, 4f, 0.3f, 0.4f, "poisonwisps", 5.5f, null, 0f, 0f, 0f, false, 0f); };
                c.Fire(s);
            },
            // 6 Shadow Clone: clones flicker in and slash everyone
            c => {
                int n = 0;
                foreach (var e in c.Near(9f, 3)) { var t = e; n++; c.After(n * 0.12f, () => { if (t == null) return; Vfx.Ghost((Vector2)t.transform.position - Fw(c, 1f), c.F, "k", 0.5f); Vfx.Slash(t.Mid, c.F, 3.2f, "p"); Vfx.Burst(t.Mid, 2f, "p", 0.3f); t.TakeDamage(c.D(1.3f), c.F, 0); }); }
                Vfx.Ghost(c.Feet, c.F, "k", 0.5f);
            },
            // 7 Smoke Bomb: vanish in smoke, reappear in style
            c => {
                c.Pl.GrantInvul(1.2f); Vfx.Pop("poisonwisps", c.P, 5f, 1.2f, 1.3f, 0, 20f, "k", 30, 0.85f); Vfx.Burst(c.P, 4f, "p", 0.4f);
                foreach (var e in c.Near(3.5f, 10)) e.Stun(0.8f, "k");
                c.Pl.Blink(-c.F * 3f, 1.2f);
                c.After(1f, () => { c.Refresh(); Vfx.Burst(c.P, 3.5f, "p", 0.4f); Vfx.Ring(c.Feet, 3f, "p", 0.4f); });
            },
            // 8 Blade Fan: seven knives fan out
            c => {
                for (int i = -3; i <= 3; i++) { float a = i * 9f * Mathf.Deg2Rad; var s = Mk(c, "dagger_red", c.P + Fw(c, 0.6f), new Vector2(c.F * Mathf.Cos(a), Mathf.Sin(a)) * 15f, 1.1f, 0.8f);
                    s.Align = true; s.BaseDeg = 45f; s.Life = 0.7f; s.Radius = 0.5f; s.Trail = "burst_g"; s.BoomKey = "g"; c.Fire(s); }
            },
            // 9 Backstab Combo: teleport behind each target
            c => {
                var l = c.Near(10f, 4);
                if (l.Count == 0) { c.Pl.Blink(c.F * 4f, 0.3f); Vfx.Slash(c.P + Fw(c, 1f), c.F, 3f, "g"); return; }
                for (int i = 0; i < l.Count; i++) { var t = l[i];
                    c.After(i * 0.22f, () => { if (t == null) return; c.Refresh(); float side = t.transform.position.x >= c.Feet.x ? 1f : -1f; c.Pl.Blink(t.transform.position.x + side * 1.1f - c.Feet.x, 0.4f); c.Pl.Facing = side > 0 ? -1 : 1;
                        Vfx.Ghost(c.Feet, c.Pl.Facing, "k", 0.35f); Vfx.Slash(t.Mid, c.Pl.Facing, 3.4f, "g"); Vfx.Burst(t.Mid, 2f, "g", 0.3f); t.TakeDamage(c.D(1.6f), c.Pl.Facing, 0); c.Shake(0.06f); }); }
            },
            // 10 Explosive Trap: a mine that blows up big
            c => { Mine(c, 2.5f, 1.1f, 3.2f, 5f); },
            // 11 Phantom Barrage: a flurry of afterimage slashes
            c => {
                for (int i = 0; i < 8; i++) { int k = i; c.After(k * 0.1f, () => { c.Refresh(); var at = c.P + Fw(c, 1.2f + (k % 3) * 0.6f, k % 2 == 0 ? 0.4f : -0.3f);
                    Vfx.Ghost(c.Feet + Fw(c, 0.5f + (k % 3) * 0.5f), c.F, "k", 0.3f, 0.5f); Vfx.Slash(at, c.F, 3.4f, k % 2 == 0 ? "p" : "g", k % 2 == 0 ? -20f : 25f, 0.25f); c.Box(at, 3f, 2f, 0.9f); }); }
            },
            // 12 Assassinate: one frozen frame, then every enemy is cut
            c => {
                var l = c.Near(12f, 8); Vfx.Ring(c.Feet, 6f, "p", 0.6f); c.Stop(0.2f);
                foreach (var e in l) { e.Stun(1.4f, "p"); Vfx.Pop("cross_p", e.Mid, 2.6f, 1f, 1.1f, 45f, 0, null, 33, 0.9f); }
                c.After(1f, () => { foreach (var e in l) { if (e == null) continue; for (int k = 0; k < 3; k++) Vfx.Slash(e.Mid + new Vector2(Random.Range(-0.4f, 0.4f), Random.Range(-0.4f, 0.4f)), k % 2 == 0 ? 1 : -1, 3.4f, "p", k * 55f, 0.3f);
                    Vfx.Burst(e.Mid, 3.4f, "p", 0.4f); e.TakeDamage(c.D(3.6f), c.F, 0); } c.Shake(0.3f); });
            },
        };

        static readonly Recipe[][] RogueUp = new Recipe[][]
        {
            // Double Throw
            new Recipe[] {
                // Boomerang Stars: the stars curve out and fly back to your hand
                c => { for (int i = -1; i <= 1; i += 2) { var s = Mk(c, "boomerang", c.P + Fw(c, 0.6f), new Vector2(c.F * 13f, i * 2.5f), 1.4f, 0.9f); s.Pierce = true; s.Life = 0.45f; s.Radius = 0.8f; s.Spin = 700f; s.ExplodeOnWall = false;
                           s.OnEnd = pos => { c.Refresh(); var r = Mk(c, "boomerang", pos, (c.P - pos).normalized * 13f, 1.4f, 0.9f); r.Pierce = true; r.Life = 0.5f; r.Radius = 0.8f; r.Spin = 700f; r.ExplodeOnWall = false; c.Fire(r); }; c.Fire(s); } },
                // Confetti Burst: stars pop overhead into a rain of razor confetti
                c => { var sp = Spots(c, 3, 10f, 2.5f); string[] cols = { "o", "g", "p" }; for (int i = 0; i < sp.Count; i++) { var at = sp[i] + Vector2.up * 3f; string col = cols[i % 3];
                           Vfx.Pop("firework", at, 4f, 0.7f, 1.4f, 0, 0, null, 33, 0.95f); Vfx.Sparks(at, 14, 5f, "burst_" + col, null, 0.35f, 0.9f, 7f); c.After(0.3f + i * 0.1f, () => c.Circle(at - Vector2.up * 2f, 2.4f, 1.2f)); } },
            },
            // Dash Slash
            new Recipe[] {
                // Frozen Frames: time stops for a heartbeat, then the cuts land
                c => { foreach (var e in c.Near(9f, 6)) { e.Stun(1.1f, "p"); Vfx.Ghost(e.transform.position, c.F, "p", 1f, 0.35f); }
                       c.After(1f, () => { foreach (var e in c.Near(9f, 6)) { Vfx.Slash(e.Mid, c.F, 3.2f, "p"); e.TakeDamage(c.D(1.5f), c.F, 0); } c.Shake(0.1f); }); },
                // Crescent Trail: crescents linger along the path, then cut again
                c => { for (int i = 0; i < 4; i++) { int k = i; var at = c.P - Fw(c, 1f + k * 1.6f); Vfx.Pop("crescent_g", at, 3.4f * c.F, 0.7f, 1.2f, 0, 0, null, 31, 0.8f);
                           c.After(0.45f + k * 0.08f, () => { Vfx.Burst(at, 2.2f, "g", 0.25f); c.Circle(at, 1.9f, 1.0f); }); } },
            },
            // Haste
            new Recipe[] {
                // Time Ripples: a clock and rings of slowed time
                c => { Vfx.Pop("clock", c.P + Vector2.up * 1f, 4f, 1.5f, 1.2f, 0, 40f, null, 30, 0.9f); for (int i = 0; i < 3; i++) { int k = i; c.After(k * 0.25f, () => { c.Refresh(); Vfx.Ring(c.Feet, 3f + k * 1.5f, "g", 0.5f); c.Circle(c.P, 3f + k * 1.5f, 0.6f); foreach (var e in c.Near(6f, 10)) e.Stun(0.6f, "g"); }); } },
                // Phantom Sprint: a ghost sprint cuts a line through the field
                c => { Vector2 a = c.Feet; c.Pl.Blink(c.F * 6f, 0.5f); c.Refresh(); for (int i = 0; i < 5; i++) Vfx.Ghost(Vector2.Lerp(a, c.Feet, i / 4f), c.F, "g", 0.5f + i * 0.08f, 0.5f);
                       c.Box((a + c.Feet) * 0.5f + Vector2.up * 0.8f, Mathf.Abs(c.Feet.x - a.x) + 1f, 2f, 1.3f); },
            },
            // Shadow Storm
            new Recipe[] {
                // Shadow Hands: hands of shadow rise and grab
                c => { foreach (var s in Spots(c, 3, 10f, 2.5f)) Erupt(c, "hand_shadow", s, 2f, 3f, 1.4f, 0.1f, 1.2f, "p"); },
                // Black Hole Blades: a black hole pulls in the blades
                c => { c.Caster.Zone(c, c.Feet + Fw(c, 4f, 1.4f), 2.6f, 2.4f, 0.3f, 0.6f, "portal", 4.4f, "k", 1.2f, 1.6f, 3f, false, 150f); },
            },
            // Venom Cloud
            new Recipe[] {
                // Thorn Vines: vines erupt from the cloud
                c => { for (int i = 0; i < 5; i++) { float x = c.Feet.x + c.F * (2.5f + i * 1.3f); Erupt(c, "vine", new Vector2(x, c.GroundY(x, c.Feet.y + 1f)), 1.4f, 2.6f, 1.2f, 0.4f + i * 0.08f, 0.8f, "g"); } },
                // Toxic Bloom: the cloud blooms into a poison burst
                c => { var at = c.Feet + Fw(c, 5f); Vfx.Pop("spores", at + Vector2.up * 1f, 6f, 1f, 1.2f, 0, 20f, null, 30, 0.9f); c.After(0.8f, () => { Vfx.Burst(at + Vector2.up, 7f, "g", 0.5f); Vfx.Ring(at, 4.5f, "g", 0.5f); c.Circle(at + Vector2.up, 4f, 2f); c.Shake(0.15f); }); },
            },
            // Shadow Clone
            new Recipe[] {
                // Ink Clones: ink clones stay and keep slashing
                c => { for (int i = 0; i < 3; i++) { int k = i; var at = c.Feet + Fw(c, 2f + k * 1.6f); Vfx.Ghost(at, c.F, "k", 3f, 0.55f);
                           for (int j = 1; j <= 4; j++) c.After(j * 0.65f + k * 0.1f, () => { Vfx.Slash(at + Vector2.up * 0.8f + Fw(c, 0.8f), c.F, 3f, "p"); c.Circle(at + Vector2.up * 0.8f, 2f, 0.8f); }); } },
                // Kaleidoscope: eight clones burst out in a ring
                c => { c.Refresh(); for (int i = 0; i < 8; i++) { float a = i * 45f * Mathf.Deg2Rad; var at = c.Feet + new Vector2(Mathf.Cos(a) * 3f, Mathf.Max(0f, Mathf.Sin(a)) * 1.2f); Vfx.Ghost(at, Mathf.Cos(a) >= 0 ? 1 : -1, i % 2 == 0 ? "p" : "g", 0.7f, 0.55f); Vfx.Slash(at + Vector2.up * 0.8f, Mathf.Cos(a) >= 0 ? 1 : -1, 2.6f, i % 2 == 0 ? "p" : "g", i * 45f); }
                       Vfx.Pop("cross_p", c.P, 8f, 0.6f, 1.3f, 45f); c.Circle(c.P, 4.5f, 1.8f); c.Shake(0.15f); },
            },
            // Smoke Bomb
            new Recipe[] {
                // Smoke Serpent: a serpent of smoke weaves through the enemy line
                c => { Spirit(c, "dragon", c.P + Fw(c, 1f, 0.3f), 12f, 3.2f, 1.8f, 1f, "k"); },
                // Raven Murder: a murder of raven feathers dives from above
                c => { for (int i = 0; i < 10; i++) { var s = Mk(c, "feather", c.P + new Vector2(Random.Range(-1f, 1f), 1.5f), new Vector2(c.F * Random.Range(1f, 5f), Random.Range(5f, 9f)), 1f, 0.6f);
                           s.Home = true; s.Turn = 4f; s.Align = true; s.BaseDeg = 45f; s.Life = 1.8f; s.Radius = 0.6f; s.Trail = "burst_shadow"; s.Tint = "k"; s.BoomKey = "p"; c.Fire(s); } },
            },
            // Blade Fan
            new Recipe[] {
                // Feather Fan: nine feathers fan wide
                c => { for (int i = -4; i <= 4; i++) { float a = i * 9f * Mathf.Deg2Rad; var s = Mk(c, "feather", c.P + Fw(c, 0.6f), new Vector2(c.F * Mathf.Cos(a), Mathf.Sin(a)) * 13f, 1.1f, 0.7f); s.Align = true; s.BaseDeg = 45f; s.Life = 0.8f; s.Radius = 0.5f; s.Trail = "burst_g"; s.BoomKey = "g"; c.Fire(s); } },
                // Pin Cushion: knives pin the nearest foe from above
                c => { var l = c.Near(10f, 1); Vector2 at = l.Count > 0 ? (Vector2)l[0].transform.position : c.Feet + Fw(c, 4f);
                       for (int i = 0; i < 10; i++) { int k = i; c.After(k * 0.07f, () => { var s = Mk(c, "dagger_red", at + new Vector2(Random.Range(-0.6f, 0.6f), 8f), new Vector2(0, -22f), 1.2f, 0.5f); s.Align = true; s.BaseDeg = 45f; s.Boom = 0.9f; s.Radius = 0.6f; s.BoomKey = "g"; s.Life = 1f; c.Fire(s); }); } },
            },
            // Backstab Combo
            new Recipe[] {
                // Combo Counter: a rising combo counter ends in a finisher
                c => { var l = c.Near(9f, 5); int n = 0; foreach (var e in l) { n++; var t = e; int num = n; c.After(num * 0.1f, () => { if (t == null) return; FloatingText.Spawn(t.transform.position + Vector3.up * 2f, "x" + num, Vfx.C("y")); t.TakeDamage(c.D(0.6f + num * 0.2f), c.F, 0); Vfx.Burst(t.Mid, 2f, "o", 0.25f); }); }
                       c.After(0.7f, () => { foreach (var e in l) if (e != null) { Vfx.Pop("cross_o", e.Mid, 3.6f, 0.5f, 1.3f, 45f); e.TakeDamage(c.D(2.4f), c.F, 0); } c.Shake(0.2f); }); },
                // Rift Steps: rifts open at each step and cut again
                c => { foreach (var e in c.Near(9f, 4)) { var t = e; Vfx.Pop("portal", t.Mid + Vector2.up * 0.3f, 2.4f, 0.8f, 1f, 0, 150f, "p", 30, 0.9f); c.After(0.4f, () => { if (t == null) return; Vfx.Slash(t.Mid, c.F, 3.4f, "p"); t.TakeDamage(c.D(1f), c.F, 0); Vfx.Burst(t.Mid, 2.4f, "p", 0.3f); }); } },
            },
            // Explosive Trap
            new Recipe[] {
                // Fireworks: the blast launches a volley of fireworks
                c => { for (int i = 0; i < 6; i++) { int k = i; c.After(1.1f + k * 0.12f, () => { var at = c.Feet + Fw(c, 2.5f + Random.Range(-2.5f, 2.5f), 2f + Random.Range(0f, 3f)); Vfx.Pop("firework", at, 3.4f, 0.6f, 1.4f, 0, 0, null, 33, 0.95f); Vfx.Sparks(at, 8, 4f, "burst_o", null, 0.35f, 0.7f, 6f); c.Circle(at - Vector2.up * 1.5f, 2f, 0.9f); }); } },
                // Chain Mines: more mines chain-detonate down the field
                c => { for (int i = 0; i < 3; i++) Mine(c, 5f + i * 2.4f, 1.1f + i * 0.35f, 2.6f, 3f); },
            },
            // Phantom Barrage
            new Recipe[] {
                // Ghost Army: three ghosts charge forward
                c => { for (int i = 0; i < 3; i++) { int k = i; c.After(k * 0.15f, () => { c.Refresh(); Spirit(c, "knight", c.P + Fw(c, 1f, -0.2f + k * 0.2f), 9f, 2.2f, 1.3f, 1f, "p"); }); } },
                // Stopped Clock: a clock stops time, then everything is hit at once
                c => { c.Refresh(); Vfx.Pop("clock", c.P + Vector2.up * 4f, 5f, 1.7f, 1.1f, 0, 0, "y", 30, 0.9f); var l = c.Near(11f, 10); foreach (var e in l) e.Stun(1.6f, "y");
                       c.After(1.6f, () => { foreach (var e in l) if (e != null) { Vfx.Burst(e.Mid, 3f, "y", 0.35f); e.TakeDamage(c.D(2f), c.F, 0); } c.Shake(0.2f); }); },
            },
            // Assassinate
            new Recipe[] {
                // Death Mark: a skull sigil marks everyone, then they pop
                c => { MarkAll(c, "skull_sigil", 12f, 8, 1.8f, 1.2f, 3f, "p"); },
                // Final Curtain: shadow curtains close in from both sides
                c => { c.Refresh(); for (int s = -1; s <= 1; s += 2) { var v = Vfx.PopWH("pillar_shadow", c.Feet + new Vector2(s * 8f, 4f), 3f, 9f, 1f, 1f, 0, 0, null, 31, 0.95f); if (v != null) v.Vel = new Vector2(-s * 7.5f, 0); }
                       c.After(1f, () => { Vfx.Pop("cross_p", c.P, 14f, 0.6f, 1.2f, 45f); c.Box(c.P, 14f, 6f, 4f); c.Shake(0.35f); c.Stop(0.1f); }); },
            },
        };
    }
}

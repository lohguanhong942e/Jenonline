using System.Collections.Generic;
using UnityEngine;

namespace Vesperhold
{
    public static partial class SkillRecipes
    {
        static readonly Recipe[] WarriorSk = new Recipe[]
        {
            // 1 Power Strike: lunge and cleave with a flaring crescent
            c => {
                c.Pl.Launch(new Vector2(c.F * 7f, 2f)); c.Pl.GrantInvul(0.1f);
                var at = c.P + Fw(c, 1.1f);
                Vfx.Slash(at, c.F, 3.4f, "o");
                c.Box(at, 2.8f, 2f, 2.2f);
                Vfx.Sparks(at, 8, 5f, "burst_o", null, 0.4f, 0.4f); c.Shake(0.08f);
            },
            // 2 Shockwave: a ground wave that pierces everything
            c => {
                var s = Mk(c, "streak_o", c.Feet + Fw(c, 0.8f, 0.6f), Fw(c, 11f), 2.8f, 1.6f);
                s.Pierce = true; s.Life = 0.9f; s.Radius = 0.9f; s.Trail = "burst_o";
                c.Fire(s); Vfx.Ring(c.Feet + Fw(c, 0.8f), 1.8f, "o", 0.35f);
            },
            // 3 War Cry: golden shout, attack aura for 12 s
            c => {
                c.Caster.Buff("atk", 1.35f, 12f);
                Vfx.Ring(c.Feet, 4.5f, "y", 0.6f); Vfx.Pillar(c.Feet, 5f, "pillar_gold", 0.7f, 2.4f);
                Vfx.Follow("burst_o", c.Pl.transform, new Vector2(0, 0.8f), 2.6f, 1.2f, "y"); c.Shake(0.12f);
            },
            // 4 Rage Slam: leap and crush the ground
            c => {
                c.Pl.Launch(new Vector2(c.F * 3f, 12f));
                c.After(0.55f, () => {
                    c.Refresh(); var g = c.Feet;
                    Vfx.Ring(g, 4f, "o", 0.55f); Vfx.Pop("lavacrack", g + Vector2.up * 0.2f, 6f, 0.9f, 1f, 0, 0, null, 26, 0.9f);
                    Vfx.Burst(g + Vector2.up * 0.5f, 5f, "o"); c.Circle(g + Vector2.up * 0.5f, 3.4f, 4f);
                    c.Shake(0.28f); c.Stop(0.08f); Vfx.Sparks(g, 14, 6f, "burst_o", null, 0.4f, 0.6f, 8f);
                });
            },
            // 5 Whirlwind: spin and slash everything around you
            c => {
                c.Pl.GrantInvul(0.7f); Vfx.Follow("tornado", c.Pl.transform, new Vector2(0, 0.9f), 3.4f, 0.7f, "o");
                for (int i = 0; i < 4; i++) { int k = i; c.After(k * 0.16f, () => {
                    c.Refresh(); Vfx.Slash(c.P, k % 2 == 0 ? 1 : -1, 4.6f, "o", k * 45f, 0.3f);
                    c.Circle(c.P, 2.5f, 1.1f); Vfx.Sparks(c.P, 5, 5f, "burst_o"); }); }
            },
            // 6 Cleaving Arc: a giant crescent flies across the field
            c => {
                var s = Mk(c, "crescent_o", c.P + Fw(c, 1f), Fw(c, 13f), 4f, 2.2f);
                s.Pierce = true; s.Life = 1.1f; s.Radius = 1.6f; s.Trail = "burst_o"; c.Fire(s);
            },
            // 7 Iron Bulwark: golden plates shield you for 4 s
            c => {
                c.Pl.ShieldT = 4f; Vfx.Follow("shield_plate", c.Pl.transform, new Vector2(0, 0.9f), 3.2f, 4f, null, 0, 31);
                Vfx.Ring(c.Feet, 2.5f, "y", 0.5f);
            },
            // 8 Blade Rain: flaming swords fall from the sky
            c => {
                for (int i = 0; i < 8; i++) {
                    float x = c.P.x + c.F * (1.5f + i * 1.0f) + Random.Range(-0.4f, 0.4f);
                    Drop(c, "sword_fire", new Vector2(x, c.GroundY(x, c.P.y)), 1.4f, 1.3f, 1.4f, "o", 9f, true, i * 0.09f);
                }
            },
            // 9 Chain Pull: chains drag every enemy to you
            c => {
                var l = c.Near(10f, 6); var to = c.P + Fw(c, 1.2f, -0.6f);
                foreach (var e in l) { Vfx.Beam(c.P, e.Mid, "bolt_gold", 0.9f, 0.35f); Vfx.Pop("hook_chain", e.Mid, 1.4f, 0.35f, 1.1f); e.Pull(to, 0.4f); }
                c.After(0.4f, () => { foreach (var e in l) if (e != null) { e.TakeDamage(c.D(1.4f), c.F, 0); Vfx.Burst(e.Mid, 1.6f, "y", 0.25f); } c.Shake(0.1f); });
            },
            // 10 Thunder Charge: charge through them wreathed in lightning
            c => {
                Vector2 a = c.P; c.Pl.Blink(c.F * 7f, 0.45f); c.Refresh(); Vector2 b = c.P;
                Vfx.Beam(a, b, "bolt_gold", 1.4f, 0.35f); Vfx.Beam(a + Vector2.up * 0.5f, b + Vector2.up * 0.5f, "bolt_blue", 0.8f, 0.3f);
                Vfx.Pop("burst_lightning", b, 3.5f, 0.4f); c.Box((a + b) * 0.5f, Mathf.Abs(b.x - a.x) + 1.2f, 2f, 2.4f); c.Shake(0.12f);
            },
            // 11 Blood Rage: crimson aura boosts your attack
            c => {
                c.Caster.Buff("atk", 1.5f, 10f);
                Vfx.Pillar(c.Feet, 4.5f, "pillar_fire", 0.8f, 2f); Vfx.Follow("burst_o", c.Pl.transform, new Vector2(0, 0.8f), 3f, 1.4f, "r");
                Vfx.Ring(c.Feet, 3.5f, "o", 0.5f); c.Shake(0.1f);
            },
            // 12 Titan's Wrath: a colossal spectral blade crashes down
            c => {
                Vector2 at = c.Feet + Fw(c, 5f);
                var v = Vfx.PopWH("sword_gold", at + Vector2.up * 9f, 8f, 8f, 0.55f, 1f, 180f, 0, null, 36, 1f);
                if (v != null) { v.Vel = new Vector2(0, -16f); v.StartScale = 1f; }
                c.After(0.55f, () => {
                    Vfx.Ring(at, 6f, "y", 0.6f); Vfx.Pillar(at, 9f, "pillar_gold", 0.7f, 4f); Vfx.Burst(at + Vector2.up, 8f, "y", 0.5f);
                    c.Box(at + Vector2.up * 2f, 8f, 5f, 7f); c.Shake(0.4f); c.Stop(0.12f);
                });
            },
        };

        static readonly Recipe[][] WarriorUp = new Recipe[][]
        {
            // Power Strike
            new Recipe[] {
                // Echo Blade: a spectral warrior repeats the cleave, far bigger
                c => { c.Refresh(); var at = c.P + Fw(c, 1.8f); Vfx.PopWH("knight", c.Feet + Fw(c, 0.6f, 1.2f), 2.8f * c.F, 2.8f, 0.5f, 1f, 0, 0, "y", 8, 0.55f);
                       Vfx.Slash(at, c.F, 5.6f, "y"); c.Box(at, 4.6f, 2.6f, 1.8f); c.Shake(0.1f); },
                // Searing Brand: a burning sword sigil brands each target, then light erupts
                c => { MarkAll(c, "rune_red", 7f, 3, 1.8f, 0.6f, 1.8f, "o"); },
            },
            // Shockwave
            new Recipe[] {
                // Fissure Run: a glowing lava crack races across the ground
                c => { for (int i = 0; i < 7; i++) { int k = i; c.After(k * 0.07f, () => { float x = c.Feet.x + c.F * (1.5f + k * 1.5f); var g = new Vector2(x, c.GroundY(x, c.Feet.y + 1f));
                           Vfx.Pop("lavacrack", g + Vector2.up * 0.2f, 2.4f, 0.8f, 1f, 0, 0, null, 26, 0.9f); Vfx.Sparks(g, 3, 3f, "burst_o", null, 0.3f, 0.4f, 6f); c.Circle(g, 1.1f, 0.6f); }); } },
                // Boulder Burst: rocks burst from the ground and rain back down
                c => { for (int i = 0; i < 5; i++) { int k = i; float x = c.Feet.x + c.F * (2.5f + k * 1.6f); var g = new Vector2(x, c.GroundY(x, c.Feet.y + 1f));
                           Erupt(c, "lavaspire", g, 1.5f, 2.4f, 1.2f, k * 0.1f, 0.4f, "o"); Drop(c, "crystal_violet", g, 0.9f, 0.7f, 1.2f, "o", 6f, false, 0.55f + k * 0.08f); } },
            },
            // War Cry
            new Recipe[] {
                // Battle Horn: three shock rings knock enemies back
                c => { for (int i = 0; i < 3; i++) { int k = i; c.After(k * 0.18f, () => { c.Refresh(); Vfx.Ring(c.Feet, 3f + k * 1.5f, "y", 0.5f); c.Circle(c.P, 3f + k * 1.5f, 0.6f);
                           foreach (var e in c.Near(5f + k * 1.5f, 10)) e.Stun(0.5f, "y"); }); }
                       Vfx.Pop("wings_gold", c.P, 4f, 0.8f, 1.3f, 0, 0, null, 8, 0.8f); c.Shake(0.12f); },
                // Titan Wings: golden wings spread behind you for 6 s and quicken your steps
                c => { Vfx.Follow("wings_gold", c.Pl.transform, new Vector2(-c.F * 0.15f, 1.2f), 4.6f, 6f, null, 0, 8); c.Caster.Buff("haste", 1.18f, 6f); Vfx.Pillar(c.Feet, 4f, "pillar_gold", 0.5f, 1.5f); },
            },
            // Rage Slam
            new Recipe[] {
                // Stone Pillars: pillars of rock erupt in a line
                c => { for (int i = 0; i < 6; i++) { float x = c.Feet.x + c.F * (1.5f + i * 1.5f); Erupt(c, "lavaspire", new Vector2(x, c.GroundY(x, c.Feet.y + 1f)), 1.5f, 3f, 1.8f, 0.45f + i * 0.08f, 0.6f, "o"); } },
                // Seismic Crack: a rune-lit crack splits both sides of the ground
                c => { c.After(0.5f, () => { c.Refresh(); for (int s = -1; s <= 1; s += 2) { Vfx.PopWH("lavacrack", c.Feet + new Vector2(s * 4f, 0.2f), 8f, 2.4f, 1.1f, 1f, 0, 0, null, 26, 0.95f); c.Box(c.Feet + new Vector2(s * 4f, 0.8f), 8f, 1.6f, 1.5f); }
                           Vfx.PopWH("rune_red", c.Feet + Vector2.up * 0.1f, 5f, 1.6f, 1f, 1.2f, 0, 0, null, 25, 0.9f); c.Shake(0.2f); }); },
            },
            // Whirlwind
            new Recipe[] {
                // Fire Tornado: a flaming tornado drifts forward
                c => { c.Caster.Zone(c, c.Feet + Fw(c, 3f, 1f), 1.9f, 2.5f, 0.25f, 0.55f, "tornado", 4.2f, "o", 0f, 0f, 0f, false, 0f); Vfx.Pop("burst_fire", c.Feet + Fw(c, 3f, 1f), 3f, 0.4f); },
                // Blade Vortex: spectral swords orbit you
                c => { c.Caster.Orbit(c, "sword_gold", 6, 2.2f, 2.2f, 0.25f, 0.7f, "o", 1.4f, 3f); },
            },
            // Cleaving Arc
            new Recipe[] {
                // Splitting Wave: the crescent splits into two flying up and down
                c => { for (int d = -1; d <= 1; d += 2) { var s = Mk(c, "crescent_o", c.P + Fw(c, 1.5f), new Vector2(c.F * 11f, d * 4.5f), 2.8f, 1.3f); s.Pierce = true; s.Life = 1f; s.Radius = 1.2f; s.Trail = "burst_o"; c.Fire(s); } },
                // Moon Slash: a moon rises over the foe, then a violet blade falls
                c => { var l = c.Near(10f, 1); Vector2 at = l.Count > 0 ? (Vector2)l[0].transform.position : c.Feet + Fw(c, 5f);
                       Vfx.Pop("moon", at + Vector2.up * 6f, 3.2f, 0.6f, 1.1f, 0, 0, "p", 30, 0.9f);
                       c.After(0.55f, () => { Vfx.Slash(at + Vector2.up * 1f, c.F, 6f, "p", -90f, 0.35f); Vfx.Pillar(at, 6f, "pillar_shadow", 0.5f, 2f); c.Circle(at + Vector2.up, 2.4f, 2.6f); c.Shake(0.15f); }); },
            },
            // Iron Bulwark
            new Recipe[] {
                // Spiked Aegis: spikes burst out of the shield twice a second
                c => { for (int i = 0; i < 6; i++) { int k = i; c.After(0.5f + k * 0.6f, () => { c.Refresh(); Vfx.Pop("cross_o", c.P, 4f, 0.35f, 1.4f, k * 30f); Vfx.Ring(c.Feet, 2.4f, "y", 0.3f); c.Circle(c.P, 2.4f, 0.7f); }); } },
                // Guardian Spirit: a knight spirit rises beside you and slams twice
                c => { c.Refresh(); Vector2 at = c.Feet + Fw(c, 2.2f); Vfx.PopWH("knight", at + Vector2.up * 1.4f, 3.4f * c.F, 3.4f, 0.55f, 1f, 0, 0, "y", 8, 0.75f);
                       Vfx.PopWH("knight", at + Vector2.up * 1.4f, 3.4f * c.F, 3.4f, 3f, 1f, 0, 0, "y", 8, 0.55f);
                       for (int i = 0; i < 2; i++) { int k = i; c.After(0.8f + k * 1.1f, () => { Vfx.Ring(at, 3f, "y", 0.4f); Vfx.Pillar(at, 4f, "pillar_gold", 0.4f, 2f); c.Circle(at + Vector2.up, 3f, 2f); c.Shake(0.15f); }); } },
            },
            // Blade Rain
            new Recipe[] {
                // Sword Garden: swords stay planted in the ground and keep cutting
                c => { for (int i = 0; i < 6; i++) { float x = c.Feet.x + c.F * (2f + i * 1.3f); var g = new Vector2(x, c.GroundY(x, c.Feet.y + 1f));
                           Vfx.PopWH("sword_gold", g + Vector2.up * 0.9f, 1.5f, 1.8f, 3f, 1f, 0, 0, null, 24, 0.85f); }
                       c.Caster.Zone(c, c.Feet + Fw(c, 5.5f), 3.8f, 3f, 0.5f, 0.5f, "rune_gold", 6f, "o", 0f, 0f, 0f, false, 0f); },
                // Sword Storm: a second, wider storm of swords
                c => { for (int i = 0; i < 14; i++) { float x = c.P.x + c.F * (1f + i * 0.9f) + Random.Range(-0.6f, 0.6f); Drop(c, "sword_gold", new Vector2(x, c.GroundY(x, c.P.y)), 1.2f, 0.9f, 1.2f, "y", 9f, true, i * 0.07f); } },
            },
            // Chain Pull
            new Recipe[] {
                // Chain Snare: chains lock everything in light cages
                c => { foreach (var e in c.Near(10f, 6)) { e.Stun(1.8f, "y"); Vfx.Pop("lightcage", e.Mid, 2.2f, 1.8f, 1f, 0, 0, null, 33, 0.8f); Vfx.Beam(c.P, e.Mid, "bolt_gold", 0.6f, 0.25f); e.TakeDamage(c.D(0.8f), c.F, 0); } },
                // Hook Reel: a huge hook reels the strongest enemy in
                c => { var l = c.Near(11f, 6); Enemy best = null; foreach (var e in l) if (best == null || e.Hp > best.Hp) best = e;
                       if (best == null) return; Vfx.Beam(c.P, best.Mid, "bolt_gold", 1f, 0.5f); Vfx.Pop("hook_chain", best.Mid, 2.4f, 0.5f, 1.2f); best.Pull(c.P + Fw(c, 1f, -0.5f), 0.5f);
                       var t = best; c.After(0.5f, () => { if (t == null) return; t.TakeDamage(c.D(2.4f), c.F, 0); Vfx.Ring(t.transform.position, 3f, "y", 0.4f); c.Shake(0.15f); }); },
            },
            // Thunder Charge
            new Recipe[] {
                // Storm Cage: a cage of lightning crackles where you stopped
                c => { c.Refresh(); c.Caster.Zone(c, c.P, 2.6f, 2f, 0.3f, 0.6f, "lightcage", 5f, null, 0f, 0f, 0f, false, 0f); Vfx.Pop("burst_lightning", c.P, 4f, 0.4f); },
                // Thunder Hammer: a hammer of thunder smashes down
                c => { c.Refresh(); Vector2 at = c.Feet + Fw(c, 1.5f); var v = Vfx.PopWH("hammer", at + Vector2.up * 7f, 3.6f * c.F, 3.6f, 0.3f, 1f, 0, 0, "y", 36, 1f); if (v != null) v.Vel = new Vector2(0, -24f);
                       c.After(0.3f, () => { Vfx.Pillar(at, 8f, "pillar_lightning", 0.5f, 3f); Vfx.Ring(at, 4f, "b", 0.5f); Vfx.Burst(at + Vector2.up, 5f, "b"); c.Circle(at + Vector2.up, 3.2f, 3.2f); c.Shake(0.3f); c.Stop(0.08f); }); },
            },
            // Blood Rage
            new Recipe[] {
                // Crimson Wolf: a crimson wolf charges ahead
                c => { Spirit(c, "wolf_red", c.Feet + Fw(c, 0.5f, 0.9f), 14f, 2.6f, 2.5f, 0.8f, null, 0f); },
                // Vampiric Mist: blood mist surrounds you and heals from every strike
                c => { c.Caster.Zone(c, c.P, 2.6f, 4f, 0.4f, 0.45f, "poisonwisps", 5.4f, "r", 0f, 0f, 0f, true, 15f);
                       for (int i = 1; i <= 8; i++) c.After(i * 0.5f, () => { c.Refresh(); c.Pl.Heal(1); }); },
            },
            // Titan's Wrath
            new Recipe[] {
                // Titan Footstep: a titan's foot stomps the field
                c => { Vector2 at = c.Feet + Fw(c, 3.5f); var v = Vfx.PopWH("foot", at + Vector2.up * 8f, 5f, 5f, 0.55f, 1f, 0, 0, null, 36, 1f); if (v != null) v.Vel = new Vector2(0, -15f);
                       c.After(0.55f, () => { Vfx.Ring(at, 6f, "o", 0.6f); Vfx.Burst(at + Vector2.up, 7f, "o", 0.5f); c.Circle(at + Vector2.up, 5f, 4f); c.Shake(0.4f); c.Stop(0.1f); }); },
                // Sundered Sky: the sky cracks and golden light falls in columns
                c => { Vfx.Pop("cross_o", c.Feet + Fw(c, 6f, 6f), 12f, 1f, 1.1f, 90f, 0, null, 30, 0.9f);
                       for (int i = 0; i < 6; i++) { int k = i; float x = c.Feet.x + c.F * (2f + k * 2f); c.After(0.2f + k * 0.12f, () => { Vfx.Pillar(new Vector2(x, c.GroundY(x, c.Feet.y + 1f)), 10f, "pillar_gold", 0.5f, 2.4f); c.Box(new Vector2(x, c.Feet.y + 1.5f), 2.2f, 4f, 1.8f); c.Shake(0.1f); }); } },
            },
        };
    }
}

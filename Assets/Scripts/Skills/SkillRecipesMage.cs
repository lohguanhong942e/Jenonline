using System.Collections.Generic;
using UnityEngine;

namespace Vesperhold
{
    public static partial class SkillRecipes
    {
        static readonly Recipe[] MageSk = new Recipe[]
        {
            // 1 Magic Claw: two seeking bolts spiral in
            c => {
                for (int i = 0; i < 2; i++) { int k = i; c.After(k * 0.1f, () => { c.Refresh();
                    var s = Mk(c, "orb_arcane", c.P + Fw(c, 0.8f, k == 0 ? 0.5f : -0.4f), new Vector2(c.F * 9f, k == 0 ? 4f : -4f), 1.0f, 1.1f);
                    s.Home = true; s.Turn = 5f; s.Life = 1.6f; s.Trail = "burst_p"; s.Tint = "p"; s.BoomKey = "p"; c.Fire(s); }); }
            },
            // 2 Fire Orb: slow fireball that explodes
            c => {
                var s = Mk(c, "orb_fire", c.P + Fw(c, 1f), Fw(c, 7f), 1.6f, 3f);
                s.Boom = 2.3f; s.Life = 2f; s.Radius = 0.7f; s.Trail = "burst_fire"; s.BoomKey = "o"; c.Fire(s);
            },
            // 3 Barrier: a runed dome blocks every hit
            c => { c.Pl.ShieldT = 4.5f; Vfx.Follow("shield_hex", c.Pl.transform, new Vector2(0, 0.9f), 4.2f, 4.5f, null, 0, 31); Vfx.Ring(c.Feet, 3f, "b", 0.5f); },
            // 4 Meteor: five meteors rain from the sky
            c => {
                var sp = Spots(c, 5, 12f, 2f);
                for (int i = 0; i < sp.Count; i++) Drop(c, "meteor", sp[i], 1.8f, 2.4f, 2f, "o", 11f, false, i * 0.18f, new Vector2(-7f * c.F, -20f));
            },
            // 5 Ice Lance: a fast icicle pierces everything
            c => {
                var s = Mk(c, "icespike", c.P + Fw(c, 1f), Fw(c, 20f), 2.4f, 2.4f);
                s.Pierce = true; s.Life = 0.8f; s.Radius = 0.8f; s.Align = true; s.BaseDeg = 90f; s.Trail = "burst_ice"; s.BoomKey = "b"; s.ExplodeOnWall = true; c.Fire(s);
            },
            // 6 Chain Lightning: lightning leaps between enemies
            c => {
                var l = c.Near(11f, 5); Vector2 prev = c.P;
                if (l.Count == 0) { Vfx.Beam(c.P, c.P + Fw(c, 8f), "bolt_blue", 1f, 0.2f); return; }
                for (int i = 0; i < l.Count; i++) { var e = l[i]; var src = prev; prev = e.Mid;
                    c.After(i * 0.09f, () => { if (e == null) return; Vfx.Beam(src, e.Mid, "bolt_chain", 1.2f, 0.22f); Vfx.Pop("burst_lightning", e.Mid, 2.2f, 0.3f); e.TakeDamage(c.D(1.6f), c.F, 0); }); }
            },
            // 7 Frost Nova: a freezing ring locks enemies in ice
            c => {
                Vfx.Ring(c.Feet, 5.5f, "b", 0.6f); Vfx.Burst(c.P, 5f, "b", 0.45f);
                Vfx.Sparks(c.P, 14, 6f, "snowflake", null, 0.5f, 0.7f);
                foreach (var e in c.Near(5.5f, 20)) { e.TakeDamage(c.D(1.6f), c.F, 0); e.Stun(2f, "b"); Vfx.Pop("icecluster", e.transform.position + Vector3.up * 0.7f, 1.6f, 2f, 1f, 0, 0, null, 33, 0.85f); }
                c.Shake(0.12f);
            },
            // 8 Gravity Well: a black hole drags enemies in
            c => { c.Caster.Zone(c, c.Feet + Fw(c, 5f, 1.2f), 2.8f, 2.6f, 0.3f, 0.5f, "portal", 4.4f, "k", 1f, 2f, 3.2f, false, 90f); },
            // 9 Thunderstorm: lightning strikes rain down
            c => {
                for (int i = 0; i < 9; i++) { int k = i; c.After(k * 0.15f, () => { float x = c.Feet.x + c.F * 1f + Random.Range(-7f, 7f); var g = new Vector2(x, c.GroundY(x, c.Feet.y + 2f));
                    Vfx.Pillar(g, 9f, "pillar_lightning", 0.3f, 1.6f); Vfx.Burst(g + Vector2.up * 0.5f, 2.2f, "b", 0.3f); c.Circle(g + Vector2.up * 0.5f, 1.3f, 1.7f); c.Shake(0.05f); }); }
            },
            // 10 Blink: teleport away and back with a trail
            c => {
                Vector2 a = c.Feet; Vfx.Ghost(a, c.F, "p", 0.5f); Vfx.Burst(c.P, 3f, "p", 0.3f);
                c.Pl.Blink(c.F * 6f, 0.4f); c.Refresh(); Vfx.Ghost(c.Feet, c.F, "p", 0.35f, 0.4f); Vfx.Burst(c.P, 3f, "p", 0.3f); Vfx.Beam(a + Vector2.up * 0.8f, c.P, "bolt_purple", 0.9f, 0.3f);
            },
            // 11 Mana Surge: orbiting orbs zap nearby enemies
            c => { c.Caster.Orbit(c, "orb_arcane", 4, 1.8f, 5f, 0.4f, 1f, "p", 0.9f, 5f); Vfx.Ring(c.Feet, 2.5f, "p", 0.4f); },
            // 12 Arcane Cataclysm: a rune circle, a beam, a huge blast
            c => {
                Vector2 at = c.Feet + Fw(c, 5f);
                Vfx.PopWH("rune_purple", at + Vector2.up * 0.1f, 9f, 3f, 0.9f, 1f, 0, 30f, null, 25, 0.95f);
                c.After(0.6f, () => { Vfx.Pillar(at, 12f, "pillar_arcane", 0.7f, 5f); Vfx.Burst(at + Vector2.up, 9f, "p", 0.6f); Vfx.Ring(at, 7f, "p", 0.7f);
                    c.Box(at + Vector2.up * 2f, 9f, 6f, 7f); c.Shake(0.45f); c.Stop(0.12f); });
            },
        };

        static readonly Recipe[][] MageUp = new Recipe[][]
        {
            // Magic Claw
            new Recipe[] {
                // Arcane Constellation: stars appear above the targets and link into a constellation
                c => { var l = c.Near(10f, 5); Vector2 prev = Vector2.zero; bool first = true;
                       foreach (var e in l) { var t = e; Vector2 sp = t.Mid + Vector2.up * 1.8f; Vfx.Pop("burst_p", sp, 1.2f, 0.7f, 1.3f, 0, 60f);
                           if (!first) Vfx.Beam(prev, sp, "bolt_purple", 0.5f, 0.6f); prev = sp; first = false;
                           c.After(0.5f, () => { if (t == null) return; Vfx.Beam(sp, t.Mid, "bolt_purple", 0.7f, 0.2f); t.TakeDamage(c.D(1f), c.F, 0); Vfx.Burst(t.Mid, 1.6f, "p", 0.25f); }); } },
                // Seeking Butterflies: glowing butterflies flutter out and burst on the targets
                c => { for (int i = 0; i < 5; i++) { var s = Mk(c, "butterfly", c.P + new Vector2(0, 0.3f * i), new Vector2(c.F * 4f, 2f + i * 0.8f), 1.1f, 1f);
                           s.Home = true; s.Turn = 3f; s.Life = 2.2f; s.Spin = 0f; s.Trail = "burst_p"; s.BoomKey = "p"; s.Tint = null; s.Boom = 1.1f; c.Fire(s); } },
            },
            // Fire Orb
            new Recipe[] {
                // Phoenix Rebirth: the fireball flares into a phoenix that sweeps ahead
                c => { Spirit(c, "phoenix", c.P + Fw(c, 2f, 0.4f), 13f, 4f, 2.5f, 1.1f); Vfx.Burst(c.P + Fw(c, 2f), 4f, "o"); },
                // Miniature Sun: a tiny sun burns where the orb landed
                c => { c.Caster.Zone(c, c.Feet + Fw(c, 6f, 1.8f), 2.6f, 3f, 0.3f, 0.7f, "sun", 4.4f, null, 0f, 3f, 3.4f, false, 25f); },
            },
            // Barrier
            new Recipe[] {
                // Mirror Reflect: the dome pulses and throws the damage back
                c => { for (int i = 0; i < 3; i++) { int k = i; c.After(k * 0.5f, () => { c.Refresh(); Vfx.Ring(c.Feet, 4f, "b", 0.4f); Vfx.Pop("cross_b", c.P, 5f, 0.4f, 1.3f, 45f); c.Circle(c.P, 4f, 1.2f); }); } },
                // Crystal Bloom: crystals bloom around you and shatter
                c => { for (int i = 0; i < 6; i++) { float a = i * 60f * Mathf.Deg2Rad; var at = c.Feet + new Vector2(Mathf.Cos(a) * 2.6f, 0.1f);
                           Vfx.PopWH("crystal_blue", at + Vector2.up * 0.9f, 1.6f, 2f, 0.9f, 1.1f, 0, 0, null, 24, 0.9f); }
                       c.After(0.7f, () => { c.Refresh(); Vfx.Burst(c.P, 6f, "b", 0.5f); Vfx.Sparks(c.P, 14, 6f, "snowflake", null, 0.5f, 0.6f); c.Circle(c.P, 3.6f, 1.8f); c.Shake(0.12f); }); },
            },
            // Meteor
            new Recipe[] {
                // Comet Shower: a swarm of smaller comets
                c => { for (int i = 0; i < 9; i++) { float x = c.Feet.x + c.F * (1f + i * 1.1f) + Random.Range(-0.5f, 0.5f); Drop(c, "comet", new Vector2(x, c.GroundY(x, c.Feet.y + 2f)), 1.2f, 1.2f, 1.3f, "o", 10f, false, i * 0.1f, new Vector2(-9f * c.F, -22f)); } },
                // Lava Craters: molten craters keep burning
                c => { var sp = Spots(c, 4, 12f, 2.2f); foreach (var s in sp) { Vfx.PopWH("lavacrack", s + Vector2.up * 0.15f, 3f, 1.1f, 2.4f, 1f, 0, 0, null, 26, 0.9f); c.Caster.Zone(c, s + Vector2.up * 0.6f, 1.6f, 2.4f, 0.4f, 0.5f, "lavaspire", 2.2f, null, 0f, 0f, 0f, false, 0f); } },
            },
            // Ice Lance
            new Recipe[] {
                // Frozen Spikes: ice spikes erupt along the lance's path
                c => { for (int i = 0; i < 7; i++) { float x = c.Feet.x + c.F * (1.8f + i * 1.3f); Erupt(c, "icespike", new Vector2(x, c.GroundY(x, c.Feet.y + 1f)), 1.2f, 2.4f, 1.2f, i * 0.06f, 0.9f, "b"); } },
                // Snowflake Fractal: snowflakes spin outward in a ring
                c => { c.Refresh(); for (int i = 0; i < 8; i++) { float a = i * 45f * Mathf.Deg2Rad; var s = Mk(c, "snowflake", c.P, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 7f, 1.3f, 1f);
                           s.Spin = 240f; s.Pierce = true; s.Life = 0.7f; s.Radius = 0.8f; s.BoomKey = "b"; s.ExplodeOnWall = false; c.Fire(s); } Vfx.Ring(c.Feet, 3f, "b", 0.4f); },
            },
            // Chain Lightning
            new Recipe[] {
                // Tesla Web: lightning links every enemy into a web
                c => { var l = c.Near(11f, 6); for (int i = 0; i < l.Count; i++) for (int j = i + 1; j < l.Count; j++) Vfx.Beam(l[i].Mid, l[j].Mid, "bolt_blue", 0.5f, 0.8f);
                       foreach (var e in l) { var t = e; Vfx.Pop("lightcage", t.Mid, 1.8f, 0.8f); for (int k = 1; k <= 3; k++) c.After(k * 0.25f, () => { if (t != null) t.TakeDamage(c.D(0.8f), c.F, 0); }); } },
                // Sky Serpent: a serpent of lightning swims across the sky
                c => { Spirit(c, "dragon", c.P + Fw(c, 1f, 0.5f), 12f, 3.4f, 2.2f, 1.1f, "b"); },
            },
            // Frost Nova
            new Recipe[] {
                // Glacier Wall: a wall of ice rises in front
                c => { for (int i = 0; i < 4; i++) { float x = c.Feet.x + c.F * (3f + i * 0.9f); Erupt(c, "icecluster", new Vector2(x, c.GroundY(x, c.Feet.y + 1f)), 2f, 3.4f, 1.3f, i * 0.07f, 1.5f, "b"); } },
                // Diamond Dust: glittering ice dust falls and keeps cutting
                c => { c.Caster.Zone(c, c.P, 5f, 3f, 0.4f, 0.4f, "snowflake", 9f, null, 0f, 0f, 0f, false, 12f); Vfx.Sparks(c.P, 18, 5f, "snowflake", null, 0.5f, 1.5f, -2f); },
            },
            // Gravity Well
            new Recipe[] {
                // Satellite Orbit: planets circle the well and strike
                c => { var at = c.Feet + Fw(c, 5f, 1.2f); for (int i = 0; i < 2; i++) { var v = Vfx.Pop("planet", at, 1.3f, 2.6f, 1f, 0, 0, null, 31, 0.95f); }
                       c.Caster.Zone(c, at, 3.4f, 2.6f, 0.35f, 0.6f, "orb_arcane", 2f, "p", 0f, 0f, 0f, false, 200f); },
                // Event Horizon: the well collapses in a huge blast
                c => { var at = c.Feet + Fw(c, 5f, 1.2f); Vfx.Pop("portal", at, 7f, 0.8f, 0.4f, 0, 200f, "k"); c.After(0.8f, () => { Vfx.Ring(at, 7f, "p", 0.6f); Vfx.Burst(at, 9f, "p", 0.5f); c.Circle(at, 6f, 4f); c.Shake(0.35f); c.Stop(0.1f); }); },
            },
            // Thunderstorm
            new Recipe[] {
                // Eye of the Storm: a tornado of lightning circles you
                c => { c.Caster.Zone(c, c.P, 3f, 3f, 0.3f, 0.7f, "tornado", 5f, "b", 0f, 0f, 0f, true, 0f); },
                // Ball Lightning: three balls of lightning bounce along the ground
                c => { for (int i = 0; i < 3; i++) { var s = Mk(c, "orb_lightning", c.P + Fw(c, 1f, 0.3f * i), new Vector2(c.F * (7f + i), 4f), 1.4f, 1.5f); s.Grav = 14f; s.Pierce = true; s.Life = 1.6f; s.Radius = 0.9f; s.Spin = 200f; s.ExplodeOnWall = false; s.BoomKey = "b"; c.Fire(s); } },
            },
            // Blink
            new Recipe[] {
                // Mirror Image: purple images slash where you were
                c => { c.Refresh(); for (int i = 0; i < 3; i++) { int k = i; c.After(0.15f + k * 0.2f, () => { var at = c.Feet + Fw(c, 1.5f + k * 1.3f); Vfx.Ghost(at, c.F, "p", 0.5f); Vfx.Slash(at + Vector2.up * 0.8f, c.F, 3f, "p"); c.Circle(at + Vector2.up * 0.8f, 1.8f, 1.2f); }); } },
                // Portal Barrage: portals open and fire bolts
                c => { var l = c.Near(11f, 3); for (int s = 0; s < 2; s++) { Vector2 pp = c.P + new Vector2((s == 0 ? -1 : 1) * 2.5f, 1.5f); Vfx.Pop("portal", pp, 2.4f, 1.6f, 1f, 0, 120f, "p", 30, 0.9f);
                           for (int k = 0; k < 3; k++) { int kk = k; c.After(0.3f + kk * 0.3f, () => { var shot = Mk(c, "orb_arcane", pp, (l.Count > 0 && l[kk % l.Count] != null ? (l[kk % l.Count].Mid - pp).normalized : new Vector2(c.F, 0)) * 11f, 0.9f, 1.1f); shot.Life = 1.2f; shot.BoomKey = "p"; shot.Boom = 1f; c.Fire(shot); }); } } },
            },
            // Mana Surge
            new Recipe[] {
                // Halo of Runes: a halo of runes spins over you
                c => { c.Caster.Zone(c, c.P + Vector2.up * 1.4f, 3f, 4f, 0.4f, 0.5f, "rune_gold", 4.4f, null, 0f, 0f, 0f, true, 90f); },
                // Overcharge: the orbs overload and strike everyone
                c => { foreach (var e in c.Near(9f, 8)) { Vfx.Beam(c.P, e.Mid, "bolt_chain", 1f, 0.25f); Vfx.Pillar(e.transform.position, 6f, "pillar_arcane", 0.4f, 1.6f); e.TakeDamage(c.D(1.5f), c.F, 0); } Vfx.Ring(c.Feet, 4f, "p", 0.5f); c.Shake(0.15f); },
            },
            // Arcane Cataclysm
            new Recipe[] {
                // Planetary Alignment: planets align over the field and fire a beam
                c => { Vector2 at = c.Feet + Fw(c, 5f); for (int i = 0; i < 3; i++) Vfx.Pop("planet", at + new Vector2((i - 1) * 2.8f, 7f), 2.2f, 1.3f, 1f, 0, 0, i == 1 ? "p" : "b", 30, 0.95f);
                       c.After(0.9f, () => { Vfx.Pillar(at, 13f, "pillar_arcane", 0.6f, 4f); Vfx.Ring(at, 6f, "p", 0.6f); c.Box(at + Vector2.up * 2f, 8f, 6f, 3.5f); c.Shake(0.3f); }); },
                // Cosmic Rift: the sky tears open and drags everything in
                c => { var at = c.Feet + Fw(c, 5f, 2f); c.Caster.Zone(c, at, 3.4f, 2.2f, 0.3f, 0.6f, "portal", 7f, "p", 1.5f, 3.5f, 4.2f, false, 120f); },
            },
        };
    }
}

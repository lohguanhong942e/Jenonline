using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Vesperhold
{
    // Everything a recipe needs: where the hero is, damage helpers and scheduling.
    public class SkillCtx
    {
        public PlayerController Pl; public SkillCaster Caster;
        public Vector2 P, Feet; public int F, A; public string Ck;

        public SkillCtx(SkillCaster caster)
        {
            Caster = caster; Pl = caster.Player; Ck = caster.ColorKey;
            Refresh(); F = Pl.Facing;
        }

        // Re-read the hero position (for follow-ups that fire after the hero moved).
        public void Refresh() { Feet = Pl.transform.position; P = Feet + Vector2.up * 0.8f; A = GameState.Attack; }
        public int D(float mult) { return Mathf.Max(1, Mathf.RoundToInt(A * mult)); }
        public int Circle(Vector2 at, float r, float mult) { return Combat.Circle(at, r, D(mult), F); }
        public int Box(Vector2 at, float w, float h, float mult) { return Combat.Box(at, new Vector2(w, h), D(mult), F); }
        public List<Enemy> Near(float range, int max = 99) { return Combat.Near(P, range, max); }
        public void After(float t, Action a) { Caster.Later(t, a); }
        public void Shake(float s) { GameManager.Instance.Shake(s); }
        public void Stop(float t) { GameManager.Instance.HitStop(t); }
        public void Fire(ProjSpec s) { s.Facing = F; SkillProjectile.Fire(s); }

        // First solid ceiling above a point (so falling skills never start inside a wall).
        public float CeilY(Vector2 from, float max)
        {
            foreach (var h in Physics2D.RaycastAll(from + Vector2.up * 0.3f, Vector2.up, max))
            {
                if (h.collider.isTrigger || h.collider.usedByEffector) continue;
                if (h.collider.GetComponentInParent<Enemy>() != null || h.collider.GetComponentInParent<PlayerController>() != null) continue;
                return h.point.y - 0.4f;
            }
            return from.y + max;
        }

        public float GroundY(float x, float fromY)
        {
            foreach (var h in Physics2D.RaycastAll(new Vector2(x, fromY), Vector2.down, 30f))
            {
                if (h.collider.isTrigger || h.collider.usedByEffector) continue;
                if (h.collider.GetComponentInParent<Enemy>() != null || h.collider.GetComponentInParent<PlayerController>() != null) continue;
                return h.point.y;
            }
            return Feet.y;
        }
    }

    public delegate void Recipe(SkillCtx c);

    // Reads the class loadout, handles the 1-0,-,= keys, cooldowns, mana, level unlocks and the
    // two level-gated follow-up upgrades per skill, and draws the skill bar.
    public class SkillCaster : MonoBehaviour
    {
        public static SkillCaster Instance { get; private set; }
        public PlayerController Player { get; private set; }
        public string ColorKey { get; private set; }
        public string ClassId { get; private set; }
        SkClass cls; Recipe[] skills; Recipe[][] ups;
        readonly float[] cd = new float[12], cdMax = new float[12];
        float atkT, atkMul = 1f, hasteT;
        readonly Dictionary<string, Texture2D> icons = new Dictionary<string, Texture2D>();
        static readonly Key[] Keys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9, Key.Digit0, Key.Minus, Key.Equals };
        static readonly string[] KeyLabels = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "-", "=" };
        string flash = ""; float flashT;

        public static SkillCaster Attach(PlayerController p, string classId)
        {
            var old = p.GetComponent<SkillCaster>(); if (old != null) Destroy(old);
            var c = p.gameObject.AddComponent<SkillCaster>();
            c.Setup(p, classId);
            return c;
        }

        void Setup(PlayerController p, string classId)
        {
            Instance = this; Player = p; ClassId = classId;
            cls = SkillCatalog.Get(classId);
            ColorKey = classId == "warrior" ? "o" : classId == "mage" ? "b" : "g";
            skills = SkillRecipes.Skills(classId); ups = SkillRecipes.Upgrades(classId);
            p.ApplyClassArt(classId);
        }

        void OnDestroy() { if (Instance == this) { Instance = null; GameState.AtkBuff = 1f; } }

        public void Later(float t, Action a) { StartCoroutine(Delay(t, a)); }
        IEnumerator Delay(float t, Action a)
        {
            while (t > 0) { if (!GameManager.Instance.Paused) t -= Time.deltaTime; yield return null; }
            if (this != null && Player != null && !Player.Dead) a();
        }

        // Timed buffs.
        public void Buff(string kind, float mult, float dur)
        {
            if (kind == "atk") { atkMul = Mathf.Max(atkMul, mult); atkT = dur; GameState.AtkBuff = atkMul; }
            else if (kind == "haste") { hasteT = dur; Player.SpeedMul = mult; }
        }

        public bool Unlocked(int i) { return GameState.Level >= cls.skills[i].unlockLevel; }
        public bool UpgradeOn(int i, int k) { return Unlocked(i) && GameState.Level >= cls.skills[i].upgrades[k].unlockLevel; }

        void Update()
        {
            if (GameManager.Instance.Paused || Player.Dead) return;
            float dt = Time.deltaTime;
            for (int i = 0; i < 12; i++) if (cd[i] > 0) cd[i] -= dt;
            if (atkT > 0) { atkT -= dt; if (atkT <= 0) { atkMul = 1f; GameState.AtkBuff = 1f; } }
            if (hasteT > 0) { hasteT -= dt; if (hasteT <= 0) Player.SpeedMul = 1f; }
            if (flashT > 0) flashT -= dt;
            var kb = Keyboard.current; if (kb == null) return;
            for (int i = 0; i < 12; i++) if (kb[Keys[i]].wasPressedThisFrame) Cast(i);
        }

        void Cast(int i)
        {
            var def = cls.skills[i];
            if (!Unlocked(i)) { Say("Unlocks at level " + def.unlockLevel); return; }
            if (cd[i] > 0) return;
            int cost = SkillCatalog.ManaCost(i);
            if (Player.Mp < cost) { Say("Not enough mana"); return; }
            Player.Mp -= cost; cd[i] = cdMax[i] = def.cooldown * 1.6f;     // slightly longer than the lab so MP and cooldown both matter
            var c = new SkillCtx(this);
            FloatingText.Spawn(Player.transform.position + Vector3.up * 2.1f, def.name, Vfx.C(ColorKey));
            Sfx.Play(ClassId == "warrior" ? "cast_fire" : ClassId == "rogue" ? "cast_shadow" : i % 3 == 0 ? "cast_ice" : i % 3 == 1 ? "cast_bolt" : "cast_holy", 0.7f, UnityEngine.Random.Range(0.95f, 1.05f));
            skills[i](c);
            for (int k = 0; k < 2; k++)
            {
                if (!UpgradeOn(i, k)) continue;
                var up = ups[i][k];
                Later(def.followUpDelay + k * 0.5f, () => { var c2 = new SkillCtx(this); c2.F = c.F; up(c2); });
            }
        }

        void Say(string s) { flash = s; flashT = 1.2f; }

        // ---- reusable effects ----
        // Damage zone with painted sprite. pull drags enemies toward the centre.
        public void Zone(SkillCtx c, Vector2 pos, float radius, float dur, float tick, float mult, string spr, float size, string tint,
            float pull = 0f, float endMult = 0f, float endR = 0f, bool follow = false, float spin = 30f)
        {
            StartCoroutine(ZoneRoutine(c, pos, radius, dur, tick, mult, spr, size, tint, pull, endMult, endR, follow, spin));
        }

        IEnumerator ZoneRoutine(SkillCtx c, Vector2 pos, float radius, float dur, float tick, float mult, string spr, float size, string tint,
            float pull, float endMult, float endR, bool follow, float spin)
        {
            var fx = Vfx.Pop(spr, pos, size, dur, 1f, 0, spin, tint, 29, 0.85f);
            if (fx != null) { fx.FadeTail = 0.3f; fx.FadeHead = 0.12f; if (follow) { fx.Target = Player.transform; fx.Offset = pos - (Vector2)Player.transform.position; } }
            float t = 0, tk = 0; Vector2 off = pos - (Vector2)Player.transform.position;
            while (t < dur)
            {
                yield return null;
                if (GameManager.Instance.Paused) continue;
                float dt = Time.deltaTime; t += dt; tk += dt;
                if (follow && Player != null) pos = (Vector2)Player.transform.position + off;
                if (pull > 0f) foreach (var e in Combat.Near(pos, radius * 1.4f, 20)) e.Pull(pos, 0.15f);
                if (tk >= tick)
                {
                    tk = 0;
                    if (Combat.Circle(pos, radius, c.D(mult), c.F) > 0) Vfx.Burst(pos, radius * 0.9f, Vfx.Key(tint ?? c.Ck), 0.25f);
                }
            }
            if (endMult > 0f)
            {
                Combat.Circle(pos, endR, c.D(endMult), c.F);
                Vfx.Burst(pos, endR * 2.4f, Vfx.Key(tint ?? c.Ck), 0.45f); Vfx.Ring(pos, endR, Vfx.Key(tint ?? c.Ck), 0.5f);
                GameManager.Instance.Shake(0.18f);
            }
        }

        // Sprites circling the hero that zap the nearest enemy.
        public void Orbit(SkillCtx c, string spr, int count, float radius, float dur, float zapEvery, float mult, string tint, float size = 0.9f, float zapRange = 5f)
        {
            StartCoroutine(OrbitRoutine(c, spr, count, radius, dur, zapEvery, mult, tint, size, zapRange));
        }

        IEnumerator OrbitRoutine(SkillCtx c, string spr, int count, float radius, float dur, float zapEvery, float mult, string tint, float size, float zapRange)
        {
            var fx = new List<VfxSprite>();
            for (int i = 0; i < count; i++)
            {
                var v = Vfx.Pop(spr, Player.transform.position, size, dur, 1f, 0, 120f, tint, 31, 0.95f);
                if (v != null) { v.Target = Player.transform; v.FadeTail = 0.25f; v.FadeHead = 0.1f; fx.Add(v); }
            }
            float t = 0, zap = 0;
            while (t < dur)
            {
                yield return null;
                if (GameManager.Instance.Paused) continue;
                t += Time.deltaTime; zap += Time.deltaTime;
                for (int i = 0; i < fx.Count; i++)
                {
                    if (fx[i] == null) continue;
                    float a = t * 3.4f + i * Mathf.PI * 2f / fx.Count;
                    fx[i].Offset = new Vector2(Mathf.Cos(a) * radius, 0.8f + Mathf.Sin(a) * radius * 0.45f);
                }
                if (zap >= zapEvery && Player != null)
                {
                    zap = 0; c.Refresh();
                    var near = Combat.Near(c.P, zapRange, 1);
                    if (near.Count > 0)
                    {
                        Vfx.Beam(c.P, near[0].Mid, "bolt_" + (tint == "g" ? "green" : tint == "o" ? "gold" : tint == "p" ? "purple" : "blue"), 0.7f, 0.15f);
                        near[0].TakeDamage(c.D(mult), c.F, 0); Vfx.Burst(near[0].Mid, 1.2f, Vfx.Key(tint ?? "b"), 0.2f);
                    }
                }
            }
        }

        // ---- HUD ----
        Texture2D Icon(string path)
        {
            Texture2D t;
            if (!icons.TryGetValue(path, out t)) { t = Resources.Load<Texture2D>(path); icons[path] = t; }
            return t;
        }

        void OnGUI() { UiScale.Begin(); Draw(); UiScale.End(); }

        void Draw()
        {
            if (cls == null || GameManager.Instance == null || GameManager.Instance.InMenu) return;
            float slot = Mathf.Clamp(UiScale.W / 17f, 40f, 58f), gap = 5f;
            float total = 12 * slot + 11 * gap, x0 = (UiScale.W - total) / 2f, y = UiScale.H - UiScale.B - slot - 22f;
            var small = new GUIStyle(GUI.skin.label) { fontSize = 11, alignment = TextAnchor.UpperLeft, fontStyle = FontStyle.Bold };
            var mid = new GUIStyle(GUI.skin.label) { fontSize = 12, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            for (int i = 0; i < 12; i++)
            {
                var def = cls.skills[i];
                var r = new Rect(x0 + i * (slot + gap), y, slot, slot);
                bool on = Unlocked(i);
                GUI.color = new Color(0, 0, 0, 0.7f); GUI.DrawTexture(new Rect(r.x - 2, r.y - 2, r.width + 4, r.height + 4), Texture2D.whiteTexture); GUI.color = Color.white;
                var frameArt = UiArt.Tex("slot_" + (i < 3 ? "common" : i < 6 ? "uncommon" : i < 9 ? "rare" : i < 11 ? "epic" : "legendary"));
                if (frameArt != null) GUI.DrawTexture(new Rect(r.x - 5, r.y - 5, r.width + 10, r.height + 10), frameArt, ScaleMode.StretchToFill);
                var ic = Icon(def.icon);
                GUI.color = on ? Color.white : new Color(0.3f, 0.3f, 0.35f, 1f);
                var inner = new Rect(r.x + r.width * 0.09f, r.y + r.height * 0.09f, r.width * 0.82f, r.height * 0.82f);
                if (ic != null) GUI.DrawTexture(inner, ic, ScaleMode.ScaleToFit); else { GUI.color = on ? Vfx.C(ColorKey) : Color.gray; GUI.DrawTexture(r, Texture2D.whiteTexture); }
                if (on && cd[i] > 0 && cdMax[i] > 0)
                {
                    GUI.color = new Color(0, 0, 0, 0.65f);
                    GUI.DrawTexture(new Rect(r.x, r.y, r.width, r.height * Mathf.Clamp01(cd[i] / cdMax[i])), Texture2D.whiteTexture);
                }
                GUI.color = Color.white;
                GUI.Label(new Rect(r.x + 3, r.y + 1, 20, 16), KeyLabels[i], small);
                if (GUI.Button(r, GUIContent.none, GUIStyle.none)) Cast(i);
                if (!on) GUI.Label(r, "Lv" + def.unlockLevel, mid);
                else
                {
                    for (int k = 0; k < 2; k++)
                    {
                        GUI.color = UpgradeOn(i, k) ? new Color(1f, 0.85f, 0.35f) : new Color(0.25f, 0.25f, 0.3f);
                        GUI.DrawTexture(new Rect(r.xMax - 12 + k * 0f, r.yMax - 12 - k * 8f, 7, 6), Texture2D.whiteTexture);
                    }
                    GUI.color = Player.Mp >= SkillCatalog.ManaCost(i) ? new Color(0.5f, 0.75f, 1f) : new Color(1f, 0.4f, 0.4f);
                    GUI.Label(new Rect(r.x + 3, r.yMax - 15, 24, 14), SkillCatalog.ManaCost(i).ToString(), small);
                    GUI.color = Color.white;
                }
            }
            if (flashT > 0) GUI.Label(new Rect(0, y - 30, UiScale.W, 24), flash, new GUIStyle(mid) { fontSize = 16 });
        }
    }
}

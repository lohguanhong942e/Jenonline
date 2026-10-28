using System;
using System.Collections.Generic;
using UnityEngine;

namespace Vesperhold
{
    // ---- data (Assets/Resources/Data/skills.json, exported from the Skill Lab) ----
    [Serializable] public class SkUpgrade { public int tier, unlockLevel; public string name, desc, icon; }
    [Serializable]
    public class SkSkill
    {
        public int index, unlockLevel; public string name, desc, icon; public float cooldown, followUpDelay;
        public SkUpgrade[] upgrades;
    }
    [Serializable] public class SkClass { public string id, name, color; public SkSkill[] skills; }
    [Serializable] public class SkFile { public int[] unlockLevels, upgradeLevelOffsets; public SkClass[] classes; }

    public static class SkillCatalog
    {
        static SkFile file;
        public static SkClass Get(string classId)
        {
            if (file == null) file = JsonUtility.FromJson<SkFile>(Resources.Load<TextAsset>("Data/skills").text);
            foreach (var c in file.classes) if (c.id == classId) return c;
            return file.classes[0];
        }

        // Mana cost grows with the skill slot: 3 for the opener up to about 27 for the ultimate.
        public static int ManaCost(int slot) { return slot == 0 ? 3 : Mathf.RoundToInt(4f + slot * 2.1f); }
    }

    // ---- damage helpers shared by every recipe ----
    public static class Combat
    {
        // Cached once per frame: dozens of projectiles and zones ask for the enemy list every frame.
        static readonly List<Enemy> cache = new List<Enemy>(); static int cacheFrame = -1;
        public static List<Enemy> All()
        {
            if (cacheFrame != Time.frameCount)
            {
                cacheFrame = Time.frameCount; cache.Clear();
                foreach (var e in UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None)) if (e != null) cache.Add(e);
            }
            var list = new List<Enemy>(cache.Count);
            foreach (var e in cache) if (e != null && e.Hp > 0) list.Add(e);
            return list;
        }

        public static List<Enemy> Near(Vector2 from, float range, int max)
        {
            var list = new List<Enemy>();
            foreach (var e in All()) if (Vector2.Distance(e.Mid, from) <= range) list.Add(e);
            list.Sort((a, b) => Vector2.Distance(a.Mid, from).CompareTo(Vector2.Distance(b.Mid, from)));
            if (list.Count > max) list.RemoveRange(max, list.Count - max);
            return list;
        }

        public static int Circle(Vector2 c, float r, int dmg, int kbDir, HashSet<Enemy> already = null)
        {
            int n = 0;
            foreach (var e in All())
            {
                if (already != null && already.Contains(e)) continue;
                // Use the closest point of the enemy's collider so big bosses count as hit when touched.
                var col = e.GetComponent<Collider2D>();
                Vector2 q = col != null ? (Vector2)col.ClosestPoint(c) : e.Mid;
                if (Vector2.Distance(q, c) > r) continue;
                if (already != null) already.Add(e);
                e.TakeDamage(dmg, kbDir, 0); n++;
            }
            return n;
        }

        public static int Box(Vector2 c, Vector2 size, int dmg, int kbDir, HashSet<Enemy> already = null)
        {
            int n = 0;
            var r = new Rect(c - size * 0.5f, size);
            foreach (var e in All())
            {
                if (already != null && already.Contains(e)) continue;
                var col = e.GetComponent<Collider2D>();
                var b = col != null ? col.bounds : new Bounds(e.Mid, Vector3.one);
                var er = new Rect(b.min.x, b.min.y, b.size.x, b.size.y);
                if (!r.Overlaps(er)) continue;
                if (already != null) already.Add(e);
                e.TakeDamage(dmg, kbDir, 0); n++;
            }
            return n;
        }
    }

    // ---- flying skill bodies ----
    public class ProjSpec
    {
        public string Spr, Tint, Trail, BoomSpr = "burst_o", BoomKey = "o";
        public Vector2 Pos, Vel;
        public float Size = 1f, Life = 1.5f, Grav, Radius = 0.5f, Boom, Spin, BaseDeg, Turn = 6f, Stun;
        public int Dmg, Facing = 1;
        public bool Pierce, Home, Align, ExplodeOnWall = true;
        public Action<Vector2> OnEnd;
    }

    public class SkillProjectile : MonoBehaviour
    {
        ProjSpec s; SpriteRenderer sr; float t, trailT, rot; readonly HashSet<Enemy> hit = new HashSet<Enemy>();

        public static SkillProjectile Fire(ProjSpec spec)
        {
            var spr = Vfx.Get(spec.Spr);
            var go = new GameObject("Proj_" + spec.Spr);
            go.transform.position = spec.Pos;
            var p = go.AddComponent<SkillProjectile>();
            p.s = spec;
            if (spr != null)
            {
                p.sr = go.AddComponent<SpriteRenderer>();
                p.sr.sprite = spr; p.sr.sharedMaterial = Vfx.Additive; p.sr.sortingOrder = 35;
                p.sr.color = spec.Tint == null ? Color.white : Vfx.C(spec.Tint);
                go.transform.localScale = new Vector3(spec.Size * (spec.Align ? 1 : spec.Facing), spec.Size, 1);
            }
            return p;
        }

        void Update()
        {
            if (GameManager.Instance.Paused) return;
            float dt = Time.deltaTime; t += dt;
            Vector2 pos = transform.position;
            if (s.Home)
            {
                var tg = Combat.Near(pos, 10f, 1);
                if (tg.Count > 0)
                {
                    float sp = s.Vel.magnitude;
                    var want = (tg[0].Mid - pos).normalized * sp;
                    s.Vel = Vector2.Lerp(s.Vel, want, Mathf.Clamp01(s.Turn * dt));
                }
            }
            s.Vel.y -= s.Grav * dt;
            pos += s.Vel * dt;
            transform.position = pos;
            if (sr != null)
            {
                rot += s.Spin * dt;
                float ang = s.Align ? Mathf.Atan2(s.Vel.y, s.Vel.x) * Mathf.Rad2Deg - s.BaseDeg : 0f;
                transform.rotation = Quaternion.Euler(0, 0, ang + rot);
            }
            if (s.Trail != null)
            {
                trailT -= dt;
                if (trailT <= 0) { trailT = 0.05f; Vfx.Pop(s.Trail, pos, s.Size * 0.7f, 0.3f, 0.5f, 0, 0, s.Tint, 30, 0.7f); }
            }
            // Enemies.
            foreach (var e in Combat.All())
            {
                if (hit.Contains(e)) continue;
                var col = e.GetComponent<Collider2D>();
                Vector2 q = col != null ? (Vector2)col.ClosestPoint(pos) : e.Mid;
                if (Vector2.Distance(q, pos) > s.Radius) continue;
                hit.Add(e);
                if (s.Boom > 0f) { End(true); return; }
                e.TakeDamage(s.Dmg, s.Vel.x >= 0 ? 1 : -1, 0);
                if (s.Stun > 0) e.Stun(s.Stun, "b");
                Vfx.Burst(q, 1.1f, s.BoomKey, 0.25f);
                if (!s.Pierce) { End(false); return; }
            }
            // Solid ground and walls.
            if (s.ExplodeOnWall && HitsSolid(pos)) { End(s.Boom > 0f); return; }
            if (t >= s.Life) End(s.Boom > 0f);
        }

        static bool HitsSolid(Vector2 p)
        {
            foreach (var c in Physics2D.OverlapPointAll(p))
            {
                if (c.isTrigger || c.usedByEffector) continue;
                if (c.GetComponentInParent<Enemy>() != null || c.GetComponentInParent<PlayerController>() != null) continue;
                return true;
            }
            return false;
        }

        void End(bool explode)
        {
            Vector2 pos = transform.position;
            if (explode && s.Boom > 0f)
            {
                Combat.Circle(pos, s.Boom, s.Dmg, s.Vel.x >= 0 ? 1 : -1);
                Vfx.Burst(pos, s.Boom * 2.4f, s.BoomKey, 0.4f);
                Vfx.Ring(pos, s.Boom * 1.1f, s.BoomKey, 0.4f);
                Vfx.Sparks(pos, 6, 4f, s.BoomSpr, s.Tint, 0.35f, 0.5f, 6f);
                GameManager.Instance.Shake(0.08f);
            }
            if (s.OnEnd != null) s.OnEnd(pos);
            Destroy(gameObject);
        }
    }
}

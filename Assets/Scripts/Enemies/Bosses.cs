using System.Collections.Generic;
using UnityEngine;

namespace Vesperhold
{
    // Bone Warden (Catacombs) and Lord Vesper (Throne of Vesper), ported from the browser build.
    // Numbers are the web values converted from pixels (16 px = 1 unit). The Enemy component owns HP,
    // contact damage and rewards; this brain only decides what the boss does.
    public class BossBrain : MonoBehaviour
    {
        public static BossBrain Current { get; private set; }
        public Enemy E { get; private set; }
        public string Title { get { return E.Type == "lord" ? "Lord Vesper" : "The Bone Warden"; } }

        Rigidbody2D rb; string st = "walk"; float cd = 2f, tm, sum = 6f, air; bool half, lord;
        float a0 = 0f, a1 = 999f;      // arena bounds (Lord Vesper teleports inside them)

        public static BossBrain Attach(Enemy e, RoomData room)
        {
            var b = e.gameObject.AddComponent<BossBrain>();
            b.E = e; b.lord = e.Type == "lord"; b.rb = e.Body;
            if (b.lord) { b.a0 = room.arena.x0 + 1.5f; b.a1 = room.w - 2.5f; b.sum = 7f; b.cd = 1.5f; }
            Current = b; e.Face = -1;
            return b;
        }

        void OnDestroy() { if (Current == this) Current = null; }

        void Update()
        {
            if (E == null || GameManager.Instance.Paused) return;
            var p = PlayerController.Instance;
            var v = rb.linearVelocity;
            if (p == null || p.Dead) { rb.linearVelocity = new Vector2(0, v.y); return; }
            float dt = Time.deltaTime;
            cd -= dt; sum -= dt; half = E.Hp < E.MaxHp * 0.5f;
            float dx = p.transform.position.x - transform.position.x; int dir = dx > 0 ? 1 : -1;
            E.Invulnerable = lord && st == "tele";
            if (lord) Lord(ref v, dx, dir, dt, p); else Bone(ref v, dx, dir, dt);
            rb.linearVelocity = v;
        }

        // Telegraph: the sprite flickers red during the wind-up of a heavy move.
        void LateUpdate()
        {
            if (E == null || E.Renderer == null) return;
            bool warn = !lord ? (st == "charge" || st == "leap") : (st == "slash" || st == "fire");
            if (warn && Mathf.FloorToInt(Time.time * 14f) % 2 == 0) E.Renderer.color = new Color(1f, 0.35f, 0.35f);
            if (lord && st == "tele") { var c = E.Renderer.color; c.a = Mathf.Clamp01(tm / 0.35f); E.Renderer.color = c; }
        }

        void Bone(ref Vector2 v, float dx, int dir, float dt)
        {
            switch (st)
            {
                case "walk":
                    E.Face = dir; v.x = dir * (half ? 2.1f : 1.5f);
                    if (cd <= 0) { st = Random.value < 0.5f ? "charge" : "leap"; tm = st == "charge" ? 0.55f : 0.4f; v.x = 0; }
                    if (half && sum <= 0)
                    {
                        if (Count("skeleton") < 3)
                        {
                            Vector2 at = transform.position;
                            Enemy.Spawn("skeleton", at + new Vector2(-3.75f, 0.6f), false, transform.parent);
                            Enemy.Spawn("skeleton", at + new Vector2(3.75f, 0.6f), false, transform.parent);
                            GameManager.Instance.Toast("The Warden raises his servants!");
                            Vfx.Burst(at + Vector2.up * 1.2f, 4f, "p", 0.4f);
                        }
                        sum = 12f;
                    }
                    break;
                case "charge":
                    v.x = 0; E.Face = dir; tm -= dt;
                    if (tm <= 0) { st = "dash"; tm = 0.6f; v.x = dir * (half ? 11.9f : 10.3f); }
                    break;
                case "dash":
                    tm -= dt; v.x = E.Face * (half ? 11.9f : 10.3f);
                    if (tm <= 0 || Blocked(E.Face)) { st = "walk"; cd = 2.2f; v.x = 0; }
                    break;
                case "leap":
                    v.x = 0; tm -= dt; E.Face = dir;
                    if (tm <= 0) { st = "air"; v.y = 15.9f; v.x = Mathf.Clamp(dx * 0.9f, -5.9f, 5.9f); air = 0; }
                    break;
                case "air":
                    air += dt;
                    if (air > 0.15f && Grounded())
                    {
                        st = "walk"; cd = 2.4f; v.x = 0;
                        GameManager.Instance.Shake(0.35f); GameManager.Instance.HitStop(0.05f);
                        Vector2 at = transform.position;
                        Vfx.Ring(at, 4f, "o", 0.5f);
                        EnemyProjectile.Wave(at + new Vector2(-0.75f, 0), -1, Dmg());
                        EnemyProjectile.Wave(at + new Vector2(0.75f, 0), 1, Dmg());
                    }
                    break;
            }
        }

        void Lord(ref Vector2 v, float dx, int dir, float dt, PlayerController p)
        {
            switch (st)
            {
                case "walk":
                    E.Face = dir; bool moving = Mathf.Abs(dx) > 2.9f; v.x = moving ? dir * (half ? 2.5f : 1.9f) : 0;
                    if (cd <= 0)
                    {
                        float q = Random.value; st = q < 0.3f ? "tele" : q < 0.65f ? "fire" : "slash";
                        tm = st == "tele" ? 0.35f : st == "fire" ? 0.6f : 0.55f; v.x = 0;
                    }
                    break;
                case "tele":
                    v.x = 0; tm -= dt;
                    if (tm <= 0)
                    {
                        Vfx.Burst(transform.position + Vector3.up, 3f, "p", 0.3f);
                        float x = Mathf.Clamp(p.transform.position.x + (Random.value < 0.5f ? -1 : 1) * 4f, a0, a1);
                        rb.position = new Vector2(x, rb.position.y); transform.position = rb.position;
                        st = "slash"; tm = 0.45f; E.Face = p.transform.position.x > x ? 1 : -1;
                        Vfx.Burst(transform.position + Vector3.up, 3f, "p", 0.3f);
                    }
                    break;
                case "fire":
                    v.x = 0; E.Face = dir; tm -= dt;
                    if (tm <= 0)
                    {
                        Vector2 at = (Vector2)transform.position + new Vector2(dir * 0.5f, 1.4f);
                        for (int i = -1; i <= 1; i++) EnemyProjectile.Fire(at, new Vector2(dir * (5.9f + Mathf.Abs(i) * 0.94f), -i * 2.9f), Dmg());
                        if (half) EnemyProjectile.Fire(at - new Vector2(dir, 0) * 1f, new Vector2(-dir * 6.25f, 1.25f), Dmg());
                        GameManager.Instance.Shake(0.15f); st = "walk"; cd = 1.5f;
                    }
                    break;
                case "slash":
                    v.x = 0; E.Face = dir; tm -= dt;
                    if (tm <= 0) { st = "dash"; tm = 0.38f; }
                    break;
                case "dash":
                    tm -= dt; v.x = E.Face * 13.4f;
                    if (tm <= 0 || Blocked(E.Face)) { st = "walk"; cd = 1.3f; v.x = 0; }
                    break;
            }
            if (half && sum <= 0)
            {
                if (Count("bat") < 3)
                {
                    Vector2 at = transform.position;
                    Enemy.Spawn("bat", at + new Vector2(-1.9f, 3.75f), false, transform.parent);
                    Enemy.Spawn("bat", at + new Vector2(1.9f, 3.75f), false, transform.parent);
                    GameManager.Instance.Toast("Vesper calls the night!");
                    Vfx.Burst(at + Vector2.up * 2f, 4f, "p", 0.4f);
                }
                sum = 13f;
            }
        }

        int Dmg() { return Mathf.RoundToInt(E.Damage * E.DamageMul); }

        static int Count(string type)
        {
            int n = 0; foreach (var e in Combat.All()) if (e.Type == type) n++;
            return n;
        }

        bool Blocked(int face)
        {
            Vector2 o = (Vector2)transform.position + Vector2.up * 1.2f;
            foreach (var h in Physics2D.RaycastAll(o, new Vector2(face, 0), 1.1f))
            {
                if (h.collider.isTrigger || h.collider.usedByEffector) continue;
                if (h.collider.GetComponentInParent<Enemy>() != null || h.collider.GetComponentInParent<PlayerController>() != null) continue;
                return true;
            }
            return false;
        }

        bool Grounded()
        {
            if (rb.linearVelocity.y > 0.1f) return false;
            foreach (var c in Physics2D.OverlapBoxAll((Vector2)transform.position + new Vector2(0, -0.05f), new Vector2(1f, 0.15f), 0))
            {
                if (c.isTrigger || c.usedByEffector) continue;
                if (c.GetComponentInParent<Enemy>() != null || c.GetComponentInParent<PlayerController>() != null) continue;
                return true;
            }
            return false;
        }
    }

    // Shockwaves (Bone Warden slam) and dark fireballs (Lord Vesper).
    public class EnemyProjectile : MonoBehaviour
    {
        Vector2 vel; float life = 3.4f, radius = 0.5f; int dmg; bool wave;

        public static void Wave(Vector2 at, int dir, int dmg) { Make(at + Vector2.up * 0.5f, new Vector2(dir * 7.2f, 0), dmg, true, 1.4f); }
        public static void Fire(Vector2 at, Vector2 vel, int dmg) { Make(at, vel, dmg, false, 3.4f); }

        static void Make(Vector2 at, Vector2 vel, int dmg, bool wave, float life)
        {
            var go = new GameObject(wave ? "BossWave" : "BossFire");
            go.transform.position = at;
            var p = go.AddComponent<EnemyProjectile>();
            p.vel = vel; p.dmg = dmg; p.wave = wave; p.life = life; p.radius = wave ? 0.6f : 0.45f;
            var spr = Vfx.Get(wave ? "shock_o" : "orb_fire");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 34;
            if (spr != null)
            {
                sr.sprite = spr; sr.sharedMaterial = Vfx.Additive;
                go.transform.localScale = wave ? new Vector3(2.4f * Mathf.Sign(vel.x), 1.4f, 1f) : new Vector3(1.4f, 1.4f, 1f);
                if (!wave) sr.color = new Color(0.8f, 0.5f, 1f);
            }
            else { sr.sprite = SpriteFactory.Square; sr.color = wave ? new Color(1f, 0.6f, 0.3f) : new Color(0.8f, 0.4f, 1f); go.transform.localScale = new Vector3(0.7f, 0.7f, 1f); }
        }

        void Update()
        {
            if (GameManager.Instance.Paused) return;
            life -= Time.deltaTime;
            transform.position += (Vector3)(vel * Time.deltaTime);
            if (life <= 0) { Destroy(gameObject); return; }
            var p = PlayerController.Instance;
            if (p != null && !p.Dead)
            {
                var pb = p.GetComponent<BoxCollider2D>().bounds;
                var b = new Bounds(transform.position, new Vector3(radius * 2f, radius * 2f, 1f));
                if (pb.Intersects(b)) { p.Hurt(dmg, transform.position.x); Vfx.Burst(transform.position, 2f, "o", 0.25f); Destroy(gameObject); return; }
            }
            if (!wave)
                foreach (var c in Physics2D.OverlapPointAll(transform.position))
                {
                    if (c.isTrigger || c.usedByEffector) continue;
                    if (c.GetComponentInParent<Enemy>() != null || c.GetComponentInParent<PlayerController>() != null) continue;
                    Vfx.Burst(transform.position, 1.6f, "p", 0.25f); Destroy(gameObject); return;
                }
        }
    }

    // Locks the room when the hero crosses the trigger, spawns the boss, hands out the rewards.
    public class ArenaController : MonoBehaviour
    {
        RoomData data; Arena ar; bool fighting; GameObject wall; Enemy boss;

        public void Init(RoomData d)
        {
            data = d; ar = d.arena;
        }

        void Start()
        {
            if (GameState.Flags.Contains(ar.id)) { Cleared(); enabled = false; }
        }

        // Already beaten: make sure the Shadow Step relic is still waiting if it was never picked up.
        void Cleared()
        {
            if (ar.id == "boss1" && !GameState.Flags.Contains("dash"))
                EntityFactory.SpawnRelic(GroundAt(ar.bx + 0.5f), "dash", transform.parent);
        }

        void Update()
        {
            if (fighting || GameManager.Instance.Paused) return;
            var p = PlayerController.Instance; if (p == null || p.Dead) return;
            if (p.transform.position.x > ar.trigger / 16f) Begin();
        }

        void Begin()
        {
            fighting = true;
            wall = new GameObject("ArenaLock");
            wall.transform.SetParent(transform, false);
            wall.transform.position = new Vector3(ar.lockCol + 0.5f, -data.h / 2f, 0);
            wall.transform.localScale = new Vector3(1f, data.h, 1f);
            var sr = wall.AddComponent<SpriteRenderer>(); sr.sprite = SpriteFactory.Square; sr.color = new Color(0.5f, 0.15f, 0.2f, 0.9f); sr.sortingOrder = 6;
            var box = wall.AddComponent<BoxCollider2D>(); box.size = Vector2.one;
            boss = Enemy.Spawn(ar.boss, GroundAt(ar.bx + 0.5f), false, transform.parent);
            BossBrain.Attach(boss, data);
            boss.OnDeath += Defeated;
            GameManager.Instance.Toast(ar.boss == "boss" ? "The Bone Warden awakens!" : "Lord Vesper rises from his throne!");
            GameManager.Instance.Shake(0.3f);
        }

        void Defeated(Enemy e)
        {
            GameState.Flags.Add(ar.id);
            if (wall != null) Destroy(wall);
            Vector2 at = e.transform.position;
            foreach (var o in Combat.All()) if (o != e) { Vfx.Burst(o.Mid, 2f, "w", 0.3f); Destroy(o.gameObject); }
            Vfx.Burst(at + Vector2.up * 1.5f, 8f, "y", 0.6f); Vfx.Ring(at, 6f, "y", 0.7f);
            GameManager.Instance.Shake(0.45f);
            if (ar.id == "boss1")
            {
                GameManager.Instance.Toast("The Bone Warden crumbles to dust!");
                if (!GameState.Flags.Contains("dash")) EntityFactory.SpawnRelic(at, "dash", transform.parent);
            }
            else
            {
                GameManager.Instance.Toast("Lord Vesper falls. The night is lifting.");
                GameManager.Instance.Win();
            }
            GameState.Save();
        }

        Vector2 GroundAt(float x)
        {
            foreach (var h in Physics2D.RaycastAll(new Vector2(x, -1.5f), Vector2.down, data.h + 2f))
            {
                if (h.collider.isTrigger || h.collider.usedByEffector) continue;
                if (h.collider.GetComponentInParent<Enemy>() != null || h.collider.GetComponentInParent<PlayerController>() != null) continue;
                return new Vector2(x, h.point.y + 0.02f);
            }
            return new Vector2(x, -(data.h - 2));
        }
    }
}

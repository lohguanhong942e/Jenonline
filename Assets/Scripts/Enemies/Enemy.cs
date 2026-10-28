using UnityEngine;

namespace Vesperhold
{
    // Skeleton, bat, ghost and armor knight, plus the two bosses (their moves live in BossBrain).
    public class Enemy : MonoBehaviour
    {
        public string Type; public int Hp, MaxHp, Damage; public float DamageMul = 1f, GoldMul = 1f, XpMul = 1f;
        public System.Action<Enemy> OnDeath;
        public int Face = 1;
        public bool IsBoss { get { return Type == "boss" || Type == "lord"; } }
        public Vector2 Mid { get { return (Vector2)transform.position + Vector2.up * (def != null ? def.h / 32f : 0.7f); } }
        public Rigidbody2D Body { get { return rb; } }
        public SpriteRenderer Renderer { get { return sr; } }
        public bool Invulnerable;     // set by bosses while teleporting
        EnemyDef def; Rigidbody2D rb; SpriteRenderer sr; Color baseColor, tint = Color.white;
        float hurtT, wander, dir = 1, phase, homeY, cd, stunT, pullT; Vector2 pullTo; bool dead, hasArt;
        bool flies => Type == "bat" || Type == "ghost";

        public static Enemy Spawn(string type, Vector2 feet, bool elite, Transform parent)
        {
            var go = new GameObject(type);
            go.transform.SetParent(parent, true);
            go.transform.position = feet;
            var e = go.AddComponent<Enemy>();
            e.Init(type, elite);
            return e;
        }

        void Init(string type, bool elite)
        {
            Type = type; def = GameData.Enemy(type);
            Hp = MaxHp = def.hp; Damage = def.dmg;
            if (elite) { Hp = MaxHp = Mathf.RoundToInt(def.hp * Tuning.EliteHp); DamageMul = Tuning.EliteDamage; GoldMul = Tuning.EliteGold; XpMul = Tuning.EliteXp; }
            float w = def.w / 16f, h = def.h / 16f;
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.freezeRotation = true; rb.gravityScale = flies ? 0 : 1;
            var box = gameObject.AddComponent<BoxCollider2D>();
            box.size = new Vector2(w, h); box.offset = new Vector2(0, h / 2f);
            box.sharedMaterial = new PhysicsMaterial2D { friction = 0f, bounciness = 0f };
            string art = Type == "skeleton" ? "skeleton" : Type == "bat" ? "vampire_bat" : Type == "ghost" ? "ghost" : Type == "armor" ? "haunted_armor" : Type == "boss" ? "bone_dragon" : "vampire_noble";
            float drawH = Type == "skeleton" ? 1.5f : Type == "bat" ? 0.9f : Type == "ghost" ? 1.3f : Type == "armor" ? 1.9f : Type == "boss" ? 3.4f : 2.5f;
            baseColor = Type == "skeleton" ? new Color(0.85f, 0.85f, 0.78f) : Type == "bat" ? new Color(0.5f, 0.3f, 0.6f)
                : Type == "ghost" ? new Color(0.6f, 0.8f, 1f, 0.7f) : new Color(0.55f, 0.58f, 0.7f);
            if (elite) baseColor = Color.Lerp(baseColor, new Color(1f, 0.45f, 0.2f), 0.45f);
            sr = SpriteFactory.Monster(transform, art, drawH, 8);
            hasArt = sr != null;
            if (hasArt) baseColor = elite ? new Color(1f, 0.7f, 0.55f) : Color.white;
            else sr = SpriteFactory.Box(transform, "Body", baseColor, new Vector2(w, h), new Vector2(0, h / 2f), 8);
            homeY = transform.position.y; phase = Random.value * 6f; dir = Random.value < 0.5f ? -1 : 1;
            cd = 1 + Random.value * 2;
        }

        void Update()
        {
            if (GameManager.Instance.Paused) { rb.linearVelocity = Vector2.zero; return; }
            var p = PlayerController.Instance; if (p == null || p.Dead) return;
            hurtT -= Time.deltaTime; cd -= Time.deltaTime; stunT -= Time.deltaTime; pullT -= Time.deltaTime;
            Vector2 d = (Vector2)p.transform.position - (Vector2)transform.position;
            float dist = Mathf.Abs(d.x);
            var v = rb.linearVelocity;
            bool aware = dist < 7f && Mathf.Abs(d.y) < 3f;
            if (pullT > 0 && !IsBoss) { v = (pullTo - (Vector2)transform.position) * 10f; v.y = Mathf.Clamp(v.y, -2f, 6f); }
            else if (stunT > 0) { v = new Vector2(0, flies ? 0 : v.y); sr.color = Color.Lerp(baseColor, tint, 0.7f); }
            else if (hurtT > 0) { sr.color = hasArt ? new Color(1f, 0.45f, 0.45f) : Color.white; }
            else
            {
                sr.color = baseColor;
                switch (Type)
                {
                    case "skeleton": v.x = (aware ? Mathf.Sign(d.x) : dir) * 2.2f; WallTurn(ref v); break;
                    case "armor": v.x = (aware ? Mathf.Sign(d.x) : dir) * 1.4f; WallTurn(ref v); break;
                    case "bat":
                        phase += Time.deltaTime * 4f;
                        v = aware || dist < 10f ? ((Vector2)p.transform.position + Vector2.up * 0.8f - (Vector2)transform.position).normalized * 3.2f : new Vector2(dir * 2f, Mathf.Sin(phase) * 1.5f);
                        v.y += Mathf.Sin(phase * 2f) * 1.2f; break;
                    case "ghost":
                        phase += Time.deltaTime * 2f;
                        v = aware ? new Vector2(Mathf.Sign(d.x) * 1.6f, Mathf.Sin(phase) * 1f + Mathf.Clamp(d.y, -1, 1) * 0.6f) : new Vector2(dir * 0.8f, Mathf.Sin(phase) * 0.8f);
                        break;
                }
            }
            if (IsBoss) sr.transform.localScale = new Vector3(Face * Mathf.Abs(sr.transform.localScale.x), sr.transform.localScale.y, 1);
            else if (Mathf.Abs(v.x) > 0.1f) sr.transform.localScale = new Vector3(Mathf.Sign(v.x) * Mathf.Abs(sr.transform.localScale.x), sr.transform.localScale.y, 1);
            rb.linearVelocity = v;
            // Contact damage.
            var pb = p.GetComponent<BoxCollider2D>().bounds; var eb = GetComponent<BoxCollider2D>().bounds;
            if (pb.Intersects(eb)) p.Hurt(Mathf.RoundToInt(Damage * DamageMul), transform.position.x);
        }

        void WallTurn(ref Vector2 v)
        {
            var origin = (Vector2)transform.position + new Vector2(Mathf.Sign(v.x) * 0.45f, 0.5f);
            var wall = Physics2D.Raycast(origin, new Vector2(Mathf.Sign(v.x), 0), 0.25f);
            var edge = Physics2D.Raycast(origin + new Vector2(0.1f * Mathf.Sign(v.x), 0), Vector2.down, 1.1f);
            if ((wall.collider != null && !wall.collider.isTrigger && wall.collider.gameObject != gameObject && wall.collider.GetComponent<PlayerController>() == null)
                || edge.collider == null) dir = -dir;
        }

        public void TakeDamage(int dm, int knockDir, int combo, bool crit = false)
        {
            if (dead || Invulnerable) return;
            Hp -= dm; hurtT = 0.12f; Sfx.Play("hit", 0.55f, Random.Range(0.9f, 1.15f));
            Vfx.Burst(Mid, crit ? 2.6f : 1.4f, crit ? "y" : "o", 0.2f);
            if (!IsBoss)
            {
                float kb = (Type == "armor" ? 40f : 100f) / 16f * (combo == 2 ? 1.9f : 1f);
                rb.linearVelocity = new Vector2(knockDir * kb, Type == "skeleton" ? 3.75f : 0f);
            }
            FloatingText.Spawn(transform.position + Vector3.up * (IsBoss ? 3.2f : 1.3f), crit ? dm + "!" : dm.ToString(), crit ? new Color(1f, 0.85f, 0.3f) : Color.white);
            if (Hp <= 0) Die();
        }

        // Freeze or stun for a short time; the colour key tints the enemy.
        public void Stun(float t, string colorKey)
        {
            if (IsBoss) t *= 0.25f;
            stunT = Mathf.Max(stunT, t); tint = Vfx.C(colorKey);
        }

        // Drag toward a point for t seconds (Chain Pull, Gravity Well).
        public void Pull(Vector2 to, float t) { if (IsBoss) return; pullTo = to; pullT = t; }

        void Die()
        {
            if (dead) return; dead = true;
            int gold = Mathf.RoundToInt(Random.Range(def.goldMin, def.goldMax + 1) * GoldMul);
            Vector2 at = Mid;
            int coins = IsBoss ? 10 : Mathf.Clamp(gold / 3, 1, 5);
            for (int i = 0; i < coins; i++) Pickup.Spawn(at, "coin", i == coins - 1 ? gold - (gold / coins) * (coins - 1) : gold / coins);
            if (Random.value < 0.4f) Pickup.Spawn(at, "heart", 1);
            if (Random.value < 0.08f || IsBoss) Pickup.Spawn(at, "potion", 1);
            if (GameState.AddXp(Mathf.RoundToInt(def.xp * XpMul))) { GameManager.Instance.Toast("Level " + GameState.Level + "! +5 stat points"); Sfx.Play("level"); Vfx.Ring(PlayerController.Instance.transform.position, 4f, "y", 0.6f); }
            LootTable.Roll(Type, DamageMul > 1f);
            Vfx.Burst(at, IsBoss ? 7f : 2.6f, "w", 0.4f); Vfx.Sparks(at, IsBoss ? 24 : 9, 5f, "burst_o", null, 0.35f, 0.6f, 8f);
            Sfx.Play(IsBoss ? "explode" : "hit", 0.8f, IsBoss ? 0.8f : 0.7f);
            OnDeath?.Invoke(this);
            Destroy(gameObject);
        }
    }

    // Gear drops: any enemy can drop a weapon for your class or a piece of armor near your level.
    // Elites and bosses drop more, so the whole 30-tier ladder is reachable by playing.
    public static class LootTable
    {
        public static void Roll(string enemy, bool elite)
        {
            bool boss = enemy == "boss" || enemy == "lord";
            float k = boss ? 1f : elite ? 2.5f : 1f;
            int around = GameState.Level * 3 / 5 + (boss ? 3 : 0);
            if (boss || Random.value < 0.05f * k)
            {
                var w = ItemDb.Weapon(GameState.Kind, Mathf.Clamp(around + Random.Range(-1, 3), 2, 30));
                if (GameState.Give(w)) { GameManager.Instance.Toast("Found " + w.Name + "!"); Sfx.Play("pickup"); }
            }
            if (boss || Random.value < 0.03f * k)
            {
                var a = ItemDb.Armor(Mathf.Clamp(around * 32 / 30 + Random.Range(-1, 3), 1, 32));
                if (GameState.Give(a)) { GameManager.Instance.Toast("Found " + a.Name + "!"); Sfx.Play("pickup"); }
            }
        }
    }
}

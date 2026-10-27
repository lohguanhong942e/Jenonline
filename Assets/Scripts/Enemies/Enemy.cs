using UnityEngine;

namespace Vesperhold
{
    // Skeleton, bat, ghost and armor knight. Bosses (Bone Warden, Lord Vesper) are ported separately.
    public class Enemy : MonoBehaviour
    {
        public string Type; public int Hp, MaxHp, Damage; public float DamageMul = 1f, GoldMul = 1f, XpMul = 1f;
        public System.Action<Enemy> OnDeath;
        EnemyDef def; Rigidbody2D rb; SpriteRenderer sr; Color baseColor;
        float hurtT, wander, dir = 1, phase, homeY, cd;
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
            baseColor = Type == "skeleton" ? new Color(0.85f, 0.85f, 0.78f) : Type == "bat" ? new Color(0.5f, 0.3f, 0.6f)
                : Type == "ghost" ? new Color(0.6f, 0.8f, 1f, 0.7f) : new Color(0.55f, 0.58f, 0.7f);
            if (elite) baseColor = Color.Lerp(baseColor, new Color(1f, 0.45f, 0.2f), 0.45f);
            sr = SpriteFactory.Box(transform, "Body", baseColor, new Vector2(w, h), new Vector2(0, h / 2f), 8);
            homeY = transform.position.y; phase = Random.value * 6f; dir = Random.value < 0.5f ? -1 : 1;
            cd = 1 + Random.value * 2;
        }

        void Update()
        {
            if (GameManager.Instance.Paused) { rb.linearVelocity = Vector2.zero; return; }
            var p = PlayerController.Instance; if (p == null || p.Dead) return;
            hurtT -= Time.deltaTime; cd -= Time.deltaTime;
            Vector2 d = (Vector2)p.transform.position - (Vector2)transform.position;
            float dist = Mathf.Abs(d.x);
            var v = rb.linearVelocity;
            bool aware = dist < 7f && Mathf.Abs(d.y) < 3f;
            if (hurtT > 0) { sr.color = Color.white; }
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
            if (Mathf.Abs(v.x) > 0.1f) sr.transform.localScale = new Vector3(Mathf.Sign(v.x) * Mathf.Abs(sr.transform.localScale.x), sr.transform.localScale.y, 1);
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

        public void TakeDamage(int dm, int knockDir, int combo)
        {
            Hp -= dm; hurtT = 0.12f;
            float kb = (Type == "armor" ? 40f : 100f) / 16f * (combo == 2 ? 1.9f : 1f);
            rb.linearVelocity = new Vector2(knockDir * kb, Type == "skeleton" ? 3.75f : 0f);
            FloatingText.Spawn(transform.position + Vector3.up * 1.3f, dm.ToString(), Color.white);
            if (Hp <= 0) Die();
        }

        void Die()
        {
            int gold = Mathf.RoundToInt(Random.Range(def.goldMin, def.goldMax + 1) * GoldMul);
            GameState.Gold += gold;
            if (Random.value < 0.4f) GameState.Hearts = Mathf.Min(99, GameState.Hearts + 1);
            if (GameState.AddXp(Mathf.RoundToInt(def.xp * XpMul))) GameManager.Instance.Toast("Level " + GameState.Level + "! +5 stat points");
            LootTable.RollSword(Type);
            OnDeath?.Invoke(this);
            Destroy(gameObject);
        }
    }

    // Sword drops from the web build: skeleton/ghost/bat/armor each have a signature blade.
    public static class LootTable
    {
        public static void RollSword(string enemy)
        {
            int idx = -1; float chance = 0.05f;
            if (enemy == "skeleton") idx = 3; else if (enemy == "ghost") idx = 4; else if (enemy == "bat") idx = 5;
            else if (enemy == "armor") { idx = Random.value < 0.5f ? 6 : 7; chance = 0.04f; }
            if (idx >= 0 && !GameState.OwnedSwords[idx] && Random.value < chance)
            {
                GameState.OwnedSwords[idx] = true;
                GameManager.Instance.Toast("Found " + GameData.Swords.swords[idx].n + "!");
            }
        }
    }
}

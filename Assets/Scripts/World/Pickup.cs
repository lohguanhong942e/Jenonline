using UnityEngine;

namespace Vesperhold
{
    // Gold, hearts and potions that pop out of defeated enemies, bounce, then fly to the hero when close.
    public class Pickup : MonoBehaviour
    {
        string kind; int amount; Vector2 vel; float age; bool magnet; VfxSprite glow;

        public static void Spawn(Vector2 at, string kind, int amount)
        {
            var go = new GameObject("Pickup_" + kind);
            go.transform.position = at;
            var p = go.AddComponent<Pickup>(); p.kind = kind; p.amount = amount;
            float a = Random.Range(0.35f, 2.8f); p.vel = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Random.Range(3f, 6.5f);
            Color c = kind == "coin" ? new Color(1f, 0.82f, 0.25f) : kind == "heart" ? new Color(1f, 0.3f, 0.4f) : new Color(0.4f, 0.9f, 0.5f);
            Vector2 size = kind == "coin" ? new Vector2(0.28f, 0.28f) : kind == "heart" ? new Vector2(0.36f, 0.32f) : new Vector2(0.28f, 0.42f);
            SpriteFactory.Box(go.transform, "Core", c, size, Vector2.zero, 12);
            p.glow = Vfx.Pop("burst_" + (kind == "coin" ? "o" : kind == "heart" ? "o" : "g"), at, 0.9f, 60f, 1f, 0, 0, null, 11, 0.55f);
            if (p.glow != null) { p.glow.Target = go.transform; p.glow.Offset = Vector2.zero; p.glow.FadeTail = 0.02f; }
        }

        void OnDestroy() { if (glow != null) Destroy(glow.gameObject); }

        void Update()
        {
            if (GameManager.Instance.Paused) return;
            float dt = Time.deltaTime; age += dt;
            var pl = PlayerController.Instance; if (pl == null) return;
            Vector2 pos = transform.position, target = (Vector2)pl.transform.position + Vector2.up * 0.7f;
            float d = Vector2.Distance(pos, target);
            if (age > 0.45f && d < 3.2f) magnet = true;
            if (magnet) { vel = Vector2.Lerp(vel, (target - pos).normalized * 14f, 12f * dt); }
            else
            {
                vel.y -= 28f * dt;
                var hit = Physics2D.Raycast(pos, Vector2.down, 0.2f);
                if (hit.collider != null && !hit.collider.isTrigger && vel.y < 0 && hit.collider.GetComponentInParent<PlayerController>() == null) { vel.y = Mathf.Abs(vel.y) > 3f ? -vel.y * 0.45f : 0f; vel.x *= 0.6f; }
            }
            transform.position = pos + vel * dt;
            if (age > 0.25f && d < 0.55f && !pl.Dead) Collect(pl);
            if (age > 40f) Destroy(gameObject);
        }

        void Collect(PlayerController pl)
        {
            switch (kind)
            {
                case "coin": GameState.Gold += amount; Sfx.Play("coin", 0.5f, Random.Range(0.9f, 1.3f)); break;
                case "heart": GameState.Hearts = Mathf.Min(99, GameState.Hearts + amount); Sfx.Play("pickup", 0.4f, 1.3f); break;
                case "potion": GameState.Potions = Mathf.Min(9, GameState.Potions + amount); Sfx.Play("pickup", 0.6f, 0.9f); GameManager.Instance.Toast("+1 potion"); break;
            }
            Destroy(gameObject);
        }
    }
}

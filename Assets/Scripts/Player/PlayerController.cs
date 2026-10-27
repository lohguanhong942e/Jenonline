using UnityEngine;
using UnityEngine.InputSystem;

namespace Vesperhold
{
    // Hero: run, variable jump, double jump (Raven Feather), dash (Warden relic), 3-hit combo, potion, interact.
    // Controls match the web build: A/D move, Space jump, J strike, L dash, Q potion, E use.
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance { get; private set; }

        public int Hp, Mp;
        public int Facing = 1;
        public bool Dead { get; private set; }

        Rigidbody2D rb; BoxCollider2D box; SpriteRenderer body;
        float coyote, jumpBuffer, invul, stun, dashT, dashCd, atkT = -1, atkCd, comboT, mpRegen;
        int combo, jumps; bool airDash, jumpHeld, atkQueued, grounded;
        readonly ContactFilter2D solidFilter = new ContactFilter2D();
        readonly Collider2D[] buf = new Collider2D[16];
        readonly RaycastHit2D[] hits = new RaycastHit2D[8];
        readonly System.Collections.Generic.HashSet<Enemy> hitThisSwing = new System.Collections.Generic.HashSet<Enemy>();

        public static PlayerController Create(Vector2 feet)
        {
            var go = new GameObject("Player");
            go.transform.position = feet;
            go.AddComponent<Rigidbody2D>();
            go.AddComponent<BoxCollider2D>();
            return go.AddComponent<PlayerController>();
        }

        void Awake()
        {
            Instance = this;
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 1f; rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            box = GetComponent<BoxCollider2D>();
            box.size = new Vector2(9f / 16f, 22f / 16f);
            box.offset = new Vector2(0, 11f / 16f);          // pivot at the feet
            var mat = new PhysicsMaterial2D { friction = 0f, bounciness = 0f };
            box.sharedMaterial = mat;
            body = SpriteFactory.Box(transform, "Body", new Color(0.55f, 0.75f, 0.9f), new Vector2(0.6f, 1.35f), new Vector2(0, 0.7f), 10);
            solidFilter.useTriggers = false;
            Hp = GameState.MaxHp; Mp = GameState.MaxMp;
        }

        void Update()
        {
            if (Dead || GameManager.Instance.Paused) return;
            var kb = Keyboard.current; if (kb == null) return;
            float dt = Time.deltaTime;
            invul -= dt; stun -= dt; dashCd -= dt; atkCd -= dt; comboT -= dt; coyote -= dt; jumpBuffer -= dt;
            mpRegen += dt * (1f + GameState.Int * 0.1f);
            if (mpRegen >= 1.2f) { mpRegen = 0; if (Mp < GameState.MaxMp) Mp++; }

            grounded = CheckGround();
            if (grounded) { coyote = Tuning.Coyote; jumps = 0; airDash = false; }

            float ax = (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1 : 0) - (kb.aKey.isPressed || kb.leftArrowKey.isPressed ? 1 : 0);
            if (kb.spaceKey.wasPressedThisFrame) { jumpBuffer = Tuning.JumpBuffer; jumpHeld = true; }
            if (kb.spaceKey.wasReleasedThisFrame) jumpHeld = false;
            if (kb.jKey.wasPressedThisFrame) PressAttack();
            if (kb.lKey.wasPressedThisFrame) PressDash(ax);
            if (kb.qKey.wasPressedThisFrame) DrinkPotion();
            if (kb.eKey.wasPressedThisFrame) Interact();

            var v = rb.linearVelocity;
            if (dashT > 0)
            {
                dashT -= dt; v = new Vector2(Facing * Tuning.DashSpeed, 0); rb.gravityScale = 0;
                if (dashT <= 0) { rb.gravityScale = 1; v.x = Facing * 90f / 16f; }
            }
            else
            {
                rb.gravityScale = 1;
                if (stun <= 0)
                {
                    float target = ax * Tuning.RunSpeed * (atkT >= 0 ? 0.4f : 1f);
                    v.x = Mathf.MoveTowards(v.x, target, (grounded ? 90f : 55f) * dt);
                    if (Mathf.Abs(ax) > 0.1f && atkT < 0) Facing = ax > 0 ? 1 : -1;
                }
                if (jumpBuffer > 0 && stun <= 0)
                {
                    if (coyote > 0) { v.y = Tuning.JumpSpeed; coyote = 0; jumpBuffer = 0; }
                    else if (GameState.Flags.Contains("dj") && jumps < 1) { v.y = Tuning.DoubleJumpSpeed; jumps = 1; jumpBuffer = 0; }
                }
                if (!jumpHeld && v.y > Tuning.JumpCutSpeed) v.y = Tuning.JumpCutSpeed;
                if (v.y < -Tuning.MaxFall) v.y = -Tuning.MaxFall;
            }
            rb.linearVelocity = v;
            UpdateAttack(dt);
            body.transform.localScale = new Vector3(0.6f * Facing, 1.35f, 1);
            body.color = invul > 0 && Mathf.FloorToInt(Time.time * 20) % 2 == 0 ? new Color(1, 1, 1, 0.4f) : new Color(0.55f, 0.75f, 0.9f);
        }

        bool CheckGround()
        {
            if (rb.linearVelocity.y > 0.5f) return false;
            Vector2 origin = (Vector2)transform.position + new Vector2(0, 0.05f);
            int n = Physics2D.BoxCast(origin, new Vector2(box.size.x * 0.9f, 0.05f), 0, Vector2.down, solidFilter, hits, 0.12f);
            for (int i = 0; i < n; i++) if (hits[i].collider != box && !hits[i].collider.isTrigger) return true;
            return false;
        }

        void PressAttack()
        {
            if (dashT > 0 || stun > 0) return;
            if (atkT >= 0) { if (atkT > 0.3f * AttackDuration() && combo < 2) atkQueued = true; return; }
            if (atkCd > 0) return;
            StartAttack(comboT > 0 && combo < 2 ? combo + 1 : 0);
        }

        float AttackDuration() { return combo == 2 ? 0.44f : 0.28f; }

        void StartAttack(int i)
        {
            combo = i; atkT = 0; atkQueued = false; comboT = 0; hitThisSwing.Clear();
            var v = rb.linearVelocity; v.x += Facing * (i == 2 ? 6.9f : 2f); rb.linearVelocity = v;
        }

        void UpdateAttack(float dt)
        {
            if (atkT < 0) return;
            atkT += dt;
            float dur = AttackDuration();
            // Active window in the middle of the swing.
            if (atkT > dur * 0.25f && atkT < dur * 0.75f)
            {
                float reach = combo == 2 ? 2.2f : 1.7f;
                Vector2 c = (Vector2)transform.position + new Vector2(Facing * reach / 2f, 0.7f);
                int n = Physics2D.OverlapBox(c, new Vector2(reach, 1.5f), 0, ContactFilter(), buf);
                for (int i = 0; i < n; i++)
                {
                    var e = buf[i].GetComponentInParent<Enemy>();
                    if (e == null || hitThisSwing.Contains(e)) continue;
                    hitThisSwing.Add(e);
                    e.TakeDamage(Mathf.RoundToInt(GameState.Attack * (combo == 2 ? 1.8f : 1f)), Facing, combo);
                    GameManager.Instance.HitStop(combo == 2 ? 0.09f : 0.05f);
                }
                foreach (var cd in Physics2D.OverlapBoxAll(c, new Vector2(reach, 1.5f), 0))
                {
                    var candle = cd.GetComponent<Candle>(); if (candle != null) candle.Smash();
                }
            }
            if (atkT >= dur)
            {
                atkT = -1; atkCd = 0.06f; comboT = 0.35f;
                if (atkQueued && combo < 2) StartAttack(combo + 1);
            }
        }

        static ContactFilter2D ContactFilter() { var f = new ContactFilter2D(); f.useTriggers = true; return f; }

        void PressDash(float ax)
        {
            if (!GameState.Flags.Contains("dash") || dashCd > 0 || dashT > 0 || stun > 0) return;
            if (!grounded && airDash) return;
            if (Mathf.Abs(ax) > 0.3f) Facing = ax > 0 ? 1 : -1;
            dashT = Tuning.DashTime; dashCd = Tuning.DashCooldown; invul = Mathf.Max(invul, 0.24f);
            if (!grounded) airDash = true;
            atkT = -1;
        }

        void DrinkPotion()
        {
            if (GameState.Potions <= 0 || Hp >= GameState.MaxHp) return;
            GameState.Potions--; Hp = Mathf.Min(GameState.MaxHp, Hp + 20);
            GameManager.Instance.Toast("Potion: +20 HP");
        }

        void Interact()
        {
            Interactable best = null; float bd = 1.6f;
            foreach (var i in Object.FindObjectsByType<Interactable>(FindObjectsSortMode.None))
            {
                float d = Vector2.Distance(i.transform.position, transform.position + Vector3.up * 0.5f);
                if (d < bd) { bd = d; best = i; }
            }
            if (best != null) best.Use(this);
        }

        public void Hurt(int dmg, float fromX, float knock = 7f)
        {
            if (invul > 0 || Dead) return;
            dmg = Mathf.Max(1, dmg - GameState.Defense);
            Hp -= dmg; invul = Tuning.Invulnerable; stun = 0.22f; dashT = 0; atkT = -1;
            rb.gravityScale = 1;
            int s = transform.position.x >= fromX ? 1 : -1;
            rb.linearVelocity = new Vector2(s * knock, 8f);
            GameManager.Instance.Shake(0.15f);
            if (Hp <= 0) { Hp = 0; Dead = true; GameManager.Instance.OnPlayerDied(); }
        }

        public void Heal(int n) { Hp = Mathf.Min(GameState.MaxHp, Hp + n); }
        public void FullRestore() { Hp = GameState.MaxHp; Mp = GameState.MaxMp; }
        public void Revive(Vector2 feet) { Dead = false; transform.position = feet; rb.linearVelocity = Vector2.zero; FullRestore(); invul = 1.3f; }
        public void Teleport(Vector2 feet) { transform.position = feet; rb.linearVelocity = Vector2.zero; invul = Mathf.Max(invul, 0.6f); }
    }
}

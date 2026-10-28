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
        public float ShieldT, SpeedMul = 1f;          // set by skills (Iron Bulwark, Barrier, Haste)
        public SpriteRenderer Body { get { return body; } }
        float bodyBaseY = 0.7f; Vector3 bodyScale = new Vector3(0.6f, 1.35f, 1f); Color bodyColor = new Color(0.55f, 0.75f, 0.9f);

        Rigidbody2D rb; BoxCollider2D box; SpriteRenderer body;
        float coyote, jumpBuffer, invul, stun, dashT, dashCd, atkT = -1, atkCd, comboT, mpRegen, shootCd, animT, recoil; bool wasGrounded = true;
        int combo, jumps; bool airDash, jumpHeld, atkQueued, grounded;
        ContactFilter2D solidFilter = new ContactFilter2D();
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
            body = SpriteFactory.Box(transform, "Body", bodyColor, new Vector2(0.6f, 1.35f), new Vector2(0, 0.7f), 10);
            solidFilter.useTriggers = false;
            Hp = GameState.MaxHp; Mp = GameState.MaxMp;
            Lighting.Point(transform, new Vector3(0, 0.9f, 0), 5.5f, new Color(1f, 0.85f, 0.6f), 0.55f);      // hero's lantern
        }

        // Swap the placeholder box for the class portrait (Art/Characters). Pixel art, feet at the pivot.
        public void ApplyClassArt(string classId)
        {
            string file = classId == "mage" ? "mage_female" : classId == "warrior" ? "warrior_male" : "rogue_male";
            var spr = SpriteFactory.LoadSprite("Art/Characters/" + file, 1.6f, classId != "rogue");   // rogue art is smoother, so no point filtering
            if (spr == null) return;
            body.sprite = spr; body.transform.localPosition = Vector3.zero; bodyBaseY = 0f;
            bodyScale = Vector3.one; bodyColor = Color.white;
        }

        void Update()
        {
            if (Dead || GameManager.Instance.Paused) return;
            ShieldT -= Time.deltaTime;
            var kb = Keyboard.current;
            float dt = Time.deltaTime; animT += dt; shootCd -= dt; recoil -= dt;
            invul -= dt; stun -= dt; dashCd -= dt; atkCd -= dt; comboT -= dt; coyote -= dt; jumpBuffer -= dt;
            mpRegen += dt * (Tuning.MpRegenBase + GameState.Int * Tuning.MpRegenPerInt + GameState.Level * Tuning.MpRegenPerLevel);
            while (mpRegen >= 1f) { mpRegen -= 1f; if (Mp < GameState.MaxMp) Mp++; }

            grounded = CheckGround();
            if (grounded) { coyote = Tuning.Coyote; jumps = 0; airDash = false; }

            if (MenuUI.Instance != null && MenuUI.Instance.Open) return;
            bool kRight = kb != null && (kb.dKey.isPressed || kb.rightArrowKey.isPressed), kLeft = kb != null && (kb.aKey.isPressed || kb.leftArrowKey.isPressed);
            float ax = (kRight || TouchInput.Right ? 1 : 0) - (kLeft || TouchInput.Left ? 1 : 0);
            if ((kb != null && kb.spaceKey.wasPressedThisFrame) || TouchInput.JumpDown) { jumpBuffer = Tuning.JumpBuffer; jumpHeld = true; }
            if ((kb != null && kb.spaceKey.wasReleasedThisFrame) || TouchInput.JumpUp) jumpHeld = false;
            if ((kb != null && kb.jKey.wasPressedThisFrame) || TouchInput.AttackDown) PressAttack();
            if ((kb != null && kb.lKey.wasPressedThisFrame) || TouchInput.DashDown) PressDash(ax);
            if ((kb != null && kb.qKey.wasPressedThisFrame) || TouchInput.PotionDown) DrinkPotion();
            if ((kb != null && kb.eKey.wasPressedThisFrame) || TouchInput.InteractDown) Interact();

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
                    float target = ax * Tuning.RunSpeed * SpeedMul * (atkT >= 0 ? 0.4f : 1f);
                    v.x = Mathf.MoveTowards(v.x, target, (grounded ? 90f : 55f) * dt);
                    if (Mathf.Abs(ax) > 0.1f && atkT < 0) Facing = ax > 0 ? 1 : -1;
                }
                if (jumpBuffer > 0 && stun <= 0)
                {
                    if (coyote > 0) { v.y = Tuning.JumpSpeed; coyote = 0; jumpBuffer = 0; Sfx.Play("jump", 0.6f, Random.Range(0.95f, 1.1f)); }
                    else if (GameState.Flags.Contains("dj") && jumps < 1) { v.y = Tuning.DoubleJumpSpeed; jumps = 1; jumpBuffer = 0; Sfx.Play("jump", 0.6f, 1.3f); Vfx.Burst(transform.position, 1.4f, "b", 0.25f); }
                }
                if (!jumpHeld && v.y > Tuning.JumpCutSpeed) v.y = Tuning.JumpCutSpeed;
                if (v.y < -Tuning.MaxFall) v.y = -Tuning.MaxFall;
            }
            rb.linearVelocity = v;
            UpdateAttack(dt);
            if (grounded && !wasGrounded) { Sfx.Play("land", 0.5f); Vfx.Sparks(transform.position, 3, 2f, "burst_o", null, 0.25f, 0.3f, 4f); }
            wasGrounded = grounded;
            Animate(ax);
            body.color = invul > 0 && Mathf.FloorToInt(Time.time * 20) % 2 == 0 ? new Color(1, 1, 1, 0.4f) : bodyColor;
        }

        // The character art is a single pose, so motion comes from the transform: idle breathing, run bob and lean,
        // jump stretch, a swing twist and a shot recoil.
        void Animate(float ax)
        {
            float sx = 1f, sy = 1f, bob = 0f, rot = 0f; var vel = rb.linearVelocity;
            bool running = grounded && Mathf.Abs(vel.x) > 0.5f;
            if (dashT > 0) { sx = 1.25f; sy = 0.82f; rot = -10f * Facing; }
            else if (!grounded) { float k = Mathf.Clamp(vel.y / 15f, -1f, 1f); sy = 1f + 0.1f * k; sx = 1f - 0.07f * k; }
            else if (running) { bob = Mathf.Abs(Mathf.Sin(animT * 14f)) * 0.07f; rot = -5f * Facing * Mathf.Min(1f, Mathf.Abs(vel.x) / Tuning.RunSpeed); sy = 1f + 0.03f * Mathf.Sin(animT * 28f); }
            else sy = 1f + 0.02f * Mathf.Sin(animT * 3f);
            if (atkT >= 0) { float k = Mathf.Clamp01(atkT / AttackDuration()); rot += Facing * (combo == 2 ? -25f : -14f) * Mathf.Sin(k * Mathf.PI); sx *= 1f + 0.08f * Mathf.Sin(k * Mathf.PI); }
            if (recoil > 0) { rot += Facing * 6f; sx *= 0.94f; }
            if (stun > 0) { rot += Facing * 12f; }
            body.transform.localScale = new Vector3(bodyScale.x * Facing * sx, bodyScale.y * sy, 1);
            body.transform.localRotation = Quaternion.Euler(0, 0, rot);
            body.transform.localPosition = new Vector3(body.transform.localPosition.x, bodyBaseY + bob, 0);
        }

        bool CheckGround()
        {
            if (rb.linearVelocity.y > 0.5f) return false;
            Vector2 origin = (Vector2)transform.position + new Vector2(0, 0.05f);
            int n = Physics2D.BoxCast(origin, new Vector2(box.size.x * 0.9f, 0.05f), 0, Vector2.down, solidFilter, hits, 0.12f);
            for (int i = 0; i < n; i++) if (hits[i].collider != box && !hits[i].collider.isTrigger) return true;
            return false;
        }

        // Wands and bows shoot instead of swinging: tier decides the damage, class decides the look.
        void Shoot()
        {
            if (shootCd > 0) return;
            var kind = GameState.Kind; bool wand = kind == WeaponKind.Wand;
            shootCd = wand ? 0.34f : 0.26f; recoil = 0.1f;
            int dmg = GameState.Attack; bool crit = Random.value < GameState.CritChance; if (crit) dmg = Mathf.RoundToInt(dmg * 1.8f);
            var s = new ProjSpec
            {
                Spr = wand ? "orb_arcane" : "dagger_red", Pos = (Vector2)transform.position + new Vector2(Facing * 0.7f, 0.95f), Vel = new Vector2(Facing * (wand ? 13f : 18f), 0),
                Size = wand ? 0.9f : 1.0f, Dmg = dmg, Life = wand ? 0.9f : 0.75f, Radius = 0.45f, Facing = Facing, Trail = wand ? "burst_p" : "burst_g",
                Tint = wand ? null : "g", BoomKey = wand ? "p" : "g", Align = !wand, BaseDeg = 45f, Home = wand, Turn = 2.5f, Pierce = !wand && GameState.WeaponTier >= 12
            };
            SkillProjectile.Fire(s);
            Sfx.Play(wand ? "shot" : "arrow", 0.6f, Random.Range(0.92f, 1.08f));
            var v = rb.linearVelocity; v.x -= Facing * 1.5f; rb.linearVelocity = v;
        }

        void PressAttack()
        {
            if (dashT > 0 || stun > 0) return;
            if (GameState.Kind != WeaponKind.Sword) { Shoot(); return; }
            if (atkT >= 0) { if (atkT > 0.3f * AttackDuration() && combo < 2) atkQueued = true; return; }
            if (atkCd > 0) return;
            StartAttack(comboT > 0 && combo < 2 ? combo + 1 : 0);
        }

        float AttackDuration() { return combo == 2 ? 0.44f : 0.28f; }

        void StartAttack(int i)
        {
            combo = i; atkT = 0; atkQueued = false; comboT = 0; hitThisSwing.Clear(); Sfx.Play("swing", 0.5f, combo == 2 ? 0.8f : Random.Range(0.95f, 1.1f));
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
                    int dmg = Mathf.RoundToInt(GameState.Attack * (combo == 2 ? 1.8f : 1f)); bool crit = Random.value < GameState.CritChance; if (crit) dmg = Mathf.RoundToInt(dmg * 1.8f);
                    e.TakeDamage(dmg, Facing, combo, crit);
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
            GameState.Potions--; Hp = Mathf.Min(GameState.MaxHp, Hp + 20); Sfx.Play("pickup", 0.7f, 0.8f); Vfx.Burst(transform.position + Vector3.up, 2.2f, "g", 0.35f);
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
            if (ShieldT > 0) { invul = 0.15f; Vfx.Burst(transform.position + Vector3.up * 0.8f, 1.6f, "y", 0.25f); return; }
            dmg = Mathf.Max(1, dmg - GameState.Defense);
            Sfx.Play("hurt", 0.8f); Haptics.Tap(); Vfx.Sparks(transform.position + Vector3.up, 6, 4f, "burst_o", "r", 0.3f, 0.4f, 5f);
            Hp -= dmg; invul = Tuning.Invulnerable; stun = 0.22f; dashT = 0; atkT = -1;
            rb.gravityScale = 1;
            int s = transform.position.x >= fromX ? 1 : -1;
            rb.linearVelocity = new Vector2(s * knock, 8f);
            GameManager.Instance.Shake(0.15f);
            if (Hp <= 0) { Hp = 0; Dead = true; GameManager.Instance.OnPlayerDied(); }
        }

        // Skill helpers: burst of velocity, wall-aware horizontal teleport, short invulnerability.
        public void Launch(Vector2 v) { rb.linearVelocity = v; stun = 0; }
        public void GrantInvul(float t) { invul = Mathf.Max(invul, t); }
        public void Blink(float dx, float invulT)
        {
            Vector2 from = rb.position, dir = new Vector2(Mathf.Sign(dx), 0);
            float allowed = Mathf.Abs(dx);
            var res = Physics2D.BoxCastAll(from + new Vector2(0, 0.7f), new Vector2(box.size.x * 0.9f, box.size.y * 0.85f), 0, dir, allowed);
            foreach (var h in res)
            {
                var c = h.collider;
                if (c == null || c == box || c.isTrigger || c.usedByEffector || c.GetComponentInParent<Enemy>() != null) continue;
                allowed = Mathf.Min(allowed, Mathf.Max(0f, h.distance - 0.05f));
            }
            rb.position = from + dir * allowed; transform.position = rb.position;
            rb.linearVelocity = new Vector2(0, Mathf.Max(0, rb.linearVelocity.y));
            invul = Mathf.Max(invul, invulT);
        }

        public void Heal(int n) { Hp = Mathf.Min(GameState.MaxHp, Hp + n); }
        public void FullRestore() { Hp = GameState.MaxHp; Mp = GameState.MaxMp; }
        public void Revive(Vector2 feet) { Dead = false; transform.position = feet; rb.linearVelocity = Vector2.zero; FullRestore(); invul = 1.3f; }
        public void Teleport(Vector2 feet) { transform.position = feet; rb.linearVelocity = Vector2.zero; invul = Mathf.Max(invul, 0.6f); }
    }
}

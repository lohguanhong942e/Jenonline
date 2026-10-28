using System.Collections.Generic;
using UnityEngine;

namespace Vesperhold
{
    public abstract class Interactable : MonoBehaviour
    {
        public abstract void Use(PlayerController p);
    }

    public static class EntityFactory
    {
        public static void Spawn(RoomEnt e, RoomBuilder room, Transform parent)
        {
            Vector2 feet = new Vector2(e.c + 0.5f, -(e.r + 1));
            switch (e.k)
            {
                case "save": Make<SavePoint>(parent, feet, new Color(1f, 0.6f, 0.2f), new Vector2(0.6f, 0.9f)); break;
                case "chest": { var c = Make<Chest>(parent, feet, new Color(0.7f, 0.5f, 0.2f), new Vector2(0.9f, 0.6f)); c.Setup(e); break; }
                case "candle": { var c = Make<Candle>(parent, feet, new Color(1f, 0.85f, 0.4f), new Vector2(0.25f, 0.8f)); break; }
                case "portal": { var p = Make<Portal>(parent, feet, new Color(0.6f, 0.4f, 1f), new Vector2(0.9f, 1.8f)); p.Setup(e); break; }
                case "relic": { var r = Make<Relic>(parent, feet + Vector2.up * 0.5f, new Color(0.5f, 1f, 1f), new Vector2(0.6f, 0.6f)); r.Kind = string.IsNullOrEmpty(e.kind) ? "dj" : e.kind; break; }
                case "npc": { var n = Make<Npc>(parent, feet, new Color(0.8f, 0.7f, 0.9f), new Vector2(0.6f, 1.3f)); n.Setup(e); break; }
                case "skel": Enemy.Spawn("skeleton", feet, room.Data.elite, parent); break;
                case "bat": Enemy.Spawn("bat", feet + Vector2.up * 0.5f, room.Data.elite, parent); break;
                case "ghost": Enemy.Spawn("ghost", feet + Vector2.up * 0.5f, room.Data.elite, parent); break;
                case "armor": Enemy.Spawn("armor", feet, room.Data.elite, parent); break;
                case "spawn":
                {
                    var go = new GameObject("Spawner_" + e.t);
                    go.transform.SetParent(parent, false);
                    go.transform.position = feet;
                    var s = go.AddComponent<Spawner>();
                    s.Type = e.t; s.Max = Mathf.Max(1, e.max); s.Elite = room.Data.elite; s.Root = parent;
                    break;
                }
            }
        }

        public static void SpawnRelic(Vector2 feet, string kind, Transform parent)
        {
            var r = Make<Relic>(parent, feet + Vector2.up * 0.5f, new Color(0.5f, 1f, 1f), new Vector2(0.6f, 0.6f));
            r.Kind = kind;
        }

        static T Make<T>(Transform parent, Vector2 feet, Color c, Vector2 size) where T : MonoBehaviour
        {
            var go = new GameObject(typeof(T).Name);
            go.transform.SetParent(parent, false);
            go.transform.position = feet;
            var body = SpriteFactory.Box(go.transform, "Body", c, size, new Vector2(0, size.y / 2f), 5);
            switch (typeof(T).Name)      // painted art from Resources/Art/Props when present
            {
                case "SavePoint": SpriteFactory.Skin(body, "Art/Props/brazier", 1.7f); break;
                case "Chest": SpriteFactory.Skin(body, "Art/Props/chest", 0.95f); break;
                case "Candle": SpriteFactory.Skin(body, "Art/Props/candle", 1.0f); break;
                case "Portal": SpriteFactory.Skin(body, "Art/Props/portal", 2.4f); break;
            }
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true; col.size = size; col.offset = new Vector2(0, size.y / 2f);
            return go.AddComponent<T>();
        }
    }

    // Keeps up to Max enemies alive, re-spawning one every 5 to 8 seconds while the player is away.
    public class Spawner : MonoBehaviour
    {
        public string Type; public int Max; public bool Elite; public Transform Root;
        readonly List<Enemy> alive = new List<Enemy>();
        float timer;
        void Update()
        {
            alive.RemoveAll(e => e == null);
            var p = PlayerController.Instance; if (p == null) return;
            if (alive.Count >= Max) { timer = 4f; return; }
            timer -= Time.deltaTime;
            if (timer > 0 || Mathf.Abs(p.transform.position.x - transform.position.x) < 4.4f) return;
            var pos = (Vector2)transform.position + (Type == "bat" || Type == "ghost" ? Vector2.up * 0.5f : Vector2.zero);
            alive.Add(Enemy.Spawn(Type == "skel" ? "skeleton" : Type, pos, Elite, Root));
            timer = 5f + Random.value * 3f;
        }
    }

    public class SavePoint : Interactable
    {
        public override void Use(PlayerController p)
        {
            GameState.SaveRoom = GameManager.Instance.CurrentRoom.Data.id;
            GameState.SaveX = transform.position.x; GameState.SaveY = transform.position.y;
            GameState.Potions = Mathf.Max(GameState.Potions, 2);
            p.FullRestore();
            GameState.Save(); Sfx.Play("level", 0.6f, 1.2f); Vfx.Pillar(transform.position, 3f, "pillar_fire", 0.6f, 1.2f);
            GameManager.Instance.Toast("Brazier lit. Progress saved.");
        }
    }

    public class Candle : MonoBehaviour
    {
        bool smashed;
        public void Smash()
        {
            if (smashed) return; smashed = true;
            if (Random.value < 0.6f) GameState.Hearts = Mathf.Min(99, GameState.Hearts + 1); else GameState.Gold += Random.Range(2, 7);
            Destroy(gameObject);
        }
    }

    public class Chest : Interactable
    {
        RoomEnt e; SpriteRenderer sr;
        public void Setup(RoomEnt ent)
        {
            e = ent; sr = GetComponentInChildren<SpriteRenderer>();
            if (GameState.Opened.Contains(e.id)) sr.color = new Color(0.3f, 0.22f, 0.1f);
        }
        public override void Use(PlayerController p)
        {
            if (GameState.Opened.Contains(e.id)) return;
            GameState.Opened.Add(e.id); sr.color = new Color(0.3f, 0.22f, 0.1f); Sfx.Play("pickup"); Vfx.Burst(transform.position + Vector3.up * 0.5f, 2.4f, "y", 0.4f);
            var parts = new List<string>();
            if (e.gold > 0) { GameState.Gold += e.gold; parts.Add(e.gold + " gold"); }
            if (e.pots > 0) { GameState.Potions = Mathf.Min(9, GameState.Potions + e.pots); parts.Add(e.pots + " potions"); }
            switch (e.item)
            {
                case "dagger": GameState.Potions = Mathf.Min(9, GameState.Potions + 1); parts.Add("a potion (dagger)"); break;
                case "axe": GameState.Gold += 60; parts.Add("60 gold (axe)"); break;
                case "key": GameState.Flags.Add("key"); parts.Add("the Sovereign Key"); GameManager.Instance.CurrentRoom.OpenGate(); break;
                case "armor1": { var a = ItemDb.Armor(5); GameState.Give(a); parts.Add(a.Name); break; }
                case "hpup": GameState.HpUps++; p.FullRestore(); parts.Add("a Heart Vessel"); break;
                default:
                    if (!string.IsNullOrEmpty(e.item) && e.item.StartsWith("sword"))
                    {
                        var w = ItemDb.Weapon(GameState.Kind, ItemDb.FromWebSword(int.Parse(e.item.Substring(5))));
                        GameState.Give(w); parts.Add(w.Name);
                    }
                    break;
            }
            GameManager.Instance.Toast("Found " + string.Join(", ", parts));
            GameState.Save();
        }
    }

    public class Relic : Interactable
    {
        public string Kind = "dj";
        public override void Use(PlayerController p)
        {
            GameState.Flags.Add(Kind);
            GameManager.Instance.Toast(Kind == "dj" ? "Raven Feather: you can jump twice." : "Shadow Step: press L to dash.");
            GameState.Save();
            Destroy(gameObject);
        }
        SpriteRenderer art; float t;
        void Start()
        {
            if (GameState.Flags.Contains(Kind)) { Destroy(gameObject); return; }
            art = GetComponentInChildren<SpriteRenderer>();
            SpriteFactory.Skin(art, Kind == "dj" ? "Art/Props/relic_feather" : "Art/Props/relic_shadowstep", 1.0f);
            Lighting.Point(transform, new Vector3(0, 0.5f, 0), 4f, Kind == "dj" ? new Color(0.5f, 0.7f, 1f) : new Color(0.7f, 0.4f, 1f), 1f);
        }
        void Update() { t += Time.deltaTime; if (art != null) art.transform.localPosition = new Vector3(0, -0.2f + Mathf.Sin(t * 3f) * 0.12f, 0); }
    }

    public class Portal : Interactable
    {
        RoomEnt e;
        public void Setup(RoomEnt ent) { e = ent; }
        public override void Use(PlayerController p)
        {
            if (!string.IsNullOrEmpty(e.need) && !GameState.Flags.Contains(e.need))
            { GameManager.Instance.Toast(string.IsNullOrEmpty(e.lockMsg) ? "The way is sealed." : e.lockMsg); return; }
            GameManager.Instance.GoToRoom(e.to, null);
        }
    }

    public class Npc : Interactable
    {
        RoomEnt e;
        public void Setup(RoomEnt ent) { e = ent; }
        public override void Use(PlayerController p)
        {
            if (e.talk == "mirelda" || e.talk == "borin") { MenuUI.Instance.OpenShop(e.talk, e.name); return; }
            MenuUI.Instance.Say("npc_" + e.talk, e.name, Hint());
        }

        static string Hint()
        {
            var f = GameState.Flags;
            if (!f.Contains("dj")) return "Climb the Grand Library. A Raven Feather rests on the way up.";
            if (!f.Contains("boss1")) return "The Bone Warden sleeps in the Catacombs. Hit him when he lands.";
            if (!f.Contains("dash")) return "The Warden guarded a relic. Go back and claim it.";
            if (!f.Contains("key")) return "The Hall of Arms hides a key on its high gallery. Dash in mid-air to cross the Undercroft gaps.";
            if (!f.Contains("boss2")) return "Climb the Clock Tower and open the sealed gate in the Throne Gallery.";
            return "It is over. Rest, traveller.";
        }
    }

    public class FloatingText : MonoBehaviour
    {
        string text; Color color; float t;
        public static void Spawn(Vector3 pos, string s, Color c)
        {
            var go = new GameObject("FText"); go.transform.position = pos;
            var f = go.AddComponent<FloatingText>(); f.text = s; f.color = c;
            Destroy(go, 0.8f);
        }
        void Update() { t += Time.deltaTime; transform.position += Vector3.up * Time.deltaTime * 1.2f; }
        void OnGUI()
        {
            var cam = Camera.main; if (cam == null) return;
            var sp = cam.WorldToScreenPoint(transform.position);
            var st = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            GUI.color = new Color(color.r, color.g, color.b, 1 - t / 0.8f);
            GUI.Label(new Rect(sp.x - 40, Screen.height - sp.y - 12, 80, 24), text, st);
            GUI.color = Color.white;
        }
    }
}

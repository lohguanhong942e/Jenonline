using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Vesperhold
{
    // Pause menu and shops. Esc opens/closes it; I, K and M jump to Items, Skills and Map.
    // Tabs: Items (equip weapons and armor), Stats (spend points), Skills (what you have and what unlocks),
    // Map (rooms you have seen), Options (volume, touch controls, save, quit). Shops reuse the same window.
    public class MenuUI : MonoBehaviour
    {
        public static MenuUI Instance { get; private set; }
        public bool Open { get; private set; }

        enum Tab { Items, Stats, Skills, Map, Options }
        Tab tab = Tab.Items; bool shopMode; string shopId, shopName;
        ItemDef selected; Vector2 scroll, scroll2; string note = ""; float noteT;
        readonly Dictionary<string, Texture2D> tex = new Dictionary<string, Texture2D>();
        GUIStyle title, body, small, btn, tabBtn;

        void Awake() { Instance = this; }

        // Short dialogue box with a portrait (NPCs) and a boss intro card, both drawn even while the menu is closed.
        string dlgPortrait, dlgName, dlgText; float dlgT;
        string introPortrait, introTitle; float introT;
        public void Say(string portrait, string name, string text) { dlgPortrait = portrait; dlgName = name; dlgText = text; dlgT = 6f; Sfx.Play("click", 0.4f); }
        public void BossIntro(string portrait, string title) { introPortrait = portrait; introTitle = title; introT = 2.8f; }

        void DrawOverlays()
        {
            float W = UiScale.W, H = UiScale.H;
            if (dlgT > 0)
            {
                float w = Mathf.Min(W - 30, 760), h = 150, x = (W - w) / 2f, y = H - 270;
                var panel = new Rect(x, y, w, h);
                GUI.color = new Color(0, 0, 0, 0.78f); GUI.DrawTexture(panel, Texture2D.whiteTexture); GUI.color = Color.white;
                UiArt.Stretch(panel, UiArt.Tex("panel_frame"));
                var pt = UiArt.Portrait(dlgPortrait);
                if (pt != null) GUI.DrawTexture(new Rect(x + 16, y + 14, 122, 122), pt, ScaleMode.ScaleToFit);
                float tx = pt != null ? x + 152 : x + 20;
                GUI.color = new Color(1f, 0.85f, 0.45f); GUI.Label(new Rect(tx, y + 12, w - (tx - x) - 16, 26), dlgName, title); GUI.color = Color.white;
                GUI.Label(new Rect(tx, y + 44, w - (tx - x) - 20, h - 54), dlgText, body);
            }
            if (introT > 0)
            {
                float a = Mathf.Clamp01(Mathf.Min(introT / 0.5f, (2.8f - introT) / 0.3f));
                GUI.color = new Color(0, 0, 0, 0.72f * a); GUI.DrawTexture(new Rect(0, H * 0.3f, W, 190), Texture2D.whiteTexture);
                GUI.color = new Color(1, 1, 1, a);
                var pt = UiArt.Portrait(introPortrait);
                if (pt != null) GUI.DrawTexture(new Rect(W * 0.5f - 330, H * 0.3f - 20, 230, 230), pt, ScaleMode.ScaleToFit);
                GUI.Label(new Rect(W * 0.5f - 80, H * 0.3f + 50, 520, 60), introTitle, new GUIStyle(title) { fontSize = 40 });
                GUI.Label(new Rect(W * 0.5f - 76, H * 0.3f + 110, 520, 30), "A guardian of Vesperhold blocks the way.", small);
                GUI.color = Color.white;
            }
        }

        public void OpenPause() { if (!Open) { shopMode = false; tab = Tab.Options; SetOpen(true); } }

        public void OpenShop(string id, string name)
        {
            shopMode = true; shopId = id; shopName = name; selected = null; SetOpen(true);
        }

        void SetOpen(bool open)
        {
            Open = open; GameManager.Instance.SetPaused(open);
            if (!open) { shopMode = false; GameState.Save(); }
            Sfx.Play("click", 0.5f);
        }

        void Update()
        {
            if (noteT > 0) noteT -= Time.unscaledDeltaTime;
            if (dlgT > 0) dlgT -= Time.unscaledDeltaTime;
            if (introT > 0) introT -= Time.unscaledDeltaTime;
            var kb = Keyboard.current; var gm = GameManager.Instance;
            if (kb == null || gm == null || gm.InMenu || !gm.InRun) return;
            if (kb.escapeKey.wasPressedThisFrame) { if (Open) SetOpen(false); else if (!gm.Paused) { shopMode = false; tab = Tab.Options; SetOpen(true); } }
            else if (!Open && !gm.Paused)
            {
                if (kb.iKey.wasPressedThisFrame) { shopMode = false; tab = Tab.Items; SetOpen(true); }
                else if (kb.kKey.wasPressedThisFrame) { shopMode = false; tab = Tab.Skills; SetOpen(true); }
                else if (kb.mKey.wasPressedThisFrame) { shopMode = false; tab = Tab.Map; SetOpen(true); }
            }
            else if (Open && !shopMode)
            {
                if (kb.iKey.wasPressedThisFrame) tab = Tab.Items; else if (kb.kKey.wasPressedThisFrame) tab = Tab.Skills; else if (kb.mKey.wasPressedThisFrame) tab = Tab.Map;
            }
        }

        Texture2D Tex(string path)
        {
            Texture2D t; if (!tex.TryGetValue(path, out t)) { t = Resources.Load<Texture2D>(path); tex[path] = t; }
            return t;
        }

        void Styles()
        {
            if (title != null) return;
            title = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
            body = new GUIStyle(GUI.skin.label) { fontSize = 15, wordWrap = true };
            small = new GUIStyle(GUI.skin.label) { fontSize = 12, wordWrap = true };
            btn = UiArt.Button(15);
            tabBtn = UiArt.Button(15);
        }

        void OnGUI() { UiScale.Begin(); Draw(); UiScale.End(); }

        void Draw()
        {
            Styles();
            if (GameManager.Instance != null && GameManager.Instance.InRun) DrawOverlays();
            if (!Open) return;
            float w = Mathf.Min(UiScale.W - 24, 900), h = Mathf.Min(UiScale.H - 24, 580);
            var win = new Rect((UiScale.W - w) / 2f, (UiScale.H - h) / 2f, w, h);
            GUI.color = new Color(0.04f, 0.03f, 0.08f, 0.97f); GUI.DrawTexture(win, Texture2D.whiteTexture); GUI.color = Color.white;
            if (!UiArt.Stretch(new Rect(win.x - 8, win.y - 8, win.width + 16, win.height + 16), UiArt.Tex("panel_frame")))
            { GUI.color = new Color(0.8f, 0.65f, 0.3f); GUI.DrawTexture(new Rect(win.x, win.y, win.width, 2), Texture2D.whiteTexture); GUI.color = Color.white; }
            if (shopMode) { Shop(win); }
            else
            {
                string[] names = { "Items (I)", "Stats", "Skills (K)", "Map (M)", "Options" };
                float tw = (w - 24) / 5f - 4;
                for (int i = 0; i < 5; i++)
                {
                    GUI.color = (int)tab == i ? new Color(1f, 0.85f, 0.45f) : Color.white;
                    if (GUI.Button(new Rect(win.x + 12 + i * (tw + 4), win.y + 10, tw, 32), names[i], tabBtn)) { tab = (Tab)i; Sfx.Play("click", 0.4f); }
                }
                GUI.color = Color.white;
                var content = new Rect(win.x + 14, win.y + 52, w - 28, h - 66);
                switch (tab)
                {
                    case Tab.Items: Items(content); break;
                    case Tab.Stats: Stats(content); break;
                    case Tab.Skills: Skills(content); break;
                    case Tab.Map: Map(content); break;
                    default: Options(content); break;
                }
            }
            if (noteT > 0) GUI.Label(new Rect(win.x, win.yMax - 26, win.width, 24), note, new GUIStyle(body) { alignment = TextAnchor.MiddleCenter });
        }

        // ---- Items ----
        void Items(Rect r)
        {
            GUI.Label(new Rect(r.x, r.y, r.width, 28), GameState.Kind + "s   (click an item, then Equip)", title);
            float cell = Mathf.Clamp((r.width - 280) / 10f, 34f, 52f);
            float y = r.y + 34;
            GUI.Label(new Rect(r.x, y, 200, 20), "Weapons", body); y += 20;
            Grid(new Rect(r.x, y, cell * 10, cell * 3), ItemDb.Weapons(GameState.Kind), cell);
            y += cell * 3 + 8;
            GUI.Label(new Rect(r.x, y, 200, 20), "Armor", body); y += 20;
            Grid(new Rect(r.x, y, cell * 11, cell * 3), ItemDb.Armors(), cell);
            // detail panel on the right
            var d = new Rect(r.x + cell * 11 + 14, r.y + 34, r.width - cell * 11 - 14, r.height - 40);
            GUI.color = new Color(1, 1, 1, 0.06f); GUI.DrawTexture(d, Texture2D.whiteTexture); GUI.color = Color.white;
            var it = selected ?? GameState.Equipped;
            Detail(d, it);
        }

        void Grid(Rect r, ItemDef[] list, float cell)
        {
            int cols = Mathf.Max(1, Mathf.FloorToInt(r.width / cell));
            for (int i = 0; i < list.Length; i++)
            {
                var it = list[i]; var c = new Rect(r.x + (i % cols) * cell, r.y + (i / cols) * cell, cell - 3, cell - 3);
                bool owned = GameState.Owns(it), eq = it.IsArmor ? GameState.Armor == it.Tier : (it.Kind == GameState.Kind && GameState.WeaponTier == it.Tier);
                GUI.color = eq ? new Color(1f, 0.85f, 0.3f) : selected == it ? Color.white : ItemDb.RarityColor(it.Rarity) * 0.6f;
                GUI.DrawTexture(new Rect(c.x - 2, c.y - 2, c.width + 4, c.height + 4), Texture2D.whiteTexture);
                GUI.color = new Color(0.08f, 0.07f, 0.12f); GUI.DrawTexture(c, Texture2D.whiteTexture);
                var icon = Tex(it.Icon);
                GUI.color = owned ? Color.white : new Color(0.2f, 0.2f, 0.24f, 1f);
                if (icon != null) GUI.DrawTexture(c, icon, ScaleMode.ScaleToFit);
                GUI.color = Color.white;
                if (GUI.Button(c, GUIContent.none, GUIStyle.none)) { selected = it; Sfx.Play("click", 0.4f); }
            }
        }

        void Detail(Rect d, ItemDef it)
        {
            bool owned = GameState.Owns(it);
            var icon = Tex(it.Icon);
            if (icon != null) { GUI.color = owned ? Color.white : new Color(0.3f, 0.3f, 0.35f); GUI.DrawTexture(new Rect(d.x + 8, d.y + 8, 72, 72), icon, ScaleMode.ScaleToFit); GUI.color = Color.white; }
            GUI.color = ItemDb.RarityColor(it.Rarity);
            GUI.Label(new Rect(d.x + 88, d.y + 8, d.width - 96, 50), it.Name, new GUIStyle(title) { fontSize = 18, wordWrap = true });
            GUI.color = Color.white;
            string stat = it.IsArmor ? "Defense +" + it.Def + "   HP +" + it.Hp : "Attack +" + it.Atk + (it.Kind == WeaponKind.Wand ? "  (magic bolts)" : it.Kind == WeaponKind.Bow ? "  (arrows)" : "  (melee combo)");
            GUI.Label(new Rect(d.x + 8, d.y + 88, d.width - 16, 40), stat, body);
            string rar = new[] { "Common", "Uncommon", "Rare", "Epic", "Legendary" }[it.Rarity];
            GUI.Label(new Rect(d.x + 8, d.y + 112, d.width - 16, 22), "Tier " + it.Tier + "   " + rar, small);
            if (!owned) { GUI.Label(new Rect(d.x + 8, d.y + 136, d.width - 16, 60), "Not found yet. Drops from enemies near your level, chests, or shops.", small); return; }
            bool eq = it.IsArmor ? GameState.Armor == it.Tier : GameState.WeaponTier == it.Tier;
            if (GUI.Button(new Rect(d.x + 8, d.y + 168, 130, 34), eq ? "Equipped" : "Equip", btn) && !eq) Equip(it);
            if (!it.IsArmor && selected == null) { }
        }

        void Equip(ItemDef it)
        {
            if (it.IsArmor) GameState.Armor = it.Tier; else if (it.Kind == GameState.Kind) GameState.WeaponTier = it.Tier;
            var p = PlayerController.Instance; if (p != null) p.Hp = Mathf.Min(p.Hp, GameState.MaxHp);
            Sfx.Play("pickup", 0.6f); Note("Equipped " + it.Name);
        }

        // ---- Stats ----
        void Stats(Rect r)
        {
            GUI.Label(new Rect(r.x, r.y, r.width, 30), "Level " + GameState.Level + "   " + GameState.ClassId.ToUpper() + "   XP " + GameState.Xp + " / " + GameData.XpToLevel(GameState.Level), title);
            GUI.Label(new Rect(r.x, r.y + 34, r.width, 24), "Stat points to spend: " + GameState.StatPoints, body);
            string[] n = { "Strength", "Dexterity", "Intelligence", "Luck" };
            string[] eff = { "+2 max HP and warrior attack", "rogue attack", "+4 max MP, faster mana, mage attack", "+1.2% crit chance" };
            for (int i = 0; i < 4; i++)
            {
                float y = r.y + 70 + i * 44;
                int val = i == 0 ? GameState.Str : i == 1 ? GameState.Dex : i == 2 ? GameState.Int : GameState.Luk;
                GUI.Label(new Rect(r.x, y, 140, 30), n[i], body); GUI.Label(new Rect(r.x + 140, y, 50, 30), val.ToString(), title);
                GUI.enabled = GameState.StatPoints > 0;
                if (GUI.Button(new Rect(r.x + 200, y, 40, 30), "+", btn)) { GameState.StatPoints--; if (i == 0) GameState.Str++; else if (i == 1) GameState.Dex++; else if (i == 2) GameState.Int++; else GameState.Luk++; Sfx.Play("click", 0.5f); }
                GUI.enabled = true;
                GUI.Label(new Rect(r.x + 250, y + 4, r.width - 260, 30), eff[i], small);
            }
            float by = r.y + 70 + 4 * 44 + 10;
            GUI.Label(new Rect(r.x, by, r.width, 100),
                "Max HP " + GameState.MaxHp + "     Max MP " + GameState.MaxMp + "\nAttack " + GameState.Attack + "     Defense " + GameState.Defense + "     Crit " + Mathf.RoundToInt(GameState.CritChance * 100f) + "%\nWeapon: " + GameState.Equipped.Name + "     Armor: " + (GameState.Armor == 0 ? "none" : ItemDb.Armor(GameState.Armor).Name), body);
        }

        // ---- Skills ----
        void Skills(Rect r)
        {
            var cls = SkillCatalog.Get(GameState.ClassId);
            GUI.Label(new Rect(r.x, r.y, r.width, 28), cls.name + " skills", title);
            var view = new Rect(r.x, r.y + 32, r.width, r.height - 36);
            UiScale.DragScroll(view, ref scroll);
            scroll = GUI.BeginScrollView(view, scroll, new Rect(0, 0, view.width - 20, cls.skills.Length * 84));
            for (int i = 0; i < cls.skills.Length; i++)
            {
                var s = cls.skills[i]; float y = i * 84; bool on = GameState.Level >= s.unlockLevel;
                GUI.color = on ? Color.white : new Color(0.4f, 0.4f, 0.45f);
                var ic = Tex(s.icon); if (ic != null) GUI.DrawTexture(new Rect(0, y + 4, 64, 64), ic, ScaleMode.ScaleToFit);
                GUI.Label(new Rect(74, y, 460, 24), (i + 1) + ". " + s.name + (on ? "" : "   (unlocks at level " + s.unlockLevel + ")") + "   MP " + SkillCatalog.ManaCost(i), body);
                GUI.Label(new Rect(74, y + 22, view.width - 100, 20), s.desc, small);
                for (int k = 0; k < 2; k++)
                {
                    bool up = on && GameState.Level >= s.upgrades[k].unlockLevel;
                    GUI.color = up ? new Color(1f, 0.85f, 0.4f) : new Color(0.45f, 0.45f, 0.5f);
                    GUI.Label(new Rect(74, y + 40 + k * 18, view.width - 100, 20), (up ? "[x] " : "[ ] ") + s.upgrades[k].name + "  (Lv " + s.upgrades[k].unlockLevel + "): " + s.upgrades[k].desc, small);
                }
                GUI.color = Color.white;
            }
            GUI.EndScrollView();
        }

        // ---- Map ----
        void Map(Rect r)
        {
            GUI.Label(new Rect(r.x, r.y, r.width, 28), "Map", title);
            int minX = 99, minY = 99, maxX = -99, maxY = -99; var seen = new List<RoomData>();
            foreach (var rm in GameData.Rooms.rooms) if (GameState.Seen.Contains(rm.id)) { seen.Add(rm); minX = Mathf.Min(minX, rm.map.x); minY = Mathf.Min(minY, rm.map.y); maxX = Mathf.Max(maxX, rm.map.x + rm.map.w); maxY = Mathf.Max(maxY, rm.map.y + rm.map.h); }
            if (seen.Count == 0) return;
            float cs = Mathf.Min((r.width - 20) / (maxX - minX), (r.height - 70) / (maxY - minY), 48f);
            float ox = r.x + (r.width - cs * (maxX - minX)) / 2f, oy = r.y + 44;
            string cur = GameManager.Instance.CurrentRoom != null ? GameManager.Instance.CurrentRoom.Data.id : "";
            foreach (var rm in seen)
            {
                var rc = new Rect(ox + (rm.map.x - minX) * cs, oy + (rm.map.y - minY) * cs, rm.map.w * cs - 3, rm.map.h * cs - 3);
                GUI.color = rm.id == cur ? new Color(1f, 0.8f, 0.3f) : new Color(0.35f, 0.4f, 0.6f); GUI.DrawTexture(rc, Texture2D.whiteTexture);
                GUI.color = new Color(0.07f, 0.07f, 0.12f); GUI.DrawTexture(new Rect(rc.x + 2, rc.y + 2, rc.width - 4, rc.height - 4), Texture2D.whiteTexture);
                GUI.color = rm.id == cur ? new Color(1f, 0.8f, 0.3f) : Color.white;
                GUI.Label(new Rect(rc.x + 4, rc.y + 2, rc.width - 8, rc.height - 4), rm.name, new GUIStyle(small) { fontSize = Mathf.Max(9, Mathf.RoundToInt(cs * 0.22f)) });
                GUI.color = Color.white;
            }
        }

        // ---- Options ----
        void Options(Rect r)
        {
            GUI.Label(new Rect(r.x, r.y, r.width, 28), "Options", title);
            float y = r.y + 44;
            GUI.Label(new Rect(r.x, y, 160, 24), "Sound effects", body); Sfx.Volume = GUI.HorizontalSlider(new Rect(r.x + 170, y + 6, 240, 20), Sfx.Volume, 0f, 1f); y += 34;
            GUI.Label(new Rect(r.x, y, 160, 24), "Music", body); Sfx.MusicVolume = GUI.HorizontalSlider(new Rect(r.x + 170, y + 6, 240, 20), Sfx.MusicVolume, 0f, 0.6f); y += 34;
            Sfx.Muted = GUI.Toggle(new Rect(r.x, y, 300, 26), Sfx.Muted, " Mute everything"); y += 32;
            TouchInput.Force = GUI.Toggle(new Rect(r.x, y, 360, 26), TouchInput.Force, " Show touch controls (phones show them automatically)"); y += 32;
            Haptics.On = GUI.Toggle(new Rect(r.x, y, 300, 26), Haptics.On, " Vibration"); y += 32;
            if (GUI.Button(new Rect(r.x, y, 200, 30), "Toggle fullscreen", btn)) WebBridge.ToggleFullscreen(); y += 40;
            Perf.BatterySaver = GUI.Toggle(new Rect(r.x, y, 360, 26), Perf.BatterySaver, " Battery saver (30 fps)"); y += 44;
            if (GUI.Button(new Rect(r.x, y, 160, 38), "Resume", btn)) SetOpen(false);
            if (GUI.Button(new Rect(r.x + 170, y, 160, 38), "Save game", btn)) { GameState.Save(); Note("Game saved."); }
            if (GUI.Button(new Rect(r.x + 340, y, 160, 38), "Quit to title", btn)) { SetOpen(false); GameManager.Instance.QuitToTitle(); }
            y += 56;
            GUI.Label(new Rect(r.x, y, r.width, 120), "Keys: A/D move, Space jump (hold for height), J attack, L dash, Q potion, E use,\n1-9, 0, -, = skills, I items, K skills, M map, Esc menu.", small);
        }

        // ---- Shop ----
        void Shop(Rect win)
        {
            bool mirelda = shopId == "mirelda";
            var shopFace = UiArt.Portrait("npc_" + shopId);
            if (shopFace != null) GUI.DrawTexture(new Rect(win.xMax - 150, win.y + 14, 120, 120), shopFace, ScaleMode.ScaleToFit);
            GUI.Label(new Rect(win.x + 16, win.y + 10, win.width - 32, 30), shopName + (mirelda ? " sells potions and light gear" : " forges heavy steel"), title);
            GUI.Label(new Rect(win.x + 16, win.y + 42, win.width - 32, 24), "Gold: " + GameState.Gold, body);
            var list = new List<ItemDef>();
            int lo = mirelda ? 2 : 8, hi = mirelda ? 9 : 18;
            foreach (var w in ItemDb.Weapons(GameState.Kind)) if (w.Tier >= lo && w.Tier <= hi) list.Add(w);
            int alo = mirelda ? 1 : 6, ahi = mirelda ? 8 : 18;
            foreach (var a in ItemDb.Armors()) if (a.Tier >= alo && a.Tier <= ahi) list.Add(a);
            var view = new Rect(win.x + 16, win.y + 72, win.width - 32, win.height - 130);
            UiScale.DragScroll(view, ref scroll2);
            scroll2 = GUI.BeginScrollView(view, scroll2, new Rect(0, 0, view.width - 20, 52 + list.Count * 46));
            // potion row
            GUI.Label(new Rect(4, 8, 360, 30), "Healing Potion  (restores 20 HP)   25 gold   you have " + GameState.Potions, body);
            if (GUI.Button(new Rect(view.width - 150, 6, 120, 32), "Buy", btn)) { if (GameState.Gold >= 25 && GameState.Potions < 9) { GameState.Gold -= 25; GameState.Potions++; Sfx.Play("coin"); } else Note(GameState.Potions >= 9 ? "You cannot carry more potions." : "Not enough gold."); }
            for (int i = 0; i < list.Count; i++)
            {
                var it = list[i]; float y = 46 + i * 46; bool owned = GameState.Owns(it);
                var ic = Tex(it.Icon); if (ic != null) GUI.DrawTexture(new Rect(4, y, 40, 40), ic, ScaleMode.ScaleToFit);
                GUI.color = ItemDb.RarityColor(it.Rarity);
                GUI.Label(new Rect(52, y + 2, view.width - 240, 22), it.Name + (it.IsArmor ? "  (armor)" : ""), body);
                GUI.color = Color.white;
                GUI.Label(new Rect(52, y + 22, view.width - 240, 20), (it.IsArmor ? "Defense +" + it.Def + "  HP +" + it.Hp : "Attack +" + it.Atk), small);
                if (owned) GUI.Label(new Rect(view.width - 150, y + 6, 120, 30), "Owned", body);
                else if (GUI.Button(new Rect(view.width - 190, y + 4, 160, 32), it.Price + " gold", btn))
                {
                    if (GameState.Gold >= it.Price) { GameState.Gold -= it.Price; GameState.Give(it); Sfx.Play("coin"); Note("Bought " + it.Name); }
                    else Note("Not enough gold.");
                }
            }
            GUI.EndScrollView();
            if (GUI.Button(new Rect(win.xMax - 150, win.yMax - 48, 130, 36), "Leave", btn)) SetOpen(false);
        }

        void Note(string s) { note = s; noteT = 2f; }
    }
}

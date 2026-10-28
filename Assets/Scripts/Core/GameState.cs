using System;
using System.Collections.Generic;
using UnityEngine;

namespace Vesperhold
{
    // Run state that survives room changes and is written to PlayerPrefs.
    public static class GameState
    {
        public static readonly HashSet<string> Flags = new HashSet<string>();     // dj, dash, key, boss1, boss2, met
        public static readonly HashSet<string> Opened = new HashSet<string>();    // opened chest ids
        public static readonly HashSet<string> Seen = new HashSet<string>();      // visited rooms
        public static readonly HashSet<string> Items = new HashSet<string>();     // owned gear: "S3" sword, "W5" wand, "B2" bow, "A7" armor
        public static string ClassId = "warrior";
        public static int Level = 1, Xp, Gold, Potions = 2, Hearts = 5, WeaponTier = 1, Armor, HpUps, StatPoints;   // Armor: 0 = none, else armor tier 1-32
        public static int Str = 8, Dex = 4, Int = 4, Luk = 4;
        public static string SaveRoom = "gate"; public static float SaveX = 6.5f, SaveY = -12f;

        public static void NewRun()
        {
            Flags.Clear(); Opened.Clear(); Seen.Clear();
            Items.Clear();
            Level = 1; Xp = 0; Gold = 0; Potions = 2; Hearts = 5; WeaponTier = 1; Armor = 0; HpUps = 0; StatPoints = 0;
            SetClass("warrior");
            SaveRoom = "gate";
        }

        public static void SetClass(string id)
        {
            ClassId = id;
            foreach (var c in GameData.Classes.classes)
                if (c.id == id) { Str = c.str; Dex = c.dex; Int = c.@int; Luk = c.luk; }
            Items.Add(ItemDb.Weapon(Kind, 1).Key); WeaponTier = 1;
        }

        public static WeaponKind Kind { get { return ItemDb.KindFor(ClassId); } }
        public static ItemDef Equipped { get { return ItemDb.Weapon(Kind, WeaponTier); } }
        public static bool Owns(ItemDef i) { return Items.Contains(i.Key); }
        // Mages scale with Int, rogues with Dex, warriors with Str.
        public static int Primary { get { return ClassId == "mage" ? Int : ClassId == "rogue" ? Dex : Str; } }
        public static float CritChance { get { return 0.05f + Luk * 0.012f; } }

        // Grants an item and equips it when it beats what you wear. Returns true if it was new.
        public static bool Give(ItemDef i)
        {
            if (!Items.Add(i.Key)) return false;
            if (i.IsArmor) { if (i.Tier > Armor) Armor = i.Tier; }
            else if (i.Kind == Kind && i.Tier > WeaponTier) WeaponTier = i.Tier;
            return true;
        }

        public static int MaxHp { get { return 30 + 5 * (Level - 1) + 8 * HpUps + (Str - 4) * 2 + ItemDb.ArmorHp(Armor); } }
        public static int MaxMp { get { return 20 + (Int - 4) * 4 + (Level - 1) * 3; } }
        public static float AtkBuff = 1f;      // War Cry / Blood Rage multiply attack while active
        public static int Attack { get { return Mathf.RoundToInt((4 + Equipped.Atk + (Level - 1) / 2 + Mathf.FloorToInt((Primary - 4) * 0.6f)) * AtkBuff); } }
        public static int Defense { get { return ItemDb.ArmorDef(Armor); } }

        // Returns true when the level changed.
        public static bool AddXp(int n)
        {
            Xp += n;
            bool up = false;
            while (Xp >= GameData.XpToLevel(Level)) { Xp -= GameData.XpToLevel(Level); Level++; StatPoints += 5; up = true; }
            return up;
        }

        [Serializable]
        class SaveBlob
        {
            public string room, cls; public float x, y;
            public int level, xp, gold, potions, hearts, weapon, armor, hpUps, sp, str, dex, intl, luk;
            public string[] flags, opened, seen, items;
        }

        const string Key = "vesperhold-save1";
        public static bool HasSave() { return PlayerPrefs.HasKey(Key); }

        public static void Save()
        {
            var b = new SaveBlob
            {
                room = SaveRoom, x = SaveX, y = SaveY, cls = ClassId, level = Level, xp = Xp, gold = Gold, potions = Potions,
                hearts = Hearts, weapon = WeaponTier, armor = Armor, hpUps = HpUps, sp = StatPoints, str = Str, dex = Dex, intl = Int, luk = Luk,
                flags = new List<string>(Flags).ToArray(), opened = new List<string>(Opened).ToArray(), seen = new List<string>(Seen).ToArray(),
                items = new List<string>(Items).ToArray()
            };
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(b));
            PlayerPrefs.Save();
        }

        public static bool Load()
        {
            if (!HasSave()) return false;
            var b = JsonUtility.FromJson<SaveBlob>(PlayerPrefs.GetString(Key));
            NewRun();
            SaveRoom = b.room; SaveX = b.x; SaveY = b.y; ClassId = b.cls; Level = b.level; Xp = b.xp; Gold = b.gold; Potions = b.potions;
            Hearts = b.hearts; WeaponTier = Mathf.Max(1, b.weapon); Armor = b.armor; HpUps = b.hpUps; StatPoints = b.sp; Str = b.str; Dex = b.dex; Int = b.intl; Luk = b.luk;
            foreach (var f in b.flags) Flags.Add(f);
            foreach (var o in b.opened) Opened.Add(o);
            foreach (var s in b.seen) Seen.Add(s);
            if (b.items != null) foreach (var it in b.items) Items.Add(it);
            Items.Add(ItemDb.Weapon(Kind, 1).Key);
            return true;
        }
    }
}

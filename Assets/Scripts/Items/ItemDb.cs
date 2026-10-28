using System.Collections.Generic;
using UnityEngine;

namespace Vesperhold
{
    public enum WeaponKind { Sword, Wand, Bow }

    public class ItemDef
    {
        public string Name, Icon, Key;          // Key is "S3", "W12", "B7" or "A5" (kind letter + tier)
        public int Tier, Atk, Def, Hp, Price, Rarity;
        public WeaponKind Kind; public bool IsArmor;
    }

    // 30 swords, 30 wands, 30 bows and 32 armors (the Canva icons in Art/Items), basic to legendary.
    // Stats follow one smooth curve per slot so every tier is a clear step up. Tweak the formulas here.
    public static class ItemDb
    {
        static readonly string[] Adj = {
            "Rusty", "Worn", "Plain", "Hunter's", "Iron", "Oaken", "Bronze", "Watchman's", "Steel", "Silver",
            "Knight's", "Ember", "Frost", "Venom", "Storm", "Moonsteel", "Crimson", "Gilded", "Sunforged", "Ashen",
            "Shadow", "Runed", "Dragonbone", "Starlit", "Warden's", "Royal", "Eclipse", "Voidtouched", "Celestial", "Vesper's" };
        static readonly string[] ArmorNames = {
            "Ragged Tunic", "Padded Jerkin", "Leather Vest", "Studded Hide", "Chain Coat", "Iron Mail", "Steel Cuirass", "Knight's Plate",
            "Ember Mail", "Frostguard", "Venomscale", "Stormplate", "Moonsteel Harness", "Crimson Aegis", "Gilded Hauberk", "Sunforged Plate",
            "Ashen Brigandine", "Shadow Shroud", "Runed Cuirass", "Dragonbone Mail", "Starlit Robes", "Warden's Bulwark", "Royal Regalia", "Eclipse Plate",
            "Voidtouched Armor", "Celestial Mail", "Vesper's Shroud", "Titan's Bastion", "Seraph's Embrace", "Nightfall Aegis", "Ancient Guardian", "Armor of the Dawn" };

        static ItemDef[] swords, wands, bows, armors;

        static ItemDef[] BuildWeapons(WeaponKind k, string noun, string folder, string file, string letter)
        {
            var list = new ItemDef[30];
            for (int i = 0; i < 30; i++)
            {
                int t = i + 1;
                list[i] = new ItemDef
                {
                    Kind = k, Tier = t, Key = letter + t, Name = Adj[i] + " " + noun,
                    Atk = Mathf.RoundToInt((t - 1) * 1.35f), Rarity = Mathf.Min(4, (t - 1) / 6),
                    Price = t == 1 ? 0 : Mathf.RoundToInt(22f * Mathf.Pow(t, 1.7f)),
                    Icon = "Art/Items/" + folder + "/" + file + "_" + t.ToString("00")
                };
            }
            return list;
        }

        static void Build()
        {
            if (swords != null) return;
            swords = BuildWeapons(WeaponKind.Sword, "Blade", "Swords", "sword", "S");
            wands = BuildWeapons(WeaponKind.Wand, "Wand", "Wands", "wand", "W");
            bows = BuildWeapons(WeaponKind.Bow, "Bow", "Bows", "bow", "B");
            armors = new ItemDef[32];
            for (int i = 0; i < 32; i++)
            {
                int t = i + 1;
                armors[i] = new ItemDef
                {
                    IsArmor = true, Tier = t, Key = "A" + t, Name = ArmorNames[i], Def = 1 + i / 4, Hp = t * 3,
                    Rarity = Mathf.Min(4, i / 7), Price = Mathf.RoundToInt(30f * Mathf.Pow(t, 1.65f)),
                    Icon = "Art/Items/Armor/armor_" + t.ToString("00")
                };
            }
        }

        public static WeaponKind KindFor(string classId) { return classId == "mage" ? WeaponKind.Wand : classId == "rogue" ? WeaponKind.Bow : WeaponKind.Sword; }

        public static ItemDef[] Weapons(WeaponKind k) { Build(); return k == WeaponKind.Wand ? wands : k == WeaponKind.Bow ? bows : swords; }
        public static ItemDef Weapon(WeaponKind k, int tier) { return Weapons(k)[Mathf.Clamp(tier, 1, 30) - 1]; }
        public static ItemDef[] Armors() { Build(); return armors; }
        public static ItemDef Armor(int tier) { Build(); return tier <= 0 ? null : armors[Mathf.Clamp(tier, 1, 32) - 1]; }
        public static int ArmorDef(int tier) { var a = Armor(tier); return a == null ? 0 : a.Def; }
        public static int ArmorHp(int tier) { var a = Armor(tier); return a == null ? 0 : a.Hp; }

        public static Color RarityColor(int r)
        {
            switch (r) { case 1: return new Color(0.45f, 0.85f, 0.45f); case 2: return new Color(0.4f, 0.65f, 1f); case 3: return new Color(0.75f, 0.45f, 1f); case 4: return new Color(1f, 0.7f, 0.25f); default: return new Color(0.8f, 0.8f, 0.8f); }
        }

        // The 10 named blades of the web build map onto tiers of the new ladder (chests and old saves).
        static readonly int[] WebSwordTier = { 1, 3, 6, 4, 5, 5, 8, 9, 10, 14 };
        public static int FromWebSword(int idx) { return WebSwordTier[Mathf.Clamp(idx, 0, 9)]; }
    }
}

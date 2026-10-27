using System;
using System.Collections.Generic;
using UnityEngine;

namespace Vesperhold
{
    // Data tables exported from the browser version (Assets/Resources/Data/*.json).
    // Units: 1 tile = 1 Unity unit. Room grids are row-major with row 0 at the top.
    public static class Tiles
    {
        public const int Empty = 0, Solid = 2, OneWay = 3, Spike = 4, SealedGate = 5;
    }

    [Serializable] public class MapRect { public int x, y, w, h; }

    [Serializable]
    public class RoomEnt
    {
        public string k;      // save, npc, chest, candle, skel, bat, ghost, armor, spawn, relic, portal, chand
        public int c, r;      // tile column and row
        public string id, item, look, name, talk, to, label, need, lockMsg, t, kind;
        public int gold, pots, max;
    }

    [Serializable] public class RoomDoor { public string side, id, to; public int r0, r1; }
    [Serializable] public class RoomTorch { public float x, y; }   // pixels in the web version
    [Serializable] public class RoomDecor { public string k, side; public float x, y; }
    [Serializable] public class Arena { public int x0, trigger, lockCol, bx; public string boss, id; }
    [Serializable] public class SealGate { public int c, r0, r1; }

    [Serializable]
    public class RoomData
    {
        public string id, name, theme;
        public int w, h;
        public float dark;
        public MapRect map;
        public int[] grid;
        public RoomEnt[] ents;
        public RoomDoor[] doors;
        public RoomTorch[] torches;
        public RoomDecor[] decor;
        public bool hasArena; public Arena arena;
        public bool hasGate; public SealGate gate;
        public int lockCol;
        public bool elite;

        public int At(int col, int row)
        {
            if (col < 0 || row < 0 || col >= w || row >= h) return Tiles.Solid;
            return grid[row * w + col];
        }
    }

    [Serializable] public class RoomFile { public int tile; public RoomData[] rooms; }

    [Serializable]
    public class EnemyDef
    {
        public string id; public int hp, dmg, xp, goldMin, goldMax;
        public float w, h;                 // pixels in the web version
    }
    [Serializable] public class EnemyFile { public EnemyDef[] enemies; }

    [Serializable] public class SwordDef { public string n, info, src; public int atk, rar; }
    [Serializable] public class SwordFile { public SwordDef[] swords; }

    [Serializable] public class SkillDef { public string n, d; public int mp; public float cd; }
    [Serializable]
    public class ClassDef
    {
        public string id, name, color, desc;
        public int str, dex, @int, luk;
        public SkillDef[] skills;
    }
    [Serializable] public class ClassFile { public ClassDef[] classes; }

    public static class GameData
    {
        public static RoomFile Rooms;
        public static EnemyFile Enemies;
        public static SwordFile Swords;
        public static ClassFile Classes;
        public static readonly int[] SkillUnlockLevels = { 1, 3, 6, 10 };
        static Dictionary<string, RoomData> roomById;
        static Dictionary<string, EnemyDef> enemyById;

        public static void Load()
        {
            if (Rooms != null) return;
            Rooms = JsonUtility.FromJson<RoomFile>(Resources.Load<TextAsset>("Data/rooms").text);
            Enemies = JsonUtility.FromJson<EnemyFile>(Resources.Load<TextAsset>("Data/enemies").text);
            Swords = JsonUtility.FromJson<SwordFile>(Resources.Load<TextAsset>("Data/swords").text);
            Classes = JsonUtility.FromJson<ClassFile>(Resources.Load<TextAsset>("Data/classes").text);
            roomById = new Dictionary<string, RoomData>();
            foreach (var r in Rooms.rooms) roomById[r.id] = r;
            enemyById = new Dictionary<string, EnemyDef>();
            foreach (var e in Enemies.enemies) enemyById[e.id] = e;
        }

        public static RoomData Room(string id) { Load(); return roomById[id]; }
        public static EnemyDef Enemy(string id) { Load(); return enemyById[id]; }

        public static int XpToLevel(int level) { return level * 40; }
    }

    public static class Tuning
    {
        // Converted from the web build (pixels and pixels per second, 16 px per tile).
        public const float Gravity = 45f;
        public const float MaxFall = 23.75f;
        public const float RunSpeed = 6.5f;
        public const float JumpSpeed = 14.9f;
        public const float DoubleJumpSpeed = 14.5f;
        public const float JumpCutSpeed = 5.9f;
        public const float DashSpeed = 15.3f;
        public const float DashTime = 0.2f;
        public const float DashCooldown = 0.55f;
        public const float Coyote = 0.1f;
        public const float JumpBuffer = 0.12f;
        public const float Invulnerable = 1.1f;
        public const float EliteHp = 2.4f, EliteDamage = 1.5f, EliteGold = 2.5f, EliteXp = 2.6f;
    }
}

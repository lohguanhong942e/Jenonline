using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Vesperhold
{
    // Builds a playable room from RoomData: tile colliders, one-way platforms, spikes, doors and entities.
    public class RoomBuilder : MonoBehaviour
    {
        public RoomData Data { get; private set; }
        public Transform EntityRoot { get; private set; }
        readonly Dictionary<string, Vector2> doorSpawns = new Dictionary<string, Vector2>();
        readonly List<SpikeZone> spikes = new List<SpikeZone>();
        GameObject sealedGate;

        public static Vector3 CellCenter(int col, int row) { return new Vector3(col + 0.5f, -row - 0.5f, 0); }

        public static RoomBuilder Build(RoomData data, Transform parent)
        {
            var go = new GameObject("Room_" + data.id);
            go.transform.SetParent(parent, false);
            var rb = go.AddComponent<RoomBuilder>();
            rb.Data = data;
            rb.Construct();
            return rb;
        }

        void Construct()
        {
            var palette = Palette.For(Data.theme);
            BuildBackdrop(palette);
            BuildTiles(palette);
            BuildDoors();
            BuildEntities();
            BuildGate();
            Lighting.Global(transform, Mathf.Lerp(1f, 0.42f, Data.dark), Color.Lerp(Color.white, palette.Platform, 0.3f));
        }

        void BuildBackdrop(Palette p)
        {
            var bg = new GameObject("Backdrop");
            bg.transform.SetParent(transform, false);
            var sr = bg.AddComponent<SpriteRenderer>();
            sr.sortingOrder = -100;
            // Optional painted backdrop: drop a PNG named after the room theme (gate, hall, library, ...)
            // into Assets/Resources/Art/Backdrops/. Falls back to a flat colour.
            var tex = Resources.Load<Texture2D>("Art/Backdrops/" + Data.id);          // room-specific art first (field, depths)
            if (tex == null) tex = Resources.Load<Texture2D>("Art/Backdrops/" + Data.theme);
            if (tex == null) tex = TileArt.Backdrop(Data.theme);
            if (tex != null)
            {
                sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.height / (float)(Data.h + 2));
                bg.AddComponent<ParallaxBackdrop>().Init(Data, tex);
                return;
            }
            sr.sprite = SpriteFactory.Square;
            sr.color = p.Back;
            bg.transform.position = new Vector3(Data.w / 2f, -Data.h / 2f, 5);
            bg.transform.localScale = new Vector3(Data.w + 40, Data.h + 40, 1);
        }

        void BuildTiles(Palette p)
        {
            var solid = MakeLayer("Solid", false, p.Solid);
            var oneWay = MakeLayer("OneWay", true, p.Platform);
            var hazard = MakeLayer("Hazard", false, p.Spike);
            hazard.GetComponent<TilemapCollider2D>().isTrigger = true;
            var platTile = TileArt.Platform(Data.theme);
            var spikeTile = TileArt.Spike(Data.theme);
            for (int r = 0; r < Data.h; r++)
            for (int c = 0; c < Data.w; c++)
            {
                int t = Data.At(c, r);
                var cell = new Vector3Int(c, -r - 1, 0);
                if (t == Tiles.Solid) solid.SetTile(cell, TileArt.Solid(Data.theme, (int)((c * 7 + r * 13) % 4), Data.At(c, r - 1) != Tiles.Solid));
                else if (t == Tiles.OneWay) oneWay.SetTile(cell, platTile);
                else if (t == Tiles.Spike)
                {
                    hazard.SetTile(cell, spikeTile);
                }
            }
            foreach (var tm in new[] { solid, oneWay, hazard }) tm.GetComponent<TilemapCollider2D>().ProcessTilemapChanges();
            var haz = hazard.gameObject.AddComponent<SpikeZone>();
            spikes.Add(haz);
        }

        Tilemap MakeLayer(string name, bool oneWay, Color tint)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.AddComponent<Grid>();
            var tm = go.AddComponent<Tilemap>();
            var tr = go.AddComponent<TilemapRenderer>();
            tr.sortingOrder = oneWay ? -1 : 0;
            var col = go.AddComponent<TilemapCollider2D>();
            if (oneWay)
            {
                col.usedByComposite = false;
                var eff = go.AddComponent<PlatformEffector2D>();
                eff.useOneWay = true;
                eff.surfaceArc = 160f;
                col.usedByEffector = true;
            }
            return tm;
        }

        void BuildDoors()
        {
            foreach (var d in Data.doors)
            {
                int col = d.side == "L" ? 0 : Data.w - 1;
                float x = d.side == "L" ? 0.5f : Data.w - 0.5f;
                var go = new GameObject("Door_" + d.id);
                go.transform.SetParent(transform, false);
                go.transform.position = new Vector3(x, -(d.r0 + d.r1 + 1) / 2f, 0);
                var box = go.AddComponent<BoxCollider2D>();
                box.isTrigger = true;
                box.size = new Vector2(1f, d.r1 - d.r0 + 1);
                var dt = go.AddComponent<DoorTrigger>();
                dt.DoorId = d.id;
                dt.TargetDoorId = d.to;
                float sx = d.side == "L" ? 2.7f : Data.w - 2.7f;
                doorSpawns[d.id] = new Vector2(sx, -(d.r1 + 1));
            }
        }

        public Vector2 SpawnForDoor(string doorId)
        {
            return doorSpawns.TryGetValue(doorId, out var v) ? v : DefaultSpawn();
        }

        public Vector2 DefaultSpawn()
        {
            foreach (var e in Data.ents)
                if (e.k == "save") return new Vector2(e.c + 0.5f, -(e.r + 1));
            return new Vector2(3f, -(Data.h - 2));
        }

        void BuildEntities()
        {
            var root = new GameObject("Entities");
            root.transform.SetParent(transform, false);
            EntityRoot = root.transform;
            foreach (var e in Data.ents) EntityFactory.Spawn(e, this, root.transform);
            if (Data.hasArena)
            {
                var ag = new GameObject("Arena");
                ag.transform.SetParent(transform, false);
                ag.AddComponent<ArenaController>().Init(Data);
            }
            foreach (var t in Data.torches)
            {
                var go = new GameObject("Torch");
                go.transform.SetParent(root.transform, false);
                go.transform.position = new Vector3(t.x / 16f, -t.y / 16f, 0);
                go.AddComponent<TorchLight>();
            }
        }

        void BuildGate()
        {
            if (!Data.hasGate) return;
            if (GameState.Flags.Contains("key")) return;
            sealedGate = new GameObject("SealedGate");
            sealedGate.transform.SetParent(transform, false);
            float h = Data.gate.r1 - Data.gate.r0 + 1;
            sealedGate.transform.position = new Vector3(Data.gate.c + 0.5f, -(Data.gate.r0 + h / 2f), 0);
            var sr = sealedGate.AddComponent<SpriteRenderer>();
            var art = SpriteFactory.LoadSprite("Art/Props/gate_sealed", h, false);
            if (art != null) { sr.sprite = art; sr.sortingOrder = 3; sealedGate.transform.position = new Vector3(Data.gate.c + 0.5f, -(Data.gate.r1 + 1), 0); sealedGate.transform.localScale = Vector3.one;
                var gb = sealedGate.AddComponent<BoxCollider2D>(); gb.size = new Vector2(1f, h); gb.offset = new Vector2(0, h / 2f); }
            else
            {
                sr.sprite = SpriteFactory.Square;
                sr.color = new Color(0.55f, 0.2f, 0.7f);
                sealedGate.transform.localScale = new Vector3(1, h, 1);
                var box = sealedGate.AddComponent<BoxCollider2D>();
                box.size = Vector2.one;
            }
        }

        public void OpenGate()
        {
            if (sealedGate != null) Destroy(sealedGate);
        }
    }
}

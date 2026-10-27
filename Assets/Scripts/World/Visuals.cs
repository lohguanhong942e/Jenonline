using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Vesperhold
{
    // Placeholder art: flat colours until real sprites are imported.
    public struct Palette
    {
        public Color Back, Solid, Platform, Spike;
        static Color C(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }
        static Palette P(string back, string solid, string plat, string spike)
        { return new Palette { Back = C(back), Solid = C(solid), Platform = C(plat), Spike = C(spike) }; }

        static readonly Dictionary<string, Palette> Themes = new Dictionary<string, Palette>
        {
            { "gate", P("#171335", "#3a3552", "#5a4a82", "#c0c8d8") },
            { "hall", P("#1b1020", "#4a3040", "#7a4a5a", "#d0c0a0") },
            { "library", P("#1a1410", "#4a3622", "#7a5a34", "#d0c0a0") },
            { "chapel", P("#101a24", "#2c4058", "#4a6a8a", "#c0d0e0") },
            { "catacomb", P("#100e12", "#3a3438", "#5a5058", "#d8d0c0") },
            { "under", P("#08141a", "#1f3a42", "#386a72", "#b0d8d8") },
            { "armory", P("#16161c", "#444854", "#6a6e7e", "#d0d4e0") },
            { "tower", P("#1a1226", "#3c2e58", "#6a4e98", "#e0d0f0") },
            { "throne", P("#1c0a12", "#4a1a2a", "#8a3048", "#f0c0c8") },
        };

        public static Palette For(string theme)
        {
            return Themes.TryGetValue(theme, out var p) ? p : Themes["catacomb"];
        }
    }

    public static class SpriteFactory
    {
        static Sprite square;
        public static Sprite Square
        {
            get
            {
                if (square == null)
                {
                    var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
                    tex.SetPixel(0, 0, Color.white); tex.Apply();
                    square = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
                }
                return square;
            }
        }

        public static Tile MakeTile(Color c)
        {
            var t = ScriptableObject.CreateInstance<Tile>();
            t.sprite = Square;
            t.color = c;
            return t;
        }

        public static SpriteRenderer Box(Transform parent, string name, Color c, Vector2 size, Vector2 localPos, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = new Vector3(size.x, size.y, 1);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Square; sr.color = c; sr.sortingOrder = order;
            return sr;
        }
    }

    public class TorchLight : MonoBehaviour
    {
        SpriteRenderer sr; float seed;
        void Start()
        {
            seed = Random.value * 10f;
            sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteFactory.Square;
            sr.sortingOrder = -50;
            transform.localScale = new Vector3(0.35f, 0.6f, 1);
        }
        void Update()
        {
            float f = 0.75f + 0.25f * Mathf.PerlinNoise(Time.time * 6f, seed);
            sr.color = new Color(1f, 0.55f * f + 0.2f, 0.15f, 0.9f);
        }
    }

    public class SpikeZone : MonoBehaviour
    {
        void OnTriggerStay2D(Collider2D other)
        {
            var p = other.GetComponent<PlayerController>();
            if (p != null) p.Hurt(6, transform.position.x, 12f);
        }
    }

    public class DoorTrigger : MonoBehaviour
    {
        public string DoorId, TargetDoorId;
        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<PlayerController>() != null) GameManager.Instance.GoThroughDoor(TargetDoorId);
        }
    }
}

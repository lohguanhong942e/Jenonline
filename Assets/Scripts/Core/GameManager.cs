using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Vesperhold
{
    // Creates the whole game at runtime so an empty scene is enough to play.
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        public RoomBuilder CurrentRoom { get; private set; }
        public bool Paused { get; private set; }
        public string ToastText = ""; float toastT;
        Camera cam; Transform world; PlayerController player;
        float shake, hitStop; bool changing; bool showMenu = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Instance != null) return;
            var go = new GameObject("GameManager");
            go.AddComponent<GameManager>();
        }

        void Awake()
        {
            Instance = this;
            Application.targetFrameRate = 60;
            Physics2D.gravity = new Vector2(0, -Tuning.Gravity);
            GameData.Load();
            GameState.NewRun();
            cam = Camera.main;
            if (cam == null) { var cg = new GameObject("Main Camera") { tag = "MainCamera" }; cam = cg.AddComponent<Camera>(); }
            cam.orthographic = true; cam.orthographicSize = 7f; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black;
            cam.transform.position = new Vector3(0, 0, -10);
            world = new GameObject("World").transform;
            Paused = true;
        }

        void StartRun(bool load)
        {
            showMenu = false; Paused = false;
            if (load) GameState.Load(); else GameState.NewRun();
            player = PlayerController.Create(Vector2.zero);
            LoadRoom(GameState.SaveRoom, null, new Vector2(GameState.SaveX, GameState.SaveY), load);
        }

        public void GoThroughDoor(string targetDoor) { if (!changing) GoToRoom(null, targetDoor); }
        public void GoToRoom(string roomId, string doorId) { if (!changing) StartCoroutine(Change(roomId, doorId)); }

        IEnumerator Change(string roomId, string doorId)
        {
            changing = true; Paused = true;
            yield return null;
            LoadRoom(roomId, doorId, null, false);
            yield return null;
            Paused = false; changing = false;
        }

        void LoadRoom(string roomId, string doorId, Vector2? spawn, bool restore)
        {
            if (CurrentRoom != null) Destroy(CurrentRoom.gameObject);
            RoomData data = null;
            if (roomId != null) data = GameData.Room(roomId);
            else
            {
                foreach (var r in GameData.Rooms.rooms) foreach (var d in r.doors) if (d.id == doorId) data = r;
            }
            CurrentRoom = RoomBuilder.Build(data, world);
            GameState.Seen.Add(data.id);
            Vector2 at = spawn ?? (doorId != null ? CurrentRoom.SpawnForDoor(doorId) : CurrentRoom.DefaultSpawn());
            player.Teleport(at);
            cam.transform.position = new Vector3(at.x, at.y + 3f, -10);
            Toast(data.name);
        }

        void LateUpdate()
        {
            if (player == null || CurrentRoom == null) return;
            var d = CurrentRoom.Data;
            float halfH = cam.orthographicSize, halfW = halfH * cam.aspect;
            var t = player.transform.position + Vector3.up * 1.5f;
            float x = d.w <= halfW * 2 ? d.w / 2f : Mathf.Clamp(t.x, halfW, d.w - halfW);
            float y = d.h <= halfH * 2 ? -d.h / 2f : Mathf.Clamp(t.y, -d.h + halfH, -halfH);
            var target = new Vector3(x, y, -10);
            cam.transform.position = Vector3.Lerp(cam.transform.position, target, 1 - Mathf.Exp(-8f * Time.unscaledDeltaTime));
            if (shake > 0) { cam.transform.position += (Vector3)Random.insideUnitCircle * shake * 0.5f; shake -= Time.unscaledDeltaTime; }
        }

        void Update()
        {
            if (toastT > 0) toastT -= Time.unscaledDeltaTime;
            if (hitStop > 0) { hitStop -= Time.unscaledDeltaTime; Time.timeScale = hitStop > 0 ? 0.05f : 1f; }
            var kb = Keyboard.current;
            if (kb != null && !showMenu && kb.escapeKey.wasPressedThisFrame) Paused = !Paused;
        }

        public void Toast(string s) { ToastText = s; toastT = 2.5f; }
        public void Shake(float s) { shake = s; }
        public void HitStop(float t) { hitStop = t; }

        public void OnPlayerDied() { StartCoroutine(Respawn()); }
        IEnumerator Respawn()
        {
            Toast("You have fallen. Waking at the last brazier.");
            yield return new WaitForSecondsRealtime(1.8f);
            GameState.Gold -= GameState.Gold / 10;
            Paused = true; yield return null;
            LoadRoom(GameState.SaveRoom, null, new Vector2(GameState.SaveX, GameState.SaveY), true);
            player.Revive(new Vector2(GameState.SaveX, GameState.SaveY));
            Paused = false;
        }

        void OnGUI()
        {
            var big = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
            if (showMenu)
            {
                GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none);
                var title = new GUIStyle(big) { fontSize = 54, alignment = TextAnchor.MiddleCenter };
                GUI.Label(new Rect(0, Screen.height * 0.2f, Screen.width, 90), "VESPERHOLD", title);
                var btn = new GUIStyle(GUI.skin.button) { fontSize = 22 };
                if (GUI.Button(new Rect(Screen.width / 2 - 130, Screen.height * 0.5f, 260, 50), "Begin the Quest", btn)) StartRun(false);
                if (GameState.HasSave() && GUI.Button(new Rect(Screen.width / 2 - 130, Screen.height * 0.5f + 62, 260, 50), "Continue", btn)) StartRun(true);
                return;
            }
            if (player == null) return;
            GUI.Label(new Rect(16, 8, 400, 30), "LV " + GameState.Level + "   " + GameState.ClassId, big);
            Bar(new Rect(16, 40, 220, 18), player.Hp / (float)GameState.MaxHp, new Color(0.8f, 0.2f, 0.2f), player.Hp + " / " + GameState.MaxHp);
            Bar(new Rect(16, 62, 220, 12), player.Mp / (float)GameState.MaxMp, new Color(0.25f, 0.5f, 0.9f), "");
            Bar(new Rect(16, 78, 220, 6), GameState.Xp / (float)GameData.XpToLevel(GameState.Level), new Color(0.9f, 0.75f, 0.3f), "");
            GUI.Label(new Rect(Screen.width - 330, 8, 320, 30), "Gold " + GameState.Gold + "   Potions " + GameState.Potions + "   Hearts " + GameState.Hearts, new GUIStyle(big) { alignment = TextAnchor.UpperRight });
            if (toastT > 0) GUI.Label(new Rect(0, 60, Screen.width, 40), ToastText, new GUIStyle(big) { alignment = TextAnchor.MiddleCenter });
            if (Paused && !changing) GUI.Label(new Rect(0, Screen.height / 2 - 30, Screen.width, 60), "Paused (Esc)", new GUIStyle(big) { fontSize = 40, alignment = TextAnchor.MiddleCenter });
        }

        static void Bar(Rect r, float f, Color c, string label)
        {
            GUI.color = Color.black; GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = c; GUI.DrawTexture(new Rect(r.x + 1, r.y + 1, (r.width - 2) * Mathf.Clamp01(f), r.height - 2), Texture2D.whiteTexture);
            GUI.color = Color.white;
            if (label != "") GUI.Label(r, label, new GUIStyle(GUI.skin.label) { fontSize = 12, alignment = TextAnchor.MiddleCenter });
        }
    }
}

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
        float shake, hitStop; bool changing; bool showMenu = true, showWin, pickClass;
        public bool InMenu { get { return showMenu; } }

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

        void StartRun(bool load, string classId = null)
        {
            showMenu = false; pickClass = false; showWin = false; Paused = false; Time.timeScale = 1f;
            if (load) GameState.Load(); else { GameState.NewRun(); GameState.SetClass(classId ?? "warrior"); }
            player = PlayerController.Create(Vector2.zero);
            SkillCaster.Attach(player, GameState.ClassId);
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

        // Lord Vesper is dead: let the victory moment breathe, then show the ending.
        public void Win() { StartCoroutine(WinRoutine()); }
        IEnumerator WinRoutine()
        {
            yield return new WaitForSecondsRealtime(3.2f);
            showWin = true; Paused = true;
        }

        void ReturnToTitle()
        {
            if (CurrentRoom != null) Destroy(CurrentRoom.gameObject);
            if (player != null) Destroy(player.gameObject);
            player = null; CurrentRoom = null; showWin = false; showMenu = true; pickClass = false; Paused = true;
            GameState.NewRun(); Time.timeScale = 1f;
        }

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
                if (pickClass)
                {
                    GUI.Label(new Rect(0, Screen.height * 0.36f, Screen.width, 40), "Choose your class", new GUIStyle(big) { alignment = TextAnchor.MiddleCenter });
                    float bw = 220, bx = Screen.width / 2f - (3 * bw + 40) / 2f;
                    string[] ids = { "warrior", "mage", "rogue" };
                    for (int i = 0; i < 3; i++)
                    {
                        var cd = GameState_Class(ids[i]);
                        var r = new Rect(bx + i * (bw + 20), Screen.height * 0.45f, bw, 120);
                        if (GUI.Button(r, cd == null ? ids[i] : cd.name + "\n" + cd.desc, new GUIStyle(btn) { fontSize = 16, wordWrap = true })) StartRun(false, ids[i]);
                    }
                    if (GUI.Button(new Rect(Screen.width / 2 - 60, Screen.height * 0.45f + 140, 120, 36), "Back")) pickClass = false;
                    return;
                }
                if (GUI.Button(new Rect(Screen.width / 2 - 130, Screen.height * 0.5f, 260, 50), "Begin the Quest", btn)) pickClass = true;
                if (GameState.HasSave() && GUI.Button(new Rect(Screen.width / 2 - 130, Screen.height * 0.5f + 62, 260, 50), "Continue", btn)) StartRun(true);
                return;
            }
            if (showWin)
            {
                GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none);
                GUI.Label(new Rect(0, Screen.height * 0.3f, Screen.width, 80), "The Night Lifts", new GUIStyle(big) { fontSize = 54, alignment = TextAnchor.MiddleCenter });
                GUI.Label(new Rect(0, Screen.height * 0.3f + 90, Screen.width, 40), "Lord Vesper is ash, and pale dawn touches the old towers. The valley is free.", new GUIStyle(big) { fontSize = 20, alignment = TextAnchor.MiddleCenter });
                if (GUI.Button(new Rect(Screen.width / 2 - 130, Screen.height * 0.6f, 260, 50), "Return to Title", new GUIStyle(GUI.skin.button) { fontSize = 22 })) ReturnToTitle();
                return;
            }
            if (player == null) return;
            GUI.Label(new Rect(16, 8, 400, 30), "LV " + GameState.Level + "   " + GameState.ClassId, big);
            Bar(new Rect(16, 40, 220, 18), player.Hp / (float)GameState.MaxHp, new Color(0.8f, 0.2f, 0.2f), player.Hp + " / " + GameState.MaxHp);
            Bar(new Rect(16, 62, 220, 12), player.Mp / (float)GameState.MaxMp, new Color(0.25f, 0.5f, 0.9f), "");
            Bar(new Rect(16, 78, 220, 6), GameState.Xp / (float)GameData.XpToLevel(GameState.Level), new Color(0.9f, 0.75f, 0.3f), "");
            GUI.Label(new Rect(Screen.width - 330, 8, 320, 30), "Gold " + GameState.Gold + "   Potions " + GameState.Potions + "   Hearts " + GameState.Hearts, new GUIStyle(big) { alignment = TextAnchor.UpperRight });
            var boss = BossBrain.Current;
            if (boss != null && boss.E != null)
            {
                float bwid = Mathf.Min(Screen.width * 0.6f, 420f);
                GUI.Label(new Rect(Screen.width / 2f - bwid / 2f, 44, bwid, 24), boss.Title, new GUIStyle(big) { fontSize = 18, alignment = TextAnchor.MiddleCenter });
                Bar(new Rect(Screen.width / 2f - bwid / 2f, 70, bwid, 12), Mathf.Max(0, boss.E.Hp) / (float)boss.E.MaxHp, new Color(0.85f, 0.25f, 0.2f), "");
            }
            if (toastT > 0) GUI.Label(new Rect(0, 60, Screen.width, 40), ToastText, new GUIStyle(big) { alignment = TextAnchor.MiddleCenter });
            if (Paused && !changing) GUI.Label(new Rect(0, Screen.height / 2 - 30, Screen.width, 60), "Paused (Esc)", new GUIStyle(big) { fontSize = 40, alignment = TextAnchor.MiddleCenter });
        }

        static ClassDef GameState_Class(string id)
        {
            foreach (var c in GameData.Classes.classes) if (c.id == id) return c;
            return null;
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

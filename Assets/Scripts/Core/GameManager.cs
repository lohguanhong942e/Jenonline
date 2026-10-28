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
        float bannerT;
        public bool InMenu { get { return showMenu; } }
        public bool InRun { get { return !showMenu && !showWin && player != null; } }
        public void SetPaused(bool p) { Paused = p; }

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
            QualitySettings.vSyncCount = 0;
            Screen.autorotateToPortrait = false; Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true; Screen.autorotateToLandscapeRight = true;
            Screen.orientation = ScreenOrientation.AutoRotation;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Physics2D.gravity = new Vector2(0, -Tuning.Gravity);
            GameData.Load();
            GameState.NewRun();
            cam = Camera.main;
            if (cam == null) { var cg = new GameObject("Main Camera") { tag = "MainCamera" }; cam = cg.AddComponent<Camera>(); }
            cam.orthographic = true; cam.orthographicSize = 7f; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black;
            cam.transform.position = new Vector3(0, 0, -10);
            PostFx.Setup(cam);
            gameObject.AddComponent<MenuUI>(); gameObject.AddComponent<TouchControls>();
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
            changing = true; Paused = true; Sfx.Play("door", 0.6f);
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
            if (bannerT > 0) bannerT -= Time.unscaledDeltaTime;
            if (hitStop > 0) { hitStop -= Time.unscaledDeltaTime; Time.timeScale = hitStop > 0 ? 0.05f : 1f; }
            Sfx.MusicTick(InRun, BossBrain.Current != null);
        }

        // Back to the title screen from the pause menu (progress was saved when the menu closed).
        public void QuitToTitle() { ReturnToTitle(); }

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
            Toast("You have fallen. Waking at the last brazier."); bannerT = 2.4f;
            yield return new WaitForSecondsRealtime(1.8f);
            GameState.Gold -= GameState.Gold / 10;
            Paused = true; yield return null;
            LoadRoom(GameState.SaveRoom, null, new Vector2(GameState.SaveX, GameState.SaveY), true);
            player.Revive(new Vector2(GameState.SaveX, GameState.SaveY));
            Paused = false;
        }

        void OnGUI() { UiScale.Begin(); Draw(); UiScale.End(); }

        void Draw()
        {
            float W = UiScale.W, H = UiScale.H;
            var big = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
            if (UiScale.Mobile && UiScale.Portrait)
            {
                GUI.color = Color.black; GUI.DrawTexture(new Rect(0, 0, W, H), Texture2D.whiteTexture); GUI.color = Color.white;
                GUI.Label(new Rect(20, H * 0.4f, W - 40, 100), "Rotate your phone to landscape", new GUIStyle(big) { fontSize = 26, wordWrap = true, alignment = TextAnchor.MiddleCenter });
                return;
            }
            var btn = UiArt.Button(22);
            if (showMenu)
            {
                var bgArt = UiArt.Tex("title_art");
                if (bgArt != null) { GUI.DrawTexture(new Rect(0, 0, W, H), bgArt, ScaleMode.ScaleAndCrop); GUI.color = new Color(0, 0, 0, 0.35f); GUI.DrawTexture(new Rect(0, 0, W, H), Texture2D.whiteTexture); GUI.color = Color.white; }
                else GUI.Box(new Rect(0, 0, W, H), GUIContent.none);
                float lw = Mathf.Min(W * 0.75f, 680f);
                if (!UiArt.Draw(new Rect(W / 2 - lw / 2, H * 0.05f, lw, lw * 0.5f), UiArt.Tex("logo")))
                    GUI.Label(new Rect(0, H * 0.2f, W, 90), "VESPERHOLD", new GUIStyle(big) { fontSize = 54, alignment = TextAnchor.MiddleCenter });
                if (pickClass)
                {
                    GUI.Label(new Rect(0, H * 0.4f, W, 40), "Choose your class", new GUIStyle(big) { alignment = TextAnchor.MiddleCenter });
                    float cw = Mathf.Min(220f, (W - 60f) / 3f), bx = W / 2f - (3 * cw + 24) / 2f; string[] ids = { "warrior", "mage", "rogue" };
                    for (int i = 0; i < 3; i++)
                    {
                        var cd = GameState_Class(ids[i]);
                        var card = new Rect(bx + i * (cw + 12), H * 0.47f, cw, Mathf.Min(300f, H * 0.42f));
                        GUI.color = new Color(0, 0, 0, 0.55f); GUI.DrawTexture(card, Texture2D.whiteTexture); GUI.color = Color.white;
                        UiArt.Draw(new Rect(card.x + cw / 2 - 48, card.y + 8, 96, 96), UiArt.Tex("crest_" + ids[i]));
                        GUI.Label(new Rect(card.x + 8, card.y + 106, cw - 16, 28), cd == null ? ids[i] : cd.name, new GUIStyle(big) { fontSize = 20, alignment = TextAnchor.MiddleCenter });
                        GUI.Label(new Rect(card.x + 10, card.y + 134, cw - 20, 60), cd == null ? "" : cd.desc, new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true, alignment = TextAnchor.UpperCenter });
                        if (GUI.Button(new Rect(card.x + 12, card.yMax - 58, cw - 24, 46), "Choose", UiArt.Button(18))) { Sfx.Play("click", 0.6f); StartRun(false, ids[i]); }
                    }
                    if (GUI.Button(new Rect(W / 2 - 70, H * 0.47f + Mathf.Min(300f, H * 0.42f) + 10, 140, 40), "Back", UiArt.Button(16))) pickClass = false;
                    return;
                }
                if (GUI.Button(new Rect(W / 2 - 140, H * 0.58f, 280, 56), "Begin the Quest", btn)) { Sfx.Play("click", 0.6f); pickClass = true; }
                if (GameState.HasSave() && GUI.Button(new Rect(W / 2 - 140, H * 0.58f + 66, 280, 56), "Continue", btn)) { Sfx.Play("click", 0.6f); StartRun(true); }
                return;
            }
            if (showWin)
            {
                GUI.color = new Color(0, 0, 0, 0.8f); GUI.DrawTexture(new Rect(0, 0, W, H), Texture2D.whiteTexture); GUI.color = Color.white;
                float bw = Mathf.Min(W * 0.8f, 720f);
                if (!UiArt.Draw(new Rect(W / 2 - bw / 2, H * 0.1f, bw, bw * 0.49f), UiArt.Tex("banner_victory")))
                    GUI.Label(new Rect(0, H * 0.3f, W, 80), "The Night Lifts", new GUIStyle(big) { fontSize = 54, alignment = TextAnchor.MiddleCenter });
                GUI.Label(new Rect(20, H * 0.1f + bw * 0.52f, W - 40, 60), "Lord Vesper is ash, and pale dawn touches the old towers. The valley is free.", new GUIStyle(big) { fontSize = 20, wordWrap = true, alignment = TextAnchor.UpperCenter });
                if (GUI.Button(new Rect(W / 2 - 140, H * 0.1f + bw * 0.52f + 70, 280, 56), "Return to Title", btn)) ReturnToTitle();
                return;
            }
            if (player == null) return;
            // HUD: class crest, level, health, mana, XP.
            float hl = UiScale.L, ht = UiScale.T;
            UiArt.Draw(new Rect(hl + 8, ht + 6, 44, 44), UiArt.Tex("crest_" + GameState.ClassId));
            GUI.Label(new Rect(hl + 58, ht + 8, 300, 30), "LV " + GameState.Level + "   " + GameState.ClassId, big);
            DrawIcon(new Rect(hl + 8, ht + 56, 26, 26), "icon_heart");
            UiArt.Bar(new Rect(hl + 46, ht + 60, 220, 16), player.Hp / (float)GameState.MaxHp, new Color(0.8f, 0.2f, 0.2f), player.Hp + " / " + GameState.MaxHp);
            DrawIcon(new Rect(hl + 8, ht + 88, 26, 26), "icon_mana");
            UiArt.Bar(new Rect(hl + 46, ht + 94, 220, 12), player.Mp / (float)GameState.MaxMp, new Color(0.25f, 0.5f, 0.9f), "");
            Bar(new Rect(hl + 46, ht + 118, 220, 5), GameState.Xp / (float)GameData.XpToLevel(GameState.Level), new Color(0.9f, 0.75f, 0.3f), "");
            var right = new GUIStyle(big) { fontSize = 18, alignment = TextAnchor.MiddleLeft };
            float rx = W - UiScale.R - 14f, ry = ht + 8;
            rx -= 70; DrawIcon(new Rect(rx, ry, 22, 22), "icon_potion"); GUI.Label(new Rect(rx + 26, ry, 44, 22), GameState.Potions.ToString(), right);
            rx -= 90; DrawIcon(new Rect(rx, ry, 22, 22), "icon_heart"); GUI.Label(new Rect(rx + 26, ry, 64, 22), GameState.Hearts.ToString(), right);
            rx -= 120; DrawIcon(new Rect(rx, ry, 22, 22), "icon_gold"); GUI.Label(new Rect(rx + 26, ry, 94, 22), GameState.Gold.ToString(), right);
            if (UiScale.Mobile && !(MenuUI.Instance != null && MenuUI.Instance.Open) && UiArt.IconButton(new Rect(W - UiScale.R - 64, ry + 34, 52, 52), "glyph_menu", "Menu")) { Sfx.Play("click", 0.5f); if (MenuUI.Instance != null) MenuUI.Instance.OpenPause(); }
            var boss = BossBrain.Current;
            if (boss != null && boss.E != null)
            {
                float bwid = Mathf.Min(W * 0.6f, 460f), fh = bwid * 0.145f, fy = 40f;
                GUI.Label(new Rect(W / 2f - bwid / 2f, 14, bwid, 24), boss.Title, new GUIStyle(big) { fontSize = 18, alignment = TextAnchor.MiddleCenter });
                float f = Mathf.Clamp01(Mathf.Max(0, boss.E.Hp) / (float)boss.E.MaxHp);
                var frame = UiArt.Tex("bossbar_frame");
                var fr = new Rect(W / 2f - bwid / 2f, fy, bwid, fh);
                var track = new Rect(fr.x + fr.width * 0.1f, fr.y + fr.height * 0.37f, fr.width * 0.8f, fr.height * 0.26f);
                if (frame != null) GUI.DrawTexture(fr, frame, ScaleMode.StretchToFill);       // frame first, then the fill inside it
                else { GUI.color = new Color(0.1f, 0.02f, 0.02f); GUI.DrawTexture(track, Texture2D.whiteTexture); }
                GUI.color = new Color(0.85f, 0.2f, 0.18f); GUI.DrawTexture(new Rect(track.x, track.y, track.width * f, track.height), Texture2D.whiteTexture); GUI.color = Color.white;
            }
            if (toastT > 0)
            {
                var tst = new GUIStyle(big) { alignment = TextAnchor.MiddleCenter };
                float tw = Mathf.Min(W - 40, tst.CalcSize(new GUIContent(ToastText)).x + 80);
                UiArt.Stretch(new Rect(W / 2 - tw / 2, 104, tw, 46), UiArt.Tex("toast_plate"));
                GUI.Label(new Rect(W / 2 - tw / 2, 104, tw, 46), ToastText, tst);
            }
            if (bannerT > 0)
            {
                float bw = Mathf.Min(W * 0.7f, 640f);
                var old = GUI.color; GUI.color = new Color(1, 1, 1, Mathf.Clamp01(bannerT / 0.6f));
                if (!UiArt.Draw(new Rect(W / 2 - bw / 2, H * 0.3f, bw, bw * 0.42f), UiArt.Tex("banner_gameover"))) GUI.Label(new Rect(0, H * 0.4f, W, 60), "YOU HAVE FALLEN", new GUIStyle(big) { fontSize = 40, alignment = TextAnchor.MiddleCenter });
                GUI.color = old;
            }
        }

        // Phones pause the app when you switch away: save and open the pause menu so nothing is lost.
        void OnApplicationPause(bool paused)
        {
            if (paused && InRun) { GameState.Save(); if (MenuUI.Instance != null) MenuUI.Instance.OpenPause(); }
        }

        void DrawIcon(Rect r, string name) { var t = UiArt.Tex(name); if (t != null) UiArt.Draw(r, t); }

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

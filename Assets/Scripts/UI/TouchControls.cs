using UnityEngine;
using UnityEngine.InputSystem;

namespace Vesperhold
{
    // On-screen buttons for phones and tablets (WebGL in a mobile browser). They appear automatically when a
    // touchscreen is detected and can be forced on from the Options tab. Read through TouchInput.
    public static class TouchInput
    {
        public static bool Left, Right, JumpHeld, JumpDown, JumpUp, AttackDown, DashDown, PotionDown, InteractDown, Force;
        public static bool Active { get { return Force || Touchscreen.current != null; } }
    }

    [DefaultExecutionOrder(-200)]
    public class TouchControls : MonoBehaviour
    {
        struct Btn { public string Id, Label; public Rect R; }
        Btn[] btns = new Btn[0];
        bool prevJump; bool[] held = new bool[0];

        void Layout()
        {
            float u = Mathf.Clamp(Mathf.Min(Screen.width, Screen.height) * 0.17f, 56f, 110f), m = u * 0.25f, y = Screen.height - u - m - 70f;
            btns = new[]
            {
                new Btn { Id = "L", Label = "<", R = new Rect(m, y, u, u) },
                new Btn { Id = "R", Label = ">", R = new Rect(m * 2 + u, y, u, u) },
                new Btn { Id = "J", Label = "JUMP", R = new Rect(Screen.width - u * 1.05f - m, y - u * 0.15f, u * 1.05f, u * 1.05f) },
                new Btn { Id = "A", Label = "ATK", R = new Rect(Screen.width - u * 2.15f - m * 2, y + u * 0.1f, u, u) },
                new Btn { Id = "D", Label = "DASH", R = new Rect(Screen.width - u * 1.9f - m, y - u * 1.3f, u * 0.8f, u * 0.8f) },
                new Btn { Id = "P", Label = "POT", R = new Rect(Screen.width - u * 2.9f - m * 2, y - u * 0.5f, u * 0.7f, u * 0.7f) },
                new Btn { Id = "E", Label = "USE", R = new Rect(Screen.width - u * 3.7f - m * 3, y + u * 0.1f, u * 0.7f, u * 0.7f) },
            };
            if (held.Length != btns.Length) held = new bool[btns.Length];
        }

        void Update()
        {
            TouchInput.JumpDown = TouchInput.JumpUp = TouchInput.AttackDown = TouchInput.DashDown = TouchInput.PotionDown = TouchInput.InteractDown = false;
            if (!TouchInput.Active) { TouchInput.Left = TouchInput.Right = TouchInput.JumpHeld = false; return; }
            Layout();
            var now = new bool[btns.Length];
            var ts = Touchscreen.current;
            if (ts != null)
                foreach (var t in ts.touches)
                {
                    if (!t.isInProgress) continue;
                    Vector2 p = t.position.ReadValue(); var gp = new Vector2(p.x, Screen.height - p.y);
                    for (int i = 0; i < btns.Length; i++)
                    {
                        if (!btns[i].R.Contains(gp)) continue;
                        now[i] = true;
                        if (!held[i])
                            switch (btns[i].Id) { case "A": TouchInput.AttackDown = true; break; case "D": TouchInput.DashDown = true; break; case "P": TouchInput.PotionDown = true; break; case "E": TouchInput.InteractDown = true; break; }
                    }
                }
            // Mouse also works, so the buttons can be tested in the editor with Force on.
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.isPressed)
            {
                Vector2 p = mouse.position.ReadValue(); var gp = new Vector2(p.x, Screen.height - p.y);
                for (int i = 0; i < btns.Length; i++) if (btns[i].R.Contains(gp)) { now[i] = true; if (!held[i]) switch (btns[i].Id) { case "A": TouchInput.AttackDown = true; break; case "D": TouchInput.DashDown = true; break; case "P": TouchInput.PotionDown = true; break; case "E": TouchInput.InteractDown = true; break; } }
            }
            TouchInput.Left = now[0]; TouchInput.Right = now[1]; TouchInput.JumpHeld = now[2];
            TouchInput.JumpDown = now[2] && !prevJump; TouchInput.JumpUp = !now[2] && prevJump; prevJump = now[2];
            held = now;
        }

        void OnGUI()
        {
            if (!TouchInput.Active || GameManager.Instance == null || GameManager.Instance.InMenu || GameManager.Instance.Paused) return;
            Layout();
            var st = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            for (int i = 0; i < btns.Length; i++)
            {
                GUI.color = held[i] ? new Color(1f, 1f, 1f, 0.55f) : new Color(1f, 1f, 1f, 0.22f);
                GUI.DrawTexture(btns[i].R, Texture2D.whiteTexture);
                GUI.color = new Color(1f, 1f, 1f, 0.8f); GUI.Label(btns[i].R, btns[i].Label, st);
            }
            GUI.color = Color.white;
        }
    }
}

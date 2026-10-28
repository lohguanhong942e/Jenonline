using System;
using System.Collections.Generic;
using UnityEngine;

namespace Vesperhold
{
    // No audio files needed: every sound effect and both music loops are synthesised on first use.
    // Swap any of them for real clips later by calling Sfx.Override(name, clip).
    public static class Sfx
    {
        const int SR = 22050;
        static AudioSource src, musicSrc;
        static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        static AudioClip explore, bossLoop; static bool bossPlaying;
        public static float Volume = 0.7f, MusicVolume = 0.28f; public static bool Muted;

        public static void Override(string name, AudioClip clip) { clips[name] = clip; }

        static void Ensure()
        {
            if (src != null) return;
            var go = new GameObject("Audio");
            UnityEngine.Object.DontDestroyOnLoad(go);
            src = go.AddComponent<AudioSource>(); src.playOnAwake = false;
            musicSrc = go.AddComponent<AudioSource>(); musicSrc.playOnAwake = false; musicSrc.loop = true;
        }

        static readonly Dictionary<string, float> lastPlayed = new Dictionary<string, float>();

        public static void Play(string name, float vol = 1f, float pitch = 1f)
        {
            if (Muted) return;
            float last; if (lastPlayed.TryGetValue(name, out last) && Time.unscaledTime - last < 0.04f) return;   // AoE skills hit many enemies in one frame
            lastPlayed[name] = Time.unscaledTime;
            Ensure();
            AudioClip c;
            if (!clips.TryGetValue(name, out c)) { c = Build(name); clips[name] = c; }
            if (c == null) return;
            src.pitch = pitch;
            src.PlayOneShot(c, vol * Volume);
        }

        // The loops are rendered a slice per frame so a phone never freezes while they are built.
        class Runner : MonoBehaviour { }
        static Runner runner; static bool building;

        // Call every frame: plays the exploration loop, or the boss loop while a boss is alive.
        public static void MusicTick(bool inGame, bool boss)
        {
            Ensure();
            musicSrc.volume = Muted ? 0f : MusicVolume;
            if (!inGame) boss = false;
            if (explore == null) { if (!building && inGame) Build(false); return; }
            if (bossLoop == null && !building) Build(true);          // build the boss loop quietly in the background
            if (boss && bossLoop == null) boss = false;
            if (!musicSrc.isPlaying || boss != bossPlaying)
            {
                bossPlaying = boss; musicSrc.clip = boss ? bossLoop : explore; musicSrc.Play();
            }
        }

        static void Build(bool boss)
        {
            building = true;
            if (runner == null) runner = src.gameObject.AddComponent<Runner>();
            runner.StartCoroutine(RenderLoop(boss));
        }

        // ---- synthesis helpers ----
        static float Noise() { return UnityEngine.Random.value * 2f - 1f; }
        static float Sine(float f, float t) { return Mathf.Sin(2f * Mathf.PI * f * t); }
        static float Saw(float f, float t) { float p = f * t; return 2f * (p - Mathf.Floor(p + 0.5f)); }
        static float Sq(float f, float t) { return Sine(f, t) >= 0 ? 1f : -1f; }
        static float Tri(float f, float t) { return 2f * Mathf.Abs(Saw(f, t)) - 1f; }
        static float Hz(int midi) { return 440f * Mathf.Pow(2f, (midi - 69) / 12f); }

        static AudioClip Make(string name, float dur, Func<float, float> f)
        {
            int n = Mathf.CeilToInt(dur * SR); var d = new float[n];
            for (int i = 0; i < n; i++) { float t = i / (float)SR; float fade = Mathf.Clamp01((dur - t) / 0.02f); d[i] = Mathf.Clamp(f(t) * fade, -1f, 1f); }
            var clip = AudioClip.Create(name, n, 1, SR, false); clip.SetData(d, 0);
            return clip;
        }

        static AudioClip Build(string name)
        {
            float lp = 0f;
            switch (name)
            {
                case "hit": return Make(name, 0.2f, t => (Noise() * Mathf.Exp(-t * 32f) * 0.8f + Sine(130f - t * 200f, t) * Mathf.Exp(-t * 16f) * 0.8f));
                case "swing": return Make(name, 0.22f, t => { lp += (Noise() - lp) * Mathf.Lerp(0.05f, 0.5f, t / 0.22f); return lp * Mathf.Sin(Mathf.PI * t / 0.22f) * 0.9f; });
                case "jump": return Make(name, 0.14f, t => Sine(300f + t * 2400f, t) * Mathf.Exp(-t * 14f) * 0.5f);
                case "land": return Make(name, 0.1f, t => Sine(90f, t) * Mathf.Exp(-t * 35f) * 0.8f);
                case "hurt": return Make(name, 0.32f, t => Saw(230f - t * 420f, t) * Mathf.Exp(-t * 7f) * 0.5f + Noise() * Mathf.Exp(-t * 30f) * 0.3f);
                case "pickup": return Make(name, 0.2f, t => (t < 0.07f ? Sine(880f, t) : Sine(1320f, t)) * Mathf.Exp(-(t % 0.07f) * 12f) * 0.4f);
                case "level": return Make(name, 0.6f, t => { int k = Mathf.Min(3, (int)(t / 0.13f)); float[] fr = { 523f, 659f, 784f, 1046f }; return Sine(fr[k], t) * Mathf.Exp(-(t - k * 0.13f) * 5f) * 0.45f; });
                case "shot": return Make(name, 0.12f, t => Sine(500f + t * 3000f, t) * Mathf.Exp(-t * 20f) * 0.45f);
                case "arrow": return Make(name, 0.14f, t => { lp += (Noise() - lp) * 0.35f; return lp * Mathf.Exp(-t * 24f) * 0.9f + Sine(180f, t) * Mathf.Exp(-t * 30f) * 0.4f; });
                case "explode": return Make(name, 0.55f, t => { lp += (Noise() - lp) * 0.12f; return lp * Mathf.Exp(-t * 7f) * 1.4f + Sine(60f - t * 40f, t) * Mathf.Exp(-t * 8f) * 0.8f; });
                case "cast_fire": return Make(name, 0.45f, t => { lp += (Noise() - lp) * Mathf.Lerp(0.04f, 0.4f, t / 0.45f); return lp * Mathf.Sin(Mathf.PI * Mathf.Min(1f, t / 0.45f)) * 1.2f + Sine(200f - t * 120f, t) * Mathf.Exp(-t * 6f) * 0.35f; });
                case "cast_ice": return Make(name, 0.5f, t => (Sine(1800f + 900f * Sine(7f, t), t) * 0.25f + Sine(2700f, t) * 0.15f) * Mathf.Exp(-t * 5f) + Noise() * Mathf.Exp(-t * 14f) * 0.2f);
                case "cast_bolt": return Make(name, 0.35f, t => (Sq(90f + Noise() * 40f, t) * 0.25f + Noise() * 0.3f) * Mathf.Exp(-t * 9f));
                case "cast_holy": return Make(name, 0.7f, t => (Sine(660f, t) * 0.3f + Sine(990f, t) * 0.2f + Sine(1320f, t) * 0.15f) * Mathf.Exp(-t * 4f));
                case "cast_shadow": return Make(name, 0.55f, t => { lp += (Noise() - lp) * 0.07f; return (lp * 1.2f + Saw(70f + t * 30f, t) * 0.25f) * Mathf.Exp(-t * 4f); });
                case "boss": return Make(name, 0.9f, t => (Saw(70f - t * 20f, t) * 0.5f + Sine(35f, t) * 0.5f) * (0.6f + 0.4f * Sine(14f, t)) * Mathf.Exp(-t * 2f));
                case "door": return Make(name, 0.18f, t => Sq(220f - t * 300f, t) * Mathf.Exp(-t * 14f) * 0.25f);
                case "click": return Make(name, 0.05f, t => Sq(700f, t) * Mathf.Exp(-t * 60f) * 0.25f);
                case "coin": return Make(name, 0.12f, t => Sine(t < 0.04f ? 1500f : 2000f, t) * Mathf.Exp(-t * 18f) * 0.3f);
            }
            return null;
        }

        // ---- music: a small synthesised loop (pad + bass + arpeggio, with drums for the boss) ----
        static System.Collections.IEnumerator RenderLoop(bool boss)
        {
            float bpm = boss ? 132f : 84f, beat = 60f / bpm; int bars = 8;
            float total = bars * 4 * beat; int n = Mathf.CeilToInt(total * SR); var d = new float[n];
            int[][] prog = boss
                ? new[] { new[] { 40, 47, 52, 55 }, new[] { 36, 43, 48, 52 }, new[] { 38, 45, 50, 54 }, new[] { 40, 47, 52, 55 } }
                : new[] { new[] { 45, 52, 57, 60 }, new[] { 41, 48, 53, 57 }, new[] { 36, 43, 48, 52 }, new[] { 43, 50, 55, 59 } };
            float lp = 0f;
            var fr = new float[4][];
            for (int c = 0; c < 4; c++) { fr[c] = new float[5]; for (int k = 0; k < 4; k++) fr[c][k] = Hz(prog[c][k]); fr[c][4] = Hz(prog[c][0] - 12); }
            const int Slice = 24000;
            for (int i0 = 0; i0 < n; i0 += Slice)
            {
                int i1 = Mathf.Min(n, i0 + Slice);
                for (int i = i0; i < i1; i++)
                {
                    float t = i / (float)SR; int bar = (int)(t / (4 * beat)); var f = fr[bar % 4];
                    float inBar = t - bar * 4 * beat; int step = (int)(inBar / (beat * 0.5f)); float st = inBar - step * beat * 0.5f;
                    float v = 0f;
                    for (int k = 1; k < 4; k++) v += (Sine(f[k], t) + 0.5f * Sine(f[k] * 1.004f, t)) * 0.05f;                         // pad
                    float bt = inBar % beat; v += (boss ? Saw(f[4], t) * 0.16f : Sine(f[4], t) * 0.3f) * Mathf.Exp(-bt * (boss ? 6f : 3f));   // bass
                    v += Tri(f[1 + step % 3] * 2f, t) * Mathf.Exp(-st * 9f) * (boss ? 0.1f : 0.07f);                                  // arpeggio
                    if (boss)
                    {
                        v += Sine(60f - bt * 60f, t) * Mathf.Exp(-bt * 14f) * 0.5f;                                                      // kick
                        if ((int)(inBar / beat) % 2 == 1) { lp += (Noise() - lp) * 0.3f; v += lp * Mathf.Exp(-bt * 18f) * 0.35f; }       // snare
                    }
                    d[i] = Mathf.Clamp(v, -1f, 1f);
                }
                yield return null;
            }
            var clip = AudioClip.Create(boss ? "boss_loop" : "explore_loop", n, 1, SR, false); clip.SetData(d, 0);
            if (boss) bossLoop = clip; else explore = clip;
            building = false;
        }
    }
}

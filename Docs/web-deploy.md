# Putting Vesperhold on the web for phones

## 1. Build (on your computer, in Unity 6)
1. Open the project. The first time, the console says it created a URP 2D renderer: that is expected.
2. Menu **Vesperhold > Build WebGL**. It needs the WebGL Build Support module (Unity Hub > Installs > Add modules).
3. The result is the folder `Builds/WebGL`. After the build the game patches `index.html` for phones: full-screen canvas, no page scrolling or pinch zoom, tap-to-fullscreen with landscape lock (Android), and it adds the home-screen install files (`manifest.webmanifest`, icons, `sw.js`).

## 2. Host it (any static host, HTTPS required)
Upload everything inside `Builds/WebGL` so `index.html` sits at the root of the site.
- **GitHub Pages** (free, you already have the repo): put the folder contents on a branch called `gh-pages` (or in `/docs` on main), then Settings > Pages. GitHub does not send the Brotli header, so the build uses Unity's built-in decompression fallback. That works, it just downloads a bit slower.
- **Netlify, Cloudflare Pages, Vercel**: drag the folder in. These also work with the fallback.
- Your own server: for the fastest load, serve `*.br` files with `Content-Encoding: br` (and `.wasm.br` with `Content-Type: application/wasm`).

## 3. Play it on your phone
- Open the link in Chrome (Android) or Safari (iPhone).
- **Android:** the first tap goes fullscreen and locks landscape. Menu > Install app (or Add to Home Screen) gives you an icon that opens full-screen.
- **iPhone:** Share > Add to Home Screen. Opened from the icon it runs without the browser bars (Safari cannot lock rotation, so turn the phone sideways yourself).
- Progress is saved in the browser. Clearing site data or using a private tab erases it.

## 4. If something looks wrong
- Black screen on an older phone: it needs WebGL 2 (any phone from about 2018 on).
- iPhone crashes on load: Safari limits memory per tab. Close other tabs; if it still fails, tell me and I will cut texture sizes.
- Buttons too small or too big: change `UiScale.Scale` (UI/UiScale.cs) or the button size `u` in `TouchControls.Layout`.
- Sound only starts after the first tap: that is a browser rule, not a bug.

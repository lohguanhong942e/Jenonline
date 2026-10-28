#if UNITY_EDITOR
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Vesperhold.EditorTools
{
    // After every WebGL build: make the page behave like a phone game (full-screen canvas, no page scrolling or
    // pinch zoom, tap-to-fullscreen and landscape lock on Android) and make it installable ("Add to Home Screen")
    // with an icon, a web manifest and a small offline cache. Works on whatever index.html Unity produced.
    public class WebGLPost : IPostprocessBuildWithReport
    {
        public int callbackOrder { get { return 100; } }

        const string Head = @"
<meta name=""viewport"" content=""width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no, viewport-fit=cover"">
<meta name=""mobile-web-app-capable"" content=""yes"">
<meta name=""apple-mobile-web-app-capable"" content=""yes"">
<meta name=""apple-mobile-web-app-status-bar-style"" content=""black-translucent"">
<meta name=""apple-mobile-web-app-title"" content=""Vesperhold"">
<meta name=""theme-color"" content=""#0b0816"">
<link rel=""manifest"" href=""manifest.webmanifest"">
<link rel=""apple-touch-icon"" href=""icon-180.png"">
<link rel=""icon"" href=""icon-192.png"">
<style>
html,body{margin:0;padding:0;width:100%;height:100%;background:#0b0816;overflow:hidden;overscroll-behavior:none;touch-action:none;-webkit-user-select:none;user-select:none;-webkit-touch-callout:none;-webkit-tap-highlight-color:transparent}
#unity-container{position:fixed!important;left:0!important;top:0!important;right:0!important;bottom:0!important;width:100%!important;height:100%!important;transform:none!important}
#unity-canvas{width:100%!important;height:100%!important;display:block}
#unity-footer,#unity-fullscreen-button,#unity-build-title,#unity-logo{display:none!important}
</style>";

        const string Tail = @"
<script>
if ('serviceWorker' in navigator && location.protocol !== 'file:') {
  window.addEventListener('load', function () { navigator.serviceWorker.register('sw.js').catch(function () {}); });
}
// First tap: go fullscreen and lock landscape where the browser allows it (Android Chrome). iPhone Safari ignores both.
document.addEventListener('touchend', function first() {
  document.removeEventListener('touchend', first);
  try {
    var el = document.documentElement, req = el.requestFullscreen || el.webkitRequestFullscreen;
    if (req && !document.fullscreenElement) {
      var p = req.call(el);
      var lock = function () { if (screen.orientation && screen.orientation.lock) screen.orientation.lock('landscape').catch(function () {}); };
      if (p && p.then) p.then(lock).catch(function () {}); else lock();
    }
  } catch (e) {}
}, { once: true });
document.addEventListener('contextmenu', function (e) { e.preventDefault(); });
</script>";

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.WebGL) return;
            string dir = report.summary.outputPath, index = Path.Combine(dir, "index.html");
            if (!File.Exists(index)) { Debug.LogWarning("Vesperhold: no index.html found to patch in " + dir); return; }
            string html = File.ReadAllText(index);
            html = Regex.Replace(html, @"<meta\s+name=[""']viewport[""'][^>]*>", "", RegexOptions.IgnoreCase);
            if (!html.Contains("manifest.webmanifest"))
            {
                html = Regex.Replace(html, @"</head>", Head + "\n</head>", RegexOptions.IgnoreCase);
                html = Regex.Replace(html, @"</body>", Tail + "\n</body>", RegexOptions.IgnoreCase);
                File.WriteAllText(index, html);
            }
            string src = Path.GetFullPath(Path.Combine(Application.dataPath, "../Tools/web"));
            if (Directory.Exists(src))
                foreach (var f in Directory.GetFiles(src)) File.Copy(f, Path.Combine(dir, Path.GetFileName(f)), true);
            Debug.Log("Vesperhold: patched index.html for phones and copied the install files (manifest, icons, service worker).");
        }
    }
}
#endif

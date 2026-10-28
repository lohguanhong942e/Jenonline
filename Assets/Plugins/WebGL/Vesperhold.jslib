mergeInto(LibraryManager.library, {
  WebVibrate: function (ms) {
    try { if (navigator.vibrate) navigator.vibrate(ms); } catch (e) {}
  },
  WebIsMobileBrowser: function () {
    var ua = navigator.userAgent || '';
    var ipadOs = navigator.maxTouchPoints > 1 && /Macintosh/.test(ua);
    return (/Android|iPhone|iPad|iPod|Mobile/i.test(ua) || ipadOs) ? 1 : 0;
  },
  WebToggleFullscreen: function () {
    try {
      var el = document.documentElement;
      if (!document.fullscreenElement && !document.webkitFullscreenElement) {
        var req = el.requestFullscreen || el.webkitRequestFullscreen;
        if (req) { var p = req.call(el); if (p && p.catch) p.catch(function () {}); }
      } else {
        var ex = document.exitFullscreen || document.webkitExitFullscreen;
        if (ex) ex.call(document);
      }
    } catch (e) {}
  }
});

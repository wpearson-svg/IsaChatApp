// A dashboard opened from the /present slides (Alt+6, Alt+7, or the Backstage menu) opens in a new tab with
// ?from=present. Esc on that page closes the tab, which puts you back on the slide you were on.
// Does nothing on any other page.
(function () {
    if (location.search.indexOf('from=present') < 0) return;
    window.addEventListener('keydown', function (e) {
        if (e.key !== 'Escape') return;
        window.close();
        // If the browser will not close it (the tab was not opened by the slides), go back instead.
        setTimeout(function () { if (!window.closed) history.back(); }, 250);
    }, true);
})();

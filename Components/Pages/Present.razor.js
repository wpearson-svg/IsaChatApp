// ============================================================
// Present.razor.js -- keys for the /present slides.
//
// Next slide: Right arrow, Down arrow, Page Down, Space (a presentation
// clicker sends Page Down / Page Up). Back: Left arrow, Up arrow,
// Page Up. Home / End: first / last slide. F: full screen on and off.
//
// Keys are ignored while someone is typing (a text box, Isa's chat
// box) or while focus is inside Isa's chat widget, so the demo never
// moves the slides by accident.
//
// The slide number is kept in the address (#3), so a refresh stays
// on the same slide.
//
// The intro animation (wwwroot/present/intro.html) plays over the page
// when the slides open on the first slide, and again with I. Its curtain
// opens onto the slide underneath.
// ============================================================

let handler = null;
let frameLoad = null;

export function init(dotnet, questions) {
    handler = (e) => {
        // Demo questions: Alt+1 to Alt+5 (or Ctrl+1 to Ctrl+5 where the browser lets the page have them)
        // type that test question into Isa's question box. Chrome and Edge keep Ctrl+1..8 for switching
        // tabs, so Alt is the one that works everywhere.
        // Alt+C: the live Salesforce Cases panel on and off. Esc closes it.
        if (e.altKey && !e.ctrlKey && !e.metaKey && e.code === 'KeyC') { e.preventDefault(); dotnet.invokeMethodAsync('ToggleCases'); return; }
        // Alt+6: the Developer Dashboard, Alt+7: the Content Health dashboard, over the slides (demo step 6, Backstage).
        // Esc, or the Back to slides button, returns to the slide you were on.
        if (e.altKey && !e.ctrlKey && !e.metaKey && (e.code === 'Digit6' || e.code === 'Digit7')) {
            e.preventDefault();
            dotnet.invokeMethodAsync('ToggleFrame', e.code === 'Digit6' ? '/dev-dashboard' : '/content-health-dashboard');
            return;
        }
        if (e.key === 'Escape') {
            dotnet.invokeMethodAsync('CloseCases');
            dotnet.invokeMethodAsync('CloseFrame');
            document.querySelector('.isa-widget__dashboardClose')?.click();   // a dashboard opened from the Backstage menu
        }
        const digit = /^Digit([1-9])$/.exec(e.code || '');
        if ((e.altKey || e.ctrlKey) && !e.metaKey && !e.shiftKey && digit && questions && questions[+digit[1] - 1]) {
            const box = document.querySelector('.isa-widget .isa-widget__input');
            if (box && !box.disabled) {
                e.preventDefault();
                box.value = questions[+digit[1] - 1];
                box.dispatchEvent(new Event('input', { bubbles: true }));   // so Isa sees the text
                box.focus();
            }
            return;
        }
        if (e.altKey || e.ctrlKey || e.metaKey) return;
        const t = e.target;
        if (t && t.closest && (t.closest('input, textarea, select, [contenteditable="true"]') || t.closest('.isa-widget'))) return;

        let action = null;
        switch (e.key) {
            case 'ArrowRight': case 'ArrowDown': case 'PageDown': case ' ': action = ['Go', 1]; break;
            case 'ArrowLeft': case 'ArrowUp': case 'PageUp': action = ['Go', -1]; break;
            case 'Home': action = ['GoTo', 0]; break;
            case 'End': action = ['GoTo', 9999]; break;
            case 'f': case 'F': toggleFullScreen(); e.preventDefault(); return;
            case 'i': case 'I': playIntro(); e.preventDefault(); return;
        }
        if (!action) return;
        e.preventDefault();
        dotnet.invokeMethodAsync(action[0], action[1]);
    };
    document.addEventListener('keydown', handler);

    // A dashboard in a frame has its own keyboard focus, so pass Esc and the Alt shortcuts on from inside it.
    frameLoad = (ev) => {
        const f = ev.target;
        if (!f || f.tagName !== 'IFRAME' || !(f.closest('.present__frame') || f.classList.contains('isa-widget__dashboardFrame'))) return;
        try {
            f.contentWindow.document.addEventListener('keydown', (e) => {
                if (e.key === 'Escape' || (e.altKey && ['Digit6', 'Digit7', 'KeyC'].includes(e.code))) handler(e);
            });
        } catch (err) { /* not our page: leave it alone */ }
    };
    document.addEventListener('load', frameLoad, true);
    console.info('[Isa present] keys ready: Alt+1..5 questions, Alt+C cases, Esc closes');

    const n = parseInt((location.hash || '').replace('#', ''), 10);
    return Number.isFinite(n) && n > 0 ? n - 1 : 0;
}

export function setSlide(index) {
    // Full path, because <base href="/"> would turn a bare "#3" into "/#3".
    history.replaceState(history.state, '', location.pathname + '#' + (index + 1));
}

export function dispose() {
    if (handler) document.removeEventListener('keydown', handler);
    if (frameLoad) document.removeEventListener('load', frameLoad, true);
    handler = null;
    frameLoad = null;
}

let intro = null;

export function playIntro() {
    if (intro) return;
    // Dark Blue until the intro has drawn its own curtain, so the slide never flashes first.
    intro = document.createElement('div');
    intro.style.cssText = 'position:fixed;inset:0;z-index:2147483600;background:#1d2d5c;';
    const frame = document.createElement('iframe');
    frame.src = 'present/intro.html?embed=1';
    frame.title = 'Isa intro';
    frame.style.cssText = 'position:absolute;inset:0;width:100%;height:100%;border:0;background:transparent;';
    frame.addEventListener('load', () => { if (intro) intro.style.background = 'transparent'; });
    intro.appendChild(frame);
    document.body.appendChild(intro);

    const finish = () => {
        window.removeEventListener('message', onMessage);
        clearTimeout(fallback);
        intro?.remove();
        intro = null;
        document.body.focus();
    };
    const onMessage = (e) => { if (e.source === frame.contentWindow && e.data?.type === 'isa-intro-done') finish(); };
    window.addEventListener('message', onMessage);
    const fallback = setTimeout(finish, 12000); // never leave the slides covered
}

function toggleFullScreen() {
    if (document.fullscreenElement) document.exitFullscreen();
    else document.documentElement.requestFullscreen?.();
}

// Readiness check result, for whoever is setting up (open the browser console, F12).
export function reportReady(r) {
    const ok = r.ollamaReachable && r.modelInstalled && r.signedInAsDev && (r.salesforce === 'ok' || r.salesforce === 'off');
    console.info('[Isa ready]', ok ? 'READY' : 'CHECK', r);
}

// The closing: Cs roll in pulling the curtain closed over the slide, the Isa logo spins once, and
// it stays. stopOutro (Back) takes it away again.
let outroEl = null;

export function playOutro() {
    if (outroEl) return;
    outroEl = document.createElement('div');
    outroEl.style.cssText = 'position:fixed;inset:0;z-index:2147483600;background:transparent;pointer-events:none;';
    const frame = document.createElement('iframe');
    frame.src = 'present/intro.html?embed=1&mode=outro';
    frame.title = 'Isa closing';
    frame.style.cssText = 'position:absolute;inset:0;width:100%;height:100%;border:0;background:transparent;';
    outroEl.appendChild(frame);
    document.body.appendChild(outroEl);
}

export function stopOutro() {
    outroEl?.remove();
    outroEl = null;
    document.body.focus();
}

// Back from a dashboard: give the keys back to the slides.
export function refocus() {
    window.focus();
    document.body.focus();
}

// Keeps the Cases panel to the left of Isa's panel and rail, however wide they are right now.
export function fitCases() {
    const panel = document.querySelector('.present__cases');
    if (!panel) return;
    let left = innerWidth;
    document.querySelectorAll('.isa-widget *').forEach((el) => {
        if (el.closest('.isa-widget__dashboardHost')) return;
        const r = el.getBoundingClientRect();
        if (r.width > 20 && r.height >= innerHeight * 0.6 && getComputedStyle(el).position === 'fixed') left = Math.min(left, r.left);
    });
    const start = panel.getBoundingClientRect().left;
    panel.style.width = Math.max(320, Math.min(innerWidth * 0.66, left - start - 24)) + 'px';
}
addEventListener('resize', () => fitCases());

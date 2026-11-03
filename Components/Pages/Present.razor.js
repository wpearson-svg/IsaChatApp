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

export function init(dotnet, questions) {
    handler = (e) => {
        if (e.altKey && !e.ctrlKey && !e.metaKey && e.code !== 'AltLeft' && e.code !== 'AltRight') console.info('[Isa present] key', 'Alt+' + e.code);
        // Demo questions: Alt+1 to Alt+5 (or Ctrl+1 to Ctrl+5 where the browser lets the page have them)
        // type that test question into Isa's question box. Chrome and Edge keep Ctrl+1..8 for switching
        // tabs, so Alt is the one that works everywhere.
        // Alt+C: the live Salesforce Cases panel on and off. Esc closes it.
        if (e.altKey && !e.ctrlKey && !e.metaKey && e.code === 'KeyC') { e.preventDefault(); dotnet.invokeMethodAsync('ToggleCases'); return; }
        // Alt+6: the Developer panel (trace lookup), Alt+7: the Content Health panel. Esc closes the panel and you
        // are on the same slide. (Also reachable from Isa's Backstage menu.)
        if (e.altKey && !e.ctrlKey && !e.metaKey && (e.code === 'Digit6' || e.code === 'Digit7')) {
            e.preventDefault();
            dotnet.invokeMethodAsync('TogglePanel', e.code === 'Digit6' ? '/dev-dashboard' : '/content-health-dashboard');
            return;
        }
        if (e.key === 'Escape') {
            console.info('[Isa present] Esc: closing panels');
            dotnet.invokeMethodAsync('CloseCases');
        }
        const digit = /^Digit([1-9])$/.exec(e.code || '') || (/^[1-9]$/.test(e.key || '') ? [null, e.key] : null);
        if ((e.altKey || e.ctrlKey) && !e.metaKey && !e.shiftKey && digit && questions && questions[+digit[1] - 1]) {
            e.preventDefault();
            // What the page holds right now, written to the console (F12), never shown on screen, so a shortcut that
            // opens a second panel can be explained: how many Isa widgets, panels, bubbles and question boxes exist.
            const count = (s) => document.querySelectorAll(s).length;
            const diag = 'widgets=' + count('.isa-widget') + ' panels=' + count('.isa-widget__panel') + ' bubbles=' + count('.isa-widget__bubble') + ' boxes=' + count('.isa-widget__input') + ' focus=' + (document.activeElement?.className || document.activeElement?.tagName);
            console.info('[Isa present] Alt+' + digit[1] + ' ' + diag);
            // There is only ever one Isa on the page. If a second widget has appeared, hide the extras and keep the one
            // with the open panel (or the last one), so a shortcut never opens a second panel beside a bubble.
            const widgets = [...document.querySelectorAll('.isa-widget')];
            if (widgets.length > 1) {
                const keep = widgets.find(w => w.querySelector('.isa-widget__panel')) || widgets[widgets.length - 1];
                widgets.forEach(w => { if (w !== keep) w.style.display = 'none'; });
                console.warn('[Isa present] ' + widgets.length + ' Isa widgets found; extras hidden');
            }
            const q = questions[+digit[1] - 1];
            const fill = () => {
                // The visible question box (if there were ever two Isa widgets on the page, never pick a hidden one).
                const box = [...document.querySelectorAll('.isa-widget .isa-widget__input')].find(b => b.offsetParent !== null && !b.closest('.isa-widget').style.display);
                if (!box || box.disabled) return false;
                toast('Alt+' + digit[1] + ': question ' + digit[1] + ' typed. Press Enter.');
                box.value = q;
                box.dispatchEvent(new Event('input', { bubbles: true }));   // so Isa sees the text
                box.focus();
                return true;
            };
            if (!fill()) {
                // Isa's panel is closed: open it, then type the question once the box is there.
                // If the panel is already open, only wait for its box; clicking the bubble then would open a second one.
                if (!document.querySelector('.isa-widget__panel')) [...document.querySelectorAll('.isa-widget')].filter(w => w.style.display !== 'none').pop()?.querySelector('.isa-widget__bubble')?.click();
                let tries = 0;
                const wait = setInterval(() => { if (fill()) clearInterval(wait); else if (++tries > 20) { clearInterval(wait); toast('Alt+' + digit[1] + ': Isa is busy or closed, try again.'); } }, 100);
            }
            return;
        }
        if (e.altKey || e.ctrlKey || e.metaKey) return;
        // A clicker's Page Down / Page Up always change slides, even when the chat box still has focus
        // after a demo question (a one-line box does not use them). Arrows and Space stay with the box.
        const clickerKey = e.key === 'PageDown' || e.key === 'PageUp';
        const t = e.target;
        if (!clickerKey && t && t.closest && (t.closest('input, textarea, select, [contenteditable="true"]') || t.closest('.isa-widget'))) return;

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
    document.addEventListener('keydown', handler, true);
    console.info('[Isa present] keys ready: Alt+1..5 questions, Alt+C cases, Alt+6/7 panels');

    const n = parseInt((location.hash || '').replace('#', ''), 10);
    return Number.isFinite(n) && n > 0 ? n - 1 : 0;
}

// A small note in the bottom-left corner, so you can see a shortcut reached the page.
let toastEl = null, toastTimer = 0;
function toast(text) {
    if (!toastEl) {
        toastEl = document.createElement('div');
        toastEl.style.cssText = 'position:fixed;left:16px;bottom:16px;z-index:2147483601;padding:8px 14px;border-radius:8px;background:#1d2d5c;color:#fff;font:500 14px Roboto,Arial,sans-serif;opacity:0;transition:opacity .2s;pointer-events:none;';
        document.body.appendChild(toastEl);
    }
    toastEl.textContent = text;
    toastEl.style.opacity = '0.92';
    clearTimeout(toastTimer);
    toastTimer = setTimeout(() => { if (toastEl) toastEl.style.opacity = '0'; }, 1600);
}

export function setSlide(index) {
    // Full path, because <base href="/"> would turn a bare "#3" into "/#3".
    history.replaceState(history.state, '', location.pathname + '#' + (index + 1));
}

export function dispose() {
    if (handler) document.removeEventListener('keydown', handler, true);
    handler = null;
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

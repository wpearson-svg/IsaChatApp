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

export function init(dotnet) {
    handler = (e) => {
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

    const n = parseInt((location.hash || '').replace('#', ''), 10);
    return Number.isFinite(n) && n > 0 ? n - 1 : 0;
}

export function setSlide(index) {
    // Full path, because <base href="/"> would turn a bare "#3" into "/#3".
    history.replaceState(history.state, '', location.pathname + '#' + (index + 1));
}

export function dispose() {
    if (handler) document.removeEventListener('keydown', handler);
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

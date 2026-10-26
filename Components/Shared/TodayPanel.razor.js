// TodayPanel.razor.js
//
// When the leader clicks "Write message" on the "Meet your Chapter Experience
// contact" step, the introduction form opens further down the Today window.
// Matthew noticed (22 October 2026) that the leader then had to scroll to
// find it. This scrolls the Today window just far enough that the whole form
// is in view, or at once for people who ask for reduced motion. If the form
// is taller than the window, its top lines up with the window's top.
//
// The glide (Matthew, 23 October 2026: "smooth, gentle, elegant, easy"): the
// browser's own smooth scrolling is quick and can't be slowed, and looked
// jerky. This glide takes 0.9 seconds and starts and ends softly, like the
// panels opening. If the leader scrolls themselves meanwhile, it stops and
// leaves the scrolling to them.
//
// Only the Today window's own scrolling area moves, never the page behind
// Isa (the browser's scrollIntoView would scroll that too).

const GLIDE_MS = 900;

export function revealElement(el) {
    if (!el) return;
    let box = el.parentElement;
    while (box && !/(auto|scroll)/.test(getComputedStyle(box).overflowY)) box = box.parentElement;
    if (!box) return;

    const gap = 12; // a little space around the form
    const view = box.getBoundingClientRect();
    const item = el.getBoundingClientRect();
    let move = 0;
    if (item.height + 2 * gap > view.height || item.top < view.top + gap) {
        move = item.top - view.top - gap;              // line its top up with the window's top
    } else if (item.bottom > view.bottom - gap) {
        move = item.bottom - view.bottom + gap;        // just far enough to show its bottom
    }
    if (move === 0) return;

    if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
        box.scrollTop += move;
        return;
    }
    glide(box, move);
}

// Moves the box's scroll position by "move" pixels over GLIDE_MS,
// slow at the start, quicker in the middle, slow again at the end.
function glide(box, move) {
    const from = box.scrollTop;
    const to = Math.max(0, Math.min(from + move, box.scrollHeight - box.clientHeight));
    if (to === from) return;

    let stopped = false;
    const stop = () => { stopped = true; };
    // The leader's own scrolling wins.
    box.addEventListener('wheel', stop, { once: true, passive: true });
    box.addEventListener('touchstart', stop, { once: true, passive: true });

    let done = false;
    const finish = () => {
        done = true;
        box.removeEventListener('wheel', stop);
        box.removeEventListener('touchstart', stop);
    };

    const start = performance.now();
    const step = (now) => {
        if (stopped || done) return;
        const t = Math.min(1, (now - start) / GLIDE_MS);
        box.scrollTop = from + (to - from) * easeInOut(t);
        if (t < 1) requestAnimationFrame(step);
        else finish();
    };
    requestAnimationFrame(step);

    // Safety net: a browser pauses animations in a window that isn't on
    // screen. The form still ends up in view, without the glide.
    setTimeout(() => {
        if (stopped || done) return;
        box.scrollTop = to;
        finish();
    }, GLIDE_MS + 300);
}

// A soft ease in and out (smootherstep), gentler at both ends than a plain curve.
function easeInOut(t) {
    return t * t * t * (t * (6 * t - 15) + 10);
}

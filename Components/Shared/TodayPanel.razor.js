// TodayPanel.razor.js
//
// When the leader clicks "Write message" on the "Meet your Chapter Experience
// contact" step, the introduction form opens further down the Today window.
// Matthew noticed (22 October 2026) that the leader then had to scroll to
// find it. This scrolls the Today window just far enough that the whole form
// is in view: gently, or at once for people who ask for reduced motion. If
// the form is taller than the window, its top lines up with the window's top.
//
// Only the Today window's own scrolling area moves, never the page behind
// Isa (the browser's scrollIntoView would scroll that too).
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

    const calm = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    box.scrollBy({ top: move, behavior: calm ? 'auto' : 'smooth' });
}

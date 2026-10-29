// SampleChapter360.razor.js -- scrolling for the sample Chapter 360 page.
// A new view opens at the top; a link to one Practice Area ("Open the
// practice") scrolls to it instead.

export function scrollToTop() {
    window.scrollTo(0, 0);
}

export function scrollToElement(el) {
    if (!el) return;
    const reduce = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    el.scrollIntoView({ block: 'start', behavior: reduce ? 'auto' : 'smooth' });
}

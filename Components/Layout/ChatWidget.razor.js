// ChatWidget.razor.js
//
// When Isa's answer arrives, scroll the chat so the leader's question sits
// at the top of the message area, with the start of the answer right under
// it. Matthew noticed (20 October 2026) that a long answer (the "show the
// work" line, the answer, the source cards, the save button, the reference
// number) otherwise lands at its END, so the reader had to scroll back up to
// find where it starts.
//
// Short answers that already fit don't move: the browser can't scroll past
// the bottom of the conversation, so nothing changes for them.
//
// Measures with getBoundingClientRect so it works with the message area's
// column-reverse layout (see .isa-widget__messages in ChatWidget.razor.css).
// When the chat opens before the leader has asked anything, show it from the
// top: the Today note, then Isa's welcome. Matthew noticed (22 October 2026)
// that the column-reverse layout opened it pinned to the bottom instead, so
// the Today note was cut off above the top edge.
//
// The Today note loads a moment after the chat opens and pushes the content
// taller, so for a short while this keeps scrolling back to the top whenever
// the content changes size. It stops after two seconds, or as soon as the
// leader scrolls, taps or types, so it never fights them.
export function showFromTop(container) {
    if (!container) return;
    // In a column-reverse box the top is the most negative scroll position;
    // a large negative value is clamped to it by the browser.
    const toTop = () => container.scrollTo({ top: -container.scrollHeight, behavior: 'auto' });
    toTop();
    const watcher = new ResizeObserver(toTop);
    for (const child of container.children) watcher.observe(child);
    const stop = () => {
        watcher.disconnect();
        for (const e of ['wheel', 'touchstart', 'keydown', 'pointerdown']) container.removeEventListener(e, stop);
    };
    for (const e of ['wheel', 'touchstart', 'keydown', 'pointerdown']) container.addEventListener(e, stop, { passive: true });
    setTimeout(stop, 2000);
}

export function scrollToMessage(container, id) {
    if (!container) return;
    const el = container.querySelector('#' + CSS.escape(id));
    if (!el) return;
    const gap = 8; // a little space above the question
    const offset = el.getBoundingClientRect().top - container.getBoundingClientRect().top - gap;
    const calm = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    container.scrollTo({ top: container.scrollTop + offset, behavior: calm ? 'auto' : 'smooth' });
}

// Escape closes one thing at a time (accessibility check, lane 11, M-021): the panel on
// the right, then Today, then the chat. The choice of what to close is made in
// ChatWidget.razor (OnEscapeKey); this only listens for the key. Escape is left alone:
//   - while a term's definition is showing (GlossaryText.razor.js hides it),
//   - while a pop-up box or a menu is open (they have their own Cancel or close),
//   - while the leader is typing in a box that has text in it, or a box in a side panel
//     has text in it, so nothing typed is lost.
// 'current' is the chat on the page now; a reload replaces it, so the key always
// reaches the live one.
let current = null;
let escapeWatched = false;
export function watchEscape(widget) {
    current = widget;
    if (escapeWatched) return;
    escapeWatched = true;
    document.addEventListener('keydown', (e) => {
        if (e.key !== 'Escape' || e.defaultPrevented || !current) return;
        if (!document.querySelector('.isa-widget__panel')) return;            // the chat is closed
        if (document.querySelector('.isa-tip:not([hidden])')) return;
        if (document.querySelector('.isa-widget [role="dialog"], .isa-widget [role="menu"]')) return;
        const at = document.activeElement;
        if (at && (at.tagName === 'TEXTAREA' || at.tagName === 'INPUT') && at.value) return;
        // A note or message half-written in a side panel: closing the panel would lose it.
        const typed = [...document.querySelectorAll('.isa-widget aside textarea, .isa-widget aside input')]
            .some((box) => !['checkbox', 'radio', 'range', 'hidden'].includes(box.type) && box.value);
        if (typed) return;
        current.invokeMethodAsync('OnEscapeKey').catch(() => { });
    });
}

// Puts keyboard focus on the first element that matches, if it is on the page.
export function focusOn(selector) {
    const el = document.querySelector(selector);
    if (el) el.focus({ preventScroll: true });
}

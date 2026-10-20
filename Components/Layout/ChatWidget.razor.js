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
export function scrollToMessage(container, id) {
    if (!container) return;
    const el = container.querySelector('#' + CSS.escape(id));
    if (!el) return;
    const gap = 8; // a little space above the question
    const offset = el.getBoundingClientRect().top - container.getBoundingClientRect().top - gap;
    const calm = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    container.scrollTo({ top: container.scrollTop + offset, behavior: calm ? 'auto' : 'smooth' });
}

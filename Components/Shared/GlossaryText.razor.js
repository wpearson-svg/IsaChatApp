// GlossaryText.razor.js (November 9, 2026): the box that shows a glossary term's definition.
// One box for the whole page, set up once. It opens when the pointer is over a marked term,
// or the term has focus (Tab, or a tap on a phone), and closes when the pointer leaves, focus
// moves away, or Escape is pressed. It copies the definition kept hidden in the term, so it
// shows exactly ISACA's words, and is placed below the term (above it near the bottom of the
// screen) and kept inside the window.

let started = false;
let box = null;
let current = null;

function ensureBox() {
    if (box) return box;
    box = document.createElement("div");
    box.className = "isa-tip";
    box.setAttribute("aria-hidden", "true"); // screen readers get the definition from the term itself
    box.hidden = true;
    document.body.appendChild(box);
    return box;
}

function show(term) {
    const def = term.querySelector(".isa-term__def");
    if (!def) return;
    const b = ensureBox();
    if (current !== term) b.innerHTML = def.innerHTML;
    current = term;
    b.hidden = false;
    const gap = 6, edge = 8;
    const r = term.getBoundingClientRect();
    const w = b.offsetWidth, h = b.offsetHeight;
    const left = Math.max(edge, Math.min(r.left, window.innerWidth - w - edge));
    let top = r.bottom + gap;
    if (top + h > window.innerHeight - edge && r.top - h - gap >= edge) top = r.top - h - gap;
    b.style.left = left + "px";
    b.style.top = top + "px";
}

function hide(term) {
    if (!box || (term && current !== term)) return;
    box.hidden = true;
    current = null;
}

export function init() {
    if (started) return;
    started = true;
    const termOf = (e) => e.target instanceof Element ? e.target.closest(".isa-term") : null;
    document.addEventListener("mouseover", (e) => { const t = termOf(e); if (t) show(t); });
    document.addEventListener("mouseout", (e) => {
        const t = termOf(e);
        if (t && !t.contains(e.relatedTarget) && document.activeElement !== t) hide(t);
    });
    document.addEventListener("focusin", (e) => { const t = termOf(e); if (t) show(t); });
    document.addEventListener("focusout", (e) => { const t = termOf(e); if (t) hide(t); });
    document.addEventListener("keydown", (e) => { if (e.key === "Escape") hide(null); });
    // Panels scroll and slide; a box left behind would point at nothing.
    document.addEventListener("scroll", () => hide(null), true);
    window.addEventListener("resize", () => hide(null));
}

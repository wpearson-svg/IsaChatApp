// StepList.razor.js -- the motion for StepList.razor (Willis, November 17, 2026).
//
// s = how many items are in the list (0 to N). While s < N, item s is a card on the slide.
// Next: the card shrinks into its row (row s), then item s+1 slides in from the right.
// After the last item is placed, step() returns false so the page changes slide.
// Back: the card (if any) leaves to the right and item s-1 grows back out of its row.
// Positions are measured on the page each time, so they follow the slide's size.

const IN_MS = 650, PLACE_MS = 700, OUT_MS = 450;
const NEXT_DELAY = 250;   // after a card lands in its row, before the next one comes in

let S = null;

export function mount(root, startAtEnd) {
    const body = root.closest(".present__body");
    const st = {
        root,
        rows: [...(body?.querySelectorAll(".present__rows li") ?? [])],
        cards: [...root.querySelectorAll(".steplist__card")],
        reduce: matchMedia("(prefers-reduced-motion: reduce)").matches,
        s: 0, timer: 0
    };
    st.N = Math.min(st.rows.length, st.cards.length);
    S = st;
    if (startAtEnd) {
        st.s = st.N;
        st.rows.forEach(r => r.style.opacity = "1");
    } else {
        st.rows.forEach(r => r.style.opacity = "0");
        // wait for the slide's own entrance, then bring the first item in
        st.timer = setTimeout(() => { if (S === st) flyIn(0); }, st.reduce ? 0 : 300);
    }
}

export function step(delta) {
    const st = S;
    if (!st) return false;
    clearTimeout(st.timer);
    if (delta > 0) {
        if (st.s >= st.N) return false;
        const i = st.s;
        st.s++;
        place(i);
        if (st.s < st.N) {
            const next = st.s;
            st.timer = setTimeout(() => { if (S === st && st.s === next) flyIn(next); }, st.reduce ? 0 : PLACE_MS + NEXT_DELAY);
        }
        return true;
    }
    if (st.s <= 0) return false;
    if (st.s < st.N) flyOut(st.s);
    st.s--;
    unplace(st.s);
    return true;
}

export function unmount() {
    if (!S) return;
    clearTimeout(S.timer);
    S = null;
}

// ---------- where things go ----------

function rootRect() { return S.root.getBoundingClientRect(); }

// The card's resting place: on the right of the slide, level with the middle of the list.
function centre(card) {
    const r = rootRect(), cw = card.offsetWidth, ch = card.offsetHeight;
    const first = S.rows[0].getBoundingClientRect(), last = S.rows[S.N - 1].getBoundingClientRect();
    const midY = (first.top + last.bottom) / 2 - r.top;
    const x = r.width - cw - r.width * .07;   // inside the slide's right margin
    const y = Math.max(0, Math.min(r.height - ch, midY - ch / 2));
    return { x, y, s: 1 };
}

// Off the right edge of the slide.
function offRight(card) {
    const c = centre(card);
    return { x: rootRect().width + 40, y: c.y, s: 1 };
}

// Shrunk down onto its row: as tall as the row, its left edge at the row's left edge.
function inRow(card, i) {
    const r = rootRect(), row = S.rows[i].getBoundingClientRect();
    const s = Math.min(1, row.height / card.offsetHeight);
    return { x: row.left - r.left, y: row.top - r.top + (row.height - card.offsetHeight * s) / 2, s };
}

const tf = p => `translate(${p.x}px, ${p.y}px) scale(${p.s})`;

// Starts a card's movement from wherever it is now (so quick presses never make it jump).
function move(card, to, opacityTo, ms, easing) {
    const from = getComputedStyle(card);
    const start = { transform: from.transform === "none" ? tf(to) : from.transform, opacity: from.opacity };
    card.getAnimations().forEach(a => a.cancel());
    card.style.transform = tf(to);
    card.style.opacity = opacityTo;
    if (S.reduce) return;
    card.animate([start, { transform: tf(to), opacity: opacityTo }], { duration: ms, easing });
}

function fadeRow(i, to, ms, delay) {
    const row = S.rows[i];
    const from = getComputedStyle(row).opacity;
    row.getAnimations().forEach(a => a.cancel());
    row.style.opacity = to;
    if (S.reduce) return;
    row.animate([{ opacity: from }, { opacity: to }], { duration: ms, delay, easing: "ease", fill: "backwards" });
}

// ---------- the four moves ----------

function flyIn(i) {
    const card = S.cards[i];
    card.getAnimations().forEach(a => a.cancel());
    card.style.transform = tf(offRight(card));
    card.style.opacity = "0";
    move(card, centre(card), "1", IN_MS, "cubic-bezier(.2,.8,.2,1)");
}

function place(i) {
    const card = S.cards[i];
    move(card, inRow(card, i), "0", PLACE_MS, "cubic-bezier(.6,0,.2,1)");
    fadeRow(i, "1", PLACE_MS * .5, PLACE_MS * .45);
}

function flyOut(i) {
    const card = S.cards[i];
    move(card, offRight(card), "0", OUT_MS, "cubic-bezier(.6,0,.8,.4)");
}

function unplace(i) {
    const card = S.cards[i];
    card.getAnimations().forEach(a => a.cancel());
    card.style.transform = tf(inRow(card, i));
    card.style.opacity = "0";
    fadeRow(i, "0", PLACE_MS * .4, 0);
    move(card, centre(card), "1", PLACE_MS, "cubic-bezier(.4,0,.2,1)");
}

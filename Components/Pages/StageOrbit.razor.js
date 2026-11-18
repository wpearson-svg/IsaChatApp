// StageOrbit.razor.js -- the motion for StageOrbit.razor (Willis, November 17, 2026).
//
// Steps: 0 = the ring, Isa in the middle, stage 1 at the front. 1 to N = that stage open as a
// card at the front. Each step forward: the open card closes back into its place (and turns
// to its status colour), the ring turns the short way round until the next stage is at the
// front, then that stage opens. Stage 2 sits to the right of stage 1, so the ring turns
// right to left. step() returns false past either end, so the page changes slide.
// Everything is placed in a box 1000 wide, scaled to the slide.

const W = 1000;
const OPEN_MS = 800, CLOSE_MS = 500, ROT_MS = 900;
const MIN_W = 330, PAD = 36;   // bigger cards (Willis, November 17)

const ease = t => t < .5 ? 4 * t * t * t : 1 - Math.pow(-2 * t + 2, 3) / 2;
const lerp = (a, b, t) => a + (b - a) * t;
const clamp = v => Math.max(0, Math.min(1, v));
const STATUS = { built: "Built", partial: "Partly built", stub: "Stub" };

let S = null;

export function mount(root, startAtEnd) {
    const els = [...root.querySelectorAll(".orbit__node")];
    const lbls = [...root.querySelectorAll(".orbit__lbl")];
    const st = {
        root,
        box: root.querySelector(".orbit__box"),
        ring: root.querySelector(".orbit__ring"),
        core: root.querySelector(".orbit__core"),
        live: root.querySelector(".orbit__live"),
        leadThen: root.closest(".present__body")?.querySelector(".present__leadThen"),
        reduce: matchMedia("(prefers-reduced-motion: reduce)").matches,
        N: els.length, SLOT: Math.PI * 2 / Math.max(1, els.length),
        step: 0, rot: 0, rotFrom: 0, rotTo: 0, rotT0: -1e9,
        H: 400, CX: 500, CY: 200, RX: 420, RY: 80, DOT: 50,
        nodes: els.map((el, i) => ({
            el, lbl: lbls[i],
            num: el.querySelector(".orbit__num"),
            card: el.querySelector(".orbit__card"),
            name: el.querySelector(".orbit__name")?.textContent ?? "",
            text: el.querySelector(".orbit__text")?.textContent ?? "",
            status: el.dataset.status ?? "",
            f: 0, from: 0, to: 0, t0: -1e9, ms: OPEN_MS, w: 300, h: 220
        }))
    };
    S = st;
    layout();
    sizeCards();
    document.fonts?.ready.then(() => { if (S === st) sizeCards(); });
    st.ro = new ResizeObserver(() => { if (S === st) layout(); });
    st.ro.observe(root);
    setStep(startAtEnd ? st.N : 0, true);
    const tick = now => { if (S !== st) return; frame(now); st.raf = requestAnimationFrame(tick); };
    st.raf = requestAnimationFrame(tick);
}

export function step(delta) {
    if (!S) return false;
    const next = S.step + (delta > 0 ? 1 : -1);
    if (next < 0 || next > S.N) return false;
    setStep(next, false);
    return true;
}

export function unmount() {
    if (!S) return;
    cancelAnimationFrame(S.raf);
    S.ro?.disconnect();
    S = null;
}

// The box is 1000 wide and as tall as the space the slide leaves; scale it to fit.
function layout() {
    const st = S, w = st.root.clientWidth, h = st.root.clientHeight;
    if (!w || !h) return;
    const s = w / W;
    st.H = h / s;
    st.box.style.height = st.H + "px";
    st.box.style.transform = `scale(${s})`;
    st.CX = W / 2;
    st.CY = st.H * .52;
    st.RX = W * .42;
    st.RY = Math.min(90, st.H * .2);
    st.DOT = Math.min(52, st.H * .13);
    const { CX, CY, RX, RY } = st;
    const back = st.root.classList.contains("is-dark") ? "rgba(255,255,255,.22)" : "#cfe3ee";
    const front = st.root.classList.contains("is-dark") ? "rgba(255,255,255,.5)" : "#9cc6dc";
    st.ring.setAttribute("viewBox", `0 0 ${W} ${st.H}`);
    st.ring.innerHTML =
        `<path d="M ${CX - RX} ${CY} A ${RX} ${RY} 0 0 1 ${CX + RX} ${CY}" fill="none" stroke="${back}" stroke-width="2" stroke-dasharray="4 7"/>` +
        `<path d="M ${CX - RX} ${CY} A ${RX} ${RY} 0 0 0 ${CX + RX} ${CY}" fill="none" stroke="${front}" stroke-width="2"/>`;
    const c = st.core.offsetWidth || 92;
    st.core.style.transform = `translate(${CX - c / 2}px, ${CY - c / 2}px)`;
}

// Each card fits its own text: as wide as the stage name on one line (at least MIN_W), as tall as the text.
function sizeCards() {
    S.nodes.forEach(n => {
        const c = n.card, name = c.querySelector(".orbit__name"), p = c.querySelector(".orbit__text");
        c.style.height = "auto";
        c.style.width = "2000px";
        n.w = Math.ceil(Math.max(MIN_W, name.scrollWidth + PAD * 2));
        c.style.width = n.w + "px";
        n.h = Math.ceil(p.offsetTop + p.offsetHeight + PAD);
        c.style.height = n.h + "px";
    });
}

function setStep(s, instant) {
    const st = S;
    st.step = Math.max(0, Math.min(st.N, s));
    const now = performance.now();
    const closing = st.nodes.some(n => n.to === 1);
    const turnAt = now + (closing && !instant ? CLOSE_MS * .8 : 0);
    const front = st.step >= 1 ? st.step - 1 : 0;
    let delta = front * st.SLOT - st.rot;
    delta = Math.atan2(Math.sin(delta), Math.cos(delta));   // the short way round
    st.rotFrom = st.rot; st.rotTo = st.rot + delta;
    st.rotT0 = instant || st.reduce ? -1e9 : turnAt;
    const openAt = turnAt + (Math.abs(delta) > .001 ? ROT_MS * .9 : 0);

    st.nodes.forEach((n, i) => {
        const target = st.step === i + 1 ? 1 : 0;
        if (instant || st.reduce) { n.f = n.from = n.to = target; n.t0 = -1e9; }
        else if (target !== n.to) {
            n.from = n.f; n.to = target;
            n.t0 = target ? openAt : now;
            n.ms = target ? OPEN_MS : CLOSE_MS;
        }
        n.el.classList.toggle("is-open", target === 1);
        n.el.classList.toggle("is-done", i < st.step - 1);
    });
    st.root.classList.toggle("is-focus", st.step >= 1);
    st.leadThen?.classList.toggle("is-shown", st.step === st.N);
    const open = st.nodes[st.step - 1];
    st.live.textContent = open
        ? `Stage ${st.step} of ${st.N}: ${open.name}. ${STATUS[open.status] ?? open.status}. ${open.text}`
        : "";
}

function frame(now) {
    const st = S;
    const open = st.step >= 1;
    st.rot = lerp(st.rotFrom, st.rotTo, ease(clamp((now - st.rotT0) / ROT_MS)));

    st.nodes.forEach((n, i) => {
        n.f = lerp(n.from, n.to, ease(clamp((now - n.t0) / n.ms)));
        const f = n.f;

        // place on the ring; depth runs from -1 (back) to 1 (front)
        const a = st.rot - i * st.SLOT + Math.PI / 2;
        const depth = Math.sin(a);
        const ox = st.CX + st.RX * Math.cos(a), oy = st.CY + st.RY * depth;
        const d = st.DOT * (.62 + .38 * (depth + 1) / 2);
        let op = .5 + .5 * (depth + 1) / 2;
        if (open && f < .01) op *= .45;   // the others step back while a card is open

        const x = lerp(ox, st.CX, f), y = lerp(oy, st.CY, f);
        const w = lerp(d, n.w, f), h = lerp(d, n.h, f);
        const el = n.el.style;
        el.width = w + "px"; el.height = h + "px";
        el.borderRadius = lerp(d / 2, 18, f) + "px";
        el.transform = `translate(${x - w / 2}px, ${y - h / 2}px)`;
        el.zIndex = f > .001 ? 2000 : Math.round(500 + depth * 400);
        el.opacity = lerp(op, 1, f);
        n.num.style.opacity = clamp(1 - f * 3);
        n.num.style.fontSize = (d * .38) + "px";
        n.card.style.opacity = clamp((f - .82) / .18);

        // the name under the dot, only on the front half of the ring
        n.lbl.style.transform = `translate(${ox}px, ${oy + d / 2 + 5}px) translateX(-50%)`;
        n.lbl.style.opacity = clamp((depth + .1) * 1.4) * clamp(1 - f * 4) * (open ? .45 : 1);
        n.lbl.style.zIndex = Math.round(500 + depth * 400);
    });
}

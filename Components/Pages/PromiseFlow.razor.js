// PromiseFlow.razor.js -- the motion for PromiseFlow.razor (November 16, 2026).
//
// Slide 7 (kept = false): a blue dot, the question, goes through the system in four runs, each with a
//   short caption and a progress dot. The dot rests on the EDGE of each part so it never covers the words.
// Slide 16 (kept = true): the diagram holds, then its boxes slide into a status rail. Wording and status
//   come from the slide's items in presentation/slides.json (matched by "name").
// R, or a click on the diagram, replays. "Reduce motion" shows the first caption (slide 7) or the finished
// rail (slide 16) with no movement.

const NS = 'http://www.w3.org/2000/svg';
const NAVY = '#1d2d5c', MED = '#07649d', BLUE = '#00a4e4', GREEN = '#6cb33f';

let root = null, svg = null, kept = false, dark = false, items = [], token = 0, onKey = null, onClick = null, timers = [];
let intro = [], first = true;   // the first showing draws the connector lines in; a replay does not

const $ = (id) => root.querySelector('#' + id);
const wait = (ms, my) => new Promise((res) => { const t = setTimeout(() => res(my === token), ms); timers.push(t); });
const reduced = () => window.matchMedia && matchMedia('(prefers-reduced-motion: reduce)').matches;

export function mount(el, isKept, isDark, slideItems) {
    root = el; kept = !!isKept; dark = !!isDark; items = slideItems || [];
    svg = root.querySelector('svg');
    // box numbers, so the parts come in one after another
    ['leader', 'front', 'access', 'trans', 'input', 'search', 'out', 'ticket', 'team', 'cyl', 'ai', 'rag']
        .forEach((id, i) => { const n = $('n-' + id); if (n) n.style.setProperty('--n', i); });
    if (kept) {
        ['cap', 'pips', 'dot', 'dot2'].forEach((id) => { const e = $(id); if (e) e.remove(); });
        if (dark) $('panel').setAttribute('opacity', 1);          // the diagram sits on a light panel on a Dark Blue slide
        else {
            // On a white slide the status rail and its two branches are Dark Blue, not white (Willis, November 17, 2026:
            // after the pilot slides were cut this slide became white and the white lines vanished).
            ['rail', 'br-trans', 'br-ai'].forEach((id) => { const p = $(id); if (p) p.setAttribute('stroke', NAVY); });
            const r = $('rail'); if (r) r.setAttribute('marker-end', 'url(#a)');
        }
    } else if (dark) {
        $('panel').setAttribute('opacity', 1);
    }
    onClick = () => play();
    onKey = (e) => { if ((e.key === 'r' || e.key === 'R') && !e.ctrlKey && !e.metaKey && !e.altKey) play(); };
    svg.addEventListener('click', onClick);
    document.addEventListener('keydown', onKey);
    first = true;
    if (!reduced()) drawConnectors();
    play();
}

export function unmount() {
    token++;
    timers.forEach(clearTimeout); timers = [];
    intro.forEach(clearTimeout); intro = [];
    if (svg && onClick) svg.removeEventListener('click', onClick);
    if (onKey) document.removeEventListener('keydown', onKey);
    root = svg = onKey = onClick = null;
}

function play() { return kept ? playKept() : playQuery(); }

/* ===== the connector lines draw themselves in, the way the status rail does on slide 16 ===== */
function drawConnectors() {
    const kids = Array.from(svg.children);
    const lines = kids.filter((c) => c.tagName === 'path' && c.getAttribute('stroke') === NAVY);
    const box = kids.find((c) => c.tagName === 'rect' && c.getAttribute('stroke-dasharray'));
    const defs = svg.querySelector('defs');
    const START = 350, STEP = 55, DUR = 450;
    const at = (ms, fn) => intro.push(setTimeout(() => { if (root) fn(); }, ms));
    lines.forEach((p, i) => {
        const L = p.getTotalLength(), dash = p.getAttribute('stroke-dasharray');
        const mS = p.getAttribute('marker-start'), mE = p.getAttribute('marker-end');
        p.removeAttribute('marker-start'); p.removeAttribute('marker-end');   // arrowheads arrive with the line's end
        let target = p, mask = null;
        if (dash) {   // a dashed line is revealed through a solid copy that draws
            mask = el('mask', { id: 'pf-m' + i, maskUnits: 'userSpaceOnUse', x: 0, y: 100, width: 1280, height: 560 });
            target = el('path', { d: p.getAttribute('d'), fill: 'none', stroke: '#fff', 'stroke-width': 8 });
            mask.appendChild(target); defs.appendChild(mask);
            p.setAttribute('mask', 'url(#pf-m' + i + ')');
        }
        target.style.strokeDasharray = L; target.style.strokeDashoffset = L;
        at(START + i * STEP, () => { target.style.transition = 'stroke-dashoffset ' + DUR + 'ms ease-out'; target.style.strokeDashoffset = 0; });
        at(START + i * STEP + DUR, () => {
            if (mask) { p.removeAttribute('mask'); mask.remove(); }
            else { target.style.transition = ''; target.style.strokeDasharray = ''; target.style.strokeDashoffset = ''; }
            if (mS) p.setAttribute('marker-start', mS);
            if (mE) p.setAttribute('marker-end', mE);
        });
    });
    if (box) {
        box.style.opacity = 0;
        at(START + 8 * STEP, () => { box.style.transition = 'opacity .6s'; box.style.opacity = 1; });
        at(START + 8 * STEP + 700, () => { box.style.transition = ''; box.style.opacity = ''; });
    }
}

/* ================= slide 7: the question goes through the system ================= */

const HEAD = [{ d: 'M94 300H118', hit: 'front', dwell: 500 }];
const EN = [   // an English question: sign-in only. Translation is a side path.
    { d: 'M118 300H272V362H300', hit: 'access', dwell: 700 },
    { d: 'M300 362H454V300' },
    { d: 'M454 300H482', hit: 'input', dwell: 700 },
    { d: 'M482 300H646', hit: 'search', dwell: 650 }
];
const TR = [   // another language: the side path through Translation
    { d: 'M118 300H272V238H300', hit: 'trans', dwell: 800 },
    { d: 'M300 238H454V300' },
    { d: 'M454 300H482', hit: 'input', dwell: 700 },
    { d: 'M482 300H646', hit: 'search', dwell: 650 }
];
const BACK = [
    { d: 'M873 268V196H210V268', hit: 'front', dwell: 350 },
    { d: 'M210 268V300H118H94', hit: 'leader', dwell: 1500 }
];
const BACK_TR = [
    { d: 'M873 268V196H363V210', hit: 'trans', dwell: 700 },
    { d: 'M363 210V238H272V300H118', hit: 'front', dwell: 350 },
    { d: 'M118 300H94', hit: 'leader', dwell: 1500 }
];
const RUNS = [
    { k: 'A QUESTION, SEARCH FIRST',
      lines: ['Check 1 reads the question.', "Search finds ISACA's own words.", 'Check 2 reads the answer.'],
      steps: [...HEAD, ...EN,
          { d: 'M646 300H709V512', hit: 'cyl', dwell: 700, a: true },
          { d: 'M709 512V300H873V268', hit: 'out', dwell: 850 }, ...BACK] },
    { k: "WHEN SEARCH ISN'T ENOUGH",
      lines: ['The AI writes only from the', 'passages found, then Check 2', 'reads every sentence.'],
      steps: [...HEAD, ...EN,
          { d: 'M646 300H873V268', hit: 'out', dwell: 350 },
          { d: 'M873 268V424', hit: 'ai', dwell: 450 },
          { d: 'M873 424V520', hit: 'rag', dwell: 400 },
          { d: 'M873 520V552H772', hit: 'cyl', dwell: 700 },
          { d: 'M772 552H873V488', hit: 'ai', dwell: 850, a: true },
          { d: 'M873 488V268', hit: 'out', dwell: 850 }, ...BACK] },
    { k: 'NOT IN ENGLISH?',
      lines: ['A side path for other languages:', 'Translation reads the question', 'in and the answer back out.'],
      steps: [...HEAD, ...TR,
          { d: 'M646 300H709V512', hit: 'cyl', dwell: 700, a: true },
          { d: 'M709 512V300H873V268', hit: 'out', dwell: 850 }, ...BACK_TR] },
    { k: 'TALK TO A PERSON',
      lines: ['One click, from any screen.', 'The question and history', 'go with it to your team.'],
      steps: [...HEAD,
          { d: 'M118 300H148V172H1049.6V268', hit: 'ticket', dwell: 800 },
          { d: 'M1049.6 268V300H1140', hit: 'team', dwell: 1700, a: true }] }
];
const SPEED = 320;   // diagram units per second

function pathEl(d) { const p = document.createElementNS(NS, 'path'); p.setAttribute('d', d); return p; }
function setNode(id, on) { const n = $('n-' + id); if (n) n.classList.toggle('on', on); }
function clearNodes() { root.querySelectorAll('.node.on').forEach((n) => n.classList.remove('on')); }
function place(g, p, len) { const pt = p.getPointAtLength(len); g.setAttribute('transform', 'translate(' + pt.x + ',' + pt.y + ')'); }
function setState(isAnswer) {
    const col = isAnswer ? GREEN : BLUE;
    $('dot-c').setAttribute('fill', col);
    $('dot-ring').setAttribute('fill', col);
    $('dot-t').textContent = isAnswer ? '✓' : '?';
}
function showCaption(i) {
    const cap = $('cap'); if (!cap) return;
    cap.setAttribute('opacity', 0);
    const my = token;
    timers.push(setTimeout(() => {
        if (my !== token || !root) return;
        $('cap-k').textContent = RUNS[i].k;
        for (let j = 0; j < 3; j++) $('cap-' + (j + 1)).textContent = RUNS[i].lines[j];
        cap.setAttribute('opacity', 1);
        root.querySelectorAll('#pips circle').forEach((c, j) => c.classList.toggle('on', j === i));
    }, 300));
}
function move(step, my) {
    return new Promise((res) => {
        const dot = $('dot'), p = pathEl(step.d), L = p.getTotalLength();
        const dur = L / SPEED * 1000;
        let t0 = null;
        function tick(t) {
            if (my !== token || !root) return res(false);
            if (t0 === null) t0 = t;
            let f = Math.min(1, (t - t0) / dur);
            f = f < 0.5 ? 2 * f * f * 0.35 + f * 0.65 : 1 - (2 * (1 - f) * (1 - f) * 0.35 + (1 - f) * 0.65);   // gentle ease
            place(dot, p, L * f);
            if (t - t0 < dur) requestAnimationFrame(tick); else { place(dot, p, L); res(true); }
        }
        requestAnimationFrame(tick);
    });
}
async function playRun(i, my) {
    const run = RUNS[i], dot = $('dot');
    showCaption(i); setState(false); clearNodes();
    dot.setAttribute('opacity', 1);
    place(dot, pathEl('M94 300H118'), 0);
    setNode('leader', true);
    if (!(await wait(700, my))) return false;
    for (let s = 0; s < run.steps.length; s++) {
        const st = run.steps[s];
        if (st.hit) setNode('leader', false);
        if (!(await move(st, my))) return false;
        if (st.a) setState(true);
        if (st.hit) {
            clearNodes(); setNode(st.hit, true);
            if (!(await wait(st.dwell, my))) return false;
            if (s < run.steps.length - 1) setNode(st.hit, false);
        }
    }
    return true;
}
async function playQuery() {
    const my = ++token;
    timers.forEach(clearTimeout); timers = [];
    const dot = $('dot');
    dot.setAttribute('opacity', 0); clearNodes();
    if (reduced()) { showCaption(0); return; }
    const lead = first ? 2700 : 1800; first = false;   // the first time, let the lines draw in before the dot starts
    if (!(await wait(lead, my))) return;
    for (let i = 0; i < RUNS.length; i++) {
        if (!(await playRun(i, my))) return;
        if (i < RUNS.length - 1) { dot.setAttribute('opacity', 0); clearNodes(); if (!(await wait(900, my))) return; }
    }
}

/* ================= slide 16: the diagram becomes a status rail ================= */

const PILL = {
    built: ['✓ Built', '#d3e8c5'],
    partial: ['◐ Partly', '#fee7c2'],
    designed: ['○ Designed', '#e7f0f5'],
    plan: ['▷ For the pilot', '#e7f0f5'],
    reword: ['✎ Reworded', '#b3e4f8']
};
// Where each part goes: [x, y, width, height, radius], and which item in slides.json it is.
const M = {
    access: { name: 'signin',      t: [154, 214, 44, 44, 22], cx: 176,  main: 1 },
    input:  { name: 'check1',      t: [386, 214, 44, 44, 22], cx: 408,  main: 1 },
    search: { name: 'search',      t: [618, 214, 44, 44, 22], cx: 640,  main: 1 },
    out:    { name: 'check2',      t: [850, 214, 44, 44, 22], cx: 872,  main: 1 },
    ticket: { name: 'ticket',      t: [1082, 214, 44, 44, 22], cx: 1104, main: 1 },
    trans:  { name: 'translation', t: [38, 382, 36, 36, 18],  cx: 56,   side: 1, w: 380 },
    ai:     { name: 'ai',          t: [506, 382, 36, 36, 18], cx: 524,  side: 1, w: 540 },
    front:  { name: 'contact',     t: [90, 548, 1100, 44, 22], bar: 1 }
};
const GONE = ['rag', 'cyl', 'leader', 'team'];
let orig = {};

const el = (name, attrs, txt) => { const e = document.createElementNS(NS, name); for (const k in attrs) e.setAttribute(k, attrs[k]); if (txt != null) e.textContent = txt; return e; };
const mainRect = (id) => root.querySelector('#n-' + id + ' > rect:not(.halo)');
const ease = (f) => (f < 0.5 ? 4 * f * f * f : 1 - Math.pow(-2 * f + 2, 3) / 2);
const item = (name) => items.find((i) => (i.name || '').toLowerCase() === name);

function wrap(text, fs, width) {
    const max = Math.floor(width / (fs * 0.5)), lines = []; let cur = '';
    text.split(' ').forEach((w) => { if ((cur + ' ' + w).trim().length > max) { lines.push(cur); cur = w; } else cur = (cur + ' ' + w).trim(); });
    if (cur) lines.push(cur);
    return lines;
}
function pill(x, y, st, anchor) {
    const p = PILL[st]; if (!p) return null;
    const w = p[0].length * 8 + 22, x0 = anchor === 'middle' ? x - w / 2 : x, g = el('g', {});
    g.appendChild(el('rect', { x: x0, y, width: w, height: 24, rx: 12, fill: p[1] }));
    g.appendChild(el('text', { x: x0 + w / 2, y: y + 17, 'text-anchor': 'middle', 'font-size': 14, 'font-weight': 700, fill: NAVY }, p[0]));
    return g;
}
function buildRail() {
    const front = $('sleek-front'); front.innerHTML = '';
    const ink = dark ? '#fff' : NAVY, sub = dark ? '#e7f0f5' : '#323e48';
    let i = 0;
    Object.keys(M).forEach((id) => {
        const m = M[id], it = item(m.name); if (!it) return;
        const g = el('g', { class: 'sl', opacity: 0, 'data-i': i++ });
        const body = it.built || it.text || '';
        if (m.main) {
            g.appendChild(el('text', { x: m.cx, y: 178, 'text-anchor': 'middle', 'font-size': 21, 'font-weight': 700, fill: ink }, it.title));
            const p = pill(m.cx, 272, it.status, 'middle'); if (p) g.appendChild(p);
            wrap(body, 16, 215).forEach((l, k) => g.appendChild(el('text', { x: m.cx, y: 322 + k * 21, 'text-anchor': 'middle', 'font-size': 16, 'font-weight': 300, fill: sub }, l)));
        } else if (m.side) {
            const x = m.cx + 32;
            g.appendChild(el('text', { x, y: 398, 'font-size': 19, 'font-weight': 700, fill: ink }, it.title));
            const p = pill(x, 408, it.status, 'start'); if (p) g.appendChild(p);
            wrap(body, 16, m.w).forEach((l, k) => g.appendChild(el('text', { x, y: 454 + k * 21, 'font-size': 16, 'font-weight': 300, fill: sub }, l)));
        } else {
            g.appendChild(el('text', { x: 124, y: 576, 'font-size': 18, 'font-weight': 700, fill: '#fff' }, it.title));
            g.appendChild(el('text', { x: 310, y: 576, 'font-size': 15.5, 'font-weight': 300, fill: '#e7f0f5' }, body));
            const p = pill(1058, 558, it.status, 'start'); if (p) g.appendChild(p);
        }
        front.appendChild(g);
        if (!m.bar && PILL[it.status]) {
            const r = m.t[4];
            front.appendChild(el('text', { class: 'glyph', x: m.t[0] + r, y: m.t[1] + r + 6, 'text-anchor': 'middle', 'font-size': m.main ? 19 : 16, 'font-weight': 700, opacity: 0,
                fill: (id === 'input' || id === 'out') ? '#fff' : NAVY }, PILL[it.status][0].charAt(0)));
        }
    });
}
function fadeOthers(op) {
    Array.prototype.forEach.call(svg.children, (c) => {
        if (c.tagName === 'defs' || c.id === 'sleek-back' || c.id === 'sleek-front') return;
        if (dark && c.id === 'panel' && op === 1) return;
        if (c.classList && c.classList.contains('node') && M[c.id.slice(2)]) return;
        c.style.animation = 'none'; c.style.transition = 'opacity .5s'; c.style.opacity = op;
    });
    GONE.forEach((id) => { const n = $('n-' + id); n.style.animation = 'none'; n.style.transition = 'opacity .5s'; n.style.opacity = op; });
}
function tween(my, instant) {
    return new Promise((res) => {
        const ids = Object.keys(M); let t0 = null; const D = instant ? 1 : 1000;
        ids.forEach((id) => {
            const r = mainRect(id);
            orig[id] = orig[id] || ['x', 'y', 'width', 'height', 'rx'].map((a) => parseFloat(r.getAttribute(a) || 0));
            r.style.transition = 'stroke .6s, fill .6s';
            r.style.stroke = dark ? '#fff' : NAVY;
            if (M[id].bar) r.style.fill = MED;
            Array.prototype.forEach.call($('n-' + id).children, (c) => { if (c !== r) { c.style.transition = 'opacity .3s'; c.style.opacity = 0; } });
        });
        function tick(t) {
            if (my !== token || !root) return res(false);
            if (t0 === null) t0 = t;
            const f = ease(Math.min(1, (t - t0) / D));
            ids.forEach((id) => { const r = mainRect(id), a = orig[id], b = M[id].t; ['x', 'y', 'width', 'height', 'rx'].forEach((k, j) => r.setAttribute(k, a[j] + (b[j] - a[j]) * f)); });
            if (f < 1) requestAnimationFrame(tick); else res(true);
        }
        requestAnimationFrame(tick);
    });
}
const draw = (id, ms) => { const p = $(id); p.style.transition = 'stroke-dashoffset ' + ms + 'ms ease-out'; p.style.strokeDashoffset = 0; };

function resetKept() {
    Object.keys(M).forEach((id) => {
        const r = mainRect(id), a = orig[id];
        r.style.transition = 'none'; r.style.stroke = ''; r.style.fill = '';
        if (a) ['x', 'y', 'width', 'height', 'rx'].forEach((k, j) => r.setAttribute(k, a[j]));
        Array.prototype.forEach.call($('n-' + id).children, (c) => { c.style.transition = 'none'; c.style.opacity = ''; });
    });
    Array.prototype.forEach.call(svg.children, (c) => { if (c.id !== 'sleek-back' && c.id !== 'sleek-front' && c.tagName !== 'defs') { c.style.transition = 'none'; c.style.opacity = ''; } });
    GONE.forEach((id) => { const n = $('n-' + id); n.style.transition = 'none'; n.style.opacity = ''; });
    ['rail', 'br-trans', 'br-ai'].forEach((id) => { const p = $(id); p.style.transition = 'none'; p.style.strokeDashoffset = p.getAttribute('stroke-dasharray'); });
    $('sleek-back').setAttribute('opacity', 0);
    $('sleek-front').innerHTML = '';
}
async function playKept() {
    const my = ++token;
    timers.forEach(clearTimeout); timers = [];
    resetKept(); buildRail();
    const instant = reduced();
    const lead = first ? 3000 : 2200; first = false;
    if (!instant && !(await wait(lead, my))) return;
    fadeOthers(0);
    if (!(await tween(my, instant))) return;
    $('sleek-back').setAttribute('opacity', 1);
    draw('rail', instant ? 0 : 700); draw('br-trans', instant ? 0 : 500); draw('br-ai', instant ? 0 : 500);
    root.querySelectorAll('.sl, .glyph').forEach((g, k) => {
        timers.push(setTimeout(() => {
            if (my !== token || !root) return;
            g.style.transition = 'opacity .5s'; g.style.opacity = 1;
        }, instant ? 0 : 150 + (parseInt(g.getAttribute('data-i') || (k % 8), 10)) * 110));
    });
}

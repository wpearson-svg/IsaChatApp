// ============================================================
// LibraryPanel.razor.js -- Listen, for the Library.
//
// Reads a document aloud word for word with the browser's own
// read-aloud (speechSynthesis): the voices already on the leader's
// computer or phone. Nothing is installed and no text is sent anywhere.
//
// It reads one sentence at a time (each <span data-i> in the text), so
// the sentence being read can be highlighted and kept in view, and so
// pausing and picking up again never loses the place. Sentences already
// read turn grey. Clicking a sentence reads from there.
//
// Player buttons (data-lib="..."): prev / next section, toggle
// (play and pause), speed (0.75x, 1x, 1.25x, 1.5x).
// ============================================================

const SPEEDS = [0.75, 1, 1.25, 1.5];
const WORDS_PER_MINUTE = 150;

let root = null;        // the panel's scrolling body for the current document
let sentences = [];     // the sentence spans, in reading order
let sectionStarts = []; // sentence numbers where each section starts
let words = [];         // words per sentence, for the time left
let current = 0;        // the sentence being read, or next to read
let playing = false;
let speed = 1;
let runId = 0;          // stops a cancelled sentence from moving on

const supported = typeof window !== 'undefined' && 'speechSynthesis' in window;

// Called by LibraryPanel.razor after a document is shown.
export function attach(body, autoplay) {
    const text = body?.querySelector('[data-lib-text]');
    if (!text) return;
    if (root !== body) {
        stop();
        root = body;
        sentences = [...text.querySelectorAll('[data-i]')];
        words = sentences.map(s => s.textContent.trim().split(/\s+/).length);
        sectionStarts = [...text.querySelectorAll('[data-section]')]
            .map(el => sentences.indexOf(el.matches('[data-i]') ? el : el.querySelector('[data-i]')))
            .filter(i => i >= 0);
        if (!sectionStarts.includes(0)) sectionStarts.unshift(0);
        current = 0;
        root.addEventListener('click', onClick);
        if (!supported) {
            setStatus("This browser can't read aloud. You can still read the text below.");
            root.querySelectorAll('[data-lib]').forEach(b => b.disabled = true);
            return;
        }
        update();
    }
    if (autoplay && supported) play();
}

export function stop() {
    runId++;
    playing = false;
    if (supported) speechSynthesis.cancel();
    if (root) {
        root.removeEventListener('click', onClick);
        sentences.forEach(s => s.classList.remove('is-reading', 'is-read'));
    }
    root = null;
    sentences = [];
}

function onClick(e) {
    const btn = e.target.closest('[data-lib]');
    if (btn) {
        const action = btn.dataset.lib;
        if (action === 'toggle') playing ? pause() : play();
        else if (action === 'prev') jump(-1);
        else if (action === 'next') jump(1);
        else if (action === 'speed') cycleSpeed(btn);
        return;
    }
    // Clicking a sentence reads from there.
    const s = e.target.closest('[data-i]');
    if (s && supported) {
        current = sentences.indexOf(s);
        play();
    }
}

function play() {
    if (!sentences.length) return;
    if (current >= sentences.length) current = 0;   // finished: start again from the top
    playing = true;
    speakCurrent();
}

function pause() {
    runId++;
    playing = false;
    speechSynthesis.cancel();
    update();
}

function speakCurrent() {
    const id = ++runId;
    speechSynthesis.cancel();
    update(true);
    const u = new SpeechSynthesisUtterance(sentences[current].textContent.trim());
    u.rate = speed;
    u.lang = document.documentElement.lang || 'en';
    u.onend = () => {
        if (id !== runId || !playing) return;
        current++;
        if (current < sentences.length) speakCurrent();
        else { playing = false; update(); setStatus('Finished. Press play to start again.'); }
    };
    u.onerror = () => {
        if (id !== runId) return;     // cancelled on purpose (pause, skip)
        playing = false;
        update();
        setStatus("Reading stopped. Press play to carry on.");
    };
    speechSynthesis.speak(u);
}

// Back or forward a section. Back from partway through a section goes to its start.
function jump(dir) {
    let target;
    if (dir < 0) {
        const before = sectionStarts.filter(i => i < current);
        target = before.length ? before[before.length - 1] : 0;
        if (playing && current - target <= 1 && before.length > 1) target = before[before.length - 2];
    } else {
        target = sectionStarts.find(i => i > current);
        if (target === undefined) return;
    }
    current = target;
    playing ? speakCurrent() : update(true);
}

function cycleSpeed(btn) {
    speed = SPEEDS[(SPEEDS.indexOf(speed) + 1) % SPEEDS.length];
    btn.textContent = `${speed}×`;
    btn.setAttribute('aria-label', `Reading speed, ${speed} times`);
    if (playing) speakCurrent();   // the new speed applies from this sentence
    else update();
}

// Highlight, greyed-out sentences, the play/pause button, the progress line and the time left.
function update(scroll) {
    if (!root) return;
    sentences.forEach((s, i) => {
        s.classList.toggle('is-reading', playing && i === current);
        s.classList.toggle('is-read', i < current);
    });
    const toggle = root.querySelector('[data-lib="toggle"]');
    if (toggle) {
        toggle.classList.toggle('is-playing', playing);
        toggle.setAttribute('aria-label', playing ? 'Pause' : 'Listen');
        toggle.title = playing ? 'Pause' : 'Listen';
    }
    const bar = root.querySelector('[data-lib-bar]');
    if (bar) bar.style.width = `${sentences.length ? (current / sentences.length) * 100 : 0}%`;

    const left = words.slice(current).reduce((a, b) => a + b, 0) / (WORDS_PER_MINUTE * speed);
    const mins = Math.max(1, Math.round(left));
    const where = sectionName();
    if (current < sentences.length) {
        setStatus(`${where ? where + ' · ' : ''}about ${mins} minute${mins === 1 ? '' : 's'} left`);
    }

    if (scroll && sentences[current]) {
        const reduce = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
        sentences[current].scrollIntoView({ block: 'center', behavior: reduce ? 'auto' : 'smooth' });
    }
}

function sectionName() {
    const start = sectionStarts.filter(i => i <= current).pop();
    const el = sentences[start];
    return el && el.matches('[data-section]') ? el.textContent.trim() : '';
}

function setStatus(text) {
    const s = root?.querySelector('[data-lib-status]');
    if (s) s.textContent = text;
}

// KazanlakEvents — Event Edit page JavaScript

(function () {

// ── State ─────────────────────────────────────────────────────────────────
let wzStep = 1;
let ttCount = document.querySelectorAll('#tt-rows .wz-tt-row').length;

// ── Progress bar ──────────────────────────────────────────────────────────
function wzProgress(n) {
    for (let i = 1; i <= 4; i++) {
        const c = document.getElementById('wz-c-' + i);
        c.classList.remove('current', 'upcoming');
        if (i === n) c.classList.add('current');
        else if (i > n) c.classList.add('upcoming');
        if (i < 4) {
            document.getElementById('wz-conn-' + i)
                    .classList.toggle('upcoming', i >= n);
        }
    }
}

// ── Navigation ────────────────────────────────────────────────────────────
function wzShow(n) {
    document.querySelectorAll('.wz-step')
            .forEach(el => el.classList.add('d-none'));
    document.getElementById('step-' + n).classList.remove('d-none');
    wzStep = n;
    wzProgress(n);
    window.scrollTo({ top: 0, behavior: 'smooth' });
}

function wzHasTicketing() {
    return document.getElementById('HasTicketing').checked;
}

function wzNext() {
    if (!wzValidate(wzStep)) return;
    let next = wzStep + 1;
    if (next === 3 && !wzHasTicketing()) next = 4;
    if (next <= 4) wzShow(next);
}

function wzPrev() {
    let prev = wzStep - 1;
    if (wzStep === 4 && !wzHasTicketing()) prev = 2;
    if (prev >= 1) wzShow(prev);
}

// ── Validation ────────────────────────────────────────────────────────────
function wzMark(el, msg) {
    el.classList.add('is-invalid');
    let fb = el.parentElement.querySelector('.wz-fb');
    if (!fb) {
        fb = document.createElement('div');
        fb.className = 'invalid-feedback wz-fb';
        el.insertAdjacentElement('afterend', fb);
    }
    fb.textContent = msg;
}
function wzClear(el) {
    el.classList.remove('is-invalid');
    const fb = el.parentElement.querySelector('.wz-fb');
    if (fb) fb.textContent = '';
}

function wzValidate(step) {
    let ok = true;
    if (step === 1) {
        [
            ['f-title',  'Title is required.'],
            ['f-cat',    'Category is required.'],
            ['f-start',  'Start date is required.'],
            ['f-end',    'End date is required.'],
        ].forEach(([id, msg]) => {
            const el = document.getElementById(id);
            if (!el.value.trim()) { wzMark(el, msg); ok = false; }
            else wzClear(el);
        });
    } else if (step === 2) {
        const desc = document.getElementById('f-desc');
        if (!desc.value.trim()) { wzMark(desc, 'Description is required.'); ok = false; }
        else wzClear(desc);
    }
    return ok;
}

// ── Character counter ─────────────────────────────────────────────────────
function wzCounter(el, id, max) {
    var counter = document.getElementById(id);
    var template = counter.dataset.template || '';
    counter.textContent = (max - el.value.length) + ' ' + template;
}

// ── Ticketing toggle ──────────────────────────────────────────────────────
function onTicketingToggle() {
    syncTicketSection();
}

function syncTicketSection() {
    const ticketing = wzHasTicketing();
    const sec = document.getElementById('tt-section');
    sec.style.opacity = ticketing ? '1' : '.4';
    sec.style.pointerEvents = ticketing ? '' : 'none';
}

// ── Tag chips ─────────────────────────────────────────────────────────────
function wzToggleTag(btn, val) {
    const cb = document.getElementById('tag-cb-' + val);
    const on = btn.classList.toggle('tag-chip-selected');
    if (cb) cb.checked = on;
}

// ── Ticket rows ───────────────────────────────────────────────────────────
function wzAddTicket(n, q, d) {
    const btn = document.getElementById('wz-add-ticket-btn');
    const nameLabel   = btn ? btn.dataset.nameLabel   : 'Name';
    const qtyLabel    = btn ? btn.dataset.qtyLabel    : 'Quantity';
    const descLabel   = btn ? btn.dataset.descLabel   : 'Description';
    const removeTitle = btn ? btn.dataset.removeTitle : 'Remove';
    const i = ttCount++;
    const row = document.createElement('div');
    row.className = 'row g-2 mb-2 align-items-end wz-tt-row';
    row.innerHTML =
        '<div class="col-md-4">' +
            '<label class="form-label fw-semibold small">' + nameLabel + '</label>' +
            '<input name="TicketTypeInputs[' + i + '].Name" class="form-control form-control-sm"' +
            '       value="' + (n || '') + '" placeholder="e.g. General Admission" />' +
        '</div>' +
        '<div class="col-md-3">' +
            '<label class="form-label fw-semibold small">' + qtyLabel + '</label>' +
            '<input name="TicketTypeInputs[' + i + '].Quantity" class="form-control form-control-sm"' +
            '       type="number" min="1" value="' + (q || 100) + '" />' +
        '</div>' +
        '<div class="col-md-4">' +
            '<label class="form-label fw-semibold small">' +
                descLabel + ' <span class="fw-normal text-muted">(opt.)</span>' +
            '</label>' +
            '<input name="TicketTypeInputs[' + i + '].Description" class="form-control form-control-sm"' +
            '       value="' + (d || '') + '" />' +
        '</div>' +
        '<div class="col-md-1 d-flex align-items-end pb-1">' +
            '<button type="button" class="ke-btn ke-btn-danger ke-btn-sm w-100"' +
            '        onclick="this.closest(\'.wz-tt-row\').remove()"' +
            '        title="' + removeTitle + '">' +
                '<i class="bi bi-x-lg"></i>' +
            '</button>' +
        '</div>';
    document.getElementById('tt-rows').appendChild(row);
}

// ── Image drag-and-drop ───────────────────────────────────────────────────
function wzDrop(e, inputId, prevId, multi) {
    e.preventDefault();
    e.currentTarget.classList.remove('dragover');
    const inp = document.getElementById(inputId);
    const dt = new DataTransfer();
    for (const f of e.dataTransfer.files) dt.items.add(f);
    inp.files = dt.files;
    wzPreview(inp, prevId, multi);
}

function wzDropExtra(e) {
    e.preventDefault();
    e.currentTarget.classList.remove('dragover');
    const inp = document.getElementById('extra-inp');
    const dt = new DataTransfer();
    for (const f of e.dataTransfer.files) dt.items.add(f);
    inp.files = dt.files;
    wzPreview(inp, 'extra-prev', true);
}

function wzPreview(inp, prevId, multi) {
    const container = document.getElementById(prevId);
    if (!inp.files || !inp.files.length) return;
    if (!multi) {
        const r = new FileReader();
        r.onload = ev => {
            container.innerHTML =
                '<img src="' + ev.target.result + '" class="rounded"' +
                '     style="max-height:160px;max-width:100%;object-fit:cover" />';
        };
        r.readAsDataURL(inp.files[0]);
    } else {
        container.innerHTML = '';
        Array.from(inp.files).forEach(file => {
            const r = new FileReader();
            r.onload = ev => {
                const d = document.createElement('div');
                d.style.cssText = 'position:relative;display:inline-block';
                d.innerHTML =
                    '<img src="' + ev.target.result + '" class="rounded"' +
                    '     style="height:80px;width:80px;object-fit:cover" />';
                container.appendChild(d);
            };
            r.readAsDataURL(file);
        });
    }
}

// ── Init ──────────────────────────────────────────────────────────────────
wzShow(1);

document.addEventListener('DOMContentLoaded', function () {
    for (let s = 1; s <= 4; s++) {
        const el = document.getElementById('step-' + s);
        if (el && el.querySelector('.field-validation-error:not(:empty), .is-invalid')) {
            wzShow(s);
            break;
        }
    }
    const sd = document.getElementById('f-short');
    if (sd) wzCounter(sd, 'cnt-short', 500);
    syncTicketSection();
});

// Expose functions needed by inline HTML event handlers
window.wzNext            = wzNext;
window.wzPrev            = wzPrev;
window.wzCounter         = wzCounter;
window.wzToggleTag       = wzToggleTag;
window.wzAddTicket       = wzAddTicket;
window.wzDrop            = wzDrop;
window.wzDropExtra       = wzDropExtra;
window.wzPreview         = wzPreview;
window.onTicketingToggle = onTicketingToggle;

}());

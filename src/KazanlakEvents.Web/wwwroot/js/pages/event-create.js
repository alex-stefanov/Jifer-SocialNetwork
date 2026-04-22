// KazanlakEvents — Event Create page JavaScript

(function () {

// ── State ─────────────────────────────────────────────────────────────────
let wzStep = 1, rtCount = 0;

// ── Progress bar ───────────────────────────────────────────────────────────
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

// ── Navigation ─────────────────────────────────────────────────────────────
function wzShow(n) {
    document.querySelectorAll('.wz-step').forEach(el => el.classList.add('d-none'));
    document.getElementById('step-' + n).classList.remove('d-none');
    wzStep = n;
    wzProgress(n);
    window.scrollTo({ top: 0, behavior: 'smooth' });
}

function wzNext() {
    if (!wzValidate(wzStep)) return;
    if (wzStep < 4) wzShow(wzStep + 1);
}

function wzPrev() {
    if (wzStep > 1) wzShow(wzStep - 1);
}

// ── Validation ─────────────────────────────────────────────────────────────
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
            ['f-title', 'Title is required.'],
            ['f-cat',   'Category is required.'],
            ['f-start', 'Start date is required.'],
            ['f-end',   'End date is required.'],
        ].forEach(([id, msg]) => {
            const el = document.getElementById(id);
            if (!el.value.trim()) { wzMark(el, msg); ok = false; }
            else wzClear(el);
        });
    } else if (step === 2) {
        const desc = document.getElementById('f-desc');
        if (!desc.value.trim()) { wzMark(desc, 'Description is required.'); ok = false; }
        else wzClear(desc);
    } else if (step === 3) {
        var cap = parseInt(document.getElementById('f-capacity') && document.getElementById('f-capacity').value, 10);
        var rows = document.querySelectorAll('.wz-rt-row');
        if (rows.length > 0 && !isNaN(cap) && cap > 0) {
            var tot = 0;
            rows.forEach(function(r) {
                var q = parseInt(r.querySelector('input[type="number"]') && r.querySelector('input[type="number"]').value, 10);
                if (!isNaN(q)) tot += q;
            });
            if (tot !== cap) {
                document.getElementById('rt-counter-text').style.color = 'var(--ke-danger, #dc2626)';
                ok = false;
            }
        }
    }
    return ok;
}

// ── Character counter ──────────────────────────────────────────────────────
(function () {
    var input = document.getElementById('short-desc-input');
    var count = document.getElementById('char-count');
    if (input && count) {
        function updateCount() { count.textContent = 500 - input.value.length; }
        input.addEventListener('input', updateCount);
        updateCount();
    }
})();

// ── Registration type rows ─────────────────────────────────────────────────
function wzUpdateCapacityCounter() {
    var capEl    = document.getElementById('f-capacity');
    var capacity = capEl ? parseInt(capEl.value, 10) : NaN;
    var counter  = document.getElementById('rt-capacity-counter');
    var text     = document.getElementById('rt-counter-text');
    var btn      = document.getElementById('step3-continue-btn');
    if (!counter || !text) return;

    var rows  = document.querySelectorAll('.wz-rt-row');
    var total = 0;
    rows.forEach(function(row) {
        var inp = row.querySelector('input[type="number"]');
        var qty = inp ? parseInt(inp.value, 10) : NaN;
        if (!isNaN(qty)) total += qty;
    });

    if (rows.length === 0 || isNaN(capacity) || capacity <= 0) {
        counter.style.display = 'none';
        if (btn) { btn.disabled = false; btn.classList.remove('disabled'); }
        return;
    }

    counter.style.display = '';
    var match = total === capacity;
    text.textContent = 'Total: ' + total + ' / ' + capacity + ' spots';
    text.style.color = match ? 'var(--ke-success, #16a34a)' : 'var(--ke-danger, #dc2626)';
    if (btn) {
        btn.disabled = !match;
        btn.classList.toggle('disabled', !match);
    }
}

function wzAddRegistration() {
    var addBtn = document.getElementById('wz-add-reg-btn');
    var nameLabel   = addBtn ? addBtn.dataset.nameLabel   : 'Name';
    var spotsLabel  = addBtn ? addBtn.dataset.spotsLabel  : 'Spots';
    var removeTitle = addBtn ? addBtn.dataset.removeTitle : 'Remove';

    var hint = document.getElementById('rt-empty-hint');
    if (hint) hint.classList.add('d-none');
    var i = rtCount++;
    var row = document.createElement('div');
    row.className = 'wz-rt-row';
    row.innerHTML =
        '<div class="ke-field wz-rt-name">' +
            '<label class="form-label fw-semibold small">' + nameLabel + '</label>' +
            '<input name="TicketTypeInputs[' + i + '].Name" class="form-control form-control-sm"' +
            '       placeholder="e.g. General Registration" />' +
        '</div>' +
        '<div class="ke-field wz-rt-cap">' +
            '<label class="form-label fw-semibold small">' + spotsLabel + '</label>' +
            '<input name="TicketTypeInputs[' + i + '].Quantity" class="form-control form-control-sm"' +
            '       type="number" min="1" placeholder="\u221e" />' +
        '</div>' +
        '<div class="wz-rt-del">' +
            '<button type="button" class="ke-btn ke-btn-danger ke-btn-sm"' +
            '        onclick="wzRemoveRegistration(this)" title="' + removeTitle + '">' +
                '<i class="bi bi-x-lg"></i>' +
            '</button>' +
        '</div>';
    document.getElementById('rt-rows').appendChild(row);
    row.querySelector('input[type="number"]').addEventListener('input', wzUpdateCapacityCounter);
    wzUpdateCapacityCounter();
}

function wzRemoveRegistration(btn) {
    btn.closest('.wz-rt-row').remove();
    if (!document.querySelector('.wz-rt-row')) {
        var hint = document.getElementById('rt-empty-hint');
        if (hint) hint.classList.remove('d-none');
    }
    wzUpdateCapacityCounter();
}

// ── Image drag-and-drop ────────────────────────────────────────────────────
function wzDrop(e, inputId, prevId, multi) {
    e.preventDefault();
    e.currentTarget.classList.remove('dragover');
    const inp = document.getElementById(inputId);
    const dt = new DataTransfer();
    for (const f of e.dataTransfer.files) dt.items.add(f);
    inp.files = dt.files;
    wzPreview(inp, prevId, multi);
}

function wzPreview(inp, prevId, multi) {
    const container = document.getElementById(prevId);
    if (!inp.files || !inp.files.length) return;
    if (!multi) {
        const r = new FileReader();
        r.onload = ev => {
            container.innerHTML =
                '<img src="' + ev.target.result + '" class="rounded"' +
                '     style="max-height:180px;max-width:100%;object-fit:cover" />';
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

// ── Tag chips ──────────────────────────────────────────────────────────────
function wzToggleTag(btn) {
    const tagId = btn.dataset.tagId;
    const checkbox = document.getElementById('tag-' + tagId);
    const selected = !checkbox.checked;
    checkbox.checked = selected;
    btn.classList.toggle('tag-chip-selected', selected);
    btn.classList.toggle('ke-btn-ghost', !selected);
}

function wzAddNewTag() {
    const input = document.getElementById('new-tag-input');
    const val = input.value.trim();
    if (!val) return;
    const chips = document.getElementById('new-tag-chips');
    const chip = document.createElement('span');
    chip.className = 'badge rounded-pill px-3 py-2';
    chip.style.cssText = 'background:var(--ke-primary);color:#fff;font-size:.8rem;font-weight:500';
    chip.textContent = val;
    chips.appendChild(chip);
    document.getElementById('new-tag-value').value = val;
    input.value = '';
    input.focus();
}

// ── Venue request panel ────────────────────────────────────────────────────
var venueReqMap = null, venueReqMarker = null;

function wzOnVenueChange(val) {
    var panel = document.getElementById('venue-request-panel');
    if (val === '__new__') {
        panel.classList.remove('d-none');
        initVenueReqMap();
    } else {
        panel.classList.add('d-none');
    }
}

function initVenueReqMap() {
    if (venueReqMap) return;
    var lat = 42.6167, lng = 25.4000;
    venueReqMap = L.map('venue-req-map').setView([lat, lng], 14);
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; OpenStreetMap contributors', maxZoom: 19
    }).addTo(venueReqMap);
    venueReqMap.on('click', function(e) {
        var la = e.latlng.lat.toFixed(6), ln = e.latlng.lng.toFixed(6);
        document.getElementById('venue-lat-input').value = la;
        document.getElementById('venue-lng-input').value = ln;
        document.getElementById('venue-latlng-label').textContent = ' (' + la + ', ' + ln + ')';
        if (venueReqMarker) venueReqMarker.setLatLng(e.latlng);
        else venueReqMarker = L.marker(e.latlng).addTo(venueReqMap);
    });
    setTimeout(function() { venueReqMap.invalidateSize(); }, 100);
}

// ── Init ───────────────────────────────────────────────────────────────────
wzShow(1);

document.addEventListener('DOMContentLoaded', function () {
    for (let s = 1; s <= 4; s++) {
        const el = document.getElementById('step-' + s);
        if (el && el.querySelector('.field-validation-error:not(:empty), .is-invalid')) {
            wzShow(s);
            break;
        }
    }
});

// Expose functions needed by inline HTML event handlers
window.wzNext               = wzNext;
window.wzPrev               = wzPrev;
window.wzToggleTag          = wzToggleTag;
window.wzAddNewTag          = wzAddNewTag;
window.wzAddRegistration    = wzAddRegistration;
window.wzRemoveRegistration = wzRemoveRegistration;
window.wzDrop               = wzDrop;
window.wzPreview            = wzPreview;
window.wzOnVenueChange      = wzOnVenueChange;

}());

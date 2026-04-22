// KazanlakEvents — Event Details page JavaScript

// ── Leaflet icon fix (must run before map init) ───────────────────
if (typeof L !== 'undefined') {
    delete L.Icon.Default.prototype._getIconUrl;
    L.Icon.Default.mergeOptions({
        iconUrl: 'https://cdn.jsdelivr.net/npm/leaflet@1.9.4/dist/images/marker-icon.png',
        iconRetinaUrl: 'https://cdn.jsdelivr.net/npm/leaflet@1.9.4/dist/images/marker-icon-2x.png',
        shadowUrl: 'https://cdn.jsdelivr.net/npm/leaflet@1.9.4/dist/images/marker-shadow.png'
    });
}

// ── Gallery lightbox ──────────────────────────────────────────────
var lbThumbs = Array.from(document.querySelectorAll('.ke-gallery-thumb'));
var lbIdx = 0;

function openLightbox(el) {
    lbIdx = lbThumbs.indexOf(el);
    document.getElementById('ke-lb-img').src = el.dataset.full || el.src;
    document.getElementById('ke-lightbox').classList.add('open');
    document.body.style.overflow = 'hidden';
    updateLbNav();
}
function closeLightbox() {
    document.getElementById('ke-lightbox').classList.remove('open');
    document.body.style.overflow = '';
}
function lbNav(dir) {
    lbIdx = (lbIdx + dir + lbThumbs.length) % lbThumbs.length;
    var t = lbThumbs[lbIdx];
    document.getElementById('ke-lb-img').src = t.dataset.full || t.src;
    updateLbNav();
}
function updateLbNav() {
    var prev = document.getElementById('ke-lb-prev');
    var next = document.getElementById('ke-lb-next');
    var show = lbThumbs.length > 1 ? '' : 'none';
    if (prev) prev.style.display = show;
    if (next) next.style.display = show;
}

// Gallery thumbnail clicks
document.querySelectorAll('.ke-gallery-thumb').forEach(function (thumb) {
    thumb.addEventListener('click', function () { openLightbox(thumb); });
});

// Lightbox backdrop click
var lightbox = document.getElementById('ke-lightbox');
if (lightbox) {
    lightbox.addEventListener('click', function (e) {
        if (e.target === lightbox) closeLightbox();
    });
}

// Lightbox close button
var lbCloseBtn = document.querySelector('.ke-lightbox-close');
if (lbCloseBtn) lbCloseBtn.addEventListener('click', closeLightbox);

// Lightbox nav buttons
var lbPrev = document.getElementById('ke-lb-prev');
var lbNext = document.getElementById('ke-lb-next');
if (lbPrev) lbPrev.addEventListener('click', function () { lbNav(-1); });
if (lbNext) lbNext.addEventListener('click', function () { lbNav(1); });

document.addEventListener('keydown', function (e) {
    var lb = document.getElementById('ke-lightbox');
    if (!lb || !lb.classList.contains('open')) return;
    if (e.key === 'Escape')     closeLightbox();
    if (e.key === 'ArrowLeft')  lbNav(-1);
    if (e.key === 'ArrowRight') lbNav(1);
});

// ── Star rating picker ────────────────────────────────────────────
(function () {
    var picker = document.getElementById('ke-star-picker');
    if (!picker) return;
    var stars = Array.from(picker.querySelectorAll('.ke-star-pick'));
    var input = document.getElementById('ke-score-input');
    stars.forEach(function (star, i) {
        star.addEventListener('mouseover', function () {
            stars.forEach(function (s, j) { s.classList.toggle('lit', j <= i); });
        });
        star.addEventListener('click', function () {
            input.value = star.dataset.val;
            stars.forEach(function (s, j) { s.classList.toggle('lit', j <= i); });
        });
    });
    picker.addEventListener('mouseleave', function () {
        var sel = parseInt(input.value) || 0;
        stars.forEach(function (s, j) { s.classList.toggle('lit', j < sel); });
    });
}());

// ── Copy link button ──────────────────────────────────────────────
(function () {
    var copyBtn = document.getElementById('ke-copy-link-btn');
    if (!copyBtn) return;
    var originalHtml = copyBtn.innerHTML;
    var copiedText   = copyBtn.dataset.copiedText || 'Copied';
    copyBtn.addEventListener('click', function () {
        navigator.clipboard.writeText(window.location.href).then(function () {
            copyBtn.style.color = 'var(--ke-success)';
            copyBtn.style.borderColor = 'var(--ke-success)';
            copyBtn.innerHTML =
                '<svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">' +
                '<polyline points="20 6 9 17 4 12"/></svg> ' + copiedText;
            setTimeout(function () {
                copyBtn.innerHTML = originalHtml;
                copyBtn.style.color = '';
                copyBtn.style.borderColor = '';
            }, 2000);
        });
    });
}());

// ── Main comment textarea: auto-expand + show send button ─────────
(function () {
    var ta   = document.getElementById('ke-main-textarea');
    var send = document.getElementById('ke-main-send');
    if (!ta || !send) return;
    function syncSend() {
        ta.style.height = 'auto';
        ta.style.height = ta.scrollHeight + 'px';
        send.classList.toggle('visible', ta.value.trim().length > 0);
    }
    ta.addEventListener('input', syncSend);
}());

// ── Reply toggle (event delegation) ──────────────────────────────
document.addEventListener('click', function (e) {
    // Reply toggle button
    if (e.target.classList.contains('reply-toggle')) {
        var actionsDiv = e.target.closest('.ke-comment-actions');
        var form = actionsDiv ? actionsDiv.nextElementSibling : null;
        if (!form || !form.classList.contains('reply-form')) return;
        document.querySelectorAll('.reply-form').forEach(function (f) {
            if (f !== form) f.style.display = 'none';
        });
        var isHidden = form.style.display === 'none' || form.style.display === '';
        form.style.display = isHidden ? 'flex' : 'none';
        if (isHidden) {
            var inp = form.querySelector('input[name="content"]');
            if (inp) inp.focus();
        }
    }

    // Delete confirm button
    var confirmBtn = e.target.closest('[data-confirm-form]');
    if (confirmBtn) {
        keConfirm(
            confirmBtn.dataset.confirmTitle,
            confirmBtn.dataset.confirmText,
            confirmBtn.dataset.confirmBtn,
            function () { document.getElementById(confirmBtn.dataset.confirmForm).submit(); }
        );
        return;
    }

    // Show-more-replies button
    if (e.target.hasAttribute('data-toggle-replies')) {
        var btn  = e.target;
        var wrap = btn.nextElementSibling;
        if (!wrap) return;
        var isHidden = wrap.classList.toggle('ke-nested-replies-hidden');
        btn.style.display = isHidden ? '' : 'none';
    }
});

// ── Leaflet map ───────────────────────────────────────────────────
(function () {
    var mapEl = document.getElementById('ke-detail-map');
    if (!mapEl) return;
    var lat = parseFloat(mapEl.dataset.lat);
    var lng = parseFloat(mapEl.dataset.lng);
    if (isNaN(lat) || isNaN(lng)) return;
    var popup = mapEl.dataset.popup || '';
    var m = L.map('ke-detail-map', { zoomControl: true, scrollWheelZoom: false })
             .setView([lat, lng], 15);
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '\u00a9 OpenStreetMap contributors', maxZoom: 19
    }).addTo(m);
    L.marker([lat, lng]).addTo(m).bindPopup(popup);
}());

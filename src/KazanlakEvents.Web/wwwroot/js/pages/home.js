// KazanlakEvents — Home page JavaScript

document.addEventListener('DOMContentLoaded', function () {

    // ── Leaflet map ───────────────────────────────────────────────
    var mapEl = document.getElementById('landing-map');
    if (mapEl) {
        // Fix Leaflet default marker icon path
        delete L.Icon.Default.prototype._getIconUrl;
        L.Icon.Default.mergeOptions({
            iconUrl: 'https://cdn.jsdelivr.net/npm/leaflet@1.9.4/dist/images/marker-icon.png',
            iconRetinaUrl: 'https://cdn.jsdelivr.net/npm/leaflet@1.9.4/dist/images/marker-icon-2x.png',
            shadowUrl: 'https://cdn.jsdelivr.net/npm/leaflet@1.9.4/dist/images/marker-shadow.png'
        });

        var map = L.map('landing-map', {
            scrollWheelZoom: false,
            zoomControl: true,
            attributionControl: false
        }).setView([42.6190, 25.3990], 14);

        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            maxZoom: 18
        }).addTo(map);

        var eventsData = mapEl.dataset.events ? JSON.parse(mapEl.dataset.events) : [];
        var viewText   = mapEl.dataset.viewText || 'View event';

        eventsData.forEach(function (ev) {
            L.marker([ev.lat, ev.lng])
                .addTo(map)
                .bindPopup(
                    '<div style="font-family:Inter,sans-serif;min-width:160px;">' +
                    '<strong style="font-size:13px;">' + ev.title + '</strong><br>' +
                    '<a href="' + ev.url + '" style="font-size:11px;color:#6366f1;font-weight:500;">' + viewText + ' \u2192</a>' +
                    '</div>'
                );
        });

        // Fix Leaflet rendering when container was hidden
        setTimeout(function () { map.invalidateSize(); }, 200);

        // Scroll into view when Map toggle clicked
        var toggleBtn = document.getElementById('ke-map-toggle-btn');
        if (toggleBtn) {
            toggleBtn.addEventListener('click', function (e) {
                e.preventDefault();
                mapEl.scrollIntoView({ behavior: 'smooth', block: 'center' });
            });
        }
    }

    // ── Hero nav: hamburger ───────────────────────────────────────
    (function () {
        var hbtn = document.getElementById('hero-hamburger');
        var mnav = document.getElementById('hero-mobile-nav');
        if (!hbtn || !mnav) return;
        hbtn.addEventListener('click', function () {
            var open = mnav.classList.toggle('open');
            hbtn.setAttribute('aria-expanded', String(open));
        });
    }());

    // ── Hero nav: avatar dropdown ─────────────────────────────────
    (function () {
        var btn  = document.getElementById('hero-avatar-btn');
        var menu = document.getElementById('hero-avatar-menu');
        if (!btn || !menu) return;
        btn.addEventListener('click', function (e) {
            e.stopPropagation();
            var opening = !menu.classList.contains('open');
            menu.classList.toggle('open', opening);
            btn.setAttribute('aria-expanded', String(opening));
        });
        document.addEventListener('click', function () {
            menu.classList.remove('open');
            btn.setAttribute('aria-expanded', 'false');
        });
    }());

});

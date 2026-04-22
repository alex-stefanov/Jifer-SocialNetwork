// KazanlakEvents — Event Check-In page JavaScript

(function () {
    var page = document.querySelector('.checkin-page');
    if (!page) return;

    var eventId      = page.dataset.eventId;
    var checkInUrl   = page.dataset.checkInUrl;
    var totalTickets = parseInt(page.dataset.totalTickets, 10) || 0;
    var tryAgainText = page.dataset.tryAgainText || 'Please try again';

    var token = (document.querySelector('input[name="__RequestVerificationToken"]') || {}).value || '';

    var viewfinder     = document.getElementById('viewfinder-wrap');
    var checkedInEl    = document.getElementById('checked-in-count');
    var attendancePctEl= document.getElementById('attendance-pct');
    var scanList       = document.getElementById('scan-list');
    var noScansMsg     = document.getElementById('no-scans-msg');
    var manualInput    = document.getElementById('manual-input');
    var manualBtn      = document.getElementById('manual-btn');
    var toastContainer = document.getElementById('toast-container');

    var lastCode = '', lastCodeTime = 0;

    // ── QR processing ─────────────────────────────────────────────
    async function processQrCode(qrCode) {
        qrCode = qrCode.trim();
        if (!qrCode) return;
        var now = Date.now();
        if (qrCode === lastCode && now - lastCodeTime < 3000) return;
        lastCode = qrCode;
        lastCodeTime = now;

        try {
            var resp = await fetch(checkInUrl, {
                method: 'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                body: '__RequestVerificationToken=' + encodeURIComponent(token)
                    + '&eventId=' + encodeURIComponent(eventId)
                    + '&qrCode='  + encodeURIComponent(qrCode)
            });
            var data = await resp.json();

            if (data.success) {
                flashViewfinder('success');
                showToast('success', '\u2713 ' + data.holderName + ' \u2014 ' + data.ticketNumber);
                checkedInEl.textContent = data.checkedInCount;
                updateAttendance(data.checkedInCount);
                addToList(data.ticketNumber, data.holderName);
            } else {
                flashViewfinder('error');
                showToast('error', data.error || 'Check-in failed');
            }
        } catch {
            flashViewfinder('error');
            showToast('error', tryAgainText);
        }
    }

    // ── Viewfinder flash ──────────────────────────────────────────
    function flashViewfinder(type) {
        viewfinder.classList.remove('flash-success', 'flash-error');
        void viewfinder.offsetWidth;
        viewfinder.classList.add(type === 'success' ? 'flash-success' : 'flash-error');
        setTimeout(function () { viewfinder.classList.remove('flash-success', 'flash-error'); }, 1000);
    }

    // ── Toasts ────────────────────────────────────────────────────
    function showToast(type, message) {
        var el = document.createElement('div');
        el.className = 'toast-item toast-' + type;
        var icon = type === 'success' ? 'bi-check-circle-fill' : 'bi-x-circle-fill';
        el.innerHTML = '<i class="bi ' + icon + '"></i><span>' + escHtml(message) + '</span>';
        toastContainer.prepend(el);
        setTimeout(function () { el.remove(); }, 3000);
    }

    // ── Attendance % ──────────────────────────────────────────────
    function updateAttendance(checkedIn) {
        if (totalTickets === 0) { attendancePctEl.textContent = '0%'; return; }
        var pct = Math.round(checkedIn / totalTickets * 1000) / 10;
        attendancePctEl.textContent = pct + '%';
    }

    // ── Recent scans list ─────────────────────────────────────────
    function addToList(ticketNumber, holderName) {
        noScansMsg.classList.add('d-none');
        var time = new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', second: '2-digit' });
        var li = document.createElement('li');
        li.className = 'scan-item';
        li.innerHTML =
            '<i class="bi bi-check-circle-fill scan-check"></i>'
            + '<span class="scan-info">'
            +   '<span class="scan-ticket">' + escHtml(ticketNumber) + '</span>'
            +   '<span class="d-block scan-holder">' + escHtml(holderName) + '</span>'
            + '</span>'
            + '<span class="scan-time">' + time + '</span>';
        scanList.insertBefore(li, scanList.firstChild);
        var items = scanList.querySelectorAll('.scan-item');
        if (items.length > 10) items[items.length - 1].remove();
    }

    // ── HTML escape ───────────────────────────────────────────────
    function escHtml(str) {
        return String(str)
            .replace(/&/g, '&amp;').replace(/</g, '&lt;')
            .replace(/>/g, '&gt;').replace(/"/g, '&quot;');
    }

    // ── Camera (Html5Qrcode) ──────────────────────────────────────
    var html5Qrcode = null;
    var cameraIds = [];
    var currentCameraIndex = 0;
    var scanConfig = { fps: 10, qrbox: { width: 250, height: 250 } };

    async function startScanner() {
        try {
            html5Qrcode = new Html5Qrcode('camera-container');
            var devices = await Html5Qrcode.getCameras();
            if (devices && devices.length) {
                cameraIds = devices.map(function (d) { return d.id; });
                await html5Qrcode.start(
                    cameraIds[currentCameraIndex],
                    scanConfig,
                    async function (decodedText) { await processQrCode(decodedText); },
                    function () {}
                );
            } else {
                showToast('error', 'No cameras found. Use manual entry.');
            }
        } catch (err) {
            console.error('Scanner error:', err);
            showToast('error', 'Camera access denied or unavailable. Check browser permissions.');
        }
    }

    async function stopScanner() {
        if (html5Qrcode && html5Qrcode.isScanning) {
            try { await html5Qrcode.stop(); } catch (e) { console.warn('Stop error:', e); }
        }
    }

    startScanner();

    document.getElementById('switch-camera').addEventListener('click', async function () {
        if (cameraIds.length < 2) return;
        await stopScanner();
        currentCameraIndex = (currentCameraIndex + 1) % cameraIds.length;
        await html5Qrcode.start(
            cameraIds[currentCameraIndex],
            scanConfig,
            async function (decodedText) { await processQrCode(decodedText); },
            function () {}
        );
    });

    window.addEventListener('beforeunload', stopScanner);

    // ── Manual entry ──────────────────────────────────────────────
    manualBtn.addEventListener('click', async function () {
        var code = manualInput.value;
        if (!code.trim()) return;
        await processQrCode(code);
        manualInput.value = '';
        manualInput.focus();
    });

    manualInput.addEventListener('keydown', async function (e) {
        if (e.key === 'Enter') { e.preventDefault(); manualBtn.click(); }
    });

}());

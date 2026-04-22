// KazanlakEvents — Admin layout JavaScript

// ── Sidebar collapse (desktop) ────────────────────────────────────
(function () {
    var shell = document.getElementById('admin-shell');
    if (!shell) return;
    if (localStorage.getItem('sidebarCollapsed') === 'true') {
        shell.classList.add('sidebar-collapsed');
    }
    var btn = document.getElementById('sidebar-toggle-btn');
    if (btn) {
        btn.addEventListener('click', function () {
            shell.classList.toggle('sidebar-collapsed');
            localStorage.setItem('sidebarCollapsed', shell.classList.contains('sidebar-collapsed'));
        });
    }
}());

// ── Sidebar mobile toggle ─────────────────────────────────────────
(function () {
    var toggle  = document.getElementById('admin-sidebar-toggle');
    var sidebar = document.querySelector('.admin-sidebar');
    var overlay = document.getElementById('admin-sidebar-overlay');
    if (!toggle || !sidebar || !overlay) return;
    function openSidebar()  { sidebar.classList.add('open'); overlay.classList.add('open'); }
    function closeSidebar() { sidebar.classList.remove('open'); overlay.classList.remove('open'); }
    toggle.addEventListener('click', function () {
        sidebar.classList.contains('open') ? closeSidebar() : openSidebar();
    });
    overlay.addEventListener('click', closeSidebar);
}());

// ── Avatar dropdown ───────────────────────────────────────────────
(function () {
    var btn  = document.getElementById('admin-avatar-btn');
    var menu = document.getElementById('admin-avatar-menu');
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
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') {
            menu.classList.remove('open');
            btn.setAttribute('aria-expanded', 'false');
            btn.focus();
        }
    });
}());

// ── Notification dot polling ──────────────────────────────────────
(function () {
    var dot = document.getElementById('notif-dot');
    if (!dot) return;
    function update() {
        fetch('/Notification/GetUnreadCount')
            .then(function (r) { return r.json(); })
            .then(function (d) { dot.style.display = d.count > 0 ? 'block' : 'none'; })
            .catch(function () {});
    }
    update();
    setInterval(update, 30000);
}());

// ── Toast close button ────────────────────────────────────────────
(function () {
    var btn = document.querySelector('.ke-toast-close');
    if (btn) {
        btn.addEventListener('click', function () {
            var toast = btn.closest('.ke-toast');
            if (toast) toast.remove();
        });
    }
}());

// ── Toast auto-remove ─────────────────────────────────────────────
setTimeout(function () {
    var t = document.getElementById('toast-notification');
    if (t) t.remove();
}, 5000);

// ── Confirm modal cancel ──────────────────────────────────────────
(function () {
    var btn = document.querySelector('[data-confirm-cancel]');
    if (btn) {
        btn.addEventListener('click', function () {
            document.getElementById('ke-confirm-modal').style.display = 'none';
        });
    }
}());

// ── Confirm modal helper (global) ─────────────────────────────────
function keConfirm(title, text, actionBtnText, onConfirm) {
    document.getElementById('ke-confirm-title').textContent = title;
    document.getElementById('ke-confirm-text').textContent = text;
    var btn = document.getElementById('ke-confirm-action');
    btn.textContent = actionBtnText;
    btn.onclick = function () {
        document.getElementById('ke-confirm-modal').style.display = 'none';
        onConfirm();
    };
    document.getElementById('ke-confirm-modal').style.display = '';
}

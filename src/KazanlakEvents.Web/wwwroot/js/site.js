// KazanlakEvents — Global JavaScript

// ── Auto-dismiss Bootstrap alerts ────────────────────────────────
document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('.alert-dismissible').forEach(function (alert) {
        setTimeout(function () {
            var bsAlert = bootstrap.Alert.getOrCreateInstance(alert);
            bsAlert.close();
        }, 5000);
    });
});

// ── Nav behaviour (homepage: hide/show on hero scroll; standard: shadow on scroll) ──
(function () {
    var header = document.getElementById('ke-header');
    if (!header) return;
    if (header.classList.contains('nav-homepage')) {
        var hero = document.getElementById('hero-section');
        if (hero) {
            var observer = new IntersectionObserver(function (entries) {
                header.classList.toggle('nav-visible', !entries[0].isIntersecting);
            }, { threshold: 0.1 });
            observer.observe(hero);
        }
    } else {
        window.addEventListener('scroll', function () {
            header.classList.toggle('scrolled', window.scrollY > 10);
        }, { passive: true });
    }
}());

// ── Hamburger / mobile nav ────────────────────────────────────────
(function () {
    var hbtn = document.getElementById('ke-hamburger');
    var mnav = document.getElementById('ke-mobile-nav');
    if (!hbtn || !mnav) return;
    hbtn.addEventListener('click', function () {
        var open = mnav.classList.toggle('open');
        hbtn.setAttribute('aria-expanded', String(open));
    });
}());

// ── Avatar dropdown ───────────────────────────────────────────────
(function () {
    var btn  = document.getElementById('ke-avatar-btn');
    var menu = document.getElementById('ke-avatar-menu');
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

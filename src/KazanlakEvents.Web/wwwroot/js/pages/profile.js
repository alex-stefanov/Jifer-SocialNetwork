// KazanlakEvents — User Profile page JavaScript

(function () {

/* ── QR modal ────────────────────────────────────────────────────────── */
function openQr(imgUrl, title, sub) {
    document.getElementById('pf-qr-img').src               = imgUrl || '/images/qr-placeholder.png';
    document.getElementById('pf-qr-title').textContent     = title || '';
    document.getElementById('pf-qr-sub').textContent       = sub   || '';
    document.getElementById('pf-qr-modal').classList.add('open');
    document.body.style.overflow = 'hidden';
}
function closeQr() {
    document.getElementById('pf-qr-modal').classList.remove('open');
    document.body.style.overflow = '';
}

/* ── Transfer modal ──────────────────────────────────────────────────── */
function openTransfer(ticketId, eventTitle) {
    document.getElementById('pf-transfer-ticketid').value  = ticketId;
    document.getElementById('pf-transfer-sub').textContent = eventTitle || '';
    document.getElementById('pf-transfer-modal').classList.add('open');
    document.body.style.overflow = 'hidden';
}
function closeTransfer() {
    document.getElementById('pf-transfer-modal').classList.remove('open');
    document.body.style.overflow = '';
}

/* ── Past tickets toggle ─────────────────────────────────────────────── */
function togglePastTickets() {
    var wrap = document.getElementById('past-tickets-wrap');
    var btn  = document.getElementById('toggle-past-btn');
    var isVisible = wrap.classList.toggle('visible');
    btn.textContent = isVisible ? btn.dataset.hideText : btn.dataset.showText;
}

/* ── Followers / Following modal ─────────────────────────────────────── */
function openUsersModal(type, userId) {
    var modal = document.getElementById('pf-users-modal');
    var title = document.getElementById('pf-users-title');
    var list  = document.getElementById('pf-users-list');

    title.textContent = type === 'followers' ? modal.dataset.followersLabel : modal.dataset.followingLabel;
    list.innerHTML    = '<div class="pf-users-loading"><div class="spinner-border spinner-border-sm text-secondary" role="status"></div></div>';
    modal.classList.add('open');
    document.body.style.overflow = 'hidden';

    fetch('/Profile/' + (type === 'followers' ? 'GetFollowers' : 'GetFollowing') + '?userId=' + userId)
        .then(function(r) { return r.ok ? r.json() : Promise.reject(r.status); })
        .then(function(users) { renderUsersList(list, users); })
        .catch(function() {
            list.innerHTML = '<div class="pf-users-empty">' + modal.dataset.errorText + '</div>';
        });
}
function closeUsersModal() {
    document.getElementById('pf-users-modal').classList.remove('open');
    document.body.style.overflow = '';
}
function renderUsersList(container, users) {
    var modal = document.getElementById('pf-users-modal');
    if (!users || users.length === 0) {
        container.innerHTML = '<div class="pf-users-empty">' + modal.dataset.emptyText + '</div>';
        return;
    }
    container.innerHTML = users.map(function(u) {
        var initials = u.fullName
            ? u.fullName.split(' ').slice(0,2).map(function(p){ return p[0]; }).join('').toUpperCase()
            : (u.userName ? u.userName[0].toUpperCase() : '?');
        var avatarHtml = u.avatarUrl
            ? '<img src="' + u.avatarUrl + '" alt="' + u.fullName + '" />'
            : initials;
        var profileUrl = '/Profile/Index/' + u.id;
        return '<a href="' + profileUrl + '" class="pf-users-item" style="text-decoration:none;">'
            + '<div class="pf-user-avatar-sm" style="width:28px;height:28px;font-size:11px;">' + avatarHtml + '</div>'
            + '<div class="pf-user-names" style="flex:1;">'
            +   '<div class="pf-user-fullname">' + (u.fullName || '') + '</div>'
            +   '<div class="pf-user-handle">@' + (u.userName || '') + '</div>'
            + '</div>'
            + '</a>';
    }).join('');
}

document.addEventListener('keydown', function(e) {
    if (e.key === 'Escape') { closeQr(); closeTransfer(); closeUsersModal(); }
});

/* ── Auto-open QR if URL has ?highlightTicket={id} ──────────────────── */
(function() {
    var params = new URLSearchParams(window.location.search);
    var highlightId = params.get('highlightTicket');
    if (highlightId) {
        var btn = document.querySelector('.show-qr-btn[data-ticket-id="' + highlightId + '"]');
        if (btn) btn.click();
    }
})();

window.openQr           = openQr;
window.closeQr          = closeQr;
window.openTransfer     = openTransfer;
window.closeTransfer    = closeTransfer;
window.togglePastTickets = togglePastTickets;
window.openUsersModal   = openUsersModal;
window.closeUsersModal  = closeUsersModal;

}());

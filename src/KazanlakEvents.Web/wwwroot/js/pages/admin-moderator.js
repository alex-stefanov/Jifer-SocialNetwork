// KazanlakEvents — Admin Moderator Dashboard page JavaScript

(function () {

function openRejectModal(id, title) {
    document.getElementById('reject-event-id').value = id;
    document.getElementById('reject-event-title').textContent = title;
    document.getElementById('reject-event-modal').style.display = '';
}

function openRejectBlogModal(id, title) {
    document.getElementById('reject-blog-id').value = id;
    document.getElementById('reject-blog-title').textContent = title;
    document.getElementById('reject-blog-modal').style.display = '';
}

window.openRejectModal     = openRejectModal;
window.openRejectBlogModal = openRejectBlogModal;

}());

// KazanlakEvents — Volunteer Event Tasks (public view) JavaScript

(function () {

function toggleTask(header) {
    header.closest('.task-card').classList.toggle('open');
}

window.toggleTask = toggleTask;

}());

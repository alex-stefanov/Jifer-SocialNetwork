// KazanlakEvents — Volunteer Manage Tasks page JavaScript

(function () {

function toggleTask(header) {
    header.closest('.task-card').classList.toggle('open');
}

function openRemoveModal(signupId) {
    document.getElementById('remove-signup-id').value = signupId;
    document.getElementById('remove-volunteer-modal').classList.add('show');
}

// Combine date + time parts into datetime-local hidden fields before submit
document.querySelectorAll('.add-shift-form form').forEach(function(form) {
    form.addEventListener('submit', function() {
        var date  = form.querySelector('[name="shiftDate"]').value;
        var start = form.querySelector('[name="startTimePart"]').value;
        var end   = form.querySelector('[name="endTimePart"]').value;
        if (!date || !start || !end) return;
        form.querySelector('[name="startTime"]').value = date + 'T' + start;
        form.querySelector('[name="endTime"]').value   = date + 'T' + end;
    });
});

window.toggleTask       = toggleTask;
window.openRemoveModal  = openRemoveModal;

}());

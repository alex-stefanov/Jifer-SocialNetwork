// KazanlakEvents — Admin Dashboard page JavaScript

(function () {

var canvas1 = document.getElementById('chartEventsPerMonth');
if (!canvas1) return;

var monthLabels = JSON.parse(canvas1.dataset.labels  || '[]');
var eventsData  = JSON.parse(canvas1.dataset.values  || '[]');
var eventLabel  = canvas1.dataset.eventLabel  || '';
var regLabel    = canvas1.dataset.regLabel    || '';

// ── Events per month: bar (#6366f1) ──────────────────────────────────────
new Chart(canvas1, {
    type: 'bar',
    data: {
        labels: monthLabels,
        datasets: [{
            label: eventLabel,
            data: eventsData,
            backgroundColor: '#6366f1',
            borderRadius: 4,
            borderSkipped: false
        }]
    },
    options: {
        responsive: true,
        maintainAspectRatio: true,
        plugins: { legend: { display: false } },
        scales: { y: { beginAtZero: true, ticks: { stepSize: 1 } } }
    }
});

// ── Registrations trend: line with gradient ───────────────────────────────
(function () {
    var ctx = document.getElementById('chartRegistrations').getContext('2d');
    var gradient = ctx.createLinearGradient(0, 0, 0, 220);
    gradient.addColorStop(0, 'rgba(99,102,241,0.35)');
    gradient.addColorStop(1, 'rgba(99,102,241,0)');

    new Chart(ctx, {
        type: 'line',
        data: {
            labels: monthLabels,
            datasets: [{
                label: regLabel,
                data: eventsData,
                borderColor: '#6366f1',
                backgroundColor: gradient,
                borderWidth: 2,
                fill: true,
                tension: 0.35,
                pointRadius: 3,
                pointHoverRadius: 5
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            plugins: { legend: { display: false } },
            scales: { y: { beginAtZero: true } }
        }
    });
})();

}());

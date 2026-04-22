// KazanlakEvents — Calendar page JavaScript

document.addEventListener('DOMContentLoaded', function () {
    var calendarEl = document.getElementById('event-calendar');
    if (!calendarEl) return;

    var locale     = calendarEl.dataset.locale     || 'en';
    var allDayText = calendarEl.dataset.allDay      || 'all-day';
    var btnToday   = calendarEl.dataset.btnToday    || 'Today';
    var btnMonth   = calendarEl.dataset.btnMonth    || 'Month';
    var btnWeek    = calendarEl.dataset.btnWeek     || 'Week';
    var btnList    = calendarEl.dataset.btnList     || 'List';
    var noEvents   = calendarEl.dataset.noEvents    || 'No events';
    var moreSuffix = calendarEl.dataset.moreSuffix  || 'more';

    var calendar = new FullCalendar.Calendar(calendarEl, {
        initialView: 'dayGridMonth',
        locale: locale,
        firstDay: 1,
        headerToolbar: {
            left: 'prev,next today',
            center: 'title',
            right: 'dayGridMonth,timeGridWeek,listWeek'
        },
        allDayText: allDayText,
        buttonText: {
            today: btnToday,
            month: btnMonth,
            week:  btnWeek,
            list:  btnList
        },
        eventTimeFormat: { hour: '2-digit', minute: '2-digit', hour12: false },
        displayEventTime: true,
        noEventsText: noEvents,
        events: { url: '/Event/CalendarEvents', method: 'GET' },
        eventDidMount: function (info) {
            info.el.style.cursor = 'pointer';
            var tooltip = document.createElement('div');
            tooltip.className = 'ke-cal-tooltip';
            tooltip.textContent = info.event.title;
            if (info.event.start) {
                tooltip.textContent += ' \u2014 ' + info.event.start.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
            }
            info.el.addEventListener('mouseenter', function (e) {
                document.body.appendChild(tooltip);
                tooltip.style.left = e.pageX + 10 + 'px';
                tooltip.style.top  = e.pageY - 30 + 'px';
                tooltip.style.display = 'block';
            });
            info.el.addEventListener('mouseleave', function () { tooltip.remove(); });
            info.el.addEventListener('mousemove', function (e) {
                tooltip.style.left = e.pageX + 10 + 'px';
                tooltip.style.top  = e.pageY - 30 + 'px';
            });
        },
        eventClick: function (info) {
            info.jsEvent.preventDefault();
            if (info.event.url) window.location.href = info.event.url;
        },
        height: 'auto',
        dayMaxEvents: 3,
        moreLinkText: function (n) { return '+' + n + ' ' + moreSuffix; },
        eventDisplay: 'block'
    });
    calendar.render();

    var datePicker = document.getElementById('calendar-date-picker');
    if (datePicker) {
        datePicker.addEventListener('change', function (e) {
            calendar.gotoDate(e.target.value);
        });
    }
});

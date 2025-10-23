class AdminCalendar {
    constructor() {
        this.currentDate = new Date();
        this.currentYear = this.currentDate.getFullYear();
        this.currentMonth = this.currentDate.getMonth();
        this.today = new Date();
        this.events = [];
        this.selectedDate = null;
        this.isEditMode = false;
        this.currentEventId = null;
        this.init();
    }

    init() {
        this.loadSampleEvents();
        this.bindEvents();
        this.renderCalendar();
    }

    bindEvents() {
        const prev = document.getElementById('prevMonth');
        const next = document.getElementById('nextMonth');
        const form = document.getElementById('eventForm');

        prev.addEventListener('click', () => {
            if (--this.currentMonth < 0) { this.currentMonth = 11; this.currentYear--; }
            this.renderCalendar();
        });

        next.addEventListener('click', () => {
            if (++this.currentMonth > 11) { this.currentMonth = 0; this.currentYear++; }
            this.renderCalendar();
        });

        form.addEventListener('submit', e => { e.preventDefault(); this.saveEvent(); });
        document.getElementById('editEventBtn').addEventListener('click', () => {
            document.getElementById('eventDetailsModal').querySelector('[data-bs-dismiss="modal"]').click();
            this.openEventModal(true);
        });
        document.getElementById('deleteEventBtn').addEventListener('click', () => this.deleteEvent());
        document.getElementById('eventModal').addEventListener('hidden.bs.modal', () => this.resetEventModal());
    }

    renderCalendar() {
        const monthNames = ['January','February','March','April','May','June','July','August','September','October','November','December'];
        document.getElementById('currentMonth').textContent = `${monthNames[this.currentMonth]} ${this.currentYear}`;
        const daysGrid = document.getElementById('calendarDays');
        daysGrid.innerHTML = '';

        const firstDay = new Date(this.currentYear, this.currentMonth, 1);
        const daysInMonth = new Date(this.currentYear, this.currentMonth + 1, 0).getDate();
        const prevMonthLastDay = new Date(this.currentYear, this.currentMonth, 0).getDate();

        for (let i = firstDay.getDay() - 1; i >= 0; i--)
            daysGrid.appendChild(this.createDayElement(prevMonthLastDay - i, true, new Date(this.currentYear, this.currentMonth - 1, prevMonthLastDay - i)));

        for (let d = 1; d <= daysInMonth; d++)
            daysGrid.appendChild(this.createDayElement(d, false, new Date(this.currentYear, this.currentMonth, d)));

        const remaining = 42 - daysGrid.children.length;
        for (let d = 1; d <= remaining; d++)
            daysGrid.appendChild(this.createDayElement(d, true, new Date(this.currentYear, this.currentMonth + 1, d)));
    }

    createDayElement(day, other, date) {
        const div = document.createElement('div');
        div.className = `calendar-day${other ? ' other-month' : ''}${this.isSameDay(date, this.today) ? ' today' : ''}`;
        div.innerHTML = `<div class="day-number">${day}</div><div class="event-items"></div>`;

        const events = this.getEventsForDate(date);
        const container = div.querySelector('.event-items');
        events.slice(0, 4).forEach(ev => {
            const eDiv = document.createElement('div');
            eDiv.className = `event-item ${ev.type}`;
            eDiv.textContent = ev.title;
            eDiv.addEventListener('click', e => { e.stopPropagation(); this.showEventDetails(ev); });
            container.appendChild(eDiv);
        });
        if (events.length > 4) {
            const more = document.createElement('div');
            more.className = 'event-item more-events';
            more.textContent = `+${events.length - 4} more`;
            more.addEventListener('click', e => { e.stopPropagation(); this.showDayEvents(date, events); });
            container.appendChild(more);
        }
        div.addEventListener('click', e => this.selectDate(date, e.currentTarget));
        return div;
    }

    selectDate(date, el) {
        document.querySelectorAll('.calendar-day.selected').forEach(d => d.classList.remove('selected'));
        el.classList.add('selected');
        this.selectedDate = date;
        this.openEventModal(false, date);
    }

    openEventModal(isEdit = false, date = null) {
        this.isEditMode = isEdit;
        const modal = new bootstrap.Modal(document.getElementById('eventModal'));
        const title = document.getElementById('modalTitle');
        const saveBtn = document.getElementById('saveButtonText');
        const delBtn = document.getElementById('deleteEventBtn');

        if (isEdit && this.currentEventId) {
            const ev = this.events.find(e => e.id === this.currentEventId);
            if (ev) this.populateEventForm(ev);
            title.textContent = 'Edit Event';
            saveBtn.textContent = 'Update Event';
            delBtn.classList.remove('d-none');
        } else {
            title.textContent = 'Add Event';
            saveBtn.textContent = 'Save Event';
            delBtn.classList.add('d-none');
            if (date) document.getElementById('eventDate').value = this.formatDateForInput(date);
        }
        modal.show();
    }

    populateEventForm(e) {
        Object.entries({
            eventId: e.id, eventTitle: e.title, eventType: e.type, eventDate: e.date,
            eventAudience: e.audience, startTime: e.startTime || '', endTime: e.endTime || '',
            eventDescription: e.description || '', eventLocation: e.location || ''
        }).forEach(([id, val]) => document.getElementById(id).value = val);
    }

    saveEvent() {
        const f = new FormData(document.getElementById('eventForm'));
        const ev = Object.fromEntries(f.entries());
        ev.id = this.isEditMode ? this.currentEventId : this.generateEventId();
        if (this.isEditMode)
            this.events = this.events.map(e => e.id === this.currentEventId ? ev : e);
        else this.events.push(ev);
        document.getElementById('eventModal').querySelector('[data-bs-dismiss="modal"]').click();
        this.renderCalendar();
        this.showAlert('success', this.isEditMode ? 'Event updated!' : 'Event added!');
    }

    deleteEvent() {
        if (this.currentEventId) {
            this.events = this.events.filter(e => e.id !== this.currentEventId);
            document.getElementById('eventModal').querySelector('[data-bs-dismiss="modal"]').click();
            this.renderCalendar();
            this.showAlert('success', 'Event deleted!');
        }
    }

    showEventDetails(e) {
        this.currentEventId = e.id;
        document.getElementById('detailTitle').textContent = e.title;
        const t = document.getElementById('detailType');
        t.textContent = this.capitalizeFirst(e.type);
        t.className = `event-badge ${e.type}`;
        document.getElementById('detailDate').textContent = this.formatDateForDisplay(new Date(e.date));
        document.getElementById('detailTime').textContent = e.startTime && e.endTime ? `${e.startTime} - ${e.endTime}` : 'All Day';
        document.getElementById('detailAudience').textContent = this.capitalizeFirst(e.audience);
        document.getElementById('detailLocation').textContent = e.location || 'Not specified';
        document.getElementById('detailDescription').textContent = e.description || 'No description';
        new bootstrap.Modal(document.getElementById('eventDetailsModal')).show();
    }

    resetEventModal() {
        document.getElementById('eventForm').reset();
        this.isEditMode = false;
        this.currentEventId = null;
    }

    getEventsForDate(date) {
        return this.events.filter(e => e.date === this.formatDateForInput(date));
    }

    generateEventId() {
        return 'event_' + Date.now();
    }

    formatDateForInput(date) {
        return `${date.getFullYear()}-${String(date.getMonth()+1).padStart(2,'0')}-${String(date.getDate()).padStart(2,'0')}`;
    }

    formatDateForDisplay(date) {
        return date.toLocaleDateString('en-US',{weekday:'long',year:'numeric',month:'long',day:'numeric'});
    }

    isSameDay(a,b){return a.getDate()==b.getDate()&&a.getMonth()==b.getMonth()&&a.getFullYear()==b.getFullYear();}
    capitalizeFirst(s){return s.charAt(0).toUpperCase()+s.slice(1).replace('-', ' ');}

    showAlert(type,msg){
        const id='alert-'+Date.now();
        document.body.insertAdjacentHTML('beforeend',`
        <div id="${id}" class="alert alert-${type} alert-dismissible fade show" style="position:fixed;top:80px;right:20px;z-index:9999;">
            <i class="fa-solid fa-${type==='success'?'check-circle':'circle-exclamation'} me-1"></i>${msg}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        </div>`);
        setTimeout(()=>document.getElementById(id)?.remove(),5000);
    }

    loadSampleEvents() {
        this.events = [
            { id:'oct1', title:'October Academic Planning', type:'academic', date:'2025-10-03', audience:'teachers', startTime:'09:00', endTime:'17:00', description:'Planning session', location:'Conference Room' },
            { id:'oct2', title:'Student Orientation', type:'academic', date:'2025-10-23', audience:'students', startTime:'08:00', endTime:'12:00', description:'Orientation Program', location:'Main Auditorium' },
            { id:'oct3', title:'Halloween Celebration', type:'school-events', date:'2025-10-31', audience:'all', startTime:'14:00', endTime:'18:00', description:'Costume Party', location:'Campus Grounds' },
            { id:'holiday3', title:'Christmas Day', type:'holidays', date:'2025-12-25', audience:'all', startTime:'00:00', endTime:'23:59', description:'Christmas Holiday', location:'No Classes' },
        ];
    }
}

document.addEventListener('DOMContentLoaded', () => new AdminCalendar());

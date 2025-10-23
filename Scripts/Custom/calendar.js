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
        this.loadEventsFromServer();
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
            if (date) {
                document.getElementById('startDate').value = this.formatDateForInput(date);
                document.getElementById('endDate').value = this.formatDateForInput(date);
            }
        }
        modal.show();
    }

    populateEventForm(e) {
        document.getElementById('eventId').value = e.id || '';
        document.getElementById('eventTitle').value = e.title || '';
        document.getElementById('eventType').value = e.type || '';
        document.getElementById('startDate').value = e.startDate || '';
        document.getElementById('endDate').value = e.endDate || '';
        document.getElementById('eventDescription').value = e.description || '';
    }

    async saveEvent() {
        const f = new FormData(document.getElementById('eventForm'));
        const ev = Object.fromEntries(f.entries());
        ev.Id = this.isEditMode ? this.currentEventId : 0;

        try {
            const response = await fetch('/Admin/SaveEvent', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    Id: ev.Id,
                    Title: ev.eventTitle,
                    Type: ev.eventType,
                    StartDate: ev.startDate,
                    EndDate: ev.endDate,
                    Description: ev.eventDescription
                })
            });

            const result = await response.json();

            if (result.success) {
                document.getElementById('eventModal').querySelector('[data-bs-dismiss="modal"]').click();
                this.showAlert('success', this.isEditMode ? 'Event updated!' : 'Event added!');
                await this.loadEventsFromServer();
            } else {
                this.showAlert('danger', 'Error saving event.');
            }
        } catch (err) {
            console.error('Save event failed:', err);
            this.showAlert('danger', 'Error saving event.');
        }
    }



    async deleteEvent() {
        if (!this.currentEventId) return;

        try {
            const response = await fetch('/Admin/DeleteEvent', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ id: this.currentEventId })
            });

            const result = await response.json();

            if (result.success) {
                document.getElementById('eventModal').querySelector('[data-bs-dismiss="modal"]').click();
                this.showAlert('success', 'Event deleted!');
                await this.loadEventsFromServer();
            } else {
                this.showAlert('danger', 'Error deleting event.');
            }
        } catch (err) {
            console.error('Delete failed:', err);
            this.showAlert('danger', 'Error deleting event.');
        }
    }


    showEventDetails(e) {
        this.currentEventId = e.id;
        document.getElementById('detailTitle').textContent = e.title;
        const t = document.getElementById('detailType');
        t.textContent = this.capitalizeFirst(e.type);
        t.className = `event-badge ${e.type}`;
        document.getElementById('detailDate').textContent =
            e.startDate && e.endDate
                ? `${this.formatDateForDisplay(new Date(e.startDate))} - ${this.formatDateForDisplay(new Date(e.endDate))}`
                : this.formatDateForDisplay(new Date(e.date));
        document.getElementById('detailDescription').textContent = e.description || 'No description';
        new bootstrap.Modal(document.getElementById('eventDetailsModal')).show();
    }

    resetEventModal() {
        document.getElementById('eventForm').reset();
        this.isEditMode = false;
        this.currentEventId = null;
    }

    getEventsForDate(date) {
        const target = this.formatDateForInput(date);
        return this.events.filter(e => {
            if (e.startDate && e.endDate) {
                return target >= e.startDate && target <= e.endDate;
            }
            return e.date === target;
        });
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
       <div id="${id}" 
             class="alert alert-${type} alert-dismissible fade show d-flex align-items-center justify-content-start" 
             style="position: fixed; bottom: 30px; left: 50%; transform: translateX(-50%); z-index: 9999; min-width: 320px; text-align: center; font-size: 16px;">
            <i class="fa-solid fa-${type === 'success' ? 'check-circle' : 'circle-exclamation'} me-3"></i>
            <span>${msg}</span>
            <button type="button" class="btn-close ms-2" data-bs-dismiss="alert"></button>
        </div>

                `);
        setTimeout(()=>document.getElementById(id)?.remove(),5000);
    }

    async loadEventsFromServer() {
        try {
            const response = await fetch('/Admin/GetEvents');
            const data = await response.json();

            this.events = data.map(e => ({
                id: e.Id,
                title: e.Title,
                type: e.Type,
                startDate: e.StartDate ? e.StartDate.split('T')[0] : null,
                endDate: e.EndDate ? e.EndDate.split('T')[0] : null,
                description: e.Description
            }));

            this.renderCalendar();
        } catch (err) {
            console.error('Error loading events:', err);
            this.showAlert('danger', 'Failed to load events.');
        }
    }

}

document.addEventListener('DOMContentLoaded', () => new AdminCalendar());

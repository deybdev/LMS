class SchoolCalendar {
    constructor(role = 'viewer') {
        this.role = role; // 'admin', 'teacher', 'student'
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

        prev?.addEventListener('click', () => {
            if (--this.currentMonth < 0) { this.currentMonth = 11; this.currentYear--; }
            this.renderCalendar();
        });

        next?.addEventListener('click', () => {
            if (++this.currentMonth > 11) { this.currentMonth = 0; this.currentYear++; }
            this.renderCalendar();
        });

        // Only bind add/edit/delete if admin
        if (this.role === 'admin') {
            const form = document.getElementById('eventForm');
            form?.addEventListener('submit', e => { e.preventDefault(); this.saveEvent(); });
            
            document.getElementById('editEventBtn')?.addEventListener('click', () => {
                const modal = bootstrap.Modal.getInstance(document.getElementById('eventDetailsModal'));
                modal?.hide();
                setTimeout(() => this.openEventModal(true), 300);
            });
            
            document.getElementById('deleteEventBtn')?.addEventListener('click', () => this.deleteEvent());
            document.getElementById('eventModal')?.addEventListener('hidden.bs.modal', () => this.resetEventModal());
        }
    }

    renderCalendar() {
        const monthNames = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'];
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

        // Only allow date selection for admin
        if (this.role === 'admin') {
            div.addEventListener('click', e => this.selectDate(date, e.currentTarget));
        }

        return div;
    }

    selectDate(date, el) {
        if (this.role !== 'admin') return;
        document.querySelectorAll('.calendar-day.selected').forEach(d => d.classList.remove('selected'));
        el.classList.add('selected');
        this.selectedDate = date;
        this.openEventModal(false, date);
    }

    openEventModal(isEdit = false, date = null) {
        if (this.role !== 'admin') return;
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
            this.resetEventForm();
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

    populateEventForm(event) {
        document.getElementById('eventId').value = event.id || '';
        document.getElementById('eventTitle').value = event.title || '';
        document.getElementById('eventType').value = event.type || '';
        document.getElementById('startDate').value = event.startDate || '';
        document.getElementById('endDate').value = event.endDate || '';
        document.getElementById('eventDescription').value = event.description || '';
    }

    resetEventForm() {
        document.getElementById('eventForm').reset();
        document.getElementById('eventId').value = '';
        this.currentEventId = null;
    }

    resetEventModal() {
        this.resetEventForm();
        this.isEditMode = false;
        this.currentEventId = null;
    }

    async saveEvent() {
        if (this.role !== 'admin') return;

        const formData = new FormData(document.getElementById('eventForm'));
        const eventData = {
            Id: parseInt(formData.get('eventId')) || 0,
            Title: formData.get('eventTitle'),
            Type: formData.get('eventType'),
            StartDate: formData.get('startDate'),
            EndDate: formData.get('endDate'),
            Description: formData.get('eventDescription') || ''
        };

        // Validation
        if (!eventData.Title || !eventData.Type || !eventData.StartDate || !eventData.EndDate) {
            this.showNotification('Please fill in all required fields.', 'error');
            return;
        }

        if (new Date(eventData.StartDate) > new Date(eventData.EndDate)) {
            this.showNotification('End date cannot be earlier than start date.', 'error');
            return;
        }

        try {
            const response = await fetch('/Admin/SaveEvent', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
                },
                body: JSON.stringify(eventData)
            });

            const result = await response.json();
            
            if (result.success) {
                this.showNotification(result.message || 'Event saved successfully!', 'success');
                bootstrap.Modal.getInstance(document.getElementById('eventModal')).hide();
                await this.loadEventsFromServer();
            } else {
                this.showNotification(result.message || 'Failed to save event.', 'error');
            }
        } catch (error) {
            console.error('Save event error:', error);
            this.showNotification('An error occurred while saving the event.', 'error');
        }
    }

    async deleteEvent() {
        if (this.role !== 'admin' || !this.currentEventId) return;

        if (!confirm('Are you sure you want to delete this event? This action cannot be undone.')) {
            return;
        }

        try {
            const response = await fetch('/Admin/DeleteEvent', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
                },
                body: JSON.stringify({ id: this.currentEventId })
            });

            const result = await response.json();
            
            if (result.success) {
                this.showNotification(result.message || 'Event deleted successfully!', 'success');
                bootstrap.Modal.getInstance(document.getElementById('eventModal')).hide();
                bootstrap.Modal.getInstance(document.getElementById('eventDetailsModal'))?.hide();
                await this.loadEventsFromServer();
            } else {
                this.showNotification(result.message || 'Failed to delete event.', 'error');
            }
        } catch (error) {
            console.error('Delete event error:', error);
            this.showNotification('An error occurred while deleting the event.', 'error');
        }
    }

    showEventDetails(e) {
        this.currentEventId = e.id;
        document.getElementById('detailTitle').textContent = e.title;
        const t = document.getElementById('detailType');
        t.textContent = this.capitalizeFirst(e.type);
        t.className = `event-badge ${e.type}`;
        
        const startDate = new Date(e.startDate);
        const endDate = new Date(e.endDate);
        
        if (e.startDate === e.endDate) {
            document.getElementById('detailDate').textContent = this.formatDateForDisplay(startDate);
        } else {
            document.getElementById('detailDate').textContent = 
                `${this.formatDateForDisplay(startDate)} - ${this.formatDateForDisplay(endDate)}`;
        }
        
        document.getElementById('detailDescription').textContent = e.description || 'No description provided.';
        
        // Hide edit button for non-admin users
        const editBtn = document.getElementById('editEventBtn');
        if (editBtn) {
            if (this.role === 'admin') {
                editBtn.style.display = 'inline-block';
            } else {
                editBtn.style.display = 'none';
            }
        }
        
        new bootstrap.Modal(document.getElementById('eventDetailsModal')).show();
    }

    showDayEvents(date, events) {
        // Create a simple modal to show all events for a day
        const dayName = this.formatDateForDisplay(date);
        let eventsHtml = `<h6>Events for ${dayName}</h6>`;
        
        events.forEach(event => {
            eventsHtml += `
                <div class="day-event-item" onclick="calendar.showEventDetails(${JSON.stringify(event).replace(/"/g, '&quot;')})">
                    <span class="event-badge ${event.type}">${this.capitalizeFirst(event.type)}</span>
                    <strong>${event.title}</strong>
                    ${event.description ? `<br><small>${event.description}</small>` : ''}
                </div>
            `;
        });
        
        // You can implement a custom modal or reuse existing one
        this.showNotification(eventsHtml, 'info');
    }

    async loadEventsFromServer() {
        try {
            const response = await fetch('/Admin/GetEvents');
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            
            const data = await response.json();
            
            if (data.error) {
                throw new Error(data.message || 'Failed to load events');
            }
            
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
            this.showNotification('Failed to load events. Please refresh the page.', 'error');
        }
    }

    getEventsForDate(date) {
        const target = this.formatDateForInput(date);
        return this.events.filter(e => {
            if (e.startDate && e.endDate) {
                return target >= e.startDate && target <= e.endDate;
            }
            return false;
        });
    }

    formatDateForInput(date) {
        return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
    }

    formatDateForDisplay(date) {
        return date.toLocaleDateString('en-US', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' });
    }

    isSameDay(a, b) {
        return a.getDate() === b.getDate() && a.getMonth() === b.getMonth() && a.getFullYear() === b.getFullYear();
    }

    capitalizeFirst(s) {
        return s.charAt(0).toUpperCase() + s.slice(1).replace('-', ' ');
    }

    showNotification(message, type = 'info') {
        // Create a simple notification system
        const notification = document.createElement('div');
        notification.className = `notification ${type}`;
        notification.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            background: ${type === 'success' ? '#28a745' : type === 'error' ? '#dc3545' : '#17a2b8'};
            color: white;
            padding: 12px 20px;
            border-radius: 8px;
            z-index: 9999;
            box-shadow: 0 4px 15px rgba(0,0,0,0.2);
            max-width: 400px;
        `;
        notification.innerHTML = message;
        
        document.body.appendChild(notification);
        
        setTimeout(() => {
            notification.style.opacity = '0';
            setTimeout(() => notification.remove(), 300);
        }, type === 'error' ? 5000 : 3000);
    }
}

// Global reference for event callbacks
let calendar;

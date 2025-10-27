// Teacher Course Management JavaScript
document.addEventListener('DOMContentLoaded', function() {
    initializeCoursePage();
});

function initializeCoursePage() {
    // Initialize event listeners
    bindFilterEvents();
    bindSearchEvents();
    bindFormEvents();
    
    // Add animation classes to course cards
    animateCards();
}

// Filter and Search Functionality
function bindFilterEvents() {
    const semesterFilter = document.getElementById('semesterFilter');
    const statusFilter = document.getElementById('statusFilter');
    
    semesterFilter?.addEventListener('change', filterCourses);
    statusFilter?.addEventListener('change', filterCourses);
}

function bindSearchEvents() {
    const searchInput = document.getElementById('courseSearch');
    
    searchInput?.addEventListener('input', debounce(filterCourses, 300));
}

function filterCourses() {
    const semesterFilter = document.getElementById('semesterFilter')?.value.toLowerCase();
    const statusFilter = document.getElementById('statusFilter')?.value.toLowerCase();
    const searchTerm = document.getElementById('courseSearch')?.value.toLowerCase();
    
    const courseCards = document.querySelectorAll('.course-card');
    let visibleCount = 0;
    
    courseCards.forEach(card => {
        const courseTitle = card.querySelector('.course-title')?.textContent.toLowerCase() || '';
        const courseCode = card.querySelector('.course-code')?.textContent.toLowerCase() || '';
        const courseStatus = card.classList.contains('ongoing') ? 'ongoing' : 
                           card.classList.contains('completed') ? 'completed' : 
                           card.classList.contains('upcoming') ? 'upcoming' : '';
        
        // Check filters
        const matchesSemester = !semesterFilter || true; // Add semester logic when data is available
        const matchesStatus = !statusFilter || courseStatus === statusFilter;
        const matchesSearch = !searchTerm || 
                             courseTitle.includes(searchTerm) || 
                             courseCode.includes(searchTerm);
        
        if (matchesSemester && matchesStatus && matchesSearch) {
            card.style.display = 'flex';
            card.classList.add('fade-in');
            visibleCount++;
        } else {
            card.style.display = 'none';
            card.classList.remove('fade-in');
        }
    });
    
    // Show/hide empty state
    toggleEmptyState(visibleCount === 0);
}

function toggleEmptyState(show) {
    let emptyState = document.querySelector('.empty-state');
    
    if (show && !emptyState) {
        emptyState = createEmptyState();
        document.querySelector('.courses-grid').appendChild(emptyState);
    } else if (!show && emptyState) {
        emptyState.remove();
    }
}

function createEmptyState() {
    const emptyDiv = document.createElement('div');
    emptyDiv.className = 'empty-state';
    emptyDiv.style.gridColumn = '1 / -1';
    emptyDiv.innerHTML = `
        <i class="fas fa-search"></i>
        <h3>No courses found</h3>
        <p>Try adjusting your search criteria or filters.</p>
    `;
    return emptyDiv;
}

// Animation Functions
function animateCards() {
    const cards = document.querySelectorAll('.course-card');
    cards.forEach((card, index) => {
        card.style.animationDelay = `${index * 0.1}s`;
        card.classList.add('slide-up');
    });
}

// Modal Functions
function openCreateClassModal() {
    const modal = new bootstrap.Modal(document.getElementById('createClassModal'));
    modal.show();
}

function bindFormEvents() {
    const form = document.getElementById('createClassForm');
    form?.addEventListener('submit', handleCreateClass);
}

async function handleCreateClass(event) {
    event.preventDefault();
    
    const formData = new FormData(event.target);
    const classData = {
        className: formData.get('className'),
        courseCode: formData.get('courseCode'),
        semester: formData.get('semester'),
        academicYear: formData.get('academicYear'),
        startDate: formData.get('startDate'),
        endDate: formData.get('endDate'),
        description: formData.get('courseDescription')
    };
    
    // Validation
    if (!validateClassData(classData)) {
        return;
    }
    
    // Show loading state
    const submitBtn = event.target.querySelector('button[type="submit"]');
    const originalText = submitBtn.innerHTML;
    submitBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Creating...';
    submitBtn.disabled = true;
    
    try {
        // Simulate API call - replace with actual endpoint
        await new Promise(resolve => setTimeout(resolve, 1500));
        
        // Success - add new card to grid
        addNewCourseCard(classData);
        
        // Close modal and show success message
        bootstrap.Modal.getInstance(document.getElementById('createClassModal')).hide();
        showNotification('Class created successfully!', 'success');
        
        // Reset form
        event.target.reset();
        
    } catch (error) {
        console.error('Error creating class:', error);
        showNotification('Failed to create class. Please try again.', 'error');
    } finally {
        // Reset button
        submitBtn.innerHTML = originalText;
        submitBtn.disabled = false;
    }
}

function validateClassData(data) {
    if (!data.className || !data.courseCode || !data.semester || !data.startDate || !data.endDate) {
        showNotification('Please fill in all required fields.', 'error');
        return false;
    }
    
    if (new Date(data.startDate) >= new Date(data.endDate)) {
        showNotification('End date must be after start date.', 'error');
        return false;
    }
    
    return true;
}

function addNewCourseCard(classData) {
    const coursesGrid = document.querySelector('.courses-grid');
    const newCard = createCourseCard(classData);
    
    // Insert at the beginning
    coursesGrid.insertBefore(newCard, coursesGrid.firstChild);
    
    // Animate the new card
    setTimeout(() => {
        newCard.classList.add('slide-up');
    }, 100);
}

function createCourseCard(data) {
    const card = document.createElement('div');
    card.className = 'course-card upcoming';
    
    card.innerHTML = `
        <div class="course-header">
            <div class="course-info">
                <h3 class="course-title">${data.className}</h3>
                <span class="course-code">${data.courseCode}</span>
            </div>
            <span class="course-status upcoming">Upcoming</span>
        </div>
        
        <div class="teacher-section">
            <div class="teacher-avatar">
                <span>SJ</span>
            </div>
            <div class="teacher-info">
                <h4>Dr. Sarah Johnson</h4>
                <p>Computer Science Department</p>
            </div>
        </div>
        
        <div class="course-stats">
            <div class="stat-item">
                <i class="fas fa-users"></i>
                <span>0 Students</span>
            </div>
            <div class="stat-item">
                <i class="fas fa-clock"></i>
                <span>Starting Soon</span>
            </div>
        </div>
        
        <div class="course-footer">
            <button class="btn-view-course" onclick="viewCourse('${data.courseCode}')">
                <i class="fas fa-eye"></i> View
            </button>
        </div>
    `;
    
    return card;
}

// Course Actions
function viewCourse(courseCode) {
    showNotification(`Opening course: ${courseCode}`, 'info');
    
    // Simulate navigation - replace with actual routing
    setTimeout(() => {
        console.log(`Navigating to course: ${courseCode}`);
        // window.location.href = `/Teacher/Course/${courseCode}`;
    }, 500);
}

// Utility Functions
function debounce(func, wait) {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func(...args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
}

function showNotification(message, type = 'info') {
    // Create notification element
    const notification = document.createElement('div');
    notification.className = `notification ${type}`;
    notification.style.cssText = `
        position: fixed;
        top: 20px;
        right: 20px;
        background: ${getNotificationColor(type)};
        color: white;
        padding: 12px 20px;
        border-radius: 8px;
        z-index: 9999;
        box-shadow: 0 4px 15px rgba(0,0,0,0.2);
        font-family: var(--archivo-plain-font);
        font-weight: 500;
        max-width: 400px;
        transform: translateX(100%);
        transition: transform 0.3s ease;
    `;
    
    notification.innerHTML = `
        <div style="display: flex; align-items: center; gap: 0.5rem;">
            <i class="fas fa-${getNotificationIcon(type)}"></i>
            <span>${message}</span>
        </div>
    `;
    
    document.body.appendChild(notification);
    
    // Slide in
    setTimeout(() => {
        notification.style.transform = 'translateX(0)';
    }, 100);
    
    // Auto remove
    setTimeout(() => {
        notification.style.transform = 'translateX(100%)';
        setTimeout(() => notification.remove(), 300);
    }, type === 'error' ? 5000 : 3000);
}

function getNotificationColor(type) {
    switch(type) {
        case 'success': return '#28a745';
        case 'error': return '#dc3545';
        case 'warning': return '#ffc107';
        default: return '#17a2b8';
    }
}

function getNotificationIcon(type) {
    switch(type) {
        case 'success': return 'check-circle';
        case 'error': return 'exclamation-circle';
        case 'warning': return 'exclamation-triangle';
        default: return 'info-circle';
    }
}

// Course Stats Updates (for future real-time updates)
function updateCourseStats() {
    // This function can be called periodically to update course statistics
    const courseCards = document.querySelectorAll('.course-card');
    
    courseCards.forEach(card => {
        // Update student counts, progress, etc.
        // This would typically fetch from an API
    });
}

// Initialize tooltips if needed
function initializeTooltips() {
    const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });
}

// Export functions for global access
window.viewCourse = viewCourse;
window.openCreateClassModal = openCreateClassModal;
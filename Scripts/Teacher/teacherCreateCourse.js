const searchInput = document.getElementById('studentSearch');
const studentsDisplay = document.querySelector('.students-display');
const selectedStudentsContainer = document.querySelector('.selected-students-container');
const studentCount = document.querySelector('.student-count');

// Create search dropdown container
const searchDropdown = document.createElement('div');
searchDropdown.className = 'search-dropdown';
searchDropdown.style.display = 'none';
searchInput.parentNode.appendChild(searchDropdown);

// Add event listeners
searchInput.addEventListener('input', handleStudentSearch);
searchInput.addEventListener('focus', showSearchDropdown);
document.addEventListener('click', handleOutsideClick);

let searchTimeout;
let lastSearchResults = []; // Fixed: Initialize properly

async function handleStudentSearch() {
    const query = searchInput.value.trim();

    clearTimeout(searchTimeout);
    
    if (!query) {
        hideSearchDropdown();
        lastSearchResults = []; // Clear when no query
        updateMainDisplay();
        return;
    }

    // Show loading state
    showSearchLoading();
    
    searchTimeout = setTimeout(async () => {
        try {
            const response = await fetch(`/Teacher/SearchStudents?query=${encodeURIComponent(query)}`);
            const data = await response.json();

            if (data.success) {
                lastSearchResults = data.students; // Store the results
                displaySearchResults(data.students);
            } else {
                showSearchError('Failed to search students');
                lastSearchResults = [];
            }
        } catch (err) {
            showSearchError('Error occurred while searching');
            lastSearchResults = [];
        }
    }, 500);
}

function displaySearchResults(students) {
    if (students.length === 0) {
        searchDropdown.innerHTML = `
            <div class="search-no-results">
                <i class="fas fa-search"></i>
                <p>No students found</p>
                <small>Try a different search term</small>
            </div>
        `;
    } else {
        searchDropdown.innerHTML = students.map(student => {
            const studentName = student.name || (student.firstName && student.lastName ? `${student.firstName} ${student.lastName}` : 'Unknown Name');
            const studentId = student.studentId || student.id || 'N/A';
            const studentEmail = student.email || student.Email || ''; // Handle different cases
            
            return `
                <div class="search-result-item" onclick="addStudentToClass(${JSON.stringify(student).replace(/"/g, '&quot;')})">
                    <div class="student-avatar">
                        ${getStudentInitials(student)}
                    </div>
                    <div class="student-details">
                        <div class="student-name">${studentName}</div>
                        <div class="student-info-row">
                            <span class="student-id">${studentId}</span>
                            ${studentEmail ? `<span class="student-email">${studentEmail}</span>` : '<span class="student-email">No email</span>'}
                        </div>
                        ${student.department ? `<div class="student-department">${student.department}</div>` : ''}
                    </div>
                    <div class="add-indicator">
                        ${isStudentSelected(student) ? 
                            '<i class="fas fa-check-circle added"></i>' : 
                            '<i class="fas fa-plus-circle"></i>'
                        }
                    </div>
                </div>
            `;
        }).join('');
    }
    
    showSearchDropdown();
}

function showSearchLoading() {
    searchDropdown.innerHTML = `
        <div class="search-loading">
            <div class="loading-spinner"></div>
            <p>Searching students...</p>
        </div>
    `;
    showSearchDropdown();
}

function showSearchError(message) {
    searchDropdown.innerHTML = `
        <div class="search-error">
            <i class="fas fa-exclamation-triangle"></i>
            <p>${message}</p>
        </div>
    `;
    showSearchDropdown();
}

function showSearchDropdown() {
    if (searchInput.value.trim()) {
        searchDropdown.style.display = 'block';
        searchInput.classList.add('search-active');
    }
}

function hideSearchDropdown() {
    searchDropdown.style.display = 'none';
    searchInput.classList.remove('search-active');
}

function handleOutsideClick(event) {
    if (!searchInput.contains(event.target) && !searchDropdown.contains(event.target)) {
        hideSearchDropdown();
    }
}

function getStudentInitials(student) {
    if (student.name) {
        const names = student.name.split(' ');
        return names.length > 1 ? 
            names[0].charAt(0).toUpperCase() + names[names.length - 1].charAt(0).toUpperCase() : 
            names[0].charAt(0).toUpperCase() + (names[0].charAt(1) || '').toUpperCase();
    } else if (student.firstName && student.lastName) {
        return student.firstName.charAt(0).toUpperCase() + student.lastName.charAt(0).toUpperCase();
    }
    return 'ST';
}

const selectedStudents = [];

function addStudentToClass(student, event) {
    if (event) event.stopPropagation();

    selectedStudents.push(student);
    updateSelectedStudentsDisplay();
    updateMainDisplay();

    if (lastSearchResults.length > 0) {
        displaySearchResults(lastSearchResults);
    }

}



function isStudentSelected(student) {
    return selectedStudents.find(s => {
        // Try multiple comparison strategies
        if (s.id && student.id) return s.id === student.id;
        if (s.studentId && student.studentId) return s.studentId === student.studentId;
        if (s.StudentID && student.StudentID) return s.StudentID === student.StudentID;
        // Fallback to name comparison
        const sName = s.name || (s.firstName && s.lastName ? `${s.firstName} ${s.lastName}` : '');
        const studentName = student.name || (student.firstName && student.lastName ? `${student.firstName} ${student.lastName}` : '');
        return sName === studentName && sName !== '';
    });
}

function removeStudentFromClass(studentId) {
    
    const index = selectedStudents.findIndex(s => {
        // Try multiple ID fields
        return s.id == studentId || 
               s.studentId == studentId || 
               s.StudentID == studentId ||
               s.Id == studentId;
    });
    
    
    if (index > -1) {
        const removedStudent = selectedStudents[index];
        selectedStudents.splice(index, 1);
        updateSelectedStudentsDisplay();
        updateMainDisplay();
        
        // Update search results if visible
        if (lastSearchResults.length > 0 && searchDropdown.style.display === 'block') {
            displaySearchResults(lastSearchResults);
        }
    }
}

function updateSelectedStudentsDisplay() {
    if (!selectedStudentsContainer) return;
    
    if (selectedStudents.length === 0) {
        selectedStudentsContainer.innerHTML = '';
    } else {
        selectedStudentsContainer.innerHTML = selectedStudents.map((student, index) => {
            const studentName = student.name || (student.firstName && student.lastName ? `${student.firstName} ${student.lastName}` : 'Unknown Name');
            const studentId = student.studentId || student.id || student.StudentID || student.Id || index;
            const studentEmail = student.email || student.Email || '';
            
            return `
                <div class="selected-student">
                    <div class="student-avatar-small">
                        ${getStudentInitials(student)}
                    </div>
                    <div class="selected-student-info">
                        <span class="selected-student-name">${studentName}</span>
                        <span class="selected-student-id">${studentId}</span>
                        ${studentEmail ? `<span class="selected-student-email">${studentEmail}</span>` : ''}
                    </div>
                    <button type="button" class="remove-student-btn" onclick="removeStudentFromClass('${studentId}')">
                        <i class="fas fa-times"></i>
                    </button>
                </div>
            `;
        }).join('');
    }
}

function updateMainDisplay() {
    if (selectedStudents.length === 0) {
        studentsDisplay.innerHTML = `
            <div class="no-students-message">
                <i class="fas fa-user-friends"></i>
                <p>No students enrolled yet</p>
                <small>Use the search above to find and add students to this class</small>
            </div>
        `;
    } else {
        // Render students directly in students-display without summary header
        studentsDisplay.innerHTML = `
            <div class="enrolled-students-list">
                ${selectedStudents.map((student, index) => {
                    const studentName = student.name || (student.firstName && student.lastName ? `${student.firstName} ${student.lastName}` : 'Unknown Name');
                    const studentId = student.studentId || student.id || student.StudentID || student.Id || index;
                    const studentEmail = student.email || student.Email || '';
                    
                    return `
                        <div class="enrolled-student-item">
                            <div class="student-avatar">
                                ${getStudentInitials(student)}
                            </div>
                            <div class="student-info">
                                <div class="student-header">
                                    <span class"student-name">${studentName} </span>
                                    <span class"student-id">${studentId}</span>
                                </div>

                                <div class="student-details-small">
                                    ${studentEmail ? `<span>${studentEmail}</span>` : '<span>No email</span>'}
                                    <span>•</span>
                                    ${student.department ? `<div class="student-department">${student.department}</div>` : ''}
                                </div>
                            </div>
                            <button type="button" class="remove-btn" onclick="removeStudentFromClass('${studentId}')">
                                <i class="fas fa-times"></i>
                            </button>
                        </div>
                    `;
                }).join('')}
            </div>
        `;
    }
    
    if (studentCount) {
        studentCount.textContent = selectedStudents.length;
    }
    
    // Update Clear All button state
    const clearAllBtn = document.querySelector('.btn-clear-all');
    if (clearAllBtn) {
        clearAllBtn.disabled = selectedStudents.length === 0;
    }
}

function clearAllStudents() {
    
    selectedStudents.length = 0;
    updateSelectedStudentsDisplay();
    updateMainDisplay();
    searchInput.value = '';
    hideSearchDropdown();
    lastSearchResults = [];
    
}

function getLastSearchResults() {
    return lastSearchResults;
}

// Initialize on page load
document.addEventListener('DOMContentLoaded', function() {
    updateMainDisplay();
});

document.getElementById('createCourseForm').addEventListener('submit', function (e) {
    const hiddenInput = document.getElementById('SelectedStudentsJson');
    hiddenInput.value = JSON.stringify(selectedStudents);
});


// Export functions for global access
window.addStudentToClass = addStudentToClass;
window.removeStudentFromClass = removeStudentFromClass;
window.clearAllStudents = clearAllStudents;


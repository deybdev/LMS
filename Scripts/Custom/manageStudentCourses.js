
let selectedStudent = null;
let enrolledCourses = [];
let selectedCoursesToAdd = [];

$(document).ready(function () {
    initializeEventHandlers();
});

function initializeEventHandlers() {
    // Student search - Button click
    $('#searchStudentBtn').on('click', function(e) {
        e.preventDefault(); // Prevent any default button behavior
        console.log('Search button clicked');
        searchStudents();
    });

    $('#studentSearchInput').on('keypress', function (e) {
        if (e.which === 13 || e.keyCode === 13) { // Enter key
            e.preventDefault();
            console.log('Enter key pressed');
            searchStudents();
        }
    });

    // Modal course search
    $('#modalCourseSearch').on('input', debounce(function () {
        searchCoursesForModal();
    }, 300));

    $('#addCourseYearLevel, #addCourseSemester').on('change', function () {
        $('#modalCourseSearch').val('');
        $('#modalCourseSearchDropdown').hide();
        clearModalSelection();
    });

    $('#confirmAddCoursesBtn').on('click', confirmAddCourses);

    $('#confirmRemoveCourseBtn').on('click', confirmRemoveCourse);

    $(document).on('click', function (e) {
        if (!$(e.target).closest('.search-filter-section').length && 
            !$(e.target).closest('.student-search-dropdown').length) {
            $('#studentSearchResults').hide();
        }
        if (!$(e.target).closest('.search-dropdown-wrapper').length) {
            $('#modalCourseSearchDropdown').hide();
        }
    });
    
    $('#studentSearchResults').on('click', function(e) {
        e.stopPropagation();
    });
}

// Debounce function for search inputs
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

// Search for students
function searchStudents() {
    const searchTerm = $('#studentSearchInput').val().trim();
    
    if (searchTerm.length === 0) {
        showAlert('warning', 'Please enter a search term');
        return;
    }

    $('#studentSearchResults').show();
    $('#studentSearchLoading').show();
    $('#studentResultsList').empty();
    $('#studentNoResults').hide();
    

    $.ajax({
        url: '/Student/SearchStudents',
        type: 'GET',
        data: { searchTerm: searchTerm },
        success: function(response) {
            $('#studentSearchLoading').hide();

            if (response.success && response.students && response.students.length > 0) {
                $('#studentNoResults').hide();
                $('#studentResultsCount').text(`${response.students.length} student${response.students.length > 1 ? 's' : ''} found`);
                displayStudentResults(response.students);
            } else {
                $('#studentNoResults').show();
                $('#studentResultsCount').text('0 students found');
            }
        },
        error: function(xhr, status, error) {
            $('#studentSearchLoading').hide();
            showAlert('danger', 'Error searching for students. Please try again.');
            $('#studentNoResults').show();
            $('#studentResultsCount').text('0 students found');
        }
    });
}

function displayStudentResults(students) {
    const $list = $('#studentResultsList');
    $list.empty();

    students.forEach(student => {
        const initials = `${student.firstName.charAt(0)}${student.lastName.charAt(0)}`;
        const $item = $(`
            <div class="student-result-item" data-student-id="${student.id}">
                <div class="student-result-avatar">${initials}</div>
                <div class="student-result-info">
                    <h5>${student.firstName} ${student.lastName}</h5>
                    <p>
                        <i class="fas fa-id-card"></i> ${student.studentId} | 
                        <i class="fas fa-envelope"></i> ${student.email}
                    </p>
                    <p>
                        <i class="fas fa-graduation-cap"></i> ${student.program} - 
                        ${student.yearLevel}${getYearSuffix(student.yearLevel)} Year, Section ${student.section}
                    </p>
                </div>
            </div>
        `);

        $item.on('click', () => selectStudent(student));
        $list.append($item);
    });
}

// Select a student
function selectStudent(student) {
    selectedStudent = student;

    $('#selectedStudentName').text(`${student.firstName} ${student.lastName}`);
    $('#selectedStudentId').text(student.studentId);
    $('#selectedStudentEmail').text(student.email);
    $('#selectedStudentProgram').text(student.program);
    $('#selectedStudentYearSection').text(`${student.yearLevel}${getYearSuffix(student.yearLevel)} Year - Section ${student.section}`);

    $('#modalStudentName').text(`${student.firstName} ${student.lastName}`);
    $('#modalStudentId').val(student.id);

    loadEnrolledCourses(student.id);

    $('#emptyStateSection').hide();
    $('#selectedStudentSection').show();
    $('#studentSearchResults').hide();

    $('html, body').animate({
        scrollTop: $('#selectedStudentSection').offset().top - 100
    }, 500);
}

// Clear student selection
function clearStudentSelection() {
    selectedStudent = null;
    enrolledCourses = [];
    $('#selectedStudentSection').hide();
    $('#emptyStateSection').show();
    $('#studentSearchInput').val('').focus();
}

// Load enrolled courses for a student
function loadEnrolledCourses(studentId) {
    // Show loading state
    $('#enrolledCoursesTable').html(`
        <tr>
            <td colspan="6" class="text-center" style="padding: 2rem;">
                <i class="fas fa-spinner fa-spin" style="font-size: 2rem; color: var(--primary-color);"></i>
                <p style="margin-top: 1rem;">Loading courses...</p>
            </td>
        </tr>
    `);

    // AJAX call to backend
    $.ajax({
        url: '/Course/GetStudentCourses',
        type: 'GET',
        data: { studentId: studentId },
        success: function(response) {
            if (response.success) {
                enrolledCourses = response.courses || [];
                displayEnrolledCourses(enrolledCourses);
            } else {
                showAlert('danger', response.message || 'Error loading student courses');
                enrolledCourses = [];
                displayEnrolledCourses(enrolledCourses);
            }
        },
        error: function(xhr, status, error) {
            console.error('Load courses error:', error);
            showAlert('danger', 'Error loading student courses. Please try again.');
            enrolledCourses = [];
            displayEnrolledCourses(enrolledCourses);
        }
    });
}

// Display enrolled courses
function displayEnrolledCourses(courses) {
    const $table = $('#enrolledCoursesTable');
    $table.empty();

    if (courses.length === 0) {
        $table.append(`
            <tr id="noCoursesRow">
                <td colspan="6" class="text-center" style="padding: 3rem; color: var(--gray-text);">
                    <i class="fas fa-inbox" style="font-size: 3rem; color: #dee2e6; margin-bottom: 1rem; display: block;"></i>
                    <h5 style="margin-bottom: 0.5rem;">No courses enrolled</h5>
                    <p style="margin: 0;">This student is not currently enrolled in any courses.</p>
                </td>
            </tr>
        `);
        $('#enrolledCoursesCount').text('0 Courses');
    } else {
        courses.forEach(course => {
            // Handle date - it could be a string or Date object
            let dateEnrolled = course.dateEnrolled;
            if (typeof dateEnrolled === 'string') {
                // Parse the date string (format: "/Date(1234567890)/" or ISO string)
                if (dateEnrolled.indexOf('/Date(') === 0) {
                    const timestamp = parseInt(dateEnrolled.replace(/\/Date\((\d+)\)\//, '$1'));
                    dateEnrolled = new Date(timestamp);
                } else {
                    dateEnrolled = new Date(dateEnrolled);
                }
            }

            const $row = $(`
                <tr class="default-row" 
                    data-course-id="${course.courseId}" 
                    data-year="${course.yearLevel}" 
                    data-semester="${course.semester}">
                    <td><span class="course-code-tag">${course.courseCode}</span></td>
                    <td class="course-title-col">${course.courseTitle}</td>
                    <td>${course.yearLevel}${getYearSuffix(course.yearLevel)} Year</td>
                    <td>${getSemesterName(course.semester)}</td>
                    <td class="last-login">
                        <div>${formatDate(dateEnrolled)}</div>
                        <div class="login-time">${formatTime(dateEnrolled)}</div>
                    </td>
                    <td class="action-buttons">
                        <button class="action-btn delete-btn" 
                                onclick="openRemoveCourseModal(${course.id}, ${course.courseId}, '${course.courseCode} - ${course.courseTitle}')"
                                title="Remove Course">
                            <i class="fa-solid fa-trash"></i>
                        </button>
                    </td>
                </tr>
            `);
            $table.append($row);
        });
        $('#enrolledCoursesCount').text(`${courses.length} Course${courses.length > 1 ? 's' : ''}`);
    }
}

// Filter enrolled courses
// Filter enrolled courses by search term only
function filterEnrolledCourses() {
    const searchTerm = $('#courseFilterInput').val().toLowerCase();

    $('#enrolledCoursesTable tr').each(function () {
        const $row = $(this);
        if ($row.attr('id') === 'noCoursesRow') return;

        const courseCode = $row.find('.course-code-tag').text().toLowerCase();
        const courseTitle = $row.find('.course-title-col').text().toLowerCase();

        if (courseCode.includes(searchTerm) || courseTitle.includes(searchTerm)) {
            $row.show();
        } else {
            $row.hide();
        }
    });
}

// Search courses for modal (Add Course)
function searchCoursesForModal() {
    const searchTerm = $('#modalCourseSearch').val().trim();
    const yearLevel = $('#addCourseYearLevel').val();
    const semester = $('#addCourseSemester').val();

    if (searchTerm.length < 2) {
        $('#modalCourseSearchDropdown').hide();
        return;
    }

    if (!yearLevel || !semester) {
        showAlert('warning', 'Please select Year Level and Semester first');
        return;
    }

    if (!selectedStudent) {
        showAlert('warning', 'No student selected');
        return;
    }

    // Show loading state
    $('#modalCourseSearchDropdown').show();
    $('#modalCourseLoading').show();
    $('#modalCourseList').empty();
    $('#modalCourseNoResults').hide();

    // AJAX call to backend
    $.ajax({
        url: '/Course/GetAvailableCoursesForStudent',
        type: 'GET',
        data: {
            studentId: selectedStudent.id,
            yearLevel: yearLevel,
            semester: semester,
            searchTerm: searchTerm
        },
        success: function(response) {
            $('#modalCourseLoading').hide();

            if (response.success && response.courses && response.courses.length > 0) {
                $('#modalCourseNoResults').hide();
                $('#modalCourseCount').text(`${response.courses.length} course${response.courses.length > 1 ? 's' : ''} found`);
                displayModalCourseResults(response.courses);
            } else {
                $('#modalCourseNoResults').show();
                $('#modalCourseCount').text('0 courses found');
            }
        },
        error: function(xhr, status, error) {
            $('#modalCourseLoading').hide();
            console.error('Search courses error:', error);
            showAlert('danger', 'Error searching for courses. Please try again.');
            $('#modalCourseNoResults').show();
            $('#modalCourseCount').text('0 courses found');
        }
    });
}

// Display course results in modal
function displayModalCourseResults(courses) {
    const $list = $('#modalCourseList');
    $list.empty();

    courses.forEach(course => {
        // Skip if already enrolled
        const isEnrolled = enrolledCourses.some(ec => ec.courseId === course.id);
        const isSelected = selectedCoursesToAdd.some(sc => sc.id === course.id);

        if (isEnrolled) return;

        const $item = $(`
            <div class="course-result-item ${isSelected ? 'disabled' : ''}" data-course-id="${course.id}">
                <span class="course-code">${course.code}</span>
                <span class="course-title">${course.title}</span>
            </div>
        `);

        if (!isSelected) {
            $item.on('click', () => addCourseToSelection(course));
        }

        $list.append($item);
    });
}

// Add course to selection
function addCourseToSelection(course) {
    if (selectedCoursesToAdd.some(c => c.id === course.id)) return;

    selectedCoursesToAdd.push(course);
    updateModalSelectedCourses();
    searchCoursesForModal(); // Refresh to show updated state
}

// Update modal selected courses display
function updateModalSelectedCourses() {
    if (selectedCoursesToAdd.length === 0) {
        $('#modalSelectedCourses').hide();
        $('#confirmAddCoursesBtn').prop('disabled', true);
        return;
    }

    $('#modalSelectedCourses').show();
    $('#confirmAddCoursesBtn').prop('disabled', false);
    $('#modalSelectedCount').text(selectedCoursesToAdd.length);

    const $list = $('#modalSelectedCoursesList');
    $list.empty();

    selectedCoursesToAdd.forEach((course, index) => {
        const $item = $(`
            <div class="selected-course-item">
                <div class="selected-course-info">
                    <span class="course-code">${course.code}</span>
                    <span class="course-title">${course.title}</span>
                </div>
                <button type="button" class="btn-remove-selected" onclick="removeCourseFromSelection(${index})">
                    <i class="fas fa-times"></i>
                </button>
            </div>
        `);
        $list.append($item);
    });
}

// Remove course from selection
function removeCourseFromSelection(index) {
    selectedCoursesToAdd.splice(index, 1);
    updateModalSelectedCourses();
    searchCoursesForModal(); // Refresh to show updated state
}

// Clear modal selection
function clearModalSelection() {
    selectedCoursesToAdd = [];
    updateModalSelectedCourses();
    searchCoursesForModal();
}

// Confirm add courses
function confirmAddCourses() {
    if (selectedCoursesToAdd.length === 0) return;

    const studentId = $('#modalStudentId').val();
    const yearLevel = $('#addCourseYearLevel').val();
    const semester = $('#addCourseSemester').val();

    // Prepare course IDs
    const courseIds = selectedCoursesToAdd.map(c => c.id).join(',');

    // Get anti-forgery token
    const token = $('input[name="__RequestVerificationToken"]').val();

    // AJAX call to add courses
    $.ajax({
        url: '/Course/AddCoursesToStudent',
        type: 'POST',
        data: {
            __RequestVerificationToken: token,
            studentId: studentId,
            courseIds: courseIds
        },
        success: function(response) {
            if (response.success) {
                // Reload enrolled courses
                loadEnrolledCourses(selectedStudent.id);
                showAlert('success', response.message || `Successfully added ${selectedCoursesToAdd.length} course(s) to ${selectedStudent.firstName} ${selectedStudent.lastName}`);

                // Reset modal
                selectedCoursesToAdd = [];
                $('#addCourseModal').modal('hide');
                $('#addCourseForm')[0].reset();
                $('#modalCourseSearch').val('');
                $('#modalCourseSearchDropdown').hide();
                updateModalSelectedCourses();
            } else {
                showAlert('danger', response.message || 'Error adding courses');
            }
        },
        error: function(xhr, status, error) {
            console.error('Add courses error:', error);
            showAlert('danger', 'Error adding courses. Please try again.');
        }
    });
}

// Open remove course modal
function openRemoveCourseModal(studentCourseId, courseId, courseName) {
    $('#removeStudentCourseId').val(studentCourseId);
    $('#removeCourseId').val(courseId);
    $('#removeStudentName').text(`${selectedStudent.firstName} ${selectedStudent.lastName}`);
    $('#removeCourseName').text(courseName);
    $('#removeCourseModal').modal('show');
}

// Confirm remove course
function confirmRemoveCourse() {
    const studentCourseId = parseInt($('#removeStudentCourseId').val());

    // Get anti-forgery token
    const token = $('input[name="__RequestVerificationToken"]').val();

    // AJAX call to remove course
    $.ajax({
        url: '/Course/RemoveCourseFromStudent',
        type: 'POST',
        data: {
            __RequestVerificationToken: token,
            studentCourseId: studentCourseId
        },
        success: function(response) {
            if (response.success) {
                // Reload enrolled courses
                loadEnrolledCourses(selectedStudent.id);
                showAlert('success', response.message || 'Course removed successfully');

                // Close modal
                $('#removeCourseModal').modal('hide');
            } else {
                showAlert('danger', response.message || 'Error removing course');
            }
        },
        error: function(xhr, status, error) {
            console.error('Remove course error:', error);
            showAlert('danger', 'Error removing course. Please try again.');
            $('#removeCourseModal').modal('hide');
        }
    });
}

// Utility Functions
function getYearSuffix(year) {
    return year === 1 ? 'st' : year === 2 ? 'nd' : year === 3 ? 'rd' : 'th';
}

function getSemesterName(semester) {
    return semester === 1 ? '1st Semester' : semester === 2 ? '2nd Semester' : semester === 3 ? 'Summer' : `Semester ${semester}`;
}

function formatDate(date) {
    const d = new Date(date);
    return `${(d.getMonth() + 1).toString().padStart(2, '0')}/${d.getDate().toString().padStart(2, '0')}/${d.getFullYear()}`;
}

function formatTime(date) {
    const d = new Date(date);
    let hours = d.getHours();
    const minutes = d.getMinutes();
    const ampm = hours >= 12 ? 'PM' : 'AM';
    hours = hours % 12 || 12;
    return `${hours}:${minutes.toString().padStart(2, '0')} ${ampm}`;
}

// Alert function
function showAlert(type, message) {
    const alertTypes = {
        'success': 'alert-success',
        'danger': 'alert-danger',
        'warning': 'alert-warning',
        'info': 'alert-info'
    };

    const icons = {
        'success': 'fa-check-circle',
        'danger': 'fa-exclamation-circle',
        'warning': 'fa-exclamation-triangle',
        'info': 'fa-info-circle'
    };

    const $alert = $(`
        <div class="alert ${alertTypes[type]} alert-dismissible fade show" role="alert" style="animation: slideInRight 0.3s ease-out;">
            <i class="fas ${icons[type]} me-2"></i>
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
        </div>
    `);

    $('#alertContainer').append($alert);

    setTimeout(() => {
        $alert.alert('close');
    }, 5000);
}

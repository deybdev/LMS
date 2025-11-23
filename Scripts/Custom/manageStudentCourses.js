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

    // Course filter input
    $('#courseFilterInput').on('input', debounce(function () {
        filterEnrolledCourses();
    }, 300));

    // Modal course search
    $('#modalCourseSearch').on('input', debounce(function () {
        searchCoursesForModal();
    }, 300));

    $('#confirmAddCoursesBtn').on('click', confirmAddCourses);

    $('#confirmRemoveCourseBtn').on('click', confirmRemoveCourse);

    $(document).on('click', function (e) {
        if (!$(e.target).closest('.search-filter-section').length && 
            !$(e.target).closest('.student-search-dropdown').length) {
            $('#studentSearchResults').hide();
        }
        if (!$(e.target).closest('.search-dropdown-wrapper').length &&
            !$(e.target).closest('#modalCourseSearchDropdown').length) {
            $('#modalCourseSearchDropdown').hide();
        }
    });
    
    $('#studentSearchResults').on('click', function(e) {
        e.stopPropagation();
    });

    $('#modalCourseSearchDropdown').on('click', function(e) {
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
            <td colspan="7" class="text-center" style="padding: 2rem;">
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
            console.log('GetStudentCourses Response:', response); // Debug log
            if (response.success) {
                enrolledCourses = response.courses || [];
                
                // Ensure sectionId is included in enrolledCourses
                enrolledCourses = enrolledCourses.map(course => ({
                    ...course,
                    sectionId: course.sectionId || 0 // Fallback to 0 if not provided
                }));
                
                console.log('Enrolled Courses with sectionId:', enrolledCourses); // Debug log
                displayEnrolledCourses(enrolledCourses);
            } else {
                showAlert('danger', response.message || 'Failed to load student courses');
                enrolledCourses = [];
                displayEnrolledCourses(enrolledCourses);
            }
        },
        error: function(xhr, status, error) {
            console.error('Load courses error:', error);
            console.error('Response:', xhr.responseText); // Debug log
            showAlert('danger', 'Failed to load student courses');
            enrolledCourses = [];
            displayEnrolledCourses(enrolledCourses);
        }
    });
}

// Display enrolled courses
function displayEnrolledCourses(courses) {
    const $table = $('#enrolledCoursesTable');
    $table.empty();

    console.log('displayEnrolledCourses called with:', courses); // Debug log

    if (courses.length === 0) {
        $table.append(`
            <tr id="noCoursesRow">
                <td colspan="8" class="text-center" style="padding: 3rem; color: var(--gray-text);">
                    <i class="fas fa-inbox" style="font-size: 3rem; color: #dee2e6; margin-bottom: 1rem; display: block;"></i>
                    <h5 style="margin-bottom: 0.5rem;">No courses enrolled</h5>
                    <p style="margin: 0;">This student is not currently enrolled in any courses.</p>
                </td>
            </tr>
        `);
        $('#enrolledCoursesCount').text('0 Courses');
    } else {
        courses.forEach(course => {
            
            console.log('Processing course:', course.courseCode, 'Day value:', course.day); // Debug day value
            
            // Format teacher info
            let teacherDisplay = 'Not Assigned';
            let teacherClass = 'text-muted';
            if (course.teacherName) {
                teacherDisplay = `<div class="teacher-name-display">${course.teacherName}</div>`;
                if (course.teacherEmail) {
                    teacherDisplay += `<small class="teacher-email-display">${course.teacherEmail}</small>`;
                }
                teacherClass = '';
            } else {
                teacherDisplay = '<span class="badge bg-warning text-dark">Not Assigned</span>';
            }

            // Format time from
            let timeFromDisplay = 'Not Set';
            if (course.timeFrom) {
                const timeFromDate = new Date(course.timeFrom);
                timeFromDisplay = formatTime(timeFromDate);
            } else {
                timeFromDisplay = '<span class="text-muted fst-italic">Not Set</span>';
            }

            // Format time to
            let timeToDisplay = 'Not Set';
            if (course.timeTo) {
                const timeToDate = new Date(course.timeTo);
                timeToDisplay = formatTime(timeToDate);
            } else {
                timeToDisplay = '<span class="text-muted fst-italic">Not Set</span>';
            }

            // Format day
            let dayDisplay = 'Not Set';
            if (course.day && course.day.trim() !== '') {
                dayDisplay = course.day;
            } else {
                dayDisplay = '<span class="text-muted fst-italic">Not Set</span>';
            }

            const $row = `
                <tr class="default-row" 
                    data-course-id="${course.courseId}" 
                    data-year="${course.yearLevel}" 
                    data-semester="${course.semester}">
                    <td><span class="course-code-tag">${course.courseCode}</span></td>
                    <td class="course-title-col">${course.courseTitle}</td>
                    <td class="${teacherClass}">${teacherDisplay}</td>
                    <td>${getSemesterName(course.semester)}</td>
                    <td>${dayDisplay}</td>
                    <td class="time-display">${timeFromDisplay}</td>
                    <td class="time-display">${timeToDisplay}</td>
                    <td class="action-buttons">
                        <button class="action-btn delete-btn" 
                                onclick="openRemoveCourseModal(${course.id}, ${course.courseId}, '${course.courseCode} - ${course.courseTitle}')"
                                title="Remove Course">
                            <i class="fa-solid fa-trash"></i>
                        </button>
                    </td>
                </tr>
            `;
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

    console.log('searchCoursesForModal called with:', searchTerm); // Debug

    if (searchTerm.length < 2) {
        $('#modalCourseSearchDropdown').hide();
        return;
    }

    if (!selectedStudent) {
        showAlert('warning', 'No student selected');
        return;
    }

    console.log('Searching courses for student:', selectedStudent.id); // Debug

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
            searchTerm: searchTerm
        },
        success: function (response) {
            console.log('Course search response:', response); // Debug
            $('#modalCourseLoading').hide();

            if (response.success && response.courses && response.courses.length > 0) {
                $('#modalCourseNoResults').hide();
                $('#modalCourseCount').text(`${response.courses.length} course${response.courses.length > 1 ? 's' : ''} found`);
                displayModalCourseResults(response.courses);
            } else {
                $('#modalCourseNoResults').show();
                $('#modalCourseCount').text('0 courses found');
                console.log('No courses found or error:', response.message); // Debug
            }
        },
        error: function (xhr, status, error) {
            $('#modalCourseLoading').hide();
            console.error('Search courses error:', error);
            console.error('Response status:', status);
            console.error('Response text:', xhr.responseText);
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

    console.log('Enrolled courses:', enrolledCourses); // Debug
    console.log('Available courses:', courses); // Debug

    courses.forEach(course => {
        // Check if already enrolled - compare both courseId and sectionId
        const isEnrolled = enrolledCourses.some(ec => {
            const enrolled = ec.courseId === course.id && ec.sectionId === course.sectionId;
            console.log(`Checking enrollment: Course ${course.id}, Section ${course.sectionId} - Enrolled: ${enrolled}`);
            return enrolled;
        });
        
        const isSelected = selectedCoursesToAdd.some(sc => sc.id === course.id && sc.sectionId === course.sectionId);

        console.log(`Course ${course.code} (Section ${course.sectionName}) - Enrolled: ${isEnrolled}, Selected: ${isSelected}`);

        if (isEnrolled) {
            console.log(`Skipping ${course.code} - ${course.sectionName} (already enrolled)`);
            return; // Skip if already enrolled
        }

        // Format time display
        let timeDisplay = '<span class="text-muted fst-italic">Not set</span>';
        if (course.timeFrom && course.timeTo) {
            const timeFrom = formatTime(new Date(course.timeFrom));
            const timeTo = formatTime(new Date(course.timeTo));
            timeDisplay = `<span class="time-info">${timeFrom} - ${timeTo}</span>`;
        }

        const $item = $(`
            <div class="course-result-item ${isSelected ? 'disabled' : ''}" 
                 data-course-id="${course.id}"
                 data-section-id="${course.sectionId}">
                <div class="course-item-header">
                    <span class="course-code">${course.code}</span>
                    <span class="course-title">${course.title}</span>
                </div>
                <div class="course-item-details">
                    <span class="detail-badge year-badge">
                        <i class="fas fa-layer-group"></i> ${course.yearLevel}${getYearSuffix(course.yearLevel)} Year
                    </span>
                    <span class="detail-badge section-badge">
                        <i class="fas fa-users"></i> Section ${course.sectionName}
                    </span>
                    <span class="detail-badge semester-badge">
                        <i class="fas fa-calendar-alt"></i> ${getSemesterName(course.semester)}
                    </span>
                    <span class="detail-badge time-badge">
                        <i class="fas fa-clock"></i> ${timeDisplay}
                    </span>
                </div>
            </div>
        `);

        if (!isSelected) {
            $item.on('click', () => addCourseToSelection(course));
        }

        $list.append($item);
    });

    if ($list.children().length === 0) {
        $list.append(`
            <div class="no-results-message" style="padding: 2rem; text-align: center;">
                <i class="fas fa-check-circle" style="font-size: 2rem; color: #28a745; display: block; margin-bottom: 0.5rem;"></i>
                <p style="margin: 0.5rem 0 0.25rem 0; font-weight: 600; color: var(--dark-primary-color);">All matching courses enrolled</p>
                <small style="color: var(--gray-text);">The student is already enrolled in all matching courses</small>
            </div>
        `);
    }
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
        // Format time display
        let timeDisplay = 'Not set';
        if (course.timeFrom && course.timeTo) {
            const timeFrom = formatTime(new Date(course.timeFrom));
            const timeTo = formatTime(new Date(course.timeTo));
            timeDisplay = `${timeFrom} - ${timeTo}`;
        }

        const $item = $(`
            <div class="selected-course-item">
                <div class="selected-course-info">
                    <div class="selected-course-main">
                        <span class="course-code">${course.code}</span>
                        <span class="course-title">${course.title}</span>
                    </div>
                    <div class="selected-course-meta">
                        <small class="meta-detail">
                            <i class="fas fa-layer-group"></i> ${course.yearLevel}${getYearSuffix(course.yearLevel)} Year
                        </small>
                        <small class="meta-detail">
                            <i class="fas fa-users"></i> Section ${course.sectionName}
                        </small>
                        <small class="meta-detail">
                            <i class="fas fa-calendar-alt"></i> ${getSemesterName(course.semester)}
                        </small>
                        <small class="meta-detail">
                            <i class="fas fa-clock"></i> ${timeDisplay}
                        </small>
                    </div>
                </div>
                <button type="button" class="btn-remove-selected" onclick="removeCourseFromSelection(${index})">
                    <i class="fas fa-times"></i>
                </button>
            </div>
        `);
        $list.append($item);
    });
}

// Confirm add courses
function confirmAddCourses() {
    if (selectedCoursesToAdd.length === 0) return;

    const studentId = $('#modalStudentId').val();

    // Prepare course data with section IDs
    const courseData = selectedCoursesToAdd.map(c => ({
        courseId: c.id,
        sectionId: c.sectionId
    }));

    // Get anti-forgery token
    const token = $('input[name="__RequestVerificationToken"]').val();

    console.log('Adding courses:', courseData); // Debug

    // AJAX call to add courses
    $.ajax({
        url: '/Course/AddCoursesToStudent',
        type: 'POST',
        data: {
            __RequestVerificationToken: token,
            studentId: studentId,
            courseDataJson: JSON.stringify(courseData)
        },
        success: function(response) {
            if (response.success) {
                // Reload enrolled courses
                loadEnrolledCourses(selectedStudent.id);
                showAlert('success', response.message || `Successfully added ${selectedCoursesToAdd.length} course(s)`);

                // Reset modal
                selectedCoursesToAdd = [];
                $('#addCourseModal').modal('hide');
                $('#modalCourseSearch').val('');
                $('#modalCourseSearchDropdown').hide();
                updateModalSelectedCourses();
            } else {
                showAlert('danger', response.message || 'Error adding courses');
            }
        },
        error: function(xhr, status, error) {
            console.error('Add courses error:', error);
            console.error('Response:', xhr.responseText);
            showAlert('danger', 'Error adding courses. Please try again.');
        }
    });
}

// Add course to selection
function addCourseToSelection(course) {
    if (selectedCoursesToAdd.some(c => c.id === course.id && c.sectionId === course.sectionId)) return;

    selectedCoursesToAdd.push(course);
    updateModalSelectedCourses();
    searchCoursesForModal(); // Refresh to show updated state
}

// Remove course from selection
function removeCourseFromSelection(index) {
    selectedCoursesToAdd.splice(index, 1);
    updateModalSelectedCourses();
    searchCoursesForModal();
}

// Clear modal selection
function clearModalSelection() {
    selectedCoursesToAdd = [];
    updateModalSelectedCourses();
    searchCoursesForModal();
}

// Open remove course modal
function openRemoveCourseModal(studentCourseId, courseId, courseName) {
    if (!selectedStudent) {
        showAlert('warning', 'No student selected');
        return;
    }

    $('#removeStudentCourseId').val(studentCourseId);
    $('#removeCourseId').val(courseId);
    $('#removeStudentName').text(`${selectedStudent.firstName} ${selectedStudent.lastName}`);
    $('#removeCourseName').text(courseName);
    $('#removeCourseModal').modal('show');
}

// Confirm remove course
function confirmRemoveCourse() {
    const studentCourseId = parseInt($('#removeStudentCourseId').val());

    if (!studentCourseId || isNaN(studentCourseId)) {
        showAlert('danger', 'Invalid course selection');
        return;
    }

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
    if (!date) return 'Not Set';
    
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

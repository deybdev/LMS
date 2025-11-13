// Assign Teacher JavaScript
let allAssignments = [];
let allCourseSections = [];
let currentPage = 1;
const itemsPerPage = 5;
let deleteItemId = null;
let viewMode = 'all'; // 'all', 'assigned', 'unassigned'

$(document).ready(function () {
    console.log("Assign Teacher page loaded");

    // Load initial data
    loadTeachers();
    loadPrograms();
    loadAllCourseSections(); // Changed to load all course-sections

    // Setup dropdowns cascade
    setupDropdownCascade();

    // Form submission handler
    $('#assignTeacherForm').on('submit', function (e) {
        e.preventDefault();
        assignTeacher();
    });

    // Search functionality (client-side, no reload)
    $('#searchInput').on('input', function () {
        currentPage = 1; // Reset to first page
        displayAssignments(); // Just re-display with filter
    });

    // Filter dropdowns (server-side, reload data)
    $('#programFilter, #yearLevelFilter, #semesterFilter').on('change', function () {
        currentPage = 1; // Reset to first page
        loadAllCourseSections(); // Reload from server with new filters
    });

    // Reset form when modal is hidden
    $('#assignTeacherModal').on('hidden.bs.modal', function () {
        resetForm();
    });

    // View mode toggle (if you add filter buttons)
    $(document).on('click', '.view-mode-btn', function() {
        viewMode = $(this).data('mode');
        $('.view-mode-btn').removeClass('active');
        $(this).addClass('active');
        currentPage = 1; // Reset to first page
        displayAssignments();
    });
});

// Load all course-section combinations (both assigned and unassigned)
function loadAllCourseSections() {
    const programId = $('#programFilter').val() || null;
    const yearLevel = $('#yearLevelFilter').val() || null;
    const semester = $('#semesterFilter').val() || null;

    $.ajax({
        url: '/IT/GetAllCourseSections',
        type: 'GET',
        data: {
            programId: programId,
            yearLevel: yearLevel,
            semester: semester
        },
        success: function (response) {
            if (response.success) {
                allCourseSections = response.courseSections;
                updateStatistics();
                displayAssignments();
            } else {
                showAlert('danger', response.message || 'Failed to load course sections');
            }
        },
        error: function (xhr, status, error) {
            console.error('Error loading course sections:', error);
            showAlert('danger', 'Failed to load course sections');
            $('#teacherAssignmentContainer').html('<div class="alert alert-danger">Error loading course sections</div>');
        }
    });
}

// Load all teachers
function loadTeachers() {
    $.ajax({
        url: '/IT/GetTeachers',
        type: 'GET',
        success: function (response) {
            if (response.success) {
                const $select = $('#teacher');
                $select.empty().append('<option value="">Choose a teacher</option>');

                response.teachers.forEach(function (teacher) {
                    $select.append(
                        $('<option></option>')
                            .val(teacher.id)
                            .text(teacher.name + ' - ' + teacher.email)
                    );
                });
            }
        },
        error: function (xhr, status, error) {
            console.error('Error loading teachers:', error);
            showAlert('danger', 'Failed to load teachers');
        }
    });
}

// Load all programs
function loadPrograms() {
    $.ajax({
        url: '/Program/GetAll',
        type: 'GET',
        success: function (programs) {
            const $select = $('#program');
            const $filterSelect = $('#programFilter');

            $select.empty().append('<option value="">Choose program</option>');
            $filterSelect.empty().append('<option value="">All Programs</option>');

            programs.forEach(function (program) {
                $select.append(
                    $('<option></option>')
                        .val(program.Id)
                        .text(program.ProgramCode + ' - ' + program.ProgramName)
                        .data('duration', program.ProgramDuration)
                );

                $filterSelect.append(
                    $('<option></option>')
                        .val(program.Id)
                        .text(program.ProgramCode)
                );
            });
        },
        error: function (xhr, status, error) {
            console.error('Error loading programs:', error);
            showAlert('danger', 'Failed to load programs');
        }
    });
}

// Setup dropdown cascade
function setupDropdownCascade() {
    // When program changes, load year levels
    $('#program').on('change', function () {
        const programId = $(this).val();
        const programDuration = $(this).find('option:selected').data('duration');

        const $yearLevelSelect = $('#yearLevel');
        $yearLevelSelect.empty().append('<option value="">Choose year level</option>');

        if (programId && programDuration) {
            $yearLevelSelect.prop('disabled', false);

            for (let i = 1; i <= programDuration; i++) {
                const suffix = getOrdinalSuffix(i);
                $yearLevelSelect.append(
                    $('<option></option>')
                        .val(i)
                        .text(i + suffix + ' Year')
                );
            }
        } else {
            $yearLevelSelect.prop('disabled', true);
            $('#section').prop('disabled', true).empty().append('<option value="">Choose section</option>');
            $('#course').prop('disabled', true).empty().append('<option value="">Choose course</option>');
        }
    });

    // When year level or semester changes, load sections
    $('#yearLevel, #semester').on('change', function () {
        const programId = $('#program').val();
        const yearLevel = $('#yearLevel').val();

        if (programId && yearLevel) {
            loadSections(programId, yearLevel);
        } else {
            $('#section').prop('disabled', true).empty().append('<option value="">Choose section</option>');
        }

        // Also load courses if semester is selected
        loadCourses();
    });

    // When section changes, reload courses to update disabled state
    $('#section').on('change', function () {
        const sectionId = $(this).val();
        
        if (sectionId) {
            // Check if there are already assigned courses for this section
            const semester = $('#semester').val();
            const assignedCourses = allCourseSections.filter(cs => 
                cs.sectionId === parseInt(sectionId) && 
                cs.semester === parseInt(semester) &&
                cs.isAssigned === true
            );

            if (assignedCourses.length > 0) {
                console.log(`Section has ${assignedCourses.length} course(s) already assigned`);
            }
        }
        
        // Reload courses with updated disabled state
        loadCourses();
    });
}

// Load sections based on program and year level
function loadSections(programId, yearLevel) {
    $.ajax({
        url: '/Program/GetSections',
        type: 'GET',
        data: { programId: programId, yearLevel: yearLevel },
        success: function (sections) {
            const $select = $('#section');
            $select.empty().append('<option value="">Choose section</option>');

            if (sections.length > 0) {
                $select.prop('disabled', false);
                sections.forEach(function (section) {
                    $select.append(
                        $('<option></option>')
                            .val(section.Id)
                            .text(section.SectionName)
                    );
                });
            } else {
                $select.prop('disabled', true);
                showAlert('warning', 'No sections found for this program and year level');
            }
        },
        error: function (xhr, status, error) {
            console.error('Error loading sections:', error);
            showAlert('danger', 'Failed to load sections');
        }
    });
}

// Load courses based on program, year level, and semester
function loadCourses() {
    const programId = $('#program').val();
    const yearLevel = $('#yearLevel').val();
    const semester = $('#semester').val();
    const sectionId = $('#section').val();

    if (!programId || !yearLevel || !semester) {
        $('#course').prop('disabled', true).empty().append('<option value="">Choose course</option>');
        return;
    }

    $.ajax({
        url: '/IT/GetCoursesForSection',
        type: 'GET',
        data: {
            programId: programId,
            yearLevel: yearLevel,
            semester: semester
        },
        success: function (response) {
            const $select = $('#course');
            $select.empty().append('<option value="">Choose course</option>');

            if (response.success && response.courses.length > 0) {
                $select.prop('disabled', false);
                
                response.courses.forEach(function (course) {
                    // Check if this course is already assigned to the selected section
                    const isAssigned = sectionId && allCourseSections.some(cs => 
                        cs.courseId === course.id && 
                        cs.sectionId === parseInt(sectionId) && 
                        cs.semester === parseInt(semester) &&
                        cs.isAssigned === true
                    );

                    const option = $('<option></option>')
                        .val(course.id)
                        .text(course.code + ' - ' + course.title + (isAssigned ? ' (Already Assigned)' : ''))
                        .prop('disabled', isAssigned)
                        .data('assigned', isAssigned);

                    // Add visual indicator for assigned courses
                    if (isAssigned) {
                        option.css('color', '#999')
                              .css('font-style', 'italic');
                    }

                    $select.append(option);
                });
            } else {
                $select.prop('disabled', true);
                showAlert('warning', 'No courses found for this curriculum');
            }
        },
        error: function (xhr, status, error) {
            console.error('Error loading courses:', error);
            showAlert('danger', 'Failed to load courses');
        }
    });
}

// Update statistics
function updateStatistics() {
    const assignedSections = allCourseSections.filter(cs => cs.isAssigned);
    const unassignedSections = allCourseSections.filter(cs => !cs.isAssigned);
    
    $('#totalAssignments').text(assignedSections.length);

    const uniqueTeachers = new Set(assignedSections.map(a => a.teacherId).filter(id => id));
    $('#totalTeachers').text(uniqueTeachers.size);

    // Changed to show unassigned courses instead of total courses
    $('#totalCourses').text(unassignedSections.length);
}

// Display assignments (both assigned and unassigned)
function displayAssignments() {
    const container = $('#teacherAssignmentContainer');
    container.empty();

    // Apply search filter
    const searchTerm = $('#searchInput').val().toLowerCase();
    let filteredData = allCourseSections;

    // Client-side search filtering
    if (searchTerm) {
        filteredData = allCourseSections.filter(function (item) {
            const teacherName = item.teacherName || '';
            return teacherName.toLowerCase().includes(searchTerm) ||
                   item.courseTitle.toLowerCase().includes(searchTerm) ||
                   item.courseCode.toLowerCase().includes(searchTerm) ||
                   item.fullSectionName.toLowerCase().includes(searchTerm) ||
                   item.programCode.toLowerCase().includes(searchTerm);
        });
    }

    // Filter by view mode
    if (viewMode === 'assigned') {
        filteredData = filteredData.filter(cs => cs.isAssigned);
    } else if (viewMode === 'unassigned') {
        filteredData = filteredData.filter(cs => !cs.isAssigned);
    }

    if (filteredData.length === 0) {
        $('.empty-state').show();
        $('.pagination-container').hide();
        return;
    }

    $('.empty-state').hide();
    $('.pagination-container').show();

    // Calculate pagination
    const totalPages = Math.ceil(filteredData.length / itemsPerPage);
    const startIndex = (currentPage - 1) * itemsPerPage;
    const endIndex = Math.min(startIndex + itemsPerPage, filteredData.length);
    const pageItems = filteredData.slice(startIndex, endIndex);

    // Display course-sections
    pageItems.forEach(function (item) {
        if (item.isAssigned) {
            const initials = getInitials(item.teacherName);
            const card = createAssignedCard(item, initials);
            container.append(card);
        } else {
            const card = createUnassignedCard(item);
            container.append(card);
        }
    });

    // Update pagination
    updatePagination(totalPages, startIndex, endIndex, filteredData.length);
}

// Create card for assigned course-section
function createAssignedCard(item, initials) {
    return `
        <div class="teacher-card assigned" data-program="${item.programId}" data-year="${item.yearLevel}" data-semester="${item.semester}">
            <div class="teacher-header">
                <div class="teacher-info">
                    <div class="teacher-avatar">${initials}</div>
                    <div class="teacher-details">
                        <h4>${item.teacherName}</h4>
                        <p><i class="fa-solid fa-envelope"></i> ${item.teacherEmail}</p>
                    </div>
                </div>
                <div class="assign-status">
                    <span class="assigned-badge">
                        <i class="fa-solid fa-check-circle"></i> Assigned
                    </span>
                    <button class="action-btn edit-btn" 
                            onclick="reassignTeacher(${item.id}, ${item.teacherId}, ${item.courseId}, ${item.sectionId}, ${item.semester}, ${item.programId}, ${item.yearLevel}, '${item.teacherName}', '${item.courseTitle}', '${item.fullSectionName}')"
                            title="Reassign Teacher">
                        <i class="fa-solid fa-user-pen"></i>
                    </button>
                </div>
            </div>
            <div class="course-section-info">
                <div class="info-item">
                    <i class="fa-solid fa-book"></i>
                    <strong>Course:</strong>
                    <span>${item.courseTitle}</span>
                </div>
                <div class="info-item">
                    <i class="fa-solid fa-users"></i>
                    <strong>Section:</strong>
                    <span>${item.fullSectionName}</span>
                </div>
                <div class="info-item">
                    <i class="fa-solid fa-calendar"></i>
                    <strong>Semester:</strong>
                    <span>${item.semesterName}</span>
                </div>
                <div class="info-item">
                    <i class="fa-solid fa-user-group"></i>
                    <strong>Students:</strong>
                    <span>${item.studentCount}</span>
                </div>
            </div>
        </div>
    `;
}

// Create card for unassigned course-section
function createUnassignedCard(item) {
    return `
        <div class="teacher-card unassigned" data-program="${item.programId}" data-year="${item.level}" data-semester="${item.semester}">
            <div class="teacher-header">
                <div class="teacher-info">
                    <div class="teacher-avatar empty-avatar">
                        <i class="fa-solid fa-user-slash"></i>
                    </div>
                    <div class="teacher-details">
                        <h4 class="text-muted">No Teacher Assigned</h4>
                        <p class="text-muted"><i class="fa-solid fa-exclamation-circle"></i> This course needs a teacher</p>
                    </div>
                </div>
                <div class="assign-status">
                    <span class="unassigned-badge">
                        <i class="fa-solid fa-clock"></i> Pending
                    </span>
                    <button class="action-btn assign-btn-quick" 
                            onclick="quickAssign(${item.courseId}, ${item.sectionId}, ${item.semester}, '${item.courseTitle}', '${item.fullSectionName}')"
                            title="Assign Teacher">
                        <i class="fa-solid fa-user-plus"></i>
                    </button>
                </div>
            </div>
            <div class="course-section-info">
                <div class="info-item">
                    <i class="fa-solid fa-book"></i>
                    <strong>Course:</strong>
                    <span>${item.courseTitle}</span>
                </div>
                <div class="info-item">
                    <i class="fa-solid fa-users"></i>
                    <strong>Section:</strong>
                    <span>${item.fullSectionName}</span>
                </div>
                <div class="info-item">
                    <i class="fa-solid fa-calendar"></i>
                    <strong>Semester:</strong>
                    <span>${item.semesterName}</span>
                </div>
                <div class="info-item">
                    <i class="fa-solid fa-user-group"></i>
                    <strong>Students:</strong>
                    <span>${item.studentCount}</span>
                </div>
            </div>
        </div>
    `;
}

// Quick assign function to pre-fill modal
function quickAssign(courseId, sectionId, semester, courseTitle, sectionName) {
    // Reset form first
    resetForm();
    
    // Clear any existing assignment ID (this is for new assignment)
    $('#assignmentId').val('');
    $('#assignTeacherModalLabel').html('<i class="fa-solid fa-user-plus"></i> Assign Teacher to Course');
    $('#submitAssignBtn').html('<i class="fa-solid fa-user-plus me-1"></i> Assign Teacher');
    
    // Get section details to pre-populate form
    const courseSection = allCourseSections.find(cs => 
        cs.courseId === courseId && 
        cs.sectionId === sectionId && 
        cs.semester === semester
    );
    
    if (courseSection) {
        // Set program
        $('#program').val(courseSection.programId).trigger('change');
        
        // Wait a bit for cascade to complete, then set other values
        setTimeout(function() {
            $('#yearLevel').val(courseSection.yearLevel).trigger('change');
            
            setTimeout(function() {
                $('#semester').val(semester).trigger('change');
                
                setTimeout(function() {
                    $('#section').val(sectionId).trigger('change');
                    
                    setTimeout(function() {
                        $('#course').val(courseId);
                    }, 100);
                }, 100);
            }, 100);
        }, 100);
    }
    
    // Show modal
    $('#assignTeacherModal').modal('show');
}

// Reassign teacher function - pre-fills modal with current assignment
function reassignTeacher(assignmentId, currentTeacherId, courseId, sectionId, semester, programId, yearLevel, teacherName, courseTitle, sectionName) {
    // Reset form first
    resetForm();
    
    // Store the assignment ID for updating
    $('#assignmentId').val(assignmentId);
    
    // Change modal title and button text
    $('#assignTeacherModalLabel').html('<i class="fa-solid fa-user-pen"></i> Reassign Teacher');
    $('#submitAssignBtn').html('<i class="fa-solid fa-save me-1"></i> Update Assignment');
    
    // Pre-fill the form with current values
    $('#program').val(programId).trigger('change');
    
    // Wait for cascade to complete, then set other values
    setTimeout(function() {
        $('#yearLevel').val(yearLevel).trigger('change');
        
        setTimeout(function() {
            $('#semester').val(semester).trigger('change');
            
            setTimeout(function() {
                $('#section').val(sectionId).trigger('change');
                
                setTimeout(function() {
                    $('#course').val(courseId);
                    
                    // Set the current teacher as selected
                    setTimeout(function() {
                        $('#teacher').val(currentTeacherId);
                    }, 100);
                }, 100);
            }, 100);
        }, 100);
    }, 100);
    
    // Show modal
    $('#assignTeacherModal').modal('show');
}

// Assign teacher
function assignTeacher() {
    const formData = {
        teacherId: $('#teacher').val(),
        courseId: $('#course').val(),
        sectionId: $('#section').val(),
        semester: $('#semester').val(),
        remarks: $('#remarks').val()
    };

    const assignmentId = $('#assignmentId').val();
    const isUpdate = assignmentId && assignmentId !== '';

    // Client-side validation
    if (!formData.teacherId || !formData.courseId || !formData.sectionId || !formData.semester) {
        showFormError('Please fill all required fields.');
        return;
    }

    // For new assignments, check if course-section is already assigned
    if (!isUpdate) {
        const isAlreadyAssigned = allCourseSections.some(cs => 
            cs.courseId === parseInt(formData.courseId) && 
            cs.sectionId === parseInt(formData.sectionId) && 
            cs.semester === parseInt(formData.semester) &&
            cs.isAssigned === true
        );

        if (isAlreadyAssigned) {
            const courseName = $('#course option:selected').text();
            const sectionName = $('#section option:selected').text();
            showFormError(`This course is already assigned to a teacher in section ${sectionName}. Please select a different course or section.`);
            return;
        }
    }

    // Get anti-forgery token
    const token = $('input[name="__RequestVerificationToken"]').val();

    // Determine which endpoint to call
    const url = isUpdate ? '/IT/ReassignTeacher' : '/IT/AssignTeacherToCourse';
    const data = isUpdate 
        ? { ...formData, assignmentId: parseInt(assignmentId), __RequestVerificationToken: token }
        : { ...formData, __RequestVerificationToken: token };

    $.ajax({
        url: url,
        type: 'POST',
        data: data,
        success: function (response) {
            if (response.success) {
                showAlert('success', response.message);
                $('#assignTeacherModal').modal('hide');
                loadAllCourseSections(); // Reload course-sections
                resetForm();
            } else {
                showFormError(response.message);
            }
        },
        error: function (xhr, status, error) {
            console.error('Error assigning teacher:', error);
            showFormError('An error occurred while assigning teacher');
        }
    });
}

// Reset form
function resetForm() {
    $('#assignTeacherForm')[0].reset();
    $('#formErrorContainer').addClass('d-none');
    $('#yearLevel, #section, #course').prop('disabled', true);
    $('#assignmentId').val(''); // Clear assignment ID
    
    // Reset modal title and button text to default
    $('#assignTeacherModalLabel').html('<i class="fa-solid fa-user-plus"></i> Assign Teacher to Course');
    $('#submitAssignBtn').html('<i class="fa-solid fa-user-plus me-1"></i> Assign Teacher');
}

// Show form error
function showFormError(message) {
    $('#formErrorMessage').text(message);
    $('#formErrorContainer').removeClass('d-none');
}

// Update pagination
function updatePagination(totalPages, startIndex, endIndex, totalItems) {
    $('#paginationInfo').text(`Showing ${startIndex + 1} to ${endIndex} of ${totalItems} entries`);
    $('#currentPageInfo').text(`Page ${currentPage} of ${totalPages}`);

    // Update buttons
    $('#prevBtn').prop('disabled', currentPage === 1);
    $('#nextBtn').prop('disabled', currentPage === totalPages);

    // Update page numbers
    const $numbers = $('#paginationNumbers');
    $numbers.empty();

    for (let i = 1; i <= totalPages; i++) {
        const btn = $('<button></button>')
            .addClass('pagination-number')
            .text(i)
            .toggleClass('active', i === currentPage)
            .on('click', function () {
                currentPage = i;
                displayAssignments();
            });
        $numbers.append(btn);
    }
}

// Pagination button handlers
$('#prevBtn').on('click', function () {
    if (currentPage > 1) {
        currentPage--;
        displayAssignments();
    }
});

$('#nextBtn').on('click', function () {
    const totalPages = Math.ceil(allCourseSections.length / itemsPerPage);
    if (currentPage < totalPages) {
        currentPage++;
        displayAssignments();
    }
});

// Helper functions
function getInitials(name) {
    if (!name) return '?';
    const parts = name.split(' ');
    if (parts.length >= 2) {
        return parts[0][0] + parts[1][0];
    }
    return name.substring(0, 2).toUpperCase();
}

function getOrdinalSuffix(num) {
    const j = num % 10;
    const k = num % 100;

    if (j === 1 && k !== 11) return 'st';
    if (j === 2 && k !== 12) return 'nd';
    if (j === 3 && k !== 13) return 'rd';
    return 'th';
}

// Show alert function
function showAlert(type, message) {
    const alertHtml = `
        <div class="alert alert-${type} alert-dismissible fade show" role="alert">
            <i class="fa-solid fa-${type === 'success' ? 'check-circle' : 'exclamation-circle'} me-2"></i>
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
        </div>
    `;

    const $container = $('#alertContainer');
    $container.append(alertHtml);

    // Auto-dismiss after 5 seconds
    setTimeout(function () {
        $container.find('.alert').first().fadeOut(function () {
            $(this).remove();
        });
    }, 5000);
}

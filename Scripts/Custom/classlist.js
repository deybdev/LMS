$(document).ready(function () {
    loadClassList();
    initializeViewSwitcher();
    initializeAttendance();
    initializeEditableGrades();
    initializeModalHandlers();
});

// View Switcher
function initializeViewSwitcher() {
    $('#viewDropdown').on('change', function () {
        const selectedView = $(this).val();
        $('.view-container').removeClass('active-view');

        switch (selectedView) {
            case 'student-info':
                $('#studentInfoView').addClass('active-view');
                break;
            case 'attendance':
                $('#attendanceView').addClass('active-view');
                loadAttendanceTable();
                break;
            case 'performance':
                $('#performanceView').addClass('active-view');
                break;
        }
    });
}

// Fetch and display class list
function loadClassList() {
    const $tableBody = $('#studentTableBody');

    $tableBody.html(`
        <tr>
            <td colspan="6" class="loading-state">
                <i class="fas fa-spinner fa-spin"></i>
                <p style="margin-top: 1rem;">Loading students...</p>
            </td>
        </tr>
    `);

    $.ajax({
        url: '/Teacher/GetEnrolledStudents',
        type: 'GET',
        data: { teacherCourseSectionId: teacherCourseSectionId },
        success: function (response) {
            if (response.success && response.students.length > 0) {
                displayStudents(response.students);
            } else {
                showNoStudents();
            }
        },
        error: function (xhr, status, error) {
            console.error('Error loading class list:', error);
            showError();
        }
    });
}

function displayStudents(students) {
    const $tableBody = $('#studentTableBody');
    $tableBody.empty();

    students.forEach(student => {
        const row = `
            <tr>
                <td><span class="student-id">${escapeHtml(student.studentId)}</span></td>
                <td><span class="student-name">${escapeHtml(student.name)}</span></td>
                <td><span class="student-email">${escapeHtml(student.email)}</span></td>
                <td><span class="grade-badge">${student.grade || '-'}</span></td>
            </tr>
        `;
        $tableBody.append(row);
    });
}

function showNoStudents() {
    $('#studentTableBody').html(`
        <tr>
            <td colspan="6" class="no-students">
                <i class="fas fa-user-slash"></i>
                <h5>No Students Enrolled</h5>
                <p>There are no students currently enrolled in this course section.</p>
            </td>
        </tr>
    `);
}

function showError() {
    $('#studentTableBody').html(`
        <tr>
            <td colspan="6" class="no-students">
                <i class="fas fa-exclamation-triangle" style="color: #dc3545;"></i>
                <h5>Error Loading Students</h5>
                <p>An error occurred while loading the class list. Please refresh the page to try again.</p>
            </td>
        </tr>
    `);
}

// Attendance
function initializeAttendance() {
    $('#attendanceDate').on('change', function () {
        loadAttendanceTable();
    });

    $(document).on('click', '#attendanceTableBody .btn-attendance', function () {
        const $row = $(this).closest('tr');
        const $button = $(this);
        
        if ($button.hasClass('btn-late')) {
            if (!$button.hasClass('active')) {
                promptLateMinutes($row, $button);
            }
        } else {
            $row.find('.btn-attendance').removeClass('active');
            $button.addClass('active');
            $row.data('late-minutes', null);
            
            updateAttendanceCounts();
            autoSaveAttendance($row);
        }
    });

    // Export to CSV button
    $('.export-attendance').on('click', function () {
        exportAttendanceToCSV();
    });
}

function promptLateMinutes($row, $button) {
    const studentName = $row.find('.student-name').text();
    const currentMinutes = $row.data('late-minutes') || 0;
    
    $('#lateStudentName').text(studentName);
    $('#lateMinutesInput').val(currentMinutes);
    
    // Set display to flex and show with opacity animation
    $('#lateModal').css({
        'display': 'flex',
        'opacity': '0'
    }).animate({ opacity: 1 }, 200);
    
    $('#lateMinutesInput').focus();
    
    $('#lateModal').data('current-row', $row);
    $('#lateModal').data('current-button', $button);
}

function initializeModalHandlers() {
    // Late Modal handlers
    $('.close-modal, .btn-cancel').on('click', function() {
        $('#lateModal').animate({ opacity: 0 }, 200, function() {
            $(this).css('display', 'none');
        });
        $('#exportModal').animate({ opacity: 0 }, 200, function() {
            $(this).css('display', 'none');
        });
    });
    
    $('#lateModal').on('click', function(e) {
        if ($(e.target).is('#lateModal')) {
            $(this).animate({ opacity: 0 }, 200, function() {
                $(this).css('display', 'none');
            });
        }
    });
    
    $('#exportModal').on('click', function(e) {
        if ($(e.target).is('#exportModal')) {
            $(this).animate({ opacity: 0 }, 200, function() {
                $(this).css('display', 'none');
            });
        }
    });
    
    // Late confirmation
    $('.btn-confirm').on('click', function() {
        const minutes = parseInt($('#lateMinutesInput').val());
        const $row = $('#lateModal').data('current-row');
        const $button = $('#lateModal').data('current-button');
        
        if (isNaN(minutes) || minutes < 0) {
            alert('Please enter a valid number of minutes (0 or greater).');
            return;
        }
        
        $row.find('.btn-attendance').removeClass('active');
        $button.addClass('active');
        $row.data('late-minutes', minutes);
        $button.html(`Late (${minutes}min)`);
        
        updateAttendanceCounts();
        autoSaveAttendance($row);
        $('#lateModal').animate({ opacity: 0 }, 200, function() {
            $(this).css('display', 'none');
        });
    });
    
    // Export confirmation
    $('#exportConfirm').on('click', function() {
        performCSVExport();
    });
    
    $('#exportCancel').on('click', function() {
        $('#exportModal').animate({ opacity: 0 }, 200, function() {
            $(this).css('display', 'none');
        });
    });
    
    $('#lateMinutesInput').on('keypress', function(e) {
        if (e.which === 13) {
            $('.btn-confirm').click();
        }
    });
}

function autoSaveAttendance($row) {
    const studentId = $row.data('student-id');
    if (!studentId) return;
    
    let status = null;
    let lateMinutes = null;
    
    if ($row.find('.btn-present').hasClass('active')) {
        status = 'Present';
    } else if ($row.find('.btn-absent').hasClass('active')) {
        status = 'Absent';
    } else if ($row.find('.btn-late').hasClass('active')) {
        status = 'Late';
        lateMinutes = $row.data('late-minutes') || 0;
    }
    
    if (status === null) return;
    
    const payload = {
        teacherCourseSectionId: teacherCourseSectionId,
        attendanceDate: $('#attendanceDate').val(),
        records: [{
            studentId: studentId,
            status: status,
            lateMinutes: lateMinutes
        }]
    };
    
    $row.addClass('saving');
    
    $.ajax({
        url: '/Teacher/SaveAttendance',
        type: 'POST',
        contentType: 'application/json; charset=utf-8',
        data: JSON.stringify(payload),
        success: function (response) {
            if (response.success) {
                $row.removeClass('saving').addClass('saved');
                setTimeout(() => $row.removeClass('saved'), 1000);
                updateAttendanceCounts();
            } else {
                $row.removeClass('saving');
                alert('Failed to save: ' + (response.message || 'Unknown error.'));
            }
        },
        error: function () {
            $row.removeClass('saving');
            alert('An error occurred while auto-saving attendance.');
        }
    });
}

function loadAttendanceTable() {
    const date = $('#attendanceDate').val();
    const $tbody = $('#attendanceTableBody');

    if (!teacherCourseSectionId || teacherCourseSectionId === 0) {
        showAttendancePlaceholder('fas fa-exclamation-triangle', 'Missing course section', 'Course section ID is not set. Please refresh the page.');
        return;
    }

    $tbody.html(`
        <tr>
            <td colspan="5" class="loading-state">
                <i class="fas fa-spinner fa-spin"></i>
                <p>Loading attendance...</p>
            </td>
        </tr>
    `);

    $.ajax({
        url: '/Teacher/GetAttendance',
        type: 'GET',
        data: { teacherCourseSectionId: teacherCourseSectionId, date: date },
        success: function (response) {
            if (response.success) {
                renderAttendanceRows(response.students || []);
            } else {
                showAttendancePlaceholder('fas fa-user-slash', 'Attendance unavailable', response.message || 'Unable to load attendance data.');
            }
        },
        error: function (xhr, status, error) {
            showAttendancePlaceholder('fas fa-exclamation-triangle', 'Error loading attendance', 'Please refresh the page. Error: ' + error);
        }
    });
}

function renderAttendanceRows(students) {
    const $tbody = $('#attendanceTableBody');
    $tbody.empty();

    if (!students || students.length === 0) {
        showAttendancePlaceholder('fas fa-user-slash', 'No enrolled students', 'There are no students assigned to this section yet.');
        updateAttendanceCounts();
        return;
    }

    students.forEach((student, index) => {
        const presentActive = student.status === 'Present' ? 'active' : '';
        const absentActive = student.status === 'Absent' ? 'active' : '';
        const lateActive = student.status === 'Late' ? 'active' : '';
        
        let lateButtonText = 'Late';
        if (lateActive && student.lateMinutes) {
            lateButtonText = `Late (${student.lateMinutes}min)`;
        }

        const row = `
            <tr data-student-id="${student.id}" data-late-minutes="${student.lateMinutes || 0}">
                <td>${index + 1}</td>
                <td><span class="student-id">${escapeHtml(student.studentId)}</span></td>
                <td><span class="student-name">${escapeHtml(student.name)}</span></td>
                <td><span class="student-email">${escapeHtml(student.email)}</span></td>
                <td>
                    <div class="attendance-buttons">
                        <button class="btn-attendance btn-present ${presentActive}">Present</button>
                        <button class="btn-attendance btn-late ${lateActive}">${lateButtonText}</button>
                        <button class="btn-attendance btn-absent ${absentActive}">Absent</button>
                    </div>
                </td>
            </tr>
        `;

        $tbody.append(row);
    });

    updateAttendanceCounts();
}

function showAttendancePlaceholder(iconClass, title, message) {
    $('#attendanceTableBody').html(`
        <tr>
            <td colspan="5" class="no-students">
                <i class="${iconClass}"></i>
                <h5>${escapeHtml(title)}</h5>
                <p>${escapeHtml(message)}</p>
            </td>
        </tr>
    `);
    updateAttendanceCounts();
}

function updateAttendanceCounts() {
    const rows = $('#attendanceTableBody tr').filter(function () {
        return !!$(this).data('student-id');
    }).length;

    const present = $('#attendanceTableBody .btn-present.active').length;
    const absent = $('#attendanceTableBody .btn-absent.active').length;
    const late = $('#attendanceTableBody .btn-late.active').length;
    const unmarked = rows - (present + absent + late);

    $('#totalStudents').text(rows);
    $('#presentCount').text(present);
    $('#absentCount').text(absent);
    $('#lateCount').text(late);
    $('#unmarkedCount').text(unmarked >= 0 ? unmarked : 0);
}

// Export Attendance to CSV
function exportAttendanceToCSV() {
    const date = $('#attendanceDate').val();
    
    if (!date) {
        alert('Please select a date first.');
        return;
    }
    
    // Format date for display
    const dateObj = new Date(date);
    const formattedDate = dateObj.toLocaleDateString('en-US', { 
        weekday: 'long',
        year: 'numeric', 
        month: 'long', 
        day: 'numeric' 
    });
    
    // Set the date in modal
    $('#exportDate').text(formattedDate);
    
    // Check if there's any data to export
    let hasData = false;
    
    $('#attendanceTableBody tr').each(function() {
        const $row = $(this);
        if ($row.data('student-id')) {
            hasData = true;
            return false; // break loop
        }
    });
    
    // Check if there's any data
    if (!hasData) {
        alert('No student data to export. Please make sure students are enrolled in this section.');
        return;
    }
    
    // Show export confirmation modal with flex display
    $('#exportModal').css({
        'display': 'flex',
        'opacity': '0'
    }).animate({ opacity: 1 }, 200);
}

function performCSVExport() {
    const date = $('#attendanceDate').val();
    const rows = [];
    
    // CSV Headers
    rows.push(['No.', 'Student ID', 'Name', 'Email', 'Status', 'Late Minutes', 'Date']);
    
    // Get attendance data
    $('#attendanceTableBody tr').each(function() {
        const $row = $(this);
        const studentId = $row.data('student-id');
        
        if (studentId) {
            const no = $row.find('td:eq(0)').text();
            const studentNumber = $row.find('.student-id').text();
            const name = $row.find('.student-name').text();
            const email = $row.find('.student-email').text();
            
            let status = 'Unmarked';
            let lateMinutes = '';
            
            if ($row.find('.btn-present').hasClass('active')) {
                status = 'Present';
            } else if ($row.find('.btn-absent').hasClass('active')) {
                status = 'Absent';
            } else if ($row.find('.btn-late').hasClass('active')) {
                status = 'Late';
                lateMinutes = $row.data('late-minutes') || 0;
            }
            
            rows.push([no, studentNumber, name, email, status, lateMinutes, date]);
        }
    });
    
    // Convert to CSV format
    const csvContent = rows.map(row => 
        row.map(cell => `"${cell}"`).join(',')
    ).join('\n');
    
    // Create and download file
    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    const url = URL.createObjectURL(blob);
    
    const filename = `Attendance_${date}.csv`;
    
    link.setAttribute('href', url);
    link.setAttribute('download', filename);
    link.style.visibility = 'hidden';
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    
    // Close modal with animation
    $('#exportModal').animate({ opacity: 0 }, 200, function() {
        $(this).css('display', 'none');
    });
}

// Editable Grades
function initializeEditableGrades() {
    $(document).on('dblclick', '.editable-grade', function () {
        const $gradeSpan = $(this);
        if ($gradeSpan.find('input').length > 0) return;

        const currentValue = $gradeSpan.text().trim();
        const maxPoints = $gradeSpan.data('max') || 100;

        const $input = $('<input>', {
            type: 'number',
            class: 'grade-input',
            value: currentValue,
            min: 0,
            max: maxPoints,
            step: 0.01
        });

        $gradeSpan.html($input);
        $input.focus().select();

        $input.on('blur', function () {
            saveGradeEdit($gradeSpan, $input, true);
        });

        $input.on('keypress', function (e) {
            if (e.which === 13) {
                e.preventDefault();
                $input.blur();
            }
        });

        $input.on('keydown', function (e) {
            if (e.which === 27) {
                e.preventDefault();
                $input.off('blur');
                $gradeSpan.text(currentValue).removeClass('editing');
            }
        });

        $gradeSpan.addClass('editing');
    });

    function saveGradeEdit($gradeSpan, $input, autoSave) {
        const newValue = parseFloat($input.val());
        const originalValue = parseFloat($gradeSpan.data('original'));
        const maxPoints = parseFloat($gradeSpan.data('max')) || 100;

        if (isNaN(newValue) || newValue < 0 || newValue > maxPoints) {
            alert(`Please enter a valid grade between 0 and ${maxPoints}`);
            $gradeSpan.text($gradeSpan.data('original')).removeClass('editing');
            return;
        }

        $gradeSpan.text(newValue).removeClass('editing');

        if (newValue !== originalValue) {
            $gradeSpan.addClass('modified');
            if (autoSave) saveGradeToBackend($gradeSpan, newValue);
        } else {
            $gradeSpan.removeClass('modified');
        }
    }

    function saveGradeToBackend($gradeSpan, newValue) {
        const studentId = $gradeSpan.data('student-id');
        const assignment = $gradeSpan.data('assignment');

        $gradeSpan.addClass('saving');

        $.ajax({
            url: '/Teacher/SaveGrade',
            type: 'POST',
            data: {
                studentId,
                assignment,
                grade: newValue,
                courseId,
                teacherCourseSectionId
            },
            success: function (response) {
                if (response.success) {
                    $gradeSpan.data('original', newValue)
                        .removeClass('modified saving')
                        .addClass('saved');
                    setTimeout(() => $gradeSpan.removeClass('saved'), 1000);
                } else {
                    alert('Failed to save grade: ' + (response.message || 'Unknown error'));
                    $gradeSpan.removeClass('saving');
                }
            },
            error: function () {
                $gradeSpan.data('original', newValue)
                    .removeClass('modified saving')
                    .addClass('saved');
                setTimeout(() => $gradeSpan.removeClass('saved'), 1000);
            }
        });
    }
}

function escapeHtml(text) {
    if (!text) return '';
    const map = { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#039;' };
    return text.replace(/[&<>"']/g, function (m) { return map[m]; });
}

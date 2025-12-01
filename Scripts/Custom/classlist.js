$(document).ready(function () {
    console.log('Classlist page loaded'); // Debug log
    console.log('Initial teacherCourseSectionId:', teacherCourseSectionId); // Debug log
    console.log('Initial courseId:', courseId); // Debug log
    
    loadClassList();
    initializeViewSwitcher();
    initializeAttendance();
    initializeEditableGrades();
    initializeModalHandlers();
    initializeManualClassworkAddition();
});

// View Switcher
function initializeViewSwitcher() {
    $('#viewDropdown').on('change', function () {
        const selectedView = $(this).val();
        console.log('View switched to:', selectedView); // Debug log
        
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
                console.log('Loading performance view with teacherCourseSectionId:', teacherCourseSectionId); // Debug log
                loadPerformanceData();
                break;
        }
    });
}

// Fetch and display class list
function loadClassList() {
    const $tableBody = $('#studentTableBody');

    $tableBody.html(`
        <tr>
            <td colspan="5" class="loading-state">
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
            </tr>
        `;
        $tableBody.append(row);
    });
}

function showNoStudents() {
    $('#studentTableBody').html(`
        <tr>
            <td colspan="5" class="no-students">
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
            <td colspan="5" class="no-students">
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

    $(document).on('click', '#attendanceTableBody .btn-attendance', function (e) {
        e.preventDefault();
        e.stopPropagation();
        
        const $row = $(this).closest('tr');
        const $button = $(this);
        
        console.log('Attendance button clicked:', $button.attr('class'));
        
        if ($button.hasClass('btn-late')) {
            if (!$button.hasClass('active')) {
                promptLateMinutes($row, $button);
            } else {
                // If already active, just update counts
                updateAttendanceCounts();
                autoSaveAttendance($row);
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
    $('.close-modal').on('click', function() {
        const modalId = $(this).closest('.modal').attr('id');
        hideModal('#' + modalId);
    });
    
    $('.btn-cancel').on('click', function() {
        const modalId = $(this).closest('.modal').attr('id');
        hideModal('#' + modalId);
    });
    
    $('#lateModal').on('click', function(e) {
        if ($(e.target).is('#lateModal')) {
            hideModal('#lateModal');
        }
    });
    
    $('#exportModal').on('click', function(e) {
        if ($(e.target).is('#exportModal')) {
            hideModal('#exportModal');
        }
    });
    
    // Late confirmation
    $('#lateModal .btn-confirm').on('click', function() {
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
        hideModal('#lateModal');
    });
    
    // Export confirmation
    $('#exportConfirm').on('click', function() {
        performCSVExport();
    });
    
    $('#exportCancel').on('click', function() {
        hideModal('#exportModal');
    });
    
    $('#lateMinutesInput').on('keypress', function(e) {
        if (e.which === 13) {
            $('#lateModal .btn-confirm').click();
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
                        <button type="button" class="btn-attendance btn-present ${presentActive}">Present</button>
                        <button type="button" class="btn-attendance btn-late ${lateActive}">${lateButtonText}</button>
                        <button type="button" class="btn-attendance btn-absent ${absentActive}">Absent</button>
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
    showModal('#exportModal');
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
    hideModal('#exportModal');
}

// Performance
function loadPerformanceData() {
    console.log('Loading performance data...'); // Debug log
    
    if (!teacherCourseSectionId || teacherCourseSectionId === 0) {
        showPerformancePlaceholder('fas fa-exclamation-triangle', 'Missing section', 'Course section ID is not set.');
        return;
    }

    const $tbody = $('#performanceTableBody');
    const $thead = $('.performance-table thead');
    
    // Clear existing content and show loading
    $thead.empty();
    $tbody.html(`
        <tr>
            <td colspan="100%" class="loading-state">
                <i class="fas fa-spinner fa-spin"></i>
                <p>Loading performance data...</p>
            </td>
        </tr>
    `);

    console.log('Making AJAX request to /Teacher/GetPerformanceData with teacherCourseSectionId:', teacherCourseSectionId); // Debug log

    $.ajax({
        url: '/Teacher/GetPerformanceData',
        type: 'GET',
        data: { teacherCourseSectionId: teacherCourseSectionId },
        success: function (response) {
            console.log('Performance data response:', response); // Debug log
            if (response.success) {
                renderPerformanceTable(response.students, response.classworks);
            } else {
                console.error('Performance data error:', response.message);
                showPerformancePlaceholder('fas fa-exclamation-triangle', 'Error', response.message || 'Unable to load performance data.');
            }
        },
        error: function (xhr, status, error) {
            console.error('AJAX error loading performance data:', {
                status: status,
                error: error,
                responseText: xhr.responseText,
                statusCode: xhr.status
            });
            
            let errorMessage = 'Please refresh the page.';
            if (xhr.status === 403 || xhr.status === 401) {
                errorMessage = 'Access denied. Please refresh the page and try again.';
            } else if (xhr.status === 500) {
                errorMessage = 'Server error. Please try again later.';
            } else if (xhr.status === 404) {
                errorMessage = 'Performance data endpoint not found.';
            }
            
            showPerformancePlaceholder('fas fa-exclamation-triangle', 'Error loading data', errorMessage + ' Error: ' + error);
        }
    });
}

function renderPerformanceTable(students, classworks) {
    console.log('Rendering performance table with:', { students: students, classworks: classworks }); // Debug log
    
    const $table = $('.performance-table');
    const $thead = $table.find('thead');
    const $tbody = $('#performanceTableBody');

    // Clear existing headers and body
    $thead.empty();
    $tbody.empty();

    if (!classworks || classworks.length === 0) {
        console.log('No classworks found'); // Debug log
        showPerformancePlaceholder('fas fa-clipboard-list', 'No classworks', 'There are no classworks created for this section yet.');
        return;
    }

    if (!students || students.length === 0) {
        console.log('No students found'); // Debug log
        showPerformancePlaceholder('fas fa-user-slash', 'No students', 'There are no students enrolled in this section yet.');
        return;
    }

    console.log(`Rendering table for ${students.length} students and ${classworks.length} classworks`); // Debug log

    // Build table headers
    const $headerRow = $('<tr></tr>');
    $headerRow.append('<th style="min-width: 180px; position: sticky; left: 0; background: white; z-index: 10; border-right: 2px solid #dee2e6;">Student Name</th>');
    
    classworks.forEach(function(classwork) {
        $headerRow.append(`
            <th style="min-width: 120px; text-align: center;">
                <div style="font-weight: 600; font-size: 0.9rem; margin-bottom: 2px;">${escapeHtml(classwork.Title)}</div>
                <div style="font-size: 0.75rem; color: #666;">${classwork.MaxPoints} pts</div>
            </th>
        `);
    });

    $thead.append($headerRow);

    // Build table rows
    students.forEach(function(student) {
        let row = `
            <tr>
                <td style="font-weight: 600; position: sticky; left: 0; background: white; z-index: 5; border-right: 2px solid #f0f0f0;">
                    <div>${escapeHtml(student.StudentName)}</div>
                    <div style="font-size: 0.75rem; color: #666;">${escapeHtml(student.StudentNumber || '')}</div>
                </td>
        `;
        
        student.Grades.forEach(function(gradeData) {
            const gradeValue = gradeData.Grade !== null && gradeData.Grade !== undefined ? gradeData.Grade : 0;
            const maxPoints = gradeData.MaxPoints;
            const status = gradeData.Status || 'Not Submitted';
            
            let cellStyle = 'text-align: center; padding: 8px;';
            let cellContent = '';
            
            if (status === 'Not Submitted') {
                cellStyle += ' background-color: #fff3e0; color: #f57c00;';
                cellContent = `
                    <div class="grade-score editable-grade" 
                         data-original="0" 
                         data-max="${maxPoints}" 
                         data-student-id="${student.StudentId}" 
                         data-classwork-id="${gradeData.ClassworkId}"
                         style="color: #f57c00; font-weight: 600; cursor: pointer;">
                        0
                    </div>
                `;
            } else {
                const percentage = (gradeValue / maxPoints) * 100;
                let gradeColor = '#d32f2f'; // Red for failing
                if (percentage >= 90) gradeColor = '#2e7d32'; // Green for A
                else if (percentage >= 80) gradeColor = '#388e3c'; // Green for B
                else if (percentage >= 70) gradeColor = '#f57c00'; // Orange for C
                else if (percentage >= 60) gradeColor = '#ff9800'; // Orange for D
                
                cellContent = `
                    <div class="grade-score editable-grade" 
                         data-original="${gradeValue}" 
                         data-max="${maxPoints}" 
                         data-student-id="${student.StudentId}" 
                         data-classwork-id="${gradeData.ClassworkId}"
                         style="color: ${gradeColor}; font-weight: 600; cursor: pointer;">
                        ${gradeValue}
                    </div>
                `;
            }
            
            row += `<td style="${cellStyle}">${cellContent}</td>`;
        });
        
        row += '</tr>';
        $tbody.append(row);
    });
    
    // Make table horizontally scrollable while keeping student names sticky
    $table.parent().css({
        'overflow-x': 'auto',
        'position': 'relative'
    });
    
    console.log('Performance table rendered successfully'); // Debug log
}

function showPerformancePlaceholder(iconClass, title, message) {
    $('#performanceTableBody').html(`
        <tr>
            <td colspan="100%" class="no-students">
                <i class="${iconClass}"></i>
                <h5>${escapeHtml(title)}</h5>
                <p>${escapeHtml(message)}</p>
            </td>
        </tr>
    `);
}

// Export Performance Data to CSV
function exportPerformanceToCSV() {
    console.log('Exporting performance data to CSV...'); // Debug log
    
    // Check if performance data is loaded
    if ($('#performanceTableBody tr').length === 0 || $('#performanceTableBody .no-students').length > 0) {
        alert('No performance data to export. Please make sure the performance table is loaded with data.');
        return;
    }

    const rows = [];
    const headers = ['Student Name', 'Student Number'];
    
    // Get classwork headers
    $('.performance-table thead th').each(function(index) {
        if (index > 0) { // Skip the first column (Student Name)
            const title = $(this).find('div:first').text().trim();
            const points = $(this).find('div:nth-child(2)').text().trim();
            const type = $(this).find('div:nth-child(3)').text().trim();
            headers.push(`${title} ${points} (${type})`);
        }
    });
    
    rows.push(headers);
    
    // Get student data
    $('#performanceTableBody tr').each(function() {
        const $row = $(this);
        if (!$row.hasClass('no-students') && $row.find('td').length > 1) {
            const rowData = [];
            
            // Get student name and number
            const studentCell = $row.find('td:first');
            const studentName = studentCell.find('div:first').text().trim();
            const studentNumber = studentCell.find('div:last').text().trim();
            rowData.push(studentName, studentNumber);
            
            // Get grades
            $row.find('td:not(:first)').each(function() {
                const gradeValue = $(this).find('.grade-score').text().trim();
                rowData.push(gradeValue || '0');
            });
            
            rows.push(rowData);
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
    
    const today = new Date().toISOString().split('T')[0];
    const filename = `Performance_Overview_${today}.csv`;
    
    link.setAttribute('href', url);
    link.setAttribute('download', filename);
    link.style.visibility = 'hidden';
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    
    console.log('Performance data exported successfully'); // Debug log
}

// Manual Classwork Addition Functions
function initializeManualClassworkAddition() {
    $('#addManualClassworkBtn').on('click', function() {
        showModal('#addClassworkModal');
    });

    $('#addClassworkSave').on('click', function() {
        saveManualClasswork();
    });

    $('#addClassworkCancel').on('click', function() {
        hideModal('#addClassworkModal');
        resetClassworkForm();
    });

    // Enable/disable save button based on form validation
    $('#addClassworkForm input, #addClassworkForm select').on('input change', function() {
        validateClassworkForm();
    });

    // Export Performance Data button
    $('#exportPerformanceBtn').on('click', function() {
        exportPerformanceToCSV();
    });
}

function validateClassworkForm() {
    const title = $('#classworkTitle').val().trim();
    const type = $('#classworkTypeSelect').val();
    const points = parseInt($('#classworkPoints').val());
    
    const isValid = title && type && points > 0;
    $('#addClassworkSave').prop('disabled', !isValid);
}

function saveManualClasswork() {
    const title = $('#classworkTitle').val().trim();
    const type = $('#classworkTypeSelect').val();
    const points = parseInt($('#classworkPoints').val());

    var token = $('input[name="__RequestVerificationToken"]').val();
    
    $.ajax({
        url: '/Teacher/AddManualClasswork',
        type: 'POST',
        data: {
            __RequestVerificationToken: token,
            teacherCourseSectionId: teacherCourseSectionId,
            title: title,
            classworkType: type,
            points: points
        },
        success: function(response) {
            if (response.success) {
                alert(response.message);
                hideModal('#addClassworkModal');
                resetClassworkForm();
                // Reload performance data to show new column
                loadPerformanceData();
            } else {
                alert('Error: ' + response.message);
            }
        },
        error: function() {
            alert('Error adding classwork');
        }
    });
}

function resetClassworkForm() {
    $('#addClassworkForm')[0].reset();
    $('#addClassworkSave').prop('disabled', true);
}

// Modal helper functions
function showModal(modalSelector) {
    $(modalSelector).css({
        'display': 'flex',
        'opacity': '0'
    }).animate({ opacity: 1 }, 200);
}

function hideModal(modalSelector) {
    $(modalSelector).animate({ opacity: 0 }, 200, function() {
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

        // Don't allow editing if no value (represents no classwork submission)
        if (currentValue === '-') return;

        const $input = $('<input>', {
            type: 'number',
            class: 'grade-input',
            value: currentValue || '',
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
                $gradeSpan.text(currentValue || '-').removeClass('editing');
            }
        });

        $gradeSpan.addClass('editing');
    });

    function saveGradeEdit($gradeSpan, $input, autoSave) {
        const newValue = parseFloat($input.val());
        const originalValue = parseFloat($gradeSpan.data('original')) || null;
        const maxPoints = parseFloat($gradeSpan.data('max')) || 100;

        if (isNaN(newValue) || newValue < 0 || newValue > maxPoints) {
            alert(`Please enter a valid grade between 0 and ${maxPoints}`);
            $gradeSpan.text(originalValue !== null ? originalValue : '-').removeClass('editing');
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
        const classworkId = $gradeSpan.data('classwork-id');

        $gradeSpan.addClass('saving');

        $.ajax({
            url: '/Teacher/SaveGrade',
            type: 'POST',
            data: {
                studentId: studentId,
                classworkId: classworkId,
                grade: newValue
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
                alert('An error occurred while saving the grade.');
                $gradeSpan.removeClass('saving');
            }
        });
    }
}

function escapeHtml(text) {
    if (!text) return '';
    const map = { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#039;' };
    return text.replace(/[&<>"']/g, function (m) { return map[m]; });
}

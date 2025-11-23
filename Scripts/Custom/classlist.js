$(document).ready(function () {

    loadClassList();
    initializeViewSwitcher();
    initializeAttendance();
    initializeEditableGrades();

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
    updateAttendanceCounts();

    $('.btn-attendance').on('click', function () {
        const $row = $(this).closest('tr');
        $row.find('.btn-attendance').removeClass('active');
        $(this).addClass('active');
        updateAttendanceCounts();
    });

    $('.btn-mark-all.btn-success').on('click', function () {
        $('.btn-present').addClass('active');
        $('.btn-absent').removeClass('active');
        updateAttendanceCounts();
    });

    $('.btn-mark-all.btn-danger').on('click', function () {
        $('.btn-absent').addClass('active');
        $('.btn-present').removeClass('active');
        updateAttendanceCounts();
    });

    $('.btn-clear').on('click', function () {
        $('.btn-attendance').removeClass('active');
        updateAttendanceCounts();
    });

    function updateAttendanceCounts() {
        const total = $('#attendanceTableBody tr').length;
        const present = $('.btn-present.active').length;
        const absent = $('.btn-absent.active').length;
        const unmarked = total - (present + absent);

        $('#totalStudents').text(total);
        $('#presentCount').text(present);
        $('#absentCount').text(absent);
        $('#unmarkedCount').text(unmarked);
    }
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
        const studentName = $gradeSpan.closest('tr').find('.student-name').text().trim();

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
                // Simulate success for now
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

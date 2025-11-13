// Delete Curriculum Handler
// Handles deletion of curriculum groups (Program, Year Level, Semester)

(function () {
    'use strict';

    // Global variable to store curriculum to be deleted
    let curriculumToDelete = null;

    // Initialize delete curriculum functionality
    function initDeleteCurriculum() {
        const modal = document.getElementById('deleteCurriculumModal');
        const confirmBtn = document.getElementById('confirmDeleteCurriculumBtn');

        if (!modal || !confirmBtn) {
            console.warn('Delete curriculum modal or confirm button not found');
            return;
        }

        // Handle confirm delete button click
        confirmBtn.addEventListener('click', function () {
            if (!curriculumToDelete) {
                if (typeof showAlert === 'function') {
                    showAlert('danger', 'No curriculum selected for deletion.');
                }
                return;
            }

            const originalHtml = confirmBtn.innerHTML;

            // Disable button and show loading
            confirmBtn.disabled = true;
            confirmBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Deleting...';

            // Get anti-forgery token
            const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
            const token = tokenInput ? tokenInput.value : '';

            // Prepare form data
            const formData = new FormData();
            formData.append('__RequestVerificationToken', token);
            formData.append('programId', curriculumToDelete.programId);
            formData.append('yearLevel', curriculumToDelete.yearLevel);
            formData.append('semester', curriculumToDelete.semester);

            // Perform AJAX delete request
            fetch('/Curriculum/DeleteCurriculumGroup', {
                method: 'POST',
                body: formData,
                headers: {
                    'X-Requested-With': 'XMLHttpRequest'
                }
            })
                .then(response => response.json())
                .then(data => {
                    // Hide modal
                    if (typeof bootstrap !== 'undefined') {
                        bootstrap.Modal.getInstance(modal)?.hide();
                    } else {
                        $(modal).modal('hide');
                    }

                    // Re-enable button
                    confirmBtn.disabled = false;
                    confirmBtn.innerHTML = originalHtml;

                    if (data.success) {
                        // Find and remove the row
                        const rowSelector = `.curriculum-group-row[data-program-id="${curriculumToDelete.programId}"][data-year="${curriculumToDelete.yearLevel}"][data-semester="${curriculumToDelete.semester}"]`;
                        const row = document.querySelector(rowSelector);

                        if (row) {
                            // Fade out and remove row
                            row.style.transition = 'opacity 0.4s';
                            row.style.opacity = '0';

                            setTimeout(() => {
                                row.remove();

                                // Check if table is empty
                                const remainingRows = document.querySelectorAll('.curriculum-group-row');
                                if (remainingRows.length === 0) {
                                    const tableBody = document.getElementById('curriculumTableBody');
                                    if (tableBody) {
                                        tableBody.innerHTML = `
                                            <tr id="noDataRow">
                                                <td colspan="5" class="text-center" style="padding: 3rem; color: var(--gray-text);">
                                                    <i class="fas fa-inbox" style="font-size: 3rem; color: #dee2e6; margin-bottom: 1rem; display: block;"></i>
                                                    <h5 style="margin-bottom: 0.5rem;">No curriculum courses assigned yet</h5>
                                                    <p style="margin: 0;">Click "Assign Courses" button to start adding courses to your curriculum.</p>
                                                </td>
                                            </tr>
                                        `;
                                    }
                                }

                                // Update pagination info
                                if (typeof updatePaginationInfo === 'function') {
                                    updatePaginationInfo();
                                }
                            }, 400);
                        }

                        // Show success message
                        if (typeof showAlert === 'function') {
                            showAlert('success', data.message);
                        }
                    } else {
                        // Show error message
                        if (typeof showAlert === 'function') {
                            showAlert('danger', data.message || 'Failed to delete curriculum group.');
                        }
                    }

                    // Clear stored data
                    curriculumToDelete = null;
                })
                .catch(error => {
                    // Hide modal
                    if (typeof bootstrap !== 'undefined') {
                        bootstrap.Modal.getInstance(modal)?.hide();
                    } else {
                        $(modal).modal('hide');
                    }

                    // Re-enable button
                    confirmBtn.disabled = false;
                    confirmBtn.innerHTML = originalHtml;

                    console.error('Delete error:', error);

                    if (typeof showAlert === 'function') {
                        showAlert('danger', 'An error occurred while deleting the curriculum group. Please try again.');
                    }

                    // Clear stored data
                    curriculumToDelete = null;
                });
        });

        // Clear data when modal is hidden
        modal.addEventListener('hidden.bs.modal', function () {
            curriculumToDelete = null;
            confirmBtn.disabled = false;
            confirmBtn.innerHTML = '<i class="fas fa-trash"></i> Delete Curriculum';
        });
    }

    // Global function to trigger delete curriculum
    window.deleteCurriculum = function (programId, yearLevel, semester, programCode, programName, yearLevelText, semesterName, courseCount) {
        // Store curriculum info for deletion
        curriculumToDelete = {
            programId: programId,
            yearLevel: yearLevel,
            semester: semester,
            programCode: programCode
        };

        // Populate modal with curriculum details
        const programNameEl = document.getElementById('deleteProgramName');
        const detailsEl = document.getElementById('deleteCurriculumDetails');
        const courseCountEl = document.getElementById('deleteCourseCount');

        if (programNameEl) {
            programNameEl.textContent = programCode + ' - ' + programName;
        }

        if (detailsEl) {
            detailsEl.textContent = yearLevelText + ' - ' + semesterName;
        }

        if (courseCountEl) {
            courseCountEl.textContent = courseCount + ' course(s)';
        }

        // Show the modal
        const modal = document.getElementById('deleteCurriculumModal');
        if (modal) {
            if (typeof bootstrap !== 'undefined') {
                new bootstrap.Modal(modal).show();
            } else if (typeof $ !== 'undefined') {
                $(modal).modal('show');
            }
        }
    };

    // Initialize on DOM ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initDeleteCurriculum);
    } else {
        initDeleteCurriculum();
    }

})();

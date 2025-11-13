// Section Management - Form Handling
$(document).ready(function () {
    console.log("Section page loaded");

    // Section form submission handler
    $('#createSectionForm').on('submit', function (e) {
        e.preventDefault();
        console.log("Form submitted");

        $('#formErrorContainer').addClass('d-none');

        var sectionId = $('#sectionId').val();
        var baseUrl = window.location.origin;
        var url = sectionId ? baseUrl + '/Section/Edit' : baseUrl + '/Section/Create';
        var formData = $(this).serialize();

        console.log("URL:", url);
        console.log("Form Data:", formData);

        $.ajax({
            url: url,
            type: 'POST',
            data: formData,
            success: function (response) {
                console.log("Response:", response);
                if (response.success) {
                    $('#createSectionModal').modal('hide');
                    showAlert('success', response.message);
                    // Reload page to show new section
                    setTimeout(function () {
                        location.reload();
                    }, 1500);
                } else {
                    $('#formErrorMessage').text(response.message);
                    $('#formErrorContainer').removeClass('d-none');
                }
            },
            error: function (xhr, status, error) {
                console.error("Error:", error);
                console.error("Response:", xhr.responseText);
                var message = xhr.responseJSON?.message || 'An error occurred while saving the section.';
                $('#formErrorMessage').text(message);
                $('#formErrorContainer').removeClass('d-none');
            }
        });
    });

    // Edit button handler
    $(document).on('click', '.edit-btn', function () {
        var sectionId = $(this).data('id');
        var baseUrl = window.location.origin;
        console.log("Edit clicked for section:", sectionId);

        $.ajax({
            url: baseUrl + '/Section/GetById',
            type: 'GET',
            data: { id: sectionId },
            success: function (response) {
                console.log("Edit data:", response);
                if (response.success) {
                    $('#modalTitle').text('Edit Section');
                    $('#sectionId').val(response.data.id);
                    $('#sectionName').val(response.data.sectionName);
                    $('#department').val(response.data.departmentId);

                    // Use the helper function from sectionDropdown.js
                    if (typeof loadProgramsForEdit === 'function') {
                        loadProgramsForEdit(response.data.departmentId, response.data.programId, response.data.yearLevel);
                    }

                    $('#btnText').text('Update');
                    $('#sectionIcon').removeClass('fa-save').addClass('fa-edit');
                    $('#createSectionModal').modal('show');
                } else {
                    showAlert('danger', response.message);
                }
            },
            error: function (xhr) {
                console.error("Edit error:", xhr);
                showAlert('danger', 'Failed to load section details');
            }
        });
    });

    // Reset form when modal is hidden
    $('#createSectionModal').on('hidden.bs.modal', function () {
        $('#createSectionForm')[0].reset();
        $('#sectionId').val('');
        $('#modalTitle').text('Create New Section');
        $('#btnText').text('Create');
        $('#sectionIcon').removeClass('fa-edit').addClass('fa-save');
        $('#formErrorContainer').addClass('d-none');

        // Call the reset function from sectionDropdown.js
        if (typeof resetSectionDropdowns === 'function') {
            resetSectionDropdowns();
        }
    });
});

class FormHandler {
    constructor(config) {
        this.config = {
            formId: config.formId,
            modalId: config.modalId,
            submitBtnId: config.submitBtnId,
            tableBodyId: config.tableBodyId,
            modalTitleId: config.modalTitleId,
            btnTextId: config.btnTextId,
            iconId: config.iconId,
            errorContainerId: config.errorContainerId,
            errorMessageId: config.errorMessageId,
            idFieldId: config.idFieldId,
            createUrl: config.createUrl,
            editUrl: config.editUrl,
            getDataUrl: config.getDataUrl,
            deleteUrl: config.deleteUrl,
            createTriggerSelector: config.createTriggerSelector,
            editBtnSelector: config.editBtnSelector || '.edit-btn',
            entityName: config.entityName,
            createTitle: config.createTitle || `Create New ${config.entityName}`,
            editTitle: config.editTitle || `Edit ${config.entityName}`,
            createBtnText: config.createBtnText || 'Create',
            editBtnText: config.editBtnText || 'Update',
            fillFormFields: config.fillFormFields, // Function to fill form with data
            buildNewRow: config.buildNewRow, // Function to build HTML for new row
            updateExistingRow: config.updateExistingRow // Function to update existing row
        };

        this.isEdit = false;
        this.init();
    }

    init() {
        this.bindFormSubmit();
        this.bindEditButtons();
        this.bindCreateTrigger();
        this.bindModalClose();
    }

    bindFormSubmit() {
        const $form = $(`#${this.config.formId}`);
        const $submitBtn = $(`#${this.config.submitBtnId}`);
        const $icon = $(`#${this.config.iconId}`);
        const $errorContainer = $(`#${this.config.errorContainerId}`);
        const $errorMessage = $(`#${this.config.errorMessageId}`);

        $form.on('submit', (e) => {
            e.preventDefault();

            // Special validation for Student role (only for user account form)
            if (this.config.formId === 'createAccountForm') {
                const role = $('#role').val();
                
                console.log('=== FORM VALIDATION DEBUG ===');
                console.log('Role selected:', role);
                
                if (role === 'Student') {
                    const programId = $('#program').val();
                    const yearLevel = $('#yearLevel').val();
                    const sectionId = $('#studentSection').val();
                    
                    // Check if fields are disabled
                    const programDisabled = $('#program').prop('disabled');
                    const yearLevelDisabled = $('#yearLevel').prop('disabled');
                    const sectionDisabled = $('#studentSection').prop('disabled');
                    
                    console.log('Student Fields Check:');
                    console.log('  Program ID:', programId, '(disabled:', programDisabled, ')');
                    console.log('  Year Level:', yearLevel, '(disabled:', yearLevelDisabled, ')');
                    console.log('  Section ID:', sectionId, '(disabled:', sectionDisabled, ')');
                    console.log('  Program field exists:', $('#program').length > 0);
                    console.log('  Year Level field exists:', $('#yearLevel').length > 0);
                    console.log('  Section field exists:', $('#studentSection').length > 0);
                    
                    // Only validate if fields are NOT disabled
                    if (!programDisabled && !yearLevelDisabled && !sectionDisabled) {
                        if (!programId || !yearLevel || !sectionId) {
                            console.error('VALIDATION FAILED - Missing fields:');
                            if (!programId) console.error('  - Program is missing or empty');
                            if (!yearLevel) console.error('  - Year Level is missing or empty');
                            if (!sectionId) console.error('  - Section is missing or empty');
                            
                            $errorContainer.removeClass('d-none');
                            $errorMessage.text('Please fill in all student-specific fields (Program, Year Level, and Section).');
                            console.log('=== END VALIDATION ===');
                            return false;
                        }
                    }
                    
                    console.log('✓ All student fields validated successfully');
                }
                
                console.log('✓ Validation passed, proceeding with form submission');
                console.log('=== END VALIDATION ===');
            }

            // Disable submit button and show loading
            $submitBtn.prop('disabled', true);
            $icon.removeClass('fa-upload fa-save fa-edit').addClass('fa-spinner fa-spin');
            $errorContainer.addClass('d-none');

            const formData = $form.serialize();
            console.log('Form data being sent:', formData);
            
            // Parse and display form data in a readable format
            const formDataArray = formData.split('&');
            console.log('=== FORM DATA BREAKDOWN ===');
            formDataArray.forEach(item => {
                const [key, value] = item.split('=');
                console.log(`  ${decodeURIComponent(key)}: ${decodeURIComponent(value)}`);
            });
            console.log('=== END FORM DATA ===');
            
            const url = this.isEdit ? this.config.editUrl : this.config.createUrl;
            console.log('Submitting to URL:', url);

            $.ajax({
                url: url,
                type: 'POST',
                data: formData,
                success: (response) => {
                    console.log('Server response:', response);
                    if (response.success) {
                        // Close modal
                        $(`#${this.config.modalId}`).modal('hide');
                        
                        // Show success message
                        showAlert('success', response.message);

                        // Update or add row
                        if (this.isEdit) {
                            this.updateRow(response.userData || response.data);
                        } else {
                            this.addNewRow(response.userData || response.data);
                        }

                        this.resetForm();
                    } else {
                        // Show error in modal
                        console.error('Server returned error:', response.message);
                        $errorContainer.removeClass('d-none');
                        $errorMessage.text(response.message);
                    }
                },
                error: (xhr) => {
                    console.error('AJAX error:', xhr.status, xhr.statusText);
                    console.error('Response text:', xhr.responseText);
                    $errorContainer.removeClass('d-none');
                    $errorMessage.text('An error occurred. Please try again.');
                },
                complete: () => {
                    // Re-enable submit button
                    $submitBtn.prop('disabled', false);
                    
                    // Restore icon based on mode
                    $icon.removeClass('fa-spinner fa-spin');
                    if (this.isEdit) {
                        $icon.addClass('fa-save');
                    } else {
                        $icon.addClass('fa-upload fa-save');
                    }
                }
            });
        });
    }

    bindEditButtons() {
        const $tableBody = $(`#${this.config.tableBodyId}`);
        
        $tableBody.on('click', this.config.editBtnSelector, (e) => {
            const itemId = $(e.currentTarget).data('id');
            this.loadDataForEdit(itemId);
        });
    }

    loadDataForEdit(itemId) {
        const $modal = $(`#${this.config.modalId}`);
        const $modalTitle = $(`#${this.config.modalTitleId}`);
        const $btnText = $(`#${this.config.btnTextId}`);
        const $icon = $(`#${this.config.iconId}`);

        $.ajax({
            url: this.config.getDataUrl,
            type: 'GET',
            data: { id: itemId },
            success: (data) => {
                console.log('Received data:', data); // Debug log
                
                if (data.success) {
                    this.isEdit = true;
                    
                    // Fill form using custom function
                    if (this.config.fillFormFields) {
                        this.config.fillFormFields(data.user || data.data);
                    }
                    
                    $modalTitle.text(this.config.editTitle);
                    $btnText.text(this.config.editBtnText);
                    $icon.removeClass('fa-upload fa-save').addClass('fa-edit fa-save');
                    $modal.modal('show');
                } else {
                    console.error('Error from server:', data.message); // Debug log
                    showAlert('danger', `Error loading ${this.config.entityName.toLowerCase()} data: ${data.message}`);
                }
            },
            error: (xhr, status, error) => {
                console.error('Ajax error:', xhr, status, error); // Debug log
                console.error('Response text:', xhr.responseText); // Debug log
                showAlert('danger', `Error loading ${this.config.entityName.toLowerCase()} data`);
            }
        });
    }

    bindCreateTrigger() {
        if (this.config.createTriggerSelector) {
            $(this.config.createTriggerSelector).on('click', () => {
                this.openCreateModal();
            });
        }
    }

    openCreateModal() {
        const $modalTitle = $(`#${this.config.modalTitleId}`);
        const $btnText = $(`#${this.config.btnTextId}`);
        const $icon = $(`#${this.config.iconId}`);

        this.isEdit = false;
        $modalTitle.text(this.config.createTitle);
        $btnText.text(this.config.createBtnText);
        $icon.removeClass('fa-save fa-edit').addClass('fa-upload fa-save');
        this.resetForm();
    }

    bindModalClose() {
        const $modal = $(`#${this.config.modalId}`);
        $modal.on('hidden.bs.modal', () => {
            this.resetForm();
        });
    }

    updateRow(data) {
        if (this.config.updateExistingRow) {
            this.config.updateExistingRow(data);
        }
    }

    addNewRow(data) {
        if (this.config.buildNewRow) {
            const newRowHtml = this.config.buildNewRow(data, this.config.deleteUrl);
            
            // Remove "no data" row if exists
            $(`#${this.config.tableBodyId}`).find('tr[id*="noData"], tr[id*="noUsers"]').remove();
            
            // Append new row
            $(`#${this.config.tableBodyId}`).append(newRowHtml);
        }
    }

    resetForm() {
        const $form = $(`#${this.config.formId}`);
        const $errorContainer = $(`#${this.config.errorContainerId}`);
        const $errorMessage = $(`#${this.config.errorMessageId}`);
        
        $form[0].reset();
        
        if (this.config.idFieldId) {
            $(`#${this.config.idFieldId}`).val('');
        }
        
        $errorContainer.addClass('d-none');
        $errorMessage.text('');
        this.isEdit = false;
    }
}

// ============================
// USER ACCOUNT FORM HANDLER
// ============================
function initUserAccountForm() {
    const $form = $('#createAccountForm');
    
    new FormHandler({
        formId: 'createAccountForm',
        modalId: 'manualAccountModal',
        submitBtnId: 'submitAccountBtn',
        tableBodyId: 'userTableBody',
        modalTitleId: 'modalTitle',
        btnTextId: 'btnText',
        iconId: 'uploadIcon',
        errorContainerId: 'formErrorContainer',
        errorMessageId: 'formErrorMessage',
        idFieldId: 'editId',
        createUrl: $form.data('create-url'),
        editUrl: $form.data('edit-url'),
        getDataUrl: $form.data('get-user-url'),
        deleteUrl: $('#userTableBody').data('delete-url'),
        createTriggerSelector: null,
        entityName: 'Account',
        createTitle: 'Create Account Manually',
        editTitle: 'Edit Account',
        
        fillFormFields: (user) => {
            $('#editId').val(user.id);
            $('#firstName').val(user.firstName);
            $('#lastName').val(user.lastName);
            $('#userId').val(user.userId);
            $('#email').val(user.email);
            $('#phone').val(user.phoneNumber);
            $('#role').val(user.role);
            
            if (user.role === 'Student') {
                $('#studentFieldsContainer').show();
                if (user.programId && user.yearLevel && user.sectionId) {
                    setTimeout(function() {
                        setTimeout(function() {
                            $('#program').val(user.programId).trigger('change');
                            
                            setTimeout(function() {
                                $('#yearLevel').val(user.yearLevel).trigger('change');
                                
                                setTimeout(function() {
                                    $('#studentSection').val(user.sectionId);
                                }, 300);
                            }, 300);
                        }, 300);
                    }, 100);
                }
            }
        },
        
        updateExistingRow: (user) => {
            const $row = $(`.edit-btn[data-id="${user.id}"]`).closest('tr');
            $row.find('.user-name').text(`${user.lastName} ${user.firstName}`);
            $row.find('.user-email').text(user.email);
            $row.find('.user-phone').text(user.userId);
            $row.find('.role-tag').text(user.role);
        },

        
        buildNewRow: (user, deleteUrl) => {
            return `
            <tr class="default-row">
                <td class="default-info">
                    <div class="user-details">
                        <div class="user-name">${user.lastName} ${user.firstName}</div>
                        <div class="user-email">${user.email}</div>
                        <div class="user-phone">${user.userId}</div>
                    </div>
                </td>
                <td><span class="role-tag role-student">${user.role}</span></td>
                <td>
                    <span class="status-badge status-active">Active</span>
                </td>
                <td class="last-login">
                    <div>New Account</div>
                    <div class="login-time">Not logged in yet</div>
                </td>
                <td class="action-buttons">
                    <button class="action-btn edit-btn" data-id="${user.id}" title="Edit">
                        <i class="fa-solid fa-pen"></i>
                    </button>
                    <button type="button" class="action-btn delete-btn"
                            data-bs-toggle="modal"
                            data-bs-target="#deleteModal"
                            data-item-id="${user.id}"
                            data-item-name="${user.lastName} ${user.firstName}"
                            data-extra="${user.role}"
                            data-target-remove=".default-row"
                            data-url="${deleteUrl}"
                            title="Delete">
                        <i class="fa-solid fa-trash"></i>
                    </button>
                </td>
            </tr>`;
        }
    });

    $('.role-choice-btn').on('click', function() {
        const selectedRole = $(this).data('role');
        console.log('Role selected from modal:', selectedRole);
        
        $('#role').val(selectedRole);
        
        $('#modalTitle').text(`Create ${selectedRole} Account`);
        
        if (selectedRole === 'Student') {
            $('#studentFieldsContainer').show();
            // Enable the program dropdown immediately since programs are pre-loaded
            $('#program').prop('disabled', false);
            // Year level and section remain disabled until program is selected
            $('#yearLevel, #studentSection').prop('disabled', true);
        } else {
            $('#studentFieldsContainer').hide();
            $('#program, #yearLevel, #studentSection').prop('disabled', true);
        }
        
        $('#roleSelectionModal').modal('hide');
        
        $('#roleSelectionModal').on('hidden.bs.modal', function() {
            $('#manualAccountModal').modal('show');
            $(this).off('hidden.bs.modal');
        });
    });

    $('#manualAccountModal').on('hidden.bs.modal', function() {
        $('#createAccountForm')[0].reset();
        $('#role').val('');
        $('#studentFieldsContainer').hide();
        $('#program, #yearLevel, #studentSection').prop('disabled', true);
        $('#formErrorContainer').addClass('d-none');
    });
}

// ============================
// PROGRAM FORM HANDLER
// ============================
function initProgramForm() {
    new FormHandler({
        formId: 'createProgramForm',
        modalId: 'createProgramModal',
        submitBtnId: 'submitProgramBtn',
        tableBodyId: 'programTableBody',
        modalTitleId: 'modalTitle',
        btnTextId: 'btnText',
        iconId: 'programIcon',
        errorContainerId: 'formErrorContainer',
        errorMessageId: 'formErrorMessage',
        idFieldId: 'programId',
        createUrl: '/Program/Create',
        editUrl: '/Program/Edit',
        getDataUrl: '/Program/GetById',
        deleteUrl: '/Program/Delete',
        createTriggerSelector: 'button[data-bs-target="#createProgramModal"]',
        entityName: 'Program',
        
        fillFormFields: (program) => {
            console.log('Filling form with program data:', program); // Debug log
            $('#programId').val(program.id);
            $('#programName').val(program.programName);
            $('#programCode').val(program.programCode);
            $('#department').val(program.departmentId);
            $('#programDuration').val(program.programDuration);
        },
        
        updateExistingRow: (program) => {
            const $row = $(`.edit-btn[data-id="${program.id}"]`).closest('tr');
            $row.attr('data-department', program.departmentCode);
            $row.find('.user-name').text(program.programName);
            $row.find('.role-tag').text(program.programCode);
            $row.find('td:eq(2)').text(program.departmentCode);
            $row.find('td:eq(3)').text(`${program.programDuration} Years`);
        },
        
        buildNewRow: (program, deleteUrl) => {
            const dateCreated = new Date().toLocaleDateString('en-US', { month: '2-digit', day: '2-digit', year: 'numeric' });
            const timeCreated = new Date().toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit' });
            
            return `
            <tr class="default-row" data-department="${program.departmentCode}">
                <td class="default-info">
                    <div class="user-details">
                        <div class="user-name">${program.programName}</div>
                    </div>
                </td>
                <td><span class="role-tag">${program.programCode}</span></td>
                <td>${program.departmentCode}</td>
                <td>${program.programDuration} Years</td>
                <td class="last-login">
                    <div>${dateCreated}</div>
                    <div class="login-time">${timeCreated}</div>
                </td>
                <td class="action-buttons">
                    <button class="action-btn edit-btn" data-id="${program.id}" title="Edit">
                        <i class="fa-solid fa-pen"></i>
                    </button>
                    <button type="button" class="action-btn delete-btn"
                            data-bs-toggle="modal"
                            data-bs-target="#deleteModal"
                            data-item-id="${program.id}"
                            data-item-name="${program.programName}"
                            data-target-remove=".default-row"
                            title="Delete">
                        <i class="fa-solid fa-trash"></i>
                    </button>
                </td>
            </tr>`;
        }
    });
}

// ============================
// DEPARTMENT FORM HANDLER
// ============================
function initDepartmentForm() {
    new FormHandler({
        formId: 'createDepartmentForm',
        modalId: 'createDepartmentModal',
        submitBtnId: 'submitDepartmentBtn',
        tableBodyId: 'departmentTableBody',
        modalTitleId: 'deptModalTitle',
        btnTextId: 'deptBtnText',
        iconId: 'deptIcon',
        errorContainerId: 'deptFormErrorContainer',
        errorMessageId: 'deptFormErrorMessage',
        idFieldId: 'departmentId',
        createUrl: '/Department/Create',
        editUrl: '/Department/Edit',
        getDataUrl: '/Department/GetById',
        deleteUrl: '/Department/Delete',
        createTriggerSelector: 'button[data-bs-target="#createDepartmentModal"]',
        entityName: 'Department',
        
        fillFormFields: (department) => {
            $('#departmentId').val(department.id);
            $('#departmentName').val(department.departmentName);
            $('#departmentCode').val(department.departmentCode);
            $('#departmentDescription').val(department.description);
            $('#departmentStatus').val(department.status);
        },
        
        updateExistingRow: (department) => {
            const $row = $(`.edit-btn[data-id="${department.id}"]`).closest('tr');
            const statusClass = department.status === 'Active' ? 'status-active' : 'status-inactive';
            
            $row.attr('data-status', department.status);
            $row.find('.user-name').text(department.departmentName);
            $row.find('.role-tag').text(department.departmentCode);
            $row.find('td:eq(2)').text(department.description || '');
            $row.find('.status-badge')
                .removeClass('status-active status-inactive')
                .addClass(statusClass)
                .text(department.status);
        },
        
        buildNewRow: (department, deleteUrl) => {
            const dateCreated = new Date().toLocaleDateString('en-US', { month: '2-digit', day: '2-digit', year: 'numeric' });
            const timeCreated = new Date().toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit' });
            const statusClass = department.status === 'Active' ? 'status-active' : 'status-inactive';
            
            return `
            <tr class="default-row" data-status="${department.status}">
                <td class="default-info">
                    <div class="user-details">
                        <div class="user-name">${department.departmentName}</div>
                    </div>
                </td>
                <td><span class="role-tag">${department.departmentCode}</span></td>
                <td class="last-login">
                    <div>${dateCreated}</div>
                </td>
                <td class="action-buttons">
                    <button class="action-btn edit-btn" data-id="${department.id}" title="Edit">
                        <i class="fa-solid fa-pen"></i>
                    </button>
                    <button type="button" class="action-btn delete-btn"
                            data-bs-toggle="modal"
                            data-bs-target="#deleteModal"
                            data-item-id="${department.id}"
                            data-item-name="${department.departmentName}"
                            data-target-remove=".default-row"
                            title="Delete">
                        <i class="fa-solid fa-trash"></i>
                    </button>
                </td>
            </tr>`;
        }
    });
}

// ============================
// AUTO-INITIALIZATION
// ============================
$(function () {
    // Auto-detect and initialize forms
    if ($('#createAccountForm').length) {
        initUserAccountForm();
    }
    
    if ($('#createProgramForm').length) {
        initProgramForm();
    }
    
    if ($('#createDepartmentForm').length) {
        initDepartmentForm();
    }
    
    if ($('#createSectionForm').length) {
        initSectionForm();
    }
});

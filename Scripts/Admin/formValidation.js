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

            // Disable submit button and show loading
            $submitBtn.prop('disabled', true);
            $icon.removeClass('fa-upload fa-save').addClass('fa-spinner fa-spin');
            $errorContainer.addClass('d-none');

            const formData = $form.serialize();
            const url = this.isEdit ? this.config.editUrl : this.config.createUrl;

            $.ajax({
                url: url,
                type: 'POST',
                data: formData,
                success: (response) => {
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
                        $errorContainer.removeClass('d-none');
                        $errorMessage.text(response.message);
                    }
                },
                error: (xhr) => {
                    $errorContainer.removeClass('d-none');
                    $errorMessage.text('An error occurred. Please try again.');
                },
                complete: () => {
                    // Re-enable submit button
                    $submitBtn.prop('disabled', false);
                    $icon.removeClass('fa-spinner fa-spin').addClass('fa-upload');
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
                    $icon.removeClass('fa-upload').addClass('fa-save');
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
        $icon.removeClass('fa-save').addClass('fa-upload');
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
        createTriggerSelector: 'a[data-bs-target="#manualAccountModal"]',
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
            $('#department').val(user.department);
        },
        
        updateExistingRow: (user) => {
            const $row = $(`.edit-btn[data-id="${user.id}"]`).closest('tr');
            $row.find('.user-name').text(`${user.lastName} ${user.firstName}`);
            $row.find('.user-email').text(user.email);
            $row.find('.user-phone').text(user.userId);
            $row.find('.role-tag').text(user.role);
            $row.find('td:eq(2)').text(user.department);
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
                <td>${user.department}</td>
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
});

$(function () {
    const $form = $('#createAccountForm');
    const createAccountUrl = $form.data('create-url');
    const editAccountUrl = $form.data('edit-url');
    const getUserDataUrl = $form.data('get-user-url');

    initAccountForm(createAccountUrl, editAccountUrl);

    setupEditButtons(getUserDataUrl);
});

function initAccountForm(createUrl, editUrl) {
    const $form = $('#createAccountForm');
    const $submitBtn = $('#submitAccountBtn');
    const $formErrorContainer = $('#formErrorContainer');
    const $formErrorMessage = $('#formErrorMessage');
    
    window.formState = { 
        isEdit: false 
    };

    $form.on('submit', function (e) {
        e.preventDefault();

        $submitBtn.prop('disabled', true);
        $('#uploadIcon').removeClass('fa-upload').addClass('fa-spinner fa-spin');

        const formData = $(this).serialize();
        const url = window.formState.isEdit ? editUrl : createUrl;

        $.ajax({
            url: url,
            type: 'POST',
            data: formData,
            success: function (response) {
                if (response.success) {
                    $('#manualAccountModal').modal('hide');

                    showAlert('success', response.message);

                    if (window.formState.isEdit) {
                        updateUserRow(response.userData);
                    } else {
                        addNewUserRow(response.userData);
                    }

                    resetForm();
                } else {
                    $formErrorContainer.removeClass('d-none');
                    $formErrorMessage.text(response.message);
                }
            },
            error: function (xhr) {
                $formErrorContainer.removeClass('d-none');
                $formErrorMessage.text('An error occurred. Please try again.');
            },
            complete: function () {
                $submitBtn.prop('disabled', false);
                $('#uploadIcon').removeClass('fa-spinner fa-spin').addClass('fa-upload');
            }
        });
    });

    $('#manualAccountModal').on('hidden.bs.modal', function () {
        resetForm();
    });

    $('a[data-bs-target="#manualAccountModal"]').on('click', function () {
        window.formState.isEdit = false;
        $('#modalTitle').text('Create Account Manually');
        $('#btnText').text('Create');
        resetForm();
    });

    function resetForm() {
        $form[0].reset();
        $('#editId').val('');
        $formErrorContainer.addClass('d-none');
        $formErrorMessage.text('');
        window.formState.isEdit = false;
    }
}

function setupEditButtons(getUserUrl) {
    $('#userTableBody').on('click', '.edit-btn', function () {
        const userId = $(this).data('id');

        // Load user dat for editing
        $.ajax({
            url: getUserUrl,
            type: 'GET',
            data: { id: userId },
            success: function (data) {
                if (data.success) {
                    fillFormWithUserData(data.user);
                    $('#modalTitle').text('Edit Account');
                    $('#btnText').text('Update');
                    $('#manualAccountModal').modal('show');
                } else {
                    showAlert('danger', 'Error loading user data');
                }
            },
            error: function () {
                showAlert('danger', 'Error loading user data');
            }
        });
    });
}

function fillFormWithUserData(user) {
    window.formState.isEdit = true;

    $('#editId').val(user.id);
    $('#firstName').val(user.firstName);
    $('#lastName').val(user.lastName);
    $('#userId').val(user.userId);
    $('#email').val(user.email);
    $('#phone').val(user.phoneNumber);
    $('#role').val(user.role);
    $('#department').val(user.department);
}

function updateUserRow(user) {
    const $row = $(`button[data-id="${user.id}"]`).closest('tr');

    $row.find('.user-name').text(`${user.lastName} ${user.firstName}`);
    $row.find('.user-email').text(user.email);
    $row.find('.user-phone').text(user.userId);
    $row.find('.role-tag').text(user.role);
    $row.find('td:eq(2)').text(user.department);
}

function addNewUserRow(user) {
    const deleteUrl = $('#userTableBody').data('delete-url');
    
    const newRow = `
    <tr class="user-row">
        <td class="user-info">
            <div class="user-details">
                <div class="user-name">${user.lastName} ${user.firstName}</div>
                <div class="user-email">${user.email}</div>
                <div class="user-phone">${user.userId}</div>
            </div>
        </td>
        <td><span class="role-tag role-student">${user.role}</span></td>
        <td>${user.department}</td>
        <td>
            <span class="status-badge status-active">
                Active
            </span>
        </td>
        <td class="last-login">
            <div>New Account</div>
            <div class="login-time">Not logged in yet</div>
        </td>
        <td class="action-buttons">
            <button class="action-btn edit-btn" data-id="${user.id}"><i class="fa-solid fa-pen"></i></button>
            <button type="button" class="action-btn delete-btn"
                    data-bs-toggle="modal"
                    data-bs-target="#deleteModal"
                    data-id="${user.id}"
                    data-name="${user.lastName} ${user.firstName}"
                    data-extra="${user.role}"
                    data-url="${deleteUrl}">
                <i class="fa-solid fa-trash"></i>
            </button>
        </td>
    </tr>`;

    if ($('#noUsersRow').length) {
        $('#noUsersRow').remove();
    }

    $('#userTableBody').append(newRow);
}

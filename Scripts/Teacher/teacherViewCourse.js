// Teacher View Course JavaScript
// Handles material upload, edit, and delete functionality

// Global alert function
function showAlert(type, message) {
    const alertId = 'alert-' + Date.now();
    const alertHtml = `
        <div id="${alertId}" class="alert alert-${type} alert-dismissible fade show mb-3" role="alert" style="box-shadow: 0 3px 10px rgba(0,0,0,0.2);">
            <i class="fa-solid fa-${
                type === 'success' ? 'check-circle' :
                type === 'danger' ? 'circle-exclamation' :
                type === 'warning' ? 'triangle-exclamation' : 'circle-info'
            } me-1"></i>
            <span>${message}</span>
            <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
        </div>
    `;
    $('#alertContainer').append(alertHtml);
    setTimeout(function() {
        $(`#${alertId}`).fadeOut(300, function() { $(this).remove(); });
    }, 5000);
}

// Store selected files globally to prevent reset
let selectedFiles = [];

// Wait for DOM to be ready
$(document).ready(function () {
    console.log('Teacher View Course script loaded');
    initializeMaterialForm();
    setupFilePreview();
});

// Initialize material form submission
function initializeMaterialForm() {
    const $form = $('#materialForm');
    const $submitBtn = $form.closest('.modal').find('button[type="submit"]');
    const $modalTitleText = $('#modalTitleText');
    const $submitButtonText = $('#submitButtonText');
    const $materialId = $('#materialId');

    $form.on('submit', function (e) {
        e.preventDefault();

        // Determine if we're uploading or updating
        const isEdit = $materialId.val() && $materialId.val() !== '';
        const url = isEdit ? window.urlConfig.updateMaterial : window.urlConfig.uploadMaterial;

        console.log('Submitting material form:', isEdit ? 'Edit' : 'Upload');
        console.log('URL:', url);

        // Validate files for new upload
        if (!isEdit) {
            if (selectedFiles.length === 0) {
                showAlert('danger', 'Please select at least one file to upload.');
                return;
            }
        }

        // Create FormData to handle file uploads
        const formData = new FormData();
        
        // Add all form fields
        formData.append('__RequestVerificationToken', $('input[name="__RequestVerificationToken"]').val());
        formData.append('teacherCourseSectionId', $('input[name="teacherCourseSectionId"]').val());
        formData.append('materialTitle', $('#materialTitle').val());
        formData.append('materialType', $('#materialType').val());
        formData.append('materialDescription', $('#materialDescription').val());
        
        if (isEdit) {
            formData.append('id', $materialId.val());
            formData.append('filesToDelete', $('#filesToDelete').val());
        }
        
        // Add all selected files
        selectedFiles.forEach(function(file) {
            formData.append('materialFile', file);
        });

        // Disable submit button
        $submitBtn.prop('disabled', true);
        const originalIcon = $submitBtn.find('i').attr('class');
        $submitBtn.find('i').removeClass('fa-save fa-upload').addClass('fa-spinner fa-spin');

        $.ajax({
            url: url,
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            success: function (response) {
                console.log('Server response:', response);
                
                if (response.success) {
                    // Close modal
                    $('#materialModal').modal('hide');
                    
                    // Show success message
                    showAlert('success', response.message);
                    
                    // Reload page to show new/updated material
                    setTimeout(function () {
                        location.reload();
                    }, 1000);
                } else {
                    // Show error
                    showAlert('danger', response.message || 'An error occurred');
                }
            },
            error: function (xhr, status, error) {
                console.error('Upload error:', error);
                console.error('Response:', xhr.responseText);
                
                let errorMessage = 'An error occurred while uploading the material.';
                if (xhr.responseJSON && xhr.responseJSON.message) {
                    errorMessage = xhr.responseJSON.message;
                }
                
                showAlert('danger', errorMessage);
            },
            complete: function () {
                // Re-enable submit button
                $submitBtn.prop('disabled', false);
                $submitBtn.find('i').attr('class', originalIcon);
            }
        });
    });

    // Reset form when modal is closed
    $('#materialModal').on('hidden.bs.modal', function () {
        resetMaterialForm();
    });
}

// Open upload modal
function openUploadModal() {
    resetMaterialForm();
    $('#modalTitleText').text('Upload Learning Material');
    $('#submitButtonText').text('Upload Material');
    $('#materialModal').modal('show');
}

// Open edit modal
function openEditModal(materialId) {
    console.log('Opening edit modal for material:', materialId);
    
    resetMaterialForm();
    $('#modalTitleText').text('Edit Learning Material');
    $('#submitButtonText').text('Update Material');
    $('#fileInputLabel').text('Add More Files (Optional)');
    
    // Load material data
    $.ajax({
        url: window.urlConfig.getMaterial,
        type: 'GET',
        data: { id: materialId },
        success: function (response) {
            console.log('Material data received:', response);
            
            if (response.success && response.data) {
                const material = response.data;
                
                // Fill form fields
                $('#materialId').val(material.id);
                $('#materialTitle').val(material.title);
                $('#materialType').val(material.type);
                $('#materialDescription').val(material.description);
                
                // Show current files section
                if (material.files && material.files.length > 0) {
                    $('#currentFilesSection').show();
                    displayCurrentFiles(material.files);
                }
                
                // Show modal
                $('#materialModal').modal('show');
            } else {
                showAlert('danger', response.message || 'Failed to load material data');
            }
        },
        error: function (xhr, status, error) {
            console.error('Error loading material:', error);
            showAlert('danger', 'An error occurred while loading the material');
        }
    });
}

// Display current files in edit mode
function displayCurrentFiles(files) {
    const $filesList = $('#currentFilesList');
    $filesList.empty();
    
    const filesToDeleteIds = [];
    
    files.forEach(function (file) {
        const $listItem = $(`
            <li class="list-group-item d-flex justify-content-between align-items-center">
                <div class="file-info">
                    <i class="fas fa-file me-2"></i>
                    <span>${file.fileName}</span>
                    <small class="text-muted ms-2">(${file.sizeInMB} MB)</small>
                </div>
                <button type="button" class="btn btn-sm btn-danger delete-file-btn" data-file-id="${file.id}">
                    <i class="fas fa-trash"></i>
                </button>
            </li>
        `);
        
        // Handle file deletion
        $listItem.find('.delete-file-btn').on('click', function () {
            const fileId = $(this).data('file-id');
            filesToDeleteIds.push(fileId);
            $listItem.remove();
            
            // Update hidden field
            $('#filesToDelete').val(filesToDeleteIds.join(','));
            
            // Hide section if no files left
            if ($filesList.find('li').length === 0) {
                $('#currentFilesSection').hide();
            }
        });
        
        $filesList.append($listItem);
    });
}

// Reset material form
function resetMaterialForm() {
    $('#materialForm')[0].reset();
    $('#materialId').val('');
    $('#filesToDelete').val('');
    $('#currentFilesSection').hide();
    $('#currentFilesList').empty();
    $('#fileList').empty();
    $('#fileInputLabel').text('Upload Files');
    $('#modalTitleText').text('Upload Learning Material');
    $('#submitButtonText').text('Upload Material');
    
    // Clear selected files array
    selectedFiles = [];
}

// Setup file preview with remove functionality
function setupFilePreview() {
    $('#materialFile').on('change', function () {
        const newFiles = Array.from(this.files);
        
        if (newFiles.length > 0) {
            // Add new files to the selected files array (prevent duplicates)
            newFiles.forEach(function(file) {
                // Check if file already exists (by name and size)
                const exists = selectedFiles.some(f => f.name === file.name && f.size === file.size);
                if (!exists) {
                    selectedFiles.push(file);
                }
            });
            
            // Render the file list
            renderFileList();
        }
    });
}

// Render file list with remove buttons
function renderFileList() {
    const $fileList = $('#fileList');
    $fileList.empty();
    
    selectedFiles.forEach(function(file, index) {
        const fileSizeMB = (file.size / 1024 / 1024).toFixed(2);
        
        const $listItem = $(`
            <li class="list-group-item d-flex justify-content-between align-items-center" data-index="${index}">
                <div>
                    <i class="fas fa-file me-2"></i>
                    <span>${file.name}</span>
                    <small class="text-muted ms-2">(${fileSizeMB} MB)</small>
                </div>
                <div>
                    <button type="button" class="btn btn-sm btn-danger remove-file-btn" data-index="${index}">
                        <i class="fas fa-times"></i>
                    </button>
                </div>
            </li>
        `);
        
        // Add remove functionality
        $listItem.find('.remove-file-btn').on('click', function() {
            const fileIndex = $(this).data('index');
            removeFile(fileIndex);
        });
        
        $fileList.append($listItem);
    });
    
    // Update file count display
    if (selectedFiles.length > 0) {
        $('#fileInputLabel').text(`Upload Files (${selectedFiles.length} selected)`);
    } else {
        $('#fileInputLabel').text('Upload Files');
    }
}

// Remove file from selected files array
function removeFile(index) {
    selectedFiles.splice(index, 1);
    renderFileList();
    
    // If no files left, reset the file input
    if (selectedFiles.length === 0) {
        $('#materialFile').val('');
    }
}

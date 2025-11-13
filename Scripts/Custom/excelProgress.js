// Simplified Excel Upload Progress Handler
class SimpleExcelUploadProgress {
    constructor() {
        this.isUploading = false;
        this.bindEvents();
    }

    bindEvents() {
        $('#excelUploadForm').on('submit', (e) => {
            e.preventDefault();
            this.handleUpload();
        });

        // Reset progress when modal is closed
        $('#uploadExcelModal').on('hidden.bs.modal', () => {
            this.resetProgress();
        });
    }

    async handleUpload() {
        if (this.isUploading) return;

        const formData = new FormData($('#excelUploadForm')[0]);
        const file = $('#excelFile')[0].files[0];
        const role = $('#roleSelect').val();

        if (!file || !role) {
            this.showAlert('warning', 'Please select both a file and role type.');
            return;
        }

        this.isUploading = true;
        this.showProgress();

        try {
            await this.uploadWithProgress(formData);
        } catch (error) {
            console.error('Upload error:', error);
            this.showAlert('danger', 'Upload failed: ' + error.message);
            this.resetProgress();
        } finally {
            this.isUploading = false;
        }
    }

    async uploadWithProgress(formData) {
        return new Promise((resolve, reject) => {
            const xhr = new XMLHttpRequest();
            
            // Upload progress
            xhr.upload.addEventListener('progress', (e) => {
                if (e.lengthComputable) {
                    const percentComplete = Math.min((e.loaded / e.total) * 70, 70);
                    this.updateProgress(percentComplete, 'Uploading Excel file...');
                }
            });

            xhr.addEventListener('load', () => {
                if (xhr.status === 200) {
                    // Simulate processing stages
                    this.updateProgress(80, 'Processing records...');
                    
                    setTimeout(() => {
                        this.updateProgress(95, 'Finalizing upload...');
                        
                        setTimeout(() => {
                            this.updateProgress(100, 'Upload completed successfully!');
                            
                            setTimeout(() => {
                                window.location.reload(); // Refresh to show results and alerts
                            }, 1000);
                        }, 500);
                    }, 1000);
                    
                    resolve();
                } else {
                    reject(new Error('Upload failed with status: ' + xhr.status));
                }
            });

            xhr.addEventListener('error', () => {
                reject(new Error('Network error during upload'));
            });

            xhr.addEventListener('timeout', () => {
                reject(new Error('Upload timeout'));
            });

            xhr.timeout = 300000; // 5 minute timeout
            xhr.open('POST', $('#excelUploadForm').attr('action'));
            xhr.send(formData);
        });
    }

    showProgress() {
        $('#uploadProgressSection').slideDown(300);
        $('#uploadInstructions').slideUp(300);
        $('#uploadSubmitBtn').prop('disabled', true);
        $('#uploadCancelBtn').prop('disabled', true);
        $('#uploadModalClose').prop('disabled', true);
        $('#uploadBtnText').text('Processing...');
        
        this.updateProgress(5, 'Initializing upload...');
    }

    updateProgress(percentage, status) {
        const roundedPercent = Math.round(Math.max(0, Math.min(100, percentage)));
        
        $('#progressBar').css('width', roundedPercent + '%');
        $('#progressBar').attr('aria-valuenow', roundedPercent);
        $('#progressPercent').text(roundedPercent + '%');
        $('#progressStatus').text(status);

        // Update progress bar color based on percentage
        $('#progressBar').removeClass('bg-info bg-warning bg-success bg-primary');
        if (roundedPercent < 30) {
            $('#progressBar').addClass('bg-info');
        } else if (roundedPercent < 70) {
            $('#progressBar').addClass('bg-primary');
        } else if (roundedPercent < 95) {
            $('#progressBar').addClass('bg-warning');
        } else {
            $('#progressBar').addClass('bg-success');
        }
    }

    resetProgress() {
        if (!this.isUploading) {
            $('#uploadProgressSection').hide();
            $('#uploadInstructions').show();
            $('#uploadSubmitBtn').prop('disabled', false);
            $('#uploadCancelBtn').prop('disabled', false);
            $('#uploadModalClose').prop('disabled', false);
            $('#uploadBtnText').text('Upload');
            $('#excelUploadForm')[0].reset();
            this.updateProgress(0, 'Ready to upload...');
        }
    }

    showAlert(type, message) {
        const alertId = 'alert-' + Date.now();
        const alertHtml = `
            <div id="${alertId}" class="alert alert-${type} alert-dismissible fade show mb-3" role="alert" style="box-shadow: 0 3px 10px rgba(0,0,0,0.2);">
                <i class="fa-solid fa-${type === 'success' ? 'check-circle' :
                    type === 'danger' ? 'circle-exclamation' :
                        type === 'warning' ? 'triangle-exclamation' : 'circle-info'
                } me-1"></i>
                <span>${message}</span>
                <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
            </div>
        `;

        $('#alertContainer').append(alertHtml);

        setTimeout(function () {
            $(`#${alertId}`).fadeOut(300, function () {
                $(this).remove();
            });
        }, 5000);
    }
}

// Global alert function for external use
function showAlert(type, message) {
    const alertId = 'alert-' + Date.now();
    const alertHtml = `
        <div id="${alertId}" class="alert alert-${type} alert-dismissible fade show mb-3" role="alert" style="box-shadow: 0 3px 10px rgba(0,0,0,0.2);">
            <i class="fa-solid fa-${type === 'success' ? 'check-circle' :
                type === 'danger' ? 'circle-exclamation' :
                    type === 'warning' ? 'triangle-exclamation' : 'circle-info'
            } me-1"></i>
            <span>${message}</span>
            <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
        </div>
    `;

    $('#alertContainer').append(alertHtml);

    setTimeout(function () {
        $(`#${alertId}`).fadeOut(300, function () {
            $(this).remove();
        });
    }, 5000);
}

// Initialize the upload progress handler when document is ready
$(document).ready(function() {
    const uploadProgress = new SimpleExcelUploadProgress();
    console.log('Simple Excel upload progress initialized');
});
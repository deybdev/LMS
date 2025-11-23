// File upload handling for classwork forms
(function () {
    'use strict';

    // Initialize when DOM is ready
    document.addEventListener('DOMContentLoaded', function () {
        const uploadArea = document.getElementById('uploadArea');
        const fileInput = document.getElementById('fileInput');
        const uploadedFilesList = document.getElementById('uploadedFilesList');
        const deadlineInput = document.getElementById('deadlineInput');
        const noDueDateCheckbox = document.getElementById('noDueDateCheckbox');

        // Handle "No due date" checkbox
        if (deadlineInput && noDueDateCheckbox) {
            // Initialize checkbox state on page load
            if (noDueDateCheckbox.checked) {
                deadlineInput.disabled = true;
                deadlineInput.removeAttribute('required');
            }
            
            noDueDateCheckbox.addEventListener('change', function () {
                if (this.checked) {
                    deadlineInput.disabled = true;
                    deadlineInput.removeAttribute('required');
                    deadlineInput.value = '';
                } else {
                    deadlineInput.disabled = false;
                    deadlineInput.setAttribute('required', 'required');
                }
            });
        }

        if (!uploadArea || !fileInput || !uploadedFilesList) {
            return; // Elements not found, exit
        }

        let selectedFiles = [];
        let filesToDelete = [];

        // Drag and drop events
        ['dragenter', 'dragover', 'dragleave', 'drop'].forEach(eventName => {
            uploadArea.addEventListener(eventName, preventDefaults, false);
        });

        function preventDefaults(e) {
            e.preventDefault();
            e.stopPropagation();
        }

        ['dragenter', 'dragover'].forEach(eventName => {
            uploadArea.addEventListener(eventName, () => {
                uploadArea.classList.add('dragover');
            }, false);
        });

        ['dragleave', 'drop'].forEach(eventName => {
            uploadArea.addEventListener(eventName, () => {
                uploadArea.classList.remove('dragover');
            }, false);
        });

        uploadArea.addEventListener('drop', handleDrop, false);

        // Allow clicking on the upload area to trigger file selection
        uploadArea.addEventListener('click', function (e) {
            // Don't trigger if clicking the button itself
            if (e.target.classList.contains('choose-file-btn') || e.target.closest('.choose-file-btn')) {
                return;
            }
            fileInput.click();
        });

        function handleDrop(e) {
            const dt = e.dataTransfer;
            const files = dt.files;
            handleFiles(files);
        }

        fileInput.addEventListener('change', function () {
            handleFiles(this.files);
        });

        function handleFiles(files) {
            // Add new files to the array
            selectedFiles = [...selectedFiles, ...Array.from(files)];
            updateFileInput();
            displayFiles();
        }

        function updateFileInput() {
            // Create a new DataTransfer object to update the file input
            // This allows us to programmatically set files on the input element
            if (typeof DataTransfer !== 'undefined') {
                try {
                    const dataTransfer = new DataTransfer();
                    selectedFiles.forEach(file => {
                        dataTransfer.items.add(file);
                    });
                    fileInput.files = dataTransfer.files;
                } catch (e) {
                    console.warn('DataTransfer not supported, files may not be properly attached to form');
                    // Fallback: The files should still be in the input from the last selection
                    // This is a limitation in older browsers
                }
            } else {
                console.warn('DataTransfer API not available in this browser');
            }
        }

        function displayFiles() {
            uploadedFilesList.innerHTML = '';
            selectedFiles.forEach((file, index) => {
                const fileItem = document.createElement('div');
                fileItem.className = 'uploaded-file-item';
                fileItem.innerHTML = `
                    <div class="file-info">
                        <i class="fa-solid fa-file"></i>
                        <span>${file.name}</span>
                        <span style="color: #999; font-size: 12px;">(${formatFileSize(file.size)})</span>
                    </div>
                    <button type="button" class="remove-file-btn" onclick="removeFile(${index})">
                        <i class="fa-solid fa-times"></i>
                    </button>
                `;
                uploadedFilesList.appendChild(fileItem);
            });
        }

        // Make functions globally available
        window.removeFile = function (index) {
            selectedFiles.splice(index, 1);
            updateFileInput();
            displayFiles();
        };

        window.removeExistingFile = function (fileId, button) {
            if (confirm('Are you sure you want to delete this file?')) {
                filesToDelete.push(fileId);
                // Hide the file item
                const fileItem = button.closest('.existing-file-item');
                if (fileItem) {
                    fileItem.style.display = 'none';
                }
                // Add hidden input to track files to delete
                const hiddenInput = document.createElement('input');
                hiddenInput.type = 'hidden';
                hiddenInput.name = 'filesToDelete';
                hiddenInput.value = fileId;
                document.querySelector('form').appendChild(hiddenInput);
            }
        };

        function formatFileSize(bytes) {
            if (bytes === 0) return '0 Bytes';
            const k = 1024;
            const sizes = ['Bytes', 'KB', 'MB', 'GB'];
            const i = Math.floor(Math.log(bytes) / Math.log(k));
            return Math.round(bytes / Math.pow(k, i) * 100) / 100 + ' ' + sizes[i];
        }
    });
})();


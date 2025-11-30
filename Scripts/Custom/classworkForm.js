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

        // Initialize student assignment functionality
        initializeStudentAssignment();

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

    // Student Assignment Functionality
    function initializeStudentAssignment() {
        const assignToAllCheckbox = document.getElementById('assignToAllCheckbox');
        const studentSelectorSection = document.getElementById('studentSelectorSection');
        const selectedStudentsList = document.getElementById('selectedStudentsList');
        const selectedStudentIdsInput = document.getElementById('selectedStudentIdsInput');
        
        if (!assignToAllCheckbox) {
            console.log('Student assignment controls not found - this may be normal if not on classwork form');
            return;
        }

        let selectedStudents = [];
        let allStudents = [];

        // Load enrolled students when page loads
        loadEnrolledStudents();

        // Handle "Assign to all students" checkbox change
        assignToAllCheckbox.addEventListener('change', function() {
            updateStudentSelectorVisibility();
            updateSelectedStudentsInput();
        });

        function updateStudentSelectorVisibility() {
            if (assignToAllCheckbox.checked) {
                // Hide student selector when "assign to all" is checked
                if (studentSelectorSection) {
                    studentSelectorSection.style.display = 'none';
                }
                selectedStudents = []; // Clear selection when assigning to all
            } else {
                // Show student selector when "assign to all" is unchecked
                if (studentSelectorSection) {
                    studentSelectorSection.style.display = 'block';
                }
                // If no students selected yet, show message
                if (selectedStudents.length === 0) {
                    updateSelectedStudentsDisplay();
                }
            }
        }

        function loadEnrolledStudents() {
            // Get teacherCourseSectionId from the global variable (set in the view)
            const teacherCourseSectionId = window.teacherCourseSectionId || 0;
            
            if (!teacherCourseSectionId) {
                console.error('teacherCourseSectionId not found');
                return;
            }

            fetch('/Teacher/GetEnrolledStudents?teacherCourseSectionId=' + teacherCourseSectionId, {
                method: 'GET',
                credentials: 'same-origin'
            })
            .then(response => response.json())
            .then(data => {
                if (data.success && data.students) {
                    allStudents = data.students;
                    console.log('Loaded ' + allStudents.length + ' students');
                    
                    // If this is edit mode, pre-select students who already have submissions
                    if (window.isEditMode && window.currentlyAssignedStudents && window.currentlyAssignedStudents.length > 0) {
                        // Check if specific students are assigned (not all students)
                        const allEnrolledIds = allStudents.map(s => s.id);
                        const assignedIds = window.currentlyAssignedStudents;
                        
                        console.log('All enrolled student IDs:', allEnrolledIds);
                        console.log('Currently assigned student IDs:', assignedIds);
                        
                        // If not all students are assigned, it means specific assignment
                        const isSpecificAssignment = assignedIds.length < allEnrolledIds.length || 
                                                   !allEnrolledIds.every(id => assignedIds.includes(id));
                        
                        if (isSpecificAssignment) {
                            // Specific students are assigned - uncheck "assign to all"
                            assignToAllCheckbox.checked = false;
                            selectedStudents = allStudents.filter(student => 
                                assignedIds.includes(student.id)
                            );
                            console.log('Detected specific assignment. Selected students:', selectedStudents.map(s => s.name));
                        } else {
                            // All students are assigned - keep "assign to all" checked
                            assignToAllCheckbox.checked = true;
                            selectedStudents = [];
                            console.log('Detected assignment to all students');
                        }
                        
                        updateStudentSelectorVisibility();
                        updateSelectedStudentsDisplay();
                        updateSelectedStudentsInput();
                    }
                } else {
                    console.error('Failed to load students:', data.message);
                }
            })
            .catch(error => {
                console.error('Error loading students:', error);
            });
        }

        function updateSelectedStudentsDisplay() {
            if (!selectedStudentsList) return;

            if (selectedStudents.length === 0) {
                selectedStudentsList.innerHTML = `
                    <div class="empty-selection">
                        <i class="fas fa-user-plus"></i>
                        <p>No students selected</p>
                        <small>Click "Select Students" to choose who should receive this classwork</small>
                    </div>
                `;
                return;
            }

            const html = selectedStudents.map(student => `
                <div class="selected-student-item" data-student-id="${student.id}">
                    <div class="student-info">
                        <div class="student-avatar">${getStudentInitials(student.name)}</div>
                        <div class="student-details">
                            <span class="student-name">${escapeHtml(student.name)}</span>
                            <span class="student-id">${escapeHtml(student.studentId)}</span>
                        </div>
                    </div>
                    <button type="button" class="remove-student-btn" onclick="removeSelectedStudent(${student.id})">
                        <i class="fas fa-times"></i>
                    </button>
                </div>
            `).join('');

            selectedStudentsList.innerHTML = html;
        }

        function updateSelectedStudentsInput() {
            if (!selectedStudentIdsInput) return;

            if (assignToAllCheckbox.checked) {
                // When "assign to all" is checked, clear the input (backend will assign to all)
                selectedStudentIdsInput.value = '';
            } else {
                // When specific students are selected, populate the input
                const ids = selectedStudents.map(student => student.id).join(',');
                selectedStudentIdsInput.value = ids;
            }

            console.log('Selected student IDs:', selectedStudentIdsInput.value);
        }

        function getStudentInitials(name) {
            if (!name) return '??';
            const parts = name.trim().split(' ');
            if (parts.length >= 2) {
                return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
            }
            return name.substring(0, 2).toUpperCase();
        }

        function escapeHtml(text) {
            if (!text) return '';
            const div = document.createElement('div');
            div.textContent = text;
            return div.innerHTML;
        }

        // Global functions for student selection
        window.selectStudents = function() {
            showStudentSelectionModal();
        };

        window.removeSelectedStudent = function(studentId) {
            selectedStudents = selectedStudents.filter(student => student.id !== studentId);
            updateSelectedStudentsDisplay();
            updateSelectedStudentsInput();
        };

        function showStudentSelectionModal() {
            // Create modal HTML
            const modalHtml = `
                <div id="studentSelectionModal" class="student-modal">
                    <div class="student-modal-content">
                        <div class="student-modal-header">
                            <h3><i class="fas fa-user-check"></i> Select Students</h3>
                            <button type="button" class="close-modal" onclick="closeStudentSelectionModal()">&times;</button>
                        </div>
                        <div class="student-modal-body">
                            <div class="student-search-bar">
                                <input type="text" id="studentSearchInput" placeholder="Search students..." class="form-control">
                            </div>
                            <div class="student-list" id="studentListContainer">
                                ${createStudentListHTML()}
                            </div>
                        </div>
                        <div class="student-modal-footer">
                            <span class="selection-count">Selected: <strong id="selectionCount">${selectedStudents.length}</strong> students</span>
                            <div class="modal-actions">
                                <button type="button" class="btn-cancel" onclick="closeStudentSelectionModal()">Cancel</button>
                                <button type="button" class="btn-confirm" onclick="confirmStudentSelection()">Confirm Selection</button>
                            </div>
                        </div>
                    </div>
                </div>
            `;

            // Remove existing modal if any
            const existingModal = document.getElementById('studentSelectionModal');
            if (existingModal) {
                existingModal.remove();
            }

            // Add modal to body
            document.body.insertAdjacentHTML('beforeend', modalHtml);

            // Add event listener for search
            document.getElementById('studentSearchInput').addEventListener('input', handleStudentSearch);

            // Show modal
            const modal = document.getElementById('studentSelectionModal');
            modal.style.display = 'flex';
            setTimeout(() => {
                modal.style.opacity = '1';
                modal.querySelector('.student-modal-content').style.transform = 'translate(-50%, -50%) scale(1)';
            }, 10);
        }

        function createStudentListHTML() {
            return allStudents.map(student => {
                const isSelected = selectedStudents.some(selected => selected.id === student.id);
                return `
                    <div class="student-list-item ${isSelected ? 'selected' : ''}" data-student-id="${student.id}">
                        <div class="student-checkbox">
                            <input type="checkbox" ${isSelected ? 'checked' : ''} onchange="toggleStudentSelection(${student.id})">
                        </div>
                        <div class="student-info">
                            <div class="student-avatar">${getStudentInitials(student.name)}</div>
                            <div class="student-details">
                                <span class="student-name">${escapeHtml(student.name)}</span>
                                <span class="student-id">${escapeHtml(student.studentId)}</span>
                            </div>
                        </div>
                    </div>
                `;
            }).join('');
        }

        function handleStudentSearch() {
            const searchTerm = document.getElementById('studentSearchInput').value.toLowerCase();
            const studentItems = document.querySelectorAll('.student-list-item');

            studentItems.forEach(item => {
                const studentName = item.querySelector('.student-name').textContent.toLowerCase();
                const studentId = item.querySelector('.student-id').textContent.toLowerCase();
                
                if (studentName.includes(searchTerm) || studentId.includes(searchTerm)) {
                    item.style.display = 'flex';
                } else {
                    item.style.display = 'none';
                }
            });
        }

        window.toggleStudentSelection = function(studentId) {
            const student = allStudents.find(s => s.id === studentId);
            if (!student) return;

            const isCurrentlySelected = selectedStudents.some(selected => selected.id === studentId);
            
            if (isCurrentlySelected) {
                // Remove from selection
                selectedStudents = selectedStudents.filter(selected => selected.id !== studentId);
            } else {
                // Add to selection
                selectedStudents.push(student);
            }

            // Update the visual state of the item
            const item = document.querySelector(`[data-student-id="${studentId}"]`);
            if (item) {
                if (isCurrentlySelected) {
                    item.classList.remove('selected');
                } else {
                    item.classList.add('selected');
                }
            }

            // Update selection count
            const countElement = document.getElementById('selectionCount');
            if (countElement) {
                countElement.textContent = selectedStudents.length;
            }
        };

        window.closeStudentSelectionModal = function() {
            const modal = document.getElementById('studentSelectionModal');
            if (modal) {
                modal.style.opacity = '0';
                modal.querySelector('.student-modal-content').style.transform = 'translate(-50%, -50%) scale(0.9)';
                setTimeout(() => {
                    modal.remove();
                }, 200);
            }
        };

        window.confirmStudentSelection = function() {
            updateSelectedStudentsDisplay();
            updateSelectedStudentsInput();
            closeStudentSelectionModal();
        };
    }
})();


// Announcement Form Handler - Shared between Create and Edit
(function() {
    // Initialize when DOM is ready
    document.addEventListener('DOMContentLoaded', function() {
        initAnnouncementForm();
        // Announcement item click handler
        document.querySelectorAll('.announcement-item').forEach(item => {
            item.addEventListener('click', function (e) {
                // Ignore clicks on dropdown and its children
                if (e.target.closest('.action-dropdown, .dropdown-menu, .dropdown-item, .action-menu-btn')) {
                    return;
                }

                const announcementId = this.dataset.announcementId;
                viewAnnouncement(announcementId);
            });
        });
    });
    


    function initAnnouncementForm() {
        const editor = document.getElementById('announcementContent');
        const titleInput = document.getElementById('title');
        const form = document.getElementById('announcementForm');

        if (!editor || !titleInput || !form) return;

        // Add placeholder behavior
        editor.addEventListener('focus', function() {
            if (this.textContent.trim() === '') {
                this.innerHTML = '';
            }
        });

        editor.addEventListener('blur', function() {
            if (this.textContent.trim() === '') {
                this.setAttribute('placeholder', 'Write your announcement here...');
            }
        });

        // Character counter for title
        if (titleInput) {
            titleInput.addEventListener('input', function() {
                const charCount = document.getElementById('titleCharCount');
                if (charCount) {
                    charCount.textContent = this.value.length;
                }
            });

            // Trigger initial count
            const event = new Event('input');
            titleInput.dispatchEvent(event);
        }

        // Rich text editor buttons
        document.querySelectorAll('.editor-btn').forEach(button => {
            button.addEventListener('click', function() {
                const command = this.getAttribute('data-command');
                
                if (command === 'createLink') {
                    const url = prompt('Enter the URL:');
                    if (url) {
                        document.execCommand(command, false, url);
                    }
                } else {
                    document.execCommand(command, false, null);
                }
                
                editor.focus();
            });
        });

        // Form submission
        form.addEventListener('submit', function(e) {
            e.preventDefault();

            const content = editor.innerHTML.trim();
            
            if (!titleInput.value.trim()) {
                alert('Please enter a title for your announcement.');
                titleInput.focus();
                return;
            }

            if (!content || content === '<br>') {
                alert('Please enter content for your announcement.');
                editor.focus();
                return;
            }

            // Set the hidden input value
            document.getElementById('contentHidden').value = content;

            // Show loading state
            const submitBtn = this.querySelector('.btn-post');
            const originalText = submitBtn.innerHTML;
            submitBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Saving...';
            submitBtn.disabled = true;

            // Submit the form via AJAX
            const formData = new FormData(form);
            
            fetch(form.action, {
                method: 'POST',
                body: new URLSearchParams(formData)
            })
            .then(response => response.json())
            .then(data => {
                if (data.success) {
                    // Redirect to announcements list
                    const courseId = document.getElementById('courseId').value;
                    window.location.href = form.getAttribute('data-redirect-url');
                } else {
                    alert(data.message || 'Failed to save announcement.');
                    submitBtn.innerHTML = originalText;
                    submitBtn.disabled = false;
                }
            })
            .catch(error => {
                console.error('Error:', error);
                alert('An error occurred while saving the announcement.');
                submitBtn.innerHTML = originalText;
                submitBtn.disabled = false;
            });
        });
    }

    // Export for global use if needed
    window.AnnouncementForm = {
        init: initAnnouncementForm
    };
})();

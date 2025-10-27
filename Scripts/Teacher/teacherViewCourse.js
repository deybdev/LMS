// === TAB SWITCH ===
function showTab(tabName) {
    document.querySelectorAll('.tab-pane, .nav-tab').forEach(el => el.classList.remove('active'));
    document.getElementById(tabName).classList.add('active');
    event.target.closest('.nav-tab').classList.add('active');
}

document.addEventListener('DOMContentLoaded', () => {

    // === GLOBAL STATE ===
    let isEditMode = false;
    let filesToDelete = [];

    // === RESET FORM ===
    const resetMaterialForm = () => {
        const form = document.getElementById('materialForm');
        form.reset();
        ['materialId', 'filesToDelete'].forEach(id => document.getElementById(id).value = '');
        ['fileList', 'currentFilesList'].forEach(id => document.getElementById(id).innerHTML = '');
        filesToDelete = [];
    };

    // === OPEN UPLOAD MODAL ===
    window.openUploadModal = () => {
        isEditMode = false;
        resetMaterialForm();

        document.getElementById('modalTitleText').innerHTML = '<i class="fas fa-upload"></i> Upload Learning Material';
        document.getElementById('submitButtonText').textContent = 'Upload Material';
        document.getElementById('fileInputLabel').textContent = 'Upload Files';
        document.getElementById('currentFilesSection').style.display = 'none';
        document.getElementById('materialFile').required = true;
        document.getElementById('materialForm').action = window.urlConfig.uploadMaterial;

        new bootstrap.Modal('#materialModal').show();
    };

    // === OPEN EDIT MODAL ===
    window.openEditModal = (id) => {
        isEditMode = true;
        resetMaterialForm();

        document.getElementById('modalTitleText').innerHTML = '<i class="fas fa-edit"></i> Edit Learning Material';
        document.getElementById('submitButtonText').textContent = 'Save Changes';
        document.getElementById('fileInputLabel').textContent = 'Add New Files (Optional)';
        document.getElementById('currentFilesSection').style.display = 'block';
        document.getElementById('materialFile').required = false;
        document.getElementById('materialForm').action = window.urlConfig.updateMaterial;

        fetch(`${window.urlConfig.getMaterial}?id=${id}`)
            .then(res => res.json())
            .then(({ success, data, message }) => {
                if (!success) return showAlert('danger', message || 'Failed to load material data.');

                Object.entries({
                    materialId: data.id,
                    materialTitle: data.title,
                    materialType: data.type,
                    materialDescription: data.description || ''
                }).forEach(([k, v]) => document.getElementById(k).value = v);

                const list = document.getElementById('currentFilesList');
                list.innerHTML = data.files?.length
                    ? data.files.map(f => `
                        <li class="list-group-item d-flex justify-content-between align-items-center" data-file-id="${f.id}">
                            <span><i class="fas fa-file me-2"></i>${f.fileName} (${f.sizeInMB.toFixed(2)} MB)</span>
                            <button type="button" class="btn btn-sm btn-outline-danger" onclick="markFileForDeletion(${f.id}, this)">
                                <i class="fas fa-trash"></i> Remove
                            </button>
                        </li>`).join('')
                    : '<li class="list-group-item text-muted">No files uploaded</li>';

                new bootstrap.Modal('#materialModal').show();
            })
            .catch(() => showAlert('danger', 'Error loading material data.'));
    };

    // === MARK FILE FOR DELETION ===
    window.markFileForDeletion = (id, btn) => {
        const li = btn.closest('li');
        const isMarked = filesToDelete.includes(id);

        if (isMarked) {
            filesToDelete = filesToDelete.filter(f => f !== id);
            li.classList.remove('list-group-item-danger');
            btn.className = 'btn btn-sm btn-outline-danger';
            btn.innerHTML = '<i class="fas fa-trash"></i> Remove';
        } else {
            filesToDelete.push(id);
            li.classList.add('list-group-item-danger');
            btn.className = 'btn btn-sm btn-secondary';
            btn.innerHTML = '<i class="fas fa-undo"></i> Undo';
        }

        document.getElementById('filesToDelete').value = filesToDelete.join(',');
    };

    // === FILE INPUT PREVIEW ===
    const input = document.getElementById('materialFile');
    const list = document.getElementById('fileList');

    input.addEventListener('change', () => {
        list.innerHTML = '';
        Array.from(input.files).forEach((file, i) => {
            const li = document.createElement('li');
            li.className = 'list-group-item d-flex justify-content-between align-items-center';
            if (isEditMode) li.classList.add('bg-light');

            li.innerHTML = `
                <span>
                    <i class="fas ${isEditMode ? 'fa-file-upload text-success' : 'fa-file'} me-2"></i>
                    ${file.name} (${(file.size / 1024 / 1024).toFixed(2)} MB)
                </span>
                <button type="button" class="btn btn-sm btn-outline-danger">
                    <i class="fas fa-times"></i>
                </button>`;

            li.querySelector('button').addEventListener('click', () => {
                const dt = new DataTransfer();
                Array.from(input.files).forEach((f, idx) => { if (idx !== i) dt.items.add(f); });
                input.files = dt.files;
                li.remove();
            });
            list.appendChild(li);
        });
    });

    // === FORM SUBMIT ===
    document.getElementById('materialForm').addEventListener('submit', async e => {
        e.preventDefault();
        e.stopPropagation();
        const form = e.target;
        const formData = new FormData(form);

        if (isEditMode) {
            // Append new files manually
            formData.delete('materialFile');
            Array.from(document.getElementById('materialFile').files)
                .forEach(f => formData.append('newFiles', f));
        }

        try {
            const res = await fetch(form.action, {
                method: 'POST',
                body: formData,
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            });
            const { success, message } = await res.json();

            bootstrap.Modal.getInstance(document.getElementById('materialModal')).hide();
            showAlert(success ? 'success' : 'danger', message);
            if (success) setTimeout(() => location.reload(), 1000);

        } catch (err) {
            console.error(err);
            showAlert('danger', 'An unexpected error occurred.');
        }
    });

    // === ALERT ===
    window.showAlert = (type, msg) => {
        const alert = document.createElement('div');
        alert.className = `alert alert-${type} alert-dismissible fade show position-fixed top-0 end-0 m-3 shadow`;
        alert.style.zIndex = '2000';
        alert.innerHTML = `
            <i class="fa-solid ${type === 'success' ? 'fa-circle-check' : 'fa-triangle-exclamation'} me-2"></i>
            ${msg}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>`;
        document.body.appendChild(alert);
        setTimeout(() => alert.remove(), 4000);
    };
});
// Force remove any modal backdrops
function forceRemoveBackdrop() {
    document.querySelectorAll('.modal-backdrop').forEach(b => b.remove());
    document.body.classList.remove('modal-open');
    document.body.style.overflow = '';
    document.body.style.paddingRight = '';
}

// Global alert function
window.showAlert = (type, message) => {
    const id = 'alert-' + Date.now();
    const icons = {
        success: 'check-circle',
        danger: 'circle-exclamation',
        warning: 'triangle-exclamation',
        info: 'circle-info'
    };

    const html = `
        <div id="${id}" class="alert alert-${type} alert-dismissible fade show mb-3" role="alert"
             style="box-shadow:0 3px 10px rgba(0,0,0,0.2);">
            <i class="fa-solid fa-${icons[type] || icons.info} me-1"></i>
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        </div>`;

    (document.getElementById('alertContainer') || document.body)
        .insertAdjacentHTML('beforeend', html);

    setTimeout(() => {
        const el = document.getElementById(id);
        if (el) {
            el.classList.remove('show');
            setTimeout(() => el.remove(), 300);
        }
    }, 5000);
};

// Delete handler
document.addEventListener('DOMContentLoaded', () => {
    const modal = document.getElementById('deleteModal');
    const form = document.getElementById('deleteForm');
    const nameEl = document.getElementById('deleteItemName');
    const idEl = document.getElementById('deleteItemId');
    if (!modal || !form || !nameEl || !idEl) return;

    let deleteUrl = '', targetEl = null;

    modal.addEventListener('show.bs.modal', e => {
        const btn = e.relatedTarget;
        idEl.value = btn.dataset.itemId;
        nameEl.textContent = btn.dataset.itemName || 'this item';
        deleteUrl = `${btn.dataset.url}?id=${btn.dataset.itemId}`;
        targetEl = btn.closest(btn.dataset.targetRemove || '.material-card');
    });

    form.addEventListener('submit', async e => {
        e.preventDefault();
        try {
            const res = await fetch(deleteUrl, {
                method: 'POST',
                body: new FormData(form),
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            });
            const data = await res.json();

            bootstrap.Modal.getInstance(modal)?.hide();
            forceRemoveBackdrop();

            if (data.success) {
                targetEl?.remove();
                showAlert('success', data.message);
            } else showAlert('danger', data.message || 'Delete failed.');
        } catch {
            bootstrap.Modal.getInstance(modal)?.hide();
            forceRemoveBackdrop();
            showAlert('danger', 'An error occurred while deleting.');
        }
    });

    modal.addEventListener('hidden.bs.modal', forceRemoveBackdrop);
});

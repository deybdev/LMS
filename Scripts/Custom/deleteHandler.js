document.addEventListener('DOMContentLoaded', () => {
    const modal = document.getElementById('deleteModal');
    const form = document.getElementById('deleteForm');
    const itemNameEl = document.getElementById('deleteItemName');
    const itemIdEl = document.getElementById('deleteItemId');

    let deleteUrl = '';
    let targetEl = null;

    // When delete button is clicked and modal opens
    modal.addEventListener('show.bs.modal', e => {
        const btn = e.relatedTarget;
    const name = btn.dataset.itemName || 'this item';
    const id = btn.dataset.itemId;
    const baseUrl = btn.dataset.url;
    const targetRemove = btn.dataset.targetRemove || '.material-card';

    deleteUrl = `${baseUrl}?id=${id}`;
    targetEl = btn.closest(targetRemove);

    itemNameEl.textContent = name;
    itemIdEl.value = id;
    });

    // Confirm delete
    form.addEventListener('submit', async e => {
        e.preventDefault();

    const formData = new FormData(form);
    formData.append('id', itemIdEl.value);

    try {
            const res = await fetch(deleteUrl, {
        method: 'POST',
    body: formData,
    headers: {'X-Requested-With': 'XMLHttpRequest' }
            });
    const data = await res.json();

    bootstrap.Modal.getInstance(modal).hide();

    if (data.success) {
        targetEl?.remove();
    showAlert('success', data.message);
            } else {
        showAlert('danger', data.message || 'Delete failed.');
            }
        } catch {
        showAlert('danger', 'An error occurred while deleting.');
        }
    });

    // Bootstrap-like alert
    function showAlert(type, message) {
        const el = document.createElement('div');
    el.className = `alert alert-${type} alert-dismissible fade show position-fixed top-0 end-0 m-3 shadow`;
    el.style.zIndex = '2000';
    el.innerHTML = `
    <i class="fa-solid ${type === 'success' ? 'fa-circle-check' : 'fa-triangle-exclamation'} me-2"></i>
    ${message}
    <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    `;
    document.body.appendChild(el);
        setTimeout(() => el.remove(), 4000);
    }
});

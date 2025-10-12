
// PAGINATION FOR TABLE
document.addEventListener("DOMContentLoaded", () => {
    const searchInput = document.getElementById("searchInput");
    const tableBody = document.getElementById("userTableBody");
    const noUsersRow = document.getElementById("noUsersRow");
    const paginationNumbers = document.getElementById("paginationNumbers");
    const prevBtn = document.getElementById("prevBtn");
    const nextBtn = document.getElementById("nextBtn");
    const paginationInfo = document.getElementById("paginationInfo");
    const currentPageInfo = document.getElementById("currentPageInfo");

    const itemsPerPage = 5;
    let currentPage = 1;
    let allRows = Array.from(tableBody.querySelectorAll("tr.user-row"));
    let filteredRows = [...allRows];

    function updateRowArrays() {
        allRows = Array.from(tableBody.querySelectorAll("tr.user-row"));
        filteredRows = allRows.filter(r => {
            const query = searchInput.value.toLowerCase();
            if (!query) return true;

            const name = r.querySelector(".user-name").textContent.toLowerCase();
            const email = r.querySelector(".user-email").textContent.toLowerCase();
            const dept = r.cells[2].textContent.toLowerCase();
            return name.includes(query) || email.includes(query) || dept.includes(query);
        });
    }

    function renderPage() {
        updateRowArrays();

        const totalPages = Math.ceil(filteredRows.length / itemsPerPage);

        // Adjust current page if necessary
        if (currentPage > totalPages && totalPages > 0) {
            currentPage = totalPages;
        } else if (totalPages === 0) {
            currentPage = 1;
        }

        const start = (currentPage - 1) * itemsPerPage;
        const end = start + itemsPerPage;

        allRows.forEach(r => r.style.display = "none");
        filteredRows.slice(start, end).forEach(r => r.style.display = "");

        if (noUsersRow) noUsersRow.style.display = filteredRows.length ? "none" : "";
        if (noUsersRow && !filteredRows.length) noUsersRow.cells[0].textContent = "No users found.";

        paginationInfo.textContent = filteredRows.length
            ? `Showing ${start + 1}-${Math.min(end, filteredRows.length)} of ${filteredRows.length} entries`
            : "Showing 0-0 of 0 entries";

        currentPageInfo.textContent = `Page ${currentPage} of ${totalPages || 1}`;

        prevBtn.disabled = currentPage === 1;
        nextBtn.disabled = currentPage === totalPages || totalPages === 0;

        // Render page numbers with ellipsis
        paginationNumbers.innerHTML = "";
        const addButton = i => {
            const btn = document.createElement("button");
            btn.textContent = i;
            btn.className = `pagination-number ${i === currentPage ? "active" : ""}`;
            btn.onclick = () => { currentPage = i; renderPage(); };
            paginationNumbers.appendChild(btn);
        };
        const addEllipsis = () => {
            const span = document.createElement("span");
            span.className = "pagination-ellipsis";
            span.textContent = "...";
            paginationNumbers.appendChild(span);
        };

        if (totalPages <= 7) {
            for (let i = 1; i <= totalPages; i++) addButton(i);
        } else {
            addButton(1);
            if (currentPage > 4) addEllipsis();

            for (let i = Math.max(2, currentPage - 1); i <= Math.min(totalPages - 1, currentPage + 1); i++) {
                addButton(i);
            }

            if (currentPage < totalPages - 3) addEllipsis();
            if (totalPages > 1) addButton(totalPages);
        }
    }

    searchInput.addEventListener("input", () => {
        currentPage = 1;
        renderPage();
    });

    prevBtn.onclick = () => { if (currentPage > 1) { currentPage--; renderPage(); } };
    nextBtn.onclick = () => { if (currentPage < Math.ceil(filteredRows.length / itemsPerPage)) { currentPage++; renderPage(); } };

    renderPage();
});


// REUSABLE ALERT MODAL
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


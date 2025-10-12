$(function () {
    const $modal = $("#deleteModal"),
        $form = $("#deleteForm"),
        $tableBody = $("#userTableBody");

    // Open modal
    $modal.on("show.bs.modal", e => {
        const btn = $(e.relatedTarget);
        $("#deleteItemId").val(btn.data("id"));
        $("#deleteItemExtra").val(btn.data("extra") || "");
        $("#deleteItemName").text(btn.data("name"));
        $form.data("url", btn.data("url"));
    });

    // Submit form via AJAX
    $form.on("submit", e => {
        e.preventDefault();

        const id = $("#deleteItemId").val();
        const url = $form.data("url");

        $.post($form.data("url"), {
            __RequestVerificationToken: $('[name="__RequestVerificationToken"]').val(),
            id: $("#deleteItemId").val(),
            role: $("#deleteItemExtra").val()
        }).done(res => {
            $modal.modal("hide");
            $(`button[data-id='${$("#deleteItemId").val()}']`).closest("tr").fadeOut(300, function () {
                $(this).remove();
            });
            
            if (res.success) {
                showAlert("success", res.message);
            }
        });

    });
});

$(document).ready(function () {
    $("#department").change(function () {
        var departmentId = $(this).val();

        // Disable program if no department selected
        if (departmentId === "") {
            $("#program").empty()
                .append('<option value="">Select program</option>')
                .prop("disabled", true);
            return;
        }

        // Enable program dropdown
        $("#program").prop("disabled", false);

        // Fetch programs via AJAX
        $.ajax({
            url: '/Course/GetProgramsByDepartment', // use absolute path
            type: "GET",
            data: { departmentId: departmentId },
            success: function (data) {
                $("#program").empty()
                    .append('<option value="">Select program</option>');

                $.each(data, function (i, program) {
                    $("#program").append(
                        `<option value="${program.Id}">${program.ProgramName}</option>`
                    );
                });
            }
        });
    });
});

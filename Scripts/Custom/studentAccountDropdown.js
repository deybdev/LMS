$(document).ready(function () {
    console.log("=== Manage Users dropdown script loaded ===");

    var baseUrl = window.location.origin;

    // Hide student fields by default
    $("#studentFieldsContainer").hide();
    $("#program, #yearLevel, #studentSection").prop("disabled", true);
    console.log("Initial state: Student fields hidden and disabled");

    // Show student fields ONLY when role = Student
    $("#role").change(function () {
        var role = $(this).val();
        console.log("=== studentAccountDropdown.js - Role change handler ===");
        console.log("Role selected:", role);

        if (role === "Student") {
            console.log("Showing student fields container with slideDown");
            $("#studentFieldsContainer").slideDown(200);
        } else {
            console.log("Hiding student fields container with slideUp");
            $("#studentFieldsContainer").slideUp(200);

            // Reset student dropdowns
            console.log("Resetting and disabling student dropdowns");
            $("#program").val("").prop("disabled", true);
            $("#yearLevel").val("").prop("disabled", true);
            $("#studentSection").val("").prop("disabled", true);
        }
    });

    // ---------------------------
    // Load Programs When Department Changes
    // ---------------------------
    $("#department").change(function () {
        var departmentName = $(this).val(); // You are using DepartmentName, NOT Id
        console.log("=== Department changed ===");
        console.log("Department value:", departmentName);

        if (!departmentName) {
            console.log("No department selected, resetting program/year/section");
            resetProgramYearSection();
            return;
        }

        $("#program").prop("disabled", false)
            .empty()
            .append('<option value="">Choose program</option>');
        
        console.log("Loading programs for department:", departmentName);

        $.ajax({
            url: baseUrl + '/Program/GetProgramsByDepartment',
            type: 'GET',
            data: { departmentId: departmentName }, // stays the same
            success: function (data) {
                console.log("Programs loaded:", data.length, "programs");
                $("#program").empty().append('<option value="">Choose program</option>');

                $.each(data, function (i, program) {
                    $("#program").append(
                        `<option value="${program.Id}" data-duration="${program.ProgramDuration}">
                            ${program.ProgramCode} - ${program.ProgramName}
                        </option>`
                    );
                });

                $("#yearLevel").empty().append('<option value="">Choose year level</option>').prop("disabled", true);
                $("#studentSection").empty().append('<option value="">Choose section</option>').prop("disabled", true);
                console.log("Program dropdown populated, year level and section reset");
            },
            error: function(xhr, status, error) {
                console.error("Error loading programs:", error);
            }
        });
    });

    // ---------------------------
    // Load Year Levels When Program Changes
    // ---------------------------
    $("#program").change(function () {
        var programId = $(this).val();
        console.log("=== Program changed ===");
        console.log("Program ID:", programId);

        if (!programId) {
            console.log("No program selected, resetting year level and section");
            $("#yearLevel").empty().append('<option value="">Choose year level</option>').prop("disabled", true);
            $("#studentSection").empty().append('<option value="">Choose section</option>').prop("disabled", true);
            return;
        }

        $("#yearLevel").prop("disabled", false)
            .empty()
            .append('<option value="">Choose year level</option>');
        
        console.log("Loading year levels for program:", programId);

        $.ajax({
            url: baseUrl + '/Program/GetYearLevelsByProgram',
            type: 'GET',
            data: { programId: programId },
            success: function (data) {
                console.log("Year levels loaded:", data.length, "levels");
                $.each(data, function (i, year) {
                    $("#yearLevel").append(`<option value="${year.Id}">${year.Name}</option>`);
                });

                $("#studentSection").empty().append('<option value="">Choose section</option>').prop("disabled", true);
                console.log("Year level dropdown populated, section reset");
            },
            error: function(xhr, status, error) {
                console.error("Error loading year levels:", error);
            }
        });
    });

    // ---------------------------
    // Load Sections When Year Level Changes
    // ---------------------------
    $("#yearLevel").change(function () {
        var programId = $("#program").val();
        var yearLevel = $(this).val();
        console.log("=== Year level changed ===");
        console.log("Program ID:", programId, "Year Level:", yearLevel);

        if (!programId || !yearLevel) {
            console.log("Missing program or year level, resetting section");
            $("#studentSection").empty().append('<option value="">Choose section</option>').prop("disabled", true);
            return;
        }

        $("#studentSection").prop("disabled", false)
            .empty()
            .append('<option value="">Choose section</option>');
        
        console.log("Loading sections for program:", programId, "year:", yearLevel);

        $.ajax({
            url: baseUrl + '/Program/GetSections',
            type: 'GET',
            data: { programId: programId, yearLevel: yearLevel },
            success: function (data) {
                console.log("Sections loaded:", data.length, "sections");
                $.each(data, function (i, s) {
                    $("#studentSection").append(`<option value="${s.Id}">${s.SectionName}</option>`);
                });
                console.log("Section dropdown populated");
            },
            error: function(xhr, status, error) {
                console.error("Error loading sections:", error);
            }
        });
    });

    // ---------------------------
    // Reset Function
    // ---------------------------
    function resetProgramYearSection() {
        console.log("Resetting program, year level, and section dropdowns");
        $("#program, #yearLevel, #studentSection")
            .empty()
            .append('<option value="">Choose</option>')
            .prop("disabled", true);
    }

});

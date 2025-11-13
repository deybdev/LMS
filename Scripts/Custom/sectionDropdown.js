// Section Page - Cascading Dropdown Handler
$(document).ready(function () {
    console.log("Section dropdown script loaded");

    var baseUrl = window.location.origin;

    // Initially disable program and year level dropdowns
    $("#program, #yearLevel").prop("disabled", true);

    // Department Change Event
    $("#department").on("change", function () {
        var departmentId = $(this).val();

        // Reset and disable dependent dropdowns
        $("#program").empty().append('<option value="" disabled selected>Choose program</option>').prop("disabled", true);
        $("#yearLevel").empty().append('<option value="" disabled selected>Choose year level</option>').prop("disabled", true);

        if (!departmentId) {
            return;
        }

        console.log("Department selected:", departmentId);

        // Load programs for selected department
        $.ajax({
            url: baseUrl + '/Program/GetProgramsByDepartment',
            type: 'GET',
            data: { departmentId: departmentId },
            success: function (programs) {
                console.log("Programs loaded:", programs);

                if (programs && programs.length > 0) {
                    $("#program").prop("disabled", false);

                    $.each(programs, function (index, program) {
                        $("#program").append(
                            $('<option>', {
                                value: program.Id,
                                text: program.ProgramName,
                                'data-duration': program.ProgramDuration
                            })
                        );
                    });
                } else {
                    console.log("No programs found for this department");
                    $("#program").append('<option value="" disabled>No programs available</option>');
                }
            },
            error: function (xhr, status, error) {
                console.error("Error loading programs:", error);
                showAlert('danger', 'Failed to load programs. Please try again.');
                $("#program").append('<option value="" disabled>Error loading programs</option>');
            }
        });
    });

    // Program Change Event
    $("#program").on("change", function () {
        var programId = $(this).val();

        // Reset year level dropdown
        $("#yearLevel").empty().append('<option value="" disabled selected>Choose year level</option>').prop("disabled", true);

        if (!programId) {
            return;
        }

        console.log("Program selected:", programId);

        // Load year levels for selected program
        $.ajax({
            url: baseUrl + '/Program/GetYearLevelsByProgram',
            type: 'GET',
            data: { programId: programId },
            success: function (yearLevels) {
                console.log("Year levels loaded:", yearLevels);

                if (yearLevels && yearLevels.length > 0) {
                    $("#yearLevel").prop("disabled", false);

                    $.each(yearLevels, function (index, year) {
                        $("#yearLevel").append(
                            $('<option>', {
                                value: year.Id,
                                text: year.Name
                            })
                        );
                    });
                } else {
                    console.log("No year levels found for this program");
                    $("#yearLevel").append('<option value="" disabled>No year levels available</option>');
                }
            },
            error: function (xhr, status, error) {
                console.error("Error loading year levels:", error);
                showAlert('danger', 'Failed to load year levels. Please try again.');
                $("#yearLevel").append('<option value="" disabled>Error loading year levels</option>');
            }
        });
    });

    // Helper function to reset all dropdowns
    window.resetSectionDropdowns = function () {
        $("#department").val('');
        $("#program").empty().append('<option value="" disabled selected>Choose program</option>').prop("disabled", true);
        $("#yearLevel").empty().append('<option value="" disabled selected>Choose year level</option>').prop("disabled", true);
    };

    // Helper function to load programs for edit mode
    window.loadProgramsForEdit = function (departmentId, programId, yearLevel) {
        if (!departmentId) return;

        $.ajax({
            url: baseUrl + '/Program/GetProgramsByDepartment',
            type: 'GET',
            data: { departmentId: departmentId },
            success: function (programs) {
                $("#program").empty().append('<option value="" disabled selected>Choose program</option>');

                if (programs && programs.length > 0) {
                    $("#program").prop("disabled", false);

                    $.each(programs, function (index, program) {
                        $("#program").append(
                            $('<option>', {
                                value: program.Id,
                                text: program.ProgramName,
                                'data-duration': program.ProgramDuration,
                                selected: program.Id == programId
                            })
                        );
                    });

                    // If program is selected, load year levels
                    if (programId) {
                        loadYearLevelsForEdit(programId, yearLevel);
                    }
                }
            },
            error: function (xhr, status, error) {
                console.error("Error loading programs for edit:", error);
            }
        });
    };

    // Helper function to load year levels for edit mode
    function loadYearLevelsForEdit(programId, selectedYearLevel) {
        if (!programId) return;

        $.ajax({
            url: baseUrl + '/Program/GetYearLevelsByProgram',
            type: 'GET',
            data: { programId: programId },
            success: function (yearLevels) {
                $("#yearLevel").empty().append('<option value="" disabled selected>Choose year level</option>');

                if (yearLevels && yearLevels.length > 0) {
                    $("#yearLevel").prop("disabled", false);

                    $.each(yearLevels, function (index, year) {
                        $("#yearLevel").append(
                            $('<option>', {
                                value: year.Id,
                                text: year.Name,
                                selected: year.Id == selectedYearLevel
                            })
                        );
                    });
                }
            },
            error: function (xhr, status, error) {
                console.error("Error loading year levels for edit:", error);
            }
        });
    }

    console.log("Section dropdown handlers initialized");
});

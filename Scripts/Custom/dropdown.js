$(document).ready(function () {
    console.log("Dropdown script loaded");
    
    var baseUrl = window.location.origin;
    var existingCombinations = [];
    
    $.ajax({
        url: baseUrl + '/IT/GetExistingCurriculumCombinations',
        type: 'GET',
        success: function (data) {
            existingCombinations = data || [];
        },
        error: function (xhr, status, error) {
            existingCombinations = [];
        }
    });
    
    $("#program, #yearLevel").prop("disabled", true);

    $("#department").change(function () {
        var departmentId = $(this).val();

        if (!departmentId) {
            $("#program, #yearLevel")
                .empty().append('<option value="">Select</option>')
                .prop("disabled", true);
            return;
        }

        $("#program").prop("disabled", false).empty().append('<option value="">Select program</option>');

        $.ajax({
            url: baseUrl + '/Program/GetProgramsByDepartment',
            type: 'GET',
            data: { departmentId: departmentId },
            success: function (data) {
                $.each(data, function (i, program) {
                    var option = '<option value="' + program.Id + '" data-duration="' + program.ProgramDuration + '">' + program.ProgramName + '</option>';
                    $("#program").append(option);
                });

                $("#yearLevel").empty().append('<option value="">Select year</option>').prop("disabled", true);
                $("#semesterSelect").val('');
            },
        });
    });

    $("#program").change(function () {
        var programId = $(this).val();

        if (!programId) {
            $("#yearLevel").empty().append('<option value="">Select year</option>').prop("disabled", true);
            $("#semesterSelect").val('');
            return;
        }

        $("#yearLevel").prop("disabled", false).empty().append('<option value="">Select year</option>');

        $.ajax({
            url: baseUrl + '/Program/GetYearLevelsByProgram',
            type: 'GET',
            data: { programId: programId },
            success: function (data) {
                console.log("Year levels received:", data);
                $.each(data, function (i, year) {
                    var option = '<option value="' + year.Id + '">' + year.Name + '</option>';
                    $("#yearLevel").append(option);
                });
                
                $("#semesterSelect").val('');
                filterSemesterOptions();
            },
        });
    });
    
    $("#yearLevel").change(function () {
        filterSemesterOptions();
    });
    
    function filterSemesterOptions() {
        var programId = parseInt($("#program").val());
        var yearLevel = parseInt($("#yearLevel").val());
        
        if (!programId || !yearLevel) {
            $("#semesterSelect option").each(function() {
                if ($(this).val() !== '') {
                    $(this).prop('disabled', false).show();
                }
            });
            return;
        }
        
        $("#semesterSelect option").each(function() {
            var semesterValue = $(this).val();
            
            if (semesterValue === '') {
                return;
            }
            
            var semester = parseInt(semesterValue);
            var exists = existingCombinations.some(function(combo) {
                return combo.programId === programId && 
                       combo.yearLevel === yearLevel && 
                       combo.semester === semester;
            });
            
            if (exists) {
                $(this).prop('disabled', true);
                var originalText = $(this).text().replace(' (Already assigned)', '');
                $(this).text(originalText + ' (Already assigned)');
            } else {
                $(this).prop('disabled', false);
                var originalText = $(this).text().replace(' (Already assigned)', '');
                $(this).text(originalText);
            }
        });
    }
});

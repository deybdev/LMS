using LMS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace LMS.Controllers
{
    public class CurriculumController : Controller
    {
        private readonly LMSContext db = new LMSContext();

        // POST: Curriculum/AssignCoursesToCurriculum
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AssignCoursesToCurriculum(int ProgramId, int YearLevel, int Semester, string CourseIds)
        {
            if (ProgramId <= 0 || YearLevel <= 0 || Semester <= 0)
            {
                TempData["ErrorMessage"] = "Please select Program, Year Level, and Semester.";
                return RedirectToAction("AssignCourse");
            }

            if (string.IsNullOrEmpty(CourseIds))
            {
                TempData["ErrorMessage"] = "Please select at least one course.";
                return RedirectToAction("AssignCourse");
            }

            // Parse course IDs
            var courseIdArray = CourseIds.Split(',')
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => int.Parse(id.Trim()))
                .ToArray();

            foreach (var courseId in courseIdArray)
            {
                var curriculumCourse = new Models.CurriculumCourse
                {
                    ProgramId = ProgramId,
                    CourseId = courseId,
                    YearLevel = YearLevel,
                    Semester = Semester
                };

                db.CurriculumCourses.Add(curriculumCourse);
            }

            db.SaveChanges();

            var program = db.Programs.Find(ProgramId);
            string programName = program != null ? program.ProgramName : "the program";

            TempData["SuccessMessage"] = $"Successfully assigned {courseIdArray.Length} course(s) to {programName} - Year {YearLevel}, Semester {Semester}.";

            return RedirectToAction("AssignedCourses", "IT");
        }

        // POST: Curriculum/DeleteCurriculumGroup
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteCurriculumGroup(int programId, int yearLevel, int semester)
        {
            try
            {
                // Find all curriculum courses matching the program, year level, and semester
                var curriculumCourses = db.CurriculumCourses
                    .Where(cc => cc.ProgramId == programId && cc.YearLevel == yearLevel && cc.Semester == semester)
                    .ToList();

                if (!curriculumCourses.Any())
                {
                    return Json(new { success = false, message = "Curriculum group not found." });
                }

                // Get program info for success message
                var program = db.Programs.Find(programId);
                string programName = program != null ? program.ProgramName : "the program";

                int courseCount = curriculumCourses.Count;

                // Remove all curriculum courses in this group
                db.CurriculumCourses.RemoveRange(curriculumCourses);
                db.SaveChanges();

                string yearSuffix = yearLevel == 1 ? "st" : yearLevel == 2 ? "nd" : yearLevel == 3 ? "rd" : "th";
                string semesterName = semester == 1 ? "1st Semester" : semester == 2 ? "2nd Semester" : semester == 3 ? "Summer" : $"Semester {semester}";

                return Json(new
                {
                    success = true,
                    message = $"Successfully deleted curriculum group for {programName} - {yearLevel}{yearSuffix} Year - {semesterName}. {courseCount} course(s) removed."
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DeleteCurriculumGroup Error: {ex.Message}");
                return Json(new { success = false, message = "An error occurred while deleting the curriculum group: " + ex.Message });
            }
        }

        // POST: Curriculum/UpdateCurriculumCourses
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateCurriculumCourses(int ProgramId, int YearLevel, int Semester, string CourseIds)
        {
            try
            {
                if (ProgramId <= 0 || YearLevel <= 0 || Semester <= 0)
                {
                    TempData["ErrorMessage"] = "Invalid curriculum parameters.";
                    return RedirectToAction("AssignedCourses", "IT");
                }

                var newCourseIds = string.IsNullOrEmpty(CourseIds)
                    ? new List<int>()
                    : CourseIds.Split(',')
                        .Where(id => !string.IsNullOrWhiteSpace(id))
                        .Select(id => int.Parse(id.Trim()))
                        .ToList();
                // Get existing
                var existingCurriculumCourses = db.CurriculumCourses
                    .Where(cc => cc.ProgramId == ProgramId && cc.YearLevel == YearLevel && cc.Semester == Semester)
                    .ToList();

                var existingCourseIds = existingCurriculumCourses.Select(cc => cc.CourseId).ToList();

                var coursesToAdd = newCourseIds.Except(existingCourseIds).ToList();
                var coursesToRemove = existingCourseIds.Except(newCourseIds).ToList();

                if (coursesToRemove.Any())
                {
                    db.CurriculumCourses.RemoveRange(
                        existingCurriculumCourses.Where(cc => coursesToRemove.Contains(cc.CourseId))
                    );
                }
                foreach (var courseId in coursesToAdd)
                {
                    db.CurriculumCourses.Add(new CurriculumCourse
                    {
                        ProgramId = ProgramId,
                        CourseId = courseId,
                        YearLevel = YearLevel,
                        Semester = Semester
                    });
                }

                db.SaveChanges();

                var program = db.Programs.Find(ProgramId);
                string programName = program?.ProgramName ?? "Unknown Program";
                TempData["SuccessMessage"] =
                    $"Curriculum updated successfully";

                return RedirectToAction("AssignedCourses", "IT");
            }
            catch
            {
                TempData["ErrorMessage"] = "An error occurred while updating the curriculum.";
                return RedirectToAction("AssignedCourses", "IT");
            }
        }



    }
}
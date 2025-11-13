using LMS.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace LMS.Controllers
{
    public class CourseController : Controller
    {
        private readonly LMSContext db = new LMSContext();

        public class StudentDto
        {
            public int id { get; set; }
            public string studentId { get; set; }
            public string name { get; set; }
        }

        // GET: Course
        public ActionResult Index()
        {
            return View();
        }

        // POST: Create Course
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateCourse(Course model)
        {
            // Check for duplicate course title
            bool titleExists = db.Courses.Any(c => c.CourseTitle.Trim().ToLower() == model.CourseTitle.Trim().ToLower());
            
            // Check for duplicate course code
            bool codeExists = db.Courses.Any(c => c.CourseCode.Trim().ToLower() == model.CourseCode.Trim().ToLower());

            if (titleExists)
            {
                ModelState.AddModelError("CourseTitle", "A course with this title already exists.");
            }

            if (codeExists)
            {
                ModelState.AddModelError("CourseCode", "A course with this code already exists.");
            }

            if (titleExists || codeExists)
            {
                // Return the view from the IT folder
                return View("~/Views/IT/CreateCourse.cshtml", model);
            }

            // Add new course
            model.DateCreated = DateTime.Now;
            db.Courses.Add(model);
            db.SaveChanges();

            TempData["SuccessMessage"] = "Course created successfully!";
            return RedirectToAction("Course", "IT");
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteCourse(int? id)
        {
            if (id == null)
            {
                return Json(new { success = false, message = "Invalid request. Course ID missing." });
            }

            var course = db.Courses.Find(id);
            if (course == null)
            {
                return Json(new { success = false, message = "Course not found!" });
            }

            string courseTitle = course.CourseTitle;

            db.Courses.Remove(course);
            db.SaveChanges();

            return Json(new { success = true, message = $"Course '{courseTitle}' deleted successfully!" });
        }

        public JsonResult SearchCourses(string term, int[] excludeIds)
        {
            if (string.IsNullOrEmpty(term))
                return Json(new { results = new object[0] }, JsonRequestBehavior.AllowGet);

            var query = db.Courses.AsQueryable();

            if (excludeIds != null && excludeIds.Length > 0)
            {
                query = query.Where(c => !excludeIds.Contains(c.Id));
            }

            var results = query
                .Where(c => c.CourseCode.Contains(term) || c.CourseTitle.Contains(term))
                .Select(c => new { c.Id, c.CourseCode, c.CourseTitle })
                .Take(20)
                .ToList();

            return Json(new { results }, JsonRequestBehavior.AllowGet);
        }

        // AddCoursesToStudent
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult AddCoursesToStudent(int studentId, string courseIds)
        {
            try
            {
                if (string.IsNullOrEmpty(courseIds))
                {
                    return Json(new { success = false, message = "No courses selected" });
                }

                var courseIdArray = courseIds.Split(',')
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .Select(id => int.Parse(id.Trim()))
                    .ToArray();

                // Get student's section
                var studentCourse = db.StudentCourses
                    .Where(sc => sc.StudentId == studentId)
                    .OrderByDescending(sc => sc.DateEnrolled)
                    .FirstOrDefault();

                if (studentCourse == null)
                {
                    return Json(new { success = false, message = "Student section not found" });
                }

                int addedCount = 0;
                foreach (var courseId in courseIdArray)
                {
                    // Check if already enrolled
                    var exists = db.StudentCourses
                        .Any(sc => sc.StudentId == studentId && sc.CourseId == courseId);

                    if (!exists)
                    {
                        var newEnrollment = new StudentCourse
                        {
                            StudentId = studentId,
                            CourseId = courseId,
                            SectionId = studentCourse.SectionId,
                            DateEnrolled = DateTime.Now
                        };

                        db.StudentCourses.Add(newEnrollment);
                        addedCount++;
                    }
                }

                db.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = $"Successfully added {addedCount} course(s) to student",
                    addedCount = addedCount
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AddCoursesToStudent Error: {ex.Message}");
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // RemoveCourseFromStudent
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult RemoveCourseFromStudent(int studentCourseId)
        {
            try
            {
                var studentCourse = db.StudentCourses.Find(studentCourseId);

                if (studentCourse == null)
                {
                    return Json(new { success = false, message = "Enrollment not found" });
                }

                var course = db.Courses.Find(studentCourse.CourseId);
                string courseName = course != null ? $"{course.CourseCode} - {course.CourseTitle}" : "Course";

                db.StudentCourses.Remove(studentCourse);
                db.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = $"Successfully removed {courseName} from student"
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RemoveCourseFromStudent Error: {ex.Message}");
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // GetAvailableCoursesForStudent
        [HttpGet]
        public JsonResult GetAvailableCoursesForStudent(int studentId, int yearLevel, int semester, string searchTerm)
        {
            try
            {
                // Get student's program
                var studentCourse = db.StudentCourses
                    .Include("Section")
                    .Where(sc => sc.StudentId == studentId)
                    .OrderByDescending(sc => sc.DateEnrolled)
                    .FirstOrDefault();

                if (studentCourse == null)
                {
                    return Json(new { success = false, message = "Student not enrolled in any program" }, JsonRequestBehavior.AllowGet);
                }

                int programId = studentCourse.Section.ProgramId;

                // Get curriculum courses for the program, year, and semester
                var availableCourses = db.CurriculumCourses
                    .Where(cc => cc.ProgramId == programId
                        && cc.YearLevel == yearLevel
                        && cc.Semester == semester)
                    .Include("Course")
                    .Select(cc => cc.Course)
                    .Where(c => c.CourseCode.Contains(searchTerm) || c.CourseTitle.Contains(searchTerm))
                    .Take(20)
                    .ToList();

                // Get already enrolled course IDs
                var enrolledCourseIds = db.StudentCourses
                    .Where(sc => sc.StudentId == studentId)
                    .Select(sc => sc.CourseId)
                    .ToList();

                // Filter out already enrolled courses
                var filteredCourses = availableCourses
                    .Where(c => !enrolledCourseIds.Contains(c.Id))
                    .Select(c => new
                    {
                        id = c.Id,
                        code = c.CourseCode,
                        title = c.CourseTitle
                    })
                    .ToList();

                return Json(new { success = true, courses = filteredCourses }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetAvailableCoursesForStudent Error: {ex.Message}");
                return Json(new { success = false, message = "Error: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // GET: Course/GetStudentCourses
        [HttpGet]
        public JsonResult GetStudentCourses(int studentId)
        {
            try
            {
                var studentCourses = db.StudentCourses
                    .Where(sc => sc.StudentId == studentId)
                    .Include("Course")
                    .Include("Section")
                    .Include("Section.Program")
                    .ToList()
                    .Select(sc => new
                    {
                        id = sc.Id,
                        courseId = sc.Course.Id,
                        courseCode = sc.Course.CourseCode,
                        courseTitle = sc.Course.CourseTitle,
                        yearLevel = sc.Section.YearLevel,
                        semester = GetSemesterFromCurriculum(sc.Section.ProgramId, sc.Course.Id, sc.Section.YearLevel),
                        dateEnrolled = sc.DateEnrolled
                    })
                    .ToList();

                return Json(new { success = true, courses = studentCourses }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetStudentCourses Error: {ex.Message}");
                return Json(new { success = false, message = "Error: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // Helper method to get semester from curriculum
        private int GetSemesterFromCurriculum(int programId, int courseId, int yearLevel)
        {
            var curriculumCourse = db.CurriculumCourses
                .FirstOrDefault(cc => cc.ProgramId == programId
                    && cc.CourseId == courseId
                    && cc.YearLevel == yearLevel);

            return curriculumCourse?.Semester ?? 1;
        }
    }
}
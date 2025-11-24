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
            bool titleExists = db.Courses.Any(c => c.CourseTitle.Trim().ToLower() == model.CourseTitle.Trim().ToLower());
            
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
                return View("~/Views/IT/CreateCourse.cshtml", model);
            }

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
        public JsonResult AddCoursesToStudent(int studentId, string courseDataJson)
        {
            try
            {
                if (string.IsNullOrEmpty(courseDataJson))
                {
                    return Json(new { success = false, message = "No courses selected" });
                }

                var courseDataList = new List<dynamic>();
                
                var jsonArray = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(courseDataJson);
                
                if (jsonArray == null || jsonArray.Count == 0)
                {
                    return Json(new { success = false, message = "No courses selected" });
                }

                int addedCount = 0;
                int updatedCount = 0;
                foreach (var item in jsonArray)
                {
                    int courseId = Convert.ToInt32(item["courseId"]);
                    int sectionId = Convert.ToInt32(item["sectionId"]);

                    var existingEnrollment = db.StudentCourses
                        .FirstOrDefault(sc => sc.StudentId == studentId && sc.CourseId == courseId);

                    if (existingEnrollment != null)
                    {
                        // Already enrolled to this course; update section/details if needed
                        if (existingEnrollment.SectionId != sectionId)
                        {
                            var targetSectionSchedule = db.StudentCourses
                                .FirstOrDefault(sc => sc.CourseId == courseId && sc.SectionId == sectionId);

                            existingEnrollment.SectionId = sectionId;
                            existingEnrollment.Day = targetSectionSchedule?.Day;
                            existingEnrollment.TimeFrom = targetSectionSchedule?.TimeFrom;
                            existingEnrollment.TimeTo = targetSectionSchedule?.TimeTo;
                            existingEnrollment.DateEnrolled = DateTime.Now;
                            updatedCount++;
                        }
                    }
                    else
                    {
                        var existingCourseTime = db.StudentCourses
                            .FirstOrDefault(sc => sc.CourseId == courseId && sc.SectionId == sectionId);

                        var newEnrollment = new StudentCourse
                        {
                            StudentId = studentId,
                            CourseId = courseId,
                            SectionId = sectionId,
                            Day = existingCourseTime?.Day,
                            TimeFrom = existingCourseTime?.TimeFrom,
                            TimeTo = existingCourseTime?.TimeTo,
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
                    message = $"Successfully added {addedCount} course(s) and updated {updatedCount} existing enrollment(s).",
                    addedCount,
                    updatedCount
                });
            }
            catch (Exception ex)
            {
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
        public JsonResult GetAvailableCoursesForStudent(int studentId, string searchTerm)
        {
            try
            {
                var studentCourse = db.StudentCourses
                    .Include("Section")
                    .Include("Section.Program")
                    .Where(sc => sc.StudentId == studentId)
                    .OrderByDescending(sc => sc.DateEnrolled)
                    .FirstOrDefault();

                if (studentCourse == null)
                {
                    return Json(new { success = false, message = "Student not enrolled in any program" }, JsonRequestBehavior.AllowGet);
                }

                int programId = studentCourse.Section.ProgramId;
                string programCode = studentCourse.Section.Program.ProgramCode;

                var enrolledCourseIds = db.StudentCourses
                    .Where(sc => sc.StudentId == studentId)
                    .Select(sc => sc.CourseId)
                    .Distinct()
                    .ToList();


                var searchTermLower = searchTerm.ToLower();
                var curriculumCourses = db.CurriculumCourses
                    .Where(cc => cc.ProgramId == programId)
                    .Include(cc => cc.Course)
                    .Include(cc => cc.Program)
                    .ToList()
                    .Where(cc => !enrolledCourseIds.Contains(cc.CourseId) &&
                                (cc.Course.CourseCode.ToLower().Contains(searchTermLower) || 
                                cc.Course.CourseTitle.ToLower().Contains(searchTermLower)))
                    .ToList();

                var courseResults = new List<object>();
                foreach (var cc in curriculumCourses)
                {
                    var sections = db.Sections
                        .Where(s => s.ProgramId == cc.ProgramId && s.YearLevel == cc.YearLevel)
                        .ToList();

                    foreach (var section in sections)
                    {
                        var existingCourseTime = db.StudentCourses
                            .FirstOrDefault(sc => sc.CourseId == cc.CourseId && sc.SectionId == section.Id);

                        courseResults.Add(new
                        {
                            id = cc.CourseId,
                            code = cc.Course.CourseCode,
                            title = cc.Course.CourseTitle,
                            yearLevel = cc.YearLevel,
                            semester = cc.Semester,
                            sectionId = section.Id,
                            sectionName = section.SectionName,
                            programCode = programCode,
                            day = existingCourseTime?.Day,
                            timeFrom = existingCourseTime?.TimeFrom,
                            timeTo = existingCourseTime?.TimeTo
                        });
                    }
                }

                return Json(new { success = true, courses = courseResults }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // GET: Course/GetStudentCourses
        [HttpGet]
        public JsonResult GetStudentCourses(int studentId)
        {
            try
            {
                var studentCoursesData = db.StudentCourses
                    .Where(sc => sc.StudentId == studentId)
                    .Include(sc => sc.Course)
                    .Include(sc => sc.Section)
                    .Include(sc => sc.Section.Program)
                    .ToList();

                var studentCourses = studentCoursesData
                    .Select(sc =>
                    {
                        var semester = GetSemesterFromCurriculum(sc.Section.ProgramId, sc.CourseId, sc.Section.YearLevel);

                        var teacherAssignment = db.TeacherCourseSections
                            .Include(tcs => tcs.Teacher)
                            .FirstOrDefault(tcs => tcs.CourseId == sc.CourseId 
                                                && tcs.SectionId == sc.SectionId 
                                                && tcs.Semester == semester);

                        return new
                        {
                            id = sc.Id,
                            courseId = sc.CourseId,
                            sectionId = sc.SectionId,
                            courseCode = sc.Course.CourseCode,
                            courseTitle = sc.Course.CourseTitle,
                            yearLevel = sc.Section.YearLevel,
                            semester = semester,
                            day = sc.Day,
                            timeFrom = sc.TimeFrom,
                            timeTo = sc.TimeTo,
                            teacherName = teacherAssignment != null ? $"{teacherAssignment.Teacher.FirstName} {teacherAssignment.Teacher.LastName}" : null,
                            teacherEmail = teacherAssignment != null ? teacherAssignment.Teacher.Email : null,
                            dateEnrolled = sc.DateEnrolled
                        };
                    })
                    .ToList();

                return Json(new { success = true, courses = studentCourses }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
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
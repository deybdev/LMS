using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace LMS.Controllers
{
    public class StudentController : Controller
    {
        private readonly LMSContext db = new LMSContext();
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Course()
        {
            return View();
        }

        public ActionResult TodoList()
        {
            return View();
        }

        public ActionResult Notification()
        {
            return View();
        }

        public new ActionResult Profile()
        {
            return View();
        }

        // GET: Student/SearchStudents
        [HttpGet]
        public JsonResult SearchStudents(string searchTerm)
        {
            try
            {
                if (string.IsNullOrEmpty(searchTerm))
                {
                    return Json(new { success = false, message = "Search term is required" }, JsonRequestBehavior.AllowGet);
                }

                var students = db.Users
                    .Where(u => u.Role == "Student" && (
                        u.UserID.Contains(searchTerm) ||
                        u.FirstName.Contains(searchTerm) ||
                        u.LastName.Contains(searchTerm) ||
                        u.Email.Contains(searchTerm)
                    ))
                    .Select(u => new
                    {
                        id = u.Id,
                        studentId = u.UserID,
                        firstName = u.FirstName,
                        lastName = u.LastName,
                        email = u.Email
                    })
                    .Take(10)
                    .ToList();

                var enrichedStudents = students.Select(s =>
                {
                    var studentCourse = db.StudentCourses
                        .Where(sc => sc.StudentId == s.id)
                        .OrderByDescending(sc => sc.DateEnrolled)
                        .FirstOrDefault();

                    string program = "Not Assigned";
                    int yearLevel = 0;
                    string section = "N/A";

                    if (studentCourse != null)
                    {
                        var sectionInfo = db.Sections
                            .Include("Program")
                            .FirstOrDefault(sec => sec.Id == studentCourse.SectionId);

                        if (sectionInfo != null)
                        {
                            program = sectionInfo.Program.ProgramName;
                            yearLevel = sectionInfo.YearLevel;
                            section = sectionInfo.SectionName;
                        }
                    }

                    return new
                    {
                        s.id,
                        s.studentId,
                        s.firstName,
                        s.lastName,
                        s.email,
                        program,
                        yearLevel,
                        section
                    };
                }).ToList();

                return Json(new { success = true, students = enrichedStudents }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SearchStudents Error: {ex.Message}");
                return Json(new { success = false, message = "Error: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

    }
}
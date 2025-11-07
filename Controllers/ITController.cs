using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace LMS.Controllers
{
    public class ITController : Controller
    {
        private readonly LMSContext db = new LMSContext();


        // GET: IT
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Course()

        {
            if (Session["Id"] == null || (string)Session["Role"] != "IT")
            {
                TempData["ErrorMessage"] = "Session expired. Please log in again.";
                return RedirectToAction("Login", "Home");
            }

            // Include Teacher, Materials, and CourseUsers
            var courses = db.Courses
                .Include("Materials.MaterialFiles")
                .Include("CourseUsers")  // Include course users to count students
                .ToList();

            return View(courses);
        }

        // GET: IT/GetCourseDetails
        [HttpGet]
        public JsonResult GetCourseDetails(int id)
        {
            try
            {
                if (Session["Id"] == null || (string)Session["Role"] != "IT")
                {
                    return Json(new { success = false, message = "Session expired. Please log in again." }, JsonRequestBehavior.AllowGet);
                }

                // Get course including teacher and materials
                var course = db.Courses
                    .Include("Teacher")
                    .Include("Materials.MaterialFiles")
                    .FirstOrDefault(c => c.Id == id);

                if (course == null)
                {
                    return Json(new { success = false, message = "Course not found" }, JsonRequestBehavior.AllowGet);
                }

                // Get students
                var studentsList = (from cu in db.CourseUsers
                                    join u in db.Users on cu.StudentId equals u.Id
                                    where cu.CourseId == id
                                    select new
                                    {
                                        id = u.Id,
                                        userId = u.UserID,
                                        firstName = u.FirstName,
                                        lastName = u.LastName,
                                        email = u.Email,
                                        department = u.Department ?? "N/A"
                                    }).ToList();

                // Get materials along with their files
                var materialsList = course.Materials?.Select(m => new
                {
                    id = m.Id,
                    title = m.Title,
                    type = m.Type,
                    description = m.Description,
                    uploadedAt = m.UploadedAt.ToString("MMM dd, yyyy"),
                    files = m.MaterialFiles.Select(f => new
                    {
                        id = f.Id,
                        fileName = f.FileName,
                        filePath = f.FilePath,
                        sizeInMB = f.SizeInMB
                    }).ToList()
                }).ToList();

                var responseData = new
                {
                    success = true,
                    title = course.CourseTitle ?? "Untitled Course",
                    code = course.CourseCode ?? "N/A",
                    //department = course.Teacher?.Department ?? "Unknown",
                    //teacherName = course.Teacher != null ? $"{course.Teacher.FirstName} {course.Teacher.LastName}" : "Unknown Teacher",
                    //teacherEmail = course.Teacher?.Email ?? "N/A",
                    studentCount = studentsList.Count,
                    materialCount = course.Materials?.Count ?? 0,
                    students = studentsList,
                    materials = materialsList
                };

                return Json(responseData, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetCourseDetails Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                return Json(new { success = false, message = "Error loading course details: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // GET: IT/CreateCourse
        public ActionResult CreateCourse()
        {
            return View();
        }
        public ActionResult EditCourse()
        {
            return View();
        }
        public ActionResult AssignedCourses()
        {
            return View();
        }
        public ActionResult AssignCourse()
        {
            return View();
        }
        public ActionResult Schedule()
        {
            return View();
        }
        public new ActionResult Profile()
        {
            return View();
        }
    }
}
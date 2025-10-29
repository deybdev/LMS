using Microsoft.AspNetCore.Mvc;

namespace G2AcademyLMS.Controllers
{
    public class StudentController : Controller
    {
        public IActionResult Dashboard()
        {
            return View();
        }

        public IActionResult Profile()
        {
            return View();
        }

        public IActionResult Grades()
        {
            return View();
        }

        public IActionResult Attendance()
        {
            return View();
        }

        public IActionResult Achievements()
        {
            return View();
        }

        public IActionResult EditProfile()
        {
            return View();
        }

        public IActionResult Notifications()
        {
            return View();
        }

        public IActionResult TodoList()
        {
            return View();
        }

        public IActionResult StartAssignment()
        {
            return View();
        }

        public IActionResult StartProject()
        {
            return View();
        }

        public IActionResult OverdueMissing()
        {
            return View();
        }

        public IActionResult ViewOverdueDetails()
        {
            return View();
        }

        public IActionResult Finished()
        {
            return View();
        }

        public IActionResult ViewFinishedDetails()
        {
            return View();
        }

        public IActionResult Courses()
        {
            return View();
        }

        public IActionResult OngoingCourses()
        {
            return View();
        }

        public IActionResult ViewCourse(int? id)
        {
            // Pass course ID to view for dynamic content
            ViewBag.CourseId = id ?? 1;
            return View();
        }

        public IActionResult ViewLearningMaterial(int? id)
        {
            // Pass material ID to view for content loading
            ViewBag.MaterialId = id ?? 1;
            return View();
        }

        public IActionResult ViewPDFMaterial(int? id)
        {
            // Pass material ID to view for content loading
            ViewBag.MaterialId = id ?? 1;
            return View();
        }

        public IActionResult StartLearningMaterial(int? id)
        {
            // For demo purposes, we'll pass the material ID to the view
            ViewBag.MaterialId = id ?? 1;
            return View("ViewLearningMaterial"); // Reuse the same view since layout is identical
        }

        public IActionResult ViewCourseClasswork(int? id)
        {
            // Pass course ID to view for dynamic content
            ViewBag.CourseId = id ?? 1;
            return View();
        }

        public IActionResult ViewCourseAnnouncements(int? id)
        {
            // Pass course ID to view for dynamic content
            ViewBag.CourseId = id ?? 1;
            return View();
        }

        public IActionResult PostAnnouncement(int? id)
        {
            // Pass course ID to view for dynamic content
            ViewBag.CourseId = id ?? 1;
            return View();
        }

        public IActionResult ViewAnnouncementDetails(int? id, int? announcementId)
        {
            // Pass course and announcement IDs for detailed view
            ViewBag.CourseId = id ?? 1;
            ViewBag.AnnouncementId = announcementId ?? 1;
            return View();
        }
    }
}
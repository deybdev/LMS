using LMS.Models;
using System;
using System.Collections.Generic;
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
        public ActionResult CreateCourse(Course model, string SelectedStudentsJson)
        {
           

            // Save course first
            model.DateCreated = DateTime.Now;
            db.Courses.Add(model);
            db.SaveChanges();

            //if (!string.IsNullOrWhiteSpace(SelectedStudentsJson))
            //{
            //    var students = Newtonsoft.Json.JsonConvert.DeserializeObject<List<StudentDto>>(SelectedStudentsJson);

            //    if (students != null && students.Count > 0)
            //    {
            //        foreach (var s in students)
            //        {
            //            // guard against duplicates
            //            var exists = db.CourseUsers.FirstOrDefault(cu => cu.CourseId == model.Id && cu.StudentId == s.id);
            //            if (exists == null)
            //            {
            //                var cu = new CourseUser
            //                {
            //                    CourseId = model.Id,
            //                    StudentId = s.id,
            //                    DateAdded = DateTime.Now
            //                };
            //                db.CourseUsers.Add(cu);
            //                System.Diagnostics.Debug.WriteLine($"Adding CourseUser CourseId={model.Id} StudentId={s.id}");
            //            }
            //        }

            //        db.SaveChanges();
            //    }

            //}

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

            // Remove related CourseUser entries
            var courseUsers = db.CourseUsers.Where(cu => cu.CourseId == id).ToList();
            if (courseUsers.Any())
                db.CourseUsers.RemoveRange(courseUsers);

            db.Courses.Remove(course);
            db.SaveChanges();

            return Json(new { success = true, message = $"Course '{courseTitle}' deleted successfully!" });
        }


    }
}
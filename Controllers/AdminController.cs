using LMS.Models;
using System;
using System.Linq;
using System.Web.Mvc;
using System.Collections.Generic;
using System.Data.Entity;

namespace LMS.Controllers
{
    public class AdminController : Controller
    {
        private readonly LMSContext db = new LMSContext();

        public class StudentDto
        {
            public int Id { get; set; }
            public string UserID { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Email { get; set; }
            public string Department { get; set; }
        }



        // GET: Admin(View)
        public ActionResult Index()
        {
            var users = db.Users.ToList();
            var events = db.Events.ToList();
            var courses = db.Courses.ToList();
            var auditLogs = db.AuditLogs
                              .OrderByDescending(a => a.Timestamp)
                              .Take(4)
                              .ToList();

            // Build the view model
            var model = new AdminDashboardViewModel
            {
                TotalStudents = users.Count(u => u.Role == "Student"),
                TotalTeachers = users.Count(u => u.Role == "Teacher"),
                TotalCourses = courses.Count(),
                AuditLogs = auditLogs,
                UpcomingEvents = events
                                 .Where(e => e.StartDate >= DateTime.Now)
                                 .OrderBy(e => e.StartDate)
                                 .Take(3)
                                 .ToList()
            };

            return View(model);
        }


        // GET: Admin/Calendar
        public ActionResult Calendar()
        {
            return View();
        }

        // GET: Admin/Program
        public ActionResult Programs()
        {
            ViewBag.Departments = db.Departments.ToList();

            var programs = db.Programs
                             .Include(p => p.Department)
                             .ToList();

            return View(programs);
        }


        // GET: Admin/Departments
        public ActionResult Departments()
        {
            var departments = db.Departments
                                .OrderBy(d => d.DepartmentName)
                                .ToList();

            return View(departments);
        }

        public ActionResult Sections()
        {
            var sections = db.Sections
                .Include("Program")
                .Include("Program.Department")
                .ToList();
            ViewBag.Programs = db.Programs.ToList();
            ViewBag.Departments = db.Departments.ToList();
            return View(sections);
        }


        // GET: Course

        //GET: Admin/Logs
        public ActionResult Logs()
        {
            var logs = db.AuditLogs.OrderByDescending(l => l.Timestamp).ToList();
            return View(logs);
        }

        // POST: Admin/DeleteLogs
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteLogs(int id)
        {
            var log = db.AuditLogs.Find(id);
            if (log == null)
            {
                return Json(new { success = false, message = "Log not found!" });
            }

            db.AuditLogs.Remove(log);
            db.SaveChanges();


            return Json(new { success = true, message = "Log deleted successfully!" });
        }
        // GET: Admin/GetEvents
        public JsonResult GetEvents()
        {
            try
            {
                var events = db.Events.ToList().Select(e => new
                {
                    e.Id,
                    e.Title,
                    e.Type,
                    StartDate = e.StartDate.ToString("yyyy-MM-dd"),
                    EndDate = e.EndDate.ToString("yyyy-MM-dd"),
                    e.Description
                });

                return Json(events, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetEvents Error: {ex.Message}");
                return Json(new { error = "Failed to load events", message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        //GET: Admin/GetUpcomingEvents
        public ActionResult GetUpcomingEvents()
        {
            try
            {
                // Get only future events
                var upcomingEvents = db.Events
                    .Where(e => e.StartDate >= DateTime.Now)
                    .OrderBy(e => e.StartDate)
                    .Take(3)
                    .ToList();

                return View(upcomingEvents);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetUpcomingEvents Error: {ex.Message}");
                ViewBag.Error = "Failed to load events.";
                return View(new List<Event>());
            }
        }


        // POST: Admin/SaveEvent
        [HttpPost]
        public JsonResult SaveEvent(Event e)
        {
            try
            {
                string logMessage = "";

                if (e.Id > 0)
                {
                    var existing = db.Events.Find(e.Id);
                    if (existing != null)
                    {
                        existing.Title = e.Title;
                        existing.Type = e.Type;
                        existing.StartDate = e.StartDate;
                        existing.EndDate = e.EndDate;
                        existing.Description = e.Description;

                        logMessage = $"Updated event: {existing.Title}";
                    }
                    else
                    {
                        return Json(new { success = false, message = "Event not found" });
                    }
                }
                else
                {
                    db.Events.Add(e);
                    logMessage = $"Created new event: {e.Title}";
                }

                db.SaveChanges();

                // Log after saving
                LogAction("Event Management", logMessage, Session["FullName"]?.ToString(), "Admin");

                return Json(new { success = true, message = "Event saved successfully" });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SaveEvent Error: {ex.Message}");
                return Json(new { success = false, message = "Failed to save event: " + ex.Message });
            }
        }


        // POST: Admin/DeleteEvent
        [HttpPost]
        public JsonResult DeleteEvent(int id)
        {
            try
            {
                var ev = db.Events.Find(id);
                if (ev != null)
                {
                    db.Events.Remove(ev);
                    db.SaveChanges();
                    return Json(new { success = true, message = "Event deleted successfully" });
                }
                else
                {
                    return Json(new { success = false, message = "Event not found" });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DeleteEvent Error: {ex.Message}");
                return Json(new { success = false, message = "Failed to delete event: " + ex.Message });
            }
        }

        // GET: Admin/ManageUsers
        public ActionResult ManageUsers()
        {
            ViewBag.Programs = db.Programs.ToList();
            ViewBag.Departments = db.Departments.ToList();

            var users = db.Users
                .OrderBy(u => u.LastName)
                .ToList();

            return View(users);
        }



        // Audit Logging
        private void LogAction(string category, string message, string userName = null, string role = null)
        {
            var log = new AuditLog
            {
                Category = category,
                Message = message,
                UserName = userName,
                Role = role,
                Timestamp = DateTime.Now
            };

            db.AuditLogs.Add(log);
            db.SaveChanges();
        }





    }
}

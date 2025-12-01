using LMS.Models;
using System;
using System.Linq;
using System.Web.Mvc;
using System.Collections.Generic;
using System.Data.Entity;
using System.Text;

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
            try
            {
                var logs = db.AuditLogs
                    .OrderByDescending(l => l.Timestamp)
                    .ToList();

                return View(logs);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Logs Error: {ex.Message}");
                ViewBag.Error = "Failed to load logs.";
                return View(new List<AuditLog>());
            }
        }

        // GET: Admin/GetLogsData - For AJAX filtering
        [HttpGet]
        public JsonResult GetLogsData(string category = "", string timeRange = "", string search = "")
        {
            try
            {
                var query = db.AuditLogs.AsQueryable();

                // Apply category filter
                if (!string.IsNullOrEmpty(category))
                {
                    query = query.Where(l => l.Category.ToLower() == category.ToLower());
                }

                // Apply time range filter
                if (!string.IsNullOrEmpty(timeRange))
                {
                    DateTime filterDate;
                    switch (timeRange.ToLower())
                    {
                        case "today":
                            filterDate = DateTime.Today;
                            query = query.Where(l => l.Timestamp >= filterDate);
                            break;
                        case "yesterday":
                            var yesterday = DateTime.Today.AddDays(-1);
                            query = query.Where(l => l.Timestamp >= yesterday && l.Timestamp < DateTime.Today);
                            break;
                        case "week":
                            filterDate = DateTime.Today.AddDays(-7);
                            query = query.Where(l => l.Timestamp >= filterDate);
                            break;
                        case "month":
                            filterDate = DateTime.Today.AddDays(-30);
                            query = query.Where(l => l.Timestamp >= filterDate);
                            break;
                    }
                }

                // Apply search filter
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(l => 
                        l.Message.Contains(search) || 
                        l.UserName.Contains(search) || 
                        l.Category.Contains(search));
                }

                var logs = query
                    .OrderByDescending(l => l.Timestamp)
                    .ToList()
                    .Select(l => new
                    {
                        l.Id,
                        l.Category,
                        l.Message,
                        l.UserName,
                        l.Role,
                        Timestamp = l.Timestamp.ToString("yyyy-MM-dd HH:mm:ss")
                    });

                return Json(logs, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetLogsData Error: {ex.Message}");
                return Json(new { error = "Failed to filter logs", message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // POST: Admin/ClearLogs - Clear all logs (optional feature)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult ClearLogs()
        {
            try
            {
                var userRole = Session["Role"]?.ToString();
                if (userRole != "Admin")
                {
                    return Json(new { success = false, message = "Unauthorized access." });
                }

                db.AuditLogs.RemoveRange(db.AuditLogs);
                db.SaveChanges();

                // Log the action
                LogAction("System", "All audit logs cleared", Session["FullName"]?.ToString(), "Admin");

                return Json(new { success = true, message = "All logs cleared successfully." });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ClearLogs Error: {ex.Message}");
                return Json(new { success = false, message = "Failed to clear logs: " + ex.Message });
            }
        }

        // POST: Admin/DeleteLogs
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteLogs(int id)
        {
            try
            {
                var log = db.AuditLogs.Find(id);
                if (log == null)
                {
                    return Json(new { success = false, message = "Log entry not found!" });
                }

                var logDescription = $"{log.Category} - {log.Message.Substring(0, Math.Min(50, log.Message.Length))}...";
                
                db.AuditLogs.Remove(log);
                db.SaveChanges();

                // Log the deletion action
                LogAction("System", $"Deleted log entry: {logDescription}", Session["FullName"]?.ToString(), Session["Role"]?.ToString());

                return Json(new { success = true, message = "Log entry deleted successfully!" });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DeleteLogs Error: {ex.Message}");
                return Json(new { success = false, message = "An error occurred while deleting the log entry." });
            }
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

            // Check if there's upload summary data in Session
            if (Session["UploadSummary"] != null)
            {
                ViewBag.UploadSummary = Session["UploadSummary"];
                // Clear it from Session after retrieving it
                Session.Remove("UploadSummary");
            }

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

        // GET: Admin/ExportLogs - Export logs as CSV
        public ActionResult ExportLogs(string category = "", string timeRange = "", string search = "")
        {
            try
            {
                var query = db.AuditLogs.AsQueryable();

                // Apply same filters as GetLogsData
                if (!string.IsNullOrEmpty(category))
                {
                    query = query.Where(l => l.Category.ToLower() == category.ToLower());
                }

                if (!string.IsNullOrEmpty(timeRange))
                {
                    DateTime filterDate;
                    switch (timeRange.ToLower())
                    {
                        case "today":
                            filterDate = DateTime.Today;
                            query = query.Where(l => l.Timestamp >= filterDate);
                            break;
                        case "yesterday":
                            var yesterday = DateTime.Today.AddDays(-1);
                            query = query.Where(l => l.Timestamp >= yesterday && l.Timestamp < DateTime.Today);
                            break;
                        case "week":
                            filterDate = DateTime.Today.AddDays(-7);
                            query = query.Where(l => l.Timestamp >= filterDate);
                            break;
                        case "month":
                            filterDate = DateTime.Today.AddDays(-30);
                            query = query.Where(l => l.Timestamp >= filterDate);
                            break;
                    }
                }

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(l => 
                        l.Message.Contains(search) || 
                        l.UserName.Contains(search) || 
                        l.Category.Contains(search));
                }

                var logs = query.OrderByDescending(l => l.Timestamp).ToList();

                // Generate CSV content
                var csv = new StringBuilder();
                csv.AppendLine("Timestamp,Category,Message,User,Role");

                foreach (var log in logs)
                {
                    var message = log.Message?.Replace("\"", "\"\"").Replace("\r\n", " ").Replace("\n", " ");
                    var user = log.UserName ?? "System";
                    var role = log.Role ?? "Automated";
                    
                    csv.AppendLine($"\"{log.Timestamp:yyyy-MM-dd HH:mm:ss}\",\"{log.Category}\",\"{message}\",\"{user}\",\"{role}\"");
                }

                var fileName = $"SystemLogs_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                var bytes = Encoding.UTF8.GetBytes(csv.ToString());

                // Log the export action
                LogAction("System", $"Exported {logs.Count} log entries to CSV", Session["FullName"]?.ToString(), Session["Role"]?.ToString());

                return File(bytes, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ExportLogs Error: {ex.Message}");
                TempData["AlertType"] = "error";
                TempData["AlertMessage"] = "Failed to export logs: " + ex.Message;
                return RedirectToAction("Logs");
            }
        }

    }
}

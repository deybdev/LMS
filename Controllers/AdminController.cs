using LMS.Models;
using LMS.Helpers;
using ClosedXML.Excel;
using System.IO;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using OfficeOpenXml;
using System.Collections.Generic;
using Newtonsoft.Json;



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

        // GET: Course
        public ActionResult Course()
        {
            if (Session["Id"] == null || (string)Session["Role"] != "Admin")
            {
                TempData["ErrorMessage"] = "Session expired. Please log in again.";
                return RedirectToAction("Login", "Home");
            }

            // Include Teacher, Materials, and CourseUsers
            var courses = db.Courses
                .Include("Teacher")
                .Include("Materials.MaterialFiles")
                .Include("CourseUsers")  // Include course users to count students
                .ToList();

            return View(courses);
        }

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
            var users = db.Users
                .OrderBy(u => u.LastName)
                .ToList();

            return View(users);
        }

        // POST: Admin/DeleteUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteUser(int id)
        {
            var user = db.Users.Find(id);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found!" });
            }

            string userFullName = $"{user.FirstName} {user.LastName}";
            string userRole = user.Role;

            // If the user is a student, remove their CourseUser entries
            if (user.Role == "Student")
            {
                var courseUsers = db.CourseUsers.Where(cu => cu.StudentId == id).ToList();
                db.CourseUsers.RemoveRange(courseUsers);
            }

            db.Users.Remove(user);
            db.SaveChanges();

            // Log the deletion
            LogAction(
                category: "User Actions",
                message: $"Deleted {userRole} account: {userFullName}",
                userName: Session["FullName"]?.ToString(),
                role: "Admin"
            );

            return Json(new { success = true, message = $"{userRole} deleted successfully!" });
        }


        // POST: Admin/CreateAccount
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateAccount(string role, string firstName, string lastName, string email, string userId, string phone, string department)
        {
                if (string.IsNullOrEmpty(role) || string.IsNullOrEmpty(firstName) ||
                    string.IsNullOrEmpty(lastName) || string.IsNullOrEmpty(email) ||
                    string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(phone) || string.IsNullOrEmpty(department))
                {
                    return Json(new { success = false, message = "All fields are required!" });
                }

                if (db.Users.Any(u => u.Email == email))
                {
                    return Json(new { success = false, message = "Email already exists!" });
                }

                if (db.Users.Any(u => u.UserID == userId))
                {
                    return Json(new { success = false, message = "Teacher/Student ID already exists!" });
                }

                string generatedPassword = GeneratePassword();

                User newUser = new User
                {
                    UserID = userId,
                    FirstName = firstName,
                    LastName = lastName,
                    Email = email,
                    PhoneNumber = phone,
                    Department = department,
                    Password = HashPassword (generatedPassword),
                    Role = role,
                    DateCreated = DateTime.Now
                };
                
                db.Users.Add(newUser);
                db.SaveChanges();

                // Send welcome email using EmailHelper
                var emailSent = EmailHelper.SendEmail(
                    toEmail: email,
                    subject: "Welcome to G2 Academy University LMS",
                    htmlBody: EmailHelper.CreateWelcomeEmailTemplate(email, generatedPassword, $"{firstName} {lastName}")
                );

            LogAction("User Actions", $"Created new {role} account: {firstName} {lastName} ({email})", null, "Admin");

            // Optional: Log if email sending failed
            if (!emailSent)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to send welcome email to {email}");
                }


            return Json(new { 
                    success = true, 
                    message = $"Account created successfully!",
                    userData = new {
                        id = newUser.Id,
                        userId = newUser.UserID,
                        firstName = newUser.FirstName,
                        lastName = newUser.LastName,
                        email = newUser.Email,
                        phone = newUser.PhoneNumber,
                        department = newUser.Department,
                        role = newUser.Role
                    }
                });
        }

        // POST: Admin/EditAccount
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditAccount(int id, string role, string firstName, string lastName, string email, string userId, string phone, string department)
        {
            try
            {
                var existingUser = db.Users.FirstOrDefault(u => u.Id == id);
                if (existingUser == null)
                    return Json(new { success = false, message = "User not found!" });

                // Build log message for changes
                var changes = new List<string>();

                if (existingUser.FirstName != firstName || existingUser.LastName != lastName)
                    changes.Add($"Full Name: '{existingUser.FirstName} {existingUser.LastName}' → '{firstName} {lastName}'");
                if (existingUser.Email != email)
                    changes.Add($"Email: '{existingUser.Email}' → '{email}'");
                if (existingUser.UserID != userId)
                    changes.Add($"User ID: '{existingUser.UserID}' → '{userId}'");
                if (existingUser.PhoneNumber != phone)
                    changes.Add($"Phone: '{existingUser.PhoneNumber}' → '{phone}'");
                if (existingUser.Department != department)
                    changes.Add($"Department: '{existingUser.Department}' → '{department}'");
                if (existingUser.Role != role)
                    changes.Add($"Role: '{existingUser.Role}' → '{role}'");

                // Update fields
                existingUser.FirstName = firstName;
                existingUser.LastName = lastName;
                existingUser.Email = email;
                existingUser.UserID = userId;
                existingUser.PhoneNumber = phone;
                existingUser.Department = department;
                existingUser.Role = role;

                db.SaveChanges();

                // Log changes only if there were any
                if (changes.Any())
                {
                    string logMessage = $"Updated user: {string.Join("; ", changes)}";
                    LogAction("User Actions", logMessage, Session["FullName"]?.ToString(), "Admin");
                }

                return Json(new
                {
                    success = true,
                    message = "Account updated successfully!",
                    userData = new
                    {
                        id = existingUser.Id,
                        userId = existingUser.UserID,
                        firstName = existingUser.FirstName,
                        lastName = existingUser.LastName,
                        email = existingUser.Email,
                        phone = existingUser.PhoneNumber,
                        department = existingUser.Department,
                        role = existingUser.Role
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }


        // GET: Admin/GetUserData
        public ActionResult GetUserData(int id)
        {
            try
            {
                var user = db.Users.Find(id);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" }, JsonRequestBehavior.AllowGet);
                }

                return Json(new
                {
                    success = true,
                    user = new
                    {
                        id = user.Id,
                        userId = user.UserID,
                        firstName = user.FirstName,
                        lastName = user.LastName,
                        email = user.Email,
                        phoneNumber = user.PhoneNumber,
                        department = user.Department,
                        role = user.Role
                    }
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // POST: Admin/UploadAccounts
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UploadAccounts(HttpPostedFileBase file, string role)
        {
            if (file == null || file.ContentLength == 0)
            {
                TempData["AlertMessage"] = "Please select an Excel file.";
                TempData["AlertType"] = "warning";
                return RedirectToAction("ManageUsers");
            }

            if (string.IsNullOrEmpty(role))
            {
                TempData["AlertMessage"] = "Please select a valid role.";
                TempData["AlertType"] = "warning";
                return RedirectToAction("ManageUsers");
            }

            try
            {
                using (var stream = new MemoryStream())
                {
                    file.InputStream.CopyTo(stream);
                    using (var workbook = new XLWorkbook(stream))
                    {
                        var worksheet = workbook.Worksheet(1);
                        var rows = worksheet.RangeUsed()?.RowsUsed().Skip(1); // skip header row

                        if (rows == null || !rows.Any())
                        {
                            TempData["AlertMessage"] = "Excel file appears to be empty or has no data rows.";
                            TempData["AlertType"] = "warning";
                            return RedirectToAction("ManageUsers");
                        }

                        int successCount = 0;
                        int errorCount = 0;
                        int totalRows = rows.Count();
                        var errorDetails = new List<string>();

                        foreach (var row in rows)
                        {
                            try
                            {
                                var userId = row.Cell(1).GetString().Trim();
                                var lastName = row.Cell(2).GetString().Trim();
                                var firstName = row.Cell(3).GetString().Trim();
                                var email = row.Cell(4).GetString().Trim();
                                var phone = row.Cell(5).GetString().Trim();
                                var department = row.Cell(6).GetString().Trim();

                                // Validate required fields
                                if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName) ||
                                    string.IsNullOrEmpty(email) || string.IsNullOrEmpty(userId))
                                {
                                    errorCount++;
                                    errorDetails.Add($"Missing required fields (ID, Name, or Email)");
                                    continue;
                                }

                                // Validate email format
                                if (!IsValidEmail(email))
                                {
                                    errorCount++;
                                    errorDetails.Add($"Invalid email format - {email}");
                                    continue;
                                }

                                // Check for duplicate email
                                if (db.Users.Any(u => u.Email == email))
                                {
                                    errorCount++;
                                    errorDetails.Add($"Email already exists - {email}");
                                    continue;
                                }

                                // Check for duplicate UserID
                                if (db.Users.Any(u => u.UserID == userId))
                                {
                                    errorCount++;
                                    errorDetails.Add($"User ID already exists - {userId}");
                                    continue;
                                }

                                string password = GeneratePassword();

                                var newUser = new User
                                {
                                    UserID = userId,
                                    LastName = lastName,
                                    FirstName = firstName,
                                    Email = email,
                                    PhoneNumber = phone,
                                    Department = department,
                                    Password = HashPassword(password),
                                    Role = role,
                                    DateCreated = DateTime.Now
                                };

                                db.Users.Add(newUser);
                                successCount++;

                                try
                                {
                                    EmailHelper.SendEmail(
                                        toEmail: email,
                                        subject: "Welcome to G2 Academy University LMS",
                                        htmlBody: EmailHelper.CreateWelcomeEmailTemplate(email, password, $"{firstName} {lastName}")
                                    );
                                }
                                catch (Exception emailEx)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Failed to send email to {email}: {emailEx.Message}");
                                }
                            }
                            catch (Exception rowEx)
                            {
                                errorCount++;
                                errorDetails.Add($"Processing error - {rowEx.Message}");
                            }
                        }

                        LogAction(
                            category: "User Actions",
                            message: $"Bulk upload completed: {successCount} accounts added, {errorCount} errors",
                            userName: Session["FullName"]?.ToString(),
                            role: "Admin"
                        );

                        db.SaveChanges();

                        // Prepare messages
                        if (successCount > 0)
                        {
                            string successMsg = $"Successfully uploaded {successCount} of {totalRows} accounts!";
                            TempData["AlertMessage"] = successMsg;
                            TempData["AlertType"] = "success";

                            if (errorCount > 0)
                            {
                                string errorMsg = $"{errorCount} records had errors and were skipped.";
                                if (errorDetails.Any())
                                    errorMsg += " Issues: " + string.Join("; ", errorDetails.Take(3));
                                TempData["AlertMessage2"] = errorMsg;
                                TempData["AlertType2"] = "warning";
                            }
                        }
                        else
                        {
                            string errorMsg = "No accounts were uploaded.";
                            if (errorDetails.Any())
                                errorMsg += " Issues found: " + string.Join("; ", errorDetails.Take(3));
                            else
                                errorMsg += " Please check your Excel file format.";

                            TempData["AlertMessage"] = errorMsg;
                            TempData["AlertType"] = "danger";
                        }

                        System.Diagnostics.Debug.WriteLine($"Upload completed: {successCount} successful, {errorCount} errors out of {totalRows} total rows");
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["AlertMessage"] = $"Error uploading accounts: {ex.Message}";
                TempData["AlertType"] = "danger";
                System.Diagnostics.Debug.WriteLine($"Upload Error: {ex.Message}");
            }

            return RedirectToAction("ManageUsers");
        }

        // Helper method to validate email format
        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        // Password Generator
        private string GeneratePassword(int length = 8)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        //Password Hashing
        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }

        // GET: Admin/GetCourseDetails
        [HttpGet]
        public JsonResult GetCourseDetails(int id)
        {
            try
            {
                if (Session["Id"] == null || (string)Session["Role"] != "Admin")
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
                    department = course.Teacher?.Department ?? "Unknown",
                    description = string.IsNullOrWhiteSpace(course.Description) ? "No description provided" : course.Description,
                    teacherName = course.Teacher != null ? $"{course.Teacher.FirstName} {course.Teacher.LastName}" : "Unknown Teacher",
                    teacherEmail = course.Teacher?.Email ?? "N/A",
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

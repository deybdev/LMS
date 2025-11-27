using ClosedXML.Excel;
using LMS.Helpers;
using LMS.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace LMS.Controllers
{
    public class AccountController : Controller
    {
        private readonly LMSContext db = new LMSContext();


        // GET: Account
        public ActionResult Index()
        {
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        public ActionResult Login(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Please enter your email and password.";
                return View();
            }
            var user = db.Users.FirstOrDefault(u => u.Email == email);

            if (user == null)
            {
                ViewBag.Error = "Invalid email or password.";
                return View();
            }

            if (HashPassword(password) == user.Password)
            {
                Session["Id"] = user.Id;
                Session["Role"] = user.Role;
                Session["FirstName"] = user.FirstName;
                Session["LastName"] = user.LastName;
                Session["UserId"] = user.UserID;


                user.LastLogin = DateTime.Now;
                db.SaveChanges();

                switch (user.Role.ToLower())
                {
                    case "admin":
                        return RedirectToAction("Index", "Admin");
                    case "teacher":
                        return RedirectToAction("Index", "Teacher");
                    case "student":
                        return RedirectToAction("Index", "Student");
                    case "it":
                        return RedirectToAction("Index", "IT");
                    default:
                        return RedirectToAction("Index", "Home");
                }
            }
            else
            {
                ViewBag.Error = "Invalid email or password.";
            }

            return View("~/Views/Home/Login.cshtml");

        }

        // POST: Account/UploadAccounts
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
                        var rows = worksheet.RangeUsed()?.RowsUsed().Skip(1);

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
                                string userId = row.Cell(1).GetString().Trim();
                                string lastName = row.Cell(2).GetString().Trim();
                                string firstName = row.Cell(3).GetString().Trim();
                                string email = row.Cell(4).GetString().Trim();
                                string phone = row.Cell(5).GetString().Trim();

                                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(firstName) ||
                                    string.IsNullOrEmpty(lastName) || string.IsNullOrEmpty(email))
                                {
                                    errorCount++;
                                    errorDetails.Add($"Missing required fields (ID, Name, or Email)");
                                    continue;
                                }

                                if (!IsValidEmail(email))
                                {
                                    errorCount++;
                                    errorDetails.Add($"Invalid email format - {email}");
                                    continue;
                                }

                                if (db.Users.Any(u => u.Email == email))
                                {
                                    errorCount++;
                                    errorDetails.Add($"Email already exists - {email}");
                                    continue;
                                }
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
                                    Role = role,
                                    Password = HashPassword(password),
                                    DateCreated = DateTime.Now
                                };

                                db.Users.Add(newUser);
                                successCount++;
                            }
                            catch (Exception rowEx)
                            {
                                errorCount++;
                                errorDetails.Add($"Processing error - {rowEx.Message}");
                            }
                        }

                        db.SaveChanges();

                        // Prepare messages
                        if (successCount > 0)
                        {
                            TempData["AlertMessage"] = $"Successfully uploaded {successCount} of {totalRows} accounts!";
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


        // GET: Account/GetUserData
        public ActionResult GetUserData(int id)
        {
            try
            {
                var user = db.Users.Find(id);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" }, JsonRequestBehavior.AllowGet);
                }

                var userData = new
                {
                    id = user.Id,
                    userId = user.UserID,
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    email = user.Email,
                    role = user.Role,
                    programId = (int?)null,
                    yearLevel = (int?)null,
                    sectionId = (int?)null,
                    department = (int?)null
                };

                // If student, get their course enrollment details
                if (user.Role == "Student")
                {
                    var studentCourse = db.StudentCourses
                        .Where(sc => sc.StudentId == user.Id)
                        .OrderByDescending(sc => sc.DateEnrolled)
                        .FirstOrDefault();

                    if (studentCourse != null)
                    {
                        var section = db.Sections.Find(studentCourse.SectionId);
                        if (section != null)
                        {
                            userData = new
                            {
                                id = user.Id,
                                userId = user.UserID,
                                firstName = user.FirstName,
                                lastName = user.LastName,
                                email = user.Email,
                                role = user.Role,
                                programId = (int?)section.ProgramId,
                                yearLevel = (int?)section.YearLevel,
                                sectionId = (int?)studentCourse.SectionId,
                                department = (int?)section.Program?.DepartmentId
                            };
                        }
                    }
                }

                return Json(new
                {
                    success = true,
                    user = userData
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetUserData Error: {ex.Message}");
                return Json(new { success = false, message = "Error: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // POST: Account/EditAccount
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditAccount(int id, string role, string firstName, string lastName, string userId,
            string email, string phone, int? programId, int? yearLevel, int? sectionId)
        {
            try
            {
                var existingUser = db.Users.FirstOrDefault(u => u.Id == id);
                if (existingUser == null)
                    return Json(new { success = false, message = "User not found!" });

                var changes = new List<string>();

                if (existingUser.FirstName != firstName || existingUser.LastName != lastName)
                    changes.Add($"Full Name: '{existingUser.FirstName} {existingUser.LastName}' → '{firstName} {lastName}'");
                if (existingUser.Email != email)
                    changes.Add($"Email: '{existingUser.Email}' → '{email}'");
                if (existingUser.UserID != userId)
                    changes.Add($"User ID: '{existingUser.UserID}' → '{userId}'");
                if (existingUser.Role != role)
                    changes.Add($"Role: '{existingUser.Role}' → '{role}'");

                existingUser.FirstName = firstName;
                existingUser.LastName = lastName;
                existingUser.Email = email;
                existingUser.UserID = userId;
                existingUser.Role = role;

                db.SaveChanges();

                if (role == "Student" && programId.HasValue && yearLevel.HasValue && sectionId.HasValue)
                {
                    var oldEnrollments = db.StudentCourses.Where(sc => sc.StudentId == id).ToList();
                    if (oldEnrollments.Any())
                    {
                        db.StudentCourses.RemoveRange(oldEnrollments);
                        db.SaveChanges();
                    }
                    int currentSemester = DateTime.Now.Month >= 6 && DateTime.Now.Month <= 10 ? 1 : 2;
                    AutoAssignCoursesToStudent(id, programId.Value, sectionId.Value, yearLevel.Value, currentSemester);
                    changes.Add($"Program/Year/Section updated");
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
                        role = existingUser.Role
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EditAccount Error: {ex.Message}");
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // POST: Account/CreateAccount
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateAccount(string role, string firstName, string lastName, string userId, 
            string email, int? semester, int? programId, int? yearLevel, int? sectionId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(role) || string.IsNullOrWhiteSpace(firstName) ||
                    string.IsNullOrWhiteSpace(lastName) || string.IsNullOrWhiteSpace(userId) ||
                    string.IsNullOrWhiteSpace(email))
                {
                    return Json(new { success = false, message = "Please fill all required fields correctly." });
                }

                if (role == "Student")
                {
                    if (!programId.HasValue || !yearLevel.HasValue || !sectionId.HasValue)
                    {
                        return Json(new { success = false, message = "Please select Program, Year Level, and Section for student accounts." });
                    }
                }

                if (db.Users.Any(u => u.Email == email))
                {
                    return Json(new { success = false, message = "Email already exists!" });
                }

                if (db.Users.Any(u => u.UserID == userId))
                {
                    return Json(new { success = false, message = "Teacher/Student ID already exists!" });
                }

                // Create user
                string generatedPassword = GeneratePassword();
                var user = new User
                {
                    UserID = userId,
                    FirstName = firstName,
                    LastName = lastName,
                    Email = email,
                    Role = role,
                    Password = HashPassword(generatedPassword),
                    DateCreated = DateTime.Now
                };

                db.Users.Add(user);
                db.SaveChanges();

                if (role == "Student" && programId.HasValue && yearLevel.HasValue && sectionId.HasValue)
                {
                    int currentSemester = semester ?? 1;
                    
                    AutoAssignCoursesToStudent(user.Id, programId.Value, sectionId.Value, yearLevel.Value, currentSemester);
                }

                // Send email notification
                try
                {
                    var htmlBody = EmailHelper.GenerateEmailTemplate(
                        EmailType.AccountCreated,
                        new
                        {
                            Name = $"{firstName} {lastName}",
                            Email = email,
                            Password = generatedPassword
                        }
                    );

                    bool emailSent = EmailHelper.SendEmail(
                        toEmail: email,
                        subject: "Your G2 Academy LMS Account",
                        htmlBody: htmlBody
                    );

                    if (!emailSent)
                    {
                        System.Diagnostics.Debug.WriteLine($"WARNING: Email failed to send to {email}");
                    }
                }
                catch (Exception emailEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Email Error: {emailEx.Message}");
                    // Continue even if email fails - account was created successfully
                }

                return Json(new
                {
                    success = true,
                    message = $"{role} account created successfully!",
                    userData = new
                    {
                        id = user.Id,
                        userId = user.UserID,
                        firstName = user.FirstName,
                        lastName = user.LastName,
                        email = user.Email,
                        role = user.Role,
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CreateAccount Error: {ex.Message}");
                return Json(new { success = false, message = "Error creating account: " + ex.Message });
            }
        }

        // POST: Account/DeleteUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteUser(int id)
        {
            try
            {
                var user = db.Users.Find(id);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found!" });
                }

                string userFullName = $"{user.FirstName} {user.LastName}";
                string userRole = user.Role;

                // Delete related records based on user role
                if (userRole == "Student")
                {
                    // Delete student-specific records
                    
                    // 1. Delete attendance records
                    var attendanceRecords = db.AttendanceRecords.Where(ar => ar.StudentId == id).ToList();
                    if (attendanceRecords.Any())
                    {
                        db.AttendanceRecords.RemoveRange(attendanceRecords);
                    }

                    // 2. Delete classwork submissions and their files
                    var submissions = db.ClassworkSubmissions
                        .Where(cs => cs.StudentId == id)
                        .ToList();

                    foreach (var submission in submissions)
                    {
                        // Get and delete physical submission files
                        var submissionFiles = db.SubmissionFiles.Where(sf => sf.SubmissionId == submission.Id).ToList();
                        foreach (var file in submissionFiles)
                        {
                            var filePath = Server.MapPath(file.FilePath);
                            if (System.IO.File.Exists(filePath))
                            {
                                try
                                {
                                    System.IO.File.Delete(filePath);
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Error deleting file {filePath}: {ex.Message}");
                                }
                            }
                        }
                        db.SubmissionFiles.RemoveRange(submissionFiles);
                    }
                    db.ClassworkSubmissions.RemoveRange(submissions);

                    // 3. Delete announcement comments
                    var comments = db.AnnouncementComments.Where(ac => ac.UserId == id).ToList();
                    if (comments.Any())
                    {
                        db.AnnouncementComments.RemoveRange(comments);
                    }

                    // 4. Delete student course enrollments
                    var studentCourses = db.StudentCourses.Where(sc => sc.StudentId == id).ToList();
                    if (studentCourses.Any())
                    {
                        db.StudentCourses.RemoveRange(studentCourses);
                    }
                }
                else if (userRole == "Teacher")
                {
                    // Delete teacher-specific records
                    
                    // 1. Delete announcement comments made by teacher
                    var comments = db.AnnouncementComments.Where(ac => ac.UserId == id).ToList();
                    if (comments.Any())
                    {
                        db.AnnouncementComments.RemoveRange(comments);
                    }

                    // 2. Get teacher course sections
                    var teacherCourseSectionIds = db.TeacherCourseSections
                        .Where(tcs => tcs.TeacherId == id)
                        .Select(tcs => tcs.Id)
                        .ToList();

                    foreach (var tcsId in teacherCourseSectionIds)
                    {
                        // Delete materials and their files
                        var materials = db.Materials.Where(m => m.TeacherCourseSectionId == tcsId).ToList();
                        foreach (var material in materials)
                        {
                            var materialFiles = db.MaterialFiles.Where(mf => mf.MaterialId == material.Id).ToList();
                            foreach (var file in materialFiles)
                            {
                                var filePath = Server.MapPath(file.FilePath);
                                if (System.IO.File.Exists(filePath))
                                {
                                    try
                                    {
                                        System.IO.File.Delete(filePath);
                                    }
                                    catch (Exception ex)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"Error deleting file {filePath}: {ex.Message}");
                                    }
                                }
                            }
                            db.MaterialFiles.RemoveRange(materialFiles);
                        }
                        db.Materials.RemoveRange(materials);

                        // Delete classworks and related data
                        var classworks = db.Classworks.Where(c => c.TeacherCourseSectionId == tcsId).ToList();
                        foreach (var classwork in classworks)
                        {
                            // Delete classwork files
                            var classworkFiles = db.ClassworkFiles.Where(cf => cf.ClassworkId == classwork.Id).ToList();
                            foreach (var file in classworkFiles)
                            {
                                var filePath = Server.MapPath(file.FilePath);
                                if (System.IO.File.Exists(filePath))
                                {
                                    try
                                    {
                                        System.IO.File.Delete(filePath);
                                    }
                                    catch (Exception ex)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"Error deleting file {filePath}: {ex.Message}");
                                    }
                                }
                            }
                            db.ClassworkFiles.RemoveRange(classworkFiles);

                            // Delete submission files for this classwork
                            var cwSubmissions = db.ClassworkSubmissions.Where(cs => cs.ClassworkId == classwork.Id).ToList();
                            foreach (var submission in cwSubmissions)
                            {
                                var submissionFiles = db.SubmissionFiles.Where(sf => sf.SubmissionId == submission.Id).ToList();
                                foreach (var file in submissionFiles)
                                {
                                    var filePath = Server.MapPath(file.FilePath);
                                    if (System.IO.File.Exists(filePath))
                                    {
                                        try
                                        {
                                            System.IO.File.Delete(filePath);
                                        }
                                        catch (Exception ex)
                                        {
                                            System.Diagnostics.Debug.WriteLine($"Error deleting file {filePath}: {ex.Message}");
                                        }
                                    }
                                }
                                db.SubmissionFiles.RemoveRange(submissionFiles);
                            }
                            db.ClassworkSubmissions.RemoveRange(cwSubmissions);
                        }
                        db.Classworks.RemoveRange(classworks);

                        // Delete announcements and their comments
                        var announcements = db.Announcements.Where(a => a.TeacherCourseSectionId == tcsId).ToList();
                        foreach (var announcement in announcements)
                        {
                            var announcementComments = db.AnnouncementComments.Where(ac => ac.AnnouncementId == announcement.Id).ToList();
                            db.AnnouncementComments.RemoveRange(announcementComments);
                        }
                        db.Announcements.RemoveRange(announcements);

                        // Delete attendance records
                        var attendanceRecords = db.AttendanceRecords.Where(ar => ar.TeacherCourseSectionId == tcsId).ToList();
                        db.AttendanceRecords.RemoveRange(attendanceRecords);
                    }

                    // Remove teacher course sections
                    var teacherCourseSections = db.TeacherCourseSections.Where(tcs => tcs.TeacherId == id).ToList();
                    db.TeacherCourseSections.RemoveRange(teacherCourseSections);
                }

                // Save changes for related records deletion
                db.SaveChanges();

                // Now delete the user
                db.Users.Remove(user);
                db.SaveChanges();

                // Log the deletion
                LogAction(
                    category: "User Actions",
                    message: $"Deleted {userRole} account: {userFullName}",
                    userName: Session["FullName"]?.ToString(),
                    role: Session["Role"]?.ToString()
                );

                return Json(new { success = true, message = $"{userRole} deleted successfully!" });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DeleteUser Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                return Json(new { success = false, message = "Error deleting user: " + ex.Message });
            }
        }

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

        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                    builder.Append(b.ToString("x2"));
                return builder.ToString();
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

        private void AutoAssignCoursesToStudent(int studentId, int programId, int sectionId, int yearLevel, int semester)
        {

                // Validate inputs
                if (studentId <= 0 || programId <= 0 || sectionId <= 0 || yearLevel <= 0 || semester <= 0)
                {
                    return;
                }

                var student = db.Users.Find(studentId);
                if (student == null)
                {
                    System.Diagnostics.Debug.WriteLine($"ERROR: Student with ID {studentId} not found");
                    return;
                }

                var section = db.Sections.Find(sectionId);
                var curriculumCourses = db.CurriculumCourses
                    .Where(cc => cc.ProgramId == programId
                                 && cc.YearLevel == yearLevel
                                 && cc.Semester == semester)
                    .ToList();

                int enrolledCount = 0;
                int skippedCount = 0;

                foreach (var cc in curriculumCourses)
                {
                    var existingEnrollment = db.StudentCourses
                        .FirstOrDefault(sc => sc.StudentId == studentId
                                           && sc.CourseId == cc.CourseId
                                           && sc.SectionId == sectionId);

                    if (existingEnrollment == null)
                    {
                        var studentCourse = new StudentCourse
                        {
                            StudentId = studentId,
                            CourseId = cc.CourseId,
                            SectionId = sectionId,
                            DateEnrolled = DateTime.Now
                        };
                        
                        db.StudentCourses.Add(studentCourse);
                        enrolledCount++;
                        
                    }
                    else
                    {
                        skippedCount++;
                    }
                }

                db.SaveChanges();
        }

        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Login", "Home");
        }

    }
}
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
using System.Data.Entity;

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

        // GET: Account/ForgotPassword
        [HttpGet]
        public ActionResult ForgotPassword()
        {
            return View();
        }

        // POST: Account/ForgotPassword - Handle email submission
        [HttpPost]
        public ActionResult ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ViewBag.Error = "Please enter your email address.";
                ViewBag.Step = 1;
                return View();
            }

            var user = db.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
            {
                ViewBag.Error = "No account found with this email address.";
                ViewBag.Step = 1;
                return View();
            }

            try
            {
                // Generate 6-digit reset code
                var resetCode = GenerateResetCode();
                
                // Set expiry time (15 minutes from now)
                var expiryTime = DateTime.Now.AddMinutes(15);
                
                // Update user with reset token
                user.ResetToken = HashPassword(resetCode);
                user.ResetTokenExpiry = expiryTime;
                db.SaveChanges();

                // Send reset email
                var htmlBody = EmailHelper.GenerateEmailTemplate(
                    EmailType.PasswordReset,
                    new
                    {
                        Name = $"{user.FirstName} {user.LastName}",
                        ResetCode = resetCode,
                        ExpiryTime = expiryTime.ToString("MMMM dd, yyyy 'at' hh:mm tt")
                    }
                );

                bool emailSent = EmailHelper.SendEmail(
                    toEmail: email,
                    subject: "Password Reset - G2 Academy LMS",
                    htmlBody: htmlBody
                );

                if (emailSent)
                {
                    ViewBag.Success = "A password reset code has been sent to your email address.";
                    ViewBag.Step = 2;
                    ViewBag.Email = email;
                }
                else
                {
                    ViewBag.Error = "Failed to send reset email. Please try again later.";
                    ViewBag.Step = 1;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SendResetCode Error: {ex.Message}");
                ViewBag.Error = "An error occurred. Please try again later.";
                ViewBag.Step = 1;
            }

            return View();
        }

        // POST: Account/VerifyCode - Handle code verification
        [HttpPost]
        public ActionResult VerifyCode(string email, string resetCode)
        {
            ViewBag.Email = email;

            if (string.IsNullOrEmpty(resetCode) || resetCode.Length != 6)
            {
                ViewBag.Error = "Please enter the 6-digit reset code.";
                ViewBag.Step = 2;
                return View("ForgotPassword");
            }

            var user = db.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
            {
                ViewBag.Error = "No account found with this email address.";
                ViewBag.Step = 1;
                return View("ForgotPassword");
            }

            // Check if reset token exists and is not expired
            if (string.IsNullOrEmpty(user.ResetToken) || user.ResetTokenExpiry == null || 
                user.ResetTokenExpiry < DateTime.Now)
            {
                ViewBag.Error = "Reset code has expired. Please request a new one.";
                ViewBag.Step = 1;
                return View("ForgotPassword");
            }

            // Verify reset code
            if (HashPassword(resetCode) != user.ResetToken)
            {
                ViewBag.Error = "Invalid reset code.";
                ViewBag.Step = 2;
                return View("ForgotPassword");
            }

            ViewBag.Success = "Reset code verified successfully.";
            ViewBag.Step = 3;
            ViewBag.ResetCode = resetCode;
            return View("ForgotPassword");
        }

        // POST: Account/ResetPasswordFinal - Handle final password reset
        [HttpPost]
        public ActionResult ResetPasswordFinal(string email, string resetCode, string newPassword, string confirmPassword)
        {
            ViewBag.Email = email;
            ViewBag.ResetCode = resetCode;

            if (string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
            {
                ViewBag.Error = "Please fill in both password fields.";
                ViewBag.Step = 3;
                return View("ForgotPassword");
            }

            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "New passwords do not match.";
                ViewBag.Step = 3;
                return View("ForgotPassword");
            }

            if (newPassword.Length < 6)
            {
                ViewBag.Error = "Password must be at least 6 characters long.";
                ViewBag.Step = 3;
                return View("ForgotPassword");
            }

            var user = db.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
            {
                ViewBag.Error = "No account found with this email address.";
                ViewBag.Step = 1;
                return View("ForgotPassword");
            }

            // Check if reset token exists and is not expired
            if (string.IsNullOrEmpty(user.ResetToken) || user.ResetTokenExpiry == null || 
                user.ResetTokenExpiry < DateTime.Now)
            {
                ViewBag.Error = "Reset code has expired. Please request a new one.";
                ViewBag.Step = 1;
                return View("ForgotPassword");
            }

            // Verify reset code
            if (HashPassword(resetCode) != user.ResetToken)
            {
                ViewBag.Error = "Invalid reset code.";
                ViewBag.Step = 2;
                return View("ForgotPassword");
            }

            try
            {
                // Update password and clear reset token
                user.Password = HashPassword(newPassword);
                user.ResetToken = null;
                user.ResetTokenExpiry = null;
                db.SaveChanges();

                ViewBag.Step = 4;
                return View("ForgotPassword");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ResetPassword Error: {ex.Message}");
                ViewBag.Error = "An error occurred. Please try again later.";
                ViewBag.Step = 3;
                return View("ForgotPassword");
            }
        }

        // POST: Account/ResendCode - Handle resending reset code
        [HttpPost]
        public ActionResult ResendCode(string email)
        {
            ViewBag.Email = email;

            var user = db.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
            {
                ViewBag.Error = "No account found with this email address.";
                ViewBag.Step = 1;
                return View("ForgotPassword");
            }

            try
            {
                // Generate new reset code
                var resetCode = GenerateResetCode();
                var expiryTime = DateTime.Now.AddMinutes(15);
                
                user.ResetToken = HashPassword(resetCode);
                user.ResetTokenExpiry = expiryTime;
                db.SaveChanges();

                // Send reset email
                var htmlBody = EmailHelper.GenerateEmailTemplate(
                    EmailType.PasswordReset,
                    new
                    {
                        Name = $"{user.FirstName} {user.LastName}",
                        ResetCode = resetCode,
                        ExpiryTime = expiryTime.ToString("MMMM dd, yyyy 'at' hh:mm tt")
                    }
                );

                bool emailSent = EmailHelper.SendEmail(
                    toEmail: email,
                    subject: "Password Reset - G2 Academy LMS",
                    htmlBody: htmlBody
                );

                if (emailSent)
                {
                    ViewBag.Success = "Reset code sent again!";
                }
                else
                {
                    ViewBag.Error = "Failed to resend code. Please try again.";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ResendCode Error: {ex.Message}");
                ViewBag.Error = "An error occurred. Please try again later.";
            }

            ViewBag.Step = 2;
            return View("ForgotPassword");
        }

        // POST: Account/UploadAccounts - Enhanced with Program/Section validation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UploadAccounts(HttpPostedFileBase file, string role)
        {
            if (file == null || file.ContentLength == 0)
            {
                TempData["AlertMessage"] = "Please select an Excel file.";
                TempData["AlertType"] = "warning";
                return RedirectToAction("ManageUsers", "Admin");
            }

            if (string.IsNullOrEmpty(role))
            {
                TempData["AlertMessage"] = "Please select a valid role.";
                TempData["AlertType"] = "warning";
                return RedirectToAction("ManageUsers", "Admin");
            }

            try
            {
                using (var stream = new MemoryStream())
                {
                    file.InputStream.CopyTo(stream);
                    using (var workbook = new XLWorkbook(stream))
                    {
                        var worksheet = workbook.Worksheet(1);
                        var rangeUsed = worksheet.RangeUsed();
                        
                        if (rangeUsed == null)
                        {
                            TempData["AlertMessage"] = "Excel file appears to be empty or has no data rows.";
                            TempData["AlertType"] = "warning";
                            return RedirectToAction("ManageUsers", "Admin");
                        }

                        var rows = rangeUsed.RowsUsed().Skip(1).ToList();

                        if (!rows.Any())
                        {
                            TempData["AlertMessage"] = "Excel file appears to be empty or has no data rows.";
                            TempData["AlertType"] = "warning";
                            return RedirectToAction("ManageUsers", "Admin");
                        }

                        int successCount = 0;
                        int errorCount = 0;
                        int totalRows = rows.Count;
                        var errorDetails = new List<string>();
                        var successDetails = new List<string>();
                        var emailSentCount = 0;
                        var emailFailedCount = 0;

                        // Pre-load data for validation to improve performance
                        var allPrograms = db.Programs.Include(p => p.Department).ToList();
                        var allSections = db.Sections.Include(s => s.Program).ToList();

                        foreach (var rangeRow in rows)
                        {
                            int rowNumber = rangeRow.RowNumber();
                            string password = string.Empty; // Store password for email
                            
                            try
                            {
                                // Basic fields (same for both Teacher and Student)
                                string userId = GetCellValue(rangeRow, 1);
                                string lastName = GetCellValue(rangeRow, 2);
                                string firstName = GetCellValue(rangeRow, 3);
                                string email = GetCellValue(rangeRow, 4);

                                // Validate basic required fields
                                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(firstName) ||
                                    string.IsNullOrEmpty(lastName) || string.IsNullOrEmpty(email))
                                {
                                    errorCount++;
                                    errorDetails.Add($"Row {rowNumber}: Missing required fields (ID, Name, or Email)");
                                    continue;
                                }

                                if (!IsValidEmail(email))
                                {
                                    errorCount++;
                                    errorDetails.Add($"Row {rowNumber}: Invalid email format - {email}");
                                    continue;
                                }

                                if (db.Users.Any(u => u.Email == email))
                                {
                                    errorCount++;
                                    errorDetails.Add($"Row {rowNumber}: Email already exists - {email}");
                                    continue;
                                }

                                if (db.Users.Any(u => u.UserID == userId))
                                {
                                    errorCount++;
                                    errorDetails.Add($"Row {rowNumber}: User ID already exists - {userId}");
                                    continue;
                                }

                                // Student-specific validation
                                if (role == "Student")
                                {
                                    // Get student-specific fields
                                    string programCode = GetCellValue(rangeRow, 5); // Column 5: Program Code
                                    string yearLevelStr = GetCellValue(rangeRow, 6); // Column 6: Year Level
                                    string sectionName = GetCellValue(rangeRow, 7); // Column 7: Section Name
                                    string semesterStr = GetCellValue(rangeRow, 8); // Column 8: Semester

                                    // Validate student-specific required fields
                                    if (string.IsNullOrEmpty(programCode) || string.IsNullOrEmpty(yearLevelStr) || 
                                        string.IsNullOrEmpty(sectionName) || string.IsNullOrEmpty(semesterStr))
                                    {
                                        errorCount++;
                                        errorDetails.Add($"Row {rowNumber}: Students require Program Code, Year Level, Section Name, and Semester");
                                        continue;
                                    }

                                    // Parse and validate year level
                                    if (!int.TryParse(yearLevelStr, out int yearLevel) || yearLevel < 1 || yearLevel > 4)
                                    {
                                        errorCount++;
                                        errorDetails.Add($"Row {rowNumber}: Invalid Year Level '{yearLevelStr}'. Must be 1, 2, 3, or 4");
                                        continue;
                                    }

                                    // Parse and validate semester
                                    if (!int.TryParse(semesterStr, out int semester) || semester < 1 || semester > 2)
                                    {
                                        errorCount++;
                                        errorDetails.Add($"Row {rowNumber}: Invalid Semester '{semesterStr}'. Must be 1 or 2");
                                        continue;
                                    }

                                    // Find program by code
                                    var program = allPrograms.FirstOrDefault(p => p.ProgramCode.Equals(programCode, StringComparison.OrdinalIgnoreCase));
                                    if (program == null)
                                    {
                                        errorCount++;
                                        errorDetails.Add($"Row {rowNumber}: Program with code '{programCode}' not found");
                                        continue;
                                    }

                                    // Find section
                                    var section = allSections.FirstOrDefault(s => 
                                        s.ProgramId == program.Id && 
                                        s.YearLevel == yearLevel && 
                                        s.SectionName.Equals(sectionName, StringComparison.OrdinalIgnoreCase));

                                    if (section == null)
                                    {
                                        errorCount++;
                                        errorDetails.Add($"Row {rowNumber}: Section '{sectionName}' not found for {programCode} Year {yearLevel}");
                                        continue;
                                    }

                                    // Check if courses exist for this program/year/semester
                                    var curriculumCourses = db.CurriculumCourses
                                        .Where(cc => cc.ProgramId == program.Id
                                                     && cc.YearLevel == yearLevel
                                                     && cc.Semester == semester)
                                        .ToList();

                                    if (curriculumCourses == null || !curriculumCourses.Any())
                                    {
                                        errorCount++;
                                        errorDetails.Add($"Row {rowNumber}: No courses found for {programCode} Year {yearLevel} Semester {semester}. Please add courses to the curriculum first.");
                                        continue;
                                    }

                                    // Create student user with enrollment
                                    password = GeneratePassword();
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
                                    db.SaveChanges(); // Save to get user ID

                                    // Auto-enroll student in courses for their program/year/semester
                                    AutoAssignCoursesToStudent(newUser.Id, program.Id, section.Id, yearLevel, semester);

                                    // Send welcome email with credentials
                                    try
                                    {
                                        var htmlBody = EmailHelper.GenerateEmailTemplate(
                                            EmailType.AccountCreated,
                                            new
                                            {
                                                Name = $"{firstName} {lastName}",
                                                Email = email,
                                                Password = password
                                            }
                                        );

                                        bool emailSent = EmailHelper.SendEmail(
                                            toEmail: email,
                                            subject: "Your G2 Academy LMS Account - Welcome Student!",
                                            htmlBody: htmlBody
                                        );

                                        if (emailSent)
                                        {
                                            emailSentCount++;
                                        }
                                        else
                                        {
                                            emailFailedCount++;
                                            System.Diagnostics.Debug.WriteLine($"Failed to send email to {email}");
                                        }
                                    }
                                    catch (Exception emailEx)
                                    {
                                        emailFailedCount++;
                                        System.Diagnostics.Debug.WriteLine($"Email error for {email}: {emailEx.Message}");
                                    }

                                    successCount++;
                                    successDetails.Add($"Row {rowNumber}: {firstName} {lastName} ({userId}) - {programCode} Year {yearLevel} {sectionName}");
                                }
                                else
                                {
                                    // Teacher or other roles - no additional validation needed
                                    password = GeneratePassword();
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
                                    db.SaveChanges(); // Save immediately to ensure user is created before email

                                    // Send welcome email with credentials to teacher
                                    try
                                    {
                                        var htmlBody = EmailHelper.GenerateEmailTemplate(
                                            EmailType.AccountCreated,
                                            new
                                            {
                                                Name = $"{firstName} {lastName}",
                                                Email = email,
                                                Password = password
                                            }
                                        );

                                        bool emailSent = EmailHelper.SendEmail(
                                            toEmail: email,
                                            subject: $"Your G2 Academy LMS Account - Welcome {role}!",
                                            htmlBody: htmlBody
                                        );

                                        if (emailSent)
                                        {
                                            emailSentCount++;
                                        }
                                        else
                                        {
                                            emailFailedCount++;
                                            System.Diagnostics.Debug.WriteLine($"Failed to send email to {email}");
                                        }
                                    }
                                    catch (Exception emailEx)
                                    {
                                        emailFailedCount++;
                                        System.Diagnostics.Debug.WriteLine($"Email error for {email}: {emailEx.Message}");
                                    }

                                    successCount++;
                                    successDetails.Add($"Row {rowNumber}: {firstName} {lastName} ({userId}) - {email}");
                                }
                            }
                            catch (Exception rowEx)
                            {
                                errorCount++;
                                errorDetails.Add($"Row {rowNumber}: Processing error - {rowEx.Message}");
                                System.Diagnostics.Debug.WriteLine($"Row {rowNumber} Error: {rowEx.Message}");
                                System.Diagnostics.Debug.WriteLine($"Stack Trace: {rowEx.StackTrace}");
                            }
                        }

                        // Save remaining changes (if any students were enrolled after last save)
                        db.SaveChanges();

                        // Store upload summary in Session instead of TempData for better persistence
                        var uploadSummary = new
                        {
                            Role = role,
                            TotalRows = totalRows,
                            SuccessCount = successCount,
                            ErrorCount = errorCount,
                            SuccessDetails = successDetails,
                            ErrorDetails = errorDetails,
                            FileName = file.FileName,
                            EmailSentCount = emailSentCount,
                            EmailFailedCount = emailFailedCount
                        };
                        
                        // Store in Session for persistence
                        Session["UploadSummary"] = uploadSummary;
                        
                        System.Diagnostics.Debug.WriteLine($"Upload completed: Success={successCount}, Errors={errorCount}");
                        System.Diagnostics.Debug.WriteLine($"UploadSummary stored in Session");
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["AlertMessage"] = $"Error uploading accounts: {ex.Message}";
                TempData["AlertType"] = "danger";
                System.Diagnostics.Debug.WriteLine($"Upload Error: {ex.Message}");
            }

            return RedirectToAction("ManageUsers", "Admin");
        }

        // Helper method to safely get cell values - Updated to accept IXLRangeRow
        private string GetCellValue(ClosedXML.Excel.IXLRangeRow row, int columnNumber)
        {
            try
            {
                var cell = row.Cell(columnNumber);
                if (cell == null) return string.Empty;
                
                var cellValue = cell.GetString();
                return cellValue?.Trim() ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
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

                    // Validate that section exists
                    var section = db.Sections
                        .Include(s => s.Program)
                        .FirstOrDefault(s => s.Id == sectionId.Value);
                    
                    if (section == null)
                    {
                        return Json(new { success = false, message = "Selected section not found." });
                    }

                    // Check if courses exist for this program/year/semester
                    int currentSemester = semester ?? (DateTime.Now.Month >= 6 && DateTime.Now.Month <= 10 ? 1 : 2);
                    var curriculumCourses = db.CurriculumCourses
                        .Where(cc => cc.ProgramId == programId.Value
                                     && cc.YearLevel == yearLevel.Value
                                     && cc.Semester == currentSemester)
                        .ToList();

                    if (curriculumCourses == null || !curriculumCourses.Any())
                    {
                        var program = db.Programs.Find(programId.Value);
                        string programCode = program?.ProgramCode ?? "the program";
                        return Json(new { success = false, message = $"No courses found for {programCode} Year {yearLevel.Value} Semester {currentSemester}. Please add courses to the curriculum first." });
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

                db.SaveChanges();

                db.Users.Remove(user);
                db.SaveChanges();

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

        // Reset Code Generator (6 digits)
        private string GenerateResetCode()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
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

        // GET: Account/DownloadExcelTemplate
        public ActionResult DownloadExcelTemplate(string role = "Student")
        {
            try
            {
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Accounts");

                    if (role == "Student")
                    {
                        // Student template headers
                        worksheet.Cell(1, 1).Value = "Student ID";
                        worksheet.Cell(1, 2).Value = "Last Name";
                        worksheet.Cell(1, 3).Value = "First Name";
                        worksheet.Cell(1, 4).Value = "Email";
                        worksheet.Cell(1, 5).Value = "Program Code";
                        worksheet.Cell(1, 6).Value = "Year Level";
                        worksheet.Cell(1, 7).Value = "Section Name";
                        worksheet.Cell(1, 8).Value = "Semester";

                        // Add sample data row
                        worksheet.Cell(2, 1).Value = "STU-2024-001";
                        worksheet.Cell(2, 2).Value = "Doe";
                        worksheet.Cell(2, 3).Value = "John";
                        worksheet.Cell(2, 4).Value = "john.doe@example.com";
                        worksheet.Cell(2, 5).Value = "BSIT"; // Example program code
                        worksheet.Cell(2, 6).Value = "1";
                        worksheet.Cell(2, 7).Value = "A";
                        worksheet.Cell(2, 8).Value = "1";

                        // Add another sample row
                        worksheet.Cell(3, 1).Value = "STU-2024-002";
                        worksheet.Cell(3, 2).Value = "Smith";
                        worksheet.Cell(3, 3).Value = "Jane";
                        worksheet.Cell(3, 4).Value = "jane.smith@example.com";
                        worksheet.Cell(3, 5).Value = "BSIT";
                        worksheet.Cell(3, 6).Value = "1";
                        worksheet.Cell(3, 7).Value = "A";
                        worksheet.Cell(3, 8).Value = "1";
                    }
                    else
                    {
                        // Teacher template headers
                        worksheet.Cell(1, 1).Value = "Teacher ID";
                        worksheet.Cell(1, 2).Value = "Last Name";
                        worksheet.Cell(1, 3).Value = "First Name";
                        worksheet.Cell(1, 4).Value = "Email";

                        // Add sample data row
                        worksheet.Cell(2, 1).Value = "TCH-2024-001";
                        worksheet.Cell(2, 2).Value = "Johnson";
                        worksheet.Cell(2, 3).Value = "Robert";
                        worksheet.Cell(2, 4).Value = "robert.johnson@example.com";

                        // Add another sample row
                        worksheet.Cell(3, 1).Value = "TCH-2024-002";
                        worksheet.Cell(3, 2).Value = "Williams";
                        worksheet.Cell(3, 3).Value = "Mary";
                        worksheet.Cell(3, 4).Value = "mary.williams@example.com";
                    }

                    // Style the header row
                    var headerRange = worksheet.Range(1, 1, 1, role == "Student" ? 8 : 4);
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
                    headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    // Auto-fit columns
                    worksheet.Columns().AdjustToContents();

                    // Prepare response
                    Response.Clear();
                    Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    string fileName = $"Account_Template_{role}_{DateTime.Now:yyyyMMdd}.xlsx";
                    Response.AddHeader("content-disposition", $"attachment;filename=\"{fileName}\"");

                    // Write to response
                    using (var memoryStream = new MemoryStream())
                    {
                        workbook.SaveAs(memoryStream);
                        memoryStream.WriteTo(Response.OutputStream);
                    }

                    Response.End();
                    return null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DownloadExcelTemplate Error: {ex.Message}");
                TempData["AlertMessage"] = $"Error generating template: {ex.Message}";
                TempData["AlertType"] = "danger";
                return RedirectToAction("ManageUsers", "Admin");
            }
        }

        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Login", "Home");
        }

    }
}
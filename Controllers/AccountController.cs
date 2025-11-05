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
            //Find User by Email
            var user = db.Users.FirstOrDefault(u => u.Email == email);

            //Check if user exists
            if (user == null)
            {
                ViewBag.Error = "Invalid email or password.";
                return View();
            }

            //Verify Password
            if (HashPassword(password) == user.Password)
            {
                Session["Id"] = user.Id;
                Session["Role"] = user.Role;
                Session["FirstName"] = user.FirstName;
                Session["LastName"] = user.LastName;


                user.LastLogin = DateTime.Now;
                db.SaveChanges();

                LogAction(
                    category: "Authentication",
                    message: $"{user.FirstName} {user.LastName} logged in.",
                    userName: $"{user.FirstName} {user.LastName}",
                    role: user.Role
                );

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

        // POST: Account/EditAccount
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

        // POST: Account/CreateAccount
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
                Password = HashPassword(generatedPassword),
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


            return Json(new
            {
                success = true,
                message = $"Account created successfully!",
                userData = new
                {
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

        // POST: Account/DeleteUser
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


    }
}
using LMS.Models;
using LMS.Helpers;
using ClosedXML.Excel;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using OfficeOpenXml;



namespace LMS.Controllers
{
    public class AdminController : Controller
    {
        private readonly LMSContext db = new LMSContext();


        // GET: Admin(View)
        public ActionResult Index()
        {
            return View();
        }

        // GET: Admin/Calendar
        public ActionResult Calendar()
        {
            return View();
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

        // POST: Admin/SaveEvent
        [HttpPost]
        public JsonResult SaveEvent(Event e)
        {
            try
            {
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
                    }
                    else
                    {
                        return Json(new { success = false, message = "Event not found" });
                    }
                }
                else
                {
                    db.Events.Add(e);
                }

                db.SaveChanges();
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
            string userRole = user.Role;
            
            db.Users.Remove(user);
            db.SaveChanges();
            
            // Return in JSON for AJAX requests
            return Json(new { 
                success = true, 
                message = $"{userRole} deleted successfully!" 
            });
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
                if (string.IsNullOrEmpty(role) || string.IsNullOrEmpty(firstName) ||
                    string.IsNullOrEmpty(lastName) || string.IsNullOrEmpty(email) ||
                    string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(phone) || string.IsNullOrEmpty(department))
                {
                    return Json(new { success = false, message = "All fields are required!" });
                }

                var existingUser = db.Users.FirstOrDefault(u => u.Id == id);
                if (existingUser == null)
                {
                    return Json(new { success = false, message = "User not found!" });
                }

                // Check for email duplication (except the current user)
                if (db.Users.Any(u => u.Email == email && u.Id != id))
                {
                    return Json(new { success = false, message = "Email already exists!" });
                }

                // Check for duplicate Student/Teacher ID (except the current user)
                if (db.Users.Any(u => u.UserID == userId && u.Id != id))
                {
                    return Json(new { success = false, message = "Teacher/Student ID already exists!" });
                }

                // Update fields
                existingUser.FirstName = firstName;
                existingUser.LastName = lastName;
                existingUser.Email = email;
                existingUser.UserID = userId;
                existingUser.PhoneNumber = phone;
                existingUser.Department = department;
                existingUser.Role = role;

                db.SaveChanges();

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
                // Ensure license is set before using ExcelPackage (safety check)
                if (ExcelPackage.LicenseContext != OfficeOpenXml.LicenseContext.NonCommercial)
                {
                    ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                }

                using (var package = new ExcelPackage(file.InputStream))
                {
                    if (package.Workbook.Worksheets.Count == 0)
                    {
                        TempData["AlertMessage"] = "Excel file has no worksheets.";
                        TempData["AlertType"] = "warning";
                        return RedirectToAction("ManageUsers");
                    }

                    var worksheet = package.Workbook.Worksheets.First();
                    int rowCount = worksheet.Dimension?.Rows ?? 0;
                    
                    if (rowCount <= 1)
                    {
                        TempData["AlertMessage"] = "Excel file appears to be empty or has no data rows.";
                        TempData["AlertType"] = "warning";
                        return RedirectToAction("ManageUsers");
                    }

                    int successCount = 0;
                    int errorCount = 0;
                    int totalRows = rowCount - 1; // Subtract header row
                    var errorDetails = new List<string>();

                    // Process each row with detailed tracking
                    for (int row = 2; row <= rowCount; row++)
                    {
                        try
                        {
                            var userId = worksheet.Cells[row, 1].Text.Trim();
                            var lastName = worksheet.Cells[row, 2].Text.Trim();
                            var firstName = worksheet.Cells[row, 3].Text.Trim();
                            var email = worksheet.Cells[row, 4].Text.Trim();
                            var phone = worksheet.Cells[row, 5].Text.Trim();
                            var department = worksheet.Cells[row, 6].Text.Trim();

                            // Validate required fields
                            if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName) || 
                                string.IsNullOrEmpty(email) || string.IsNullOrEmpty(userId))
                            {
                                errorCount++;
                                errorDetails.Add($"Row {row}: Missing required fields (ID, Name, or Email)");
                                continue;
                            }

                            // Validate email format
                            if (!IsValidEmail(email))
                            {
                                errorCount++;
                                errorDetails.Add($"Row {row}: Invalid email format - {email}");
                                continue;
                            }

                            // Check for duplicate email
                            if (db.Users.Any(u => u.Email == email))
                            {
                                errorCount++;
                                errorDetails.Add($"Row {row}: Email already exists - {email}");
                                continue;
                            }

                            // Check for duplicate UserID
                            if (db.Users.Any(u => u.UserID == userId))
                            {
                                errorCount++;
                                errorDetails.Add($"Row {row}: User ID already exists - {userId}");
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

                            // Send welcome email for each user
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
                                // Continue processing even if email fails
                            }

                            // Log progress for debugging
                            System.Diagnostics.Debug.WriteLine($"Processed {successCount + errorCount}/{totalRows} records");
                        }
                        catch (Exception rowEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error processing row {row}: {rowEx.Message}");
                            errorCount++;
                            errorDetails.Add($"Row {row}: Processing error - {rowEx.Message}");
                        }
                    }

                    // Save all changes at once for better performance
                    db.SaveChanges();

                    // Prepare detailed results
                    if (successCount > 0)
                    {
                        string successMsg = $"Successfully uploaded {successCount} of {totalRows} accounts!";
                        
                        if (successCount == totalRows)
                        {
                            successMsg += " All records processed successfully.";
                        }
                        
                        TempData["AlertMessage"] = successMsg;
                        TempData["AlertType"] = "success";

                        if (errorCount > 0)
                        {
                            string errorMsg = $"{errorCount} records had errors and were skipped.";
                            if (errorDetails.Count > 0 && errorDetails.Count <= 5)
                            {
                                errorMsg += " Issues: " + string.Join("; ", errorDetails.Take(3));
                                if (errorDetails.Count > 3)
                                {
                                    errorMsg += $" and {errorDetails.Count - 3} more...";
                                }
                            }
                            
                            TempData["AlertMessage2"] = errorMsg;
                            TempData["AlertType2"] = "warning";
                        }
                    }
                    else
                    {
                        string errorMsg = "No accounts were uploaded.";
                        if (errorDetails.Any())
                        {
                            errorMsg += " Issues found: " + string.Join("; ", errorDetails.Take(3));
                        }
                        else
                        {
                            errorMsg += " Please check your Excel file format.";
                        }
                        
                        TempData["AlertMessage"] = errorMsg;
                        TempData["AlertType"] = "danger";
                    }

                    // Log final statistics
                    System.Diagnostics.Debug.WriteLine($"Upload completed: {successCount} successful, {errorCount} errors out of {totalRows} total rows");
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
    }
}

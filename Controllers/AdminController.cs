using LMS.Models;
using LMS.Helpers;
using OfficeOpenXml;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Configuration;



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
                using (var package = new ExcelPackage(file.InputStream))
                {
                    var worksheet = package.Workbook.Worksheets.First();
                    int rowCount = worksheet.Dimension.Rows;
                    int successCount = 0;
                    int errorCount = 0;

                    for (int row = 2; row <= rowCount; row++)
                    {
                        try
                        {
                            var lastName = worksheet.Cells[row, 3].Text.Trim();
                            var firstName = worksheet.Cells[row, 4].Text.Trim();
                            var email = worksheet.Cells[row, 5].Text.Trim();

                            if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName) || string.IsNullOrEmpty(email))
                            {
                                errorCount++;
                                continue;
                            }

                            if (db.Users.Any(u => u.Email == email))
                            {
                                errorCount++;
                                continue;
                            }

                            string password = GeneratePassword();

                            db.Users.Add(new User
                            {
                                LastName = lastName,
                                FirstName = firstName,
                                Email = email,
                                Password = password,
                                Role = role,
                                DateCreated = DateTime.Now
                            });

                            successCount++;
                        }
                        catch
                        {
                            errorCount++;
                        }
                    }

                    db.SaveChanges();

                    if (successCount > 0)
                    {
                        TempData["AlertMessage"] = $"Successfully uploaded {successCount} accounts!";
                        TempData["AlertType"] = "success";
                        
                        if (errorCount > 0)
                        {
                            // Add a second alert for the warnings
                            TempData["AlertMessage2"] = $"{errorCount} records had errors and were skipped.";
                            TempData["AlertType2"] = "warning";
                        }
                    }
                    else
                    {
                        TempData["AlertMessage"] = "No accounts were uploaded. Please check your Excel file format.";
                        TempData["AlertType"] = "danger";  // Using danger instead of error for Bootstrap
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["AlertMessage"] = $"Error uploading accounts: {ex.Message}";
                TempData["AlertType"] = "danger";
            }

            return RedirectToAction("ManageUsers");
        }

        // Password Generator
        private string GeneratePassword(int length = 8)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789@#$!";
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

using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using LMS.Models;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using System.Web;

namespace LMS.Controllers
{
    public class ProfileController : Controller
    {
        private readonly LMSContext db = new LMSContext();

        // GET: Profile (for Teachers, IT, Admin)
        public ActionResult Index()
        {
            // Validate session
            if (Session["Id"] == null)
            {
                TempData["ErrorMessage"] = "Session expired. Please log in again.";
                return RedirectToAction("Login", "Home");
            }

            var userRole = Session["Role"]?.ToString();
            if (userRole == "Student")
            {
                // Students use their own profile page
                return RedirectToAction("Profile", "Student");
            }

            int userId = Convert.ToInt32(Session["Id"]);
            var user = db.Users
                .Include(u => u.Department)
                .FirstOrDefault(u => u.Id == userId);

            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Index", "Home");
            }

            // Get departments for dropdown
            var departments = db.Departments.OrderBy(d => d.DepartmentName).ToList();

            // Create profile view model with full access for non-students
            var profileViewModel = new ProfileUpdateViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                DateOfBirth = user.DateOfBirth,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                DepartmentId = user.DepartmentId,
                EmergencyContactName = user.EmergencyContactName,
                EmergencyContactPhone = user.EmergencyContactPhone,
                EmergencyContactRelationship = user.EmergencyContactRelationship,
                Role = user.Role,
                UserID = user.UserID,
                ProfilePicture = user.ProfilePicture,
                AvailableDepartments = departments
            };

            ViewBag.User = user;
            ViewBag.ProfileViewModel = profileViewModel;
            ViewBag.Departments = departments;

            return View(profileViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateProfile(ProfileUpdateViewModel model)
        {
            try
            {
                if (Session["Id"] == null)
                {
                    return Json(new { success = false, message = "Session expired. Please log in again." });
                }

                var userRole = Session["Role"]?.ToString();
                if (userRole == "Student")
                {
                    return Json(new { success = false, message = "Students cannot use this profile update method." });
                }

                int userId = Convert.ToInt32(Session["Id"]);
                var user = db.Users.Find(userId);

                if (user == null)
                {
                    return Json(new { success = false, message = "User not found." });
                }

                // Basic validation for all users
                if (string.IsNullOrWhiteSpace(model.FirstName) || string.IsNullOrWhiteSpace(model.LastName))
                {
                    return Json(new { success = false, message = "First name and last name are required." });
                }

                if (string.IsNullOrWhiteSpace(model.Email))
                {
                    return Json(new { success = false, message = "Email is required." });
                }

                // Check if email is already in use by another user
                var existingUser = db.Users.FirstOrDefault(u => u.Email == model.Email && u.Id != userId);
                if (existingUser != null)
                {
                    return Json(new { success = false, message = "This email is already in use by another user." });
                }

                // Update basic fields for all users
                user.FirstName = model.FirstName.Trim();
                user.LastName = model.LastName.Trim();
                user.Email = model.Email.Trim();

                // Admin users only get basic profile - no additional fields
                if (userRole == "Admin")
                {
                    // Admins can only update: FirstName, LastName, Email
                    // No contact info, emergency contacts, date of birth, or department
                }
                else
                {
                    // Teachers and IT can update additional profile fields
                    user.DateOfBirth = model.DateOfBirth;
                    user.PhoneNumber = model.PhoneNumber?.Trim();
                    user.Address = model.Address?.Trim();
                    user.EmergencyContactName = model.EmergencyContactName?.Trim();
                    user.EmergencyContactPhone = model.EmergencyContactPhone?.Trim();
                    user.EmergencyContactRelationship = model.EmergencyContactRelationship?.Trim();

                    // Only IT can change department assignments (not Teachers)
                    if (userRole == "IT")
                    {
                        user.DepartmentId = model.DepartmentId;
                    }
                }

                db.SaveChanges();

                // Update session variables
                Session["FirstName"] = user.FirstName;
                Session["LastName"] = user.LastName;

                return Json(new { success = true, message = "Profile updated successfully!" });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateProfile Error: {ex.Message}");
                return Json(new { success = false, message = "An error occurred while updating your profile." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UploadProfilePicture()
        {
            try
            {
                if (Session["Id"] == null)
                {
                    return Json(new { success = false, message = "Session expired. Please log in again." });
                }

                if (Request.Files.Count == 0 || Request.Files[0] == null || Request.Files[0].ContentLength == 0)
                {
                    return Json(new { success = false, message = "Please select a file to upload." });
                }

                var file = Request.Files[0];
                
                // Validate file type
                var allowedTypes = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var fileExtension = System.IO.Path.GetExtension(file.FileName).ToLower();
                
                if (!allowedTypes.Contains(fileExtension))
                {
                    return Json(new { success = false, message = "Only JPG, PNG, and GIF files are allowed." });
                }

                // Validate file size (5MB max)
                if (file.ContentLength > 5 * 1024 * 1024)
                {
                    return Json(new { success = false, message = "File size must be less than 5MB." });
                }

                int userId = Convert.ToInt32(Session["Id"]);
                var user = db.Users.Find(userId);

                if (user == null)
                {
                    return Json(new { success = false, message = "User not found." });
                }

                // Create uploads directory if it doesn't exist
                var uploadsDir = Server.MapPath("~/Uploads/ProfilePictures");
                if (!System.IO.Directory.Exists(uploadsDir))
                {
                    System.IO.Directory.CreateDirectory(uploadsDir);
                }

                // Delete old profile picture if exists
                if (!string.IsNullOrEmpty(user.ProfilePicture))
                {
                    var oldFilePath = Server.MapPath(user.ProfilePicture);
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        try
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error deleting old profile picture: {ex.Message}");
                        }
                    }
                }

                // Generate unique filename
                var fileName = $"{user.UserID}_{DateTime.Now:yyyyMMddHHmmss}{fileExtension}";
                var filePath = System.IO.Path.Combine(uploadsDir, fileName);
                
                // Save file
                file.SaveAs(filePath);

                // Update user profile picture path
                user.ProfilePicture = $"~/Uploads/ProfilePictures/{fileName}";
                db.SaveChanges();

                return Json(new { 
                    success = true, 
                    message = "Profile picture updated successfully!",
                    profilePictureUrl = Url.Content(user.ProfilePicture)
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UploadProfilePicture Error: {ex.Message}");
                return Json(new { success = false, message = "An error occurred while uploading the profile picture." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(PasswordChangeViewModel model)
        {
            try
            {
                if (Session["Id"] == null)
                {
                    return Json(new { success = false, message = "Session expired. Please log in again." });
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return Json(new { success = false, message = string.Join(", ", errors) });
                }

                int userId = Convert.ToInt32(Session["Id"]);
                var user = db.Users.Find(userId);

                if (user == null)
                {
                    return Json(new { success = false, message = "User not found." });
                }

                // Verify current password
                if (HashPassword(model.CurrentPassword) != user.Password)
                {
                    return Json(new { success = false, message = "Current password is incorrect." });
                }

                // Update password
                user.Password = HashPassword(model.NewPassword);
                db.SaveChanges();

                return Json(new { success = true, message = "Password changed successfully!" });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ChangePassword Error: {ex.Message}");
                return Json(new { success = false, message = "An error occurred while changing your password." });
            }
        }

        // Helper method for password hashing
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                var builder = new StringBuilder();
                foreach (byte b in bytes)
                    builder.Append(b.ToString("x2"));
                return builder.ToString();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
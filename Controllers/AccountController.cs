using Microsoft.AspNetCore.Mvc;

namespace G2AcademyLMS.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            // Basic authentication - integrate with Identity in production
            if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(password))
            {
                // Redirect to student dashboard after successful login
                return RedirectToAction("Dashboard", "Student");
            }

            // If login fails, return to login page with error
            ViewBag.Error = "Invalid credentials. Please try again.";
            return View();
        }

    }
}
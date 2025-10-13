using LMS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;

namespace LMS.Controllers
{
    public class HomeController : Controller
    {

        private readonly LMSContext db = new LMSContext();

        public ActionResult Index()
        {
            return View();
        }


        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Login(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Please enter your email and password.";
                return View();
            }

            // Administrator login bypass
            if (email == "admin@gmail.com" && password == "password")
            {
                Session["Role"] = "Admin";
                return RedirectToAction("Index", "Admin");
            }

            // Find User by Email
            var user = db.Users.FirstOrDefault(u => u.Email == email);

            // Check if user exists
            if (user == null)
            {
                ViewBag.Error = "Invalid email or password.";
                return View();
            }

            // Verify Password
            if (HashPassword(password) == user.Password)
            {
                Session["UserID"] = user.UserID;
                Session["Role"] = user.Role;
                Session["FullName"] = $"{user.FirstName} {user.LastName}";

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
                    default:
                        return RedirectToAction("Index", "Home");
                }
            }
            else
            {
                ViewBag.Error = "Invalid email or password.";
                return View();
            }
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
    }
}
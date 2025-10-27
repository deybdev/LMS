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

            //// Administrator login bypass
            //if (email == "admin@gmail.com" && password == "password")
            //{
            //    Session["Role"] = "Admin";
            //    return RedirectToAction("Index", "Admin");
            //}else if(email == "student@gmail.com" && password == "password")
            //{
            //    Session["Role"] = "Student";
            //    return RedirectToAction("Index", "Student");
            //}else if(email == "teacher@gmail.com" && password == "password")
            //{
            //    Session["Role"] = "Teacher";
            //    return RedirectToAction("Index", "Teacher");
            //}

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
                    default:
                        return RedirectToAction("Index", "Home");
                }
            }
            else
            {
                ViewBag.Error = "Invalid email or password.";
            }

            return View();
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
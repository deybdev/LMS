using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace LMS.Controllers
{
    public class HomeController : Controller
    {
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
        public ActionResult Login(string username, string password)
        {
            if (username == "admin" && password == "password")
            {
                Session["Role"] = "Admin";
                return RedirectToAction("Index", "Admin");
            }
            else if (username == "student" && password == "password")
            {
                Session["Role"] = "Student";
                return RedirectToAction("Index", "Student");
            }
            else if (username == "teacher" && password == "password")
            {
                Session["Role"] = "Teacher";
                return RedirectToAction("Index", "Teacher");
            }
                ViewBag.Error = "Invalid username or password";
            return View();
        }
    }
}
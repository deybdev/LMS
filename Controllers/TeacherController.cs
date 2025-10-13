using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace LMS.Controllers
{
    public class TeacherController : Controller
    {
        // GET: Teacher
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult Course()
        {
            return View();
        }
        public ActionResult Notification()
        {
            return View();
        }
        public new ActionResult Profile()
        {
            return View();
        }
    }
}
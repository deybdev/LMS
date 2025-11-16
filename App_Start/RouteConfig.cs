using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace LMS
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            // FIRST: Exact match for course list
            routes.MapRoute(
                name: "TeacherCourseList",
                url: "Teacher/Course",
                defaults: new { controller = "Teacher", action = "Course" }
            );

            routes.MapRoute(
                name: "TeacherCourseAction",
                url: "Teacher/Course/{action}/{id}",
                defaults: new { controller = "Teacher", id = UrlParameter.Optional }
            );


            // Default route (must be LAST)
            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}

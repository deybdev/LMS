using System;
using System.Collections.Generic;

namespace LMS.Models
{
    public class AdminDashboardViewModel
    {
        public List<AuditLog> AuditLogs { get; set; }
        public List<Event> UpcomingEvents { get; set; }
        public int TotalStudents { get; set; }
        public int TotalTeachers { get; set; }
        public int TotalCourses { get; set; }
    }
}

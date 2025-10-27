using System;

namespace LMS.Models
{
    public class AuditLog
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string Category { get; set; }
        public string Message { get; set; }
        public string UserName { get; set; }
        public string Role { get; set; }
    }

}
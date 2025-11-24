using System.Collections.Generic;

namespace LMS.Models
{
    public class AttendanceStudentRecord
    {
        public int StudentId { get; set; }
        public string Status { get; set; } // "Present", "Absent", "Late"
        public int? LateMinutes { get; set; } // How many minutes late
    }

    public class SaveAttendanceRequest
    {
        public int TeacherCourseSectionId { get; set; }
        public string AttendanceDate { get; set; }
        public List<AttendanceStudentRecord> Records { get; set; } = new List<AttendanceStudentRecord>();
    }
}


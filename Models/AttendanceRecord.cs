using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LMS.Models
{
    public class AttendanceRecord
    {
        public int Id { get; set; }

        [Required]
        public int TeacherCourseSectionId { get; set; }

        [Required]
        public int StudentId { get; set; }

        [Column(TypeName = "date")]
        public DateTime AttendanceDate { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; }

        public DateTime MarkedAt { get; set; } = DateTime.Now;

        public int? LateMinutes { get; set; }

        public virtual TeacherCourseSection TeacherCourseSection { get; set; }
        public virtual User Student { get; set; }
    }
}







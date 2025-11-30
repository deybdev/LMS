using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LMS.Models
{
    public class GradingConfiguration
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TeacherCourseSectionId { get; set; }

        // JSON string to store classwork type percentages
        // Format: {"Assignment": 30, "Quiz": 10, "Exam": 60}
        [Required]
        public string TypePercentagesJson { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        // Navigation property
        [ForeignKey("TeacherCourseSectionId")]
        public virtual TeacherCourseSection TeacherCourseSection { get; set; }
    }
}


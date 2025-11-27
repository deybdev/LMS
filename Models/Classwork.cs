using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LMS.Models
{
    public class Classwork
    {
        public int Id { get; set; }

        [Required]
        public int TeacherCourseSectionId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [Required]
        [StringLength(50)]
        public string ClassworkType { get; set; } // Assignment, Quiz, Activity, Project, Exam

        public string Description { get; set; }

        public DateTime? Deadline { get; set; }

        [Required]
        public int Points { get; set; }

        public DateTime DateCreated { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;

        // Scheduling properties
        public bool IsScheduled { get; set; } = false;
        public DateTime? ScheduledPublishDate { get; set; }

        // JSON field to store questions (for Quiz, Exam, Activity)
        public string QuestionsJson { get; set; }

        // Navigation properties
        [ForeignKey("TeacherCourseSectionId")]
        public virtual TeacherCourseSection TeacherCourseSection { get; set; }

        public virtual ICollection<ClassworkFile> ClassworkFiles { get; set; } = new List<ClassworkFile>();
        public virtual ICollection<ClassworkSubmission> ClassworkSubmissions { get; set; } = new List<ClassworkSubmission>();
    }

    public class ClassworkFile
    {
        public int Id { get; set; }

        [Required]
        public int ClassworkId { get; set; }

        [Required]
        [StringLength(255)]
        public string FileName { get; set; }

        [Required]
        [StringLength(500)]
        public string FilePath { get; set; }

        public decimal SizeInMB { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.Now;

        // Navigation property
        [ForeignKey("ClassworkId")]
        public virtual Classwork Classwork { get; set; }
    }

    public class ClassworkSubmission
    {
        public int Id { get; set; }

        [Required]
        public int ClassworkId { get; set; }

        [Required]
        public int? StudentId { get; set; }

        public string SubmissionText { get; set; }

        // JSON field to store answers (for Quiz, Exam, Activity)
        public string AnswersJson { get; set; }

        public DateTime? SubmittedAt { get; set; }

        public decimal? Grade { get; set; }

        public string Feedback { get; set; }

        public DateTime? GradedAt { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Not Submitted"; // Not Submitted, Submitted, Graded, Late

        // Navigation properties
        [ForeignKey("ClassworkId")]
        public virtual Classwork Classwork { get; set; }

        public virtual User Student { get; set; }

        public virtual ICollection<SubmissionFile> SubmissionFiles { get; set; } = new List<SubmissionFile>();
    }

    public class SubmissionFile
    {
        public int Id { get; set; }

        [Required]
        public int SubmissionId { get; set; }

        [Required]
        [StringLength(255)]
        public string FileName { get; set; }

        [Required]
        [StringLength(500)]
        public string FilePath { get; set; }

        public decimal SizeInMB { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.Now;

        // Navigation property
        [ForeignKey("SubmissionId")]
        public virtual ClassworkSubmission Submission { get; set; }
    }
}

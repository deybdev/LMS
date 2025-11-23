using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LMS.Models
{
    public class Announcement
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TeacherCourseSectionId { get; set; }

        [Required]
        public string Content { get; set; } // Rich text HTML content

        public DateTime PostedAt { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;

        // Navigation Properties
        [ForeignKey("TeacherCourseSectionId")]
        public virtual TeacherCourseSection TeacherCourseSection { get; set; }

        public virtual ICollection<AnnouncementComment> Comments { get; set; } = new List<AnnouncementComment>();
    }

    public class AnnouncementComment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AnnouncementId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public string Comment { get; set; }

        public int? ParentCommentId { get; set; } // For nested replies

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation Properties
        [ForeignKey("AnnouncementId")]
        public virtual Announcement Announcement { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [ForeignKey("ParentCommentId")]
        public virtual AnnouncementComment ParentComment { get; set; }

        public virtual ICollection<AnnouncementComment> Replies { get; set; } = new List<AnnouncementComment>();
    }
}

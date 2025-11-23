using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LMS.Models
{
    public class StudentCourse
    {
        public int Id { get; set; }

        public int StudentId { get; set; }
        public int CourseId { get; set; }
        public int SectionId { get; set; }

        public DateTime DateEnrolled { get; set; } = DateTime.Now;

        public string Day { get; set; }

        public DateTime? TimeFrom { get; set; }
        public DateTime? TimeTo { get; set; }

        public string Status { get; set; } = "Ongoing";

        public virtual User Student { get; set; }
        public virtual Course Course { get; set; }
        public virtual Section Section { get; set; }
    }


}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LMS.Models
{
    public class TeacherCourseSection
    {
        public int Id { get; set; }

        public int TeacherId { get; set; }
        public int CourseId { get; set; }
        public int SectionId { get; set; }

        public int Semester { get; set; }

        public DateTime DateAssigned { get; set; } = DateTime.Now;

        public virtual User Teacher { get; set; }
        public virtual Course Course { get; set; }
        public virtual Section Section { get; set; }
    }
}
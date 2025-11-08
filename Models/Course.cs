using System;
using System.Collections.Generic;

namespace LMS.Models
{
    public class Course
    {
        public int Id { get; set; }
        public string CourseTitle { get; set; }
        public string CourseCode { get; set; }
        public int Semester { get; set; }

        public int YearLevel { get; set; }
        public string Description { get; set; } 
        public int ProgramId { get; set; }
        public DateTime DateCreated { get; set; }

        //public virtual User Teacher { get; set; }
        public virtual ICollection<Material> Materials { get; set; } = new List<Material>();
        public virtual ICollection<CourseUser> CourseUsers { get; set; } = new List<CourseUser>();
        public virtual Program Program { get; set; }

    }

}

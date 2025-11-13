using System;
using System.Collections.Generic;

namespace LMS.Models
{
    public class CurriculumGroupViewModel
    {
        public int ProgramId { get; set; }
        public int YearLevel { get; set; }
        public int Semester { get; set; }
        public string ProgramName { get; set; }
        public string ProgramCode { get; set; }
        public int CourseCount { get; set; }
        public List<Course> Courses { get; set; }
    }
}

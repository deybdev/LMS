using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LMS.Models
{
    public class Course
    {
        public int Id { get; set; }
        public string CourseTitle { get; set; }
        public string CourseCode { get; set; }
        public string Description { get; set; } 
        public DateTime DateCreated { get; set; }
        public int CourseUnit { get; set; }
        
    }

    public class CurriculumCourse
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProgramId { get; set; }
        [Required]
        public int CourseId { get; set; }
        [Required]
        public int YearLevel { get; set; }
        [Required]
        public int Semester { get; set; }

        [ForeignKey("ProgramId")]
        public Program Program { get; set; }
        [ForeignKey("CourseId")]
        public Course Course { get; set; }
    }

}

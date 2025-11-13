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
        
        // Navigation properties
        public virtual ICollection<Material> Materials { get; set; } = new List<Material>();
        
        // Note: No direct relationship with Program
        // Courses are linked to Programs through the CurriculumCourse junction table
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


    public class TeacherCourse
    {
            [Key]
            public int Id { get; set; }

            [Required]
            public int TeacherId { get; set; }

            [Required]
            public int CurriculumCourseId { get; set; }

            [Required]
            public DateTime DateAssigned { get; set; } = DateTime.Now;

            // Navigation properties
            [ForeignKey("TeacherId")]
            public User Teacher { get; set; }

            [ForeignKey("CurriculumCourseId")]
            public CurriculumCourse CurriculumCourse { get; set; }


    }


}

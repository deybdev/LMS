using System;
using System.Collections.Generic;

namespace LMS.Models
{
    public class Material
    {
        public int Id { get; set; }
        
        public int TeacherCourseSectionId { get; set; }
        
        public string Title { get; set; }
        public string Type { get; set; } // Lecture Video, Notes, etc.
        public string Description { get; set; }
        public DateTime UploadedAt { get; set; }

        // Navigation properties
        public virtual TeacherCourseSection TeacherCourseSection { get; set; }
        public virtual ICollection<MaterialFile> MaterialFiles { get; set; } = new List<MaterialFile>();
        public virtual ICollection<MaterialComment> MaterialComments { get; set; } = new List<MaterialComment>();
    }


    public class MaterialFile
    {
        public int Id { get; set; }
        public int MaterialId { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public decimal SizeInMB { get; set; }
        public virtual Material Material { get; set; }
    }
}

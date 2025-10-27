using System;

namespace LMS.Models
{
    public class CourseUser
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public int StudentId { get; set; }

        public DateTime DateAdded { get; set; }
    }
}

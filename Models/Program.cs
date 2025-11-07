using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LMS.Models
{
    public class Program
    {
        public int Id { get; set; }

        public int DepartmentId { get; set; }

        [Required]
        public string ProgramName { get; set; }

        [Required]

        public string ProgramCode { get; set; }

        [Required]
        public int DurationYears { get; set; }

        public DateTime DateCreated { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;


        // ✅ Relationship: Program belongs to one Department
        public Department Department { get; set; }
    }

}
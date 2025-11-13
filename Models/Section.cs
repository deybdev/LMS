using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace LMS.Models
{
    public class Section
    {
        public int Id { get; set; }

        [Required]
        public int ProgramId { get; set; }

        [Required]
        public int YearLevel { get; set; }

        [Required]
        [StringLength(50)]
        public string SectionName { get; set; }

        public DateTime DateCreated { get; set; } = DateTime.Now;

        // Navigation Properties
        [ForeignKey("ProgramId")]
        public virtual Program Program { get; set; }

        // Student count will be calculated from Users table
        // We'll add a computed property for this
        [NotMapped]
        public int StudentCount { get; set; }
    }
}
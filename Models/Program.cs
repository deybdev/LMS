using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LMS.Models
{
    public class Program
    {
        public int Id { get; set; }

        public int DepartmentId { get; set; }

        public string ProgramName { get; set; }


        public string ProgramCode { get; set; }

        public int ProgramDuration { get; set; }

        public DateTime DateCreated { get; set; } = DateTime.Now;

        public Department Department { get; set; }

    }

}
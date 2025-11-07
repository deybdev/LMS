using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LMS.Models
{
    public class Department
    {
        public int Id { get; set; }

        public string DepartmentName { get; set; }

        public string DepartmentCode { get; set; }

        public DateTime DateCreated { get; set; } = DateTime.Now;


        // ✅ List of programs under this department
        public ICollection<Program> Programs { get; set; }

    }
}
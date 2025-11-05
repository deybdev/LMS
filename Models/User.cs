using System;

namespace LMS.Models
{
    public class User
    {
        public int Id { get; set; }

        // Example: STU001, TCH001, ADM001
        public string UserID { get; set; }

        // Personal Information
        public string FirstName { get; set; }
        public string LastName { get; set; }

        // Contact & Department Info
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Department { get; set; }

        // Role: "Student", "Teacher", "IT", "Admin"
        public string Role { get; set; }

        // Login Info
        public string Password { get; set; }
        public DateTime? LastLogin { get; set; }

        // Date Created
        public DateTime DateCreated { get; set; }
    }
}

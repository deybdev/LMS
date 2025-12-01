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
        public DateTime? DateOfBirth { get; set; }

        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        
        // Role: "Student", "Teacher", "IT", "Admin"
        public string Role { get; set; }

        // Department for Teachers/IT/Admin
        public int? DepartmentId { get; set; }

        // Emergency Contact Information
        public string EmergencyContactName { get; set; }
        public string EmergencyContactPhone { get; set; }
        public string EmergencyContactRelationship { get; set; }

        // Profile Picture Path
        public string ProfilePicture { get; set; }

        // Login Info
        public string Password { get; set; }
        public DateTime? LastLogin { get; set; }

        // Password Reset Properties
        public string ResetToken { get; set; }
        public DateTime? ResetTokenExpiry { get; set; }

        // Date Created
        public DateTime DateCreated { get; set; }

        // Navigation Properties
        public virtual Department Department { get; set; }
    }
}

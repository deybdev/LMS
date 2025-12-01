using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace LMS.Models
{
    public class ProfileUpdateViewModel
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "First name is required")]
        [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters")]
        public string FirstName { get; set; }
        
        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters")]
        public string LastName { get; set; }
        
        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime? DateOfBirth { get; set; }
        
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
        public string Email { get; set; }
        
        [Phone(ErrorMessage = "Please enter a valid phone number")]
        [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }
        
        [StringLength(200, ErrorMessage = "Address cannot exceed 200 characters")]
        public string Address { get; set; }
        
        [Display(Name = "Department")]
        public int? DepartmentId { get; set; }
        
        [StringLength(100, ErrorMessage = "Emergency contact name cannot exceed 100 characters")]
        [Display(Name = "Emergency Contact Name")]
        public string EmergencyContactName { get; set; }
        
        [Phone(ErrorMessage = "Please enter a valid emergency contact phone")]
        [StringLength(20, ErrorMessage = "Emergency contact phone cannot exceed 20 characters")]
        [Display(Name = "Emergency Contact Phone")]
        public string EmergencyContactPhone { get; set; }
        
        [StringLength(50, ErrorMessage = "Relationship cannot exceed 50 characters")]
        [Display(Name = "Relationship")]
        public string EmergencyContactRelationship { get; set; }

        public string Role { get; set; }
        public string UserID { get; set; }
        public string ProfilePicture { get; set; }
        
        // For dropdowns
        public IEnumerable<Department> AvailableDepartments { get; set; }
    }

    public class PasswordChangeViewModel
    {
        [Required(ErrorMessage = "Current password is required")]
        [Display(Name = "Current Password")]
        public string CurrentPassword { get; set; }
        
        [Required(ErrorMessage = "New password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long")]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; }
        
        [Required(ErrorMessage = "Please confirm your new password")]
        [Compare("NewPassword", ErrorMessage = "New passwords do not match")]
        [Display(Name = "Confirm New Password")]
        public string ConfirmPassword { get; set; }
    }
}
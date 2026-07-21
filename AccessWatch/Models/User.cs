using System;
using System.ComponentModel.DataAnnotations;

namespace AccessWatch.Models
{
    public enum UserRole
    {
        PersonWithDisability,
        PlatformAdministrator,
        AccessibilityInspector,
        FacilityMaintenanceOfficer
    }

    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        [Required]
        public UserRole Role { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
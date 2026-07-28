using System.ComponentModel.DataAnnotations;
using AccessWatch.Models;

namespace AccessWatch.ViewModels
{
    // Used by the Admin to create accounts for roles other than
    // Person with Disability (which self-registers via AccountController).
    public class CreateUserViewModel
    {
        [Required, StringLength(100)]
        public string Name { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 6)]
        public string Password { get; set; }

        [Required]
        public UserRole Role { get; set; } = UserRole.AccessibilityInspector;
    }

    public class CategoryViewModel
    {
        public int CategoryId { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; }

        [StringLength(500)]
        public string Description { get; set; }
    }
}
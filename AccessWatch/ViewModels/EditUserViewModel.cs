using System.ComponentModel.DataAnnotations;
using AccessWatch.Models;

namespace AccessWatch.ViewModels
{
    public class EditUserViewModel
    {
        public int UserId { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        public UserRole Role { get; set; }

        // Optional -- only set a new password if the admin actually types one.
        // Leave blank to keep the user's existing password unchanged.
        [DataType(DataType.Password)]
        public string? NewPassword { get; set; }
    }
}
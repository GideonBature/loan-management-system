using System.ComponentModel.DataAnnotations;

namespace FirstLend.Application.Dtos.Request
{
    public class CreateAdminRequest
    {
        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100, ErrorMessage = "Full name must be less than 100 characters")]
        public string FullName { get; set; } = "";

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Email format is invalid")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters")]
        public string Password { get; set; } = "";

        [Phone(ErrorMessage = "Phone number format is invalid")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Admin role is required")]
        [StringLength(50, ErrorMessage = "Role must be less than 50 characters")]
        public string Role { get; set; } = "Admin";
    }
}

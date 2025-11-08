using System.ComponentModel.DataAnnotations;

namespace FirstLend.Application.Dtos.Request
{
    public class ResetPasswordRequest
    {
        [Required]
        public string Token { get; set; } = "";

        [Required]
        [StringLength(100, MinimumLength = 8)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
            ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character.")]
        public string NewPassword { get; set; } = "";
    }
}
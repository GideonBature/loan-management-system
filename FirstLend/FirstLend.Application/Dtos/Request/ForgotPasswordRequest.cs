using System.ComponentModel.DataAnnotations;

namespace FirstLend.Application.Dtos.Request
{
    public class ForgotPasswordRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";
    }
}
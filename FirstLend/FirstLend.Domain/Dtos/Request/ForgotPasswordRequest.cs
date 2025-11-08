using System.ComponentModel.DataAnnotations;

namespace FirstLend.Domain.Dtos.Request
{
    public class ForgotPasswordRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";
    }
}
using System.ComponentModel.DataAnnotations;

namespace FirstLend.Domain.Dtos.Request
{
    public class LoginRequest
    {
        [Required]
        public string EmailOrUsername { get; set; } = "";

        [Required]
        public string Password { get; set; } = "";

        [Required]
        public string UserType { get; set; } = "customer"; // "customer" or "admin"
    }
}
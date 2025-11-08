using System.ComponentModel.DataAnnotations;

namespace FirstLend.Domain.Dtos.Request
{
    public class RefreshTokenRequest
    {
        [Required]
        public string RefreshToken { get; set; } = "";
    }
}
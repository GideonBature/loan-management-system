using System.ComponentModel.DataAnnotations;

namespace FirstLend.Application.Dtos.Request
{
    public class RefreshTokenRequest
    {
        [Required]
        public string RefreshToken { get; set; } = "";
    }
}
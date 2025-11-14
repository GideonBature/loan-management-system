using System.ComponentModel.DataAnnotations;
using Swashbuckle.AspNetCore.Annotations;

namespace FirstLend.Domain.Dtos.Request;

[SwaggerSchema(Description = "Request to resend email verification OTP")]
public class ResendOtpRequest
{
    [Required]
    [EmailAddress]
    [SwaggerSchema(Description = "The email address to resend OTP to")]
    public string Email { get; set; } = string.Empty;
}

using System.ComponentModel.DataAnnotations;
using Swashbuckle.AspNetCore.Annotations;

namespace FirstLend.Domain.Dtos.Request;

[SwaggerSchema(Description = "Request to verify email with OTP")]
public class VerifyEmailRequest
{
    [Required]
    [EmailAddress]
    [SwaggerSchema(Description = "The email address to verify")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [SwaggerSchema(Description = "The 6-digit OTP code sent to the email (or bypass code)")]
    public string Token { get; set; } = string.Empty;
    
    // Alias for Token to support both 'token' and 'otp' in requests
    public string Otp 
    { 
        get => Token; 
        set => Token = value; 
    }
}

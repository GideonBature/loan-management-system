namespace FirstLend.Domain.Abstractions;

public interface IEmailService
{
    Task<bool> SendEmailVerificationOtpAsync(string email, string userName, string otp);
    Task<bool> SendWelcomeEmailAsync(string email, string userName);
}

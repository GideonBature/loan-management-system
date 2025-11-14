using System.Net;
using System.Net.Mail;
using FirstLend.Domain.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FirstLend.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly string _smtpUsername;
    private readonly string _smtpPassword;
    private readonly string _fromEmail;
    private readonly string _fromName;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        _smtpHost = configuration["Email:SmtpHost"] ?? "smtp.gmail.com";
        _smtpPort = int.Parse(configuration["Email:SmtpPort"] ?? "587");
        _smtpUsername = configuration["Email:SmtpUsername"] ?? "";
        _smtpPassword = configuration["Email:SmtpPassword"] ?? "";
        _fromEmail = configuration["Email:FromEmail"] ?? _smtpUsername;
        _fromName = configuration["Email:FromName"] ?? "FirstLend";
    }

    public async Task<bool> SendEmailVerificationOtpAsync(string email, string userName, string otp)
    {
        try
        {
            var subject = "Verify Your Email - FirstLend";
            var body = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #1e3a8a; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
                        .content {{ background-color: #f9fafb; padding: 30px; border-radius: 0 0 5px 5px; }}
                        .otp-box {{ background-color: white; border: 2px dashed #1e3a8a; padding: 20px; text-align: center; margin: 20px 0; border-radius: 5px; }}
                        .otp-code {{ font-size: 32px; font-weight: bold; color: #1e3a8a; letter-spacing: 5px; }}
                        .footer {{ margin-top: 20px; text-align: center; font-size: 12px; color: #666; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>Welcome to FirstLend!</h1>
                        </div>
                        <div class='content'>
                            <p>Hello <strong>{userName}</strong>,</p>
                            <p>Thank you for registering with FirstLend. To complete your registration, please verify your email address using the OTP code below:</p>
                            
                            <div class='otp-box'>
                                <p style='margin: 0; font-size: 14px; color: #666;'>Your OTP Code</p>
                                <p class='otp-code'>{otp}</p>
                                <p style='margin: 0; font-size: 12px; color: #666;'>This code will expire in 10 minutes</p>
                            </div>
                            
                            <p><strong>Note:</strong> If you didn't create an account with FirstLend, please ignore this email.</p>
                            
                            <p>Best regards,<br>The FirstLend Team</p>
                        </div>
                        <div class='footer'>
                            <p>&copy; 2025 FirstLend. All rights reserved.</p>
                        </div>
                    </div>
                </body>
                </html>
            ";

            return await SendEmailAsync(email, subject, body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error sending OTP email to {email}");
            return false;
        }
    }

    public async Task<bool> SendWelcomeEmailAsync(string email, string userName)
    {
        try
        {
            var subject = "Welcome to FirstLend - Your Account is Ready!";
            var body = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #1e3a8a; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
                        .content {{ background-color: #f9fafb; padding: 30px; border-radius: 0 0 5px 5px; }}
                        .button {{ display: inline-block; background-color: #1e3a8a; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
                        .footer {{ margin-top: 20px; text-align: center; font-size: 12px; color: #666; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>🎉 Welcome to FirstLend!</h1>
                        </div>
                        <div class='content'>
                            <p>Hello <strong>{userName}</strong>,</p>
                            <p>Your email has been successfully verified! Your FirstLend account is now active.</p>
                            
                            <h3>What's Next?</h3>
                            <ul>
                                <li>Complete your KYC verification to unlock all features</li>
                                <li>Upload required documents (Government ID, Proof of Address, etc.)</li>
                                <li>Build your credit score</li>
                                <li>Apply for loans with competitive rates</li>
                            </ul>
                            
                            <p style='text-align: center;'>
                                <a href='{_configuration["Frontend:BaseUrl"]}' class='button'>Get Started</a>
                            </p>
                            
                            <p>If you have any questions, feel free to reach out to our support team.</p>
                            
                            <p>Best regards,<br>The FirstLend Team</p>
                        </div>
                        <div class='footer'>
                            <p>&copy; 2025 FirstLend. All rights reserved.</p>
                        </div>
                    </div>
                </body>
                </html>
            ";

            return await SendEmailAsync(email, subject, body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error sending welcome email to {email}");
            return false;
        }
    }

    private async Task<bool> SendEmailAsync(string to, string subject, string body)
    {
        try
        {
            using var smtpClient = new SmtpClient(_smtpHost, _smtpPort)
            {
                Credentials = new NetworkCredential(_smtpUsername, _smtpPassword),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_fromEmail, _fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            mailMessage.To.Add(to);

            await smtpClient.SendMailAsync(mailMessage);
            _logger.LogInformation($"Email sent successfully to {to}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to send email to {to}");
            return false;
        }
    }
}

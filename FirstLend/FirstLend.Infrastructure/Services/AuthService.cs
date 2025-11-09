using FirstLend.Domain.Abstractions;
using FirstLend.Domain.Dtos.Request;
using FirstLend.Domain.Dtos.Response;
using FirstLend.Domain.Enums;
using FirstLend.Infrastructure.Data;
using FirstLend.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace FirstLend.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly FirstLendDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            FirstLendDbContext context,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _configuration = configuration;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            try
            {
                // Check if email already exists
                var existingUser = await _userManager.FindByEmailAsync(request.Email);
                if (existingUser != null)
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "Email already registered",
                        Code = "EMAIL_EXISTS"
                    };
                }

                // Check if phone already exists
                var phoneExists = await _context.Users.AnyAsync(u => u.PhoneNumber == request.Phone);
                if (phoneExists)
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "Phone number already registered",
                        Code = "PHONE_EXISTS"
                    };
                }

                // Create new user
                var user = new ApplicationUser
                {
                    UserName = request.Email,
                    Email = request.Email,
                    PhoneNumber = request.Phone,
                    FirstName = request.FullName.Split(' ').FirstOrDefault() ?? "",
                    LastName = string.Join(" ", request.FullName.Split(' ').Skip(1)),
                    Address = request.Address,
                    UserType = UserType.Customer,
                    Status = UserStatus.Active,
                    EmailVerified = false,
                    PhoneVerified = false
                };

                var result = await _userManager.CreateAsync(user, request.Password);
                if (!result.Succeeded)
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "Registration failed",
                        Code = "REGISTRATION_FAILED",
                        Errors = result.Errors.Select(e => new ValidationError
                        {
                            Field = e.Code,
                            Message = e.Description
                        }).ToList()
                    };
                }

                // Assign Customer role to new user
                await _userManager.AddToRoleAsync(user, "Customer");

                var response = new RegisterResponse
                {
                    UserId = user.Id,
                    Email = user.Email!,
                    FullName = request.FullName,
                    UserType = user.UserType
                };

                return new AuthResponse
                {
                    Success = true,
                    Message = "Registration successful. Please log in.",
                    Data = response
                };
            }
            catch (Exception)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Internal server error",
                    Code = "INTERNAL_ERROR"
                };
            }
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            try
            {
                // Find user by email or username
                var user = await _userManager.FindByEmailAsync(request.EmailOrUsername) ??
                          await _userManager.FindByNameAsync(request.EmailOrUsername);

                if (user == null)
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "Invalid email or password",
                        Code = "INVALID_CREDENTIALS"
                    };
                }

                // Check user type
                var requestedUserType = Enum.Parse<UserType>(request.UserType, true);
                if (user.UserType != requestedUserType)
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = $"User type mismatch. This is a {user.UserType.ToString().ToLower()} account.",
                        Code = "USER_TYPE_MISMATCH"
                    };
                }

                // Check user status
                if (user.Status != UserStatus.Active)
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "Account is suspended",
                        Code = "ACCOUNT_SUSPENDED"
                    };
                }

                // Verify password
                var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
                if (!result.Succeeded)
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "Invalid email or password",
                        Code = "INVALID_CREDENTIALS"
                    };
                }

                // Update last login
                user.LastLoginAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);

                // Generate tokens
                var token = await GenerateJwtToken(user);
                var refreshToken = GenerateRefreshToken();

                // Store refresh token (in a real app, hash it and store in DB)
                // For now, just return it

                var loginResponse = new LoginResponse
                {
                    Token = token,
                    RefreshToken = refreshToken,
                    User = new UserResponse
                    {
                        UserId = user.Id,
                        Email = user.Email!,
                        FullName = $"{user.FirstName} {user.LastName}".Trim(),
                        PhoneNumber = user.PhoneNumber ?? "",
                        Address = user.Address,
                        UserType = user.UserType,
                        Status = user.Status,
                        CreatedAt = user.CreatedAt
                    }
                };

                return new AuthResponse
                {
                    Success = true,
                    Message = "Login successful",
                    Data = loginResponse
                };
            }
            catch (Exception)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Internal server error",
                    Code = "INTERNAL_ERROR"
                };
            }
        }

        public Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request)
        {
            // Simplified - in real app, validate refresh token from DB
            // For now, just return new tokens
            return Task.FromResult(new AuthResponse
            {
                Success = false,
                Message = "Refresh token functionality not implemented yet",
                Code = "NOT_IMPLEMENTED"
            });
        }

        public Task<AuthResponse> LogoutAsync(string userId)
        {
            // Simplified - in real app, blacklist the token
            return Task.FromResult(new AuthResponse
            {
                Success = true,
                Message = "Logged out successfully"
            });
        }

        public async Task<AuthResponse> ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(request.Email);
                
                // Always return success to prevent email enumeration
                // Don't reveal if the email exists or not for security
                if (user == null)
                {
                    return new AuthResponse
                    {
                        Success = true,
                        Message = "If the email exists, a reset link has been sent"
                    };
                }

                // Generate password reset token
                var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                
                // Hash the token for storage
                var tokenHash = Convert.ToBase64String(
                    System.Security.Cryptography.SHA256.HashData(
                        System.Text.Encoding.UTF8.GetBytes(resetToken)));

                // Store the token in database
                var passwordResetToken = new FirstLend.Domain.Entities.PasswordResetToken
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    TokenHash = tokenHash,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(30), // Token expires in 30 minutes
                    CreatedAt = DateTime.UtcNow
                };

                _context.PasswordResetTokens.Add(passwordResetToken);
                await _context.SaveChangesAsync();

                // In a real application, you would send an email here with the reset link
                // For now, we'll just return the token in the response (for testing only!)
                // TODO: Implement email service to send reset link
                // Example: await _emailService.SendPasswordResetEmail(user.Email, resetToken);

                return new AuthResponse
                {
                    Success = true,
                    Message = "If the email exists, a reset link has been sent",
                    Data = new { resetToken } // Remove this in production!
                };
            }
            catch (Exception)
            {
                return new AuthResponse
                {
                    Success = true,
                    Message = "If the email exists, a reset link has been sent"
                };
            }
        }

        public async Task<AuthResponse> ResetPasswordAsync(ResetPasswordRequest request)
        {
            try
            {
                // Hash the provided token
                var tokenHash = Convert.ToBase64String(
                    System.Security.Cryptography.SHA256.HashData(
                        System.Text.Encoding.UTF8.GetBytes(request.Token)));

                // Find the reset token
                var resetTokenRecord = await _context.PasswordResetTokens
                    .FirstOrDefaultAsync(t => 
                        t.TokenHash == tokenHash && 
                        t.ExpiresAt > DateTime.UtcNow && 
                        t.UsedAt == null);

                if (resetTokenRecord == null)
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "Invalid or expired reset token",
                        Code = "INVALID_TOKEN"
                    };
                }

                // Find the user
                var user = await _userManager.FindByIdAsync(resetTokenRecord.UserId);
                if (user == null)
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "User not found",
                        Code = "USER_NOT_FOUND"
                    };
                }

                // Reset the password
                var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
                if (!result.Succeeded)
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "Failed to reset password",
                        Code = "RESET_FAILED",
                        Errors = result.Errors.Select(e => new ValidationError
                        {
                            Field = e.Code,
                            Message = e.Description
                        }).ToList()
                    };
                }

                // Mark the token as used
                resetTokenRecord.UsedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return new AuthResponse
                {
                    Success = true,
                    Message = "Password reset successful"
                };
            }
            catch (Exception)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Internal server error",
                    Code = "INTERNAL_ERROR"
                };
            }
        }

        public async Task<UserResponse?> GetCurrentUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return null;

            return new UserResponse
            {
                UserId = user.Id,
                Email = user.Email!,
                FullName = $"{user.FirstName} {user.LastName}".Trim(),
                PhoneNumber = user.PhoneNumber ?? "",
                Address = user.Address,
                UserType = user.UserType,
                Status = user.Status,
                CreatedAt = user.CreatedAt
            };
        }

        private async Task<string> GenerateJwtToken(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email!),
                new Claim("userType", user.UserType.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
            };

            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Secret"] ?? "default-secret-key"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"] ?? "firstlend",
                audience: _configuration["Jwt:Audience"] ?? "firstlend-api",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            var randomBytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }
    }
}
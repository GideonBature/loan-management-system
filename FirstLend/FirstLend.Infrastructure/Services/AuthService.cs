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
                var token = GenerateJwtToken(user);
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
                        UserType = user.UserType,
                        Status = user.Status
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

        public Task<AuthResponse> ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            // Simplified - in real app, generate token and send email
            return Task.FromResult(new AuthResponse
            {
                Success = true,
                Message = "If the email exists, a reset link has been sent"
            });
        }

        public Task<AuthResponse> ResetPasswordAsync(ResetPasswordRequest request)
        {
            // Simplified - in real app, validate token and reset password
            return Task.FromResult(new AuthResponse
            {
                Success = false,
                Message = "Reset password functionality not implemented yet",
                Code = "NOT_IMPLEMENTED"
            });
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
                UserType = user.UserType,
                Status = user.Status
            };
        }

        private string GenerateJwtToken(ApplicationUser user)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email!),
                new Claim("userType", user.UserType.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
            };

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
using FirstLend.Application.Dtos.Request;
using FirstLend.Application.Dtos.Response;

namespace FirstLend.Application.Abstractions
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request);
        Task<AuthResponse> LogoutAsync(string userId);
        Task<AuthResponse> ForgotPasswordAsync(ForgotPasswordRequest request);
        Task<AuthResponse> ResetPasswordAsync(ResetPasswordRequest request);
        Task<UserResponse?> GetCurrentUserAsync(string userId);
    }
}
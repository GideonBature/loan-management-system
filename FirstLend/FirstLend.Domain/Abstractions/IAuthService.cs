using FirstLend.Domain.Dtos.Request;
using FirstLend.Domain.Dtos.Response;

namespace FirstLend.Domain.Abstractions
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
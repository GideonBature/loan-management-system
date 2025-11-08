namespace FirstLend.Domain.Dtos.Response
{
    public class LoginResponse
    {
        public string Token { get; set; } = "";
        public string RefreshToken { get; set; } = "";
        public UserResponse User { get; set; } = new();
    }
}
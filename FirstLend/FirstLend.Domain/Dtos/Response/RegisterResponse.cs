using FirstLend.Domain.Enums;

namespace FirstLend.Domain.Dtos.Response
{
    public class RegisterResponse
    {
        public string UserId { get; set; } = "";
        public string Email { get; set; } = "";
        public string FullName { get; set; } = "";
        public UserType UserType { get; set; }
    }
}
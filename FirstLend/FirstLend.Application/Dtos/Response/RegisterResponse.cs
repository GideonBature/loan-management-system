using FirstLend.Domain.Enums;

namespace FirstLend.Application.Dtos.Response
{
    public class RegisterResponse
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = "";
        public string FullName { get; set; } = "";
        public UserType UserType { get; set; }
    }
}
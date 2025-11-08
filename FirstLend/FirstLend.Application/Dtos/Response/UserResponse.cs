using FirstLend.Domain.Enums;
using System.Text.Json.Serialization;

namespace FirstLend.Application.Dtos.Response
{
    public class UserResponse
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = "";
        public string FullName { get; set; } = "";
        public string PhoneNumber { get; set; } = "";
        public string Address { get; set; } = "";
        
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public UserType UserType { get; set; }
        
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public UserStatus Status { get; set; }
        
        public DateTime CreatedAt { get; set; }
    }
}
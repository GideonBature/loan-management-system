using System.Text.Json.Serialization;
using FirstLend.Domain.Enums;

namespace FirstLend.Application.Dtos.Response
{
    public class AdminUserResponse
    {
        public string Id { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string PhoneNumber { get; set; } = "";
        public string Address { get; set; } = "";
        
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public UserType UserType { get; set; }
        
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public UserStatus Status { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public int TotalLoans { get; set; }
        public decimal TotalBorrowed { get; set; }
        public string LoanStatus { get; set; } = ""; // "Active Loan", "No Loan", "Overdue"
    }
}

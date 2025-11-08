using System.Text.Json.Serialization;
using FirstLend.Domain.Enums;

namespace FirstLend.Application.Dtos.Response
{
    public class LoanResponse
    {
        public Guid Id { get; set; }
        public string BorrowerId { get; set; } = ""; // Changed from Guid to string
        public Guid LoanTypeId { get; set; }
        
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LoanStatus Status { get; set; }
        
        public decimal Principal { get; set; }
        public decimal Rate { get; set; }
        public int Term { get; set; }
        public decimal OutstandingBalance { get; set; }
        public decimal AmountDue { get; set; }
        public string EmploymentStatus { get; set; } = "";
        public decimal MonthlyIncome { get; set; }
        public DateTime? NextPaymentDate { get; set; }
        public string Purpose { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime? DueAt { get; set; }
        
        // Navigation properties
        public string BorrowerName { get; set; } = "";
        public string BorrowerEmail { get; set; } = "";
        public string LoanTypeName { get; set; } = "";
        public decimal LoanTypeInterest { get; set; }
    }
}

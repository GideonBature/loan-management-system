using FirstLend.Domain.Enums;
using System.Text.Json.Serialization;

namespace FirstLend.Application.Dtos.Response
{
    public class AdminLoanResponse
    {
        public Guid Id { get; set; }
        public string BorrowerName { get; set; } = "";
        public string BorrowerEmail { get; set; } = "";
        public string BorrowerId { get; set; } = "";
        public decimal Principal { get; set; }
        public decimal AmountDue { get; set; }
        public string LoanTypeName { get; set; } = "";
        public decimal Rate { get; set; }
        public int Term { get; set; }
        public string EmploymentStatus { get; set; } = "";
        public decimal MonthlyIncome { get; set; }
        public string Purpose { get; set; } = "";
        
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LoanStatus Status { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime NextPaymentDate { get; set; }
        public DateTime DueAt { get; set; }
    }
}

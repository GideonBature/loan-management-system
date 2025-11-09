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
        
        /// <summary>
        /// Calculated payment progress percentage (0-100)
        /// </summary>
        public decimal PaymentProgress 
        { 
            get 
            {
                if (Principal <= 0) return 0;
                var amountPaid = Principal - OutstandingBalance;
                var progress = (amountPaid / Principal) * 100;
                return Math.Round(Math.Max(0, Math.Min(100, progress)), 2);
            } 
        }
    }
}
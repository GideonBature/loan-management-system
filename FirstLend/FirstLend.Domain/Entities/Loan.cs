using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using FirstLend.Domain.Enums;

namespace FirstLend.Domain.Entities
{
    public class Loan
    {
        [Key]
        public Guid Id { get; set; }
        public string BorrowerId { get; set; } = ""; // Changed to string to match ApplicationUser.Id
        public Guid LoanTypeId { get; set; }
        public LoanStatus Status { get; set; } = LoanStatus.pending;
        public decimal Principal { get; set; } 
        public decimal Rate { get; set; } 
        public int Term { get; set; } 
        public decimal OutstandingBalance { get; set; } 
        public decimal AmountDue { get; set; }
        public string EmploymentStatus { get; set; } = "";
        public decimal MonthlyIncome { get; set; }
        public DateTime NextPaymentDate { get; set; }
        public string Purpose { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ApprovedAt { get; set; }
        public DateTime? ActivatedAt { get; set; }
        public DateTime DueAt { get; set; }

        // Navigation properties removed - will be configured in DbContext
        public LoanType? LoanType { get; set; }
    }
}
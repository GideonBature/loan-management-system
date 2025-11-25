using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace FirstLend.Domain.Entities
{
    public class PaymentHistory
    {
        [Key]
        public Guid Id { get; set; }
        public Guid LoanId { get; set; }
        public Guid TransactionId { get; set; }
        public string UserId { get; set; } = ""; // Changed to string to match ApplicationUser.Id
        public decimal Amount { get; set; }
        public decimal Principal { get; set; }
        public decimal Interest { get; set; }
        public string Method { get; set; } = "";
        public string Status { get; set; } = "";
        public string DueAt { get; set; } = "";
        public string CreatedAt { get; set; } = "";


        public Loan? Loan { get; set; }
    }
}
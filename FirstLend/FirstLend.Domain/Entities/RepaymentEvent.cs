
// Simple repayment event (month-ish) for payment-history analysis
using System.ComponentModel.DataAnnotations;
namespace FirstLend.Domain.Entities
{
    public class RepaymentEvent
    {
        [Key]
            public Guid Id { get; set; }
            public string Period { get; set; }     // e.g., "04-2022" or "2022-04"
            public string Status { get; set; }     // "paid", "pending", "missed", "late"
        }
    
}
using System.ComponentModel.DataAnnotations;
using FirstLend.Domain.Enums;

namespace FirstLend.Domain.Entities
{
    public class CreditAccount
    {
        [Key]
        public Guid Id { get; set; }
        public string Institution { get; set; }
        public AccountType Type { get; set; }
        public PerformanceStatus PerformanceStatus { get; set; }
        public DateTime DateOpened { get; set; }
        public DateTime? ClosedDate { get; set; }   // null if open
        public decimal OpeningBalance { get; set; } // loan principal or 0
        public decimal CurrentBalance { get; set; } // current outstanding
        public decimal? CreditLimit { get; set; }   // for revolving accounts
        public decimal RepaymentAmount { get; set; } // monthly payment, helpful for heuristics
        public List<RepaymentEvent> RepaymentHistory { get; set; } = new List<RepaymentEvent>();
    }

}

using System;

namespace FirstLend.Application.Dtos.Request
{
    public class InitiatePaymentRequest
    {
        public Guid LoanId { get; set; }
        public decimal Amount { get; set; }
    }
}

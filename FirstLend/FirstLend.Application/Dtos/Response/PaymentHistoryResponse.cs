using System;

namespace FirstLend.Application.Dtos.Response
{
    public class PaymentHistoryResponse
    {
        public Guid Id { get; set; }
        public string TransactionId { get; set; } = "";
        public Guid LoanId { get; set; }
        public string LoanReference { get; set; } = "";
        public decimal Amount { get; set; }
        public decimal Principal { get; set; }
        public decimal Interest { get; set; }
        public string Method { get; set; } = "";
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class PaymentHistorySummaryResponse
    {
        public int TotalPayments { get; set; }
        public decimal TotalAmountPaid { get; set; }
        public int SuccessfulPayments { get; set; }
        public int FailedOrPendingPayments { get; set; }
    }
}

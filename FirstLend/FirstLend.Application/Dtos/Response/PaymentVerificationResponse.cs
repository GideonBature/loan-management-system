using System;

namespace FirstLend.Application.Dtos.Response
{
    public class PaymentVerificationResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public decimal Amount { get; set; }
        public string Status { get; set; } = "";
        public string Reference { get; set; } = "";
        public DateTime PaidAt { get; set; }
        public string Channel { get; set; } = "";
    }
}

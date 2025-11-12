namespace FirstLend.Application.Dtos.Response
{
    public class AdminPaymentResponse
    {
        public string PaymentId { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public string LoanId { get; set; } = "";
        public DateTime DueDate { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }
}

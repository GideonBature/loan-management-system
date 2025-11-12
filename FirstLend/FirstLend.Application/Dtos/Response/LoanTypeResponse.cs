namespace FirstLend.Application.Dtos.Response
{
    public class LoanTypeResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public decimal Interest { get; set; }
        public int MaxTermMonths { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

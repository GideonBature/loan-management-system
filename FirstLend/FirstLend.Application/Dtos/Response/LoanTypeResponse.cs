namespace FirstLend.Application.Dtos.Response
{
    public class LoanTypeResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public decimal Interest { get; set; }
    }
}

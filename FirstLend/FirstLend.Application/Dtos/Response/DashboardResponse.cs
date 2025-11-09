namespace FirstLend.Application.Dtos.Response
{
    public class DashboardSummaryResponse
    {
        public int TotalLoans { get; set; }
        public int PendingLoans { get; set; }
        public int ApprovedLoans { get; set; }
        public int RejectedLoans { get; set; }
        public decimal TotalLoanAmount { get; set; }
        public decimal DisbursedAmount { get; set; }
        public int TotalCustomers { get; set; }
        public int ActiveCustomers { get; set; }
        public decimal AverageMonthlyIncome { get; set; }
    }

    public class LoanStatisticsResponse
    {
        public int TotalLoans { get; set; }
        public int PendingCount { get; set; }
        public int ApprovedCount { get; set; }
        public int ActiveCount { get; set; }
        public int CompletedCount { get; set; }
        public int RejectedCount { get; set; }
        public int DefaultedCount { get; set; }
        public decimal TotalPrincipal { get; set; }
        public decimal AverageAmount { get; set; }
        public double AverageRate { get; set; }
    }

    public class UserStatisticsResponse
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int InactiveUsers { get; set; }
        public int SuspendedUsers { get; set; }
        public int UsersWithLoans { get; set; }
        public int UsersWithoutLoans { get; set; }
        public decimal AverageMonthlyIncome { get; set; }
        public decimal TotalBorrowedAmount { get; set; }
    }
}

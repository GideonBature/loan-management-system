using System.ComponentModel.DataAnnotations;

namespace FirstLend.Application.Dtos.Request
{
    public class CreateLoanRequest
    {
        [Required(ErrorMessage = "Loan type is required")]
        [StringLength(100, ErrorMessage = "Loan type name must be less than 100 characters")]
        public string LoanTypeName { get; set; } = "";

        [Required(ErrorMessage = "Principal amount is required")]
        [Range(1, double.MaxValue, ErrorMessage = "Principal must be greater than 0")]
        public decimal Principal { get; set; }

        [Range(0.01, 100, ErrorMessage = "Interest rate must be between 0.01 and 100")]
        public decimal? Rate { get; set; }

        [Required(ErrorMessage = "Term is required")]
        [Range(1, 360, ErrorMessage = "Term must be between 1 and 360 months")]
        public int Term { get; set; }

        [Required(ErrorMessage = "Employment status is required")]
        [StringLength(50, ErrorMessage = "Employment status must be less than 50 characters")]
        public string EmploymentStatus { get; set; } = "";

        [Required(ErrorMessage = "Monthly income is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Monthly income must be 0 or greater")]
        public decimal MonthlyIncome { get; set; }

        [Required(ErrorMessage = "Purpose is required")]
        [StringLength(500, ErrorMessage = "Purpose must be less than 500 characters")]
        public string Purpose { get; set; } = "";
    }
}

using System.ComponentModel.DataAnnotations;

namespace FirstLend.Application.Dtos.Request
{
    public class CreateLoanTypeRequest
    {
        [Required(ErrorMessage = "Loan type name is required")]
        [StringLength(100, ErrorMessage = "Name must be between 1 and 100 characters", MinimumLength = 1)]
        public string Name { get; set; } = "";

        [Required(ErrorMessage = "Interest rate is required")]
        [Range(0.01, 100, ErrorMessage = "Interest rate must be between 0.01 and 100")]
        public decimal Interest { get; set; }
    }
}

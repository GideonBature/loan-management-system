using System.ComponentModel.DataAnnotations;

namespace FirstLend.Application.Dtos.Request
{
    public class UpdateLoanTypeRequest
    {
        [StringLength(100, ErrorMessage = "Name must be between 1 and 100 characters", MinimumLength = 1)]
        public string? Name { get; set; }

        [Range(0.01, 100, ErrorMessage = "Interest rate must be between 0.01 and 100")]
        public decimal? Interest { get; set; }

        [Range(1, 600, ErrorMessage = "Maximum term must be between 1 and 600 months")]
        public int? MaxTermMonths { get; set; }
    }
}

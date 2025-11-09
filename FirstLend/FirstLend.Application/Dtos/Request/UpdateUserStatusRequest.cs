using System.ComponentModel.DataAnnotations;

namespace FirstLend.Application.Dtos.Request
{
    public class UpdateUserStatusRequest
    {
        [Required(ErrorMessage = "Status is required")]
        [StringLength(50, ErrorMessage = "Status must be less than 50 characters")]
        public string Status { get; set; } = "";
    }
}

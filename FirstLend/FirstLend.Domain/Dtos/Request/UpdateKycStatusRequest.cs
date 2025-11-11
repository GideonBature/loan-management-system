using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using Swashbuckle.AspNetCore.Annotations;

namespace FirstLend.Domain.Dtos.Request
{
    [SwaggerSchema(Description = "Request to update KYC verification status")]
    public class UpdateKycStatusRequest
    {
        [Required]
        [Description("The KYC verified status (true/false)")]
        public bool IsVerified { get; set; }

        [Description("Optional reason for status update")]
        public string? Reason { get; set; }
    }
}

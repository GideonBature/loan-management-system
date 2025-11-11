using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using Swashbuckle.AspNetCore.Annotations;

namespace FirstLend.Domain.Dtos.Request
{
    [SwaggerSchema(Description = "KYC Verification request - Verify user identity using BVN and NIN. Full name is retrieved from the logged-in user profile.")]
    public class KycVerificationRequest
    {
        [Required]
        [RegularExpression(@"^\d{11}$", ErrorMessage = "BVN must be 11 digits")]
        [Description("Bank Verification Number - 11 digits (REQUIRED)")]
        public string BVN { get; set; } = "22227777222";

        [Description("National Identification Number - Optional for cross-verification")]
        public string? NIN { get; set; }
    }
}

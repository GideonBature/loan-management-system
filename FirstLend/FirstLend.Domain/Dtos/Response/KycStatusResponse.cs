using System.ComponentModel;
using Swashbuckle.AspNetCore.Annotations;

namespace FirstLend.Domain.Dtos.Response
{
    [SwaggerSchema(Description = "Response containing KYC verification status")]
    public class KycStatusResponse
    {
        [Description("Indicates if the request was successful")]
        public bool Success { get; set; } = true;

        [Description("Human-readable message")]
        public string Message { get; set; } = "";

        [Description("Machine-readable code")]
        public string Code { get; set; } = "";

        [Description("KYC status data")]
        public KycStatusData? Data { get; set; }
    }

    [SwaggerSchema(Description = "KYC status information")]
    public class KycStatusData
    {
        [Description("Whether the user's KYC is verified")]
        public bool IsVerified { get; set; }

        [Description("Date and time when KYC was verified")]
        public DateTime? VerificationDate { get; set; }

        [Description("The BVN that was verified")]
        public string? BVN { get; set; }

        [Description("The NIN that was verified")]
        public string? NIN { get; set; }

        [Description("User's full name from profile")]
        public string? FullName { get; set; }

        [Description("User's email")]
        public string? Email { get; set; }

        [Description("User's phone number")]
        public string? PhoneNumber { get; set; }
    }
}

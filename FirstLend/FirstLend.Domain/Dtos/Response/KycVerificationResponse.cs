using System.ComponentModel;
using Swashbuckle.AspNetCore.Annotations;

namespace FirstLend.Domain.Dtos.Response
{
    [SwaggerSchema(Description = "Response containing KYC verification results")]
    public class KycVerificationResponse
    {
        [Description("Indicates if the verification was successful")]
        public bool Success { get; set; } = false;

        [Description("Descriptive message about the verification result")]
        public string Message { get; set; } = "";

        [Description("Machine-readable code for the result (e.g., KYC_VERIFIED, BVN_VERIFICATION_FAILED)")]
        public string Code { get; set; } = "";

        [Description("Detailed verification data including all field checks")]
        public KycVerificationData? Data { get; set; }
    }

    [SwaggerSchema(Description = "Detailed KYC verification information")]
    public class KycVerificationData
    {
        [Description("Whether the user has been successfully verified")]
        public bool IsVerified { get; set; } = false;

        [Description("User's full name from Mono records")]
        public string FullName { get; set; } = "";

        [Description("User's date of birth from Mono records (Format: DD-MM-YYYY)")]
        public string DateOfBirth { get; set; } = "";

        [Description("User's gender (male/female)")]
        public string Gender { get; set; } = "";

        [Description("List of phone numbers associated with the user")]
        public List<string> PhoneNumbers { get; set; } = new();

        [Description("List of email addresses associated with the user")]
        public List<string> EmailAddresses { get; set; } = new();

        [Description("Historical addresses from credit records")]
        public List<AddressHistoryDto> AddressHistory { get; set; } = new();
        
        [Description("BVN verification result")]
        public FieldVerificationDto BvnVerification { get; set; } = new();

        [Description("NIN verification result")]
        public FieldVerificationDto NinVerification { get; set; } = new();

        [Description("Full name verification result")]
        public FieldVerificationDto FullNameVerification { get; set; } = new();
        
        [Description("List of warnings for fields that did not match")]
        public List<string> Warnings { get; set; } = new();
    }

    [SwaggerSchema(Description = "Verification result for a single field")]
    public class FieldVerificationDto
    {
        [Description("Whether the provided value matches the verified value")]
        public bool IsMatched { get; set; } = false;

        [Description("The value provided by the user")]
        public string ProvidedValue { get; set; } = "";

        [Description("The value from official records")]
        public string VerifiedValue { get; set; } = "";

        [Description("Message describing the verification result")]
        public string Message { get; set; } = "";
    }

    [SwaggerSchema(Description = "Historical address information from credit records")]
    public class AddressHistoryDto
    {
        [Description("The actual address")]
        public string Address { get; set; } = "";

        [Description("Type of address (e.g., Residential, Business)")]
        public string Type { get; set; } = "";

        [Description("Date when address was reported (Format: DD-MM-YYYY)")]
        public string DateReported { get; set; } = "";
    }
}

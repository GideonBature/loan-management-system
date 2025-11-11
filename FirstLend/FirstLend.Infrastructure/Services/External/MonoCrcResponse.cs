using System.Text.Json.Serialization;

namespace FirstLend.Infrastructure.Services.External
{
    /// <summary>
    /// DTO for Mono Credit History CRC API Response
    /// </summary>
    public class MonoCrcResponse
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = "";

        [JsonPropertyName("message")]
        public string Message { get; set; } = "";

        [JsonPropertyName("data")]
        public MonoCrcData Data { get; set; } = new();
    }

    public class MonoCrcData
    {
        [JsonPropertyName("providers")]
        public List<string> Providers { get; set; } = new();

        [JsonPropertyName("profile")]
        public MonoProfile Profile { get; set; } = new();

        [JsonPropertyName("credit_history")]
        public List<MonoCreditHistory> CreditHistory { get; set; } = new();
    }

    public class MonoProfile
    {
        [JsonPropertyName("full_name")]
        public string FullName { get; set; } = "";

        [JsonPropertyName("date_of_birth")]
        public string DateOfBirth { get; set; } = "";

        [JsonPropertyName("address_history")]
        public List<MonoAddressHistory> AddressHistory { get; set; } = new();

        [JsonPropertyName("email_address")]
        public List<string> EmailAddress { get; set; } = new();

        [JsonPropertyName("phone_number")]
        public List<string> PhoneNumber { get; set; } = new();

        [JsonPropertyName("gender")]
        public string Gender { get; set; } = "";

        [JsonPropertyName("identifications")]
        public List<MonoIdentification> Identifications { get; set; } = new();
    }

    public class MonoAddressHistory
    {
        [JsonPropertyName("address")]
        public string Address { get; set; } = "";

        [JsonPropertyName("type")]
        public string Type { get; set; } = "";

        [JsonPropertyName("date_reported")]
        public string DateReported { get; set; } = "";
    }

    public class MonoIdentification
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "";

        [JsonPropertyName("no")]
        public string Number { get; set; } = "";
    }

    public class MonoCreditHistory
    {
        [JsonPropertyName("institution")]
        public string Institution { get; set; } = "";

        [JsonPropertyName("history")]
        public List<MonoLoanHistory> History { get; set; } = new();
    }

    public class MonoLoanHistory
    {
        [JsonPropertyName("date_opened")]
        public string DateOpened { get; set; } = "";

        [JsonPropertyName("institution")]
        public string Institution { get; set; } = "";

        [JsonPropertyName("opening_balance")]
        public decimal OpeningBalance { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = "";

        [JsonPropertyName("performance_status")]
        public string PerformanceStatus { get; set; } = "";

        [JsonPropertyName("tenor")]
        public int Tenor { get; set; }

        [JsonPropertyName("closed_date")]
        public string ClosedDate { get; set; } = "";

        [JsonPropertyName("loan_status")]
        public string LoanStatus { get; set; } = "";

        [JsonPropertyName("repayment_frequency")]
        public string RepaymentFrequency { get; set; } = "";

        [JsonPropertyName("repayment_amount")]
        public decimal RepaymentAmount { get; set; }
    }
}

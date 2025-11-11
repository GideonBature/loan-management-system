using System.ComponentModel;
using Swashbuckle.AspNetCore.Annotations;
using FirstLend.Domain.Models;

namespace FirstLend.Domain.Dtos.Response
{
    [SwaggerSchema(Description = "Response containing user's credit score information")]
    public class CreditScoreResponse
    {
        [Description("Indicates if the request was successful")]
        public bool Success { get; set; } = true;

        [Description("Human-readable message")]
        public string Message { get; set; } = "";

        [Description("Machine-readable code")]
        public string Code { get; set; } = "";

        [Description("Credit score data")]
        public CreditScoreData? Data { get; set; }
    }

    [SwaggerSchema(Description = "Credit score details and breakdown")]
    public class CreditScoreData
    {
        [Description("Overall credit score (0-100)")]
        public double Score { get; set; }

        [Description("Credit score rating (Excellent, Very Good, Good, Fair, Poor)")]
        public string Rating { get; set; } = "";

        [Description("Number of credit accounts analyzed")]
        public int TotalAccounts { get; set; }

        [Description("Detailed score breakdown by components")]
        public ScoreBreakdown? Breakdown { get; set; }

        [Description("Date when score was calculated")]
        public DateTime CalculatedAt { get; set; }
    }
}

namespace FirstLend.Application.Dtos.Response
{
    public class AiInsightsResponse
    {
        public string Insight { get; set; } = "";
        public string Mode { get; set; } = ""; // "HighConfidence" or "PredictiveAnalytics"
        public List<string> Tags { get; set; } = new();
        public Dictionary<string, object> Metrics { get; set; } = new();
    }
}

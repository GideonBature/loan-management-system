namespace FirstLend.Application.Behaviour.Models
{
    
  public class ScoreBreakdown
    {
        public double PaymentHistoryScore { get; set; }
        public double AmountsOwedScore { get; set; }
        public double LengthOfHistoryScore { get; set; }
        public double CreditMixScore { get; set; }
        public double NewCreditScore { get; set; }
        public double TotalScore { get; set; }
    }
}
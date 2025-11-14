using FirstLend.Domain.Entities;
using FirstLend.Domain.Enums;
using FirstLend.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
public class CreditScoreEngine
{
    public double WPaymentHistory { get; set; } = 0.35;
    public double WAmountsOwed { get; set; } = 0.30;
    public double WLengthOfHistory { get; set; } = 0.15;
    public double WCreditMix { get; set; } = 0.10;
    public double WNewCredit { get; set; } = 0.10;

    public int NewCreditWindowMonths { get; set; } = 12;

    public ScoreBreakdown CalculateScore(IEnumerable<CreditAccount> accounts, int hardInquiries = 0, DateTime? asOfDate = null)
    {
        var asOf = asOfDate ?? DateTime.UtcNow;

        var accList = accounts?.ToList() ?? new List<CreditAccount>();

        var paymentScore = CalculatePaymentHistoryScore(accList);
        var amountsScore = CalculateAmountsOwedScore(accList);
        var lengthScore = CalculateLengthOfHistoryScore(accList, asOf);
        var mixScore = CalculateCreditMixScore(accList);
        var newCreditScore = CalculateNewCreditScore(accList, hardInquiries, asOf);

        var total = paymentScore * WPaymentHistory
                  + amountsScore * WAmountsOwed
                  + lengthScore * WLengthOfHistory
                  + mixScore * WCreditMix
                  + newCreditScore * WNewCredit;

        return new ScoreBreakdown
        {
            PaymentHistoryScore = Math.Round(paymentScore, 2),
            AmountsOwedScore = Math.Round(amountsScore, 2),
            LengthOfHistoryScore = Math.Round(lengthScore, 2),
            CreditMixScore = Math.Round(mixScore, 2),
            NewCreditScore = Math.Round(newCreditScore, 2),
            TotalScore = Math.Round(total, 2)
        };
    }

    #region Component calculators
    private double CalculatePaymentHistoryScore(List<CreditAccount> accounts)
    {
        if (!accounts.Any()) return 50.0; // neutral

        if (accounts.Any(a => a.PerformanceStatus == PerformanceStatus.Default
                           || a.PerformanceStatus == PerformanceStatus.WrittenOff
                           || a.PerformanceStatus == PerformanceStatus.NonPerforming
                           || a.PerformanceStatus == PerformanceStatus.InCollections))
        {
            return 20.0;
        }

        var allEvents = accounts.SelectMany(a => a.RepaymentHistory).ToList();
        if (allEvents.Any())
        {
            int paid = allEvents.Count(e => StringEqualsIgnoreCase(e.Status, "paid"));
            int late = allEvents.Count(e => StringEqualsIgnoreCase(e.Status, "late"));
            int pending = allEvents.Count(e => StringEqualsIgnoreCase(e.Status, "pending"));
            int missed = allEvents.Count(e => StringEqualsIgnoreCase(e.Status, "missed"));

            int total = Math.Max(1, paid + late + pending + missed);

            double goodRatio = (double)paid / total;

            double penalty = 0.0;
            penalty += late * 0.05;
            penalty += pending * 0.03;
            penalty += missed * 0.15;

            double raw = goodRatio * 100.0 - penalty * 100.0;
            return Clamp(raw, 0, 100);
        }
        else
        {
            double score = 70.0;
            foreach (var a in accounts)
            {
                switch (a.PerformanceStatus)
                {
                    case PerformanceStatus.Performing:
                        score = Math.Max(score, 90);
                        break;
                    case PerformanceStatus.UnderWatch:
                    case PerformanceStatus.Restructured:
                        score = Math.Min(80, score);
                        break;
                    case PerformanceStatus.Delinquent30:
                        score = Math.Min(60, score);
                        break;
                    case PerformanceStatus.Delinquent60:
                        score = Math.Min(45, score);
                        break;
                    case PerformanceStatus.Delinquent90:
                        score = Math.Min(30, score);
                        break;
                    default:
                        break;
                }
            }
            return score;
        }
    }

    private double CalculateAmountsOwedScore(List<CreditAccount> accounts)
    {
        if (!accounts.Any()) return 75.0;

        var utilizations = new List<double>();

        foreach (var acc in accounts)
        {
            if (acc.Type == AccountType.Revolving && acc.CreditLimit.HasValue && acc.CreditLimit.Value > 0)
            {
                var util = (double)(acc.CurrentBalance / acc.CreditLimit.Value);
                utilizations.Add(util);
            }
            else
            {
                if (acc.OpeningBalance > 0)
                {
                    var util = (double)(acc.CurrentBalance / acc.OpeningBalance);
                    utilizations.Add(util);
                }
                else
                {
                    utilizations.Add(0.0);
                }
            }
        }

        // average utilization
        double avgUtil = utilizations.Average();
        double score = 95 - avgUtil * 120;
        return Clamp(score, 0, 100);
    }

    private double CalculateLengthOfHistoryScore(List<CreditAccount> accounts, DateTime asOf)
    {
        if (!accounts.Any()) return 40.0;

        var agesYears = accounts.Select(a =>
        {
            var end = a.ClosedDate ?? asOf;
            return (end - a.DateOpened).TotalDays / 365.25;
        }).ToList();

        double oldest = agesYears.Max();
        double average = agesYears.Average();

        double baseScore;
        if (oldest < 2) baseScore = 30;
        else if (oldest < 5) baseScore = 60;
        else if (oldest < 10) baseScore = 85;
        else baseScore = 95;

        double ratio = average / (oldest == 0 ? 1 : oldest);
        baseScore = baseScore * (0.7 + 0.3 * ratio);
        return Clamp(baseScore, 0, 100);
    }

    private double CalculateCreditMixScore(List<CreditAccount> accounts)
    {
        if (!accounts.Any()) return 30.0;
        var types = accounts.Select(a => a.Type).Distinct().Count();

        double score;
        switch (types)
        {
            case 1: score = 35; break;
            case 2: score = 60; break;
            case 3: score = 80; break;
            default: score = 95; break;
        }
        return score;
    }

    private double CalculateNewCreditScore(List<CreditAccount> accounts, int hardInquiries, DateTime asOf)
    {
        var windowStart = asOf.AddMonths(-NewCreditWindowMonths);
        int recentAccounts = accounts.Count(a => a.DateOpened >= windowStart);

        double score = 90.0;

        for (int i = 1; i <= recentAccounts; i++)
        {
            score -= 20.0 / i;
        }

        score -= hardInquiries * 6.0;

        return Clamp(score, 0, 100);
    }

    #endregion

    #region Utilities
    private static double Clamp(double v, double lo, double hi)
    {
        if (v < lo) return lo;
        if (v > hi) return hi;
        return v;
    }

    private static bool StringEqualsIgnoreCase(string a, string b)
        => string.Equals(a?.Trim(), b?.Trim(), StringComparison.OrdinalIgnoreCase);
    #endregion
}
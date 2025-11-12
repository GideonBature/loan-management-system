using FirstLend.Application.Abstractions;
using FirstLend.Application.Dtos.Request;
using FirstLend.Application.Dtos.Response;
using FirstLend.Domain.Entities;
using FirstLend.Infrastructure.Data;
using FirstLend.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FirstLend.Infrastructure.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IConfiguration _configuration;
        private readonly FirstLendDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly HttpClient _httpClient;
        private readonly string _paystackSecretKey;
        private readonly string _paystackPublicKey;

        public PaymentService(
            IConfiguration configuration,
            FirstLendDbContext context,
            UserManager<ApplicationUser> userManager,
            IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _context = context;
            _userManager = userManager;
            _httpClient = httpClientFactory.CreateClient();
            _paystackSecretKey = _configuration["Paystack:SecretKey"] ?? "";
            _paystackPublicKey = _configuration["Paystack:PublicKey"] ?? "";
            
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_paystackSecretKey}");
        }

        public async Task<PaymentInitiationResponse> InitiatePayment(InitiatePaymentRequest request, string userId)
        {
            try
            {
                var loan = await _context.Loans
                    .Include(l => l.LoanType)
                    .FirstOrDefaultAsync(l => l.Id == request.LoanId && l.BorrowerId == userId);

                if (loan == null)
                {
                    return new PaymentInitiationResponse
                    {
                        Success = false,
                        Message = "Loan not found or you don't have access to it"
                    };
                }

                if (loan.Status != Domain.Enums.LoanStatus.active)
                {
                    return new PaymentInitiationResponse
                    {
                        Success = false,
                        Message = "Can only make payment for active loans"
                    };
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return new PaymentInitiationResponse
                    {
                        Success = false,
                        Message = "User not found"
                    };
                }

                // Convert amount to kobo (Paystack uses kobo for NGN)
                var amountInKobo = (int)(request.Amount * 100);

                // Generate unique reference
                var reference = $"FL-{Guid.NewGuid().ToString().Substring(0, 8)}-{DateTime.UtcNow.Ticks}";

                var payload = new
                {
                    email = user.Email,
                    amount = amountInKobo,
                    reference = reference,
                    currency = "NGN",
                    metadata = new
                    {
                        loan_id = loan.Id.ToString(),
                        user_id = userId,
                        loan_type = loan.LoanType?.Name ?? "Unknown"
                    },
                    callback_url = _configuration["Paystack:CallbackUrl"] ?? "http://localhost:5128/api/payments/callback"
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.PostAsync("https://api.paystack.co/transaction/initialize", content);
                var responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var paystackResponse = JsonSerializer.Deserialize<PaystackInitializeResponse>(responseString);
                    
                    if (paystackResponse?.status == true && paystackResponse.data != null)
                    {
                        // Store pending transaction in database
                        var paymentHistory = new PaymentHistory
                        {
                            Id = Guid.NewGuid(),
                            LoanId = loan.Id,
                            TransactionId = Guid.NewGuid(), // Generate a new Guid for the transaction
                            UserId = userId, // userId is already a string from JWT claims
                            Amount = request.Amount,
                            Principal = 0, // Will be calculated after verification
                            Interest = 0,  // Will be calculated after verification
                            Method = "Paystack",
                            Status = "Pending",
                            CreatedAt = DateTime.UtcNow.ToString("o")
                        };

                        _context.PaymentHistories.Add(paymentHistory);
                        await _context.SaveChangesAsync();

                        return new PaymentInitiationResponse
                        {
                            Success = true,
                            Message = "Payment initialized successfully",
                            AuthorizationUrl = paystackResponse.data.authorization_url,
                            AccessCode = paystackResponse.data.access_code,
                            Reference = paystackResponse.data.reference
                        };
                    }
                }

                return new PaymentInitiationResponse
                {
                    Success = false,
                    Message = $"Failed to initialize payment: {responseString}"
                };
            }
            catch (Exception ex)
            {
                return new PaymentInitiationResponse
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }

        public async Task<PaymentVerificationResponse> VerifyPayment(string reference)
        {
            try
            {
                var response = await _httpClient.GetAsync($"https://api.paystack.co/transaction/verify/{reference}");
                var responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var paystackResponse = JsonSerializer.Deserialize<PaystackVerifyResponse>(responseString);

                    if (paystackResponse?.status == true && paystackResponse.data != null)
                    {
                        var data = paystackResponse.data;
                        
                        // Convert from kobo to naira
                        var amount = data.amount / 100m;

                        // Parse loan_id from metadata
                        if (!Guid.TryParse(data.metadata.loan_id, out var loanId))
                        {
                            return new PaymentVerificationResponse
                            {
                                Success = false,
                                Message = "Invalid loan ID in payment metadata"
                            };
                        }

                        // Get user_id from metadata (it's already a string)
                        var userId = data.metadata.user_id;

                        var paymentHistory = await _context.PaymentHistories
                            .FirstOrDefaultAsync(p => p.LoanId == loanId && p.UserId == userId && p.Status == "Pending");

                        if (paymentHistory != null)
                        {
                            if (data.status == "success")
                            {
                                var loan = await _context.Loans.FindAsync(loanId);
                                
                                if (loan != null)
                                {
                                    // Calculate total interest for the loan
                                    var totalInterest = loan.AmountDue - loan.Principal;
                                    
                                    // Calculate how much interest is remaining
                                    var principalPaid = loan.Principal - loan.OutstandingBalance;
                                    var totalPaid = loan.Principal + totalInterest - loan.AmountDue;
                                    var interestPaid = totalPaid - principalPaid;
                                    var remainingInterest = Math.Max(0, totalInterest - interestPaid);
                                    
                                    // Apply payment: Interest first, then principal
                                    var interestPayment = Math.Min(remainingInterest, amount);
                                    var principalPayment = amount - interestPayment;

                                    paymentHistory.Status = "Success";
                                    paymentHistory.Amount = amount;
                                    paymentHistory.Interest = interestPayment;
                                    paymentHistory.Principal = principalPayment;

                                    // Update loan balances
                                    loan.AmountDue -= amount;
                                    loan.OutstandingBalance -= principalPayment;
                                    
                                    // Ensure balances don't go negative
                                    if (loan.OutstandingBalance < 0) loan.OutstandingBalance = 0;
                                    if (loan.AmountDue < 0) loan.AmountDue = 0;
                                    
                                    // If loan is fully paid (both must be <= 0)
                                    if (loan.AmountDue <= 0 && loan.OutstandingBalance <= 0)
                                    {
                                        loan.Status = Domain.Enums.LoanStatus.completed;
                                        loan.AmountDue = 0;
                                        loan.OutstandingBalance = 0;
                                    }

                                    await _context.SaveChangesAsync();
                                }

                                return new PaymentVerificationResponse
                                {
                                    Success = true,
                                    Message = "Payment verified successfully",
                                    Amount = amount,
                                    Status = data.status,
                                    Reference = data.reference,
                                    PaidAt = data.paid_at,
                                    Channel = data.channel
                                };
                            }
                            else
                            {
                                // Payment was declined, abandoned, or failed
                                paymentHistory.Status = "Failed";
                                paymentHistory.Amount = 0;
                                paymentHistory.Interest = 0;
                                paymentHistory.Principal = 0;
                                await _context.SaveChangesAsync();

                                return new PaymentVerificationResponse
                                {
                                    Success = false,
                                    Message = $"Payment {data.status}",
                                    Amount = amount,
                                    Status = data.status,
                                    Reference = data.reference,
                                    PaidAt = data.paid_at,
                                    Channel = data.channel
                                };
                            }
                        }

                        // If no pending payment history found, still return the Paystack status
                        return new PaymentVerificationResponse
                        {
                            Success = data.status == "success",
                            Message = data.status == "success" ? "Payment verified successfully" : $"Payment {data.status}",
                            Amount = amount,
                            Status = data.status,
                            Reference = data.reference,
                            PaidAt = data.paid_at,
                            Channel = data.channel
                        };
                    }
                }

                return new PaymentVerificationResponse
                {
                    Success = false,
                    Message = "Payment verification failed"
                };
            }
            catch (Exception ex)
            {
                return new PaymentVerificationResponse
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }

        public async Task<bool> ProcessPaystackWebhook(string payload, string signature)
        {
            try
            {
                // Verify webhook signature
                var hash = ComputeHash(payload, _paystackSecretKey);
                
                if (hash != signature)
                {
                    return false;
                }

                var webhookData = JsonSerializer.Deserialize<PaystackWebhookData>(payload);
                
                if (webhookData?.@event == "charge.success" && webhookData.data != null)
                {
                    var reference = webhookData.data.reference;
                    await VerifyPayment(reference);
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private string ComputeHash(string input, string secret)
        {
            var encoding = new UTF8Encoding();
            var keyBytes = encoding.GetBytes(secret);
            var messageBytes = encoding.GetBytes(input);

            using (var hmac = new HMACSHA512(keyBytes))
            {
                var hashBytes = hmac.ComputeHash(messageBytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }

        // Paystack response models
        private class PaystackInitializeResponse
        {
            public bool status { get; set; }
            public string message { get; set; } = "";
            public PaystackInitializeData? data { get; set; }
        }

        private class PaystackInitializeData
        {
            public string authorization_url { get; set; } = "";
            public string access_code { get; set; } = "";
            public string reference { get; set; } = "";
        }

        private class PaystackVerifyResponse
        {
            public bool status { get; set; }
            public string message { get; set; } = "";
            public PaystackVerifyData? data { get; set; }
        }

        private class PaystackVerifyData
        {
            public int amount { get; set; }
            public string status { get; set; } = "";
            public string reference { get; set; } = "";
            public DateTime paid_at { get; set; }
            public string channel { get; set; } = "";
            public PaystackMetadata metadata { get; set; } = new();
        }

        private class PaystackMetadata
        {
            public string loan_id { get; set; } = "";
            public string user_id { get; set; } = "";
            public string loan_type { get; set; } = "";
        }

        private class PaystackWebhookData
        {
            public string @event { get; set; } = "";
            public PaystackVerifyData? data { get; set; }
        }
    }
}

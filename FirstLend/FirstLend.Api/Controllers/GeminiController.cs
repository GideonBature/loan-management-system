using FirstLend.Application.Abstractions;
using FirstLend.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FirstLend.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GeminiController : ControllerBase
    {
        private readonly IGeminiService _geminiService;
        private readonly FirstLendDbContext _context;
        private readonly ILogger<GeminiController> _logger;

        public GeminiController(
            IGeminiService geminiService, 
            FirstLendDbContext context,
            ILogger<GeminiController> logger)
        {
            _geminiService = geminiService;
            _context = context;
            _logger = logger;
        }

        [HttpPost("analyze")]
        public async Task<IActionResult> AnalyzeLoan([FromBody] string prompt)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(prompt))
                    return BadRequest("Prompt cannot be empty.");

                // Fetch real loan types from the database
                var loanTypes = await _context.LoanTypes
                    .Where(lt => lt.IsActive)
                    .OrderBy(lt => lt.Name)
                    .ToListAsync();

                // Build loan types information from database
                var loanTypesInfo = string.Join("\n", loanTypes.Select(lt => 
                    $"- {lt.Name}: {lt.Interest}% per annum{(lt.MaxTermMonths > 0 ? $", Max Term: {lt.MaxTermMonths} months" : "")}"));

                // Get total number of customers and loans for context
                var totalCustomers = await _context.Users.CountAsync();
                var totalActiveLoans = await _context.Loans
                    .Where(l => l.Status == Domain.Enums.LoanStatus.active)
                    .CountAsync();
                var totalLoansIssued = await _context.Loans.CountAsync();

                // System message prefix with REAL data
                string prefix = @$"You are FirstLend AI Assistant, a helpful financial advisor for FirstLend - a modern loan management platform.

ABOUT FIRSTLEND:
FirstLend is a digital lending platform that provides quick, transparent, and accessible loans to individuals and businesses. We are committed to financial inclusion and helping our customers achieve their financial goals.

COMPANY STATISTICS:
- Total Customers: {totalCustomers:N0}
- Active Loans: {totalActiveLoans:N0}
- Total Loans Issued: {totalLoansIssued:N0}

OUR LOAN PRODUCTS (Real-time from our database):
{loanTypesInfo}

ELIGIBILITY REQUIREMENTS:
- Must complete KYC verification (BVN, NIN, and valid ID)
- Minimum credit score of 50% required to apply
- Must upload required documents (Government ID, Proof of Address, Bank Statement, Guarantor's Document)
- Must be at least 18 years old
- Must have a valid email and phone number

HOW TO APPLY:
1. Create an account and verify your email
2. Complete KYC verification (BVN and NIN)
3. Upload required documents
4. Check your credit score (automatically calculated from your credit history)
5. Choose a loan product and apply
6. Wait for admin approval
7. Once approved, funds are disbursed to your account

REPAYMENT:
- Flexible repayment terms
- Online payment through Paystack integration
- Track payment history in your dashboard
- Get AI-powered insights to improve your credit score

YOUR ROLE:
- Answer questions about FirstLend's loan products, interest rates, eligibility, and application process
- Provide accurate information based on the data above
- Be helpful, friendly, and professional
- If asked about personal account details, politely redirect them to log in to their dashboard
- If asked non-financial questions, politely say you can only help with loan and financial questions

USER QUESTION: ";

                // Combine system message with user prompt
                string fullPrompt = prefix + prompt;

                // Call Gemini service to get response
                var result = await _geminiService.AnalyzeLoanDataAsync(fullPrompt);

                return Ok(new { 
                    success = true,
                    response = result,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Gemini AI Assistant");
                return StatusCode(500, new { 
                    success = false,
                    message = "Sorry, I encountered an error. Please try again.",
                    error = ex.Message
                });
            }
        }
    }
}

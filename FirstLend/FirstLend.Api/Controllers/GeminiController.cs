using FirstLend.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;


namespace FirstLend.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GeminiController : ControllerBase
    {
        private readonly IGeminiService _geminiService;

        public GeminiController(IGeminiService geminiService)
        {
            _geminiService = geminiService;
        }

        [HttpPost("analyze")]
        public async Task<IActionResult> AnalyzeLoan([FromBody] string prompt)
            {
                if (string.IsNullOrWhiteSpace(prompt))
                return BadRequest("Prompt cannot be empty.");

            // system message prefix
            string prefix = "You are a financial assistant for FirstLend, a loan management application. " +
            "Only respond to questions about loans, banking, or personal finance. " +
            "Keep your answers short, clear, and relevant. " +
            "If the input is unrelated, reply politely: " +
            "\"I'm sorry, I can only answer financial and loan-related questions. Let's try this again.\" " +
            "\n\nHere are the loan types offered by FirstLend and their interest rates:\n" +
            "- Personal Loan: 10% per annum\n" +
            "- Business Loan: 12% per annum\n" +
            "- Student Loan: 6% per annum\n" +
            "- Mortgage Loan: 8% per annum\n" +
            "\nUse this information to answer questions about loans.\n\n";

            // this combines the system message with the user prompt
            string fullPrompt = prefix + prompt;

            // this calls the Gemini service to get a response
            var result = await _geminiService.AnalyzeLoanDataAsync(fullPrompt);

                return Ok(result);
            }

    }
}

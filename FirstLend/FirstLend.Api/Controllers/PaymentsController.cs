using FirstLend.Application.Abstractions;
using FirstLend.Application.Dtos.Request;
using FirstLend.Application.Dtos.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FirstLend.Api.Controllers
{
    [ApiController]
    [Route("api/payments")]
    [Authorize]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly IConfiguration _configuration;

        public PaymentsController(IPaymentService paymentService, IConfiguration configuration)
        {
            _paymentService = paymentService;
            _configuration = configuration;
        }

        /// <summary>
        /// Initialize a loan repayment using Paystack
        /// </summary>
        [HttpPost("initialize")]
        public async Task<IActionResult> InitializePayment([FromBody] InitiatePaymentRequest request)
        {
            try
            {
                // Try to get user ID from "sub" claim first, then fallback to NameIdentifier
                var userId = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
                
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ServiceResponse<PaymentInitiationResponse>
                    {
                        Success = false,
                        Message = "User not authenticated",
                        Code = "401",
                        Data = null
                    });
                }

                var result = await _paymentService.InitiatePayment(request, userId);

                if (result.Success)
                {
                    return Ok(new ServiceResponse<PaymentInitiationResponse>
                    {
                        Success = true,
                        Message = "Payment initialized successfully",
                        Code = "200",
                        Data = result
                    });
                }

                return BadRequest(new ServiceResponse<PaymentInitiationResponse>
                {
                    Success = false,
                    Message = result.Message,
                    Code = "400",
                    Data = null
                });
            }
            catch (FormatException ex)
            {
                return BadRequest(new ServiceResponse<PaymentInitiationResponse>
                {
                    Success = false,
                    Message = $"Error: {ex.Message}",
                    Code = "400",
                    Data = null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ServiceResponse<PaymentInitiationResponse>
                {
                    Success = false,
                    Message = $"Error: {ex.Message}",
                    Code = "500",
                    Data = null
                });
            }
        }
        
        /// <summary>
        /// Verify a payment using Paystack reference
        /// </summary>
        [HttpGet("verify/{reference}")]
        public async Task<IActionResult> VerifyPayment(string reference)
        {
            var result = await _paymentService.VerifyPayment(reference);

            if (result.Success)
            {
                return Ok(new ServiceResponse<PaymentVerificationResponse>
                {
                    Success = true,
                    Message = "Payment verified successfully",
                    Code = "200",
                    Data = result
                });
            }

            return BadRequest(new ServiceResponse<PaymentVerificationResponse>
            {
                Success = false,
                Message = result.Message,
                Code = "400",
                Data = null
            });
        }

        /// <summary>
        /// Payment callback endpoint - handles redirect from Paystack after payment
        /// </summary>
        [HttpGet("callback")]
        [AllowAnonymous]
        public async Task<IActionResult> PaymentCallback([FromQuery] string reference, [FromQuery] string trxref)
        {
            try
            {
                // Use reference or trxref (they should be the same)
                var paymentReference = reference ?? trxref;
                
                if (string.IsNullOrEmpty(paymentReference))
                {
                    // Redirect to frontend with error
                    return Redirect($"{GetFrontendUrl()}/payment/failed?message=No reference provided");
                }

                // Verify the payment
                var result = await _paymentService.VerifyPayment(paymentReference);

                if (result.Success)
                {
                    // Redirect to frontend success page
                    return Redirect($"{GetFrontendUrl()}/payment/success?reference={paymentReference}&amount={result.Amount}");
                }
                else
                {
                    // Redirect to frontend failure page
                    return Redirect($"{GetFrontendUrl()}/payment/failed?reference={paymentReference}&message={Uri.EscapeDataString(result.Message)}");
                }
            }
            catch (Exception ex)
            {
                // Redirect to frontend error page
                return Redirect($"{GetFrontendUrl()}/payment/failed?message={Uri.EscapeDataString(ex.Message)}");
            }
        }

        private string GetFrontendUrl()
        {
            // Get from configuration or use default
            return _configuration["Frontend:BaseUrl"] ?? "http://localhost:8080";
        }

        /// <summary>
        /// Webhook endpoint for Paystack payment notifications
        /// </summary>
        [HttpPost("webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> PaystackWebhook()
        {
            try
            {
                using var reader = new StreamReader(Request.Body);
                var payload = await reader.ReadToEndAsync();
                var signature = Request.Headers["x-paystack-signature"].ToString();

                var result = await _paymentService.ProcessPaystackWebhook(payload, signature);

                if (result)
                {
                    return Ok();
                }

                return BadRequest();
            }
            catch
            {
                return BadRequest();
            }
        }
    }
}
